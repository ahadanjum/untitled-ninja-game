using System;
using Kaname.Sim;
using NUnit.Framework;

namespace Kaname.Tests
{
    // Rung U0, behaviour: the same facts the JavaScript acceptance tests state about the stage, the body, the camera and the loop.
    public class SimulationTests
    {
        static World NewWorld() => new World(new Tuning());
        static void Run(World w, int steps, IControls c = null)
        {
            c ??= NoControls.Instance;
            for (int i = 0; i < steps; i++) w.Step(Mathx.Step, c);
        }

        // ---- the numbers ----
        [Test]
        public void TheTuningHoldsTheDesignedNumbers()
        {
            var k = new Tuning();
            Assert.AreEqual(16, k.BodyW); Assert.AreEqual(34, k.BodyH);
            Assert.AreEqual(240, k.Run); Assert.AreEqual(740, k.JumpV); Assert.AreEqual(2200, k.Grav); Assert.AreEqual(900, k.FallMax);
            Assert.AreEqual(640, k.DashSpeed); Assert.AreEqual(336, k.Sprint); Assert.AreEqual(46, k.Reach);
            Assert.AreEqual(5, k.Hp); Assert.AreEqual(4, k.ParryWindows.Length); Assert.AreEqual(0.26, k.ParryWindows[0]);
            Assert.AreEqual(36, k.CameraLead); Assert.AreEqual(0.16, k.CameraSmooth);
        }

        // ---- the stage ----
        [Test]
        public void TheStageHasTheGroundFourPillarsAndALedge()
        {
            Assert.AreEqual(6, Stage.Solids.Count);
            Assert.AreEqual(SolidKind.Ground, Stage.Solids[0].Kind);
            for (int i = 0; i < 4; i++)
            {
                var b = Stage.Solids[1 + i].Box;
                Assert.AreEqual(420 + 200 * i, b.X); Assert.AreEqual(60, b.W); Assert.AreEqual(32 * (i + 1), b.H); Assert.AreEqual(-32 * (i + 1), b.Y);
            }
            var l = Stage.Solids[5].Box;
            Assert.AreEqual(1260, l.X); Assert.AreEqual(-96, l.Y); Assert.AreEqual(200, l.W); Assert.AreEqual(24, l.H);
        }

        [Test]
        public void FloorBelowFindsTheTopOfWhatIsUnderAPoint()
        {
            Assert.AreEqual(0, Stage.FloorBelow(0, -1));
            Assert.AreEqual(-32, Stage.FloorBelow(450, -40));          // on the 1-tile pillar
            Assert.AreEqual(-96, Stage.FloorBelow(1300, -100));        // on the ledge
            Assert.AreEqual(0, Stage.FloorBelow(1300, -50));           // beneath the ledge: the ground
        }

        // ---- the body ----
        [Test]
        public void HeStartsStandingAtTheOrigin()
        {
            var h = NewWorld().Hero;
            Assert.AreEqual(0, h.Cx); Assert.AreEqual(0, h.Y); Assert.IsTrue(h.OnGround); Assert.AreEqual(1, h.Face);
            Assert.AreEqual(16, h.W); Assert.AreEqual(34, h.H);
        }

        [Test]
        public void HeStaysStandingOnTheGround()
        {
            var w = NewWorld(); Run(w, 120);
            Assert.AreEqual(0, w.Hero.Y); Assert.IsTrue(w.Hero.OnGround); Assert.AreEqual(0, w.Hero.Vy);
        }

        [Test]
        public void HeFallsAndLands()
        {
            var w = NewWorld(); w.Hero.Place(0, -200);
            Assert.IsFalse(w.Hero.OnGround);
            int landed = -1;
            for (int i = 1; i <= 200 && landed < 0; i++) { w.Step(Mathx.Step, NoControls.Instance); if (w.Hero.OnGround) landed = i; }
            Assert.Greater(landed, 0, "he never landed");
            Assert.AreEqual(0, w.Hero.Y);
            Assert.IsTrue(w.Hero.JustLanded);
            // a fall of 200 u at 2200 u/s² takes sqrt(2 * 200 / 2200) = 0.426 s, about 26 steps
            Assert.That(landed, Is.InRange(24, 28));
        }

        [Test]
        public void HisFallSpeedIsCapped()
        {
            var w = NewWorld(); w.Hero.Place(0, -3000);
            double fastest = 0;
            for (int i = 0; i < 300; i++) { w.Step(Mathx.Step, NoControls.Instance); fastest = Math.Max(fastest, w.Hero.Vy); }
            Assert.AreEqual(900, fastest, 1e-9);
        }

        [Test]
        public void HeLandsOnAPillarAndOnTheLedge()
        {
            var w = NewWorld(); w.Hero.Place(450, -300); Run(w, 120);
            Assert.AreEqual(-32, w.Hero.Y); Assert.IsTrue(w.Hero.OnGround);
            w = NewWorld(); w.Hero.Place(1350, -300); Run(w, 150);
            Assert.AreEqual(-96, w.Hero.Y); Assert.IsTrue(w.Hero.OnGround);
        }

        [Test]
        public void HeBumpsHisHeadOnTheLedgeAndFallsBack()
        {
            var w = NewWorld(); w.Hero.Place(1300, -10, 0, -900);
            double highest = 0; bool bumped = false;
            for (int i = 0; i < 150; i++)
            {
                double vy = w.Hero.Vy; w.Step(Mathx.Step, NoControls.Instance);
                if (vy < 0 && w.Hero.Vy == 0 && w.Hero.Y == -38) bumped = true;       // the ledge's underside is at y = -72; his head is 34 u above his feet
                highest = Math.Min(highest, w.Hero.Y);
            }
            Assert.IsTrue(bumped, "he never hit the ledge's underside");
            Assert.AreEqual(-38, highest, 1e-9);
            Assert.AreEqual(0, w.Hero.Y); Assert.IsTrue(w.Hero.OnGround);
        }

        [Test]
        public void RunningIntoAWallStopsHim()
        {
            var w = NewWorld(); w.Hero.Place(300, 0); var c = new ControlState { Push = 1 }; c.SetHeld(Act.Right, true);   // running at the 1-tile pillar (x 420..480), on the ground
            int hit = 0;
            for (int i = 0; i < 80; i++) { w.Step(Mathx.Step, c); c.EndStep(); if (w.Hero.HitX != 0) hit = w.Hero.HitX; }
            Assert.AreEqual(1, hit);
            Assert.AreEqual(420 - 8, w.Hero.Cx, 1e-9);                                  // his right edge against the pillar's left
            Assert.AreEqual(0, w.Hero.Vx);
        }

        [Test]
        public void HeCannotTunnelThroughTheGroundAtFullFallSpeed()
        {
            var w = NewWorld(); w.Hero.Place(0, -1000, 0, 900); Run(w, 400);
            Assert.AreEqual(0, w.Hero.Y);
        }

        [Test]
        public void HeTurnsToFaceTheWayHePushes()
        {
            var w = NewWorld(); var c = new ControlState();
            c.Push = -1; Run(w, 1, c); Assert.AreEqual(-1, w.Hero.Face);
            c.Push = 0.4; Run(w, 1, c); Assert.AreEqual(1, w.Hero.Face);
            c.Push = 0; Run(w, 5, c); Assert.AreEqual(1, w.Hero.Face, "he keeps facing the way he last pushed");
        }

        [Test]
        public void ThePreviousPositionIsKeptForBlending()
        {
            var w = NewWorld(); w.Hero.Place(0, -100);
            double y0 = w.Hero.Y; w.Step(Mathx.Step, NoControls.Instance);
            Assert.AreEqual(y0, w.Hero.Py); Assert.Greater(w.Hero.Y, w.Hero.Py);
        }

        // ---- the camera ----
        [Test]
        public void TheCameraLeadsHimByThirtySixUnitsAndSettles()
        {
            var w = NewWorld(); Run(w, 120);
            Assert.AreEqual(36, w.Cam.X, 0.01);
            var c = new ControlState { Push = -1 }; Run(w, 1, c); c.Push = 0;
            Run(w, 180, c);
            Assert.AreEqual(w.Hero.Cx - 36, w.Cam.X, 0.01, "after he turns, the camera swings to lead the other way (he slid a little as he turned)");
        }

        [Test]
        public void TheCameraNeverOvershoots()
        {
            var s = new Spring { X = 0, V = 0 };
            double highest = 0;
            for (int i = 0; i < 240; i++) { s.SmoothDamp(100, 0.16, Mathx.Step); highest = Math.Max(highest, s.X); }
            Assert.LessOrEqual(highest, 100.0000001);
            Assert.AreEqual(100, s.X, 1e-6);
        }

        // ---- the dev keys ----
        [Test]
        public void TheDevKeysWork()
        {
            var w = NewWorld(); var c = new ControlState();
            c.PressDev(Dev.Grid); Run(w, 1, c); c.EndStep(); Assert.IsFalse(w.View.Grid);
            c.PressDev(Dev.Night); Run(w, 1, c); c.EndStep(); Assert.IsTrue(w.View.Night);
            c.PressDev(Dev.Hints); Run(w, 1, c); c.EndStep(); Assert.IsFalse(w.View.Hints);
            c.PressDev(Dev.Debug); Run(w, 1, c); c.EndStep(); Assert.IsTrue(w.View.Debug);
            c.PressDev(Dev.ZoomIn); Run(w, 1, c); c.EndStep(); Assert.AreEqual(3 * 1.15, w.View.Zoom, 1e-12);
            for (int i = 0; i < 40; i++) { c.PressDev(Dev.ZoomIn); Run(w, 1, c); c.EndStep(); }
            Assert.AreEqual(6, w.View.Zoom);
            for (int i = 0; i < 80; i++) { c.PressDev(Dev.ZoomOut); Run(w, 1, c); c.EndStep(); }
            Assert.AreEqual(1.5, w.View.Zoom);
        }

        [Test]
        public void ResetPutsHimBackAtTheStart()
        {
            var w = NewWorld(); w.Hero.Place(900, -50, 100, 0); w.Cam.X = 900;
            var c = new ControlState(); c.PressDev(Dev.Reset); Run(w, 1, c);
            Assert.AreEqual(0, w.Hero.Cx); Assert.AreEqual(0, w.Hero.Y); Assert.IsTrue(w.Hero.OnGround);
            Assert.AreEqual(0, w.CamPrev);
        }

        [Test]
        public void TossLiftsHimTwoHundredUnitsAndLetsGo()
        {
            var w = NewWorld(); Run(w, 5);
            var c = new ControlState(); c.PressDev(Dev.Toss); w.Step(Mathx.Step, c); c.EndStep();
            Assert.Less(w.Hero.Y, -190); Assert.IsFalse(w.Hero.OnGround);
            Run(w, 200); Assert.AreEqual(0, w.Hero.Y); Assert.IsTrue(w.Hero.OnGround);
        }

        // ---- the controls ----
        [Test]
        public void APressSurvivesUntilAStepConsumesIt()
        {
            var c = new ControlState();
            c.Press(Act.Jump); c.SetHeld(Act.Jump, true);
            Assert.IsTrue(c.Hit(Act.Jump)); Assert.IsTrue(c.Hit(Act.Jump), "still there until the step ends");
            c.EndStep();
            Assert.IsFalse(c.Hit(Act.Jump)); Assert.IsTrue(c.Held(Act.Jump), "held stays");
        }

        [Test]
        public void TheStickHasADeadZoneAndAFullPush()
        {
            Assert.AreEqual(0, Mathx.PadCurve(0.1));
            Assert.AreEqual(0, Mathx.PadCurve(-0.18));
            Assert.AreEqual(1, Mathx.PadCurve(0.95));
            Assert.AreEqual(-1, Mathx.PadCurve(-1));
            Assert.AreEqual(0.5, Mathx.PadCurve(0.565), 1e-9);
        }

        [Test]
        public void StunnedControlsSayNothing()
        {
            var n = NoControls.Instance;
            foreach (Act a in Enum.GetValues(typeof(Act))) { Assert.IsFalse(n.Held(a)); Assert.IsFalse(n.Hit(a)); }
            Assert.AreEqual(0, n.MoveX());
        }

        // ---- the loop ----
        [Test]
        public void TheLoopRunsSixtyStepsASecondAndKeepsTheRemainder()
        {
            var clock = new FixedClock(); int steps = 0;
            Action<double> step = dt => { steps++; Assert.AreEqual(1.0 / 60, dt); };
            Assert.AreEqual(0, clock.Advance(0.5, step, () => { }), "the first frame only starts the clock");
            for (int i = 0; i < 6; i++) clock.Advance(0.01, step, () => { });             // 60 ms: three steps are due at 50 ms, the rest is the remainder
            Assert.AreEqual(3, steps);
            Assert.AreEqual(0.06 - 3.0 / 60, clock.Acc, 1e-12);
        }

        [Test]
        public void AStallRunsAtMostSixStepsAndDoesNotBurst()
        {
            var clock = new FixedClock(); clock.Advance(0, d => { }, () => { });
            int steps = 0; clock.Advance(5.0, d => steps++, () => { });
            Assert.AreEqual(6, steps);                                                       // 0.1 s, the most it will catch up
        }

        [Test]
        public void StepsAreEndedAfterEachStepSoPressesAreConsumed()
        {
            var clock = new FixedClock(); clock.Advance(0, d => { }, () => { });
            int ended = 0, steps = 0; clock.Advance(0.05, d => steps++, () => ended++);
            Assert.AreEqual(steps, ended); Assert.AreEqual(3, steps);
        }

        [Test]
        public void ItBlendsOnlyOnAFastDisplay()
        {
            var fast = new FixedClock(); fast.Advance(0, d => { }, () => { });
            for (int i = 0; i < 40; i++) fast.Advance(1.0 / 120, d => { }, () => { });
            Assert.AreEqual(120, fast.Hz, 0.01); Assert.IsTrue(fast.Interp);
            Assert.That(fast.Alpha, Is.InRange(0.0, 1.0));

            var slow = new FixedClock(); slow.Advance(0, d => { }, () => { });
            for (int i = 0; i < 40; i++) slow.Advance(1.0 / 60, d => { }, () => { });
            Assert.AreEqual(60, slow.Hz, 0.01); Assert.IsFalse(slow.Interp); Assert.AreEqual(1, slow.Alpha, "a 60 Hz display draws the latest step as it is");

            var forced = new FixedClock { ForceInterp = true }; forced.Advance(0, d => { }, () => { });
            for (int i = 0; i < 40; i++) forced.Advance(1.0 / 60, d => { }, () => { });
            Assert.IsTrue(forced.Interp);
        }
    }
}
