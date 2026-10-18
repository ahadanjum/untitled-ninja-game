using UnityEngine;

namespace Kaname.Game
{
    // The placeholder look is made of flat coloured rectangles: one white sprite (one world unit wide and tall, its pivot at the top-left
    // corner) that is moved, scaled and tinted. World units (u) of the simulation are Little Shinobi's pixels, one tile is 32 u, and one
    // Unity unit is one tile; y is flipped (the simulation's up is negative).
    public static class Pixel
    {
        public const double UnitsPerTile = 32;

        static Sprite sprite;
        public static Sprite Sprite
        {
            get
            {
                if (sprite == null)
                {
                    var t = Texture2D.whiteTexture;
                    sprite = Sprite.Create(t, new Rect(0, 0, t.width, t.height), new Vector2(0, 1), t.width);
                    sprite.hideFlags = HideFlags.HideAndDontSave;
                }
                return sprite;
            }
        }

        public static float X(double x) => (float)(x / UnitsPerTile);
        public static float Y(double y) => (float)(-y / UnitsPerTile);

        public static SpriteRenderer Quad(Transform parent, string name, int order)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var r = go.AddComponent<SpriteRenderer>();
            r.sprite = Sprite;
            r.sortingOrder = order;
            return r;
        }

        // cover the box whose top-left corner is (x, y) and whose size is (w, h), in the simulation's units
        public static void Place(SpriteRenderer r, double x, double y, double w, double h)
        {
            var t = r.transform;
            t.localPosition = new Vector3(X(x), Y(y), 0);
            t.localScale = new Vector3((float)(w / UnitsPerTile), (float)(h / UnitsPerTile), 1);
        }

        public static Color Hex(string rgb, float alpha = 1f)
        {
            ColorUtility.TryParseHtmlString(rgb, out var c);
            c.a = alpha;
            return c;
        }
    }
}
