using Kaname.Ink;
using Kaname.Look;
using Kaname.Sim;
using UnityEngine;

namespace Kaname.Game
{
    // Kaname on the screen: the look state (the eased pose, the scarf, the trail) lives in HeroLook and is stepped with the simulation; this draws it
    // every frame, blended between the last two steps, with the ink canvas into one mesh (the ink silhouette of js/art_hero.js, ported line for line).
    public sealed class HeroView
    {
        readonly InkCanvas canvas = new InkCanvas();
        readonly InkRenderer ink;
        public readonly HeroLook Look;
        readonly LookInput input = new LookInput();

        public HeroView(Transform parent, Tuning tuning, Material material)
        {
            Look = new HeroLook(tuning);
            ink = new InkRenderer(parent, "Kaname", material, 10);
        }

        public LookInput Input => input;

        // after each simulation step: what the body did (the pose reel adds what the mechanics would say to `Input`), then the look takes its step
        public void Fill(Hero h) => Fill(input, h);

        // what the body did, for the look (shared with the pose sheet tool)
        public static void Fill(LookInput li, Hero h)
        {
            li.Cx = h.Cx; li.Y = h.Y; li.Vx = h.Vx; li.Vy = h.Vy; li.LandVy = h.LandVy;
            li.Face = h.Face; li.OnGround = h.OnGround;
            li.DashT = h.DashT; li.TrailActive = h.TrailActive;
            // Everything else is told afresh every step, so nothing can be left over from the step before (the pose reel once left "down" switched on
            // after it was turned off). The body has none of these yet: they come from it as the mechanics arrive (sword U4, charge U5, parry and health U6).
            li.Atk = null; li.Charge = 0; li.ParryT = 0; li.Invuln = 0; li.DeadT = 0;
        }

        public void Advance(double dt) => Look.Update(input, dt);

        // every frame: `a` is how far between the last two steps the picture is (1 on a 60 Hz display), `t` the world's clock
        public void Draw(Hero h, double a, double t)
        {
            double rx = Mathx.Lerp(h.Px, h.Cx, a), ry = Mathx.Lerp(h.Py, h.Y, a);
            canvas.Clear();
            KanameArt.DrawKaname(canvas, Look, t, rx, ry, a);
            ink.Show(canvas);
        }

        public void Destroy() => ink.Destroy();
    }
}
