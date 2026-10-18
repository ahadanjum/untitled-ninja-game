using System;
using System.Collections.Generic;
using System.IO;
using Kaname.Game;
using Kaname.Ink;
using Kaname.Look;
using Kaname.Sim;
using UnityEditor;
using UnityEngine;

namespace Kaname.EditorTools
{
    // Renders Kaname's look, frame by frame, with no Play mode and no real clock: each pose of the pose reel is run for a fixed number of 60 Hz steps
    // exactly as the game would, then drawn with the ink canvas and photographed by a preview camera into a PNG. Deterministic, so the same code gives
    // the same picture: the evidence for the look. Menu: Kaname > Render the pose sheet. From a shell / the Editor CLI:
    //   unity command eval --code 'Kaname.EditorTools.PoseSheet.Render("/some/folder");'
    public static class PoseSheet
    {
        // (what to call it, the pose, the step to photograph)
        static readonly (string name, PoseKind kind, int step)[] Frames =
        {
            ("standing", PoseKind.Stand, 90),
            ("running", PoseKind.Run, 40), ("sprinting", PoseKind.Sprint, 60), ("dashing", PoseKind.Dash, 8),
            ("jump: rising", PoseKind.Jump, 8), ("jump: apex", PoseKind.Jump, 22), ("jump: falling", PoseKind.Jump, 33), ("jump: landing", PoseKind.Jump, 44),
            ("sword: forward cut", PoseKind.Sword, 7), ("sword: up strike", PoseKind.SwordUp, 7), ("sword: down strike", PoseKind.SwordDown, 21),
            ("the draw", PoseKind.Draw, 8), ("charging", PoseKind.Charge, 30), ("charge ready", PoseKind.Charge, 80),
            ("the parry's guard", PoseKind.Guard, 6), ("hurt", PoseKind.Hurt, 12), ("down", PoseKind.Down, 40),
        };
        const double W = 110, H = 90, PxPerU = 8;          // the window round him, in u, and the pixels per u of the render

        [MenuItem("Kaname/Render the pose sheet")]
        public static void Run() => Render(Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Logs", "pose-sheet")));

        public static string Render(string folder)
        {
            Directory.CreateDirectory(folder);
            foreach (var f in Directory.GetFiles(folder, "*.png")) File.Delete(f);
            var material = AssetDatabase.LoadAssetAtPath<Material>(ProjectSetup.InkMaterialPath);     // the game's own ink material
            var manifest = new List<string>();
            try
            {
                for (int i = 0; i < Frames.Length; i++)
                {
                    var (name, kind, step) = Frames[i];
                    var canvas = new InkCanvas();
                    var (cx, cy) = Simulate(kind, step, canvas);
                    string file = $"{i:00}-{name.Replace(':', '-').Replace(' ', '-').Replace('\'', '-')}.png";
                    File.WriteAllBytes(Path.Combine(folder, file), Photograph(material, canvas, cx, cy));
                    manifest.Add($"{file}\t{name}");
                }
            }
            finally { }
            File.WriteAllLines(Path.Combine(folder, "manifest.tsv"), manifest);
            return folder;
        }

        // runs the pose for `step` fixed steps as the game does, draws the last frame into the canvas, and returns where he is
        static (double cx, double cy) Simulate(PoseKind kind, int steps, InkCanvas canvas)
        {
            var tuning = new Tuning(); var world = new World(tuning); var look = new HeroLook(tuning); var reel = new PoseReel(); var li = new LookInput();
            int guard = 0; while (reel.Kind != kind && guard++ < 20) reel.Next(world, look);
            if (kind != PoseKind.Stand) { look = new HeroLook(tuning); }                    // a fresh look, so every pose starts from standing
            for (int s = 0; s < steps; s++)
            {
                reel.BeforeStep(world, look, Mathx.Step);
                world.Step(Mathx.Step, NoControls.Instance);
                HeroView.Fill(li, world.Hero);
                reel.Tell(li);
                look.Update(li, Mathx.Step);
            }
            var h = world.Hero;
            canvas.Clear();
            canvas.FillStyle(Rgba.Hex("#f2f4f8")); canvas.FillRect(h.Cx - 100, 0, 200, 60);                                   // the floor and the ground line
            canvas.FillStyle(Rgba.Hex("#2b3350")); canvas.FillRect(h.Cx - 100, -1, 200, 2);
            KanameArt.DrawKaname(canvas, look, world.T, h.Cx, h.Y, 1);
            return (h.Cx, Math.Min(-30, h.Y - 20));                                                                       // the window follows him up when he jumps
        }

        // a camera of its own, on a layer nothing else uses, looks at the mesh through the game's own ink material and pipeline, into a texture
        public static byte[] Photograph(Material material, InkCanvas canvas, double cx, double cy, double winW = W, double winH = H)
        {
            const int Layer = 31;
            var mesh = new Mesh { indexFormat = UnityEngine.Rendering.IndexFormat.UInt32 };
            var verts = new List<Vector3>(); var cols = new List<Color>(); var uvs = new List<Vector2>();
            for (int i = 0; i < canvas.Verts.Count; i++)
            {
                verts.Add(new Vector3((float)canvas.Verts[i].X, (float)canvas.Verts[i].Y, 0));
                var k = canvas.Colors[i]; cols.Add(new Color(k.R, k.G, k.B, k.A)); uvs.Add(Vector2.zero);
            }
            mesh.SetVertices(verts); mesh.SetColors(cols); mesh.SetUVs(0, uvs); mesh.SetTriangles(canvas.Indices, 0);
            int w = (int)(winW * PxPerU), h = (int)(winH * PxPerU);

            var meshGo = new GameObject("PoseSheet mesh") { layer = Layer, hideFlags = HideFlags.HideAndDontSave };
            meshGo.transform.localScale = new Vector3(1f / 32, -1f / 32, 1f);
            meshGo.AddComponent<MeshFilter>().sharedMesh = mesh;
            meshGo.AddComponent<MeshRenderer>().sharedMaterial = material;
            var camGo = new GameObject("PoseSheet camera") { hideFlags = HideFlags.HideAndDontSave };
            var cam = camGo.AddComponent<Camera>();
            cam.orthographic = true; cam.orthographicSize = (float)(winH / 2 / 32); cam.clearFlags = CameraClearFlags.SolidColor; cam.backgroundColor = Color.white;
            cam.nearClipPlane = 0.1f; cam.farClipPlane = 50f; cam.cullingMask = 1 << Layer; cam.enabled = false;
            cam.transform.position = new Vector3((float)(cx / 32), (float)(-cy / 32), -10);
            var rt = new RenderTexture(w, h, 24, RenderTextureFormat.ARGB32) { antiAliasing = 4 };
            cam.targetTexture = rt;
            cam.Render();
            var prev = RenderTexture.active; RenderTexture.active = rt;
            var tex = new Texture2D(w, h, TextureFormat.RGB24, false); tex.ReadPixels(new Rect(0, 0, w, h), 0, 0); tex.Apply();
            RenderTexture.active = prev;
            var bytes = tex.EncodeToPNG();
            UnityEngine.Object.DestroyImmediate(tex); UnityEngine.Object.DestroyImmediate(rt); UnityEngine.Object.DestroyImmediate(camGo); UnityEngine.Object.DestroyImmediate(meshGo); UnityEngine.Object.DestroyImmediate(mesh);
            return bytes;
        }
    }
}
