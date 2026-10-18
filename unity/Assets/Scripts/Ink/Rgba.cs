using System;
using System.Globalization;

namespace Kaname.Ink
{
    // A colour with straight (non-premultiplied) alpha, each channel 0..1. Parses the CSS forms the JavaScript art uses, so the port can
    // keep its colours as written: "#5b3cae", "rgba(143,77,255,0.3)" and a bare "143,77,255" triplet with a separate alpha.
    public readonly struct Rgba
    {
        public readonly float R, G, B, A;
        public Rgba(float r, float g, float b, float a = 1f) { R = r; G = g; B = b; A = a; }

        public Rgba WithAlpha(float a) => new Rgba(R, G, B, a);
        public Rgba MulAlpha(float k) => new Rgba(R, G, B, A * k);

        public static Rgba Lerp(Rgba a, Rgba b, float t) => new Rgba(a.R + (b.R - a.R) * t, a.G + (b.G - a.G) * t, a.B + (b.B - a.B) * t, a.A + (b.A - a.A) * t);

        // "#rrggbb" or "#rgb"
        public static Rgba Hex(string s, float alpha = 1f)
        {
            if (s.Length > 0 && s[0] == '#') s = s.Substring(1);
            if (s.Length == 3) s = $"{s[0]}{s[0]}{s[1]}{s[1]}{s[2]}{s[2]}";
            if (s.Length != 6) throw new FormatException("colour: " + s);
            return new Rgba(Convert.ToInt32(s.Substring(0, 2), 16) / 255f, Convert.ToInt32(s.Substring(2, 2), 16) / 255f, Convert.ToInt32(s.Substring(4, 2), 16) / 255f, alpha);
        }

        // "r,g,b" with channels 0..255, and an alpha: the form the JavaScript uses to build `rgba(${rgb},${a})`
        public static Rgba Of(string triplet, double alpha)
        {
            var p = triplet.Split(',');
            if (p.Length != 3) throw new FormatException("colour triplet: " + triplet);
            return new Rgba(F(p[0]) / 255f, F(p[1]) / 255f, F(p[2]) / 255f, (float)alpha);
        }

        // any CSS colour the art uses: "#rrggbb", "rgb(r,g,b)" or "rgba(r,g,b,a)"
        public static Rgba Css(string s)
        {
            s = s.Trim();
            if (s.StartsWith("#")) return Hex(s);
            int open = s.IndexOf('('), close = s.LastIndexOf(')');
            if (open < 0 || close < open) throw new FormatException("colour: " + s);
            var p = s.Substring(open + 1, close - open - 1).Split(',');
            if (p.Length < 3) throw new FormatException("colour: " + s);
            return new Rgba(F(p[0]) / 255f, F(p[1]) / 255f, F(p[2]) / 255f, p.Length > 3 ? F(p[3]) : 1f);
        }

        static float F(string s) => float.Parse(s.Trim(), CultureInfo.InvariantCulture);
    }

    public readonly struct V2
    {
        public readonly double X, Y;
        public V2(double x, double y) { X = x; Y = y; }
        public static V2 operator +(V2 a, V2 b) => new V2(a.X + b.X, a.Y + b.Y);
        public static V2 operator -(V2 a, V2 b) => new V2(a.X - b.X, a.Y - b.Y);
        public static V2 operator *(V2 a, double k) => new V2(a.X * k, a.Y * k);
        public double Dot(V2 o) => X * o.X + Y * o.Y;
        public double Cross(V2 o) => X * o.Y - Y * o.X;
        public double Length => Math.Sqrt(X * X + Y * Y);
        public V2 Normalized() { double l = Length; return l < 1e-12 ? new V2(0, 0) : new V2(X / l, Y / l); }
        public V2 Perp => new V2(-Y, X);
    }
}
