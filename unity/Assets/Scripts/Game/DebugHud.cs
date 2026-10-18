using System.Collections.Generic;
using System.Text;
using Kaname.Sim;
using UnityEngine;

namespace Kaname.Game
{
    // The test environment's text (js/game.js drawHud): the height scale at the left edge, the hint row along the bottom, and the numbers (the
    // backquote key). Drawn with Unity's immediate-mode GUI, which is plenty for a test stage. It is not the game's HUD.
    public sealed class DebugHud
    {
        GUIStyle style;

        // IMGUI colours are taken as linear values in a project that uses linear colour space, which makes pale text paler still;
        // converting the sRGB colours we mean gives the colours the JavaScript game draws.
        static Color Ink(string hex) => QualitySettings.activeColorSpace == ColorSpace.Linear ? Pixel.Hex(hex).linear : Pixel.Hex(hex);

        public void Draw(World w, FixedClock clock, InputReader input, PoseReel reel)
        {
            float k = Screen.height / 720f;                                   // the layout is designed on 1280 x 720
            style ??= new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleLeft, wordWrap = false };
            style.fontSize = Mathf.Max(10, Mathf.RoundToInt(12 * k));
            var v = w.View;

            if (v.Grid)                                                        // the height scale: 32, 64, ...
            {
                style.normal.textColor = v.Night ? Ink("#7f8ac0") : Ink("#a5adc0");
                float groundY = (float)(CameraRig.GroundFromTop * Screen.height), px = (float)(Screen.height * v.Zoom / CameraRig.ViewHeight);
                for (int i = 1; i <= Stage.GridLines; i++)
                    GUI.Label(new Rect(14 * k, groundY - i * (float)Stage.Grid * px - 9 * k, 80 * k, 18 * k), (i * 32).ToString(), style);
            }
            if (v.Hints)
            {
                style.normal.textColor = v.Night ? Ink("#7f8ac0") : Ink("#9aa2b6");
                var wrap = new GUIStyle(style) { wordWrap = true, alignment = TextAnchor.LowerLeft };
                GUI.Label(new Rect(18 * k, Screen.height - 72 * k, Screen.width - 36 * k, 60 * k),
                    $"Kaname's look. P next pose (now: {reel.Name})   G grid   N night   [ ] zoom   R reset   T toss him up   H hide this   ` numbers. Arrows or stick run, Space or the cross jumps, Shift or the circle dashes (hold it and a direction to sprint). J, C or the square swings the sword (hold up for an up strike, down in the air for a down strike: it bounces him off what it hits). The charge and parry do not work yet.", wrap);
            }
            if (v.Debug)
            {
                var P = w.Hero;
                var held = new StringBuilder();
                foreach (Act a in System.Enum.GetValues(typeof(Act)))
                    if (input.State.Held(a)) held.Append(a.ToString().ToLowerInvariant()).Append(' ');
                var rows = new List<string>
                {
                    $"{clock.Hz:0} Hz  {(clock.Interp ? "blended" : "direct")}",
                    $"x {P.Cx:0.0}  y {P.Y:0.0}  vx {P.Vx:0}  vy {P.Vy:0}  {(P.OnGround ? "ground" : "air")}  facing {(P.Face > 0 ? "right" : "left")}",
                    $"held: {(held.Length == 0 ? "-" : held.ToString().TrimEnd())}   push {input.State.MoveX():0.00}",
                    $"coyote {P.Coyote:0.000}  buffer {P.JumpBuf:0.000}   apex {P.Apex:0.0} u   last jump {P.LastJump:0.0} u",
                    $"dash {(P.DashT > 0 ? "ON" : "-")}  cooldown {P.DashCd:0.00}  {(P.CanDash ? "ready" : "spent")}   last dash {P.LastDash:0} u",
                    $"sword {(P.Atk != null ? P.Atk.Dir.ToString().ToLowerInvariant() + " " + P.Atk.T.ToString("0.00") : "-")}  cooldown {P.AtkCd:0.00}  blows landed {P.Strikes}  hit-stop {w.Hitstop:0.000}",
                    $"camera {w.Cam.X:0.0} (leads him by {w.K.CameraLead:0} u)   zoom {v.Zoom:0.00}",
                };
                style.normal.textColor = v.Night ? Ink("#cfd8ff") : Ink("#2b3350");
                for (int i = 0; i < rows.Count; i++) GUI.Label(new Rect(18 * k, (14 + i * 18) * k, Screen.width - 36 * k, 20 * k), rows[i], style);
            }
        }
    }
}
