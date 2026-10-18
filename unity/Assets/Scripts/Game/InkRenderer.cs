using System.Collections.Generic;
using Kaname.Ink;
using UnityEngine;

namespace Kaname.Game
{
    // Turns what the ink canvas drew into one mesh and puts it on the screen. The canvas works in world units with y down; the object's scale
    // (1/32, -1/32) makes that Unity units (a tile) with y up, so the drawing code needs no conversion. One draw call, triangles in drawing order.
    public sealed class InkRenderer
    {
        readonly GameObject go;
        readonly Mesh mesh;
        readonly List<Vector3> verts = new List<Vector3>();
        readonly List<Color> colors = new List<Color>();
        readonly List<Vector2> uvs = new List<Vector2>();

        public InkRenderer(Transform parent, string name, Material material, int sortingOrder)
        {
            go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localScale = new Vector3(1f / (float)Pixel.UnitsPerTile, -1f / (float)Pixel.UnitsPerTile, 1f);
            mesh = new Mesh { name = name, indexFormat = UnityEngine.Rendering.IndexFormat.UInt32 };
            mesh.MarkDynamic();
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var r = go.AddComponent<MeshRenderer>();
            r.sharedMaterial = material != null ? material : new Material(Shader.Find("Sprites/Default"));
            r.sortingOrder = sortingOrder;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            r.receiveShadows = false;
        }

        public void Show(InkCanvas c)
        {
            verts.Clear(); colors.Clear(); uvs.Clear();
            for (int i = 0; i < c.Verts.Count; i++)
            {
                var v = c.Verts[i]; var k = c.Colors[i];
                verts.Add(new Vector3((float)v.X, (float)v.Y, 0));
                colors.Add(new Color(k.R, k.G, k.B, k.A));
                uvs.Add(Vector2.zero);
            }
            mesh.Clear();
            mesh.SetVertices(verts);
            mesh.SetColors(colors);
            mesh.SetUVs(0, uvs);
            mesh.SetTriangles(c.Indices, 0, calculateBounds: false);
            mesh.bounds = new Bounds(Vector3.zero, new Vector3(100000, 100000, 10));      // always drawn: the figure is small and the camera sees most of it
        }

        public void Destroy() { Object.Destroy(go); Object.Destroy(mesh); }
    }
}
