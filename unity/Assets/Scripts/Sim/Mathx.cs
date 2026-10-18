using System;

namespace Kaname.Sim
{
    // The small helpers of js/util.js, with the same behaviour (so the traces match).
    public static class Mathx
    {
        public const double Step = 1.0 / 60.0;   // the simulation is a fixed 60 Hz step; only drawing varies

        public static double Clamp(double v, double a, double b) => Math.Max(a, Math.Min(b, v));
        public static double Lerp(double a, double b, double t) => a + (b - a) * t;
        public static int Sign(double v) => v < 0 ? -1 : v > 0 ? 1 : 0;
        public static double Approach(double v, double t, double d) => v < t ? Math.Min(v + d, t) : Math.Max(v - d, t);
        public static double Ease(double t) => t * t * (3 - 2 * t);

        // The stick: nothing below 0.18, full speed at 0.95 (js/util.js padCurve). Keys and the d-pad are a full push and pass through.
        public const double PadDeadzone = 0.18, PadFull = 0.95;
        public static double PadCurve(double a)
        {
            double m = Math.Abs(a);
            return m < PadDeadzone ? 0 : Sign(a) * Math.Min(1, (m - PadDeadzone) / (PadFull - PadDeadzone));
        }
    }

    // A critically damped spring (the camera): X chases `target` over about `smooth` seconds, with momentum (js/util.js smoothDamp).
    public struct Spring
    {
        public double X, V;

        public void SmoothDamp(double target, double smooth, double dt)
        {
            double w = 2 / smooth, x = w * dt, e = 1 / (1 + x + 0.48 * x * x + 0.235 * x * x * x);
            double d = X - target, t = (V + w * d) * dt;
            V = (V - w * t) * e;
            X = target + (d + t) * e;
        }
    }
}
