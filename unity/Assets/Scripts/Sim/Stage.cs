using System.Collections.Generic;

namespace Kaname.Sim
{
    // An axis-aligned box in world units (u): x, y is the top-left corner, up is negative (js/stage.js).
    public readonly struct Box
    {
        public readonly double X, Y, W, H;
        public Box(double x, double y, double w, double h) { X = x; Y = y; W = w; H = h; }
        public bool Overlaps(Box o) => X < o.X + o.W && X + W > o.X && Y < o.Y + o.H && Y + H > o.Y;
    }

    public enum SolidKind { Ground, Block, Ledge }

    public readonly struct Solid
    {
        public readonly Box Box;
        public readonly SolidKind Kind;
        public Solid(double x, double y, double w, double h, SolidKind kind) { Box = new Box(x, y, w, h); Kind = kind; }
    }

    // The test stage (js/stage.js): a flat ground and a few blocks to judge heights against. World units match Little Shinobi's pixels:
    // Kaname is about 34 u tall and one tile is 32 u. y = 0 is the ground and up is negative.
    public static class Stage
    {
        public const double Grid = 32;       // one tile: a height line every 32 u
        public const int GridLines = 5;

        // Pillars of 1, 2, 3 and 4 tiles, 200 u apart, to judge a jump against (a held jump clears 3 tiles but not 4), and a floating
        // ledge whose edge tests coyote time and whose underside tests the head bump.
        public static readonly (double x, double h)[] Pillars = { (420, 32), (620, 64), (820, 96), (1020, 128) };
        public static readonly Box Ledge = new Box(1260, -96, 200, 24);

        public static readonly IReadOnlyList<Solid> Solids = Build();

        static List<Solid> Build()
        {
            var s = new List<Solid> { new Solid(-100000, 0, 200000, 4000, SolidKind.Ground) };
            foreach (var (x, h) in Pillars) s.Add(new Solid(x, -h, 60, h, SolidKind.Block));
            s.Add(new Solid(Ledge.X, Ledge.Y, Ledge.W, Ledge.H, SolidKind.Ledge));
            return s;
        }

        // the top of the highest solid under a point at or below `y` (where a shadow falls)
        public static double FloorBelow(double x, double y)
        {
            double best = double.PositiveInfinity;
            foreach (var s in Solids)
                if (x >= s.Box.X && x <= s.Box.X + s.Box.W && s.Box.Y >= y - 0.5 && s.Box.Y < best) best = s.Box.Y;
            return double.IsPositiveInfinity(best) ? 0 : best;
        }
    }
}
