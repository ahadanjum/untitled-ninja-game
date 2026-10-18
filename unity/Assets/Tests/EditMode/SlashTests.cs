using System;
using System.Collections.Generic;
using Kaname.Game;
using Kaname.Ink;
using Kaname.Look;
using Kaname.Sim;
using NUnit.Framework;
using Arc = Kaname.Look.HeroLook.Arc;

namespace Kaname.Tests
{
    // Task 1 of the polish milestone (D123, revision 1): what is DRAWN of a forward, up or down cut lies over what its blow can really hit, the picture a hit freezes
    // has the blade at the target, and nothing is drawn under the floor. Every case drives the real World with scripted presses and steps the look exactly as
    // GameDriver.Step does (the kicks, Fill, Update, and nothing on a frozen step), then draws every step on an InkCanvas and reads the marks the art leaves (the
    // blade's axis from the guard to the tip, the trail's two ends): the numbers measured are the ones on the screen. Cases: forward (both alternations), up,
    // standing, running and sprinting, down in the air (over the post and in open air), facing right and left, a whiff and hits at different reaches (a hit
    // holds the world still for 4 steps, and since the box goes live on step 2 and a dummy in it is struck at once, what it freezes is step 2's picture).
    public class SlashTests
    {
        public enum Move { Stand, Run, Sprint }

        public sealed class Case
        {
            public string Name; public SwingDir Dir; public bool Alt, Hit, Air; public Move Move; public int Face; public double Offset;      // Offset: how far the struck dummy's middle is from him when the blow lands
            public override string ToString() => Name;
        }

        // what the screen showed after one world step of a cut (step 0 is the step the press was made on)
        sealed class Frame
        {
            public int K; public bool Frozen; public Box? Box; public Dictionary<string, V2> Marks;
            public double Rx, Ry, Lean, Sq, Dash; public int Face;
        }

        sealed class Cut { public List<Frame> Frames; public Box? Target; }

        static IEnumerable<Case> Cases()
        {
            foreach (int face in new[] { 1, -1 })
            {
                string f = face > 0 ? "right" : "left";
                foreach (var move in new[] { Move.Stand, Move.Run, Move.Sprint })
                {
                    string m = move.ToString().ToLower();
                    foreach (bool alt in new[] { false, true })
                    {
                        string n = $"forward {(alt ? "low-to-high" : "high-to-low")}, {m}, facing {f}";
                        yield return new Case { Name = n + ", whiff", Dir = SwingDir.Fwd, Alt = alt, Move = move, Face = face };
                        foreach (double reach in new[] { 30.0, 60.0 }) yield return new Case { Name = $"{n}, hit a post {reach:0} u away", Dir = SwingDir.Fwd, Alt = alt, Hit = true, Move = move, Face = face, Offset = reach };
                    }
                    yield return new Case { Name = $"up, {m}, facing {f}, whiff", Dir = SwingDir.Up, Move = move, Face = face };
                    foreach (double dx in new[] { 0.0, 20.0 }) yield return new Case { Name = $"up, {m}, facing {f}, hit a dummy {dx:0} u ahead overhead", Dir = SwingDir.Up, Hit = true, Move = move, Face = face, Offset = dx };
                }
                yield return new Case { Name = $"down in the air over the post, facing {f}", Dir = SwingDir.Down, Hit = true, Air = true, Face = face };
                yield return new Case { Name = $"down in open air, facing {f}", Dir = SwingDir.Down, Hit = false, Air = true, Face = face };
            }
        }

        static IEnumerable<Case> OnTheGround() { foreach (var c in Cases()) if (!c.Air) yield return c; }       // the forward and up cuts, on the floor
        static IEnumerable<Case> Hits() { foreach (var c in Cases()) if (c.Hit) yield return c; }

        // The pipeline of the running game with no pose reel: the world steps, and unless it is held still (hit-stop) the look gets its kicks, what the body did, and
        // its own step. Then the frame is drawn, as the game draws it every frame.
        sealed class Rig
        {
            public readonly World W; public readonly HeroLook Look; public readonly LookInput Li = new LookInput(); public readonly ControlState C = new ControlState(); public readonly InkCanvas Canvas = new InkCanvas();
            public Rig() { var t = new Tuning(); W = new World(t); Look = new HeroLook(t); W.Dummies = new List<Dummy>(); }
            public Hero H => W.Hero;
            public void Step()
            {
                W.Step(Mathx.Step, C);
                if (!W.Frozen)
                {
                    if (H.JustJumped) Look.Kick(-0.28);
                    if (H.JustDashed) Look.Kick(-0.12);
                    if (H.JustPogoed) Look.Kick(-0.25);
                    HeroView.Fill(Li, H); Look.Update(Li, Mathx.Step);
                }
                C.EndStep();
            }
            public InkCanvas Draw(double alpha = 1) { Canvas.Clear(); KanameArt.DrawKaname(Canvas, Look, W.T, H.Cx, H.Y, alpha); return Canvas; }
        }

        const int Steps = 26;                                                                                    // from the press to well after the blade has faded
        static readonly Dictionary<string, Cut> Played = new Dictionary<string, Cut>();                        // a case is a pure function of itself: play it once for all the tests

        static Cut Play(Case c)
        {
            if (Played.TryGetValue(c.Name, out var done)) return done;
            var r = new Rig(); var h = r.H; var frames = new List<Frame>(); Dummy target = null;
            if (c.Air) { if (c.Hit) h.Place(240, -62, 0, 0, c.Face); else h.Place(0, -400, 0, 0, c.Face); }
            else h.Place(0, 0, 0, 0, c.Face);
            r.W.Cam.X = r.W.CamPrev = h.Cx;
            double speed = c.Move == Move.Sprint ? 336 : c.Move == Move.Run ? 240 : 0;
            if (speed > 0) { r.C.Push = c.Face; r.C.SetHeld(c.Face > 0 ? Act.Right : Act.Left, true); }
            if (c.Move == Move.Sprint) r.C.SetHeld(Act.Dash, true);                                              // hold dash and a direction: a sprint, with no dash burst
            for (int i = 0; i < (c.Air ? 6 : 40); i++) r.Step();
            if (speed > 0) Assert.AreEqual(speed, Math.Abs(h.Vx), 0.5, $"{c.Name}: he is at full speed when he cuts");
            if (c.Hit)
            {
                double x = h.Cx + c.Face * (c.Offset + speed * 3 / 60);                                          // (he moves on for the press step and the next two, until the box goes live and the blow lands)
                target = c.Dir == SwingDir.Up ? new Dummy(x, -60, DummyKind.Hanging) : c.Dir == SwingDir.Down ? new Dummy(h.Cx, 0) : new Dummy(x, 0);
                r.W.Dummies = new List<Dummy> { target };
            }

            if (c.Dir == SwingDir.Up) r.C.SetHeld(Act.Up, true);
            if (c.Dir == SwingDir.Down) r.C.SetHeld(Act.Down, true);
            if (c.Dir == SwingDir.Fwd) h.SlashAlt = !c.Alt;                                                      // the press flips it: this is the alternation the cut will have
            for (int k = 0; k <= Steps; k++)
            {
                if (k == 0) { r.C.SetHeld(Act.Attack, true); r.C.Press(Act.Attack); } else if (k == 1) r.C.SetHeld(Act.Attack, false);
                r.Step();
                if (k == 0)
                {
                    Assert.IsNotNull(h.Atk, $"{c.Name}: the press starts a swing");
                    Assert.AreEqual(c.Dir == SwingDir.Fwd ? StrikeDir.Fwd : c.Dir == SwingDir.Up ? StrikeDir.Up : StrikeDir.Down, h.Atk.Dir, $"{c.Name}: the swing's direction");
                    if (c.Dir == SwingDir.Fwd) Assert.AreEqual(c.Alt, h.Atk.Alt, $"{c.Name}: the swing's alternation");
                }
                var canvas = r.Draw(); var v = r.Look.Vis;
                frames.Add(new Frame
                {
                    K = k, Frozen = r.W.Frozen, Box = h.Blow()?.Box, Marks = new Dictionary<string, V2>(canvas.Marks), Rx = h.Cx, Ry = h.Y,
                    Lean = v.Lean, Sq = v.Sq, Dash = v.Dash, Face = r.Look.In.Face,
                });
            }
            Assert.AreEqual(c.Hit ? 1 : 0, h.Strikes, $"{c.Name}: {(c.Hit ? "the blow lands" : "nothing is struck")}");
            if (c.Hit) Assert.AreEqual(1, target.Hits, $"{c.Name}: the dummy was struck, once (so a check against it is not empty)");
            int first = frames.FindIndex(f => f.Box.HasValue);
            Assert.AreEqual(2, first, $"{c.Name}: the box goes live on step 2 (0.03 s after the press)");
            if (c.Hit) Assert.AreEqual(3, frames.FindIndex(f => f.Frozen), $"{c.Name}: the blow lands on step 2, so the world is held still from step 3");
            return Played[c.Name] = new Cut { Frames = frames, Target = target?.Box() };
        }

        static bool Inside(V2 p, Box b, double grow) => p.X >= b.X - grow && p.X <= b.X + b.W + grow && p.Y >= b.Y - grow && p.Y <= b.Y + b.H + grow;
        static double Outside(V2 p, Box b)        // how far a point is from a box: 0 inside
        {
            double dx = Math.Max(0, Math.Max(b.X - p.X, p.X - (b.X + b.W))), dy = Math.Max(0, Math.Max(b.Y - p.Y, p.Y - (b.Y + b.H)));
            return Math.Sqrt(dx * dx + dy * dy);
        }
        static string At(V2 p) => $"({p.X:0.0}, {p.Y:0.0})";
        static string Show(Box b) => $"x {b.X:0.0}..{b.X + b.W:0.0}, y {b.Y:0.0}..{b.Y + b.H:0.0}";

        // a world point back into the body's own frame, undoing what DrawKaname does to it: the translation, the squash and stretch and the mirror, then the lean
        static (double x, double y) ToBody(Frame f, V2 p)
        {
            double sx = f.Face * (1 + f.Sq * 0.4 + f.Dash * HeroLook.DashStretch), sy = 1 - f.Sq * 0.4 - f.Dash * HeroLook.DashSquash;
            double ux = (p.X - f.Rx) / sx, uy = (p.Y - f.Ry) / sy, c = Math.Cos(f.Lean), s = Math.Sin(f.Lean);
            return (ux * c + uy * s, -ux * s + uy * c);
        }

        // the angle of a world point about the swing's pivot, in the swing's own frame, and how far it is from the pivot: the lean put back, since a cut's arc is the blade's angle in the world
        static (double angle, double radius) AboutThePivot(Frame f, Arc arc, V2 p)
        {
            var (bx, by) = ToBody(f, p); double dx = bx - arc.Px, dy = by - arc.Py;
            return (Math.Atan2(dy, dx) + f.Lean, Math.Sqrt(dx * dx + dy * dy));
        }

        [TestCaseSource(nameof(Cases))]
        public void TheVisibleDamagingSweepLiesOverTheLiveHitbox(Case c)
        {
            var frames = Play(c).Frames; int live = 0;
            foreach (var f in frames)
            {
                if (!f.Box.HasValue) continue;
                live++; var b = f.Box.Value; string box = Show(b);
                Assert.IsTrue(f.Marks.ContainsKey("blade.4") && f.Marks.ContainsKey("blade.2"), $"{c.Name} step {f.K}: the blade is live but is not drawn");
                var m2 = f.Marks["blade.2"]; var m3 = f.Marks["blade.3"]; var tip = f.Marks["blade.4"];
                Assert.IsTrue(Inside(m2, b, 3) || Inside(m3, b, 3) || Inside(tip, b, 3),
                    $"{c.Name} step {f.K}: no part of the blade's outer half is inside the box grown by 3 u ({box}; blade.2 {At(m2)}, blade.3 {At(m3)}, tip {At(tip)})");
                Assert.LessOrEqual(Outside(tip, b), 8, $"{c.Name} step {f.K}: the tip {At(tip)} is {Outside(tip, b):0.0} u outside the box ({box})");
            }
            Assert.AreEqual(c.Hit ? 11 : 7, live, $"{c.Name}: live steps (7, and 4 more held still by a hit)");
        }

        [TestCaseSource(nameof(OnTheGround))]
        public void NothingIsDrawnUnderTheFloor(Case c)
        {
            foreach (var f in Play(c).Frames)                                                           // every step from the press to the end of the fade, the faint ones too
                foreach (var kv in f.Marks)
                    Assert.LessOrEqual(kv.Value.Y, f.Ry + 0.5, $"{c.Name} step {f.K}: {kv.Key} {At(kv.Value)} is {kv.Value.Y - f.Ry:0.0} u under his feet (y {f.Ry:0.0})");
        }

        [TestCaseSource(nameof(Cases))]
        public void TheTrailCoversOnlyTheLiveSweepAndStaysWhereTheCutHappened(Case c)
        {
            var arc = HeroLook.Swings(c.Dir, c.Alt); double dir = Math.Sign(arc.O - arc.I), lag = -dir * Math.Atan2(HeroLook.BladeLen * 0.1, 5 + HeroLook.BladeLen); int trails = 0;
            foreach (var f in Play(c).Frames)
            {
                bool trail = f.Marks.ContainsKey("trail.from") || f.Marks.ContainsKey("trail.to");
                if (!f.Box.HasValue && f.K < 4) Assert.IsFalse(trail, $"{c.Name} step {f.K}: there is a trail before the box has gone live");
                if (f.K == 2) Assert.IsTrue(trail, $"{c.Name} step 2: from the first live step the trail covers what the blade has crossed");
                if (!trail) continue;
                trails++;
                var (from, rFrom) = AboutThePivot(f, arc, f.Marks["trail.from"]); var (to, rTo) = AboutThePivot(f, arc, f.Marks["trail.to"]);
                Assert.AreEqual(arc.N + lag, from, 1e-6, $"{c.Name} step {f.K}: the trail begins where the cut enters the box (N {arc.N:0.000}), not at {from:0.000}");
                Assert.AreEqual(arc.R, rFrom, 1e-6, $"{c.Name} step {f.K}: the trail is drawn about the swing's pivot, not the hand ({rFrom:0.00} u from it, its radius {arc.R})"); Assert.AreEqual(arc.R, rTo, 1e-6, $"{c.Name} step {f.K}: its far end too");
                Assert.LessOrEqual(dir * (to - arc.O), 1e-6, $"{c.Name} step {f.K}: the trail's end {to:0.000} has passed O ({arc.O:0.000}) into the follow-through");
                Assert.GreaterOrEqual(dir * (to - (arc.N + lag)), -1e-6, $"{c.Name} step {f.K}: the trail's end {to:0.000} lies before N ({arc.N:0.000}), over the wind-up");
            }
            Assert.Greater(trails, 8, $"{c.Name}: the cut leaves a trail (on {trails} steps)");
        }

        [TestCaseSource(nameof(Cases))]
        public void TheTrailFromNToOCoversEightyPercentOfTheBox(Case c)
        {
            var frames = Play(c).Frames; var f = frames[frames.FindLastIndex(x => x.Box.HasValue)];                    // the last live step: the sweep is done, the trail has reached O
            var b = f.Box.Value; string axis = c.Dir == SwingDir.Fwd ? "height" : "width";
            Assert.IsTrue(f.Marks.ContainsKey("trail.from") && f.Marks.ContainsKey("trail.to"), $"{c.Name} step {f.K}: there is a trail on the last live step");
            double a = c.Dir == SwingDir.Fwd ? Mathx.Clamp(f.Marks["trail.from"].Y, b.Y, b.Y + b.H) : Mathx.Clamp(f.Marks["trail.from"].X, b.X, b.X + b.W);        // the trail's ends at the tip's radius, clamped to the box
            double z = c.Dir == SwingDir.Fwd ? Mathx.Clamp(f.Marks["trail.to"].Y, b.Y, b.Y + b.H) : Mathx.Clamp(f.Marks["trail.to"].X, b.X, b.X + b.W);
            double size = c.Dir == SwingDir.Fwd ? b.H : b.W;
            Assert.GreaterOrEqual(Math.Abs(z - a), 0.8 * size, $"{c.Name} step {f.K}: the trail from N to O, clamped to the box, covers {Math.Abs(z - a):0.0} u of its {axis} of {size:0.0} u (needs 80 %: {0.8 * size:0.0} u; from {At(f.Marks["trail.from"])} to {At(f.Marks["trail.to"])}; {Show(b)})");
        }

        [TestCaseSource(nameof(Cases))]
        public void TheBladeIsMidBoxOnTheFirstLiveStepWhichIsTheFrameAHitFreezes(Case c)
        {
            var cut = Play(c); var f2 = cut.Frames[2]; var b = f2.Box.Value; var tip = f2.Marks["blade.4"];
            double along = c.Dir == SwingDir.Fwd ? (tip.Y - b.Y) / b.H : (tip.X - b.X) / b.W; string axis = c.Dir == SwingDir.Fwd ? "height" : "width";
            Assert.That(along, Is.InRange(0.25, 0.75), $"{c.Name} step 2: the tip {At(tip)} is {along * 100:0} % of the way across the box's {axis} ({Show(b)}), not between 25 and 75 %");
            if (!c.Hit) return;
            foreach (var f in cut.Frames)                                                              // the picture the hit freezes: step 2, held through the hit-stop
            {
                if (f.K != 2 && !f.Frozen) continue;
                double best = double.MaxValue; string near = "";
                foreach (var k in new[] { "blade.2", "blade.3", "blade.4" }) { double d = Outside(f.Marks[k], cut.Target.Value); if (d < best) { best = d; near = $"{k} {At(f.Marks[k])}"; } }
                Assert.LessOrEqual(best, 4, $"{c.Name} step {f.K}{(f.Frozen ? " (held still by the hit)" : "")}: the blade is {best:0.0} u from the struck dummy ({Show(cut.Target.Value)}); nearest {near}, needs 4 u");
            }
        }

        [Test]
        public void DrawingWithMarksChangesNothingInTheSimulationOrTheLook()
        {
            foreach (var dir in new[] { SwingDir.Fwd, SwingDir.Up })
            {
                var r = new Rig(); var h = r.H; h.Place(0, 0);
                r.Step(); if (dir == SwingDir.Up) r.C.SetHeld(Act.Up, true);
                r.C.SetHeld(Act.Attack, true); r.C.Press(Act.Attack); r.Step(); r.C.SetHeld(Act.Attack, false);
                for (int i = 0; i < 5; i++) r.Step();                                                    // in the middle of the cut
                string look = LookTests.State(r.Look); double x = h.Cx, y = h.Y, t = h.Atk.T, w = r.W.T;
                var first = r.Draw(0.5); Assert.IsTrue(first.Marks.ContainsKey("blade.4"), $"{dir}: the cut is drawn, with its marks");
                var verts = new List<V2>(first.Verts); var cols = new List<Rgba>(first.Colors);
                r.Draw(1); var again = r.Draw(0.5);
                Assert.AreEqual(look, LookTests.State(r.Look), $"{dir}: the look is as it was"); Assert.AreEqual(x, h.Cx); Assert.AreEqual(y, h.Y); Assert.AreEqual(t, h.Atk.T); Assert.AreEqual(w, r.W.T);
                Assert.AreEqual(verts.Count, again.Verts.Count, $"{dir}: the same picture twice"); Assert.AreEqual(cols.Count, again.Colors.Count);
                for (int i = 0; i < verts.Count; i++) { Assert.AreEqual(verts[i].X, again.Verts[i].X, 0, $"{dir} vertex {i}"); Assert.AreEqual(verts[i].Y, again.Verts[i].Y, 0, $"{dir} vertex {i}"); }
            }
        }

        [Test]
        public void TheForwardCutsAlternateAndEachHasItsOwnArcNotOneArcReversed()
        {
            var hl = HeroLook.Swings(SwingDir.Fwd); var lh = HeroLook.Swings(SwingDir.Fwd, true);
            Assert.Less(hl.I, hl.O, "high to low runs down"); Assert.Greater(lh.I, lh.O, "low to high runs up");
            Assert.AreNotEqual(hl.I, lh.O, "and it is not the first arc turned round"); Assert.AreNotEqual(hl.O, lh.I);
            var r = new Rig(); r.H.Place(0, 0); r.Step(); r.C.SetHeld(Act.Attack, true); r.C.Press(Act.Attack); r.Step(); bool first = r.H.Atk.Alt;
            r.C.SetHeld(Act.Attack, false); for (int i = 0; i < 25; i++) r.Step();
            r.C.SetHeld(Act.Attack, true); r.C.Press(Act.Attack); r.Step();
            Assert.AreNotEqual(first, r.H.Atk.Alt, "the simulation alternates the cuts, and the look draws each its own way");
        }
    }
}
