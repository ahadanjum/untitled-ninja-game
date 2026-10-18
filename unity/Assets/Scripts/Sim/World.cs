using System;

namespace Kaname.Sim
{
    // How the test environment is shown: switched by the dev keys (js/game.js G.grid, G.night, G.hints, G.debug, G.zoom).
    public sealed class ViewOptions
    {
        public bool Grid = true, Night, Hints = true, Debug;
        public double Zoom = 3;                       // screen pixels per world unit at the logical 1280 x 720 view: 3 shows 240 u of height
        public const double ZoomMin = 1.5, ZoomMax = 6;
    }

    // The whole simulated world of the test environment: Kaname, the camera and the clock (js/game.js step). It is stepped at a fixed
    // 60 Hz and knows nothing of Unity. Later rungs add the dummies, the striker, hit-stop and the zoom kick here.
    public sealed class World
    {
        public readonly Tuning K;
        public Hero Hero;
        public Spring Cam;                            // the camera's x and its speed
        public double CamPrev;                        // last step's camera x, for blending between steps
        public double T;                              // seconds simulated
        public readonly ViewOptions View = new ViewOptions();

        public World(Tuning tuning)
        {
            K = tuning;
            Reset();
        }

        // R: a clean start, him standing at the origin and the camera on him (js/game.js resetHero)
        public void Reset()
        {
            Hero = new Hero(K, 0, 0);
            Cam = default;
            CamPrev = 0;
        }

        // one fixed step
        public void Step(double dt, IControls controls)
        {
            DevKeys(controls);
            CamPrev = Cam.X;
            Hero.Update(dt, controls);
            Cam.SmoothDamp(Hero.Cx + Hero.Face * K.CameraLead, K.CameraSmooth, dt);
            T += dt;
        }

        void DevKeys(IControls c)
        {
            if (c.DevHit(Dev.Grid)) View.Grid = !View.Grid;
            if (c.DevHit(Dev.Night)) View.Night = !View.Night;
            if (c.DevHit(Dev.Hints)) View.Hints = !View.Hints;
            if (c.DevHit(Dev.Debug)) View.Debug = !View.Debug;
            if (c.DevHit(Dev.ZoomIn)) View.Zoom = Mathx.Clamp(View.Zoom * 1.15, ViewOptions.ZoomMin, ViewOptions.ZoomMax);
            if (c.DevHit(Dev.ZoomOut)) View.Zoom = Mathx.Clamp(View.Zoom / 1.15, ViewOptions.ZoomMin, ViewOptions.ZoomMax);
            if (c.DevHit(Dev.Reset)) Reset();
            if (c.DevHit(Dev.Toss)) Hero.Place(Hero.Cx, Hero.Y - 200, 0, 0, Hero.Face);
        }
    }
}
