using System;
using System.Collections.Generic;

namespace Kaname.Ink
{
    public enum Cap { Butt, Round }

    // A linear or radial colour gradient, evaluated in the canvas's user space at the moment of filling (the HTML canvas rule).
    public sealed class Gradient
    {
        readonly bool radial;
        readonly double x0, y0, x1, y1, r1;
        readonly List<(double t, Rgba c)> stops = new List<(double, Rgba)>();

        Gradient(bool radial, double x0, double y0, double x1, double y1, double r1)
        { this.radial = radial; this.x0 = x0; this.y0 = y0; this.x1 = x1; this.y1 = y1; this.r1 = r1; }

        public static Gradient Linear(double x0, double y0, double x1, double y1) => new Gradient(false, x0, y0, x1, y1, 0);
        // a radial gradient from a point (radius 0) out to radius r (the only kind the art uses)
        public static Gradient Radial(double x, double y, double r) => new Gradient(true, x, y, x, y, r);

        public bool IsRadial => radial;
        public V2 Centre => new V2(x0, y0);

        public void AddColorStop(double t, Rgba c) { stops.Add((t, c)); stops.Sort((a, b) => a.t.CompareTo(b.t)); }

        public Rgba At(V2 p)
        {
            double t;
            if (radial) t = r1 <= 0 ? 1 : (p - new V2(x0, y0)).Length / r1;
            else
            {
                var d = new V2(x1 - x0, y1 - y0); double l2 = d.Dot(d);
                t = l2 < 1e-18 ? 0 : (p - new V2(x0, y0)).Dot(d) / l2;
            }
            if (stops.Count == 0) return new Rgba(0, 0, 0, 0);
            if (t <= stops[0].t) return stops[0].c;
            for (int i = 1; i < stops.Count; i++)
                if (t <= stops[i].t) return Rgba.Lerp(stops[i - 1].c, stops[i].c, (float)((t - stops[i - 1].t) / Math.Max(1e-12, stops[i].t - stops[i - 1].t)));
            return stops[stops.Count - 1].c;
        }
    }

    // An immediate-mode 2D vector canvas that records triangles instead of pixels: the same calls as the HTML canvas the JavaScript game draws
    // with (paths, curves, arcs, fill, stroke, gradients, a transform stack, global alpha, one convex clip), so the ink art ports line for line.
    // Triangles come out in the order they were drawn, in the canvas's own units (here, world units with y down), ready to become one mesh.
    // Round caps and joins; a stroke is one run of triangles, so a translucent stroke does not darken where it bends.
    public sealed class InkCanvas
    {
        // ---- the output: a triangle list, drawn in order ----
        public readonly List<V2> Verts = new List<V2>();
        public readonly List<Rgba> Colors = new List<Rgba>();
        public readonly List<int> Indices = new List<int>();
        public readonly Dictionary<string, V2> Marks = new Dictionary<string, V2>();       // named points the art leaves behind for the tests: drawn as nothing, kept as they came out of the transform
        public readonly List<(string name, int first, int count)> Groups = new List<(string, int, int)>();   // named runs of triangles (their first index in Indices and how many indices), for the tests: they draw nothing

        // ---- state (saved and restored together) ----
        struct M
        {
            public double A, B, C, D, E, F;
            public static readonly M Identity = new M { A = 1, D = 1 };
            public M Then(M o) => new M { A = A * o.A + C * o.B, B = B * o.A + D * o.B, C = A * o.C + C * o.D, D = B * o.C + D * o.D, E = A * o.E + C * o.F + E, F = B * o.E + D * o.F + F };
            public V2 Apply(V2 p) => new V2(A * p.X + C * p.Y + E, B * p.X + D * p.Y + F);
            public bool Invert(out M inv)
            {
                double det = A * D - B * C;
                if (Math.Abs(det) < 1e-12) { inv = Identity; return false; }
                double ia = D / det, ib = -B / det, ic = -C / det, id = A / det;
                inv = new M { A = ia, B = ib, C = ic, D = id, E = -(ia * E + ic * F), F = -(ib * E + id * F) };
                return true;
            }
        }

        sealed class State
        {
            public M Ctm = M.Identity;
            public double Alpha = 1;
            public Rgba FillC = new Rgba(0, 0, 0, 1), StrokeC = new Rgba(0, 0, 0, 1);
            public Gradient FillG, StrokeG;
            public double LineWidth = 1;
            public Cap Cap = Cap.Butt;
            public List<V2> Clip;                          // a convex polygon in canvas units
            public State Clone() => (State)MemberwiseClone();
        }

        State st = new State();
        readonly Stack<State> stack = new Stack<State>();

        public double GlobalAlpha { get => st.Alpha; set => st.Alpha = value; }
        public double LineWidth { get => st.LineWidth; set => st.LineWidth = value; }
        public Cap LineCap { get => st.Cap; set => st.Cap = value; }
        public void FillStyle(Rgba c) { st.FillC = c; st.FillG = null; }
        public void FillStyle(Gradient g) { st.FillG = g; }
        public void StrokeStyle(Rgba c) { st.StrokeC = c; st.StrokeG = null; }
        public void StrokeStyle(Gradient g) { st.StrokeG = g; }

        public void Save() { stack.Push(st); st = st.Clone(); }
        public void Restore() { if (stack.Count > 0) st = stack.Pop(); }

        // each transform applies to what is drawn next, inside the transforms already in force (the HTML canvas rule)
        public void Translate(double x, double y) => st.Ctm = st.Ctm.Then(new M { A = 1, D = 1, E = x, F = y });
        public void Scale(double x, double y) => st.Ctm = st.Ctm.Then(new M { A = x, D = y });
        public void Rotate(double a) { double c = Math.Cos(a), s = Math.Sin(a); st.Ctm = st.Ctm.Then(new M { A = c, B = s, C = -s, D = c }); }

        // a named point, taken through the transform in force (the world point the art's own coordinates land on); it draws nothing
        public void Mark(string name, double x, double y) => Marks[name] = st.Ctm.Apply(new V2(x, y));

        // everything drawn between BeginGroup and EndGroup is recorded as one named run of triangles (an empty one is not kept); groups do not nest
        string openGroup; int openFirst;
        public void BeginGroup(string name) { openGroup = name; openFirst = Indices.Count; }
        public void EndGroup() { if (openGroup != null && Indices.Count > openFirst) Groups.Add((openGroup, openFirst, Indices.Count - openFirst)); openGroup = null; }

        public void Clear()
        {
            Verts.Clear(); Colors.Clear(); Indices.Clear(); Marks.Clear(); Groups.Clear(); openGroup = null;
            stack.Clear(); st = new State(); path.Clear(); closed.Clear();
        }

        // ---- paths (kept in user space; the transform in force when fill or stroke is called is applied then) ----
        readonly List<List<V2>> path = new List<List<V2>>();
        readonly List<bool> closed = new List<bool>();

        public void BeginPath() { path.Clear(); closed.Clear(); }

        List<V2> Current => path.Count > 0 && !closed[path.Count - 1] ? path[path.Count - 1] : null;

        public void MoveTo(double x, double y) { path.Add(new List<V2> { new V2(x, y) }); closed.Add(false); }

        public void LineTo(double x, double y)
        {
            var cur = Current;
            if (cur == null)
            {
                V2 start = path.Count > 0 ? path[path.Count - 1][0] : new V2(x, y);
                path.Add(new List<V2> { start }); closed.Add(false); cur = Current;
            }
            cur.Add(new V2(x, y));
        }

        public void ClosePath() { if (path.Count > 0) closed[path.Count - 1] = true; }

        public void Rect(double x, double y, double w, double h)
        {
            MoveTo(x, y); LineTo(x + w, y); LineTo(x + w, y + h); LineTo(x, y + h); ClosePath();
        }

        // a rectangle with rounded corners of radius r (the HTML canvas roundRect)
        public void RoundRect(double x, double y, double w, double h, double r)
        {
            const double Q = Math.PI / 2;
            r = Math.Min(r, Math.Min(w, h) / 2);
            MoveTo(x + r, y);
            Arc(x + w - r, y + r, r, -Q, 0);
            Arc(x + w - r, y + h - r, r, 0, Q);
            Arc(x + r, y + h - r, r, Q, 2 * Q);
            Arc(x + r, y + r, r, 2 * Q, 3 * Q);
            ClosePath();
        }

        public void QuadraticCurveTo(double cx, double cy, double x, double y)
        {
            var cur = Current; if (cur == null) { MoveTo(cx, cy); cur = Current; }
            V2 p0 = cur[cur.Count - 1], c = new V2(cx, cy), p1 = new V2(x, y);
            int n = Segments((c - p0).Length + (p1 - c).Length);
            for (int i = 1; i <= n; i++)
            {
                double t = (double)i / n, u = 1 - t;
                cur.Add(new V2(u * u * p0.X + 2 * u * t * c.X + t * t * p1.X, u * u * p0.Y + 2 * u * t * c.Y + t * t * p1.Y));
            }
        }

        public void BezierCurveTo(double c1x, double c1y, double c2x, double c2y, double x, double y)
        {
            var cur = Current; if (cur == null) { MoveTo(c1x, c1y); cur = Current; }
            V2 p0 = cur[cur.Count - 1], c1 = new V2(c1x, c1y), c2 = new V2(c2x, c2y), p1 = new V2(x, y);
            int n = Segments((c1 - p0).Length + (c2 - c1).Length + (p1 - c2).Length);
            for (int i = 1; i <= n; i++)
            {
                double t = (double)i / n, u = 1 - t, a = u * u * u, b = 3 * u * u * t, c = 3 * u * t * t, d = t * t * t;
                cur.Add(new V2(a * p0.X + b * c1.X + c * c2.X + d * p1.X, a * p0.Y + b * c1.Y + c * c2.Y + d * p1.Y));
            }
        }

        static int Segments(double controlLength) => (int)Math.Max(6, Math.Min(40, Math.Ceiling(controlLength / 1.2)));

        public void Arc(double cx, double cy, double r, double a0, double a1, bool ccw = false) => Ellipse(cx, cy, r, r, 0, a0, a1, ccw);

        public void Ellipse(double cx, double cy, double rx, double ry, double rot, double a0, double a1, bool ccw = false)
        {
            const double Tau = Math.PI * 2;
            double sweep;
            if (!ccw) { sweep = a1 - a0; if (sweep >= Tau) sweep = Tau; else while (sweep < 0) sweep += Tau; }
            else { sweep = a1 - a0; if (sweep <= -Tau) sweep = -Tau; else while (sweep > 0) sweep -= Tau; }
            double rmax = Math.Max(Math.Abs(rx), Math.Abs(ry));
            int n = (int)Math.Max(6, Math.Min(64, Math.Ceiling(Math.Abs(sweep) / Tau * (8 * Math.Sqrt(rmax) + 12))));
            double cr = Math.Cos(rot), sr = Math.Sin(rot);
            for (int i = 0; i <= n; i++)
            {
                double a = a0 + sweep * i / n, ex = Math.Cos(a) * rx, ey = Math.Sin(a) * ry;
                double x = cx + ex * cr - ey * sr, y = cy + ex * sr + ey * cr;
                if (i == 0) { if (Current != null) LineTo(x, y); else MoveTo(x, y); } else LineTo(x, y);
            }
            if (Math.Abs(sweep) >= Tau - 1e-9) ClosePath();
        }

        // ---- clipping: one convex polygon, the current path's first sub-path (the art clips the band to the head's circle) ----
        public void Clip()
        {
            if (path.Count == 0) return;
            var poly = new List<V2>(); foreach (var p in path[0]) poly.Add(st.Ctm.Apply(p));
            st.Clip = poly;
        }

        // ---- fill ----
        public void Fill()
        {
            for (int s = 0; s < path.Count; s++) FillPolygon(path[s], st.FillC, st.FillG);
        }

        public void FillRect(double x, double y, double w, double h)
        {
            FillPolygon(new List<V2> { new V2(x, y), new V2(x + w, y), new V2(x + w, y + h), new V2(x, y + h) }, st.FillC, st.FillG);
        }

        public void StrokeRect(double x, double y, double w, double h)
        {
            StrokePolyline(new List<V2> { new V2(x, y), new V2(x + w, y), new V2(x + w, y + h), new V2(x, y + h) }, true);
        }

        // Rectangles (in canvas units) that fills do not draw into: the stage's solids, so a slash stops at a floor or a pillar instead of passing through it.
        // Each filled polygon is cut into triangles and each triangle is cut against every rectangle (what is left is convex). Strokes ignore it.
        public IReadOnlyList<(double x0, double y0, double x1, double y1)> Exclude;

        void FillPolygon(List<V2> user, Rgba flat, Gradient grad)
        {
            if (user.Count < 3) return;
            var dev = new List<V2>(user.Count); foreach (var p in user) dev.Add(st.Ctm.Apply(p));
            if (st.Clip != null) { dev = ClipConvex(dev, st.Clip); if (dev.Count < 3) return; }
            if (Exclude == null || Exclude.Count == 0) { EmitPolygon(dev, flat, grad); return; }
            var tris = new List<int>(); Triangulate(dev, tris);
            for (int t = 0; t + 2 < tris.Count; t += 3)
            {
                var pieces = new List<List<V2>> { new List<V2> { dev[tris[t]], dev[tris[t + 1]], dev[tris[t + 2]] } };
                foreach (var r in Exclude) pieces = SubtractRect(pieces, r);
                foreach (var pc in pieces) if (pc.Count >= 3) EmitPolygon(pc, flat, grad);
            }
        }

        // convex polygons minus one rectangle: what lies left of it, right of it, and above and below it within its columns (y runs down)
        static List<List<V2>> SubtractRect(List<List<V2>> polys, (double x0, double y0, double x1, double y1) r)
        {
            var res = new List<List<V2>>();
            List<V2> Rect(double x0, double y0, double x1, double y1) => new List<V2> { new V2(x0, y0), new V2(x1, y0), new V2(x1, y1), new V2(x0, y1) };
            const double Big = 1e7;
            foreach (var pg in polys)
            {
                double minX = double.MaxValue, maxX = double.MinValue, minY = double.MaxValue, maxY = double.MinValue;
                foreach (var p in pg) { minX = Math.Min(minX, p.X); maxX = Math.Max(maxX, p.X); minY = Math.Min(minY, p.Y); maxY = Math.Max(maxY, p.Y); }
                if (maxX <= r.x0 || minX >= r.x1 || maxY <= r.y0 || minY >= r.y1) { res.Add(pg); continue; }       // clear of the rectangle
                foreach (var c in new[] { Rect(-Big, -Big, r.x0, Big), Rect(r.x1, -Big, Big, Big), Rect(r.x0, -Big, r.x1, r.y0), Rect(r.x0, r.y1, r.x1, Big) })
                {
                    var piece = ClipConvex(pg, c); if (piece.Count >= 3) res.Add(piece);
                }
            }
            return res;
        }

        // ---- stroke ----
        public void Stroke()
        {
            for (int s = 0; s < path.Count; s++) StrokePolyline(path[s], closed[s]);
        }

        void StrokePolyline(List<V2> src, bool isClosed)
        {
            double h = st.LineWidth / 2; if (h <= 0) return;
            var p = new List<V2>(src.Count);
            foreach (var q in src) if (p.Count == 0 || (q - p[p.Count - 1]).Length > 1e-9) p.Add(q);
            if (isClosed && p.Count > 1 && (p[0] - p[p.Count - 1]).Length < 1e-9) p.RemoveAt(p.Count - 1);
            if (p.Count == 0) return;
            if (p.Count == 1) { if (st.Cap == Cap.Round) Disk(p[0], h); return; }

            int n = p.Count, segs = isClosed ? n : n - 1;
            var dir = new V2[segs]; var nrm = new V2[segs];
            for (int i = 0; i < segs; i++) { dir[i] = (p[(i + 1) % n] - p[i]).Normalized(); nrm[i] = dir[i].Perp; }

            // a vertex is smooth if the turn there is gentle: the stroke is then one continuous strip with a mitred corner
            bool Smooth(int i) { int a = (i - 1 + segs) % segs, b = i % segs; return nrm[a].Dot(nrm[b]) > 0.5; }

            if (isClosed)
            {
                bool all = true; for (int i = 0; i < n && all; i++) all = Smooth(i);
                if (all)
                {
                    var L = new V2[n]; var R = new V2[n];
                    for (int i = 0; i < n; i++) { var m = Miter(nrm[(i - 1 + segs) % segs], nrm[i % segs], h); L[i] = p[i] + m; R[i] = p[i] - m; }
                    for (int i = 0; i < n; i++) { int j = (i + 1) % n; StripQuad(L[i], L[j], R[j], R[i]); }
                    return;
                }
                // a closed outline with a corner: break it at the first corner and treat it as an open run joined by a disk there
                int s0 = 0; while (Smooth(s0)) s0++;
                var q2 = new List<V2>(); for (int i = 0; i <= n; i++) q2.Add(p[(s0 + i) % n]);
                StrokeOpen(q2, h, joinEnds: true);
                return;
            }
            StrokeOpen(p, h, joinEnds: false);
        }

        void StrokeOpen(List<V2> p, double h, bool joinEnds)
        {
            int n = p.Count;
            var dir = new V2[n - 1]; var nrm = new V2[n - 1];
            for (int i = 0; i < n - 1; i++) { dir[i] = (p[i + 1] - p[i]).Normalized(); nrm[i] = dir[i].Perp; }
            var L = new List<V2>(); var R = new List<V2>();                 // the current run's left and right edges
            void Flush()
            {
                for (int i = 0; i + 1 < L.Count; i++) StripQuad(L[i], L[i + 1], R[i + 1], R[i]);
                L.Clear(); R.Clear();
            }
            L.Add(p[0] + nrm[0] * h); R.Add(p[0] - nrm[0] * h);
            if (!joinEnds && st.Cap == Cap.Round) HalfDisk(p[0], nrm[0], dir[0] * -1, h);
            if (joinEnds) Disk(p[0], h);
            for (int i = 1; i < n - 1; i++)
            {
                if (nrm[i - 1].Dot(nrm[i]) > 0.5)
                {
                    var m = Miter(nrm[i - 1], nrm[i], h); L.Add(p[i] + m); R.Add(p[i] - m);
                }
                else
                {
                    L.Add(p[i] + nrm[i - 1] * h); R.Add(p[i] - nrm[i - 1] * h); Flush();
                    Disk(p[i], h);
                    L.Add(p[i] + nrm[i] * h); R.Add(p[i] - nrm[i] * h);
                }
            }
            L.Add(p[n - 1] + nrm[n - 2] * h); R.Add(p[n - 1] - nrm[n - 2] * h);
            Flush();
            if (!joinEnds && st.Cap == Cap.Round) HalfDisk(p[n - 1], nrm[n - 2], dir[n - 2], h);
            if (joinEnds) Disk(p[n - 1], h);
        }

        // the offset at a gentle corner between two segments whose unit normals are n0 and n1: it keeps the stroke's width on both sides
        static V2 Miter(V2 n0, V2 n1, double h) => (n0 + n1) * (h / (1 + n0.Dot(n1)));

        void StripQuad(V2 l0, V2 l1, V2 r1, V2 r0) => EmitUserTris(new[] { l0, l1, r1, r0 }, new[] { 0, 1, 2, 0, 2, 3 }, stroke: true);

        void Disk(V2 c, double r)
        {
            int n = DiskSegments(r); var pts = new V2[n + 1]; pts[0] = c;
            for (int i = 0; i < n; i++) { double a = Math.PI * 2 * i / n; pts[i + 1] = c + new V2(Math.Cos(a), Math.Sin(a)) * r; }
            var idx = new int[n * 3]; for (int i = 0; i < n; i++) { idx[i * 3] = 0; idx[i * 3 + 1] = 1 + i; idx[i * 3 + 2] = 1 + (i + 1) % n; }
            EmitUserTris(pts, idx, stroke: true);
        }

        // a half disk on the end of a line: it starts at the line's normal and sweeps through `outward`
        void HalfDisk(V2 c, V2 normal, V2 outward, double r)
        {
            int n = DiskSegments(r); double a0 = Math.Atan2(normal.Y, normal.X), sgn = normal.Cross(outward) > 0 ? 1 : -1;
            var pts = new V2[n + 2]; pts[0] = c;
            for (int i = 0; i <= n; i++) { double a = a0 + sgn * Math.PI * i / n; pts[i + 1] = c + new V2(Math.Cos(a), Math.Sin(a)) * r; }
            var idx = new int[n * 3]; for (int i = 0; i < n; i++) { idx[i * 3] = 0; idx[i * 3 + 1] = 1 + i; idx[i * 3 + 2] = 2 + i; }
            EmitUserTris(pts, idx, stroke: true);
        }

        static int DiskSegments(double r) => (int)Math.Max(8, Math.Min(40, 6 * Math.Sqrt(Math.Abs(r)) + 8));

        // ---- emitting triangles ----
        void EmitUserTris(V2[] user, int[] idx, bool stroke)
        {
            int b = Verts.Count;
            Rgba flat = stroke ? st.StrokeC : st.FillC; Gradient g = stroke ? st.StrokeG : st.FillG;
            for (int i = 0; i < user.Length; i++) { Verts.Add(st.Ctm.Apply(user[i])); Colors.Add(Shade(flat, g, user[i])); }
            foreach (int i in idx) Indices.Add(b + i);
        }

        void EmitPolygon(List<V2> dev, Rgba flat, Gradient grad)
        {
            st.Ctm.Invert(out var inv);
            int b = Verts.Count;
            if (grad != null && grad.IsRadial)
            {
                // a radial gradient is a colour per distance from its centre, so the shape is fanned out from that centre (the art's glows are circles)
                var centre = st.Ctm.Apply(grad.Centre);
                Verts.Add(centre); Colors.Add(Shade(flat, grad, grad.Centre));
                foreach (var d in dev) { Verts.Add(d); Colors.Add(Shade(flat, grad, inv.Apply(d))); }
                for (int i = 0; i < dev.Count; i++) { Indices.Add(b); Indices.Add(b + 1 + i); Indices.Add(b + 1 + (i + 1) % dev.Count); }
                return;
            }
            foreach (var d in dev) { Verts.Add(d); Colors.Add(Shade(flat, grad, grad != null ? inv.Apply(d) : d)); }
            var tris = new List<int>(); Triangulate(dev, tris);
            foreach (int i in tris) Indices.Add(b + i);
        }

        Rgba Shade(Rgba flat, Gradient g, V2 userPoint)
        {
            var c = g != null ? g.At(userPoint) : flat;
            return c.MulAlpha((float)st.Alpha);
        }

        // ---- polygon tools ----
        // ear clipping (a fan when the polygon is convex); the art's shapes are simple polygons
        public static void Triangulate(List<V2> poly, List<int> outIdx)
        {
            int n = poly.Count; if (n < 3) return;
            double area = 0; for (int i = 0; i < n; i++) area += poly[i].Cross(poly[(i + 1) % n]);
            if (Math.Abs(area) < 1e-12) return;
            double orient = area > 0 ? 1 : -1;
            bool convex = true;
            for (int i = 0; i < n && convex; i++)
            {
                double cr = (poly[(i + 1) % n] - poly[i]).Cross(poly[(i + 2) % n] - poly[(i + 1) % n]) * orient;
                if (cr < -1e-9) convex = false;
            }
            if (convex) { for (int i = 1; i < n - 1; i++) { outIdx.Add(0); outIdx.Add(i); outIdx.Add(i + 1); } return; }

            var idx = new List<int>(n); for (int i = 0; i < n; i++) idx.Add(i);
            int guard = n * n + 16;
            while (idx.Count > 3 && guard-- > 0)
            {
                bool clipped = false;
                for (int k = 0; k < idx.Count && !clipped; k++)
                {
                    int i0 = idx[(k + idx.Count - 1) % idx.Count], i1 = idx[k], i2 = idx[(k + 1) % idx.Count];
                    V2 a = poly[i0], b = poly[i1], c = poly[i2];
                    double cr = (b - a).Cross(c - b) * orient;
                    if (Math.Abs(cr) <= 1e-12) { idx.RemoveAt(k); clipped = true; break; }       // a collinear vertex adds nothing
                    if (cr < 0) continue;                                                           // a reflex corner is never an ear
                    bool inside = false;
                    foreach (int j in idx)
                    {
                        if (j == i0 || j == i1 || j == i2) continue;
                        if (InTriangle(poly[j], a, b, c, orient)) { inside = true; break; }
                    }
                    if (inside) continue;
                    outIdx.Add(i0); outIdx.Add(i1); outIdx.Add(i2); idx.RemoveAt(k); clipped = true;
                }
                if (!clipped) break;
            }
            if (idx.Count >= 3) for (int i = 1; i < idx.Count - 1; i++) { outIdx.Add(idx[0]); outIdx.Add(idx[i]); outIdx.Add(idx[i + 1]); }
        }

        static bool InTriangle(V2 p, V2 a, V2 b, V2 c, double orient)
        {
            double d1 = (b - a).Cross(p - a) * orient, d2 = (c - b).Cross(p - b) * orient, d3 = (a - c).Cross(p - c) * orient;
            if ((p - a).Length < 1e-9 || (p - b).Length < 1e-9 || (p - c).Length < 1e-9) return false;     // a repeated corner is not inside
            return d1 >= -1e-9 && d2 >= -1e-9 && d3 >= -1e-9;                                             // on an edge counts: it would make a sliver over the shape
        }

        // Sutherland-Hodgman: a polygon cut to a convex clip polygon
        public static List<V2> ClipConvex(List<V2> subject, List<V2> clip)
        {
            double area = 0; for (int i = 0; i < clip.Count; i++) area += clip[i].Cross(clip[(i + 1) % clip.Count]);
            double o = area >= 0 ? 1 : -1;
            var output = new List<V2>(subject);
            for (int e = 0; e < clip.Count && output.Count > 0; e++)
            {
                V2 a = clip[e], b = clip[(e + 1) % clip.Count];
                var input = output; output = new List<V2>();
                for (int i = 0; i < input.Count; i++)
                {
                    V2 cur = input[i], prev = input[(i + input.Count - 1) % input.Count];
                    double dc = (b - a).Cross(cur - a) * o, dp = (b - a).Cross(prev - a) * o;
                    if (dc >= 0)
                    {
                        if (dp < 0) output.Add(Cut(prev, cur, dp, dc));
                        output.Add(cur);
                    }
                    else if (dp >= 0) output.Add(Cut(prev, cur, dp, dc));
                }
            }
            return output;
        }

        static V2 Cut(V2 p, V2 q, double dp, double dq) => p + (q - p) * (dp / (dp - dq));
    }
}
