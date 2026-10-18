using System;

namespace Kaname.Sim
{
    // THE NUMBERS ARE THE DESIGN. This is the `K` table of js/hero.js (plus the camera's two numbers from js/game.js), field for field,
    // in Little Shinobi's units: one tile is 32 u, Kaname's body is 16 x 34 u, y = 0 is the ground and up is negative.
    // It is [Serializable] so the KanameTuning asset (Kaname.Game) can hold it and the designer can tune it in the Inspector; the
    // simulation itself only ever sees this plain class. Defaults are the values the designer approved by hand in the JavaScript
    // prototype (tag js-prototype-rung7). Retune on purpose only, and change the matching test in the same change.
    [Serializable]
    public sealed class Tuning
    {
        // the body (K.W, K.H)
        public double BodyW = 16, BodyH = 34;

        // run (K.RUN ... K.AIR_DECEL), jump (K.GRAV ... K.JUMP_BUFFER)
        public double Run = 240;
        public double GroundAccel = 3000, GroundDecel = 3600, Turn = 5200, AirAccel = 2200, AirDecel = 1500;
        public double Grav = 2200, FallMax = 900;
        public double JumpV = 740, JumpCut = 0.42, Coyote = 0.10, JumpBuffer = 0.12;

        // dash and sprint
        public double DashSpeed = 640, DashTime = 0.20, DashCooldown = 0.45, DashBuffer = 0.10, DashExit = 210, Sprint = 336;

        // the sword and the pogo
        public double AttackStartup = 0.03, AttackActive = 0.11, AttackTime = 0.20, AttackCooldown = 0.30, AttackBuffer = 0.12;
        public double Reach = 46, PogoV = 560;

        // the charge and the draw
        public double ChargeStart = 0.25, ChargeReady = 0.62, ChargeSlow = 0.4;
        public double DrawSpeed = 820, DrawTime = 0.16, DrawExit = 110, DrawReach = 62;

        // health and the parry
        public int Hp = 5;
        public double HurtInvuln = 1.0, HurtStun = 0.22, HurtKnock = 300, HurtHop = 220;
        public double[] ParryWindows = { 0.26, 0.16, 0.09, 0.05 };
        public double ParrySpamTime = 0.5, ParryBuffer = 0.12, ParrySlow = 0.3, ParryInvuln = 0.35;

        // hit-stop: how long the whole world holds still
        public double Hitstop = 0.066, HitstopDraw = 0.1, HitstopParry = 0.12, HitstopHurt = 0.1, HitstopCrit = 0.14;

        // the headband's tail: 7 links of 2.4 u (drawn, not simulated)
        public double ScarfSeg = 2.4;

        // the camera (js/game.js): a critically damped spring that leads him by 36 u and settles in about 0.16 s
        public double CameraLead = 36, CameraSmooth = 0.16;
    }
}
