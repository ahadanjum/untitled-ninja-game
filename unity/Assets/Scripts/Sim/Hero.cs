using System;

namespace Kaname.Sim
{
    // Kaname's body (js/hero.js). Rung U2 has the run and the jump: a box that accelerates, turns and stops on the numbers of the design, jumps (a variable
    // height, coyote time, a buffered press), falls and lands on the stage and bumps its head. The dash and sprint arrive in U3, the sword and pogo
    // in U4, the charge and draw in U5, health and the parry in U6, each one proved against the recorded traces. Nothing here reads or writes anything
    // drawn: the simulation is numbers, and the look reads them.
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

        // the jump
        public double Coyote;              // time left in which a jump still works after he left the ground
        public double JumpBuf;             // time left on a jump press made too early
        public bool Jumping;               // rising from a jump with the button down (letting go cuts it short)
        public bool JustJumped;            // true for the step on which a jump began (the look squashes him up)
        public double AirStartY, Apex;     // measured: how high above where his feet left the ground he has been
        public double LastJump;            // and how high the last jump went

        public bool Scripted;              // the pose reel (a test aid for judging his look) drives his speed itself, so the run code leaves it alone

        public Hero(Tuning tuning, double x = 0, double y = 0)
        {
            k = tuning;
            Cx = Px = x; Y = Py = y;
            W = k.BodyW; H = k.BodyH;
            Coyote = k.Coyote; AirStartY = y;
        }

        public Box Box() => new Box(Cx - W / 2, Y - H, W, H);

        // One fixed step. The order is the JavaScript game's: controls, horizontal, jump, vertical (gravity), move and collide x then y, then the measurements.
        public void Update(double dt, IControls controls)
        {
            Px = Cx; Py = Y;
            bool wasGround = OnGround;
            JustJumped = false;
            IControls inp = controls;                                   // (while stunned or down he will hear NoControls: U6)
            double mx = inp.MoveX();

            // ---- horizontal: the keys are a full push, the stick is analog (sprint, the guard and the charge change the top speed: U3, U5, U6) ----
            if (!Scripted)
            {
                double target = mx * k.Run;
                double acc;
                if (mx == 0) acc = wasGround ? k.GroundDecel : k.AirDecel;
                else if (Vx * mx < 0) acc = wasGround ? k.Turn : k.AirAccel * 1.4;
                else if (Math.Abs(Vx) > Math.Abs(target)) acc = wasGround ? k.GroundDecel : k.AirDecel;
                else acc = wasGround ? k.GroundAccel : k.AirAccel;
                Vx = Mathx.Approach(Vx, target, acc * dt);
                if (mx != 0) Face = Mathx.Sign(mx);                       // (not while committed to a swing: U4)
            }

            // ---- jump: coyote time, a buffered press, a variable height ----
            Coyote = wasGround ? k.Coyote : Math.Max(0, Coyote - dt);
            JumpBuf = inp.Hit(Act.Jump) ? k.JumpBuffer : Math.Max(0, JumpBuf - dt);
            if (JumpBuf > 0 && Coyote > 0)
            {
                Vy = -k.JumpV; JumpBuf = 0; Coyote = 0; Jumping = true;
                OnGround = false; AirStartY = Y; Apex = 0;
                JustJumped = true;                                        // (a jump also cuts a dash short: U3)
            }
            if (Jumping && Vy < 0 && !inp.Held(Act.Jump)) { Vy *= k.JumpCut; Jumping = false; }
            if (Vy >= 0) Jumping = false;
            Vy = Math.Min(Vy + k.Grav * dt, k.FallMax);                  // gravity, capped at the fall speed (a dash and a lunge are flat: U3, U5)

            // ---- move and collide: x first, then y ----
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
                else if (Vy < 0) { Y = s.Y + s.H + H; Vy = 0; Jumping = false; }     // a head bump
            }

            // ---- measurements ----
            if (wasGround && !OnGround && !Jumping) { AirStartY = Y; Apex = 0; }       // walked off an edge
            if (!OnGround) Apex = Math.Max(Apex, AirStartY - Y);
            if (!wasGround && OnGround) { JustLanded = true; LastJump = Apex; }
        }

        // put him somewhere, standing still with nothing held (tests, and the Toss key)
        public void Place(double x, double y, double vx = 0, double vy = 0, int face = 1)
        {
            Cx = Px = x; Y = Py = y; Vx = vx; Vy = vy; Face = face;
            OnGround = y == Stage.FloorBelow(x, y - 1);
            Coyote = OnGround ? k.Coyote : 0;
            Jumping = false; JumpBuf = 0;
        }
    }
}
