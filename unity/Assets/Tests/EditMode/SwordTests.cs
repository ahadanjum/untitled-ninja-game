using System.Collections.Generic;
using Kaname.Sim;
using NUnit.Framework;

namespace Kaname.Tests
{
    // Rungs U4, behaviour: the facts the JavaScript acceptance tests state about the sword, the dummy, hit-stop and the pogo (dev/tests.js: sword, dummy, pogo,
    // attack-to-reset), driven like a player drives the game. The golden traces prove the numbers step by step; these say what the numbers mean.
    public class SwordTests
    {
        // one dummy, nothing else to hit
        static Dummy Lone(Player p, double x, double y = 0, DummyKind kind = DummyKind.Post) { var d = new Dummy(x, y, kind); p.W.Dummies = new List<Dummy> { d }; return d; }

        // how many swings start in `total` steps, given presses of attack at the listed frames (each held for one step)
        static int CountSwings(Player p, int total, params int[] pressAt)
        {
            int n = 0; bool was = false;
            for (int f = 0; f < total; f++)
            {
                if (System.Array.IndexOf(pressAt, f) >= 0) p.Down(Act.Attack);
                if (System.Array.IndexOf(pressAt, f - 1) >= 0) p.Up(Act.Attack);
                p.Run(1); bool now = p.P.Atk != null; if (now && !was) n++; was = now;
            }
            p.Up(Act.Attack); return n;
        }

        [Test]
        public void APressStartsAStrikeTheBladeIsLiveFrom003To014SecondsAndTheSwingIsOverAt02()
        {
            var p = new Player(); p.W.Dummies.Clear(); p.Down(Act.Attack); p.Run(1); p.Up(Act.Attack);
            Assert.IsNotNull(p.P.Atk); Assert.AreEqual(StrikeDir.Fwd, p.P.Atk.Dir);
            int first = -1, live = 0, steps = 1;
            for (int i = 1; i <= 20; i++) { p.Run(1); if (p.P.Atk != null) steps = i + 1; if (p.P.Blow() != null) { live++; if (first < 0) first = i; } }
            Assert.That(first, Is.InRange(1, 3)); Assert.That(live, Is.InRange(6, 8)); Assert.That(steps, Is.InRange(11, 13));
        }

        [Test]
        public void AForwardStrikeReaches46PastHisBodyAndTheDummyTakesOneBlowPerSwing()
        {
            foreach (var (face, act) in new[] { (1, Act.Right), (-1, Act.Left) })
            {
                var p = new Player(); p.Down(act); p.Run(2); p.Up(act); p.Run(10); Assert.AreEqual(face, p.P.Face, "faces the way he last pushed");
                var d = Lone(p, face * 60); p.Tap(Act.Attack, 1); p.Run(14); Assert.AreEqual(1, d.Hits, "struck once, though the blade is live for several steps");
                p.Run(30); d = Lone(p, face * 75); p.Tap(Act.Attack, 1); p.Run(14); Assert.AreEqual(0, d.Hits, "75 u away is out of reach");
            }
        }

        [Test]
        public void UpStrikesAboveHisHeadDownStrikesBelowHisFeetInTheAirAndOnTheGroundDownSwingsForward()
        {
            var p = new Player(); var d = Lone(p, 0, -60, DummyKind.Hanging); p.Tap(Act.Attack, 1); p.Run(14); Assert.AreEqual(0, d.Hits, "a forward strike does not reach a dummy hanging overhead");
            p.Run(30); p.Down(Act.Up); p.Tap(Act.Attack, 1); p.Up(Act.Up); p.Run(14); Assert.AreEqual(1, d.Hits, "up + strike hits it");
            p = new Player(); p.Place(0, -60); d = Lone(p, 0, 0); p.Down(Act.Down); p.Tap(Act.Attack, 1); p.Run(3); p.Up(Act.Down);
            Assert.AreEqual(1, p.P.Strikes, "down + strike in the air hits the post below him"); Assert.AreEqual(1, d.Hits);
            p = new Player(); p.W.Dummies.Clear(); p.Down(Act.Down); p.Tap(Act.Attack, 1); Assert.AreEqual(StrikeDir.Fwd, p.P.Atk.Dir, "down on the ground is an ordinary forward strike");
        }

        [Test]
        public void WhenABlowLandsTheWorldHoldsStillFor4StepsAndPressesMadeThenAreNotLost()
        {
            var p = new Player(); var d = Lone(p, 40); p.Tap(Act.Attack, 1); int n = 0; while (d.Hits == 0 && n++ < 20) p.Run(1);
            Assert.AreEqual(1, d.Hits, "the blow lands"); double t0 = p.P.Atk.T, x0 = p.P.Cx; int frozen = 0;
            for (int i = 0; i < 12; i++) { if (i == 1) p.Down(Act.Jump); if (i == 2) p.Up(Act.Jump); p.Run(1); if (p.P.Atk != null && p.P.Atk.T == t0) frozen++; else break; }
            Assert.AreEqual(4, frozen, "the hero and the dummy are held for 4 steps (0.066 s)"); Assert.AreEqual(x0, p.P.Cx);
            p.Run(10); Assert.IsTrue(!p.P.OnGround || p.P.Y < -1, "a jump pressed while it was frozen is still made once it moves again");
        }

        [Test]
        public void HeCannotTurnDashOrSwingAgainWhileTheBladeIsLiveADashCutsTheRecoveryShortTheCooldownIs03AndAPressUp012EarlyIsKept()
        {
            var p = new Player(); p.W.Dummies.Clear(); p.Tap(Act.Attack, 1); p.Down(Act.Left); p.Run(3); Assert.AreEqual(1, p.P.Face, "he cannot turn while the blade is live"); p.Run(12); Assert.AreEqual(-1, p.P.Face, "and turns once it is spent");
            p = new Player(); p.W.Dummies.Clear(); p.Tap(Act.Attack, 1); p.Tap(Act.Dash, 1); Assert.AreEqual(0, p.P.DashT, "a dash pressed mid-blade does not start");
            p = new Player(); p.W.Dummies.Clear(); p.Tap(Act.Attack, 1); p.Run(9); Assert.IsTrue(p.P.Atk != null && !p.P.Swinging(), "in the recovery"); p.Tap(Act.Dash, 1);
            Assert.IsTrue(p.P.DashT > 0 && p.P.Atk == null, "a dash in the recovery goes, and ends the swing");
            p = new Player(); p.W.Dummies.Clear(); p.Tap(Act.Dash, 1); Assert.Greater(p.P.DashT, 0); p.Tap(Act.Attack, 1); Assert.IsNull(p.P.Atk, "no swing while dashing");
            p = new Player(); p.W.Dummies.Clear(); Assert.AreEqual(1, CountSwings(p, 40, 0, 8), "a press 0.13 s after the last is ignored (the buffer is 0.12 s)");
            p = new Player(); p.W.Dummies.Clear(); Assert.AreEqual(2, CountSwings(p, 40, 0, 14), "a press 4 steps before the cooldown ends is kept and swings as soon as it can");
            p = new Player(); p.W.Dummies.Clear(); Assert.AreEqual(2, CountSwings(p, 40, 0, 25), "a press after the cooldown swings at once");
        }

        [Test]
        public void TheStickUpWithTheSwordButtonIsAnUpStrike()
        {
            var p = new Player(); p.W.Dummies.Clear(); p.Down(Act.Up); p.Down(Act.Attack); p.Run(2); Assert.AreEqual(StrikeDir.Up, p.P.Atk.Dir);
        }

        [Test]
        public void ForwardCutsAlternateDirectionEachTime()
        {
            var p = new Player(); p.W.Dummies.Clear(); p.Tap(Act.Attack, 1); bool a = p.P.Atk.Alt; p.Run(25); p.Tap(Act.Attack, 1); Assert.AreNotEqual(a, p.P.Atk.Alt);
        }

        [Test]
        public void ADummyFlinchesAwayFromABlowAndSettlesBackWhereItStoodItIsNotSolid()
        {
            foreach (var (dir, axis, sgn) in new[] { (StrikeDir.Fwd, 'x', 1), (StrikeDir.Up, 'y', -1), (StrikeDir.Down, 'y', 1) })
            {
                var d = new Dummy(100, 0); d.Hit(dir, 1); double peak = 0;
                for (int i = 0; i < 12; i++) { d.Update(Mathx.Step); peak = System.Math.Max(peak, System.Math.Abs(axis == 'x' ? d.Ox : d.Oy)); }
                Assert.Greater(peak, 2, $"a {dir} blow knocks it {peak:0.0} u");
                for (int i = 0; i < 180; i++) d.Update(Mathx.Step);
                Assert.IsTrue(System.Math.Abs(d.Ox) < 0.05 && System.Math.Abs(d.Oy) < 0.05 && System.Math.Abs(d.Lean) < 0.01, "and it settles back"); Assert.AreEqual(100, d.X, "where it stood"); Assert.AreEqual(0, d.Flash); Assert.AreEqual(1, d.Hits);
                var e = new Dummy(100, 0); e.Hit(dir, 1); e.Update(Mathx.Step); Assert.IsTrue(sgn * (axis == 'x' ? e.Ox : e.Oy) > 0 || axis == 'x', "it moves away from the blow");
            }
            var p = new Player(); Lone(p, 30); p.Down(Act.Right); p.Run(40); Assert.Greater(p.P.Cx, 80, "he runs straight through a dummy");
        }

        // ---- the pogo and attack-to-reset ----
        // a scripted player over a post: strikes down whenever he is falling and within reach of its top
        static (int blows, bool grounded, List<double> risings) PogoChain(Player p, int maxSteps, int wantBlows)
        {
            int blows = 0; bool grounded = false; var risings = new List<double>(); int last = p.P.Strikes; double y0 = 0, minY = 0; bool rising = false;
            p.Down(Act.Down);
            for (int i = 0; i < maxSteps && blows < wantBlows; i++)
            {
                double above = -46 - p.P.Y;
                if (p.P.Vy > 0 && above < 20 && p.P.Atk == null && p.P.AtkCd <= 0 && p.P.Strikes == last) p.Tap(Act.Attack, 1); else p.Run(1);
                if (p.P.OnGround) grounded = true;
                if (p.P.Strikes != last) { if (rising) risings.Add(y0 - minY); blows++; last = p.P.Strikes; y0 = p.P.Y; minY = p.P.Y; rising = true; }
                else if (rising) minY = System.Math.Min(minY, p.P.Y);
            }
            p.Up(Act.Down); return (blows, grounded, risings);
        }

        [Test]
        public void ADownStrikeThatLandsBouncesHimUpAt560GivesBackTheAirDashAndTheBounceStartsAfterTheHitStop()
        {
            var p = new Player(); var d = Lone(p, 240, 0); p.Place(240, -62); p.P.CanDash = false; p.Down(Act.Down); p.Tap(Act.Attack, 1);
            int n = 0; while (d.Hits == 0 && n++ < 12) p.Run(1); p.Up(Act.Down);
            Assert.AreEqual(1, d.Hits, "the blow lands on the post below him"); Assert.AreEqual(-560, p.P.Vy, "he is thrown up at 560 u/s"); Assert.IsTrue(p.P.CanDash, "and the air dash is his again"); Assert.IsFalse(p.P.OnGround);
            double y0 = p.P.Y, minY = y0; int frozen = 0; for (int i = 0; i < 60; i++) { p.Run(1); if (i < 4 && p.P.Y == y0) frozen++; minY = System.Math.Min(minY, p.P.Y); }
            Assert.That(y0 - minY, Is.InRange(66.0, 76.0), "the bounce rises about 71 u"); Assert.AreEqual(4, frozen, "after the 4-step hit-stop");
        }

        [Test]
        public void ThePogoNeedsSomethingToHitOpenAirAndAForwardCutOnTheGroundDoNotBounceHim()
        {
            var p = new Player(); p.W.Dummies.Clear(); p.Place(0, -200); p.Down(Act.Down); p.Tap(Act.Attack, 1); p.Run(8); p.Up(Act.Down);
            Assert.IsTrue(p.P.Vy > 0 && p.P.Strikes == 0, "nothing hit, nothing bounces");
            p = new Player(); Lone(p, 50); p.Tap(Act.Attack, 1); p.Run(8); Assert.AreEqual(1, p.P.Strikes, "the cut lands"); Assert.IsTrue(p.P.OnGround && p.P.Vy >= 0, "but a cut on the ground is not a pogo");
        }

        [Test]
        public void HeCanChainThePogoStrikingDownEachTimeHeFallsBackKeepsHimBouncingOverThePost()
        {
            var p = new Player(); Lone(p, 240, 0); p.Place(240, -62); var r = PogoChain(p, 900, 6);
            Assert.GreaterOrEqual(r.blows, 6, "he lands six blows in a row"); Assert.IsFalse(r.grounded, "without touching the ground");
            Assert.GreaterOrEqual(r.risings.Count, 3); foreach (var h in r.risings) Assert.That(h, Is.InRange(64.0, 80.0), "each bounce rises about 71 u");
        }

        [Test]
        public void AnyBlowLandedInTheAirGivesBackTheAirDashAndNoneOfThemBouncesHim()
        {
            var p = new Player(); Lone(p, 50, -60, DummyKind.Hanging); p.Place(0, -60); p.P.CanDash = false; p.Tap(Act.Attack, 1); p.Run(6);
            Assert.AreEqual(1, p.P.Strikes, "the forward cut lands in the air"); Assert.IsTrue(p.P.CanDash, "and refills the air dash"); Assert.Greater(p.P.Vy, -100, "without a bounce");
            p = new Player(); Lone(p, 0, -110, DummyKind.Hanging); p.Place(0, -60); p.P.CanDash = false; p.Down(Act.Up); p.Tap(Act.Attack, 1); p.Up(Act.Up); p.Run(6);
            Assert.AreEqual(1, p.P.Strikes, "the up cut lands in the air"); Assert.IsTrue(p.P.CanDash); Assert.Greater(p.P.Vy, -100);
        }

        [Test]
        public void OnceABlowHasLandedHeCanDashOrTurnAtOnceBeforeItLandsHeCannot()
        {
            var p = new Player(); Lone(p, 50, -60, DummyKind.Hanging); p.Place(0, -60); p.Tap(Act.Attack, 1); p.Tap(Act.Dash, 1);
            Assert.AreEqual(0, p.P.DashT, "a dash pressed while the blade is still looking for its mark does not start");
            p = new Player(); Lone(p, 50, -60, DummyKind.Hanging); p.Place(0, -60); p.Tap(Act.Attack, 1); int n = 0; while (p.P.Strikes == 0 && n++ < 10) p.Run(1);
            Assert.AreEqual(1, p.P.Strikes, "the blow lands"); p.Tap(Act.Dash, 1); p.Run(5); Assert.IsTrue(p.P.DashT > 0 && p.P.Atk == null, "now he dashes as soon as the hit-stop lets go, and the swing is over");
            p = new Player(); Lone(p, 50, -60, DummyKind.Hanging); p.Place(0, -60); p.Tap(Act.Attack, 1); n = 0; while (p.P.Strikes == 0 && n++ < 10) p.Run(1);
            p.Down(Act.Left); p.Run(6); Assert.AreEqual(-1, p.P.Face, "and he can turn");
        }

        [Test]
        public void TheHangingStairBounceOffOneDummyAirDashAndStrikeTheNextAllWithoutTouchingTheGround()
        {
            var p = new Player(); p.W.Dummies = new List<Dummy> { new Dummy(-240, -60, DummyKind.Hanging), new Dummy(-310, -100, DummyKind.Hanging) }; p.Place(-240, -112);
            p.Down(Act.Down); p.Tap(Act.Attack, 1); p.Run(6); p.Up(Act.Down);
            Assert.AreEqual(1, p.W.Dummies[0].Hits, "the first is struck from above"); Assert.IsTrue(p.P.CanDash && p.P.Vy < -500, "he is bouncing, with his dash back");
            p.Run(4); p.Down(Act.Left); p.Tap(Act.Dash, 1); Assert.Greater(p.P.DashT, 0, "he can dash at once after the blow, mid-swing"); p.Up(Act.Left); p.Run(20);
            Assert.IsFalse(p.P.CanDash, "the air dash is spent crossing to the next one"); Assert.Less(p.P.Cx, -300, "he has crossed to the left"); Assert.IsFalse(p.P.OnGround, "and is still in the air");
        }

        [Test]
        public void TheWorldReportsAHitStopStepSoNothingDrawnFromItMovesThen()
        {
            var p = new Player(); var d = Lone(p, 40); p.Tap(Act.Attack, 1); int frozenSteps = 0, n = 0;
            while (n++ < 40) { p.Run(1); if (p.W.Frozen) frozenSteps++; }
            Assert.AreEqual(4, frozenSteps, "a blow holds the world still for 4 steps and the world says so");
            Assert.IsFalse(p.W.Frozen);
        }

        [Test]
        public void ThePogoBounceAndTheBlowAreMarkedSoTheLookCanStretchHim()
        {
            var p = new Player(); Lone(p, 240, 0); p.Place(240, -62); p.Down(Act.Down); p.Tap(Act.Attack, 1); int count = 0;
            for (int i = 0; i < 20; i++) { p.Run(1); if (p.P.JustPogoed) count++; }
            Assert.AreEqual(1, count);
        }
    }
}
