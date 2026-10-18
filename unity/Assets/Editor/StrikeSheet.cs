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
    // Renders the sword at work with no Play mode and no real clock: the real simulation (the sword, the dummies, hit-stop, the pogo) is run with scripted
    // presses for a fixed number of 60 Hz steps, then the frame is drawn with the ink canvas and photographed through the game's own material.
    // Menu: Kaname > Render the strike sheet (or ... with hitboxes).   unity command eval --code 'Kaname.EditorTools.StrikeSheet.Render("/some/folder");'
    public static class StrikeSheet
    {
        sealed class Shot
        {
            public string Name; public int Steps; public Action<World, ControlState, int> Script; public Action<World> Setup;
        }

        static readonly Shot[] Shots =
        {
            new Shot { Name = "forward cut: the blade goes live", Steps = 3, Setup = w => w.Hero.Place(190, 0), Script = (w, c, i) => Press(c, Act.Attack, i, 1) },
            new Shot { Name = "the blow lands: the world holds still (hit-stop)", Steps = 6, Setup = w => w.Hero.Place(190, 0), Script = (w, c, i) => Press(c, Act.Attack, i, 1) },
            new Shot { Name = "the post flinches away", Steps = 12, Setup = w => w.Hero.Place(190, 0), Script = (w, c, i) => Press(c, Act.Attack, i, 1) },
            new Shot { Name = "up strike on the hanging dummy", Steps = 6, Setup = w => w.Hero.Place(-240, 0), Script = (w, c, i) => { c.SetHeld(Act.Up, true); Press(c, Act.Attack, i, 1); } },
            new Shot { Name = "down strike over the post", Steps = 6, Setup = w => w.Hero.Place(240, -62), Script = (w, c, i) => { c.SetHeld(Act.Down, true); Press(c, Act.Attack, i, 1); } },
            new Shot { Name = "the pogo: bounced up at 560 u/s", Steps = 16, Setup = w => w.Hero.Place(240, -62), Script = (w, c, i) => { c.SetHeld(Act.Down, true); Press(c, Act.Attack, i, 1); } },
            new Shot { Name = "the pogo: the top of the bounce", Steps = 30, Setup = w => w.Hero.Place(240, -62), Script = (w, c, i) => { c.SetHeld(Act.Down, true); Press(c, Act.Attack, i, 1); } },
            new Shot { Name = "the stair: bounce off the first dummy", Steps = 14, Setup = w => w.Hero.Place(-240, -112), Script = (w, c, i) => { c.SetHeld(Act.Down, true); Press(c, Act.Attack, i, 1); } },
        };

        static void Press(ControlState c, Act a, int step, int at) { if (step == at) { c.SetHeld(a, true); c.Press(a); } else if (step == at + 1) c.SetHeld(a, false); }

        [MenuItem("Kaname/Render the strike sheet")]
        public static void Run() => Render(Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Logs", "strike-sheet")));

        [MenuItem("Kaname/Render the strike sheet with hitboxes")]
        public static void RunWithBoxes() => Render(Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Logs", "strike-sheet-boxes")), true);

        // `boxes`: also outline the live sword box and the dummies' boxes in pale blue (the developer's debug view; the picture a cut is judged against)
        public static string Render(string folder, bool boxes = false)
        {
            Directory.CreateDirectory(folder);
            foreach (var f in Directory.GetFiles(folder, "*.png")) File.Delete(f);
            var material = AssetDatabase.LoadAssetAtPath<Material>(ProjectSetup.InkMaterialPath);
            var manifest = new List<string>();
            for (int n = 0; n < Shots.Length; n++)
            {
                var shot = Shots[n];
                var t = new Tuning(); var w = new World(t); var look = new HeroLook(t); var li = new LookInput(); var c = new ControlState();
                shot.Setup(w);
                w.Cam.X = w.CamPrev = w.Hero.Cx + 36;
                for (int i = 1; i <= shot.Steps; i++)
                {
                    shot.Script(w, c, i);
                    w.Step(Mathx.Step, c);
                    if (!w.Frozen)
                    {
                        if (w.Hero.JustJumped) look.Kick(-0.28);
                        if (w.Hero.JustDashed) look.Kick(-0.12);
                        if (w.Hero.JustPogoed) look.Kick(-0.25);
                        HeroView.Fill(li, w.Hero); look.Update(li, Mathx.Step);
                    }
                    c.EndStep();
                }
                var canvas = new InkCanvas(); var h = w.Hero;
                canvas.FillStyle(Rgba.Hex("#f2f4f8")); canvas.FillRect(h.Cx - 150, 0, 300, 60); canvas.FillStyle(Rgba.Hex("#2b3350")); canvas.FillRect(h.Cx - 150, -1, 300, 2);
                foreach (var d in w.Dummies) DummyArt.Draw(canvas, d);
                KanameArt.DrawKaname(canvas, look, w.T, h.Cx, h.Y, 1);
                if (boxes)
                {
                    foreach (var d in w.Dummies) Brush.BoxOutline(canvas, d.Box(), 0.4);
                    var blow = h.Blow(); if (blow != null) Brush.BoxOutline(canvas, blow.Box, 0.9);
                }
                string file = $"{n:00}-{System.Text.RegularExpressions.Regex.Replace(shot.Name, "[^A-Za-z0-9]+", "-").Trim('-')}.png";
                File.WriteAllBytes(Path.Combine(folder, file), PoseSheet.Photograph(material, canvas, h.Cx + 30, Math.Min(-35, h.Y - 25), 200, 130));
                manifest.Add($"{file}\t{shot.Name}");
            }
            File.WriteAllLines(Path.Combine(folder, "manifest.tsv"), manifest);
            return folder;
        }
    }
}
