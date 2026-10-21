using System;
using System.Collections.Generic;
using Kaname.Game;
using Kaname.Ink;
using Kaname.Look;
using Kaname.Sim;
using NUnit.Framework;

namespace Kaname.Tests
{
    // Kaname's look: the same facts the JavaScript acceptance tests state about how he is drawn (the scarf never comes loose, drawing changes no state, nothing
    // on him is red or yellow, the outfit is black and the band a deep violet), driven by the pose reel exactly as the game drives it.
    public class LookTests
    {
        sealed class Rig
        {
            public readonly World World; public readonly HeroLook Look; public readonly PoseReel Reel = new PoseReel(); public readonly LookInput Li = new LookInput();
            public Rig(PoseKind kind)
            {
                var t = new Tuning(); World = new World(t); Look = new HeroLook(t);
                int guard = 0; while (Reel.Kind != kind && guard++ < 20) Reel.Next(World, Look);
                Look = new HeroLook(t);
            }
            public void Step()
            {
                Reel.BeforeStep(World, Look, Mathx.Step);
                World.Step(Mathx.Step, NoControls.Instance);
                HeroView.Fill(Li, World.Hero);
                Reel.Tell(Li);
                Look.Update(Li, Mathx.Step);
            }
            public InkCanvas Draw(double alpha = 1)
            {
                var c = new InkCanvas(); KanameArt.DrawKaname(c, Look, World.T, World.Hero.Cx, World.Hero.Y, alpha); return c;
            }
        }

        static IEnumerable<PoseKind> AllPoses() => (PoseKind[])Enum.GetValues(typeof(PoseKind));

        // a fingerprint of everything the look holds
        internal static string State(HeroLook l)
        {
            var v = l.Vis; var sb = new System.Text.StringBuilder();
            sb.Append($"{v.Run:R} {v.Air:R} {v.Rise:R} {v.Lean:R} {v.Sq:R} {v.Sqv:R} {v.Dash:R} {v.Sprint:R} {v.Atk:R} {v.Charge:R} {l.RunT:R} {l.T:R} {l.Blink:R} {l.ScarfTh:R} {l.Trail.Count} {l.Swing.P:R} {l.Swing.Fade:R}");
            sb.Append($" {l.Swing.Dir} {l.Swing.Alt} {l.Swing.T:R} {l.Swing.Live0:R} {l.Swing.Live1:R} {l.Swing.Total:R}");
            foreach (var p in l.Scarf) sb.Append($" {p.X:R} {p.Y:R} {p.Ox:R} {p.Oy:R}");
            return sb.ToString();
        }

        [Test]
        public void TheOutfitIsBlackAndTheBandADeepViolet()
        {
            Assert.Less(Palette.Inkc.R + Palette.Inkc.G + Palette.Inkc.B, 0.15f, "the hood and body are black");
            Assert.Less(Palette.Blk.R + Palette.Blk.G + Palette.Blk.B, 0.15f);
            Assert.Less(Palette.Band.R, 0.30f); Assert.Less(Palette.Band.G, 0.2f); Assert.Greater(Palette.Band.B, 0.3f);      // violet, and darker than the first #5b3cae
            Assert.Less(Palette.BandEdge.B, Palette.Band.B);
        }

        [TestCaseSource(nameof(AllPoses))]
        public void TheScarfIsTiedToTheBackOfHisHeadInEveryPose(PoseKind kind)
        {
            var r = new Rig(kind);
            for (int i = 0; i < 300; i++)
            {
                r.Step();
                var root = r.Look.Head(r.World.Hero.Cx, r.World.Hero.Y).Root;
                Assert.AreEqual(root.x, r.Look.Scarf[0].X, 1e-9, $"{kind} step {i}"); Assert.AreEqual(root.y, r.Look.Scarf[0].Y, 1e-9, $"{kind} step {i}");
            }
        }

        [TestCaseSource(nameof(AllPoses))]
        public void TheScarfKeepsItsLinksTheirLengthAndStaysAboveTheFloor(PoseKind kind)
        {
            var r = new Rig(kind);
            for (int i = 0; i < 300; i++)
            {
                r.Step();
                var s = r.Look.Scarf;
                for (int k = 1; k < s.Length; k++)
                {
                    double d = Math.Sqrt((s[k].X - s[k - 1].X) * (s[k].X - s[k - 1].X) + (s[k].Y - s[k - 1].Y) * (s[k].Y - s[k - 1].Y));
                    // (a link is held to 2.4 u, unless the floor has pushed its end up to the floor: lying down, the head's own band is below it)
                    bool onTheFloor = Math.Abs(s[k].Y - (r.Look.ShadowY - 0.6)) < 1e-9;
                    Assert.IsTrue(d <= r.World.K.ScarfSeg + 1e-6 || onTheFloor, $"{kind} step {i} link {k}: {d}");
                    Assert.LessOrEqual(s[k].Y, r.Look.ShadowY - 0.6 + 1e-9, $"{kind} step {i} link {k} below the floor");
                }
            }
        }

        [TestCaseSource(nameof(AllPoses))]
        public void DrawingChangesNoState(PoseKind kind)
        {
            var r = new Rig(kind);
            for (int i = 0; i < 90; i++) r.Step();
            string before = State(r.Look); double x = r.World.Hero.Cx, y = r.World.Hero.Y;
            r.Draw(0.5); r.Draw(1);
            Assert.AreEqual(before, State(r.Look)); Assert.AreEqual(x, r.World.Hero.Cx); Assert.AreEqual(y, r.World.Hero.Y);
        }

        [TestCaseSource(nameof(AllPoses))]
        public void EveryPoseDrawsFiniteGeometry(PoseKind kind)
        {
            var r = new Rig(kind);
            for (int i = 0; i < 120; i++)
            {
                r.Step();
                if (i % 7 != 0) continue;
                var c = r.Draw();
                Assert.Greater(c.Indices.Count, 300, $"{kind} step {i} drew almost nothing");
                foreach (var v in c.Verts) Assert.IsTrue(!double.IsNaN(v.X) && !double.IsNaN(v.Y) && !double.IsInfinity(v.X) && !double.IsInfinity(v.Y), $"{kind} step {i}");
                foreach (var col in c.Colors) Assert.IsTrue(col.A >= 0 && col.A <= 1.0001f && col.R >= 0 && col.R <= 1.0001f, $"{kind} step {i} colour");
            }
        }

        [TestCaseSource(nameof(AllPoses))]
        public void NothingOnKanameIsRedOrYellow(PoseKind kind)
        {
            var r = new Rig(kind);
            for (int i = 0; i < 100; i++)
            {
                r.Step();
                foreach (var col in r.Draw().Colors)
                {
                    if (col.A < 0.04f) continue;
                    bool red = col.R > 0.55f && col.G < 0.4f && col.B < 0.4f;
                    bool yellow = col.R > 0.6f && col.G > 0.5f && col.B < 0.45f;
                    Assert.IsFalse(red || yellow, $"{kind} step {i}: rgb({col.R:0.00},{col.G:0.00},{col.B:0.00}) is red or yellow (D010: those mean danger and parry)");
                }
            }
        }

        [Test]
        public void ALandingSquashesHimByHowHardHeFell()
        {
            var r = new Rig(PoseKind.Stand); r.World.Hero.Place(0, -200);
            bool landed = false; double squash = 0;
            for (int i = 0; i < 80 && !landed; i++) { r.Step(); if (r.World.Hero.JustLanded) { landed = true; squash = r.Look.Vis.Sq; } }
            Assert.IsTrue(landed); Assert.Greater(squash, 0.1, "a hard landing squashes him");
            for (int i = 0; i < 120; i++) r.Step();
            Assert.AreEqual(0, r.Look.Vis.Sq, 0.001, "and the squash springs back to rest");
        }

        [Test]
        public void RunningLeansHimInAndSprintingFurther()
        {
            var run = new Rig(PoseKind.Run); for (int i = 0; i < 60; i++) run.Step();
            var sprint = new Rig(PoseKind.Sprint); for (int i = 0; i < 60; i++) sprint.Step();
            Assert.That(run.Look.Vis.Lean, Is.InRange(0.10, 0.16)); Assert.Greater(sprint.Look.Vis.Lean, run.Look.Vis.Lean + 0.1);
            Assert.AreEqual(1, run.Look.Vis.Run, 1e-6); Assert.Greater(sprint.Look.Vis.Sprint, 0.9);
        }

        [Test]
        public void ADashStretchesHimAndLeavesSpeedLines()
        {
            var r = new Rig(PoseKind.Dash);
            for (int i = 0; i < 8; i++) r.Step();
            Assert.Greater(r.Look.Vis.Dash, 0.95); Assert.Greater(r.Look.Trail.Count, 3);
            var c = r.Draw(); Assert.Greater(c.Indices.Count, 500);
        }

        [Test]
        public void TheBladeShowsOnlyInACutOrAGuard()
        {
            var stand = new Rig(PoseKind.Stand); for (int i = 0; i < 30; i++) stand.Step();
            Assert.AreEqual(0, stand.Look.Vis.Atk, 1e-9);
            var cut = new Rig(PoseKind.Sword); for (int i = 0; i < 7; i++) cut.Step();
            Assert.Greater(cut.Look.Vis.Atk, 0.5);
            Assert.Greater(cut.Draw().Indices.Count, stand.Draw().Indices.Count + 200, "a cut draws the blade, its aura and its trail");
            var guard = new Rig(PoseKind.Guard); for (int i = 0; i < 6; i++) guard.Step();
            Assert.AreEqual(SwingDir.Guard, guard.Look.Swing.Dir); Assert.Greater(guard.Look.Vis.Atk, 0.5);
        }

        [Test]
        public void TheSwingsHaveTheDesignedArcs()
        {
            // a forward, up or down cut: the pivot and the five angles (cocked, enters the box, first live step, out, end), in the world's frame; the trail runs N to O (D123, revision 1)
            void Arc(string what, HeroLook.Arc a, double px, double py, double c, double n, double i, double o, double e)
            {
                Assert.AreEqual(px, a.Px, what); Assert.AreEqual(py, a.Py, what); Assert.AreEqual(45, a.R, what);
                Assert.AreEqual(c, a.C, what + " C"); Assert.AreEqual(n, a.N, what + " N"); Assert.AreEqual(i, a.I, what + " I"); Assert.AreEqual(o, a.O, what + " O"); Assert.AreEqual(e, a.E, what + " E");
            }
            Arc("forward, high to low", HeroLook.Swings(SwingDir.Fwd), 11, -15, -1.50, -0.68, -0.22, 0.19, 0.20);
            Arc("forward, low to high", HeroLook.Swings(SwingDir.Fwd, true), 11, -15, 0.09, 0.09, -0.30, -0.76, -1.05);
            Arc("up", HeroLook.Swings(SwingDir.Up), 13, -28, -2.85, -2.60, -1.76, -1.17, -1.00);
            Arc("down", HeroLook.Swings(SwingDir.Down), 12, -10, 0.80, 1.20, 1.75, 2.45, 2.60);
            Assert.AreEqual(0.08, HeroLook.SweepTime, "the sweep takes 0.08 s from the box going live");
            foreach (var (dir, alt) in new[] { (SwingDir.Fwd, false), (SwingDir.Fwd, true), (SwingDir.Up, false), (SwingDir.Down, false) })
            {
                var a = HeroLook.Swings(dir, alt); double d = Math.Sign(a.O - a.I);
                Assert.GreaterOrEqual(d * (a.N - a.C), 0, $"{dir}: N lies on the wind-up, at or past the cock"); Assert.LessOrEqual(d * (a.N - a.I), 0, $"{dir}: and before I, so the wind-up passes it");
                Assert.Greater(d * (a.O - a.I), 0, $"{dir}: the sweep runs on from I to O"); Assert.GreaterOrEqual(d * (a.E - a.O), 0, $"{dir}: and the follow-through does not turn back");
            }
            // the draw and the guard are as they were: one arc, eased over the draw's own time
            var dr = HeroLook.Swings(SwingDir.Draw); Assert.AreEqual(-0.5, dr.A0); Assert.AreEqual(0.2, dr.A1); Assert.AreEqual(14, dr.Px); Assert.AreEqual(-15, dr.Py); Assert.AreEqual(45, dr.R);
            var gd = HeroLook.Swings(SwingDir.Guard); Assert.AreEqual(-1.1, gd.A0); Assert.AreEqual(-1.1, gd.A1); Assert.AreEqual(14, gd.Px); Assert.AreEqual(-18, gd.Py);
            Assert.AreEqual((-0.68, 0.19), HeroLook.SwingAngles(new SwingState { Dir = SwingDir.Fwd }), "the trail's span of the high-to-low cut: N to O");
            Assert.AreEqual((0.09, -0.76), HeroLook.SwingAngles(new SwingState { Dir = SwingDir.Fwd, Alt = true }), "low to high is an arc of its own, running the other way");
            Assert.AreEqual((-0.5, 0.2), HeroLook.SwingAngles(new SwingState { Dir = SwingDir.Draw }));
            Assert.AreEqual(0, HeroLook.SwingEase(0)); Assert.AreEqual(1, HeroLook.SwingEase(1), 1e-12);
        }

        [Test]
        public void ACutFollowsFourPhasesReadFromTheSwingsOwnTimes()
        {
            foreach (var (dir, alt) in new[] { (SwingDir.Fwd, false), (SwingDir.Fwd, true), (SwingDir.Up, false), (SwingDir.Down, false) })
            {
                var g = HeroLook.Swings(dir, alt); var sw = new SwingState { Dir = dir, Alt = alt, Live0 = 0.03, Live1 = 0.14, Total = 0.2 };
                double At(double t) { sw.T = t; return HeroLook.SwingAngle(sw); }
                string w = $"{dir}{(alt ? " alt" : "")}";
                Assert.AreEqual(g.C, At(0), 1e-12, w + ": cocked at the press");
                Assert.AreEqual(g.I, At(0.03), 1e-12, w + ": at I the moment the box goes live");
                Assert.AreEqual(g.O, At(0.11), 1e-12, w + ": at O when the sweep is done (0.08 s later)");
                Assert.AreEqual(g.O, At(0.125), 1e-12, w + ": held at O while the blade is live"); Assert.AreEqual(g.O, At(0.139), 1e-12, w + ": still");
                Assert.AreEqual(g.O, At(0.14), 1e-12, w + ": the follow-through starts from O");
                Assert.AreEqual(g.E, At(0.2), 1e-12, w + ": and ends at E when the swing does");
                Assert.AreEqual(g.C + (g.I - g.C) * 0.25, At(0.015), 1e-12, w + ": the wind-up accelerates in (u squared)");
                Assert.AreEqual(g.I + (g.O - g.I) * 0.75, At(0.03 + 0.04), 1e-12, w + ": and the sweep eases out: three quarters done in half the time");
                double dirS = Math.Sign(g.O - g.I), last = At(0.03);
                for (double t = 0.03; t <= 0.2 + 1e-9; t += 0.001) { double a = At(t); Assert.GreaterOrEqual(dirS * (a - last), -1e-12, $"{w}: never turns back at t={t:0.000}"); last = a; }
            }
            // the draw and the guard do not read the swing's times: they keep the old eased arc
            var d = new SwingState { Dir = SwingDir.Draw, P = 0.5 }; Assert.AreEqual(-0.5 + 0.7 * HeroLook.SwingEase(0.5), HeroLook.SwingAngle(d), 1e-12);
        }

        [Test]
        public void ALeaningCutIsPinnedToTheWorldTheHandMovesButTheBladeDoesNotTilt()
        {
            double Angle(SwingDir dir, double lean)
            {
                var l = new HeroLook(new Tuning()); l.Vis.Atk = 1; l.Vis.Lean = lean; l.In.Y = -300;       // in the air: no surface turns the blade back (R3), so this isolates the lean
                l.Swing.Dir = dir; l.Swing.T = 0.1; l.Swing.Live0 = 0.03; l.Swing.Live1 = 0.14; l.Swing.Total = 0.2; l.Swing.P = 0.5;
                return l.Place().Ang + lean;                                                  // the blade's angle in the world: the body's angle, and the body's lean
            }
            foreach (var dir in new[] { SwingDir.Fwd, SwingDir.Up, SwingDir.Down })
                Assert.AreEqual(Angle(dir, 0), Angle(dir, 0.43), 1e-12, $"{dir}: leaning does not tilt the cut");
            Assert.AreNotEqual(Angle(SwingDir.Draw, 0), Angle(SwingDir.Draw, 0.43), "the draw keeps the body's frame, as it was");
        }

        [Test]
        public void TheHeadFrameAndThePoseAgreeOnTheHeadsHeight()
        {
            var r = new Rig(PoseKind.Sprint); for (int i = 0; i < 40; i++) r.Step();
            Assert.AreEqual(r.Look.Head(0, 0).Hy, r.Look.Place().Hy, 1e-12);
        }

        // the pipeline of the running game, one step: the reel drives him, the simulation steps, the look is told, the look steps
        static void GameStep(World w, HeroLook look, PoseReel reel, LookInput li, IControls controls, bool jumpKick = true)
        {
            reel.BeforeStep(w, look, Mathx.Step);
            w.Step(Mathx.Step, controls);
            if (jumpKick && w.Hero.JustJumped) look.Kick(-0.28);
            HeroView.Fill(li, w.Hero);
            reel.Tell(li);
            look.Update(li, Mathx.Step);
        }

        [Test]
        public void TurningThePoseReelOffLeavesNothingBehind()
        {
            var t = new Tuning(); var w = new World(t); var look = new HeroLook(t); var reel = new PoseReel(); var li = new LookInput();
            // cycle through every pose, long enough that each one is in the middle of its loop when the next key is pressed (down: lying; guard; charge...)
            for (int pose = 0; pose < PoseReel.Names.Length - 1; pose++)
            {
                reel.Next(w, look);
                for (int i = 0; i < 55; i++) GameStep(w, look, reel, li, NoControls.Instance);
            }
            Assert.AreEqual(PoseKind.Down, reel.Kind);
            reel.Next(w, look);                                                       // back to off
            Assert.AreEqual(PoseKind.Off, reel.Kind);
            var c = new ControlState { Push = 1 }; c.SetHeld(Act.Right, true);
            for (int i = 0; i < 90; i++) { GameStep(w, look, reel, li, c); c.EndStep(); }
            Assert.AreEqual(0, li.DeadT); Assert.AreEqual(0, li.Invuln); Assert.AreEqual(0, li.ParryT); Assert.AreEqual(0, li.Charge); Assert.AreEqual(0, li.DashT);
            Assert.IsNull(li.Atk); Assert.IsFalse(li.TrailActive);
            Assert.AreEqual(1.0, look.Vis.Run, 1e-6, "he is running, on his feet");
            Assert.That(look.Vis.Lean, Is.InRange(0.10, 0.16), "leaning into the run, not lying down");
            Assert.AreEqual(0, look.Vis.Atk, 1e-6); Assert.AreEqual(0, look.Vis.Charge, 1e-6);
            Assert.IsFalse(w.Hero.Scripted, "the player's controls run him again");
            Assert.AreEqual(240, w.Hero.Vx, 0.01);
        }

        [Test]
        public void TheBlinkIsRandomButSeededSoTheTestsAreSteady()
        {
            var a = new Rig(PoseKind.Stand); var b = new Rig(PoseKind.Stand);
            for (int i = 0; i < 600; i++) { a.Step(); b.Step(); }
            Assert.AreEqual(State(a.Look), State(b.Look));
        }
    }
}
