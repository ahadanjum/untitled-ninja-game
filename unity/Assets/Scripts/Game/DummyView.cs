using System.Collections.Generic;
using Kaname.Ink;
using Kaname.Look;
using Kaname.Sim;
using UnityEngine;

namespace Kaname.Game
{
    // The test dummies on the screen: drawn with the ink canvas into one mesh behind Kaname, each frame, from where the simulation has them.
    public sealed class DummyView
    {
        readonly InkCanvas canvas = new InkCanvas();
        readonly InkRenderer ink;

        public DummyView(Transform parent, Material material) { ink = new InkRenderer(parent, "Dummies", material, 8); }

        public void Draw(List<Dummy> dummies, bool boxes = false)
        {
            canvas.Clear();
            foreach (var d in dummies) DummyArt.Draw(canvas, d);
            if (boxes) foreach (var d in dummies) Brush.BoxOutline(canvas, d.Box(), 0.4);           // the debug view (the ` key): the boxes a blow is judged against, fainter than the blade's
            ink.Show(canvas);
        }

        public void Destroy() => ink.Destroy();
    }
}
