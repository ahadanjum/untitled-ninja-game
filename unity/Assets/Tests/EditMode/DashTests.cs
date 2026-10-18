using Kaname.Game;
using Kaname.Look;
using Kaname.Sim;
using NUnit.Framework;

namespace Kaname.Tests
{
    // Rung U3, behaviour: the facts the JavaScript acceptance tests state about the dash (a tap) and the sprint (hold), driven like a player drives the game.
    // The golden traces prove the numbers step by step; these say what the numbers mean.
    public class DashTests
    {
        // press dash (and keep it down only for one step), then step until the dash is over; how far, how flat, and whether he stayed on the floor
        static (int steps, double dx, double flat, bool grounded) DoDash(Player p)
        {
            double x0 = p.P.Cx, y0 = p.P.Y; int steps = 1; double minY = y0, maxY = y0; bool grounded = p.P.OnGround;
            p.Down(Act.Dash); p.Run(1); p.Up(Act.Dash);
            while (p.P.DashT > 0 && steps < 80) { p.Run(1); steps++; minY = System.Math.Min(minY, p.P.Y); maxY = System.Math.Max(maxY, p.P.Y); grounded = grounded && p.P.OnGround; }
            return (steps, p.P.Cx - x0, maxY - minY, grounded);
        }

        // how many dashes start in `total` steps, given presses of dash at the listed frames (each held for one step)
        static int CountDashes(Player p, int total, params int[] pressAt)
        {
            int n = 0; bool was = false;
            for (int f = 0; f < total; f++)
            {
                if (System.Array.IndexOf(pressAt, f) >= 0) p.Down(Act.Dash);
                if (System.Array.IndexOf(pressAt, f - 1) >= 0) p.Up(Act.Dash);
                p.Run(1); bool now = p.P.DashT > 0; if (now && !was) n++; was = now;
            }
            p.Up(Act.Dash); return n;
        }

        [Test]
        public void ATapGoes128UnitsInASecondOfFiveAlongTheFloorThenEndsCrisply()
        {
            var p = new Player(); var r = DoDash(p);
            Assert.AreEqual(12, r.steps); Assert.AreEqual(128, r.dx, 0.5); Assert.IsTrue(r.grounded, "a ground dash keeps him on the floor"); Assert.AreEqual(0, p.P.Y);
            Assert.AreEqual(210, p.P.Vx, 0.001, "it ends at 210 u/s, not sliding"); Assert.AreEqual(128, p.P.LastDash, 0.5);
            p.Run(8); Assert.AreEqual(0, p.P.Vx, "then he stops (nothing held)");
        }

        [Test]
        public void ItGoesTheWayYouPushAndTheWayHeFacesWhenYouPushNothing()
        {
            var p = new Player(); p.Down(Act.Left); var r = DoDash(p); p.Up(Act.Left);
            Assert.Less(r.dx, -126); Assert.AreEqual(-1, p.P.Face);
            p.Run(60); r = DoDash(p); Assert.Less(r.dx, -126, "with nothing held he dashes the way he faces, left");
            p.Run(60); p.Down(Act.Right); r = DoDash(p); p.Up(Act.Right); Assert.Greater(r.dx, 126); Assert.AreEqual(1, p.P.Face);
        }

        [Test]
        public void ThereIsAHalfSecondCooldownAPressUpToATenthEarlyIsKeptAnEarlierOneIsNot()
        {
            Assert.AreEqual(2, CountDashes(new Player(), 60, 0, 40), "a press after the cooldown dashes again");
            Assert.AreEqual(1, CountDashes(new Player(), 60, 0, 14), "a press 13 steps (0.22 s) before the cooldown ends is ignored");
            Assert.AreEqual(2, CountDashes(new Player(), 60, 0, 23), "a press 4 steps (0.07 s) before the cooldown ends is kept and fires as soon as it ends");
            Assert.AreEqual(1, CountDashes(new Player(), 60, 0, 8), "it cannot be chained during the cooldown");
        }

        [Test]
        public void InTheAirItIsFlatCancelsTheFallAndThereIsOnePerJumpRefilledOnLanding()
        {
            var p = new Player(); p.Place(0, -1500); Assert.IsFalse(p.P.OnGround); p.Run(20); Assert.Greater(p.P.Vy, 300, "falling fast");
            var r = DoDash(p);
            Assert.Less(r.flat, 1e-6, "flat"); Assert.AreEqual(128, r.dx, 0.5); Assert.IsFalse(p.P.CanDash, "the air dash is spent"); Assert.Less(p.P.Vy, 100, "the dash cancelled the fall");
            p.Run(30); Assert.IsFalse(p.P.OnGround, "still in the air, and the cooldown is over");
            Assert.AreEqual(0, CountDashes(p, 5, 0), "a second dash in the same jump does nothing");
            Assert.Greater(p.UntilLanded(400), 0); Assert.IsTrue(p.P.CanDash, "landing refills it"); p.Run(30);
            Assert.AreEqual(1, CountDashes(p, 5, 0), "and he can dash again");
        }

        [Test]
        public void AWallEndsIt()
        {
            var p = new Player(); p.Place(Stage.Pillars[0].x - 80, 0); var r = DoDash(p);
            Assert.AreEqual(Stage.Pillars[0].x - 8, p.P.Cx, 0.001, "he stops against the pillar"); Assert.Less(r.steps, 12); Assert.AreEqual(0, p.P.DashT); Assert.AreEqual(0, p.P.Vx);
        }

        [Test]
        public void AJumpCutsItShortAndKeepsGoingForward()
        {
            var p = new Player(); p.Down(Act.Right); p.Down(Act.Dash); p.Run(1); p.Up(Act.Dash); p.Run(3); Assert.Greater(p.P.DashT, 0, "mid-dash");
            p.Tap(Act.Jump, 1);
            Assert.AreEqual(0, p.P.DashT, "the jump ended the dash"); Assert.IsTrue(p.P.Vy < -500 && !p.P.OnGround); Assert.GreaterOrEqual(p.P.Vx, 205, "and still moving forward");
        }

        [Test]
        public void HoldingDashAndADirectionSprintsAt336AfterABurstAndLettingGoDropsHimToARun()
        {
            var p = new Player(); p.Place(-3000, 0); p.Down(Act.Right); p.Down(Act.Dash); int burst = 0; double peakAfter = 0;
            for (int i = 1; i <= 70; i++) { p.Run(1); if (p.P.DashT > 0) burst = i; if (i > 14) peakAfter = System.Math.Max(peakAfter, p.P.Vx); }
            Assert.AreEqual(12, burst + 1, "the press bursts for 12 steps first"); Assert.AreEqual(128, p.P.LastDash, 0.5, "the burst covers 128 u");
            Assert.AreEqual(336, p.P.Vx, 0.001, "then he sprints at 336"); Assert.LessOrEqual(peakAfter, 336.001);
            p.Up(Act.Dash); p.Run(6); Assert.AreEqual(240, p.P.Vx, 0.001, "letting go of dash brings him back to a run");
        }

        [Test]
        public void TheSprintSpeedCarriesThroughAJumpWhileDashIsHeldAndDropsBackWhenItIsLetGo()
        {
            var p = new Player(); p.Place(-3000, 0); p.Down(Act.Right); p.Down(Act.Dash); p.Run(40); Assert.AreEqual(336, p.P.Vx, 0.001);
            p.Down(Act.Jump); p.Run(12); Assert.IsFalse(p.P.OnGround); Assert.AreEqual(336, p.P.Vx, 0.001, "the sprint speed carries in the air");
            p.Up(Act.Dash); p.Run(8); Assert.AreEqual(240, p.P.Vx, 0.001, "let go of dash: back to the run speed in the air");
        }

        [Test]
        public void ADashHeldWithNoDirectionDoesNothingButTheBurst()
        {
            var p = new Player(); p.Down(Act.Dash); p.Run(40);
            Assert.AreEqual(0, p.P.Vx, "no direction: no sprint"); Assert.AreEqual(128, p.P.LastDash, 0.5, "just the one burst");
        }

        [Test]
        public void ADashStartIsMarkedOncePerDashSoTheLookCanStretchHim()
        {
            var p = new Player(); p.Down(Act.Dash); int count = 0;
            for (int i = 0; i < 40; i++) { p.Run(1); if (p.P.JustDashed) count++; }
            Assert.AreEqual(1, count);
        }

        // the look, driven by the real body the way the game drives it
        sealed class Looking
        {
            public readonly Player P = new Player(); public readonly HeroLook Look; readonly LookInput li = new LookInput();
            public Looking() { Look = new HeroLook(new Tuning()); }
            public void Run(int steps)
            {
                for (int i = 0; i < steps; i++)
                {
                    P.Run(1);
                    if (P.P.JustJumped) Look.Kick(-0.28);
                    if (P.P.JustDashed) Look.Kick(-0.12);
                    HeroView.Fill(li, P.P); Look.Update(li, Mathx.Step);
                }
            }
        }

        [Test]
        public void TheSprintLeansHimInWithLongerFasterStridesAndALowerHeadTheDashIsLongerAndLowerStill()
        {
            var g = new Looking(); g.P.Place(-3000, 0); g.P.Down(Act.Right); g.Run(40);
            double runLean = g.Look.Vis.Lean, runCrouch = HeroLook.Crouch(g.Look.Vis), r0 = g.Look.RunT; g.Run(30); double runRate = (g.Look.RunT - r0) / 30;
            g.P.Down(Act.Dash); g.Run(5);
            double dashD = g.Look.Vis.Dash, dashLean = g.Look.Vis.Lean, dashCrouch = HeroLook.Crouch(g.Look.Vis); int dashGhosts = g.Look.Trail.Count;
            g.Run(40);
            double spS = g.Look.Vis.Sprint, spLean = g.Look.Vis.Lean, spCrouch = HeroLook.Crouch(g.Look.Vis); int spGhosts = g.Look.Trail.Count; r0 = g.Look.RunT; g.Run(30); double spRate = (g.Look.RunT - r0) / 30;
            Assert.Greater(dashD, 0.9, "the dash pose is full within 5 steps"); Assert.GreaterOrEqual(dashGhosts, 2, "a dash records a trail to draw");
            Assert.Greater(spS, 0.95, "the sprint pose is full"); Assert.GreaterOrEqual(spGhosts, 1, "and so does a sprint");
            Assert.Greater(spLean, runLean + 0.15, "he leans further into a sprint"); Assert.Greater(dashLean, runLean + 0.15, "and a dash");
            Assert.AreEqual(1.4, spRate / runRate, 0.05, "the strides cycle 1.4 times faster, at 1.4 times the speed");
            Assert.IsTrue(spCrouch > runCrouch + 1 && dashCrouch > spCrouch, "lower in a sprint and lowest in a dash");
            g.P.Up(Act.Dash); g.P.Up(Act.Right); g.Run(120);
            Assert.AreEqual(0, g.Look.Trail.Count, "the trail dies away"); Assert.IsTrue(g.Look.Vis.Sprint < 0.01 && g.Look.Vis.Dash < 0.01, "and the poses ease back to standing");
        }
    }
}
