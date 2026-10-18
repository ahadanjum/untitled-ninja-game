using Kaname.Look;
using Kaname.Sim;

namespace Kaname.Game
{
    public enum PoseKind { Stand, Run, Sprint, Dash, Jump, Sword, SwordUp, SwordDown, Draw, Charge, Guard, Hurt, Down }

    // A test aid for judging Kaname's LOOK before his mechanics exist (the P key): it makes him do each thing the look has a pose for, on a loop,
    // by driving his body and by telling the look what the mechanics would say (a dash, a swing, a charge, a guard, a hit). Nothing here is a
    // mechanic: when run, jump, dash and the sword are ported (U2 to U6) the real ones replace it. In the JavaScript game the designer saw all of
    // these by playing.
    public sealed class PoseReel
    {
        public PoseKind Kind = PoseKind.Stand;
        public static readonly string[] Names =
        {
            "standing", "running", "sprinting", "dashing", "jumping", "sword: forward cuts", "sword: up strike", "sword: down strike (in the air)",
            "the draw", "charging", "the parry's guard", "hurt (blinking)", "down",
        };
        public string Name => Names[(int)Kind];

        const double Edge = 360;                         // he turns round at this x, either side of the start
        double t; int cycle; int dir = 1;
        double dashT, charge, parryT, invuln, deadT; bool trail; LookAtk atk;
        readonly LookAtk swing = new LookAtk();

        public void Next(World w, HeroLook look)
        {
            Kind = (PoseKind)(((int)Kind + 1) % Names.Length);
            t = 0; cycle = 0; dir = 1; dashT = charge = parryT = invuln = deadT = 0; trail = false; atk = null;
            var h = w.Hero; h.Place(0, 0); h.Vx = 0; h.Face = 1; w.Cam.X = w.CamPrev = 0;
        }

        // before the simulation's step: drive his body
        public void BeforeStep(World w, HeroLook look, double dt)
        {
            var h = w.Hero; double ph;
            dashT = charge = parryT = invuln = deadT = 0; trail = false; atk = null;
            t += dt;
            switch (Kind)
            {
                case PoseKind.Stand: h.Vx = 0; break;
                case PoseKind.Run: Patrol(h, 240); break;
                case PoseKind.Sprint: Patrol(h, 336); break;
                case PoseKind.Dash:
                    ph = t % 1.0; if (ph < dt * 1.5) { cycle++; look.Kick(-0.12); }
                    if (ph < 0.2) { dashT = 0.2 - ph; trail = true; h.Vx = dir * 640; } else h.Vx = 0;
                    TurnAtTheEdge(h);
                    break;
                case PoseKind.Jump:
                    h.Vx = 0; ph = t % 1.4;
                    if (ph < dt * 1.5) { h.Vy = -740; h.OnGround = false; look.Kick(-0.28); }
                    break;
                case PoseKind.Sword: case PoseKind.SwordUp:
                    h.Vx = 0; ph = t % 0.8; if (ph < dt * 1.5) cycle++;
                    if (ph < 0.2) Swing(Kind == PoseKind.Sword ? SwingDir.Fwd : SwingDir.Up, cycle % 2 == 0, ph, 0.14, 0.2);
                    break;
                case PoseKind.SwordDown:
                    h.Vx = 0; ph = t % 1.4;
                    if (ph < dt * 1.5) { h.Vy = -560; h.OnGround = false; look.Kick(-0.28); }
                    if (ph >= 0.25 && ph < 0.45) Swing(SwingDir.Down, false, ph - 0.25, 0.14, 0.2);
                    break;
                case PoseKind.Draw:
                    ph = t % 1.2; if (ph < dt * 1.5) cycle++;
                    if (ph < 0.28) { Swing(SwingDir.Draw, false, ph, 0.16, 0.28); }
                    if (ph < 0.16) { trail = true; h.Vx = dir * 820; } else h.Vx = 0;
                    TurnAtTheEdge(h);
                    break;
                case PoseKind.Charge:
                    h.Vx = 0; ph = t % 2.4;
                    charge = ph < 1.6 ? Mathx.Clamp((ph - 0.25) / (0.62 - 0.25), 0, 1) : 0;
                    break;
                case PoseKind.Guard:
                    h.Vx = 0; ph = t % 0.8; parryT = ph < 0.26 ? 0.26 - ph : 0;
                    break;
                case PoseKind.Hurt:
                    ph = t % 1.6;
                    if (ph < dt * 1.5) { h.Vx = -h.Face * 300; h.Vy = -220; h.OnGround = false; look.Kick(0.3); }
                    else if (h.OnGround) h.Vx = 0;
                    invuln = ph < 1.0 ? 1.0 - ph : 0;
                    break;
                case PoseKind.Down:
                    ph = t % 3.0;
                    if (ph < dt * 1.5) { h.Vx = -h.Face * 300; h.Vy = -220; h.OnGround = false; look.Kick(0.3); }
                    else if (h.OnGround) h.Vx = 0;
                    deadT = ph < 1.4 ? 1.4 - ph : 0;
                    break;
            }
        }

        // after the simulation's step: what the mechanics would tell the look
        public void Tell(LookInput li)
        {
            li.DashT = dashT; li.TrailActive = trail; li.Atk = atk; li.Charge = charge; li.ParryT = parryT; li.Invuln = invuln; li.DeadT = deadT;
        }

        void Swing(SwingDir d, bool alt, double at, double live1, double total)
        {
            swing.Dir = d; swing.Alt = alt; swing.T = at; swing.Live1 = live1; swing.Total = total; atk = swing;
        }

        void Patrol(Hero h, double speed)
        {
            TurnAtTheEdge(h);
            h.Vx = dir * speed;
        }

        void TurnAtTheEdge(Hero h)
        {
            if (h.Cx > Edge) dir = -1; else if (h.Cx < -Edge) dir = 1;
            h.Face = dir;
        }
    }
}
