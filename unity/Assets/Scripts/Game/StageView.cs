using System.Collections.Generic;
using Kaname.Sim;
using UnityEngine;

namespace Kaname.Game
{
    // Draws the test stage (js/game.js drawStage): a white world, a pale floor under the ground line, a height line every 32 u, and the
    // pillars and the ledge as pale blocks with a thin outline. Line widths are fixed in screen pixels, so they are re-sized when the zoom changes.
    public sealed class StageView
    {
        struct Palette
        {
            public Color Backdrop, Floor, Grid, Block, Outline, Ground;
        }

        static readonly Palette Day = new Palette
        {
            Backdrop = Pixel.Hex("#ffffff"), Floor = Pixel.Hex("#f2f4f8"), Grid = Pixel.Hex("#e3e7f0"),
            Block = Pixel.Hex("#e1e6f0"), Outline = Pixel.Hex("#aab3c8"), Ground = Pixel.Hex("#2b3350"),
        };
        static readonly Palette Night = new Palette    // (the JavaScript night sky has a gradient and stars: that comes with the look, U6)
        {
            Backdrop = Pixel.Hex("#1a1548"), Floor = Pixel.Hex("#120d30"), Grid = Pixel.Hex("#8faaff", 0.13f),
            Block = Pixel.Hex("#1d1760"), Outline = Pixel.Hex("#4b3aa0"), Ground = Pixel.Hex("#3a2f66"),
        };

        const double Reach = 20000;                    // how far the floor and the lines run either side of the origin, in u
        const int Ticks = 40;                          // a tick under the ground line every 64 u, taller every 320 u

        readonly Transform root;
        readonly SpriteRenderer floor, groundLine;
        readonly SpriteRenderer[] gridLines = new SpriteRenderer[Stage.GridLines];
        readonly List<(SpriteRenderer fill, SpriteRenderer[] edges, Box box)> blocks = new List<(SpriteRenderer, SpriteRenderer[], Box)>();
        readonly SpriteRenderer[] ticks = new SpriteRenderer[Ticks];

        public StageView(Transform parent)
        {
            root = new GameObject("Stage").transform;
            root.SetParent(parent, false);
            floor = Pixel.Quad(root, "Floor", 0);
            for (int i = 0; i < gridLines.Length; i++) gridLines[i] = Pixel.Quad(root, "Height " + (i + 1) * 32, 1);
            foreach (var s in Stage.Solids)
            {
                if (s.Kind == SolidKind.Ground) continue;
                var fill = Pixel.Quad(root, s.Kind + " fill", 2);
                var edges = new SpriteRenderer[4];
                for (int e = 0; e < 4; e++) edges[e] = Pixel.Quad(root, s.Kind + " edge", 3);
                blocks.Add((fill, edges, s.Box));
            }
            groundLine = Pixel.Quad(root, "Ground line", 4);
            for (int i = 0; i < ticks.Length; i++) ticks[i] = Pixel.Quad(root, "Tick", 4);
        }

        public Color Backdrop(bool night) => (night ? Night : Day).Backdrop;

        // `camX` is where the camera is, in u; `unitsPerPixel` is how many u one pixel of the real screen covers
        public void Update(ViewOptions view, double camX, double unitsPerPixel)
        {
            var p = view.Night ? Night : Day;
            double half = 640 / view.Zoom + 40, left = camX - half, right = camX + half;
            // A line is a whole number of real screen pixels wide (never less than one): a sprite thinner than a pixel can miss every pixel it
            // covers and vanish, so the JavaScript game's "1 logical pixel" line becomes 1 real pixel at 720p and 2 at 1440p.
            double devicePerLogical = (1 / view.Zoom) / unitsPerPixel;
            double Line(double logicalPx) => System.Math.Max(1, System.Math.Round(logicalPx * devicePerLogical)) * unitsPerPixel;
            double px = Line(1);

            floor.color = p.Floor; Pixel.Place(floor, -Reach, 0, 2 * Reach, 4000);
            for (int i = 0; i < gridLines.Length; i++)
            {
                var g = gridLines[i]; g.enabled = view.Grid; g.color = p.Grid;
                Pixel.Place(g, -Reach, -(i + 1) * Stage.Grid - px / 2, 2 * Reach, px);
            }
            double t = Line(1.5);                                   // the outline: 1.5 logical pixels, centred on the block's edge
            foreach (var (fill, edges, b) in blocks)
            {
                fill.color = p.Block; Pixel.Place(fill, b.X, b.Y, b.W, b.H);
                foreach (var e in edges) e.color = p.Outline;
                Pixel.Place(edges[0], b.X - t / 2, b.Y - t / 2, b.W + t, t);              // top
                Pixel.Place(edges[1], b.X - t / 2, b.Y + b.H - t / 2, b.W + t, t);        // bottom
                Pixel.Place(edges[2], b.X - t / 2, b.Y - t / 2, t, b.H + t);              // left
                Pixel.Place(edges[3], b.X + b.W - t / 2, b.Y - t / 2, t, b.H + t);        // right
            }
            double g2 = Line(2);                                    // the ground line: 2 logical pixels, centred on y = 0
            groundLine.color = p.Ground; Pixel.Place(groundLine, -Reach, -g2 / 2, 2 * Reach, g2);
            double x0 = System.Math.Ceiling(left / 64) * 64;
            for (int i = 0; i < ticks.Length; i++)
            {
                double x = x0 + i * 64;
                var r = ticks[i];
                r.enabled = x < right;
                if (!r.enabled) continue;
                r.color = p.Ground;
                Pixel.Place(r, x - px / 2, 0, px, x % 320 == 0 ? 9 : 5);
            }
        }

        public void Destroy() => Object.Destroy(root.gameObject);
    }
}
