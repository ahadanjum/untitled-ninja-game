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
    // has the blade at the target, and nothing is drawn under the floor. Task 2 (D123, D127): the slash has two layers, an inner crescent that covers the whole damaging
    // sweep from the first live step and a soft afterimage that only follows the blade, both only while the blade can hit, both inside the tip's path, and the frozen
    // hit frame shows the blow reaching the target (measured on the rendered triangles). Every case drives the real World with scripted presses and steps the look
    // exactly as GameDriver.Step does (the kicks, Fill, Update, and nothing on a frozen step), then draws every step on an InkCanvas and reads the marks and the
    // triangle groups the art leaves: the numbers measured are the ones on the screen. Cases: forward (both alternations), up, standing, running and sprinting,
    // down in the air (over the post and in open air), facing right and left, a whiff and hits at different reaches (a hit holds the world still for 4 steps, and
    // since the box goes live on step 2 and a dummy in it is struck at once, what it freezes is step 2's picture), a dash that ends the swing after a hit, and a
    // hit that comes on the last live step.
    public class SlashTests
    {
        public enum Move { Stand, Run, Sprint }

        public sealed class Case
        {
            public string Name; public SwingDir Dir; public bool Alt, Hit, Air, EarlyDash, Late; public Move Move; public int Face; public double Offset;      // Offset: how far the struck dummy's middle is from him when the blow lands (Late: from him when the cut begins)
            public override string ToString() => Name;
        }

        // a drawn triangle of one of the canvas's groups: its corners in the world and its largest vertex alpha
        struct Tri { public V2 A, B, C; public float Alpha; }

        // what the screen showed after one world step of a cut (step 0 is the step the press was made on)
        sealed class Frame
        {
            public int K; public bool Frozen; public Box? Box; public Dictionary<string, V2> Marks; public Dictionary<string, List<Tri>> Tris; public SwingState Sw;
            public double Rx, Ry, Lean, Sq, Dash; public int Face;
            public bool Live => Box.HasValue;                                                                  // the blade can hit (the simulation's own word)
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

        // a dash after the blow ends the swing early; a post that comes into the box only on step 8, the last live step, so the hit-stop holds a late frame of the hold
        static IEnumerable<Case> Special()
        {
            yield return new Case { Name = "forward high-to-low, stand, facing right, hit a post 40 u away, then a dash ends the swing", Dir = SwingDir.Fwd, Hit = true, EarlyDash = true, Move = Move.Stand, Face = 1, Offset = 40 };
            yield return new Case { Name = "forward low-to-high, stand, facing left, hit a post 40 u away, then a dash ends the swing", Dir = SwingDir.Fwd, Alt = true, Hit = true, EarlyDash = true, Move = Move.Stand, Face = -1, Offset = 40 };
            yield return new Case { Name = "forward high-to-low, run, facing right, hit a post that comes into the box on step 8", Dir = SwingDir.Fwd, Hit = true, Late = true, Move = Move.Run, Face = 1, Offset = 101 };
            yield return new Case { Name = "up, run, facing left, hit a dummy that comes into the box on step 8", Dir = SwingDir.Up, Hit = true, Late = true, Move = Move.Run, Face = -1, Offset = 77 };
        }
        static IEnumerable<Case> Everything() { foreach (var c in Cases()) yield return c; foreach (var c in Special()) yield return c; }

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
                double x = h.Cx + c.Face * (c.Offset + (c.Late ? 0 : speed * 3 / 60));                           // (he moves on for the press step and the next two, until the box goes live and the blow lands)
                target = c.Dir == SwingDir.Up ? new Dummy(x, -60, DummyKind.Hanging) : c.Dir == SwingDir.Down ? new Dummy(h.Cx, 0) : new Dummy(x, 0);
                r.W.Dummies = new List<Dummy> { target };
            }

            if (c.Dir == SwingDir.Up) r.C.SetHeld(Act.Up, true);
            if (c.Dir == SwingDir.Down) r.C.SetHeld(Act.Down, true);
            if (c.Dir == SwingDir.Fwd) h.SlashAlt = !c.Alt;                                                      // the press flips it: this is the alternation the cut will have
            for (int k = 0; k <= Steps; k++)
            {
                if (k == 0) { r.C.SetHeld(Act.Attack, true); r.C.Press(Act.Attack); } else if (k == 1) r.C.SetHeld(Act.Attack, false);
                if (c.EarlyDash && k == 4) { r.C.SetHeld(Act.Dash, true); r.C.Press(Act.Dash); } else if (c.EarlyDash && k == 5) r.C.SetHeld(Act.Dash, false);       // pressed in the hit-stop, kept, and made the moment it lets go
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
                    K = k, Frozen = r.W.Frozen, Box = h.Blow()?.Box, Marks = new Dictionary<string, V2>(canvas.Marks), Tris = Triangles(canvas), Rx = h.Cx, Ry = h.Y,
                    Lean = v.Lean, Sq = v.Sq, Dash = v.Dash, Face = r.Look.In.Face, Sw = CopyOf(r.Look.Swing),
                });
            }
            Assert.AreEqual(c.Hit ? 1 : 0, h.Strikes, $"{c.Name}: {(c.Hit ? "the blow lands" : "nothing is struck")}");
            if (c.Hit) Assert.AreEqual(1, target.Hits, $"{c.Name}: the dummy was struck, once (so a check against it is not empty)");
            int first = frames.FindIndex(f => f.Live);
            Assert.AreEqual(2, first, $"{c.Name}: the box goes live on step 2 (0.03 s after the press)");
            if (c.Hit) Assert.AreEqual(c.Late ? 9 : 3, frames.FindIndex(f => f.Frozen), $"{c.Name}: the blow lands on step {(c.Late ? 8 : 2)}, so the world is held still from step {(c.Late ? 9 : 3)}");
            if (c.EarlyDash) { Assert.IsFalse(frames[7].Live, $"{c.Name}: the dash ends the swing on step 7, the moment the hit-stop lets go"); Assert.IsNull(h.Atk); }
            return Played[c.Name] = new Cut { Frames = frames, Target = target?.Box() };
        }

        static SwingState CopyOf(SwingState s) => new SwingState { Dir = s.Dir, Alt = s.Alt, P = s.P, Fade = s.Fade, T = s.T, Live0 = s.Live0, Live1 = s.Live1, Total = s.Total };

        // the triangles of each named group of a canvas (what the art drew as the slash and as the blade's body), in the world
        static Dictionary<string, List<Tri>> Triangles(InkCanvas c)
        {
            var d = new Dictionary<string, List<Tri>>();
            foreach (var (name, first, count) in c.Groups)
            {
                if (!d.TryGetValue(name, out var list)) d[name] = list = new List<Tri>();
                for (int i = first; i + 2 < first + count; i += 3)
                {
                    int a = c.Indices[i], b = c.Indices[i + 1], e = c.Indices[i + 2];
                    list.Add(new Tri { A = c.Verts[a], B = c.Verts[b], C = c.Verts[e], Alpha = Math.Max(c.Colors[a].A, Math.Max(c.Colors[b].A, c.Colors[e].A)) });
                }
            }
            return d;
        }
        static IEnumerable<Tri> Of(Frame f, params string[] groups) { foreach (var g in groups) if (f.Tris.TryGetValue(g, out var l)) foreach (var t in l) yield return t; }

        // The exact distance between a triangle and an axis-aligned box: 0 if they overlap (a separating-axis test: the box's two axes and the triangle's three
        // edge normals), else the smallest distance between a corner of one and the edges of the other.
        static double Distance(Tri t, Box b)
        {
            double x0 = b.X, x1 = b.X + b.W, y0 = b.Y, y1 = b.Y + b.H; var v = new[] { t.A, t.B, t.C }; var corner = new[] { new V2(x0, y0), new V2(x1, y0), new V2(x1, y1), new V2(x0, y1) };
            bool apart = Math.Max(v[0].X, Math.Max(v[1].X, v[2].X)) < x0 || Math.Min(v[0].X, Math.Min(v[1].X, v[2].X)) > x1 || Math.Max(v[0].Y, Math.Max(v[1].Y, v[2].Y)) < y0 || Math.Min(v[0].Y, Math.Min(v[1].Y, v[2].Y)) > y1;
            for (int i = 0; i < 3 && !apart; i++)
            {
                var e = v[(i + 1) % 3] - v[i]; var n = new V2(-e.Y, e.X); if (n.Length < 1e-12) continue;
                double tmin = double.MaxValue, tmax = double.MinValue, cmin = double.MaxValue, cmax = double.MinValue;
                foreach (var p in v) { double d = n.Dot(p); tmin = Math.Min(tmin, d); tmax = Math.Max(tmax, d); }
                foreach (var p in corner) { double d = n.Dot(p); cmin = Math.Min(cmin, d); cmax = Math.Max(cmax, d); }
                apart = tmax < cmin || tmin > cmax;
            }
            if (!apart) return 0;
            double best = double.MaxValue;
            foreach (var p in v) best = Math.Min(best, Outside(p, b));
            foreach (var p in corner) for (int i = 0; i < 3; i++) best = Math.Min(best, ToSegment(p, v[i], v[(i + 1) % 3]));
            return best;
        }
        static double ToSegment(V2 p, V2 a, V2 b)
        {
            var ab = b - a; double l2 = ab.Dot(ab), t = l2 < 1e-18 ? 0 : Mathx.Clamp((p - a).Dot(ab) / l2, 0, 1); return (p - (a + ab * t)).Length;
        }

        static bool Inside(V2 p, Box b, double grow) => p.X >= b.X - grow && p.X <= b.X + b.W + grow && p.Y >= b.Y - grow && p.Y <= b.Y + b.H + grow;
        static double Outside(V2 p, Box b)        // how far a point is from a box: 0 inside
        {
            double dx = Math.Max(0, Math.Max(b.X - p.X, p.X - (b.X + b.W))), dy = Math.Max(0, Math.Max(b.Y - p.Y, p.Y - (b.Y + b.H)));
            return Math.Sqrt(dx * dx + dy * dy);
        }
        static double Lag(Arc a) => (a.O > a.N ? -1 : 1) * Math.Atan2(HeroLook.BladeLen * 0.1, 5 + HeroLook.BladeLen);        // the curved blade's tip trails its axis by this: the slash is drawn at the tip's angles (the axis's plus lag), as task 1's trail was
        static string At(V2 p) => $"({p.X:0.0}, {p.Y:0.0})";
        static string Show(Box b) => $"x {b.X:0.0}..{b.X + b.W:0.0}, y {b.Y:0.0}..{b.Y + b.H:0.0}";

        // a world point back into the body's own frame, undoing what DrawKaname does to it: the translation, the squash and stretch and the mirror, then the lean
        static (double x, double y) ToBody(Frame f, V2 p)
        {
            double sx = f.Face * (1 + f.Sq * 0.4 + f.Dash * HeroLook.DashStretch), sy = 1 - f.Sq * 0.4 - f.Dash * HeroLook.DashSquash;
            double ux = (p.X - f.Rx) / sx, uy = (p.Y - f.Ry) / sy, c = Math.Cos(f.Lean), s = Math.Sin(f.Lean);
            return (ux * c + uy * s, -ux * s + uy * c);
        }

        // the angle of a world point about the swing's pivot (given in his own frame), in the swing's own frame, and how far it is from the pivot: the lean put back, since a cut's arc is the blade's angle in the world
        static (double angle, double radius) AboutThePivot(Frame f, (double x, double y) pivot, V2 p)
        {
            var (bx, by) = ToBody(f, p); double dx = bx - pivot.x, dy = by - pivot.y;
            return (Math.Atan2(dy, dx) + f.Lean, Math.Sqrt(dx * dx + dy * dy));
        }
        static (double x, double y) PivotOf(Arc a) => (a.Px, a.Py);

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
            {
                foreach (var kv in f.Marks)
                    Assert.LessOrEqual(kv.Value.Y, f.Ry + 0.5, $"{c.Name} step {f.K}: {kv.Key} {At(kv.Value)} is {kv.Value.Y - f.Ry:0.0} u under his feet (y {f.Ry:0.0})");
                foreach (var t in Of(f, "cut.bright", "cut.lead", "after"))                             // and every corner of every triangle of the slash
                    foreach (var v in new[] { t.A, t.B, t.C })
                        Assert.LessOrEqual(v.Y, f.Ry + 0.5, $"{c.Name} step {f.K}: a corner of the slash {At(v)} is {v.Y - f.Ry:0.0} u under his feet (y {f.Ry:0.0})");
            }
        }

        [TestCaseSource(nameof(Everything))]
        public void TheCutCoversTheWholeDamagingSweepWhileTheBladeCanHitAndOnlyThen(Case c)
        {
            var arc = HeroLook.Swings(c.Dir, c.Alt); double dir = Math.Sign(arc.O - arc.N), lag = Lag(arc); int live = 0;
            foreach (var f in Play(c).Frames)
            {
                bool cut = f.Tris.ContainsKey("cut.bright") || f.Tris.ContainsKey("cut.lead"), marks = f.Marks.ContainsKey("inner.from") || f.Marks.ContainsKey("inner.to") || f.Marks.ContainsKey("inner.lead") || f.Marks.ContainsKey("slash.pivot");
                if (!f.Live)                                                                            // the wind-up, the follow-through, after an early end: no cut
                {
                    Assert.IsFalse(cut, $"{c.Name} step {f.K}: there is a cut when the blade cannot hit"); Assert.IsFalse(marks, $"{c.Name} step {f.K}: there are cut marks when the blade cannot hit");
                    continue;
                }
                live++;
                Assert.IsTrue(f.Tris.ContainsKey("cut.bright") && marks && f.Marks.ContainsKey("inner.from") && f.Marks.ContainsKey("inner.to") && f.Marks.ContainsKey("slash.pivot"), $"{c.Name} step {f.K}: the blade can hit but there is no cut");
                var pv = ToBody(f, f.Marks["slash.pivot"]); Assert.AreEqual(arc.Px, pv.x, 1e-6, $"{c.Name} step {f.K}: the slash is about the swing's pivot, not the hand"); Assert.AreEqual(arc.Py, pv.y, 1e-6, $"{c.Name} step {f.K}: the pivot's height");
                var (from, rFrom) = AboutThePivot(f, pv, f.Marks["inner.from"]); var (to, rTo) = AboutThePivot(f, pv, f.Marks["inner.to"]);
                Assert.AreEqual(arc.N + lag, from, 1e-6, $"{c.Name} step {f.K}: the cut begins where the tip enters the box (N {arc.N:0.000} + lag {lag:0.0000}), not at {from:0.000}");
                Assert.AreEqual(arc.O + lag, to, 1e-6, $"{c.Name} step {f.K}: the cut runs to where the tip's sweep ends (O {arc.O:0.000} + lag {lag:0.0000}), not to {to:0.000}");
                Assert.LessOrEqual(dir * (to - arc.O), 1e-6, $"{c.Name} step {f.K}: the cut's end {to:0.000} has passed O ({arc.O:0.000})");
                Assert.AreEqual(arc.R, rFrom, 1e-6, $"{c.Name} step {f.K}: the cut's near end is {rFrom:0.00} u from the pivot (the tip's radius {arc.R})"); Assert.AreEqual(arc.R, rTo, 1e-6, $"{c.Name} step {f.K}: its far end too");
            }
            Assert.AreEqual(c.Late ? 11 : c.EarlyDash ? 5 : c.Hit ? 11 : 7, live, $"{c.Name}: steps on which the blade can hit");
        }

        [TestCaseSource(nameof(Everything))]
        public void TheBrightPartEndsAtTheBladeAndNothingElseLeadsIt(Case c)
        {
            var arc = HeroLook.Swings(c.Dir, c.Alt); double lag = Lag(arc);
            foreach (var f in Play(c).Frames)
            {
                if (!f.Live) continue;
                var pv = ToBody(f, f.Marks["slash.pivot"]); var (lead, r) = AboutThePivot(f, pv, f.Marks["inner.lead"]);
                double blade = HeroLook.SwingAngle(f.Sw); blade = (arc.O > arc.N ? Mathx.Clamp(blade, arc.N, arc.O) : Mathx.Clamp(blade, arc.O, arc.N)) + lag;       // the blade's tip: its axis angle plus lag, capped at O + lag
                Assert.AreEqual(blade, lead, 0.02, $"{c.Name} step {f.K}: the bright part ends at {lead:0.000}, the blade is at {blade:0.000}"); Assert.AreEqual(arc.R, r, 1e-6, $"{c.Name} step {f.K}: the lead mark is at the tip's radius");
                foreach (var t in Of(f, "cut.bright"))                                                  // and the bright part really stops there: no bright triangle has a corner ahead of the blade
                    foreach (var v in new[] { t.A, t.B, t.C })
                    {
                        double a = AboutThePivot(f, pv, v).angle; Assert.LessOrEqual((arc.O > arc.N ? 1 : -1) * (a - blade), 0.01, $"{c.Name} step {f.K}: a bright corner at {a:0.000} is ahead of the blade ({blade:0.000})");
                    }
            }
        }

        // k, the afterimage's strength: 1 while the blade sweeps, then falling to 0 at Live1 over the hold
        static double Strength(SwingState s) { double sweepEnd = s.Live0 + Math.Min(HeroLook.SweepTime, s.Live1 - s.Live0); return s.T < sweepEnd ? 1 : Mathx.Clamp((s.Live1 - s.T) / (s.Live1 - sweepEnd), 0, 1); }

        [TestCaseSource(nameof(Everything))]
        public void TheAfterimageIsDrawnOnlyWhileTheBladeCanHitFadesOutOverTheHoldAndOnlyFollowsTheBlade(Case c)
        {
            var arc = HeroLook.Swings(c.Dir, c.Alt); double d = arc.O > arc.I ? 1 : -1, lag = Lag(arc);
            foreach (var f in Play(c).Frames)
            {
                bool after = f.Tris.ContainsKey("after") || f.Marks.ContainsKey("after.from") || f.Marks.ContainsKey("after.to");
                if (!f.Live) { Assert.IsFalse(after, $"{c.Name} step {f.K}: there is an afterimage when the blade cannot hit (before it, in the follow-through or after an early end)"); continue; }
                Assert.IsTrue(f.Tris.ContainsKey("after") && f.Marks.ContainsKey("after.from") && f.Marks.ContainsKey("after.to"), $"{c.Name} step {f.K}: the blade can hit but there is no afterimage");
                double k = Strength(f.Sw), max = 0; foreach (var t in f.Tris["after"]) max = Math.Max(max, t.Alpha);
                Assert.LessOrEqual(max, 0.22 * k + 1e-6, $"{c.Name} step {f.K} (T {f.Sw.T:0.0000}): the afterimage's strongest vertex is {max:0.0000}, its limit 0.22 x k {k:0.000} = {0.22 * k:0.0000}");
                if (f.Sw.T >= f.Sw.Live0 + HeroLook.SweepTime) Assert.Greater(max, 0.22 * k * 0.5, $"{c.Name} step {f.K}: the faded afterimage is there, at {max:0.0000} (k {k:0.000})");
                var pv = ToBody(f, f.Marks["slash.pivot"]); var (from, _) = AboutThePivot(f, pv, f.Marks["after.from"]); var (to, _) = AboutThePivot(f, pv, f.Marks["after.to"]);
                double headWant = HeroLook.SwingAngle(f.Sw), tailWant = HeroLook.SwingAngle(new SwingState { Dir = f.Sw.Dir, Alt = f.Sw.Alt, P = f.Sw.P, T = Math.Max(f.Sw.Live0, f.Sw.T - 0.05), Live0 = f.Sw.Live0, Live1 = f.Sw.Live1, Total = f.Sw.Total });
                double lo = Math.Min(arc.I, arc.O) + lag, hi = Math.Max(arc.I, arc.O) + lag; headWant = Mathx.Clamp(headWant + lag, lo, hi); tailWant = Mathx.Clamp(tailWant + lag, lo, hi);       // everything at the tip's angles: (I..O) + lag
                Assert.AreEqual(headWant, to, 1e-6, $"{c.Name} step {f.K}: the afterimage's head is at {to:0.000}, the blade's tip is at {headWant:0.000}"); Assert.AreEqual(tailWant, from, 1e-6, $"{c.Name} step {f.K}: the afterimage's tail is at {from:0.000}, 0.05 s behind the tip is {tailWant:0.000}");
                Assert.GreaterOrEqual(from, lo - 1e-6, $"{c.Name} step {f.K}: the tail {from:0.000} is outside (I..O) + lag"); Assert.LessOrEqual(to, hi + 1e-6, $"{c.Name} step {f.K}: the head {to:0.000} is outside (I..O) + lag");
                Assert.LessOrEqual(d * (from - to), 1e-6, $"{c.Name} step {f.K}: the afterimage runs ahead of the blade (tail {from:0.000}, head {to:0.000})");
            }
        }

        [TestCaseSource(nameof(Everything))]
        public void NothingOfTheSlashLiesBeyondTheTipsPathOrTheCutsSpanAndItStaysNearTheBox(Case c)
        {
            var arc = HeroLook.Swings(c.Dir, c.Alt); double lag = Lag(arc), lo = Math.Min(arc.N, arc.O) + lag - 0.01, hi = Math.Max(arc.N, arc.O) + lag + 0.01;
            foreach (var f in Play(c).Frames)
                foreach (var t in Of(f, "cut.bright", "cut.lead", "after"))
                    foreach (var v in new[] { t.A, t.B, t.C })
                    {
                        var pv = ToBody(f, f.Marks["slash.pivot"]); var (a, r) = AboutThePivot(f, pv, v);
                        Assert.LessOrEqual(r, arc.R + 0.05, $"{c.Name} step {f.K}: a corner of the slash {At(v)} is {r:0.00} u from the pivot, beyond the tip's path (radius {arc.R})");
                        Assert.That(a, Is.InRange(lo, hi), $"{c.Name} step {f.K}: a corner of the slash {At(v)} is at angle {a:0.000}, outside (N..O) + lag ({lo + 0.01:0.000}..{hi - 0.01:0.000})");
                        Assert.LessOrEqual(Outside(v, f.Box.Value), 8, $"{c.Name} step {f.K}: a corner of the slash {At(v)} is {Outside(v, f.Box.Value):0.0} u from the live box ({Show(f.Box.Value)})");
                    }
        }

        [TestCaseSource(nameof(Cases))]
        public void TheCutFromNToOCoversEightyPercentOfTheBox(Case c)
        {
            var frames = Play(c).Frames; var f = frames[frames.FindLastIndex(x => x.Live)];                              // the last live step: the sweep is done
            var b = f.Box.Value; string axis = c.Dir == SwingDir.Fwd ? "height" : "width";
            Assert.IsTrue(f.Marks.ContainsKey("inner.from") && f.Marks.ContainsKey("inner.to"), $"{c.Name} step {f.K}: there is a cut on the last live step");
            double a = c.Dir == SwingDir.Fwd ? Mathx.Clamp(f.Marks["inner.from"].Y, b.Y, b.Y + b.H) : Mathx.Clamp(f.Marks["inner.from"].X, b.X, b.X + b.W);        // the cut's ends at the tip's radius, clamped to the box
            double z = c.Dir == SwingDir.Fwd ? Mathx.Clamp(f.Marks["inner.to"].Y, b.Y, b.Y + b.H) : Mathx.Clamp(f.Marks["inner.to"].X, b.X, b.X + b.W);
            double size = c.Dir == SwingDir.Fwd ? b.H : b.W;
            Assert.GreaterOrEqual(Math.Abs(z - a), 0.8 * size, $"{c.Name} step {f.K}: the cut from N to O, clamped to the box, covers {Math.Abs(z - a):0.0} u of its {axis} of {size:0.0} u (needs 80 %: {0.8 * size:0.0} u; from {At(f.Marks["inner.from"])} to {At(f.Marks["inner.to"])}; {Show(b)})");
        }

        [TestCaseSource(nameof(Cases))]
        public void TheBladeIsMidBoxOnTheFirstLiveStepWhichIsTheFrameAHitFreezes(Case c)
        {
            var cut = Play(c); var f2 = cut.Frames[2]; var b = f2.Box.Value; var tip = f2.Marks["blade.4"];
            double along = c.Dir == SwingDir.Fwd ? (tip.Y - b.Y) / b.H : (tip.X - b.X) / b.W; string axis = c.Dir == SwingDir.Fwd ? "height" : "width";
            // (2b, R3: a down strike so near the floor that the blade cannot reach its hit-frame angle without sinking in rests on the floor instead, a little further
            // forward or back than mid-box: then the tip may be up to 10 % of the box's width past its front or back edge. Only a tip that is on the floor gets this.)
            bool resting = c.Dir == SwingDir.Down && Math.Abs(tip.Y - Stage.FloorBelow(tip.X, tip.Y - 2)) <= 0.6;
            Assert.That(along, resting ? Is.InRange(-0.1, 1.1) : Is.InRange(0.25, 0.75), $"{c.Name} step 2: the tip {At(tip)} is {along * 100:0} % of the way across the box's {axis} ({Show(b)}), not between {(resting ? "-10 and 110" : "25 and 75")} %");
            if (!c.Hit) return;
            foreach (var f in cut.Frames)                                                              // the picture the hit freezes: step 2, held through the hit-stop
            {
                if (f.K != 2 && !f.Frozen) continue;
                double best = double.MaxValue; string near = "";
                foreach (var k in new[] { "blade.2", "blade.3", "blade.4" }) { double d = Outside(f.Marks[k], cut.Target.Value); if (d < best) { best = d; near = $"{k} {At(f.Marks[k])}"; } }
                double need = resting ? 8 : 4;                                                         // (a blade resting on the floor may be a little further from the post: the slash layers show the contact, which the frozen-frame tests below measure)
                Assert.LessOrEqual(best, need, $"{c.Name} step {f.K}{(f.Frozen ? " (held still by the hit)" : "")}: the blade is {best:0.0} u from the struck dummy ({Show(cut.Target.Value)}); nearest {near}, needs {need} u");
            }
        }

        // ---- the hit frame shows the blow reaching the target (task 2, measured on the rendered triangles) ----
        public enum Cut5 { ForwardHighToLow, ForwardLowToHigh, Up, Down }

        const double ContactTolerance = 2.05;                                                                  // how near a drawn triangle must be to the struck dummy's box to count as touching it, in u: a test number (2.00 until the designer approved 2.05 for a 0.02 u miss), not a look or gameplay number

        // Strike one dummy at `offset` u from his middle (positive: ahead of him, negative: behind) and class what the frozen hit-stop frames show of the blow against the
        // dummy's box, with the exact distance to the rendered triangles: B = a visible blade-body or bright-cut triangle within ContactTolerance, L = only the thin leading
        // line within it, N = nothing (the gap to the nearest visible blade or cut triangle). A triangle is visible when its largest vertex alpha is at least 0.2.
        static (bool hit, int step, char cls, double gap) Probe(Cut5 cut, Move move, int face, double offset)
        {
            var r = new Rig(); var h = r.H; bool down = cut == Cut5.Down; double speed = move == Move.Sprint ? 336 : move == Move.Run ? 240 : 0;
            if (down) h.Place(240, -62, speed * face, 0, face); else h.Place(0, 0, 0, 0, face);
            r.W.Cam.X = r.W.CamPrev = h.Cx;
            if (speed > 0) { r.C.Push = face; r.C.SetHeld(face > 0 ? Act.Right : Act.Left, true); }
            if (move == Move.Sprint) r.C.SetHeld(Act.Dash, true);
            for (int i = 0; i < (down ? 6 : 40); i++) r.Step();
            double x = h.Cx + face * (offset + speed * 3 / 60);                                                  // (he moves on for the press step and the next two, until the box goes live and the blow lands)
            var target = cut == Cut5.Up ? new Dummy(x, -60, DummyKind.Hanging) : new Dummy(x, 0);               // the lowest hanging dummy, or the post (below him, or in front of him)
            r.W.Dummies = new List<Dummy> { target };
            if (cut == Cut5.Up) r.C.SetHeld(Act.Up, true); if (down) r.C.SetHeld(Act.Down, true);
            if (cut == Cut5.ForwardHighToLow || cut == Cut5.ForwardLowToHigh) h.SlashAlt = cut == Cut5.ForwardHighToLow;
            char worst = 'B'; double gap = 0; int frozen = 0, hitStep = -1;
            for (int k = 0; k <= 14 && frozen < 4; k++)
            {
                if (k == 0) { r.C.SetHeld(Act.Attack, true); r.C.Press(Act.Attack); } else if (k == 1) r.C.SetHeld(Act.Attack, false);
                r.Step();
                if (!r.W.Frozen) { if (hitStep < 0 && target.Hits > 0) hitStep = k; continue; }
                frozen++; var tris = Triangles(r.Draw()); var box = target.Box(); double bestB = double.MaxValue, bestL = double.MaxValue;
                foreach (var g in new[] { "blade.body", "cut.bright" }) if (tris.TryGetValue(g, out var list)) foreach (var t in list) if (t.Alpha >= 0.2f) bestB = Math.Min(bestB, Distance(t, box));
                if (tris.TryGetValue("cut.lead", out var lead)) foreach (var t in lead) if (t.Alpha >= 0.2f) bestL = Math.Min(bestL, Distance(t, box));
                char cls = bestB <= ContactTolerance ? 'B' : bestL <= ContactTolerance ? 'L' : 'N'; double g2 = Math.Min(bestB, bestL);
                if (cls == 'N' || (cls == 'L' && worst == 'B')) { worst = cls; gap = cls == 'N' ? g2 : 0; }                    // the worst of the frozen frames (they are one picture)
            }
            if (target.Hits == 0) return (false, -1, '-', 0);
            Assert.AreEqual(1, target.Hits, $"{cut} {move} {face} {offset}: struck once"); Assert.AreEqual(4, frozen, $"{cut} {move} {face} {offset}: the world is held still for 4 steps");
            return (true, hitStep, worst, gap);
        }

        static (double lo, double hi) Window(Cut5 cut) => cut == Cut5.Up ? (-30, 40) : cut == Cut5.Down ? (-30, 35) : (5, 65);        // the target's middle from his middle: forward 5 u ahead to the reach limit, up 30 u behind to 40 u ahead (2b: it was 10 u behind), down 30 u behind to 35 u ahead

        static IEnumerable<object[]> ContactCases()
        {
            foreach (Cut5 cut in Enum.GetValues(typeof(Cut5))) foreach (Move move in Enum.GetValues(typeof(Move))) foreach (int face in new[] { 1, -1 }) yield return new object[] { cut, move, face };
        }

        [TestCaseSource(nameof(ContactCases))]
        public void TheFrozenHitFrameShowsTheBlowReachingTheTargetInsideItsWindow(Cut5 cut, Move move, int face)
        {
            var (lo, hi) = Window(cut); var problems = new List<string>(); int hits = 0; double speed = move == Move.Sprint ? 336 : move == Move.Run ? 240 : 0;
            for (double o = -60; o <= 90; o += 5)                                                              // every position a hit can come from, and the ones around it
            {
                var (hit, step, cls, gap) = Probe(cut, move, face, o); if (!hit) { if (o >= lo && o <= hi) problems.Add($"{o:0} u: the simulation does not register a hit"); continue; }
                double at = o + (2 - step) * speed / 60;                                                       // where he stood from the dummy when the blow landed (a dummy a runner closes on is struck later, nearer the reach limit)
                TestContext.WriteLine($"CONTACT\t{cut}\t{move}\t{face}\t{at:0.#}\t{step}\t{cls}\t{(cls == 'N' ? gap.ToString("0.00") : "")}");
                if (step != 2) continue;                                                                       // (only a blow that lands on the first live step is a position of the table's windows)
                hits++; if (o >= lo && o <= hi && cls == 'N') problems.Add($"{o:0} u: nothing drawn within {ContactTolerance:0.00} u of the dummy (gap {gap:0.00} u)");
            }
            Assert.Greater(hits, 5, $"{cut} {move} {face}: the cases are not vacuous (the simulation registered {hits} hits on the first live step)");
            Assert.IsEmpty(problems, $"{cut}, {move}, facing {face}, window {lo:0} to {hi:0} u:\n" + string.Join("\n", problems));
        }

        // ---- task 2b (R1 to R3) ----

        // Every up-strike position at which the blade or the bright cut visibly touched the dummy before 2b (measured on task 2's code, 2.5 u apart, offsets as in Probe)
        // still does (D128: the close-range backward hits stay visible).
        static readonly (Move move, double from, double to)[] UpBefore2b = { (Move.Stand, -25, 20), (Move.Run, -20, 22.5), (Move.Sprint, -10, 22.5) };

        [Test]
        public void EveryUpStrikePositionThatShowedItsBladeBeforeStillDoes([Values(1, -1)] int face)
        {
            var lost = new List<string>(); int n = 0;
            foreach (var (move, from, to) in UpBefore2b)
                for (double o = from; o <= to + 1e-9; o += 2.5)
                {
                    var (hit, step, cls, gap) = Probe(Cut5.Up, move, face, o); n++;
                    if (!hit || cls != 'B') lost.Add($"{move} {o:0.0}: {(hit ? cls.ToString() + " gap " + gap.ToString("0.00") : "no hit")}");
                }
            Assert.Greater(n, 45); Assert.IsEmpty(lost, $"up strikes that showed the blade before 2b and no longer do, facing {face}:\n" + string.Join("\n", lost));
        }

        // The straightening (R1): at the frame a hit freezes at most 30 % of the run's or sprint's lean is left, and it comes back after the swing.
        [Test]
        public void HeStraightensForAnUpStrikeAndTheLeanComesBack([Values(Move.Run, Move.Sprint)] Move move, [Values(1, -1)] int face)
        {
            var r = new Rig(); var h = r.H; h.Place(0, 0, 0, 0, face); r.C.Push = face; r.C.SetHeld(face > 0 ? Act.Right : Act.Left, true); if (move == Move.Sprint) r.C.SetHeld(Act.Dash, true);
            for (int i = 0; i < 40; i++) r.Step();
            double lean0 = Math.Abs(r.Look.Vis.Lean); Assert.Greater(lean0, 0.1, "the run leans him");
            r.C.SetHeld(Act.Up, true); double at2 = 0, end = 0;
            for (int k = 0; k <= 30; k++)
            {
                if (k == 0) { r.C.SetHeld(Act.Attack, true); r.C.Press(Act.Attack); } else if (k == 1) r.C.SetHeld(Act.Attack, false);
                r.Step(); if (k == 2) at2 = Math.Abs(r.Look.Vis.Lean); if (k == 30) end = Math.Abs(r.Look.Vis.Lean);
            }
            Assert.LessOrEqual(at2, 0.3 * lean0, $"{move}: {at2:0.000} of the run's lean {lean0:0.000} is left on step 2");
            Assert.GreaterOrEqual(end, 0.8 * lean0, $"{move}: the lean has not come back ({end:0.000} of {lean0:0.000})");
        }

        // The up cut's tip, its two ends and the blade's tip stay within 4 u of the up box (the hand is raised, the cut is no bigger).
        [TestCaseSource(nameof(OnTheGround))]
        public void TheUpCutStaysWithin4uOfItsBox(Case c)
        {
            if (c.Dir != SwingDir.Up) return;
            foreach (var f in Play(c).Frames)
            {
                if (!f.Live) continue;
                foreach (var k in new[] { "inner.from", "inner.to", "inner.lead", "blade.4" })
                    Assert.LessOrEqual(Outside(f.Marks[k], f.Box.Value), 4, $"{c.Name} step {f.K}: {k} {At(f.Marks[k])} is {Outside(f.Marks[k], f.Box.Value):0.0} u from the box ({Show(f.Box.Value)})");
            }
        }

        // The floor (R3): the blade's body and every part of the slash stay out of the stage's solids, for a down strike from every height near the ground, on the ground
        // and on top of a pillar, beside a pillar's edge, and for forward and up cuts on the ground.
        static string Inside(InkCanvas cv, double tolerance)
        {
            foreach (var (name, first, count) in cv.Groups)
            {
                if (name != "blade.body" && name != "cut.bright" && name != "cut.lead" && name != "after") continue;
                for (int i = first; i < first + count; i++)
                {
                    var v = cv.Verts[cv.Indices[i]];
                    foreach (var sol in Stage.Solids)
                        if (v.X > sol.Box.X + tolerance && v.X < sol.Box.X + sol.Box.W - tolerance && v.Y > sol.Box.Y + tolerance && v.Y < sol.Box.Y + sol.Box.H && cv.Colors[cv.Indices[i]].A >= 0.01f)
                            return $"{name} vertex ({v.X:0.0}, {v.Y:0.0}) is {v.Y - sol.Box.Y:0.00} u below a surface at y {sol.Box.Y:0.0} (x {sol.Box.X:0}..{sol.Box.X + sol.Box.W:0})";
                }
            }
            return null;
        }

        static IEnumerable<object[]> DownNearSurfaces()
        {
            foreach (int face in new[] { 1, -1 })
                foreach (var (x, top) in new[] { (240.0, 0.0), (450.0, -32.0), (395.0, 0.0), (470.0, -32.0), (1065.0, -128.0) })        // open ground, the 1-tile pillar's top, ground 13 u from its wall, near the top's edge, the 4-tile pillar's top
                    foreach (double up in new[] { 2.0, 8, 14, 20, 30, 40, 50, 60 })
                        yield return new object[] { face, x, top, up };
        }

        [TestCaseSource(nameof(DownNearSurfaces))]
        public void ADownStrikeNearASurfaceNeverDrawsTheBladeOrTheSlashIntoIt(int face, double x, double top, double up)
        {
            var r = new Rig(); var h = r.H; h.Place(x, top - up, 0, 0, face); r.W.Cam.X = r.W.CamPrev = h.Cx; r.Step(); r.C.SetHeld(Act.Down, true);
            for (int k = 0; k <= 16; k++)
            {
                if (k == 0) { r.C.SetHeld(Act.Attack, true); r.C.Press(Act.Attack); } else if (k == 1) r.C.SetHeld(Act.Attack, false);
                r.Step(); if (h.OnGround) break;                                                                  // a down strike is an air attack: it is judged until he lands
                var bad = Inside(r.Draw(), 0.05); Assert.IsNull(bad, $"down strike facing {face} at x {x:0} from {up:0} u above y {top:0}, step {k}: {bad}");
            }
        }

        [TestCaseSource(nameof(OnTheGround))]
        public void AForwardOrUpCutOnTheGroundNeverDrawsTheSlashIntoIt(Case c)
        {
            foreach (var f in Play(c).Frames)
                foreach (var t in Of(f, "cut.bright", "cut.lead", "after"))
                    foreach (var v in new[] { t.A, t.B, t.C })
                        Assert.LessOrEqual(v.Y, f.Ry + 0.05, $"{c.Name} step {f.K}: a corner of the slash {At(v)} is {v.Y - f.Ry:0.00} u under the floor");
        }

        [TestCaseSource(nameof(Cases))]
        public void TheBladeIsDrawnOverTheSlashAndTheSlashAddsNoRedOrYellow(Case c)
        {
            foreach (var f in Play(c).Frames)
            {
                if (!f.Live || !f.Tris.ContainsKey("blade.body") || !f.Tris.ContainsKey("cut.bright")) continue;
                Assert.IsTrue(f.Tris["blade.body"].Count > 20 && f.Tris["cut.bright"].Count > 20, $"{c.Name} step {f.K}: the blade's body and the cut are both drawn");
            }
            var r = new Rig(); r.H.Place(0, 0, 0, 0, c.Face); r.Step(); r.C.SetHeld(Act.Attack, true); r.C.Press(Act.Attack); r.Step(); r.C.SetHeld(Act.Attack, false); for (int i = 0; i < 3; i++) r.Step();
            var cv = r.Draw(); var gr = cv.Groups; int blade = gr.FindIndex(g => g.name == "blade.body"), bright = gr.FindIndex(g => g.name == "cut.bright"), lead = gr.FindIndex(g => g.name == "cut.lead"), aft = gr.FindIndex(g => g.name == "after");
            Assert.That(aft, Is.GreaterThanOrEqualTo(0)); Assert.That(bright, Is.GreaterThan(aft), "the afterimage is under the cut"); Assert.That(blade, Is.GreaterThan(Math.Max(bright, lead)), "the blade is over the slash");
            foreach (var col in cv.Colors)
            {
                if (col.A < 0.04f) continue; bool red = col.R > 0.55f && col.G < 0.4f && col.B < 0.4f, yellow = col.R > 0.6f && col.G > 0.5f && col.B < 0.45f;
                Assert.IsFalse(red || yellow, $"{c.Name}: rgb({col.R:0.00},{col.G:0.00},{col.B:0.00}) is red or yellow");
            }
        }

        [TestCaseSource(nameof(Cases))]
        public void TheSlashHasNoDarkMassAndTheAfterimageIsNeverTooStrong(Case c)
        {
            if (c.Hit) return;                                                                                 // (the same picture: a hit only holds it still)
            var r = new Rig(); r.H.Place(0, 0, 0, 0, c.Face); r.Step();
            if (c.Dir == SwingDir.Up) r.C.SetHeld(Act.Up, true); if (c.Dir == SwingDir.Fwd) r.H.SlashAlt = !c.Alt;
            r.C.SetHeld(Act.Attack, true); r.C.Press(Act.Attack); r.Step(); r.C.SetHeld(Act.Attack, false);
            for (int i = 0; i < 4; i++) r.Step();                                                              // step 5: T = 0.083, in the sweep
            Assert.That(r.Look.Swing.T, Is.InRange(r.Look.Swing.Live0 + 0.01, r.Look.Swing.Live0 + HeroLook.SweepTime - 0.01), "a mid-sweep step");
            var cv = new InkCanvas(); KanameArt.DrawSlash(cv, r.Look, r.Look.Place());                         // the slash alone, on a fresh canvas
            Assert.Greater(cv.Indices.Count, 300, $"{c.Name}: the slash is drawn");
            var after = new HashSet<int>(); foreach (var (name, first, count) in cv.Groups) if (name == "after") for (int i = first; i < first + count; i++) after.Add(cv.Indices[i]);
            Assert.Greater(after.Count, 10, "there is an afterimage");
            foreach (int i in after) Assert.LessOrEqual(cv.Colors[i].A, 0.22f + 1e-6f, $"{c.Name}: an afterimage vertex has alpha {cv.Colors[i].A:0.000}, over 0.22");
            var ink = Palette.SlashInk; int dark = 0;
            foreach (var col in cv.Colors)
            {
                if (col.A < 0.01f) continue;
                bool old1 = Math.Abs(col.R - 28 / 255f) < 0.002f && Math.Abs(col.G - 12 / 255f) < 0.002f && Math.Abs(col.B - 64 / 255f) < 0.002f, old2 = Math.Abs(col.R - 18 / 255f) < 0.002f && Math.Abs(col.G - 6 / 255f) < 0.002f && Math.Abs(col.B - 44 / 255f) < 0.002f;
                Assert.IsFalse(old1 || old2, $"{c.Name}: one of the old violet-black bands (28,12,64 / 18,6,44) is drawn: rgb({col.R * 255:0},{col.G * 255:0},{col.B * 255:0})");
                if (Math.Max(col.R, Math.Max(col.G, col.B)) < 0.35f) { dark++; Assert.That(col.R, Is.EqualTo(ink.R).Within(0.002f), $"{c.Name}: a dark colour that is not the slash's ink line: rgb({col.R * 255:0},{col.G * 255:0},{col.B * 255:0})"); Assert.That(col.G, Is.EqualTo(ink.G).Within(0.002f)); Assert.That(col.B, Is.EqualTo(ink.B).Within(0.002f)); }
            }
            Assert.Greater(dark, 20, "the ink line is there to carry the slash on a white stage");
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
