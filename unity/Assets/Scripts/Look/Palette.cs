using Kaname.Ink;

namespace Kaname.Look
{
    // The colours of Kaname's look, as js/art_hero.js has them (the outfit black and the band a deep violet, D117). No red or yellow anywhere on him (those mean danger and parry); the blade's glow is violet-black.
    public static class Palette
    {
        public static readonly Rgba Inkc = Rgba.Hex("#0a0a0e"), Rim = Rgba.Hex("#7fa8e8"), Blk = Rgba.Hex("#07090d");
        public static readonly Rgba Band = Rgba.Hex("#3a2273"), BandEdge = Rgba.Hex("#150a30");          // the headband, its tail and the sash: a deep violet (D117)
        public const string BandRgb = "58,34,115";                                                     // the same colour, for the speed lines
        public const string CurseRgb = "143,77,255";
        public static readonly Rgba Hilt = Rgba.Hex("#3a2a70"), HiltLt = Rgba.Hex("#6a52b0"), Steel = Rgba.Hex("#9aa6b8"), Saya = Rgba.Hex("#241a46"), SayaLt = Rgba.Hex("#4a3a80");
        public static readonly Rgba Rope = Rgba.Hex("#b79f78"), RopeDk = Rgba.Hex("#4a3e28");           // the dummies' rope bands
        public static readonly Rgba SlashCore = Rgba.Hex("#f4efff"), SlashBand = Rgba.Hex("#b79cff"), SlashInk = Rgba.Hex("#24143e");   // the slash of a cut (task 2): a bright core, a pale violet band, a thin ink line so it reads on the white stage
        public static readonly Rgba Ink = Rgba.Hex("#1a2140"), InkHead = Rgba.Hex("#262f52"), Wood = Rgba.Hex("#5a4a3a");   // the test dummies' ink (used from rung U4)
    }
}
