using System;
using System.Collections.Generic;

namespace Kaname.Sim
{
    // The fixed-step loop of js/game.js: the simulation runs at exactly 60 Hz whatever the display does; only drawing varies.
    // On a fast display (a 120 Hz MacBook screen) the last two steps are blended so motion is smooth; on a 60 Hz screen the latest step
    // is drawn as it is (blending would only add a frame of lag). The display is measured over its first 40 frames: blend only if it is
    // faster than about 75 Hz.
    public sealed class FixedClock
    {
        public double Acc;                            // time not yet simulated, 0 <= Acc < Step
        public double Hz = 60;                        // the measured display rate
        public bool Interp;                           // whether to blend the last two steps
        public bool? ForceInterp;                     // override (the JavaScript game's ?interp=1|0)

        const int Samples = 40;
        readonly List<double> samples = new List<double>(Samples);
        bool started;

        // How far between the last two steps the picture is, 0..1 (use it only when Interp is true; otherwise draw alpha = 1).
        public double Alpha => Interp ? Acc / Mathx.Step : 1;

        // Feed it the real time since the last frame; it runs `step` as many fixed steps as that time holds, calling `afterStep` after each
        // (the controls drop their presses there). Returns how many steps ran.
        public int Advance(double realDt, Action<double> step, Action afterStep)
        {
            if (!started) { started = true; return 0; }              // the first frame only starts the clock
            double dt = Math.Min(0.1, realDt);                       // a stall (a breakpoint, a hidden window) must not run a burst of steps
            if (samples.Count < Samples)
            {
                samples.Add(dt);
                if (samples.Count == Samples)
                {
                    var s = new List<double>(samples); s.Sort();
                    Hz = 1 / s[20];
                    Interp = ForceInterp ?? Hz > 75;
                }
            }
            Acc += dt;
            int n = 0;
            while (Acc >= Mathx.Step) { step(Mathx.Step); afterStep(); Acc -= Mathx.Step; n++; }
            return n;
        }
    }
}
