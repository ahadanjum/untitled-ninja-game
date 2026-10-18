using Kaname.Ink;
using Kaname.Sim;
using static Kaname.Look.Brush;

namespace Kaname.Look
{
    // The test dummy, in the hero's ink style (js/dummy.js drawDummy): a dark post and crossbar, a round straw head, rope bands; white-blue for an instant
    // when struck (never red or yellow). A hanging one dangles from a rope that runs off the top of the screen and swings from it.
    public static class DummyArt
    {
        public static void Draw(InkCanvas ctx, Dummy d)
        {
            bool hit = d.Flash > 0;
            var wood = hit ? Rgba.Hex("#e9efff") : Palette.Ink; var head = hit ? Rgba.Hex("#f4f7ff") : Palette.InkHead;
            ctx.Save(); ctx.Translate(d.X + d.Ox, d.Y + d.Oy);
            if (d.Kind == DummyKind.Post)                                                         // a soft shadow on the ground
            {
                ctx.FillStyle(Rgba.Of("7,9,13", 0.2)); ctx.BeginPath(); ctx.Ellipse(-d.Ox, -d.Oy + 0.4, 15, 1.8, 0, 0, 7); ctx.Fill();
            }
            else                                                                                  // the rope it hangs from, running off the top of the screen
            {
                ctx.BeginPath(); ctx.MoveTo(0, -d.H); ctx.LineTo(0, -d.H - 600); ctx.LineWidth = 0.9; ctx.StrokeStyle(Palette.Rope); ctx.Stroke();
                ctx.Translate(0, -d.H);                                                           // it swings from its rope
            }
            ctx.Rotate(d.Lean);
            if (d.Kind == DummyKind.Hanging) ctx.Translate(0, d.H);
            InkShape(ctx, wood, () => ctx.RoundRect(-3.6, -41, 7.2, 41, 1.6));                    // the post
            InkShape(ctx, wood, () => ctx.RoundRect(-14, -33, 28, 5, 2));                         // the crossbar (the arms)
            InkShape(ctx, head, () => ctx.Arc(0, -43, 9.4, 0, 7));                                // the straw head
            foreach (var (x, y, w) in new[] { (-14.0, -33.0, 3.0), (11.0, -33.0, 3.0), (-3.6, -22.0, 7.2) })   // rope bands
            {
                double hh = 5 * (y == -22 ? 0.7 : 1);
                ctx.FillStyle(Palette.Rope); ctx.FillRect(x, y, w, hh); ctx.LineWidth = 0.2; ctx.StrokeStyle(Palette.RopeDk); ctx.StrokeRect(x, y, w, hh);
            }
            ctx.BeginPath(); ctx.MoveTo(-9, -36); ctx.LineTo(9, -36); ctx.LineWidth = 0.8; ctx.StrokeStyle(Palette.Rope); ctx.Stroke();   // the band at its neck
            ctx.Restore();
        }
    }
}
