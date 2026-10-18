using Kaname.Sim;
using UnityEngine;

namespace Kaname.Game
{
    // Puts the Unity camera where the simulation's camera is (js/game.js render). The JavaScript view is 1280 x 720 logical pixels at a zoom of
    // 3, so 240 u of height are visible, and the ground sits 78 % of the way down the screen. Here one Unity unit is one tile (32 u).
    public static class CameraRig
    {
        public const double ViewHeight = 720, GroundFromTop = 0.78;

        // how many u one pixel of the real screen covers (the camera shows ViewHeight / zoom u of height)
        public static double UnitsPerPixel(Camera cam, ViewOptions view) => ViewHeight / view.Zoom / System.Math.Max(1, cam.pixelHeight);

        // `camX` is the camera's x in u (already blended between steps)
        public static void Apply(Camera cam, ViewOptions view, double camX, Color backdrop)
        {
            double visible = ViewHeight / view.Zoom;                           // u of height on screen
            cam.orthographic = true;
            cam.orthographicSize = (float)(visible / 2 / Pixel.UnitsPerTile);
            double centreAboveGround = (GroundFromTop - 0.5) * visible;         // the screen's middle is this far above the ground, u
            cam.transform.position = new Vector3(Pixel.X(camX), (float)(centreAboveGround / Pixel.UnitsPerTile), -10);
            cam.backgroundColor = backdrop;
        }
    }
}
