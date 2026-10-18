using Kaname.Sim;
using NUnit.Framework;

namespace Kaname.Tests
{
    // Rung U2, behaviour: the facts the JavaScript acceptance tests state about running and jumping (dev/tests.js: run, stop, turn, stick, jump,
    // heights, head bump, fall), driven like a player drives the game. The golden traces prove the numbers step by step; these say what the numbers mean.
    public class RunJumpTests
    {
        // a player: a fresh world, the controls, and the buttons
        sealed class Player
        {
            public readonly World W = new World(new Tuning());
            readonly ControlState c = new ControlState();
            public Hero P => W.Hero;
            public Player() { Run(2); }                                       // (a clean start: two steps standing, as the JavaScript reset)

            public void Run(int steps, System.Action<int> each = null)
            {
                for (int i = 0; i < steps; i++) { each?.Invoke(i); W.Step(Mathx.Step, c); c.EndStep(); }
            }
            public void Down(Act a) { c.SetHeld(a, true); c.Press(a); Push(); }
            public void Up(Act a) { c.SetHeld(a, false); Push(); }
            public void Tap(Act a, int steps = 3) { Down(a); Run(steps); Up(a); }
            void Push() { c.Push = (c.Held(Act.Right) ? 1 : 0) - (c.Held(Act.Left) ? 1 : 0); }
            // a stick pushed over by `axis` (-1..1): analog, with the dead zone and the curve
            public void Stick(double axis)
            {
                c.Push = Mathx.PadCurve(axis); c.SetHeld(Act.Left, axis < -0.4); c.SetHeld(Act.Right, axis > 0.4);
            }
            public void Place(double x, double y) { P.Place(x, y); W.Cam.X = W.CamPrev = x; if (P.OnGround) Run(1); }
            public int UntilLanded(int max = 300) { for (int i = 1; i <= max; i++) { Run(1); if (P.OnGround) return i; } return -1; }
        }

        // press jump, hold it for `frames`, let go, wait for the landing; the height reached and the steps spent in the air
        static (double h, int frames) JumpHeld(int frames)
        {
            var p = new Player(); p.Down(Act.Jump); int air = 0; bool released = false;
            for (int i = 1; i <= 240; i++)
            {
                p.Run(1); if (!p.P.OnGround) air++;
                if (i == frames) { p.Up(Act.Jump); released = true; }
                if (released && p.P.OnGround) break;
            }
            return (p.P.LastJump, air);
        }

        // a running jump toward a pillar, letting go of right at the top of the arc (as a player would) so he lands on it
        static (double x, double y, bool standing) RunJumpAt(double pillarX)
        {
            var p = new Player(); p.Place(pillarX - 80, 0); p.Down(Act.Right); p.Down(Act.Jump); bool air = false;
            for (int i = 0; i < 200; i++)
            {
                p.Run(1); if (!p.P.OnGround) air = true;
                if (i == 18) p.Up(Act.Right);
                if (i == 30) p.Up(Act.Jump);
                if (air && p.P.OnGround) break;
            }
            p.Up(Act.Right); p.Up(Act.Jump); p.Run(2);
            return (p.P.Cx, p.P.Y, p.P.OnGround);
        }

        [Test]
        public void HeStartsStandingStillFacingRight()
        {
            var p = new Player();
            Assert.AreEqual(0, p.P.Cx); Assert.AreEqual(0, p.P.Y); Assert.IsTrue(p.P.OnGround); Assert.AreEqual(1, p.P.Face); Assert.AreEqual(0, p.P.Vx);
        }

        [Test]
        public void HoldingRightReachesTheTopSpeedOf240WithinATenthOfASecondAndNeverExceedsIt()
        {
            var p = new Player(); p.Down(Act.Right); int n = 0; double peak = 0;
            for (int i = 0; i < 90; i++) { p.Run(1); peak = System.Math.Max(peak, p.P.Vx); if (n == 0 && p.P.Vx >= 239.999) n = i + 1; }
            Assert.That(n, Is.InRange(1, 6)); Assert.AreEqual(240, peak, 0.001); Assert.AreEqual(1, p.P.Face);
        }

        [Test]
        public void LettingGoStopsHimWithinATenthOfASecondAndAboutEightUnits()
        {
            var p = new Player(); p.Down(Act.Right); p.Run(30); double x0 = p.P.Cx; p.Up(Act.Right); int n = 0;
            for (int i = 1; i <= 30; i++) { p.Run(1); if (p.P.Vx == 0) { n = i; break; } }
            Assert.That(n, Is.InRange(1, 6)); Assert.That(p.P.Cx - x0, Is.InRange(4.0, 12.0));
        }

        [Test]
        public void ReversingFlipsHimAtOnceAndHeIsThroughZeroWithin007Seconds()
        {
            var p = new Player(); p.Down(Act.Right); p.Run(30); p.Up(Act.Right); p.Down(Act.Left); p.Run(1);
            Assert.AreEqual(-1, p.P.Face, "faces left the instant he turns"); int n = 0;
            for (int i = 1; i <= 12; i++) { if (p.P.Vx <= 0) { n = i; break; } p.Run(1); }
            Assert.That(n, Is.InRange(1, 4)); p.Run(10); Assert.AreEqual(-240, p.P.Vx, 0.001);
        }

        [Test]
        public void ALightTouchOfTheStickWalksFullTiltRunsTheDeadZoneDoesNothing()
        {
            var p = new Player(); p.Stick(0.1); p.Run(30); Assert.AreEqual(0, p.P.Vx, "inside the dead zone");
            p.Stick(0.5); p.Run(40); double half = p.P.Vx; Assert.That(half, Is.InRange(70.0, 130.0)); Assert.AreEqual(240 * Mathx.PadCurve(0.5), half, 0.5);
            p.Stick(1); p.Run(40); Assert.AreEqual(240, p.P.Vx, 0.001);
            p.Stick(-1); p.Run(40); Assert.AreEqual(-240, p.P.Vx, 0.001); Assert.AreEqual(-1, p.P.Face);
        }

        [Test]
        public void AHeldJumpRisesAbout118UnitsAndIsInTheAirAbout065Seconds()
        {
            var j = JumpHeld(70);
            Assert.That(j.h, Is.InRange(114.0, 122.0)); Assert.That(j.frames, Is.InRange(36, 42));
        }

        [Test]
        public void ATapRisesAbout45UnitsAndTheHeightIsVariableBetweenATapAndAHold()
        {
            var tap = JumpHeld(3); var mid = JumpHeld(12); var full = JumpHeld(70);
            Assert.That(tap.h, Is.InRange(36.0, 56.0)); Assert.Greater(mid.h, tap.h + 10); Assert.Less(mid.h, full.h - 5);
        }

        [Test]
        public void ThereIsNoDoubleJump()
        {
            var p = new Player(); p.Down(Act.Jump); p.Run(18); Assert.That(p.P.Vy, Is.InRange(-200.0, 100.0), "near the top of the arc"); double y = p.P.Y;
            p.Up(Act.Jump); p.Tap(Act.Jump, 2); p.Run(2);
            Assert.Greater(p.P.Vy, -150, "a second press in the air must not launch him again"); Assert.That(p.P.Y, Is.InRange(y - 8, y + 10));
        }

        [Test]
        public void APressUpToAbout012SecondsBeforeLandingStillJumpsButNotQuarterOfASecondBefore()
        {
            (int n, bool jumped) Drop(int pressBefore)
            {
                var p = new Player(); p.Place(0, -96); int n = p.UntilLanded();
                p = new Player(); p.Place(0, -96); p.Run(n - pressBefore); p.Tap(Act.Jump, 1); p.Run(pressBefore + 3);
                return (n, !p.P.OnGround && p.P.Y < -1);
            }
            Assert.IsTrue(Drop(4).jumped, "a press 4 steps (0.07 s) before landing should jump on landing");
            Assert.IsFalse(Drop(15).jumped, "a press 15 steps (0.25 s) before landing should not");
        }

        [Test]
        public void AJumpUpToAbout008SecondsAfterRunningOffALedgeWorksButNot015SecondsAfter()
        {
            bool WalkOff(int wait)
            {
                var p = new Player(); p.Place(Stage.Ledge.X + 140, -96); Assert.IsTrue(p.P.OnGround, "standing on the ledge"); p.Down(Act.Right);
                bool left = false; for (int i = 0; i < 200 && !left; i++) { p.Run(1); if (!p.P.OnGround) left = true; }
                Assert.IsTrue(left, "he should run off the ledge"); p.Run(wait); p.Down(Act.Jump); p.Run(1); p.Up(Act.Jump); p.Up(Act.Right);
                return p.P.Vy < 0;
            }
            Assert.IsTrue(WalkOff(2), "a jump 3 steps (0.05 s) after leaving the ledge should work");
            Assert.IsFalse(WalkOff(9), "a jump 10 steps (0.17 s) after leaving the ledge should not");
        }

        [Test]
        public void AHeldRunJumpClearsA3TilePillarAndTheLedgeButNotA4TilePillar()
        {
            var p3 = RunJumpAt(Stage.Pillars[2].x); var p4 = RunJumpAt(Stage.Pillars[3].x); var ledge = RunJumpAt(Stage.Ledge.X);
            Assert.IsTrue(p3.standing && System.Math.Abs(p3.y + 96) < 0.01, $"should land on the 96 u pillar (landed at y {p3.y:0.0}, standing {p3.standing})");
            Assert.IsFalse(System.Math.Abs(p4.y + 128) < 0.01, $"must not reach the top of the 128 u pillar (y {p4.y:0.0})");
            Assert.IsTrue(ledge.standing && System.Math.Abs(ledge.y + 96) < 0.01, $"should land on the 96 u ledge (y {ledge.y:0.0})");
        }

        [Test]
        public void JumpingUnderTheLedgeStopsHimAt38UnitsAndHeFallsStraightBack()
        {
            var p = new Player(); p.Place(Stage.Ledge.X + 100, 0); p.Down(Act.Jump); double minY = 0; bool bumped = false;
            for (int i = 0; i < 20; i++) { p.Run(1); minY = System.Math.Min(minY, p.P.Y); if (p.P.Vy >= 0 && p.P.Y < -1) bumped = true; }
            p.Up(Act.Jump);
            Assert.That(-minY, Is.InRange(37.5, 38.5)); Assert.IsTrue(bumped, "vertical speed should reset on the bump"); Assert.Greater(p.UntilLanded(), 0, "he comes back down");
        }

        [Test]
        public void TheFallSpeedIsCappedAt900()
        {
            var p = new Player(); p.Place(5000, -3000); double peak = 0; p.Run(150, _ => peak = System.Math.Max(peak, p.P.Vy));
            Assert.AreEqual(900, peak, 0.001);
        }

        [Test]
        public void TheJumpStartIsMarkedSoTheLookCanStretchHim()
        {
            var p = new Player(); p.Down(Act.Jump); bool seen = false; int count = 0;
            for (int i = 0; i < 40; i++) { p.Run(1); if (p.P.JustJumped) { seen = true; count++; } }
            Assert.IsTrue(seen); Assert.AreEqual(1, count, "once per jump");
        }
    }
}
