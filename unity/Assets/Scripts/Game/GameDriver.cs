using Kaname.Sim;
using UnityEngine;

namespace Kaname.Game
{
    // The loop of the test environment (js/game.js frame): every frame it reads the controls, runs as many fixed 60 Hz simulation steps as the
    // elapsed time holds, and draws the latest state, blended between the last two steps on a fast display. One of these sits on the "Game"
    // object of the TestStage scene; it builds the stage, the box and the HUD itself, so the scene stays tiny.
    [DisallowMultipleComponent]
    public sealed class GameDriver : MonoBehaviour
    {
        [Tooltip("The numbers of the design. Leave empty to use the built-in defaults.")]
        [SerializeField] KanameTuning tuning;
        [SerializeField] Camera viewCamera;
        [Tooltip("The material the ink drawing is rendered with (a sprite material that takes vertex colours).")]
        [SerializeField] Material inkMaterial;

        public World World { get; private set; }

        FixedClock clock;
        InputReader input;
        StageView stage;
        HeroView hero;
        DummyView dummies;
        DebugHud hud;
        PoseReel reel;

        void OnEnable()
        {
            World = new World(tuning != null ? tuning.values : new Tuning());
            clock = new FixedClock();
            input = new InputReader();
            stage = new StageView(transform);
            dummies = new DummyView(transform, inkMaterial);
            hero = new HeroView(transform, World.K, inkMaterial);
            reel = new PoseReel();
            hud = new DebugHud();
            if (viewCamera == null) viewCamera = Camera.main;
            Application.targetFrameRate = -1;
        }

        void OnDisable()
        {
            input?.Dispose(); input = null;
            stage?.Destroy(); stage = null;
            hero?.Destroy(); hero = null;
            dummies?.Destroy(); dummies = null;
        }

        void Update()
        {
            input.Poll();
            clock.Advance(Time.unscaledDeltaTime, Step, input.State.EndStep);
            Present();
        }

        // one fixed step: the pose reel drives his body, the simulation steps, and the look takes its step with what the body did
        void Step(double dt)
        {
            if (input.State.DevHit(Dev.Pose)) reel.Next(World, hero.Look);
            reel.BeforeStep(World, hero.Look, dt);
            World.Step(dt, input.State);
            if (World.Frozen) return;                                         // hit-stop: the world holds still, and so does his look (its pose, its scarf, its clocks)
            if (World.Hero.JustJumped) hero.Look.Kick(-0.28);                 // a stretch as he leaves the ground
            if (World.Hero.JustDashed) hero.Look.Kick(-0.12);                 // a small stretch as a dash leaves
            if (World.Hero.JustPogoed) hero.Look.Kick(-0.25);                 // a stretch as a down blow bounces him
            hero.Fill(World.Hero);
            reel.Tell(hero.Input);
            hero.Advance(dt);
        }

        void Present()
        {
            var w = World;
            double a = clock.Alpha;
            double camX = Mathx.Lerp(w.CamPrev, w.Cam.X, a);
            double unitsPerPixel = viewCamera != null ? CameraRig.UnitsPerPixel(viewCamera, w.View) : 1 / w.View.Zoom;
            stage.Update(w.View, camX, unitsPerPixel);
            dummies.Draw(w.Dummies, w.View.Debug);
            hero.Draw(w.Hero, a, w.T, w.View.Debug);
            if (viewCamera != null) CameraRig.Apply(viewCamera, w.View, camX, stage.Backdrop(w.View.Night));
        }

        void OnGUI()
        {
            if (World != null && Event.current.type == EventType.Repaint) hud.Draw(World, clock, input, reel);
        }
    }
}
