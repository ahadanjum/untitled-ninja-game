using System;

namespace Kaname.Sim
{
    // Kaname's body (js/hero.js). Rung U0 has the body only: a box that falls, lands on the stage and bumps its head, turns to face the
    // way he pushes, and carries any horizontal speed it is given. Run and jump arrive in U1, the dash and sprint in U2, the sword and
    // pogo in U3, the charge and draw in U4, health and the parry in U5, the look in U6, each one proved against the recorded traces.
    // Nothing here reads or writes anything drawn: the simulation is numbers, and the view reads them.
    public sealed class Hero
    {
        readonly Tuning k;

        public double Cx, Y;               // position: the middle of his feet (y = 0 is the ground, up is negative)
        public double Px, Py;              // last step's, so drawing can blend between steps
        public double W, H;                // the body
        public double Vx, Vy;
        public int Face = 1;
        public bool OnGround = true, JustLanded;
        public double LandVy;              // how fast he was falling when he last landed
        public int HitX;                   // -1 or 1 on a step he ran into a wall, else 0

        public Hero(Tuning tuning, double x = 0, double y = 0)
        {
            k = tuning;
            Cx = Px = x; Y = Py = y;
            W = k.BodyW; H = k.BodyH;
        }

        public Box Box() => new Box(Cx - W / 2, Y - H, W, H);

        // One fixed step. The order is the JavaScript game's: controls, horizontal, vertical (gravity), move and collide x then y.
        public void Update(double dt, IControls controls)
        {
            Px = Cx; Py = Y;
            bool wasGround = OnGround;
            IControls inp = controls;                                   // (while stunned or down he will hear NoControls: U5)
            double mx = inp.MoveX();

            // horizontal: U1. For now his speed is whatever he was given, and he turns to face the push.
            if (mx != 0) Face = Mathx.Sign(mx);

            // vertical: gravity, capped at the fall speed (jumping: U1)
            Vy = Math.Min(Vy + k.Grav * dt, k.FallMax);

            // move and collide: x first, then y
            var solids = Stage.Solids;
            HitX = 0;
            Cx += Vx * dt;
            for (int i = 0; i < solids.Count; i++)
            {
                var s = solids[i].Box;
                if (!Box().Overlaps(s)) continue;
                if (Vx > 0) Cx = s.X - W / 2; else if (Vx < 0) Cx = s.X + s.W + W / 2;
                HitX = Mathx.Sign(Vx); Vx = 0;
            }
            OnGround = false; JustLanded = false;
            Y += Vy * dt;
            for (int i = 0; i < solids.Count; i++)
            {
                var s = solids[i].Box;
                if (!Box().Overlaps(s)) continue;
                if (Vy > 0) { Y = s.Y; LandVy = Vy; Vy = 0; OnGround = true; }
                else if (Vy < 0) { Y = s.Y + s.H + H; Vy = 0; }
            }
            if (!wasGround && OnGround) JustLanded = true;
        }

        // put him somewhere, standing still with nothing held (tests, and the Toss key)
        public void Place(double x, double y, double vx = 0, double vy = 0, int face = 1)
        {
            Cx = Px = x; Y = Py = y; Vx = vx; Vy = vy; Face = face;
            OnGround = y == Stage.FloorBelow(x, y - 1);
        }
    }
}
