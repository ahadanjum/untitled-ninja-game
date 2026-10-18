using System;
using System.Collections.Generic;
using Kaname.Ink;
using NUnit.Framework;

namespace Kaname.Tests
{
    // The ink canvas turns the HTML-canvas calls the JavaScript art uses into triangles. These tests check the geometry by area, position and colour.
    public class InkCanvasTests
    {
        static double Area(InkCanvas c)
        {
            double a = 0;
            for (int i = 0; i + 2 < c.Indices.Count; i += 3)
            {
                var p = c.Verts[c.Indices[i]]; var q = c.Verts[c.Indices[i + 1]]; var r = c.Verts[c.Indices[i + 2]];
                a += Math.Abs((q - p).Cross(r - p)) / 2;
            }
            return a;
        }

        static (double x0, double y0, double x1, double y1) Bounds(InkCanvas c)
        {
            double x0 = double.MaxValue, y0 = double.MaxValue, x1 = double.MinValue, y1 = double.MinValue;
            foreach (int i in c.Indices) { var v = c.Verts[i]; x0 = Math.Min(x0, v.X); y0 = Math.Min(y0, v.Y); x1 = Math.Max(x1, v.X); y1 = Math.Max(y1, v.Y); }
            return (x0, y0, x1, y1);
        }

        [Test]
        public void ARectangleFillsItsArea()
        {
            var c = new InkCanvas(); c.FillStyle(Rgba.Hex("#5b3cae")); c.FillRect(2, 3, 10, 4);
            Assert.AreEqual(40, Area(c), 1e-9);
            Assert.AreEqual(2, c.Indices.Count / 3);
            Assert.AreEqual(0x5b / 255f, c.Colors[0].R, 1e-5);
        }

        [Test]
        public void GlobalAlphaScalesTheColour()
        {
            var c = new InkCanvas(); c.GlobalAlpha = 0.45; c.FillStyle(Rgba.Of("143,77,255", 0.5)); c.FillRect(0, 0, 1, 1);
            Assert.AreEqual(0.225f, c.Colors[0].A, 1e-5);
        }

        [Test]
        public void ACircleHasTheAreaOfACircle()
        {
            var c = new InkCanvas(); c.BeginPath(); c.Arc(5, 5, 12.4, 0, 7); c.FillStyle(Rgba.Hex("#0b1236")); c.Fill();
            Assert.AreEqual(Math.PI * 12.4 * 12.4, Area(c), Math.PI * 12.4 * 12.4 * 0.02);
        }

        [Test]
        public void AnEllipseHasTheAreaOfAnEllipse()
        {
            var c = new InkCanvas(); c.BeginPath(); c.Ellipse(0, 0, 7.8, 9, 0.7, 0, 7); c.Fill();
            Assert.AreEqual(Math.PI * 7.8 * 9, Area(c), Math.PI * 7.8 * 9 * 0.02);
        }

        [Test]
        public void AHalfCircleIsHalfTheArea()
        {
            var c = new InkCanvas(); c.BeginPath(); c.Arc(0, 0, 4, 0, Math.PI); c.ClosePath(); c.Fill();
            Assert.AreEqual(Math.PI * 16 / 2, Area(c), Math.PI * 16 / 2 * 0.03);
        }

        [Test]
        public void AConcavePolygonIsFilledExactly()
        {
            // an L: 6 x 6 with a 3 x 3 corner cut out = 27
            var c = new InkCanvas(); c.BeginPath(); c.MoveTo(0, 0); c.LineTo(6, 0); c.LineTo(6, 3); c.LineTo(3, 3); c.LineTo(3, 6); c.LineTo(0, 6); c.ClosePath(); c.Fill();
            Assert.AreEqual(27, Area(c), 1e-9);
        }

        [Test]
        public void AFiveArmedStarIsFilledExactly()
        {
            var pts = new List<V2>(); double r0 = 10, r1 = 4;
            for (int i = 0; i < 10; i++) { double a = Math.PI * 2 * i / 10, r = i % 2 == 0 ? r0 : r1; pts.Add(new V2(Math.Cos(a) * r, Math.Sin(a) * r)); }
            double want = 0; for (int i = 0; i < 10; i++) want += pts[i].Cross(pts[(i + 1) % 10]) / 2; want = Math.Abs(want);
            var c = new InkCanvas(); c.BeginPath(); c.MoveTo(pts[0].X, pts[0].Y); for (int i = 1; i < 10; i++) c.LineTo(pts[i].X, pts[i].Y); c.ClosePath(); c.Fill();
            Assert.AreEqual(want, Area(c), 1e-6);
        }

        [Test]
        public void AButtLineHasLengthTimesWidthAndARoundOneAddsTheCaps()
        {
            var c = new InkCanvas(); c.LineWidth = 3; c.BeginPath(); c.MoveTo(0, 0); c.LineTo(10, 0); c.Stroke();
            Assert.AreEqual(30, Area(c), 1e-9);
            var d = new InkCanvas(); d.LineWidth = 3; d.LineCap = Cap.Round; d.BeginPath(); d.MoveTo(0, 0); d.LineTo(10, 0); d.Stroke();
            Assert.AreEqual(30 + Math.PI * 1.5 * 1.5, Area(d), 0.35);
            var b = Bounds(d); Assert.AreEqual(-1.5, b.x0, 0.01); Assert.AreEqual(11.5, b.x1, 0.01);
        }

        [Test]
        public void ASmoothCurveStrokeKeepsItsWidth()
        {
            var c = new InkCanvas(); c.LineWidth = 2; c.BeginPath(); c.MoveTo(0, 0); c.QuadraticCurveTo(20, 0, 20, 20); c.Stroke();
            // the length of that curve is about 31.5, so the area is about 63
            Assert.AreEqual(63, Area(c), 3.5);
        }

        [Test]
        public void ATranslucentStrokeDoesNotDoubleUpWhereItBends()
        {
            var c = new InkCanvas(); c.LineWidth = 4; c.StrokeStyle(new Rgba(1, 1, 1, 0.3f));
            c.BeginPath(); c.MoveTo(0, 0); c.QuadraticCurveTo(20, 0, 20, 20); c.Stroke();
            // overlapping triangles would make the summed area exceed width x length by a wide margin
            Assert.AreEqual(4 * 31.5, Area(c), 8);
        }

        [Test]
        public void ATransformAppliesInsideTheOnesBeforeIt()
        {
            var c = new InkCanvas(); c.Translate(10, 0); c.Rotate(Math.PI / 2); c.FillRect(0, 0, 1, 1);
            var b = Bounds(c);
            Assert.AreEqual(9, b.x0, 1e-9); Assert.AreEqual(10, b.x1, 1e-9); Assert.AreEqual(0, b.y0, 1e-9); Assert.AreEqual(1, b.y1, 1e-9);
        }

        [Test]
        public void ScalingStretchesAStrokeToo()
        {
            var c = new InkCanvas(); c.Scale(1, 3); c.LineWidth = 1; c.BeginPath(); c.MoveTo(0, 0); c.LineTo(10, 0); c.Stroke();
            var b = Bounds(c); Assert.AreEqual(-1.5, b.y0, 1e-9); Assert.AreEqual(1.5, b.y1, 1e-9);
            c.Clear(); c.Scale(-1, 1); c.FillRect(0, 0, 4, 2); Assert.AreEqual(8, Area(c), 1e-9);
        }

        [Test]
        public void SaveAndRestoreBringBackTheStateAndTheTransform()
        {
            var c = new InkCanvas(); c.GlobalAlpha = 0.5; c.Save(); c.Translate(100, 100); c.GlobalAlpha = 0.1; c.Restore();
            Assert.AreEqual(0.5, c.GlobalAlpha); c.FillRect(0, 0, 1, 1);
            Assert.AreEqual(0, Bounds(c).x0, 1e-9);
        }

        [Test]
        public void ARadialGradientFadesFromItsCentreToNothing()
        {
            var c = new InkCanvas(); var g = Gradient.Radial(3, 4, 6); g.AddColorStop(0, Rgba.Of("143,77,255", 0.6)); g.AddColorStop(1, Rgba.Of("143,77,255", 0));
            c.FillStyle(g); c.BeginPath(); c.Arc(3, 4, 6, 0, 7); c.Fill();
            float centre = float.MinValue, rim = float.MaxValue;
            for (int i = 0; i < c.Verts.Count; i++)
            {
                double d = (c.Verts[i] - new V2(3, 4)).Length;
                if (d < 1e-6) centre = c.Colors[i].A; if (d > 5.99) rim = Math.Min(rim, c.Colors[i].A);
            }
            Assert.AreEqual(0.6f, centre, 1e-5); Assert.AreEqual(0f, rim, 1e-5);
        }

        [Test]
        public void ALinearGradientRunsAlongItsAxis()
        {
            var c = new InkCanvas(); var g = Gradient.Linear(0, 0, 10, 0); g.AddColorStop(0, new Rgba(1, 0, 0, 0)); g.AddColorStop(1, new Rgba(1, 0, 0, 1));
            c.FillStyle(g); c.FillRect(0, 0, 10, 2);
            for (int i = 0; i < c.Verts.Count; i++) Assert.AreEqual((float)(c.Verts[i].X / 10), c.Colors[i].A, 1e-5);
        }

        [Test]
        public void AClipCutsAFillToTheClipShape()
        {
            var c = new InkCanvas(); c.BeginPath(); c.Arc(0, 0, 5, 0, 7); c.Clip(); c.FillRect(-20, -1, 40, 2);
            // a strip 2 high cut to a circle of radius 5: its area is that of the circle between y = -1 and 1
            double want = 2 * (1 * Math.Sqrt(25 - 1) + 25 * Math.Asin(1 / 5.0));
            Assert.AreEqual(want, Area(c), want * 0.03);
            var b = Bounds(c); Assert.LessOrEqual(b.x1, 5.001); Assert.GreaterOrEqual(b.x0, -5.001);
        }

        [Test]
        public void TriangleOrderIsDrawingOrder()
        {
            var c = new InkCanvas(); c.FillStyle(Rgba.Hex("#ff0000")); c.FillRect(0, 0, 1, 1); c.FillStyle(Rgba.Hex("#0000ff")); c.FillRect(0, 0, 1, 1);
            Assert.AreEqual(1f, c.Colors[c.Indices[0]].R); Assert.AreEqual(1f, c.Colors[c.Indices[c.Indices.Count - 1]].B);
        }

        [Test]
        public void ColoursParseAsTheArtWritesThem()
        {
            var a = Rgba.Css("rgba(217,200,255,0.5)"); Assert.AreEqual(217 / 255f, a.R, 1e-5); Assert.AreEqual(0.5f, a.A, 1e-5);
            var b = Rgba.Css("#b79cff"); Assert.AreEqual(0xb7 / 255f, b.R, 1e-5); Assert.AreEqual(1f, b.A);
            Assert.AreEqual(1f, Rgba.Hex("#fff").R);
        }
    }
}
