using System;
using Kaname.Sim;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Kaname.Game
{
    // Reads the keyboard and the PlayStation pad through Unity's Input System and fills a ControlState for the simulation (js/util.js Input).
    // The bindings are written here, in code, so they are reviewed like any other change (a rebinding screen can come later).
    //
    //            keyboard                               pad (DualShock 4 / DualSense through the standard gamepad layout)
    // move       arrows or WASD                         left stick (analog: a light touch walks) or d-pad
    // jump       Space, Z                               ✕
    // dash       Shift, X, K    (tap: dash; hold: sprint)   ○, R1, R2
    // attack     J, C           (the sword)             □
    // parry      L, V                                   L1, L2
    // test keys  G grid, N night, H hints, ` numbers, [ ] zoom, R reset, T toss, P next pose of the pose reel (keyboard only)
    //
    // Known difference from the JavaScript game: the Input System applies its own small dead zone to the stick before ours (0.18 to 0.95).
    public sealed class InputReader : IDisposable
    {
        public readonly ControlState State = new ControlState();

        const float SideThreshold = 0.4f, VerticalThreshold = 0.55f;     // the stick counts as left/right past 0.4 and up/down past 0.55

        readonly InputAction move, jump, dash, attack, parry;

        public InputReader()
        {
            move = new InputAction("Move", InputActionType.Value, expectedControlType: "Vector2");
            AddDigital(move, "<Keyboard>/w", "<Keyboard>/s", "<Keyboard>/a", "<Keyboard>/d");
            AddDigital(move, "<Keyboard>/upArrow", "<Keyboard>/downArrow", "<Keyboard>/leftArrow", "<Keyboard>/rightArrow");
            AddDigital(move, "<Gamepad>/dpad/up", "<Gamepad>/dpad/down", "<Gamepad>/dpad/left", "<Gamepad>/dpad/right");
            move.AddBinding("<Gamepad>/leftStick");

            jump = Button("Jump", "<Keyboard>/space", "<Keyboard>/z", "<Gamepad>/buttonSouth");
            dash = Button("Dash", "<Keyboard>/leftShift", "<Keyboard>/rightShift", "<Keyboard>/x", "<Keyboard>/k",
                "<Gamepad>/buttonEast", "<Gamepad>/rightShoulder", "<Gamepad>/rightTrigger");
            attack = Button("Attack", "<Keyboard>/j", "<Keyboard>/c", "<Gamepad>/buttonWest");
            parry = Button("Parry", "<Keyboard>/l", "<Keyboard>/v", "<Gamepad>/leftShoulder", "<Gamepad>/leftTrigger");

            foreach (var a in new[] { move, jump, dash, attack, parry }) a.Enable();
        }

        static InputAction Button(string name, params string[] paths)
        {
            var a = new InputAction(name, InputActionType.Button);
            foreach (var p in paths) a.AddBinding(p);
            return a;
        }

        // four keys as a stick that is a full push, with no slowing on the diagonals (mode 1 = Digital)
        static void AddDigital(InputAction a, string up, string down, string left, string right)
        {
            a.AddCompositeBinding("2DVector(mode=1)").With("Up", up).With("Down", down).With("Left", left).With("Right", right);
        }

        // once per frame, before the simulation steps
        public void Poll()
        {
            Vector2 m = move.ReadValue<Vector2>();
            State.Push = Mathx.PadCurve(m.x);
            State.SetHeld(Act.Left, m.x < -SideThreshold);
            State.SetHeld(Act.Right, m.x > SideThreshold);
            State.SetHeld(Act.Up, m.y > VerticalThreshold);
            State.SetHeld(Act.Down, m.y < -VerticalThreshold);
            Track(Act.Jump, jump);
            Track(Act.Dash, dash);
            Track(Act.Attack, attack);
            Track(Act.Parry, parry);

            var kb = Keyboard.current;
            if (kb == null) return;
            if (kb.gKey.wasPressedThisFrame) State.PressDev(Dev.Grid);
            if (kb.nKey.wasPressedThisFrame) State.PressDev(Dev.Night);
            if (kb.hKey.wasPressedThisFrame) State.PressDev(Dev.Hints);
            if (kb.backquoteKey.wasPressedThisFrame) State.PressDev(Dev.Debug);
            if (kb.rightBracketKey.wasPressedThisFrame) State.PressDev(Dev.ZoomIn);
            if (kb.leftBracketKey.wasPressedThisFrame) State.PressDev(Dev.ZoomOut);
            if (kb.rKey.wasPressedThisFrame) State.PressDev(Dev.Reset);
            if (kb.tKey.wasPressedThisFrame) State.PressDev(Dev.Toss);
            if (kb.pKey.wasPressedThisFrame) State.PressDev(Dev.Pose);
        }

        void Track(Act act, InputAction action)
        {
            State.SetHeld(act, action.IsPressed());
            if (action.WasPressedThisFrame()) State.Press(act);
        }

        public void Dispose()
        {
            foreach (var a in new[] { move, jump, dash, attack, parry }) { a.Disable(); a.Dispose(); }
        }
    }
}
