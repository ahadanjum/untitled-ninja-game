namespace Kaname.Sim
{
    // The logical actions. The ORDER IS THE BIT ORDER of "held" and "hit" in the golden traces (dev/trace.mjs: ACTIONS): do not reorder.
    public enum Act { Left, Right, Up, Down, Jump, Dash, Attack, Parry }

    // Keys that are not play but the test environment (js/game.js devKeys): grid, night, hints, numbers, zoom, reset. Toss is new in Unity:
    // it lifts him 200 u straight up and lets go, to see him fall (the JavaScript harness did this with T.place). Pose cycles the pose reel (Unity only).
    public enum Dev { Grid, Night, Hints, Debug, ZoomIn, ZoomOut, Reset, Toss, Pose }

    // What the controls say, as the simulation sees them (js/util.js Input): "held" is down now, "hit" is pressed since the last step
    // (it survives until a step consumes it, so a press between two steps is never lost) and MoveX is the horizontal push, -1..1.
    public interface IControls
    {
        bool Held(Act a);
        bool Hit(Act a);
        double MoveX();
        bool DevHit(Dev d);
    }

    // The controls as a plain snapshot. The Unity input reader (Kaname.Game) fills one every frame; the golden-trace tests fill one per
    // recorded step. Call EndStep after each simulation step so the presses are consumed.
    public sealed class ControlState : IControls
    {
        int held, pressed, dev;
        public double Push;                                           // the horizontal push: keys a full push, the stick analog

        public bool Held(Act a) => (held >> (int)a & 1) != 0;
        public bool Hit(Act a) => (pressed >> (int)a & 1) != 0;
        public double MoveX() => Push;
        public bool DevHit(Dev d) => (dev >> (int)d & 1) != 0;

        public void SetHeld(Act a, bool down) { if (down) held |= 1 << (int)a; else held &= ~(1 << (int)a); }
        public void Press(Act a) => pressed |= 1 << (int)a;
        public void PressDev(Dev d) => dev |= 1 << (int)d;
        public void Load(int heldMask, int hitMask, double push) { held = heldMask; pressed = hitMask; Push = push; }
        public void EndStep() { pressed = 0; dev = 0; }
        public void Clear() { held = 0; pressed = 0; dev = 0; Push = 0; }
    }

    // What the controls say while he is stunned or down: nothing (js/hero.js NO_INPUT).
    public sealed class NoControls : IControls
    {
        public static readonly NoControls Instance = new NoControls();
        public bool Held(Act a) => false;
        public bool Hit(Act a) => false;
        public double MoveX() => 0;
        public bool DevHit(Dev d) => false;
    }
}
