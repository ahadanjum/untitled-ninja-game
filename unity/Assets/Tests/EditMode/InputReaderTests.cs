using Kaname.Game;
using Kaname.Sim;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Kaname.Tests
{
    // Rung U0, the controls: the keyboard and a PlayStation-style pad, through Unity's Input System with virtual devices.
    public class InputReaderTests : InputTestFixture
    {
        Keyboard kb;
        Gamepad pad;
        InputReader reader;

        [SetUp]
        public override void Setup()
        {
            base.Setup();
            kb = InputSystem.AddDevice<Keyboard>();
            pad = InputSystem.AddDevice<Gamepad>();
            reader = new InputReader();
        }

        [TearDown]
        public override void TearDown()
        {
            reader.Dispose();
            base.TearDown();
        }

        ControlState S => reader.State;
        // Press, Release and Set already run an input update, so the frame's presses are visible: the game reads them with Poll
        void Poll() => reader.Poll();
        void NextFrame() { InputSystem.Update(); reader.Poll(); }

        [Test]
        public void ArrowsAndWasdPushLeftAndRightAtFullSpeed()
        {
            Press(kb.dKey); Poll();
            Assert.AreEqual(1, S.MoveX(), 1e-6); Assert.IsTrue(S.Held(Act.Right)); Assert.IsFalse(S.Held(Act.Left));
            Release(kb.dKey); Press(kb.leftArrowKey); Poll();
            Assert.AreEqual(-1, S.MoveX(), 1e-6); Assert.IsTrue(S.Held(Act.Left));
            Release(kb.leftArrowKey); Poll();
            Assert.AreEqual(0, S.MoveX()); Assert.IsFalse(S.Held(Act.Left)); Assert.IsFalse(S.Held(Act.Right));
        }

        [Test]
        public void ADiagonalIsStillAFullPushSideways()
        {
            Press(kb.dKey); Press(kb.wKey); Poll();
            Assert.AreEqual(1, S.MoveX(), 1e-6, "keys are a full push, with no slowing on the diagonal");
            Assert.IsTrue(S.Held(Act.Up)); Assert.IsTrue(S.Held(Act.Right));
        }

        [Test]
        public void UpAndDownComeFromTheKeysAndTheStick()
        {
            Press(kb.sKey); Poll(); Assert.IsTrue(S.Held(Act.Down)); Release(kb.sKey);
            Set(pad.leftStick, new Vector2(0, 1)); Poll(); Assert.IsTrue(S.Held(Act.Up)); Assert.IsFalse(S.Held(Act.Down));
            Set(pad.leftStick, new Vector2(0, -1)); Poll(); Assert.IsTrue(S.Held(Act.Down));
            Set(pad.leftStick, new Vector2(0, 0.3f)); Poll(); Assert.IsFalse(S.Held(Act.Up), "a light touch is not up");
        }

        [Test]
        public void TheStickIsAnalogWithADeadZone()
        {
            Set(pad.leftStick, new Vector2(1, 0)); Poll(); Assert.AreEqual(1, S.MoveX(), 1e-6);
            Set(pad.leftStick, new Vector2(-1, 0)); Poll(); Assert.AreEqual(-1, S.MoveX(), 1e-6);
            Set(pad.leftStick, new Vector2(0.1f, 0)); Poll(); Assert.AreEqual(0, S.MoveX(), "inside the dead zone");
            Set(pad.leftStick, new Vector2(0.6f, 0)); Poll(); Assert.That(S.MoveX(), Is.InRange(0.2, 0.8), "a light touch walks");
            Assert.IsTrue(S.Held(Act.Right));
        }

        [Test]
        public void TheDpadIsAFullPush()
        {
            Press(pad.dpad.right); Poll();
            Assert.AreEqual(1, S.MoveX(), 1e-6); Assert.IsTrue(S.Held(Act.Right));
            Release(pad.dpad.right); Press(pad.dpad.left); Poll();
            Assert.AreEqual(-1, S.MoveX(), 1e-6);
        }

        [Test]
        public void TheActionButtonsOnTheKeyboard()
        {
            foreach (var (key, act) in new (Key, Act)[] {
                (Key.Space, Act.Jump), (Key.Z, Act.Jump),
                (Key.LeftShift, Act.Dash), (Key.RightShift, Act.Dash), (Key.X, Act.Dash), (Key.K, Act.Dash),
                (Key.J, Act.Attack), (Key.C, Act.Attack),
                (Key.L, Act.Parry), (Key.V, Act.Parry) })
            {
                S.Clear(); S.EndStep();
                Press(kb[key]); Poll();
                Assert.IsTrue(S.Held(act), key + " should hold " + act);
                Assert.IsTrue(S.Hit(act), key + " should press " + act);
                Release(kb[key]); Poll(); S.EndStep();
            }
        }

        [Test]
        public void ThePadButtons()
        {
            // the standard gamepad layout: south is ✕, east is ○, west is □ on a DualShock 4 or a DualSense
            Check(pad.buttonSouth, Act.Jump);
            Check(pad.buttonEast, Act.Dash); Check(pad.rightShoulder, Act.Dash);
            Check(pad.buttonWest, Act.Attack);
            Check(pad.leftShoulder, Act.Parry);
        }

        [Test]
        public void TheTriggersAreButtons()
        {
            Set(pad.rightTrigger, 1f); Poll(); Assert.IsTrue(S.Held(Act.Dash)); Assert.IsTrue(S.Hit(Act.Dash));
            Set(pad.rightTrigger, 0f); Poll(); S.EndStep();
            Set(pad.leftTrigger, 1f); Poll(); Assert.IsTrue(S.Held(Act.Parry)); Assert.IsTrue(S.Hit(Act.Parry));
            Set(pad.leftTrigger, 0.2f); Poll(); S.EndStep(); Poll(); Assert.IsFalse(S.Held(Act.Parry), "a light touch of the trigger is not a press");
        }

        [Test]
        public void APressIsKeptUntilTheStepEndsEvenIfTheFrameHasPassed()
        {
            Press(kb.spaceKey); Poll();
            Assert.IsTrue(S.Hit(Act.Jump));
            NextFrame();                                                  // another frame has passed, and no step has run yet
            Assert.IsTrue(S.Hit(Act.Jump), "the press waits for a simulation step to consume it");
            S.EndStep();
            Assert.IsFalse(S.Hit(Act.Jump)); Assert.IsTrue(S.Held(Act.Jump));
        }

        [Test]
        public void TheTestKeys()
        {
            foreach (var (key, dev) in new (Key, Dev)[] {
                (Key.G, Dev.Grid), (Key.N, Dev.Night), (Key.H, Dev.Hints), (Key.Backquote, Dev.Debug),
                (Key.RightBracket, Dev.ZoomIn), (Key.LeftBracket, Dev.ZoomOut), (Key.R, Dev.Reset), (Key.T, Dev.Toss), (Key.P, Dev.Pose) })
            {
                S.Clear(); S.EndStep();
                Press(kb[key]); Poll();
                Assert.IsTrue(S.DevHit(dev), key + " should be " + dev);
                Release(kb[key]); Poll(); S.EndStep();
            }
        }

        void Check(UnityEngine.InputSystem.Controls.ButtonControl button, Act act)
        {
            S.Clear(); S.EndStep();
            Press(button); Poll();
            Assert.IsTrue(S.Held(act), button.name + " should hold " + act);
            Assert.IsTrue(S.Hit(act), button.name + " should press " + act);
            Release(button); Poll(); S.EndStep();
        }
    }
}
