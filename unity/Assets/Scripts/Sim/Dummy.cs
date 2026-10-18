using System.Collections.Generic;

namespace Kaname.Sim
{
    public enum DummyKind { Post, Hanging }

    // The direction of a blow of his sword (js/hero.js `atk.dir`): a forward cut, up, down, or the draw (the lunge, rung U5).
    public enum StrikeDir { Fwd, Up, Down, Draw }

    // The test dummy for the sword (js/dummy.js): a training post that stands, or hangs from above, and flinches when it is struck. It cannot be hurt
    // or killed and shows nothing over its head (the debug numbers count the blows). It is not solid: he runs and dashes straight through it.
    // Units as everywhere: world units, y = 0 is the ground, up is negative; (x, y) is the middle of its bottom.
    public class Dummy
    {
        public double X, Y;
        public readonly DummyKind Kind;           // a post stands on y; a hanging one dangles by a rope from far above, its bottom at y
        public double W = 26, H = 46;
        public int Hits;
        public double Flash;                      // seconds of white-blue flash left after a blow
        public StrikeDir Last = StrikeDir.Fwd;
        public double Ox, Vox, Oy, Voy;           // how far a blow has knocked it, and the speed of the spring back
        public double Lean, Vl;                   // and how far it has swung round its base (or its rope)

        public Dummy(double x, double y, DummyKind kind = DummyKind.Post) { X = x; Y = y; Kind = kind; }

        public Box Box() => new Box(X - W / 2, Y - H, W, H);

        // struck by a blow from `dir` swung by someone facing `face`, `power` 1 for a strike and 3 for the draw: it flinches away from it
        public virtual bool Hit(StrikeDir dir, int face, double power = 1)
        {
            Hits++; Flash = power > 1 ? 0.13 : 0.08; Last = dir;
            if (dir == StrikeDir.Draw) { Vox = face * 330; Vl = face * 13; }                 // the draw is a heavy blow: it flings the dummy
            else if (dir == StrikeDir.Fwd) { Vox = face * 140; Vl = face * 7; }
            else if (dir == StrikeDir.Up) Voy = -130;
            else Voy = 150;
            return false;                                                                    // (a blow on an opened Striker returns true: a critical blow, U6)
        }

        public virtual void Update(double dt)
        {
            Vox += (-300 * Ox - 22 * Vox) * dt; Ox += Vox * dt;                              // springs back to rest, a little bouncy
            Voy += (-300 * Oy - 22 * Voy) * dt; Oy += Voy * dt;
            Vl += (-220 * Lean - 14 * Vl) * dt; Lean += Vl * dt;
            Flash = System.Math.Max(0, Flash - dt);
        }

        // the standing set the sword is judged against: two posts on the ground and a short stair of hanging ones going up to the left (for the up
        // strike, the pogo, and chaining bounces and air dashes from one to the next); the striker that fights back joins them in U6
        public static List<Dummy> MakeSet() => new List<Dummy>
        {
            new Dummy(240, 0, DummyKind.Post), new Dummy(340, 0, DummyKind.Post),
            new Dummy(-240, -60, DummyKind.Hanging), new Dummy(-310, -100, DummyKind.Hanging), new Dummy(-380, -140, DummyKind.Hanging),
        };
    }
}
