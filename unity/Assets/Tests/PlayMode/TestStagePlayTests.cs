using System.Collections;
using System.IO;
using Kaname.Game;
using Kaname.Sim;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
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

        // The Input System drops keyboard events when the application is not the one in front or the Game view has no focus (rules for a person at the
        // keyboard); a test run cannot count on either, so while a keyboard test runs input is accepted whatever has the focus, and the settings are put back.
        static InputSettings.EditorInputBehaviorInPlayMode routed;
        static InputSettings.BackgroundBehavior background;
        static void RouteAllInputToTheGame()
        {
            routed = InputSystem.settings.editorInputBehaviorInPlayMode; background = InputSystem.settings.backgroundBehavior;
            InputSystem.settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
            InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
        }
        static void RestoreInputRouting()
        {
            InputSystem.settings.editorInputBehaviorInPlayMode = routed; InputSystem.settings.backgroundBehavior = background;
        }

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

        // U2, end to end: a keyboard pressed through the Input System reaches the body in the running scene
        [UnityTest]
        public IEnumerator TheKeyboardRunsAndJumpsHimInTheRunningScene()
        {
            var w = driver.World; w.Hero.Place(-3000, 0); w.Cam.X = w.CamPrev = -3000;
            RouteAllInputToTheGame();
            var kb = InputSystem.AddDevice<Keyboard>("U2 test keyboard");
            try
            {
                InputSystem.QueueStateEvent(kb, new KeyboardState(Key.D));                    // (queued: the player loop processes it with the frame, as for a real key)
                yield return new WaitForSeconds(0.5f);
                Assert.AreEqual(240, w.Hero.Vx, 0.01, "holding D runs him at the top speed"); Assert.Greater(w.Hero.Cx, -3000 + 60);
                InputSystem.QueueStateEvent(kb, new KeyboardState(Key.D, Key.Space));
                yield return new WaitForSeconds(0.2f);
                Assert.IsFalse(w.Hero.OnGround, "Space jumps"); Assert.Less(w.Hero.Y, -20);
                InputSystem.QueueStateEvent(kb, new KeyboardState());
                yield return new WaitForSeconds(1.0f);
                Assert.IsTrue(w.Hero.OnGround); Assert.AreEqual(0, w.Hero.Vx, 0.01, "and he stops when the keys are let go");
            }
            finally { InputSystem.RemoveDevice(kb); RestoreInputRouting(); }
        }

        // U3, end to end: a tap of Shift dashes 128 u; Shift and D together sprint at 336 u/s; letting go of Shift drops him back to a run
        [UnityTest]
        public IEnumerator TheKeyboardDashesAndSprintsInTheRunningScene()
        {
            var w = driver.World; w.Hero.Place(-3000, 0); w.Cam.X = w.CamPrev = -3000;
            RouteAllInputToTheGame();
            var kb = InputSystem.AddDevice<Keyboard>("U3 test keyboard");
            try
            {
                InputSystem.QueueStateEvent(kb, new KeyboardState(Key.LeftShift));
                yield return new WaitForSeconds(0.1f);
                InputSystem.QueueStateEvent(kb, new KeyboardState());
                yield return new WaitForSeconds(0.7f);
                Assert.AreEqual(128, w.Hero.LastDash, 1.0, "a tap of Shift dashes 128 u");
                InputSystem.QueueStateEvent(kb, new KeyboardState(Key.D, Key.LeftShift));
                yield return new WaitForSeconds(1.0f);
                Assert.AreEqual(336, w.Hero.Vx, 0.01, "Shift and D together sprint");
                InputSystem.QueueStateEvent(kb, new KeyboardState(Key.D));
                yield return new WaitForSeconds(0.3f);
                Assert.AreEqual(240, w.Hero.Vx, 0.01, "and letting go of Shift drops him back to a run");
                InputSystem.QueueStateEvent(kb, new KeyboardState());
                yield return new WaitForSeconds(0.5f);
                Assert.AreEqual(0, w.Hero.Vx, 0.01);
            }
            finally { InputSystem.RemoveDevice(kb); RestoreInputRouting(); }
        }

        // U4, end to end: a key swings the sword in the running scene, the blow lands on the post and holds the world still for a moment
        [UnityTest]
        public IEnumerator TheKeyboardSwingsTheSwordAndStrikesADummyInTheRunningScene()
        {
            var w = driver.World; w.Reset(); w.Hero.Place(190, 0); w.Cam.X = w.CamPrev = 190;
            var post = w.Dummies[0];
            RouteAllInputToTheGame();
            var kb = InputSystem.AddDevice<Keyboard>("U4 test keyboard");
            try
            {
                InputSystem.QueueStateEvent(kb, new KeyboardState(Key.J));
                yield return new WaitForSeconds(0.1f);
                InputSystem.QueueStateEvent(kb, new KeyboardState());
                yield return new WaitForSeconds(0.5f);
                Assert.AreEqual(1, post.Hits, "J swings the sword and the blow lands on the post once");
                Assert.AreEqual(1, w.Hero.Strikes);
            }
            finally { InputSystem.RemoveDevice(kb); RestoreInputRouting(); }
        }

        [UnityTest]
        public IEnumerator TheDummiesAreDrawnWhereTheSimulationHasThem()
        {
            var w = driver.World; w.Reset(); w.Hero.Place(160, 0); w.Cam.X = w.CamPrev = 160;
            yield return new WaitForSeconds(1.0f);
            var cam = Camera.main;
            var rt = new RenderTexture(W, H, 24, RenderTextureFormat.ARGB32); cam.targetTexture = rt; cam.Render();
            RenderTexture.active = rt; var tex = new Texture2D(W, H, TextureFormat.RGB24, false); tex.ReadPixels(new Rect(0, 0, W, H), 0, 0); tex.Apply(); RenderTexture.active = null;
            Color32 At(double x, double y) { var p = cam.WorldToScreenPoint(new Vector3(Pixel.X(x), Pixel.Y(y), 0)); return tex.GetPixel(Mathf.RoundToInt(p.x), Mathf.RoundToInt(p.y)); }
            AssertColour("the post at x=240 (dark ink)", At(240, -10), "#1a2140");
            AssertColour("the open stage beside it", At(215, -10), "#ffffff");
            cam.targetTexture = null; Object.Destroy(rt); Object.Destroy(tex);
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
