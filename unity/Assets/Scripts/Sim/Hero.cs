using System;

namespace Kaname.Sim
{
    // Kaname's body (js/hero.js). Rungs U2 and U3 have the run, the jump, the dash and the sprint: a box that accelerates, turns and stops on the numbers of
    // the design, jumps (a variable height, coyote time, a buffered press), dashes (a burst of 128 u, one per jump in the air) and sprints (hold dash
    // and a direction), falls and lands on the stage and bumps its head. The sword and pogo arrive in U4, the charge and draw in U5, health and the
    // parry in U6, each one proved against the recorded traces. Nothing here reads or writes anything
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

        // the dash and the sprint
        public double DashT;               // time left in a dash (a tap of the dash button: a burst of 640 u/s for 0.2 s, flat, untouchable from U6)
        public double DashCd;              // time until the next dash may start
        public double DashBuf;             // time left on a dash press made too early
        public bool CanDash = true;        // a dash is ready (in the air: one per jump, refilled on landing)
        public int DashDir = 1;            // the way the dash goes
        public double DashX0, LastDash;    // measured: where the last dash began and how far it went
        public bool JustDashed;            // true for the step on which a dash began (the look stretches him)
        public bool TrailActive;           // dashing or lunging this step (the look draws speed lines from it)

        public bool Scripted;              // the pose reel (a test aid for judging his look) drives his speed itself, so the run code leaves it alone

        public Hero(Tuning tuning, double x = 0, double y = 0)
        {
            k = tuning;
            Cx = Px = x; Y = Py = y;
            W = k.BodyW; H = k.BodyH;
            Coyote = k.Coyote; AirStartY = y; DashX0 = x;
        }

        public Box Box() => new Box(Cx - W / 2, Y - H, W, H);

        // One fixed step. The order is the JavaScript game's: controls, horizontal, jump, vertical (gravity), move and collide x then y, then the measurements.
        public void Update(double dt, IControls controls)
        {
            Px = Cx; Py = Y;
            bool wasGround = OnGround;
            JustJumped = false; JustDashed = false;
            IControls inp = controls;                                   // (while stunned or down he will hear NoControls: U6)
            double mx = inp.MoveX(), dashHeld = inp.Held(Act.Dash) ? 1 : 0;

            // ---- dash: a press starts it (a press up to 0.1 s early is kept); on the ground only the cooldown limits it, in the air it is one per jump ----
            DashCd = Math.Max(0, DashCd - dt);
            DashBuf = inp.Hit(Act.Dash) ? k.DashBuffer : Math.Max(0, DashBuf - dt);
            if (DashBuf > 0 && DashCd <= 0 && CanDash && DashT <= 0) StartDash(mx);    // (a dash cuts a swing's recovery short, never a blade still looking for its mark: U4)
            bool dashing = DashT > 0, dashEnds = false;
            TrailActive = dashing;                                                      // (and a lunge: U5)

            // ---- horizontal: the keys are a full push, the stick is analog; holding dash raises the top speed (sprint) (the guard and the charge slow it: U5, U6) ----
            if (Scripted) { }
            else if (dashing)
            {
                DashT -= dt; Vx = DashDir * k.DashSpeed;
                if (DashT <= 1e-6) { DashT = 0; dashEnds = true; }
            }
            else
            {
                double target = mx * (dashHeld > 0 ? k.Sprint : k.Run);
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
                JustJumped = true;
                if (dashing) { DashT = 0; dashEnds = true; }                          // a jump cuts a dash short
            }
            if (Jumping && Vy < 0 && !inp.Held(Act.Jump)) { Vy *= k.JumpCut; Jumping = false; }
            if (Vy >= 0) Jumping = false;
            if (dashing && !JustJumped) Vy = wasGround ? k.Grav * dt : 0;               // a dash is flat (on the ground it stays pressed to the floor)
            else Vy = Math.Min(Vy + k.Grav * dt, k.FallMax);                           // gravity, capped at the fall speed (a lunge is flat too: U5)

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
            if (HitX != 0 && dashing) { DashT = 0; dashEnds = true; }                   // a wall ends a dash
            OnGround = false; JustLanded = false;
            Y += Vy * dt;
            for (int i = 0; i < solids.Count; i++)
            {
                var s = solids[i].Box;
                if (!Box().Overlaps(s)) continue;
                if (Vy > 0) { Y = s.Y; LandVy = Vy; Vy = 0; OnGround = true; }
                else if (Vy < 0) { Y = s.Y + s.H + H; Vy = 0; Jumping = false; }     // a head bump
            }

            if (dashEnds)                                                                 // it ends crisply; with dash still held and the push kept, it runs on into a sprint
            {
                if (HitX == 0) Vx = DashDir * (dashHeld > 0 && mx == DashDir ? k.Sprint : k.DashExit);
                LastDash = Math.Abs(Cx - DashX0);
            }

            // ---- measurements ----
            if (wasGround && !OnGround && !Jumping) { AirStartY = Y; Apex = 0; }       // walked off an edge
            if (!OnGround) Apex = Math.Max(Apex, AirStartY - Y);
            if (!wasGround && OnGround) { JustLanded = true; LastJump = Apex; }
            if (OnGround) CanDash = true;                                                 // landing refills the air dash
        }

        // a dash: the way he pushes (the way he faces if he pushes nothing), flat, cancelling a rise or a fall
        void StartDash(double mx)
        {
            DashDir = mx != 0 ? Mathx.Sign(mx) : Face; Face = DashDir;
            DashT = k.DashTime; DashCd = k.DashCooldown; CanDash = false; DashBuf = 0;
            Jumping = false; Vy = 0; DashX0 = Cx;
            JustDashed = true;
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
