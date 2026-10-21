using System;
using System.Collections.Generic;
using Kaname.Game;
using Kaname.Ink;
using Kaname.Look;
using Kaname.Sim;
using NUnit.Framework;

namespace Kaname.Tests
{
    // Task 3 of the polish milestone (D130, D132): the sword he swings is the designer's cursed katana without its effects. Measured on the rendered triangles of
    // KanameArt.DrawBlade (drawn alone on a fresh canvas, where the only transform is Translate(W1), Rotate(Ang) and the flip that makes the edge lead, which these tests
    // take back off) and on KatanaShape, the one outline that DrawBlade fills and that the surface rest checks against. Every step of a forward (both alternations), up
    // and down cut, standing, running and sprinting, facing both ways, plus the sword, up strike, down strike, unsheath (the draw) and guard poses of the pose reel.
    public class KatanaTests
    {
        // Saved from HEAD c4a3c237487acaf5dde7b554adaee5203bdfc62c (task 2b, before task 3): in every one of the 24 whiff cases below, the farthest vertex of blade.body from the
        // hand W1 was 45.577 u (the tip plus the old edge stroke's round cap). Its SlashTests CONTACT lines, 452 of them (372 B, 58 L, 22 N), are in unity/Logs/task3/contact_head.tsv.
        const double HeadReach = 45.577, ReachTolerance = 0.5;

        sealed class Shot
        {
            public string Name; public int Step; public InkCanvas Alone; public double W1x, W1y, Ang, WA, Reach = double.NaN; public bool Flip;
            public override string ToString() => $"{Name} step {Step}";
            // a canvas point of the blade drawn alone, back into the blade's own frame
            public V2 ToBlade(V2 p)
            {
                double dx = p.X - W1x, dy = p.Y - W1y, c = Math.Cos(Ang), s = Math.Sin(Ang), x = dx * c + dy * s, y = -dx * s + dy * c;
                return new V2(x, Flip ? -y : y);
            }
        }

        // ---- the frames: a cut's every step, and the pose reel's sword poses, each drawn once ----
        sealed class Rig
        {
            public readonly World W; public readonly HeroLook Look; public readonly LookInput Li = new LookInput(); public readonly ControlState C = new ControlState();
            public Rig() { var t = new Tuning(); W = new World(t); Look = new HeroLook(t); W.Dummies = new List<Dummy>(); }
            public Hero H => W.Hero;
            public void Step()
            {
                W.Step(Mathx.Step, C);
                if (!W.Frozen) { if (H.JustJumped) Look.Kick(-0.28); if (H.JustDashed) Look.Kick(-0.12); if (H.JustPogoed) Look.Kick(-0.25); HeroView.Fill(Li, H); Look.Update(Li, Mathx.Step); }
                C.EndStep();
            }
        }

        static Shot Shoot(string name, int step, HeroLook look, double rx, double ry, bool full)
        {
            var q = look.Place(rx, ry); if (q.WA < 0.02) return null;
            var (a0, a1) = HeroLook.SwingAngles(look.Swing); var alone = new InkCanvas(); KanameArt.DrawBlade(alone, look, q);
            var shot = new Shot { Name = name, Step = step, Alone = alone, W1x = q.W1x, W1y = q.W1y, Ang = q.Ang, WA = q.WA, Flip = a1 < a0 };
            if (full)                                                                                         // the farthest vertex of blade.body from the hand, as DrawKaname places it in the world
            {
                var cv = new InkCanvas(); KanameArt.DrawKaname(cv, look, 0, rx, ry, 1);
                var v = look.Vis; double fx = look.In.Face * (1 + v.Sq * 0.4 + v.Dash * HeroLook.DashStretch), fy = 1 - v.Sq * 0.4 - v.Dash * HeroLook.DashSquash, c = Math.Cos(v.Lean), s = Math.Sin(v.Lean);
                var w1 = new V2(rx + fx * (q.W1x * c - q.W1y * s), ry + fy * (q.W1x * s + q.W1y * c)); double best = 0;
                foreach (var (g, first, count) in cv.Groups) if (g == "blade.body") for (int i = first; i < first + count; i++) best = Math.Max(best, (cv.Verts[cv.Indices[i]] - w1).Length);
                shot.Reach = best;
            }
            return shot;
        }

        static List<Shot> shots;
        static List<Shot> Shots
        {
            get
            {
                if (shots != null) return shots;
                shots = new List<Shot>();
                foreach (int face in new[] { 1, -1 }) foreach (int mv in new[] { 0, 1, 2 }) foreach (int kind in new[] { 0, 1, 2, 3 })
                {
                    bool air = kind == 3; var r = new Rig(); var h = r.H; string name = $"{(kind == 0 ? "forward high-to-low" : kind == 1 ? "forward low-to-high" : kind == 2 ? "up" : "down")}, {(mv == 0 ? "stand" : mv == 1 ? "run" : "sprint")}, facing {(face > 0 ? "right" : "left")}";
                    if (air) h.Place(0, -400, 0, 0, face); else h.Place(0, 0, 0, 0, face);
                    r.W.Cam.X = r.W.CamPrev = h.Cx;
                    double speed = mv == 2 ? 336 : mv == 1 ? 240 : 0;
                    if (speed > 0) { r.C.Push = face; r.C.SetHeld(face > 0 ? Act.Right : Act.Left, true); }
                    if (mv == 2) r.C.SetHeld(Act.Dash, true);
                    for (int i = 0; i < (air ? 6 : 40); i++) r.Step();
                    if (kind == 2) r.C.SetHeld(Act.Up, true); if (kind == 3) r.C.SetHeld(Act.Down, true);
                    if (kind < 2) h.SlashAlt = kind == 0;
                    for (int k = 0; k <= 26; k++)
                    {
                        if (k == 0) { r.C.SetHeld(Act.Attack, true); r.C.Press(Act.Attack); } else if (k == 1) r.C.SetHeld(Act.Attack, false);
                        r.Step();
                        var s = Shoot(name, k, r.Look, h.Cx, h.Y, true); if (s != null) shots.Add(s);
                    }
                }
                foreach (var kind in new[] { PoseKind.Sword, PoseKind.SwordUp, PoseKind.SwordDown, PoseKind.Draw, PoseKind.Guard })               // the pose reel: the cuts again, and the unsheath and the guard
                {
                    var t = new Tuning(); var w = new World(t); var look = new HeroLook(t); var reel = new PoseReel(); var li = new LookInput();
                    int guard = 0; while (reel.Kind != kind && guard++ < 20) reel.Next(w, look);
                    look = new HeroLook(t);
                    for (int i = 0; i < 150; i++)
                    {
                        reel.BeforeStep(w, look, Mathx.Step); w.Step(Mathx.Step, NoControls.Instance); HeroView.Fill(li, w.Hero); reel.Tell(li); look.Update(li, Mathx.Step);
                        var s = Shoot("reel " + kind, i, look, w.Hero.Cx, w.Hero.Y, false); if (s != null) shots.Add(s);
                    }
                }
                return shots;
            }
        }
        static IEnumerable<Shot> Drawn() => Shots;

        // ---- geometry of the shared outline ----
        static bool InPolygon(V2 p, (double x, double y)[] poly)
        {
            bool inside = false;
            for (int i = 0, j = poly.Length - 1; i < poly.Length; j = i++)
                if ((poly[i].y > p.Y) != (poly[j].y > p.Y) && p.X < (poly[j].x - poly[i].x) * (p.Y - poly[i].y) / (poly[j].y - poly[i].y) + poly[i].x) inside = !inside;
            return inside;
        }
        static double ToSegment(V2 p, (double x, double y) a, (double x, double y) b)
        {
            double abx = b.x - a.x, aby = b.y - a.y, l2 = abx * abx + aby * aby, t = l2 < 1e-18 ? 0 : Mathx.Clamp(((p.X - a.x) * abx + (p.Y - a.y) * aby) / l2, 0, 1);
            double dx = p.X - (a.x + abx * t), dy = p.Y - (a.y + aby * t); return Math.Sqrt(dx * dx + dy * dy);
        }
        static double ToRing(V2 p, (double x, double y)[] ring) { double best = double.MaxValue; for (int i = 0; i < ring.Length; i++) best = Math.Min(best, ToSegment(p, ring[i], ring[(i + 1) % ring.Length])); return best; }
        static readonly (double x, double y)[] GuardRing = BuildGuardRing();
        static (double x, double y)[] BuildGuardRing()
        {
            var r = new (double, double)[96]; for (int i = 0; i < r.Length; i++) { double a = Math.PI * 2 * i / r.Length; r[i] = (KatanaShape.GuardX + KatanaShape.GuardRx * Math.Cos(a), KatanaShape.GuardRy * Math.Sin(a)); }
            return r;
        }
        // how far a point (in the blade's frame) is from the union of the outline's shapes (the blade polygon, the guard oval, the grip rectangle, the pommel circle): 0 inside
        static double ToOutline(V2 p)
        {
            double best = InPolygon(p, KatanaShape.Polygon) ? 0 : ToRing(p, KatanaShape.Polygon);
            double gx = (p.X - KatanaShape.GuardX) / KatanaShape.GuardRx, gy = p.Y / KatanaShape.GuardRy;
            best = Math.Min(best, gx * gx + gy * gy <= 1 ? 0 : ToRing(p, GuardRing));
            double dx = Math.Max(0, Math.Max(KatanaShape.GripX0 - p.X, p.X - KatanaShape.GripX1)), dy = Math.Max(0, Math.Abs(p.Y) - KatanaShape.GripHalf); best = Math.Min(best, Math.Sqrt(dx * dx + dy * dy));
            best = Math.Min(best, Math.Max(0, Math.Sqrt((p.X - KatanaShape.PommelX) * (p.X - KatanaShape.PommelX) + p.Y * p.Y) - KatanaShape.PommelR));
            return best;
        }
        static double Luma(Rgba c) => 0.2126 * c.R + 0.7152 * c.G + 0.0722 * c.B;                                  // Rec. 709, on the sRGB values

        // every vertex of every triangle a group (or, with null, the whole canvas) drew with the given minimum alpha
        static IEnumerable<(V2 p, Rgba c)> Vertices(InkCanvas cv, string group, float minAlpha)
        {
            IEnumerable<(int first, int count)> runs()
            {
                if (group == null) yield return (0, cv.Indices.Count);
                else foreach (var (n, f, c) in cv.Groups) if (n == group) yield return (f, c);
            }
            foreach (var (first, count) in runs())
                for (int i = first; i + 2 < first + count; i += 3)
                {
                    int a = cv.Indices[i], b = cv.Indices[i + 1], e = cv.Indices[i + 2];
                    if (Math.Max(cv.Colors[a].A, Math.Max(cv.Colors[b].A, cv.Colors[e].A)) < minAlpha) continue;
                    yield return (cv.Verts[a], cv.Colors[a]); yield return (cv.Verts[b], cv.Colors[b]); yield return (cv.Verts[e], cv.Colors[e]);
                }
        }

        [Test]
        public void TheFramesAreNotVacuous()
        {
            Assert.Greater(Shots.Count, 450, "the cuts and the reel's poses drew the blade on many steps");
            int withReach = 0; foreach (var s in Shots) if (!double.IsNaN(s.Reach)) withReach++;
            Assert.Greater(withReach, 24 * 7, "and the 24 cuts measured their reach on every live step");
        }

        [Test]
        public void NothingIsDrawnOutsideTheBladesOwnOutline()                                                 // (1) no halo, aura or wisp can return to read as reach (D123, D132)
        {
            foreach (var s in Drawn())
                foreach (var (p, _) in Vertices(s.Alone, null, 0.04f))
                {
                    double d = ToOutline(s.ToBlade(p));
                    if (d > 0.6) Assert.Fail($"{s}: a triangle with a corner {d:0.00} u outside the katana's outline (at {s.ToBlade(p).X:0.0}, {s.ToBlade(p).Y:0.0} in the blade's frame)");
                }
        }

        [Test]
        public void TheReachIsUnchangedFromTask2b()                                                            // (2) the farthest vertex of blade.body from the hand, against the saved HEAD value
        {
            int n = 0; double lo = double.MaxValue, hi = 0;
            foreach (var s in Drawn())
            {
                if (double.IsNaN(s.Reach)) continue; n++; lo = Math.Min(lo, s.Reach); hi = Math.Max(hi, s.Reach);
                Assert.AreEqual(HeadReach, s.Reach, ReachTolerance, $"{s}: the blade's body reaches {s.Reach:0.000} u from the hand; at HEAD it was {HeadReach} u");
            }
            Assert.Greater(n, 100); TestContext.WriteLine($"REACH\t{lo:0.000}\t{hi:0.000}\tover {n} steps (HEAD {HeadReach})");
        }

        [Test]
        public void TheBladeBodyIsTheSharedOutlineAndTheTipIsWhereItWas()                                      // (3) one outline, two users
        {
            Assert.AreEqual(45, KatanaShape.Polygon[KatanaShape.Samples].x, 0.05, "the tip's x"); Assert.AreEqual(-4, KatanaShape.Polygon[KatanaShape.Samples].y, 0.05, "the tip's y");
            double grow = Math.Max(KatanaShape.OutlineW, KatanaShape.EdgeLineW) / 2 * 1.16 + 0.05;               // the canvas's mitres reach 1.155 times the half width
            foreach (var s in Drawn())
                foreach (var (p, _) in Vertices(s.Alone, "blade.body", 0f))
                {
                    double d = ToOutline(s.ToBlade(p)); if (d > grow) Assert.Fail($"{s}: a vertex of blade.body {d:0.00} u outside the shared outline (more than {grow:0.00})");
                }
        }

        [Test]
        public void TheGlyphsAreFewFaintAndOnTheFlat()                                                         // (4)
        {
            int checkedShots = 0;
            foreach (var s in Drawn())
            {
                var marks = new List<(int k, V2 p)>(); foreach (var kv in s.Alone.Marks) if (kv.Key.StartsWith("glyph.")) marks.Add((int.Parse(kv.Key.Substring(6)), s.ToBlade(kv.Value)));
                marks.Sort((a, b) => a.k.CompareTo(b.k));
                Assert.That(marks.Count, Is.InRange(6, 8), $"{s}: the glyph marks");
                for (int i = 0; i < marks.Count; i++)
                {
                    Assert.AreEqual(i, marks[i].k, $"{s}: marks glyph.0 to glyph.N, without gaps"); Assert.IsTrue(InPolygon(marks[i].p, KatanaShape.Polygon), $"{s}: glyph.{i} lies inside the blade");
                    if (i > 0) Assert.Greater(marks[i].p.X, marks[i - 1].p.X, $"{s}: the glyphs run from the guard to the tip");
                }
                Assert.GreaterOrEqual(marks[0].p.X, 10.5, $"{s}: the first glyph starts past the guard"); Assert.LessOrEqual(marks[marks.Count - 1].p.X, 39, $"{s}: the last ends before the tip");
                float alpha = -1;
                foreach (var (_, c) in Vertices(s.Alone, "blade.glyphs", 0f))
                {
                    if (alpha < 0) alpha = c.A; Assert.AreEqual(alpha, c.A, 1e-6f, $"{s}: every glyph triangle has the same alpha (none is lit)");
                }
                Assert.Greater(alpha, 0, $"{s}: the glyphs are drawn"); if (s.WA > 0.999) { Assert.LessOrEqual(alpha, 0.75f, $"{s}: and faint"); checkedShots++; }
                foreach (var (p, _) in Vertices(s.Alone, "blade.glyphs", 0f)) Assert.LessOrEqual(ToOutline(s.ToBlade(p)), 0.0001, $"{s}: the glyph strokes lie inside the outline");
            }
            Assert.Greater(checkedShots, 50, "the faintness was measured at full strength on many steps");
        }

        [Test]
        public void TheSlashStaysTheBrightestThingInACut()                                                     // (5) D132
        {
            double limit = Luma(Palette.SlashBand) + 1e-6;
            foreach (var s in Drawn())
                foreach (var g in new[] { "blade.body", "blade.glyphs" })
                    foreach (var (_, c) in Vertices(s.Alone, g, 0f))
                        if (Luma(c) > limit) Assert.Fail($"{s}: a triangle of {g} is lighter than the slash's band (luma {Luma(c):0.000} against {limit:0.000}; rgb {c.R:0.00},{c.G:0.00},{c.B:0.00})");
            Assert.Less(Luma(Palette.Steel), limit, "the guard's steel is the closest colour and passes");
        }

        [Test]
        public void TheBladeIsOfAKatanasConstantWidthAndNarrowsOnlyAtThePoint()                                // (6) measured on the outline, perpendicular to the spine
        {
            const int M = 400; var arc = new double[M + 1]; var prev = KatanaShape.Spine(0);
            for (int i = 1; i <= M; i++) { var q = KatanaShape.Spine(i / (double)M); arc[i] = arc[i - 1] + Math.Sqrt((q.x - prev.x) * (q.x - prev.x) + (q.y - prev.y) * (q.y - prev.y)); prev = q; }
            double total = arc[M];
            double WidthAt(double length)                                                                       // the spine's arc length from the guard
            {
                int k = 0; while (k < M && arc[k + 1] < length) k++;
                double t = (k + (length - arc[k]) / (arc[k + 1] - arc[k])) / M; var p = KatanaShape.Spine(t); var tg = KatanaShape.Tangent(t); double nx = -tg.y, ny = tg.x, best = double.MaxValue;
                var poly = KatanaShape.Polygon;
                for (int i = 0; i < poly.Length; i++)                                                           // the nearest crossing of the ray (the spine point along the normal, toward the edge) with the outline
                {
                    var a = poly[i]; var b = poly[(i + 1) % poly.Length]; double ex = b.x - a.x, ey = b.y - a.y, den = nx * ey - ny * ex; if (Math.Abs(den) < 1e-12) continue;
                    double u = ((a.x - p.x) * ey - (a.y - p.y) * ex) / den, v = ((a.x - p.x) * ny - (a.y - p.y) * nx) / den;
                    if (u > 1e-6 && v >= -1e-9 && v <= 1 + 1e-9) best = Math.Min(best, u);
                }
                return best;
            }
            double w25 = WidthAt(total * 0.25), w50 = WidthAt(total * 0.5), w75 = WidthAt(total * 0.75), wide = Math.Max(w25, Math.Max(w50, w75)), thin = Math.Min(w25, Math.Min(w50, w75));
            Assert.LessOrEqual(wide / thin, 1.1, $"the widths at 25, 50 and 75 % ({w25:0.00}, {w50:0.00}, {w75:0.00} u) are within 10 % of each other");
            Assert.AreEqual(KatanaShape.Width, w50, 0.05, "and the width is the one the shape names");
            Assert.AreEqual(KatanaShape.Width, WidthAt(total - KatanaShape.PointLen - 0.3), 0.1 * KatanaShape.Width, "right up to the point");
            Assert.Less(WidthAt(total - 1), KatanaShape.Width * 0.75, "and it narrows within the last 4 u");
        }

        [Test]
        public void TheBladeLeavesTheGuardStraightAndCurvesLittleAndLate()                                     // (7) revision 3: no kink at the guard, a bow of about 1.5 u, curvature greater in the outer half
        {
            var t0 = KatanaShape.Tangent(0); Assert.AreEqual(0, t0.y, 1e-9, "the spine leaves the guard parallel to the grip's axis"); Assert.AreEqual(1, t0.x, 1e-9);
            var a = KatanaShape.Spine(0); var b = KatanaShape.Spine(1); double cx = b.x - a.x, cy = b.y - a.y, cl = Math.Sqrt(cx * cx + cy * cy), bow = 0, k1 = 0, k2 = 0; const int M = 200;
            for (int i = 0; i <= M; i++)
            {
                var p = KatanaShape.Spine(i / (double)M); bow = Math.Max(bow, ((p.x - a.x) * -cy + (p.y - a.y) * cx) / cl);
                if (i > 0 && i < M) { var u = KatanaShape.Tangent((i - 1) / (double)M); var v = KatanaShape.Tangent((i + 1) / (double)M); double k = Math.Abs(u.x * v.y - u.y * v.x); if (i < M / 2) k1 += k; else k2 += k; }
            }
            Assert.That(bow, Is.InRange(1.3, 1.7), "the bow of the spine from its chord (revision 2: 2.2 u)");
            Assert.Greater(k2, 2 * k1, "the curve builds toward the tip");
            for (int i = 0; i <= 40; i++) Assert.Greater(KatanaShape.Tangent(i / 40.0).x, 0.9, "the spine only runs forward, never hooks");
        }

        [Test]
        public void TheSheathedHiltKeepsItsGlowAndTheUnsheathsChargeStill()                                    // the sheathed hilt's wrap, guard and pommel changed; its glow, motes and ring did not (D087, D134)
        {
            var t = new Tuning(); var look = new HeroLook(t); var li = new LookInput(); var w = new World(t); var h = w.Hero;
            for (int i = 0; i < 30; i++) { w.Step(Mathx.Step, NoControls.Instance); HeroView.Fill(li, h); look.Update(li, Mathx.Step); }
            var rest = new InkCanvas(); KanameArt.DrawKaname(rest, look, 0.5, h.Cx, h.Y, 1);
            li.Charge = 1; for (int i = 0; i < 60; i++) { HeroView.Fill(li, h); li.Charge = 1; look.Update(li, Mathx.Step); }
            var charged = new InkCanvas(); KanameArt.DrawKaname(charged, look, 0.5, h.Cx, h.Y, 1);
            Assert.Greater(charged.Indices.Count, rest.Indices.Count + 100, "a full charge still adds its motes and its ring around the sheathed hilt");
            Assert.IsFalse(rest.Marks.ContainsKey("glyph.0"), "and no blade is drawn while it is sheathed");
        }
    }
}
