using System.Collections;
using System.IO;
using Kaname.Game;
using Kaname.Sim;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Kaname.Tests
{
    // Rung U0 in a running game: the TestStage scene loads, builds the stage and the box, steps the simulation, and draws what the numbers say.
    // The drawing is checked by rendering the camera to a texture and reading pixels at world positions (and the picture is kept as evidence in
    // unity/Logs/evidence/, which Unity's .gitignore leaves out of git).
    public class TestStagePlayTests
    {
        const string SceneName = "TestStage";
        const int W = 1280, H = 720;

        static GameDriver driver;

        [UnitySetUp]
        public IEnumerator LoadTheStage()
        {
            yield return SceneManager.LoadSceneAsync(SceneName);
            yield return null;
            driver = Object.FindFirstObjectByType<GameDriver>();
        }

        [UnityTest]
        public IEnumerator TheSceneHasADriverACameraAndBuildsTheStageAndTheBox()
        {
            Assert.IsNotNull(driver, "no GameDriver in the TestStage scene");
            Assert.IsNotNull(Camera.main, "no main camera");
            Assert.IsNotNull(driver.transform.Find("Stage"), "the stage was not built");
            Assert.IsNotNull(driver.transform.Find("Kaname"), "the box was not built");
            Assert.IsNotNull(driver.World);
            yield return null;
        }

        [UnityTest]
        public IEnumerator TheSimulationAdvancesInRealTimeAndHeStandsOnTheGround()
        {
            float t0 = Time.realtimeSinceStartup;
            double s0 = driver.World.T;
            yield return new WaitForSeconds(0.5f);
            double simulated = driver.World.T - s0, elapsed = Time.realtimeSinceStartup - t0;
            Assert.AreEqual(elapsed, simulated, 0.2, "the simulation should keep pace with the clock (60 Hz)");
            Assert.IsTrue(driver.World.Hero.OnGround);
            Assert.AreEqual(0, driver.World.Hero.Y);
        }

        [UnityTest]
        public IEnumerator HeFallsWhenPlacedInTheAirAndTheCameraFollowsHim()
        {
            var w = driver.World;
            w.Hero.Place(300, -200); w.Cam.X = 300; w.CamPrev = 300;
            yield return new WaitForSeconds(1.2f);
            Assert.IsTrue(w.Hero.OnGround); Assert.AreEqual(0, w.Hero.Y);
            Assert.AreEqual(336, w.Cam.X, 0.5, "the camera leads him by 36 u");
        }

        [Test]
        public void TheProjectDrawsInGammaColourSpaceLikeTheJavaScriptCanvas()
        {
            Assert.AreEqual(ColorSpace.Gamma, QualitySettings.activeColorSpace, "the ink look is drawn and blended in gamma, as the JavaScript canvas does");
        }

        [UnityTest]
        public IEnumerator TheCameraFramesTheStageAsTheJavaScriptGameDoes()
        {
            yield return new WaitForSeconds(0.3f);
            var cam = Camera.main;
            Assert.IsTrue(cam.orthographic);
            Assert.AreEqual(3.75f, cam.orthographicSize, 1e-4, "240 u of height are visible at zoom 3");
            var rt = new RenderTexture(W, H, 24); cam.targetTexture = rt;
            var ground = cam.WorldToViewportPoint(new Vector3(0, 0, 0));
            cam.targetTexture = null; Object.Destroy(rt);
            Assert.AreEqual(1 - 0.78f, ground.y, 0.005, "the ground sits 78 % of the way down the screen");
        }

        [UnityTest]
        public IEnumerator ItDrawsTheStageAndTheBoxWhereTheNumbersSayTheyAre()
        {
            var w = driver.World;
            w.Hero.Place(700, 0); w.Cam.X = 700; w.CamPrev = 700;                      // between the 2- and 3-tile pillars, standing on the ground
            yield return new WaitForSeconds(1.0f);
            yield return null;

            var cam = Camera.main;
            var rt = new RenderTexture(W, H, 24, RenderTextureFormat.ARGB32); cam.targetTexture = rt;
            cam.Render();
            RenderTexture.active = rt;
            var tex = new Texture2D(W, H, TextureFormat.RGB24, false); tex.ReadPixels(new Rect(0, 0, W, H), 0, 0); tex.Apply();
            RenderTexture.active = null;

            Color32 At(double x, double y)                                               // a colour at a world position (u; up is negative)
            {
                var p = cam.WorldToScreenPoint(new Vector3(Pixel.X(x), Pixel.Y(y), 0));
                return tex.GetPixel(Mathf.RoundToInt(p.x), Mathf.RoundToInt(p.y));
            }
            var dir = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Logs", "evidence")); Directory.CreateDirectory(dir);
            File.WriteAllBytes(Path.Combine(dir, "u0-test-stage.png"), tex.EncodeToPNG());
            // (the camera keeps its render texture until the end, so WorldToScreenPoint answers in the texture's pixels)

            AssertColour("Kaname's body (black)", At(700, -8), "#0a0a0e");
            AssertColour("the band across his head (deep violet)", At(696, -27), "#3a2273");
            AssertColour("the 3-tile pillar's fill", At(850, -60), "#e1e6f0");
            AssertColour("the floor under the ground line", At(900, 20), "#f2f4f8");
            AssertColour("the open sky", At(900, -146), "#ffffff");
            bool line = false;                                                           // the ground line is a dark band a pixel or two thick
            for (int dy = -4; dy <= 4 && !line; dy++)
            {
                var p = cam.WorldToScreenPoint(new Vector3(Pixel.X(900), Pixel.Y(0), 0));
                var c = tex.GetPixel(Mathf.RoundToInt(p.x), Mathf.RoundToInt(p.y) + dy);
                line = c.r < 0.4f && c.b < 0.5f;
            }
            Assert.IsTrue(line, "no ground line was drawn");
            for (int i = 1; i <= Stage.GridLines; i++)                                   // all five height lines, 32 u apart (thin lines can vanish at some sizes)
            {
                var p = cam.WorldToScreenPoint(new Vector3(Pixel.X(900), Pixel.Y(-i * Stage.Grid), 0));
                bool found = false;
                for (int dy = -2; dy <= 2 && !found; dy++) found = tex.GetPixel(Mathf.RoundToInt(p.x), Mathf.RoundToInt(p.y) + dy).r < 0.97f;
                Assert.IsTrue(found, $"the {i * 32} u height line was not drawn");
            }
            cam.targetTexture = null;
            Object.Destroy(rt); Object.Destroy(tex);
        }

        static void AssertColour(string what, Color32 got, string hex)
        {
            ColorUtility.TryParseHtmlString(hex, out var want);
            Color32 w = want;
            Assert.That(Mathf.Abs(got.r - w.r) <= 8 && Mathf.Abs(got.g - w.g) <= 8 && Mathf.Abs(got.b - w.b) <= 8,
                $"{what}: drew rgb({got.r},{got.g},{got.b}), wanted {hex} (rgb({w.r},{w.g},{w.b}))");
        }
    }
}
