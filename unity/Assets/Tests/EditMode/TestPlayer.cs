using Kaname.Sim;

namespace Kaname.Tests
{
    // A player for the behaviour tests: a fresh world, the controls, and the buttons, driven the way a person drives the game (press, hold, let go, wait).
    public sealed class Player
    {
        public readonly World W = new World(new Tuning());
        readonly ControlState c = new ControlState();
        public Hero P => W.Hero;
        public Player() { Run(2); }                                       // (a clean start: two steps standing, as the JavaScript reset)

        public void Run(int steps, System.Action<int> each = null)
        {
            for (int i = 0; i < steps; i++) { each?.Invoke(i); W.Step(Mathx.Step, c); c.EndStep(); }
        }
        public void Down(Act a) { c.SetHeld(a, true); c.Press(a); Push(); }
        public void Up(Act a) { c.SetHeld(a, false); Push(); }
        public void Tap(Act a, int steps = 3) { Down(a); Run(steps); Up(a); }
        void Push() { c.Push = (c.Held(Act.Right) ? 1 : 0) - (c.Held(Act.Left) ? 1 : 0); }
        // a stick pushed over by `axis` (-1..1): analog, with the dead zone and the curve
        public void Stick(double axis)
        {
            c.Push = Mathx.PadCurve(axis); c.SetHeld(Act.Left, axis < -0.4); c.SetHeld(Act.Right, axis > 0.4);
        }
        public void Place(double x, double y) { P.Place(x, y); W.Cam.X = W.CamPrev = x; if (P.OnGround) Run(1); }
        public int UntilLanded(int max = 300) { for (int i = 1; i <= max; i++) { Run(1); if (P.OnGround) return i; } return -1; }
    }
}
