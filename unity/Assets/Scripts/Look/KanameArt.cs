using System;
using Kaname.Ink;
using Kaname.Sim;
using static Kaname.Look.Brush;

namespace Kaname.Look
{
    // Kaname, drawn in Little Shinobi's ink-silhouette style (js/art_hero.js, line for line): dark round shapes (a head, a body, two feet), a thin pale rim,
    // glowing eyes, a purple headband whose tail is the scarf, a purple sash, and no arms at all (D103): the sword simply appears in his hand.
    // Units are world units (head radius 12.4). It reads the look state and changes none. No red or yellow anywhere on him (those mean danger and
    // parry); the blade's glow is violet-black. The look is the designer's pick "A" (D091, D095).
    public static class KanameArt
    {
        static Rgba C(string rgb, double a) => Rgba.Of(rgb, a);

        // the scarf points as drawn: the chain, blended between the last step and this one, with its first point pinned to the head exactly
        static (double x, double y)[] ScarfPoints(HeroLook P, double rx, double ry, double alpha)
        {
            var pts = new (double x, double y)[P.Scarf.Length];
            for (int i = 0; i < pts.Length; i++) pts[i] = (Mathx.Lerp(P.Scarf[i].Ox, P.Scarf[i].X, alpha), Mathx.Lerp(P.Scarf[i].Oy, P.Scarf[i].Y, alpha));
            var r = P.Head(rx, ry).Root; pts[0] = (r.x, r.y);
            return pts;
        }

        // the headband's tail: tapered strokes along the chain (a dark outline pass, then a lighter one), the first game's scarf
        static void DrawScarf(InkCanvas ctx, HeroLook P, double rx, double ry, double alpha)
        {
            var pts = ScarfPoints(P, rx, ry, alpha);
            ctx.Save(); ctx.LineCap = Cap.Round;
            for (int pass = 0; pass < 2; pass++)
            {
                ctx.StrokeStyle(pass == 1 ? Palette.Band : Palette.BandEdge);
                for (int i = 1; i < pts.Length; i++)
                {
                    ctx.LineWidth = (5 - i * 0.52) * (pass == 1 ? 0.72 : 1);
                    ctx.BeginPath(); ctx.MoveTo(pts[i - 1].x, pts[i - 1].y); ctx.LineTo(pts[i].x, pts[i].y); ctx.Stroke();
                }
            }
            ctx.Restore();
        }

        // ---- the speed lines of a dash and a sprint (D098): four tapered violet wedges from where the trail began to him, each fading to nothing at its far end ----
        static void DrawTrail(InkCanvas ctx, HeroLook P, double rx, double ry)
        {
            var tr = P.Trail; if (tr.Count == 0) return;
            var last = tr[tr.Count - 1]; double a = Math.Min(1, last.K * 1.6) * (last.Life / last.Max);
            double dx = rx - tr[0].X, len = Math.Min(Math.Abs(dx), 90); int dir = Mathx.Sign(dx);
            if (len < 3 || a <= 0.01) return;
            var Q = P.Place(); double headY = P.Head(rx, ry).Head.y;
            var lines = new[] { (y: headY - 4, w: 2.6, f: 1.0), (y: headY + 7, w: 1.5, f: 0.6), (y: ry + Q.Bob - 12, w: 3.2, f: 0.85), (y: ry - 4, w: 2.0, f: 0.5) };
            ctx.Save();
            foreach (var l in lines)
            {
                double x1 = rx, x0 = rx - dir * len * l.f;
                var gr = Gradient.Linear(x0, 0, x1, 0);
                gr.AddColorStop(0, C(Palette.BandRgb, 0)); gr.AddColorStop(1, C(Palette.BandRgb, a));
                ctx.FillStyle(gr); ctx.BeginPath(); ctx.MoveTo(x0, l.y); ctx.LineTo(x1, l.y - l.w / 2); ctx.LineTo(x1, l.y + l.w / 2); ctx.ClosePath(); ctx.Fill();
            }
            ctx.Restore();
        }

        // `t` is the world's clock (the blinking and the pulsing), (rx, ry) is where his feet are, `alpha` how far between the last two steps the picture is
        public static void DrawKaname(InkCanvas ctx, HeroLook P, double t, double rx, double ry, double alpha)
        {
            var inp = P.In; var v = P.Vis; double run = v.Run, air = v.Air, rise = v.Rise, d = v.Dash;
            bool blink = inp.Invuln > 0 && inp.DashT <= 0 && inp.DeadT <= 0 && Math.Floor(t * 22) % 2 == 0;       // untouchable after a hit: he blinks
            if (blink) { ctx.Save(); ctx.GlobalAlpha = 0.45; }

            // the shadow on whatever is below him, fainter the higher he is
            double sy = P.ShadowY, hgt = Math.Max(0, sy - ry), sh = Mathx.Clamp(1 - hgt / 150, 0.2, 1);
            ctx.Save(); ctx.FillStyle(C("7,9,13", 0.2 * sh));
            ctx.BeginPath(); ctx.Ellipse(rx, sy + 0.4, 13 * (0.62 + 0.38 * sh), 1.7, 0, 0, 7); ctx.Fill(); ctx.Restore();

            DrawTrail(ctx, P, rx, ry);
            DrawScarf(ctx, P, rx, ry, alpha);

            ctx.Save();
            ctx.Translate(rx, ry);
            ctx.Scale(inp.Face * (1 + v.Sq * 0.4 + d * HeroLook.DashStretch), 1 - v.Sq * 0.4 - d * HeroLook.DashSquash);   // squash and stretch (a dash is long and low)
            ctx.Rotate(v.Lean);                                                                                          // leaning into the run, further into a sprint, furthest in a dash
            var Q = P.Place();
            double l1 = Q.L1, l2 = Q.L2, lift1 = Q.Lift1, lift2 = Q.Lift2, bob = Q.Bob, step = Q.Step, hy = Q.Hy, hx = Q.Hx, hyy = Q.Hyy, tx = Q.Tx, ty = Q.Ty;
            double w1x = Q.W1x, w1y = Q.W1y, kx = Q.Kx, ky = Q.Ky, wA = Q.WA;
            double pulse = 0.5 + 0.5 * Math.Sin(t * 2.4);

            // the katana in its sheath at the hip: plain, steel fittings, nothing white
            double sa = Math.Atan2(hyy - ty, hx - tx);
            Limb(ctx, new[] { new Pt(hx, hyy), new Pt(tx, ty) }, Palette.Blk, 2.9);
            Limb(ctx, new[] { new Pt(hx, hyy), new Pt(tx, ty) }, Palette.Saya, 2.3);
            Limb(ctx, new[] { new Pt(hx - 1, hyy + 0.2), new Pt(tx + 1, ty + 0.3) }, Palette.SayaLt, 0.35);
            foreach (var (fx, fy) in new[] { (Mathx.Lerp(hx, tx, 0.1), Mathx.Lerp(hyy, ty, 0.1)), (tx, ty) })
            {
                ctx.Save(); ctx.Translate(fx, fy); ctx.Rotate(sa); ctx.FillStyle(Palette.Steel); ctx.FillRect(-0.75, -1.15, 1.5, 2.3);
                ctx.LineWidth = 0.18; ctx.StrokeStyle(Palette.Blk); ctx.StrokeRect(-0.75, -1.15, 1.5, 2.3); ctx.Restore();
            }

            // feet and body
            InkShape(ctx, Palette.Blk, () => ctx.Ellipse(-4 + l1, -3 - air * 2 - lift1, 4.6, 3.7, 0, 0, 7));
            InkShape(ctx, Palette.Blk, () => ctx.Ellipse(5.5 + l2, -3.4 - air * Mathx.Lerp(1, 4, rise) - lift2, 4.6, 3.7, 0, 0, 7));
            InkShape(ctx, Palette.Inkc, () => ctx.Ellipse(0, -11 + bob - step * 1.1, 7.8, 9, 0, 0, 7));
            // the sash across the body, its tail flicking behind
            double sb = bob - step * 1.1;
            InkShape(ctx, Palette.Band, () =>
            {
                ctx.MoveTo(-7.5, -8.8 + sb); ctx.BezierCurveTo(-3, -6.6 + sb, 3, -6.6 + sb, 7.5, -8.8 + sb); ctx.LineTo(7.2, -6.4 + sb);
                ctx.BezierCurveTo(3, -4.2 + sb, -3, -4.2 + sb, -7.2, -6.4 + sb); ctx.ClosePath();
            }, Palette.Blk, 0.2);
            InkShape(ctx, Palette.Band, () =>
            {
                ctx.MoveTo(-7, -7.6 + sb); ctx.QuadraticCurveTo(-9.4 - run * 2 - d * 2, -6.4 + sb, -10.4 - run * 3 - d * 4, -3 + sb + air * -2);
                ctx.QuadraticCurveTo(-8.4 - run, -4.4 + sb, -6.2, -4.8 + sb); ctx.ClosePath();
            }, Palette.Blk, 0.2);

            // the head: a round dark hood with a headband and glowing eyes (no hood tail)
            ctx.Save(); ctx.Translate(0, hy); ctx.Rotate(0.08 * run); ctx.Translate(0, -hy);
            InkShape(ctx, Palette.Inkc, () => ctx.Arc(0, hy, 12.4, 0, 7));
            // the headband: a cloth band wrapped round the head just above the eyes; its tail is the scarf (the first game's look)
            ctx.Save(); ctx.BeginPath(); ctx.Arc(0, hy, 12.3, 0, 7); ctx.Clip();
            ctx.FillStyle(Palette.Band); ctx.FillRect(-14, hy - 3.6, 28, 5.2);
            ctx.FillStyle(Palette.BandEdge); ctx.FillRect(-14, hy - 3.6, 28, 0.32); ctx.FillRect(-14, hy + 1.28, 28, 0.32);
            ctx.Restore();
            ctx.BeginPath(); ctx.Arc(0, hy, 12.4, 0, 7); ctx.LineWidth = 0.32; ctx.StrokeStyle(Palette.Rim); ctx.Stroke();      // the pale rim, over the band's ends
            double eyeS = 1 - Mathx.Clamp(v.Run, 0, 1) * 0.12 - d * 0.2, blinkK = v.Air > 0.5 && rise > 0.5 ? 0.85 : 1, bl = P.Blink > 0 ? 0.1 : 1;
            GlowDot(ctx, 5.4, hy + 3.4, 6.6, "207,230,255", 0.62);
            ctx.FillStyle(new Rgba(1, 1, 1, 1));
            ctx.BeginPath(); ctx.Ellipse(2.6, hy + 3.5, 2.8, 1.2 * eyeS * blinkK * bl, -0.31, 0, 7); ctx.Fill();
            ctx.BeginPath(); ctx.Ellipse(8.6, hy + 2.8, 2.6, 1.15 * eyeS * blinkK * bl, -0.31, 0, 7); ctx.Fill();
            ctx.Restore();

            // the hilt at his side, out of the sheath's mouth (the unseen hand is on it): the cursed blade's glow shows here while it is sheathed
            if (wA < 0.4)
            {
                Limb(ctx, new[] { new Pt(w1x, w1y - 0.1), new Pt(kx, ky) }, Palette.Blk, 2.5); Limb(ctx, new[] { new Pt(w1x, w1y - 0.1), new Pt(kx, ky) }, Palette.Hilt, 1.9);
                ctx.LineWidth = 0.27; ctx.StrokeStyle(Palette.HiltLt);
                for (int i = 0; i < 3; i++)
                {
                    double f = 0.28 + i * 0.24, px = Mathx.Lerp(w1x, kx, f), py = Mathx.Lerp(w1y, ky, f);
                    ctx.BeginPath(); ctx.MoveTo(px, py + 0.9); ctx.LineTo(px + 0.4, py - 0.9); ctx.Stroke();
                }
                InkShape(ctx, Palette.Steel, () => ctx.Ellipse(w1x - 0.5, w1y - 0.4, 0.9, 2.1, -0.3, 0, 7), Palette.Blk, 0.2);
                double ch = v.Charge;                                                    // the charge: the glow swells, motes gather into the hilt, and when it is ready a ring pulses out
                GlowDot(ctx, (w1x + kx) / 2, (w1y + ky) / 2, 7 + ch * 9, Palette.CurseRgb, 0.22 + 0.2 * pulse + ch * 0.45);
                GlowDot(ctx, kx + 0.3, ky - 0.1, 5.4 + ch * 7, Palette.CurseRgb, 0.2 + 0.24 * pulse + ch * 0.4);          // the glow on the sword (no smoke)
                InkShape(ctx, Palette.Steel, () => ctx.Arc(kx + 0.3, ky - 0.1, 1, 0, 7), Palette.Blk, 0.2);
                if (ch > 0.03)
                    for (int i = 0; i < 6; i++)
                    {
                        double ph = (t * 1.6 + i / 6.0) % 1, r = (1 - ph) * (10 + 16 * ch), th = i * 1.047 + t * 2;
                        GlowDot(ctx, w1x + Math.Cos(th) * r, w1y + Math.Sin(th) * r * 0.8, 2.4, "205,175,255", 0.85 * ch * ph);
                    }
                if (inp.Charge >= 1)
                {
                    double ph = (t * 2.6) % 1;
                    ctx.BeginPath(); ctx.Arc(w1x, w1y, 6 + ph * 16, 0, 7); ctx.LineWidth = 1.2; ctx.StrokeStyle(C("217,200,255", 0.9 * (1 - ph))); ctx.Stroke();
                }
            }
            DrawCrescent(ctx, P, Q);
            DrawBlade(ctx, P, Q);
            if (inp.ParryT > 0) Glint(ctx, Q.W1x + Math.Cos(Q.Ang) * 40, Q.W1y + Math.Sin(Q.Ang) * 40, 7 + 2 * Math.Sin(t * 40), "225,215,255", 0.9);   // the open parry: a small pale glint at the blade's tip
            ctx.Restore();
            if (blink) ctx.Restore();
        }

        // The trail of a cut: the sweep of the blade's outer part, a crescent hair-thin where the blade began and widest at its tip, whose outer edge
        // is the path of the blade's tip itself (so the trail hangs off the blade, never ahead of it). Violet-black with a pale violet edge, thinning
        // and fading as the cut ends (D087: the essence shows in the swing). Drawn in his own frame about his hand, so it moves with him exactly as
        // the blade does.
        static void DrawCrescent(InkCanvas ctx, HeroLook P, Pose Q)
        {
            var sw = P.Swing; double al = 1 - sw.Fade; if (Q.WA < 0.02 || al < 0.02) return;
            var g = HeroLook.Swings(sw.Dir); var (b0, b1) = HeroLook.SwingAngles(sw); double e0 = Mathx.Lerp(b0, b1, HeroLook.SwingEase(sw.P));
            if (Math.Abs(e0 - b0) < 0.03) return;
            double lag = (b1 > b0 ? -1 : 1) * Math.Atan2(HeroLook.BladeLen * 0.1, 5 + HeroLook.BladeLen);          // the curved blade's tip trails its axis a little: so does the trail's tip
            double a0 = b0 + lag, span = e0 - b0, W = (sw.Dir == SwingDir.Draw ? 22 : 14) * (1 - 0.4 * sw.Fade), px = Q.W1x, py = Q.W1y; const int M = 40;
            (double x, double y) Pt(double t, double r) => (px + Math.Cos(a0 + span * t) * r, py + Math.Sin(a0 + span * t) * r);
            // a crescent from t0 to the tip: its outer edge is the tip's path (radius rOut), its inner edge comes in by a width that grows from nothing at t0
            void Band(double t0, double wMul, double rOut, Rgba fill)
            {
                ctx.BeginPath();
                for (int i = 0; i <= M; i++) { var (x, y) = Pt(t0 + (1 - t0) * i / M, rOut); if (i > 0) ctx.LineTo(x, y); else ctx.MoveTo(x, y); }
                for (int i = M; i >= 0; i--)
                {
                    double t = t0 + (1 - t0) * i / M, ramp = (t - t0) / (1 - t0); var (x, y) = Pt(t, rOut - (W * Math.Pow(t, 1.2) * ramp + 0.3) * wMul);
                    ctx.LineTo(x, y);
                }
                ctx.ClosePath(); ctx.FillStyle(fill); ctx.Fill();
            }
            ctx.Save();
            Band(0, 1.5, g.R + 1.4, C(Palette.CurseRgb, 0.2 * al));                  // a soft violet glow round a violet-black body that darkens toward the tip
            Band(0, 1, g.R, C("28,12,64", 0.5 * al)); Band(0.45, 1, g.R, C("28,12,64", 0.45 * al)); Band(0.75, 1, g.R, C("18,6,44", 0.4 * al));
            ctx.LineCap = Cap.Round; ctx.LineWidth = 0.9;                                                                // the pale edge along the tip's path, fading toward where it began
            for (int i = 0; i < M; i++)
            {
                double t = (i + 0.5) / M; var (x0, y0) = Pt((double)i / M, g.R); var (x1, y1) = Pt((i + 1.0) / M, g.R);
                ctx.BeginPath(); ctx.MoveTo(x0, y0); ctx.LineTo(x1, y1); ctx.StrokeStyle(C("217,200,255", t * t * 0.95 * al)); ctx.Stroke();
            }
            ctx.Restore();
        }

        // The blade in his hand during a cut, drawn as Little Shinobi draws its cutlass but cursed: a wrapped grip, a guard and a curved blade of
        // violet-black with a spine and a band of pale violet fire along the cutting edge, wisps of violet fire streaming back off it, and a soft
        // aura hugging the whole blade, strongest toward the tip. The aura is the blade's own: it moves with it and ends at its tip.
        static void DrawBlade(InkCanvas ctx, HeroLook P, Pose Q)
        {
            double al = Q.WA; if (al < 0.02) return;
            var (a0, a1) = HeroLook.SwingAngles(P.Swing); double len = HeroLook.BladeLen, tipX = 5 + len, tipY = -len * 0.1, mid = 5 + len * 0.6, ecx = 5 + len * 0.66;
            (double x, double y) Edge(double u) { double w = 1 - u; return (w * w * tipX + 2 * w * u * ecx + u * u * 5.5, w * w * tipY + 2 * w * u * 4.2 + u * u * 2.1); }   // u: 0 at the tip, 1 at the guard
            double seed = Math.Floor(P.T * 24), flick = 0.88 + 0.12 * Hash2(seed, 99);
            ctx.Save(); ctx.Translate(Q.W1x, Q.W1y); ctx.Rotate(Q.Ang); if (a1 < a0) ctx.Scale(1, -1);                      // the cutting edge always leads
            ctx.GlobalAlpha = al;
            // the aura: a soft halo along the blade and glow where the edge burns
            ctx.LineCap = Cap.Round; ctx.BeginPath(); ctx.MoveTo(5.5, 0); ctx.QuadraticCurveTo(mid, -1.2, tipX, tipY);
            ctx.LineWidth = 11; ctx.StrokeStyle(C(Palette.CurseRgb, 0.16 * flick)); ctx.Stroke(); ctx.LineWidth = 6; ctx.StrokeStyle(C(Palette.CurseRgb, 0.3 * flick)); ctx.Stroke();
            for (int i = 1; i <= 6; i++) { double u = (i - 0.5) / 6.5; var e = Edge(u); GlowDot(ctx, e.x, e.y + 1, 5 + Hash2(seed, i) * 3, Palette.CurseRgb, (0.16 + (1 - u) * 0.3) * flick); }
            // the grip, the pommel and the guard
            ctx.FillStyle(Rgba.Hex("#24143e")); ctx.FillRect(-6, -1.7, 10, 3.4);
            ctx.StrokeStyle(Palette.HiltLt); ctx.LineWidth = 0.7; ctx.BeginPath(); for (int i = 0; i < 4; i++) { ctx.MoveTo(-5 + i * 2.4, -1.7); ctx.LineTo(-3.6 + i * 2.4, 1.7); } ctx.Stroke();
            InkShape(ctx, Palette.Steel, () => ctx.Arc(-7, 0, 2, 0, 7), Palette.Blk, 0.2);
            InkShape(ctx, Palette.Steel, () => ctx.Ellipse(4.6, 0, 1.6, 4.2, 0, 0, 7), Palette.Blk, 0.2);
            // the curved blade, its spine, and the band of pale violet fire along the cutting edge
            ctx.BeginPath(); ctx.MoveTo(5.5, -1.7); ctx.QuadraticCurveTo(mid, -2.2, tipX, tipY); ctx.QuadraticCurveTo(5 + len * 0.66, 4.2, 5.5, 2.1); ctx.ClosePath();
            ctx.FillStyle(Rgba.Hex("#1b1034")); ctx.Fill(); ctx.LineWidth = 0.35; ctx.StrokeStyle(Palette.Blk); ctx.Stroke();
            ctx.StrokeStyle(Rgba.Hex("#5a4a8c")); ctx.LineWidth = 0.9; ctx.BeginPath(); ctx.MoveTo(5.5, -1.4); ctx.QuadraticCurveTo(mid, -1.9, tipX - 1, tipY + 0.3); ctx.Stroke();
            ctx.BeginPath(); ctx.MoveTo(tipX, tipY); ctx.QuadraticCurveTo(ecx, 4.2, 5.5, 2.1); ctx.LineTo(6, 0.4); ctx.QuadraticCurveTo(ecx, 2.2, tipX - 1.5, tipY + 0.2); ctx.ClosePath();
            ctx.FillStyle(Rgba.Hex("#b79cff")); ctx.Fill();
            ctx.BeginPath(); ctx.MoveTo(tipX, tipY); ctx.QuadraticCurveTo(ecx, 4.2, 5.5, 2.1); ctx.LineWidth = 0.8; ctx.StrokeStyle(Rgba.Hex("#efe6ff")); ctx.Stroke();
            // wisps of violet fire streaming back off the blade (away from the way it is cutting), each with a pale core: they fade into the trail
            for (int i = 1; i <= 5; i++)
            {
                double u = (i - 0.35) / 5.4; var e = Edge(u); double x = e.x, y = -1.2, fh = 3 + Hash2(seed, i + 30) * 5, sway = (Hash2(seed, i + 50) - 0.5) * 2.2, w = 1.5 + Hash2(seed, i + 70) * 0.8;
                ctx.FillStyle(C(Palette.CurseRgb, 0.42));
                ctx.BeginPath(); ctx.MoveTo(x - w, y); ctx.QuadraticCurveTo(x - w + sway * 0.4 - 0.8, y - fh * 0.55, x + sway - 1.2, y - fh); ctx.QuadraticCurveTo(x + w + sway * 0.4, y - fh * 0.5, x + w, y); ctx.ClosePath(); ctx.Fill();
                ctx.FillStyle(C("224,208,255", 0.5));
                ctx.BeginPath(); ctx.MoveTo(x - w * 0.4, y); ctx.QuadraticCurveTo(x + sway * 0.3 - 0.4, y - fh * 0.45, x + sway * 0.8 - 0.6, y - fh * 0.7); ctx.QuadraticCurveTo(x + w * 0.4 + sway * 0.2, y - fh * 0.4, x + w * 0.4, y); ctx.ClosePath(); ctx.Fill();
            }
            ctx.Restore();
        }
    }
}
