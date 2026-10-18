using System;
using System.Collections.Generic;
using Kaname.Sim;

namespace Kaname.Look
{
    public enum SwingDir { Fwd, Up, Down, Draw, Guard }

    // The sword swing in progress, as the look needs it (js/hero.js `atk`).
    public sealed class LookAtk
    {
        public SwingDir Dir;
        public bool Alt;
        public double T, Live0, Live1, Total;               // time into the swing, when the blade goes live, when it stops being live, when the swing ends
        public bool Swinging => T < Live1 - 1e-6;
    }

    // What the simulation tells the look after every step. The look only ever READS the simulation; nothing here feeds back (a test checks that
    // drawing changes no simulation state). Rungs after U0 fill in more of it as their mechanics arrive.
    public sealed class LookInput
    {
        public double Cx, Y, Vx, Vy, LandVy;
        public int Face = 1;
        public bool OnGround = true;
        public double DashT;                                // time left in a dash
        public bool TrailActive;                            // dashing or lunging as the step began (the speed lines are drawn from it)
        public LookAtk Atk;                                 // the swing, or null
        public double Charge;                               // 0..1, the charge of the draw
        public double ParryT;                               // time left in the open parry window
        public double Invuln, DeadT;                        // untouchable time (he blinks), time down
    }

    // The eased pose state that the drawing reads (js/hero.js `vis`).
    public sealed class Vis
    {
        public double Run, Air, Rise, Lean, Sq, Sqv, Dash, Sprint, Atk, Charge;
    }

    public sealed class SwingState { public SwingDir Dir = SwingDir.Fwd; public bool Alt; public double P, Fade = 1, T, Live0, Live1, Total; }       // (T and the swing's times: the cuts' four phases read them)
    public sealed class TrailSample { public double X, Life, Max, K; }
    public sealed class ScarfPoint { public double X, Y, Ox, Oy; }

    public struct HeadFrame { public double Hy; public (double x, double y) Head, Root; }

    public struct Pose
    {
        public double Run, Air, Rise, D, Sp, L1, L2, Lift1, Lift2, Crouch, Bob, Step, Hb, Hy, Hx, Hyy, Tx, Ty;
        public double W1x, W1y, Kx, Ky, Ang, WA;
    }

    // Kaname's look state: the eased pose, the headband's tail, the speed-line trail and the clocks the drawing reads. Drawn, not simulated:
    // nothing in the simulation reads it (js/hero.js updateVis, updateTrail, updateScarf, kanameHeadFrame; js/art_hero.js kanamePose).
    public sealed class HeroLook
    {
        public const double DashStretch = 0.22, DashSquash = 0.08;     // a dash stretches him long and low: 22 % wider, 8 % shorter at full dash
        public const int ScarfLinks = 8;

        readonly Tuning k;
        readonly Func<double> random;

        public LookInput In = new LookInput();                          // what the simulation last told it
        public readonly Vis Vis = new Vis();
        public readonly SwingState Swing = new SwingState();
        public readonly List<TrailSample> Trail = new List<TrailSample>();
        public readonly ScarfPoint[] Scarf = new ScarfPoint[ScarfLinks];
        public double RunT, T, Blink, BlinkIn = 2.5, ScarfTh = 0.3, ShadowY;
        bool wasGround = true;

        public HeroLook(Tuning tuning, double x = 0, double y = 0, Func<double> rng = null)
        {
            k = tuning;
            if (rng == null) { var r = new Random(20261018); rng = () => r.NextDouble(); }
            random = rng;
            In.Cx = x; In.Y = y;
            var root = Head(x, y).Root;
            for (int i = 0; i < ScarfLinks; i++)
            {
                double px = root.x - i * k.ScarfSeg;
                Scarf[i] = new ScarfPoint { X = px, Y = root.y, Ox = px, Oy = root.y };
            }
        }

        // How far he crouches (u) as he sprints and dashes. Shared by the head frame and the drawing so the two always agree.
        public static double Crouch(Vis v) => v.Sprint * 1.2 + v.Dash * 2 + v.Charge * 1.6;

        // Where Kaname's head and the headband's tail are, in the world, for the current pose, for a body drawn at (rx, ry). ONE definition, used by the
        // simulation of the scarf (its root) and by drawing (the head and the band), so the scarf is tied to the back of his head in every frame.
        public HeadFrame Head(double rx, double ry)
        {
            var v = Vis; double run = v.Run;
            double bob = Math.Sin(T * 3) * 0.6 * (1 - run * 0.5), step = Math.Abs(Math.Sin(RunT)) * run;
            double hy = -26 + bob * 1.4 - step * 1.6 + Crouch(v) * 1.4;     // the head's centre, in the body's own frame (facing +x)
            double tilt = 0.08 * run, ct = Math.Cos(tilt), st = Math.Sin(tilt);
            double c = Math.Cos(v.Lean), s = Math.Sin(v.Lean);
            double fx = In.Face * (1 + v.Sq * 0.4 + v.Dash * DashStretch), fy = 1 - v.Sq * 0.4 - v.Dash * DashSquash;
            (double x, double y) ToWorld(double lx, double ly) => (rx + fx * (lx * c - ly * s), ry + fy * (lx * s + ly * c));
            (double x, double y) Rim(double dx, double dy) => ToWorld(dx * ct - dy * st, dx * st + dy * ct + hy);   // a point on the head, rotated about its centre
            return new HeadFrame { Hy = hy, Head = ToWorld(0, hy), Root = Rim(-12.3, -1) };                          // the band sits 1 u above the head's centre
        }

        // a kick to the squash and stretch: a jump, a dash, a landing, a hit... (the simulation calls it when the event happens)
        public void Kick(double sq) { Vis.Sq = sq; Vis.Sqv = 0; }

        // One step: the simulation's state in `input`, and the time `dt`. Call it after the simulation's step.
        public void Update(LookInput input, double dt)
        {
            In = input;
            if (!wasGround && In.OnGround) Kick(Mathx.Clamp(In.LandVy / 900, 0.25, 1) * 0.42);       // a landing squashes him by how hard he fell
            wasGround = In.OnGround;
            if (In.OnGround) RunT += (Math.Abs(In.Vx) / k.Run) * dt * 14;
            T += dt;
            ShadowY = Stage.FloorBelow(In.Cx, In.Y);
            UpdateVis(dt);
            UpdateTrail(dt, In.TrailActive);
            UpdateScarf(dt);
        }

        // ---- the pose, eased toward the real state so nothing pops ----
        void UpdateVis(double dt)
        {
            var v = Vis; var a = In.Atk; var sw = Swing;
            double speed = Math.Abs(In.Vx) / k.Run;
            bool dashing = In.DashT > 0 || (a != null && a.Dir == SwingDir.Draw && a.Swinging);
            v.Run = Mathx.Approach(v.Run, In.OnGround ? Math.Min(speed, 1.4) : Math.Min(speed, 1) * 0.4, dt * 9);
            v.Air = Mathx.Approach(v.Air, In.OnGround ? 0 : 1, dt * 14);
            v.Rise = Mathx.Approach(v.Rise, In.Vy < 0 ? 1 : 0, dt * 10);
            v.Dash = Mathx.Approach(v.Dash, dashing ? 1 : 0, dt * 30);                                  // the quick one: about two frames
            if (a != null)                                                                                // the blade's progress along its arc
            {
                sw.Dir = a.Dir; sw.Alt = a.Alt; sw.T = a.T; sw.Live0 = a.Live0; sw.Live1 = a.Live1; sw.Total = a.Total;
                sw.P = Mathx.Clamp(a.T / (a.Dir == SwingDir.Draw ? k.DrawTime : 0.19), 0, 1);
                sw.Fade = Mathx.Clamp((a.T - a.Live1) / (a.Total - a.Live1), 0, 1);
            }
            else sw.Fade = 1;
            if (In.ParryT > 0) { sw.Dir = SwingDir.Guard; sw.Alt = false; sw.P = 1; sw.Fade = 0; }       // the guard: the blade held up in front of him
            v.Atk = Mathx.Approach(v.Atk, (a != null && sw.Fade < 0.6) || In.ParryT > 0 ? 1 : 0, dt * 24);   // the arm comes up to the swing and back to the hilt
            v.Charge = Mathx.Approach(v.Charge, In.Charge, dt * 10);                                    // he coils as the charge builds
            double sprintT = Mathx.Clamp((Math.Abs(In.Vx) - k.Run) / (k.Sprint - k.Run), 0, 1) * (dashing ? 0 : 1) * (In.OnGround ? 1 : 0.6);
            v.Sprint = Mathx.Approach(v.Sprint, sprintT, dt * 7);                                       // faster than a run, so it builds visibly
            double leanT = Mathx.Clamp(In.Vx / k.Run, -1, 1) * 0.13 * In.Face + v.Sprint * 0.2 + v.Dash * 0.36
                + v.Atk * (sw.Dir == SwingDir.Up ? -0.04 : sw.Dir == SwingDir.Down ? 0.06 : 0.1) - v.Charge * 0.1 - (In.DeadT > 0 ? 1.3 : 0);
            v.Lean = Mathx.Lerp(v.Lean, leanT, 1 - Math.Exp(-dt * 16));
            v.Sqv += (-380 * v.Sq - 24 * v.Sqv) * dt; v.Sq += v.Sqv * dt;                               // squash and stretch: a damped spring back to rest
            BlinkIn -= dt;
            if (BlinkIn <= 0) { Blink = 0.14; BlinkIn = 2.2 + random() * 3; }
            Blink = Math.Max(0, Blink - dt);
        }

        // ---- the trail the speed lines are drawn from: where he was over the last 0.24 s of a dash, 0.12 s of a sprint ----
        void UpdateTrail(double dt, bool dashing)
        {
            foreach (var g in Trail) g.Life -= dt;
            while (Trail.Count > 0 && Trail[0].Life <= 0) Trail.RemoveAt(0);
            if (dashing || Vis.Sprint > 0.6)                                                             // a sample every step, so the lines' strength is steady
            {
                double life = dashing ? 0.24 : 0.12;
                Trail.Add(new TrailSample { X = In.Cx, Life = life, Max = life, K = dashing ? 0.42 : 0.16 });
            }
        }

        // ---- the scarf: the headband's tail, a chain of 8 points tied to the back of the head, in world space ----
        // Each point chases where a resting scarf would be (streaming back and down, flatter the faster he runs, trailing below him on the way up
        // and lifting on the way down, with a ripple that grows toward the tip), and the tip lags more than the root. Then the segments are held
        // to a fixed length. Chasing a target (instead of drifting) means it always comes back to a sensible shape.
        void UpdateScarf(double dt)
        {
            var p = Scarf; double f = In.Face, s = Math.Min(1, Math.Abs(In.Vx) / k.Run), seg = k.ScarfSeg; var v = Vis;
            var root = Head(In.Cx, In.Y).Root;                                                           // tied to the back of the head, at the headband
            p[0].Ox = p[0].X; p[0].Oy = p[0].Y; p[0].X = root.x; p[0].Y = root.y;
            double droop = Mathx.Lerp(0.5, 0, s);                                                        // it curves down toward the tip at rest, streams flat at speed
            double thT = Mathx.Lerp(0.3, 0.1, s);                                                        // radians below horizontal: hangs back and down at rest, flat at speed
            thT += Mathx.Clamp(-In.Vy / 740, 0, 1) * 0.6;                                                // rising: it trails below him
            thT -= Mathx.Clamp(In.Vy / 900, 0, 1) * 0.9;                                                 // falling: it lifts above him
            thT -= v.Sprint * 0.1 + v.Dash * 0.3;                                                        // a sprint or a dash blows it dead straight
            ScarfTh = Mathx.Lerp(ScarfTh, thT, 1 - Math.Exp(-dt * 10));                                  // eased, so it swings back gracefully on a landing
            double th = ScarfTh, ripple = (0.12 + 0.18 * s) * (1 - v.Dash * 0.7);
            double tx = p[0].X, ty = p[0].Y;
            for (int i = 1; i < p.Length; i++)
            {
                var c = p[i]; var q = p[i - 1]; c.Ox = c.X; c.Oy = c.Y;
                double a = th + droop * ((double)i / (p.Length - 1)) + Math.Sin(T * 7 + i * 0.9) * ripple * ((double)i / (p.Length - 1));
                tx += -f * Math.Cos(a) * seg; ty += Math.Sin(a) * seg;
                double kk = Math.Max(0.1, 0.5 - i * 0.05);                                               // the tip lags more than the root
                c.X += (tx - c.X) * kk; c.Y += (ty - c.Y) * kk;
                double dx = c.X - q.X, dy = c.Y - q.Y, d = Math.Sqrt(dx * dx + dy * dy); if (d == 0) d = 1e-6;
                c.X = q.X + dx / d * seg; c.Y = q.Y + dy / d * seg;
                if (c.Y > ShadowY - 0.6) c.Y = ShadowY - 0.6;                                            // never below the floor
            }
        }

        // ---- the arcs a strike cuts (js/art_hero.js SWINGS; the forward, up and down cuts are redrawn in Unity to match their hitboxes: D123) ----
        // The blade turns about his hand, which goes to the pivot (px, py) in his own frame (facing +x, y down, the feet at the origin; angle 0 = straight
        // ahead, positive = down), and R is how far its tip is from the pivot. A forward, up or down cut has five angles, in the world's frame (the lean is
        // taken off when it is placed): C where the blade is cocked at the press, N where the cut enters the box (the trail starts here), I where the blade is
        // on the first step the box is live (mid-box: the picture a hit freezes, since a blow lands on that step), O where the sweep is done, and E where the
        // follow-through ends. The damaging part is N to O, drawn over the box the blow really covers. The draw and the guard keep the two-angle arc (A0 to
        // A1) and the old easing: for them C = N = I = A0 and O = E = A1.
        public const double BladeLen = 40;
        public const double SweepTime = 0.08;                          // how long the blade takes from I to O, from the moment the box goes live (a look constant, not a tuning number)
        public readonly struct Arc
        {
            public readonly double Px, Py, C, N, I, O, E, R;
            public Arc(double px, double py, double c, double n, double i, double o, double e, double r) { Px = px; Py = py; C = c; N = n; I = i; O = o; E = e; R = r; }
            public Arc(double px, double py, double a0, double a1, double r) : this(px, py, a0, a0, a0, a1, a1, r) { }
            public double A0 => I; public double A1 => O;
        }
        public static Arc Swings(SwingDir d, bool alt = false)
        {
            switch (d)
            {
                case SwingDir.Fwd: return alt ? new Arc(11, -15, 0.09, 0.09, -0.30, -0.76, -1.05, 45)       // low to high: from the knee, up across the front, to above the head
                                              : new Arc(11, -15, -1.50, -0.68, -0.22, 0.19, 0.20, 45);      // high to low: cocked over his head, across the front, to the knee
                case SwingDir.Up: return new Arc(14, -22, -2.85, -2.60, -1.80, -1.22, -1.00, 45);           // from behind and above, over his head, to the front
                case SwingDir.Down: return new Arc(12, -10, 0.80, 1.20, 1.75, 2.45, 2.60, 45);              // from the front, down and under, to behind
                case SwingDir.Draw: return new Arc(14, -15, -0.5, 0.2, 45);        // the draw: one long, flat cut across the front as he lunges
                default: return new Arc(14, -18, -1.1, -1.1, 45);                  // the parry: the blade held up and forward, not moving
            }
        }
        // the trail's span: from where the cut enters the box (N) to where the sweep ends (O); the draw and the guard: their two angles
        public static (double a0, double a1) SwingAngles(SwingState sw)
        {
            var g = Swings(sw.Dir, sw.Alt);
            return (g.N, g.O);
        }
        public static double SwingEase(double p) { double u = 1 - Math.Min(1, p * 1.7); return 1 - u * u; }     // the draw's: quick through the cut, settling at the end
        // a forward, up or down cut: its arc is the blade's angle in the world, so the lean is taken off when it is placed (the draw and the guard keep the body's frame)
        public static bool Pinned(SwingDir d) => d == SwingDir.Fwd || d == SwingDir.Up || d == SwingDir.Down;
        // Where the blade points now, before the lean is taken off. A cut runs in four phases read from the swing's own times: wind-up (the press to the box going
        // live: it accelerates in from C to I, passing N), the sweep (I to O over SweepTime, easing out), the hold (at O until the blade stops being live), the
        // follow-through (O to E over what is left of the swing). The draw is one eased arc over its own time.
        public static double SwingAngle(SwingState sw)
        {
            var g = Swings(sw.Dir, sw.Alt);
            if (!Pinned(sw.Dir)) return Mathx.Lerp(g.A0, g.A1, SwingEase(sw.P));
            double t = sw.T, sweep = Math.Max(1e-9, Math.Min(SweepTime, sw.Live1 - sw.Live0)), sweepEnd = sw.Live0 + sweep;
            if (t < sw.Live0) { double u = Mathx.Clamp(t / sw.Live0, 0, 1); return g.C + (g.I - g.C) * u * u; }
            if (t < sweepEnd) { double u = (t - sw.Live0) / sweep; return g.I + (g.O - g.I) * (1 - (1 - u) * (1 - u)); }
            if (t < sw.Live1) return g.O;
            double w = Mathx.Clamp((t - sw.Live1) / Math.Max(1e-9, sw.Total - sw.Live1), 0, 1);
            return g.O + (g.E - g.O) * (1 - (1 - w) * (1 - w));
        }

        // Every number that places his body for the current pose: used by the drawing and by the speed lines (for their heights).
        public Pose Place()
        {
            var v = Vis; double run = v.Run, air = v.Air, rise = v.Rise, ph = RunT, d = v.Dash, sp = v.Sprint;
            // stride: the legs scissor as he runs, reach further as he sprints, and trail straight out behind in a dash
            double stride = Math.Sin(ph) * 6 * run * (1 + 0.2 * sp) * (1 - d);
            double l1 = stride - d * 7, l2 = -stride - d * 2.5;
            double lift = 3.5 * run * (1 + 0.4 * sp) * (1 - d);
            double lift1 = Math.Max(0, Math.Sin(ph)) * lift + d * 2.5, lift2 = Math.Max(0, -Math.Sin(ph)) * lift + d * 1.2;
            double crouch = Crouch(v);
            double bob = Math.Sin(T * 3) * 0.6 * (1 - run * 0.5) + crouch, step = Math.Abs(Math.Sin(ph)) * run, hb = bob * 0.8;
            double hy = Head(0, 0).Hy;                                       // the head's height: one definition, shared with the scarf's root
            // the katana in its sheath at the hip (it streams back, level, in a dash)
            double hx = 12, hyy = -13.2 + hb, tx = -19.6 - run * 0.8 - d * 4 - sp * 1.5, ty = -5.9 - run * 2.2 - air * 2.5 - d * 2.5 - sp;
            // the hand's place, on the hilt at his side; in a cut it goes to the swing's pivot and the blade turns about it (there is no arm to draw)
            double w1x = 12.6, w1y = -12.9 + hb - air * 0.5, kx = 18, ky = -15 + hb, ang = 0;
            double wA = v.Atk;
            if (wA > 0.001)
            {
                var g = Swings(Swing.Dir, Swing.Alt); ang = SwingAngle(Swing);
                if (Pinned(Swing.Dir)) ang -= v.Lean;                                                    // the cut stays level in the world: the lean moves his hand but does not tilt the blade
                w1x = Mathx.Lerp(w1x, g.Px, wA); w1y = Mathx.Lerp(w1y, g.Py, wA);
                kx = Mathx.Lerp(kx, w1x - Math.Cos(ang) * 3, wA); ky = Mathx.Lerp(ky, w1y - Math.Sin(ang) * 3, wA);
            }
            return new Pose { Run = run, Air = air, Rise = rise, D = d, Sp = sp, L1 = l1, L2 = l2, Lift1 = lift1, Lift2 = lift2, Crouch = crouch, Bob = bob, Step = step, Hb = hb, Hy = hy,
                Hx = hx, Hyy = hyy, Tx = tx, Ty = ty, W1x = w1x, W1y = w1y, Kx = kx, Ky = ky, Ang = ang, WA = wA };
        }
    }
}
