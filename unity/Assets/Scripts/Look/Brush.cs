using System;
using Kaname.Ink;
using Kaname.Sim;

namespace Kaname.Look
{
    // The small drawing helpers of js/art_hero.js and js/striker.js, over the ink canvas.
    public static class Brush
    {
        public readonly struct Pt { public readonly double X, Y; public Pt(double x, double y) { X = x; Y = y; } }

        // a dark filled shape with a thin pale rim
        public static void InkShape(InkCanvas ctx, Rgba fill, Action path, Rgba? rim = null, double lw = 0.32, bool noRim = false)
        {
            ctx.BeginPath(); path(); ctx.FillStyle(fill); ctx.Fill();
            if (!noRim) { ctx.LineWidth = lw; ctx.StrokeStyle(rim ?? Palette.Rim); ctx.Stroke(); }
        }

        // a round-capped stroke along a few points (a limb, the sheath, the hilt)
        public static void Limb(InkCanvas ctx, Pt[] pts, Rgba color, double w)
        {
            ctx.BeginPath(); ctx.MoveTo(pts[0].X, pts[0].Y); for (int i = 1; i < pts.Length; i++) ctx.LineTo(pts[i].X, pts[i].Y);
            ctx.LineCap = Cap.Round; ctx.LineWidth = w; ctx.StrokeStyle(color); ctx.Stroke();
        }

        // a soft round glow: a colour fading to nothing at radius r
        public static void GlowDot(InkCanvas ctx, double x, double y, double r, string rgb, double a)
        {
            var g = Gradient.Radial(x, y, r);
            g.AddColorStop(0, Rgba.Of(rgb, a)); g.AddColorStop(1, Rgba.Of(rgb, 0));
            ctx.FillStyle(g); ctx.BeginPath(); ctx.Arc(x, y, r, 0, 7); ctx.Fill();
        }

        // a thin pale-blue outline of a hit box, for the developer's debug view and the strike sheet's box option (rule 5: never in play; never red or yellow)
        public static void BoxOutline(InkCanvas ctx, Box b, double alpha)
        {
            ctx.Save(); ctx.LineWidth = 0.6; ctx.StrokeStyle(Palette.Rim.WithAlpha((float)alpha)); ctx.StrokeRect(b.X, b.Y, b.W, b.H); ctx.Restore();
        }

        // a steady pseudo-random 0..1 (the blade's flicker)
        public static double Hash2(double a, double b) { double s = Math.Sin(a * 127.1 + b * 311.7) * 43758.5453; return s - Math.Floor(s); }

        // A four-point sparkle: two thin crossed diamonds with a white heart and no ring or circle (js/striker.js drawGlint).
        public static void Glint(InkCanvas ctx, double x, double y, double size, string rgb, double a)
        {
            if (a <= 0.01 || size <= 0.5) return;
            ctx.Save(); ctx.Translate(x, y); ctx.GlobalAlpha = Math.Min(1, a);
            var col = Rgba.Of(rgb, 1);
            foreach (var (len, wid, rot) in new[] { (size, size * 0.1, 0.0), (size * 0.72, size * 0.09, Math.PI / 2), (size * 0.5, size * 0.06, Math.PI / 4), (size * 0.5, size * 0.06, -Math.PI / 4) })
            {
                ctx.Save(); ctx.Rotate(rot); ctx.BeginPath(); ctx.MoveTo(-len, 0); ctx.LineTo(0, -wid); ctx.LineTo(len, 0); ctx.LineTo(0, wid); ctx.ClosePath(); ctx.FillStyle(col); ctx.Fill(); ctx.Restore();
            }
            ctx.FillStyle(new Rgba(1, 1, 1, 1)); ctx.BeginPath(); ctx.MoveTo(-size * 0.22, 0); ctx.LineTo(0, -size * 0.07); ctx.LineTo(size * 0.22, 0); ctx.LineTo(0, size * 0.07); ctx.ClosePath(); ctx.Fill();
            ctx.Restore();
        }
    }
}
