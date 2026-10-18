'use strict';
// ---------------------------------------------------------------------------
// hero.js — Kaname's body: movement physics and the sword (rungs 1–7: run, jump, dash, sprint, strike, pogo, charge and draw, health and the parry) and the eased pose state that the drawing reads.
// Units are Little Shinobi's pixels (see stage.js). Fixed 60 Hz step. Nothing in the simulation reads `vis`, `scarf` or `trail`:
// they are drawn, not simulated (a test checks that drawing changes no simulation state).
//
// THE NUMBERS ARE THE DESIGN. They start from Little Shinobi's measured feel (run 240, jump 720, coyote 0.1 s, buffer 0.12 s,
// dash 640 u/s for 0.2 s, sword 0.2 s swing with a live window of 0.03–0.14 s and a 0.3 s cooldown) and are tuned by the designer's hands. If you retune one, change it here AND in dev/tests.js, and write
// it in CLAUDE.md.
// ---------------------------------------------------------------------------
const K = {
  W: 16, H: 34,                       // the body: 16 wide, 34 tall (feet at y)
  RUN: 240,                           // top speed, u/s
  GROUND_ACCEL: 3000,                 // u/s²: 0 -> 240 in 0.08 s
  GROUND_DECEL: 3600,                 // no input: 240 -> 0 in 0.067 s (about 8 u of slide)
  TURN: 5200,                         // against the motion: 240 -> -240 in 0.092 s
  AIR_ACCEL: 2200, AIR_DECEL: 1500,
  GRAV: 2200, FALL_MAX: 900,          // u/s², u/s
  JUMP_V: 740,                        // a held jump rises about 118 u (3.7 tiles): it clears 3 tiles, not 4 (measured; the first game's 720 measured 112 here)
  JUMP_CUT: 0.42,                     // let go while rising: the rise is cut to 42 % (a tap is about 47 u)
  COYOTE: 0.10, JUMP_BUFFER: 0.12,    // s
  DASH_SPEED: 640, DASH_TIME: 0.20,   // a tap of dash: 640 u/s for 0.2 s = 128 u (4 tiles), flat (no gravity), the way he faces or pushes (Little Shinobi's numbers)
  DASH_COOLDOWN: 0.45,                // s from one dash to the next (on the ground; in the air it is one dash per jump, refilled on landing)
  DASH_BUFFER: 0.10,                  // s: a press this early before the cooldown ends still dashes
  DASH_EXIT: 210,                     // the speed it ends at, so it stops crisply instead of sliding
  SPRINT: 336,                        // holding dash while pushing a direction: top speed, 1.4 × run (on the ground and in the air)
  ATTACK_STARTUP: 0.03, ATTACK_ACTIVE: 0.11,   // s: the strike's blade is live from 0.03 s to 0.14 s after the press (Little Shinobi's timing)
  ATTACK_TIME: 0.20, ATTACK_COOLDOWN: 0.30,    // s: a swing lasts 0.2 s; the next can start 0.3 s after the last began
  ATTACK_BUFFER: 0.12,                         // s: a press this early before he can swing is kept
  REACH: 46,                                   // u: a forward strike reaches 46 u beyond his body (a body is 16 u wide, so 54 u from his centre)
  POGO_V: 560,                                 // u/s: a down strike that lands bounces him up at this speed, about 71 u (Little Shinobi's pogo)
  CHARGE_START: 0.25, CHARGE_READY: 0.62,      // s: holding the strike button, a charge builds from 0.25 s after the press and is ready at 0.62 s (Little Shinobi's)
  CHARGE_SLOW: 0.4,                            // he walks at 40 % of his run speed while he charges
  DRAW_SPEED: 820, DRAW_TIME: 0.16, DRAW_EXIT: 110,   // the draw (let go of a ready charge): a lunge at 820 u/s for 0.16 s, about 130 u, flat, ending at 110 u/s
  DRAW_REACH: 62,                              // u: the blade covers 62 u in front of his body as he lunges
  HITSTOP_DRAW: 0.1,                           // s: a blow of the draw holds the world still for 6 steps
  HP: 5,                                       // health: a bar of 5; a plain or yellow hit takes 1, a red one 2 (D082)
  HURT_INVULN: 1.0, HURT_STUN: 0.22,           // s: after a hit he cannot be hurt for 1 s and cannot act for 0.22 s
  HURT_KNOCK: 300, HURT_HOP: 220,              // u/s: and he is thrown back and up a little
  PARRY_WINDOWS: [0.26, 0.16, 0.09, 0.05],     // s: a press opens the parry for 0.26 s; each further press within 0.5 s of the last opens a shorter one (0.16, 0.09, 0.05):
  PARRY_SPAM_TIME: 0.5,                        //    he can always press again (no lockout), but mashing is a bad bet; a clean parry resets it (Little Shinobi's)
  PARRY_BUFFER: 0.12,                          // s: a press made during a hit-stop is kept (and a parry cancels his own swing)
  PARRY_SLOW: 0.3,                             // he moves at 30 % speed with the guard up
  PARRY_INVULN: 0.35,                          // s: a clean parry leaves him untouchable a moment
  HITSTOP_PARRY: 0.12, HITSTOP_HURT: 0.1, HITSTOP_CRIT: 0.14,   // s: the pause at a parry, at being hit, and at a blow on an opened enemy
  HITSTOP: 0.066,                              // s: the whole world holds still for 4 steps when a strike lands
  SCARF_SEG: 2.4,                     // the headband's tail: 7 links of 2.4 u = 17 u (it began as 4.6 u links, 32 u: "a bit too long", then 3.2 u: "shorter still"; D093, D094)
};

// A dash stretches him long and low (Little Shinobi's proportions): 22 % wider, 8 % shorter at full dash.
const DASH_STRETCH = 0.22, DASH_SQUASH = 0.08;

// How far he crouches (u) as he sprints and dashes. Shared by the head frame and the drawing so the two always agree.
const kanameCrouch = v => v.sprint * 1.2 + v.dash * 2 + (v.charge || 0) * 1.6;

// Where Kaname's head and the headband's tail are, in the world, for a given pose. ONE definition, used by the simulation (the
// scarf's root) and by drawing (the head and the band), so the scarf is tied to the back of his head in every frame and can never
// come loose. The body is drawn as: translate(rx, ry), then scale (face, squash, dash stretch), then rotate(lean); the head tilts a
// little as he runs.
function kanameHeadFrame(P, rx, ry) {
  const v = P.vis, run = v.run;
  const bob = Math.sin(P.t * 3) * 0.6 * (1 - run * 0.5), step = Math.abs(Math.sin(P.runT)) * run;
  const hy = -26 + bob * 1.4 - step * 1.6 + kanameCrouch(v) * 1.4;   // the head's centre, in the body's own frame (facing +x)
  const tilt = 0.08 * run, ct = Math.cos(tilt), st = Math.sin(tilt);
  const c = Math.cos(v.lean), s = Math.sin(v.lean), fx = P.face * (1 + v.sq * 0.4 + v.dash * DASH_STRETCH), fy = 1 - v.sq * 0.4 - v.dash * DASH_SQUASH;
  const toWorld = (lx, ly) => ({ x: rx + fx * (lx * c - ly * s), y: ry + fy * (lx * s + ly * c) });
  const rim = (dx, dy) => toWorld(dx * ct - dy * st, dx * st + dy * ct + hy);          // a point on the head, rotated about its centre
  return { hy, head: toWorld(0, hy), root: rim(-12.3, -1) };                           // the band sits 1 u above the head's centre
}

const NO_INPUT = { held: () => false, hit: () => false, moveX: () => 0 };      // what the controls say while he is stunned or down

class Kaname {
  constructor(x = 0, y = 0) {
    this.cx = x; this.y = y; this.px = x; this.py = y;          // position (feet centre) and last step's, for drawing between steps
    this.w = K.W; this.h = K.H;
    this.vx = 0; this.vy = 0; this.face = 1;
    this.onGround = true; this.coyote = K.COYOTE; this.jumpBuf = 0; this.jumping = false;
    this.airStartY = y; this.apex = 0; this.lastJump = 0;        // measured: height above where the feet left the ground
    this.justLanded = false; this.landVy = 0; this.hitX = 0;
    this.dashT = 0; this.dashCd = 0; this.dashBuf = 0; this.canDash = true; this.dashDir = 1;   // dash: time left, cooldown, buffered press, air-dash ready
    this.dashX0 = x; this.lastDash = 0;                          // measured: how far the last dash went
    this.atk = null; this.atkCd = 0; this.atkBuf = 0; this.strikes = 0; this.slashAlt = false;   // the sword: the swing in progress { dir, t, face, hit }, cooldown, buffered press, blows landed
    this.swing = { dir: 'fwd', p: 0, fade: 1 };                  // how far through its arc the blade is (drawn only)
    this.hp = K.HP; this.invuln = 0; this.stun = 0; this.deadT = 0; this.parryT = 0; this.parryRecent = 0; this.parrySpam = 0; this.parryBuf = 0; this.threat = null;   // health, time he cannot be hurt, time he cannot act, time down, the parry window, how recently and how often he has pressed it, a buffered press; where the nearest attacker is (set by the game)
    this.holdT = 0; this.holdLock = false; this.wasHeld = false; this.charge = 0;   // the charge: how long the strike button has been held, whether a dash broke it, the 0..1 charge
    this.runT = 0; this.shadowY = 0; this.t = 0;
    // visual state (eased toward the real state; the simulation never reads it)
    this.vis = { run: 0, air: 0, rise: 0, lean: 0, sq: 0, sqv: 0, dash: 0, sprint: 0, atk: 0, charge: 0 };
    this.blink = 0; this.blinkIn = 2.5; this.scarfTh = 0.3;
    this.trail = [];                                             // where he was while dashing and sprinting (drawn only)
    const r0 = kanameHeadFrame(this, x, y).root;
    this.scarf = { p: Array.from({ length: 8 }, (_, i) => ({ x: r0.x - i * K.SCARF_SEG, y: r0.y, ox: r0.x - i * K.SCARF_SEG, oy: r0.y })) };
  }

  startDash(mx) {
    this.dashDir = mx ? sign(mx) : this.face; this.face = this.dashDir;
    this.dashT = K.DASH_TIME; this.dashCd = K.DASH_COOLDOWN; this.canDash = false; this.dashBuf = 0;
    this.jumping = false; this.vy = 0; this.dashX0 = this.cx;     // (an air dash cancels a rise or a fall)
    this.holdLock = true; this.charge = 0; this.parryT = 0;      // and it breaks a charge (until the button is let go and pressed again) and a parry
    this.vis.sq = -0.12; this.vis.sqv = 0;                       // a small stretch as it leaves
  }

  // the strike's direction is fixed when it starts: up if up is held, down if down is held in the air, otherwise the way he faces
  startAttack() {
    const dir = Input.held('up') ? 'up' : (Input.held('down') && !this.onGround) ? 'down' : 'fwd';
    if (dir === 'fwd') this.slashAlt = !this.slashAlt;                      // forward cuts alternate: high to low, then low to high
    this.atk = { dir, t: 0, face: this.face, alt: this.slashAlt, hit: [], live0: K.ATTACK_STARTUP, live1: K.ATTACK_STARTUP + K.ATTACK_ACTIVE, total: K.ATTACK_TIME };
    this.atkCd = K.ATTACK_COOLDOWN; this.atkBuf = 0;
    this.swing = { dir, alt: this.slashAlt, p: 0, fade: 0 };
  }
  // the draw: let go of a ready charge and he lunges forward in one cut, flat, the blade out in front of him
  startDraw() {
    this.atk = { dir: 'draw', t: 0, face: this.face, alt: false, hit: [], live0: 0, live1: K.DRAW_TIME, total: K.DRAW_TIME + 0.12 };
    this.atkCd = Math.max(this.atkCd, K.ATTACK_COOLDOWN); this.atkBuf = 0; this.charge = 0; this.holdLock = true; this.jumping = false; this.vy = 0;
    this.swing = { dir: 'draw', alt: false, p: 0, fade: 0 };
  }
  // true from the press until the blade stops being live: he cannot turn, dash or start another swing in this part of it
  swinging() { return !!this.atk && this.atk.t < this.atk.live1 - 1e-6; }
  // committed to the cut: the blade is live and has hit nothing yet. Once a blow lands he is free to dash or turn at once (attack-to-reset)
  committed() { return this.swinging() && (this.atk.dir === 'draw' || this.atk.hit.length === 0); }   // (a lunge is committed to the end)
  // the box the blade covers right now, in the world (null when it is not live)
  swordBox() {
    const a = this.atk; if (!a || a.t < a.live0 - 1e-6 || !this.swinging()) return null;
    const f = a.face, x0 = this.cx + f * this.w / 2;
    if (a.dir === 'draw') return { dir: 'draw', x: f > 0 ? this.cx - 4 : this.cx - 8 - K.DRAW_REACH, w: 12 + K.DRAW_REACH, y: this.y - 46, h: 46 };   // all of him and 62 u ahead, as he lunges
    if (a.dir === 'up') return { dir: 'up', x: this.cx - 30, y: this.y - 70, w: 60, h: 40 };           // above and around his head
    if (a.dir === 'down') return { dir: 'down', x: this.cx - 26, y: this.y - 6, w: 52, h: 40 };       // below and around his feet
    return { dir: 'fwd', x: f > 0 ? x0 : x0 - K.REACH, y: this.y - 40, w: K.REACH, h: 38 };           // in front, from the knee to the head
  }
  // the blow in progress that can hit things this step: its box, who it has already struck, and how hard (the game resolves it)
  blow() { const box = this.swordBox(); return box ? { box, dir: box.dir, face: this.atk.face, hit: this.atk.hit, power: box.dir === 'draw' ? 3 : 1 } : null; }
  // a blow of his has landed (the game calls this): any blow struck in the air gives back his air dash, and a down strike is a pogo: it
  // bounces him up, cancelling the fall, as if he had pushed off the thing he hit, with a fresh jump's worth of control
  onStrike(dir) {
    this.strikes++;
    if (!this.onGround) this.canDash = true;
    if (dir === 'down') {
      this.vy = -K.POGO_V; this.jumping = false; this.coyote = 0; this.airStartY = this.y; this.apex = 0;   // (apex now measures the bounce)
      this.vis.sq = -0.25; this.vis.sqv = 0;
    }
  }
  // true while he cannot be hurt: after a hit, after a clean parry, and all through a dash (it is the dodge)
  invulnerable() { return this.invuln > 0 || this.dashT > 0 || this.deadT > 0; }
  // the box round him in which an attack is turned aside while the parry is open: both sides, so it works wherever the attacker stands
  parryBox() { return { x: this.cx - 46, y: this.y - 46, w: 92, h: 46 }; }
  // an attack of `dmg` has hit him from the side of `fromX`: he is hurt and thrown back, and cannot act for a moment (D082)
  hurt(dmg, fromX) {
    this.hp = Math.max(0, this.hp - dmg);
    this.invuln = K.HURT_INVULN; this.stun = K.HURT_STUN; this.onGround = false; this.jumping = false;
    this.vx = (this.cx >= fromX ? 1 : -1) * K.HURT_KNOCK; this.vy = -K.HURT_HOP;
    this.atk = null; this.charge = 0; this.holdLock = true; this.dashT = 0; this.parryT = 0;
    this.vis.sq = 0.3; this.vis.sqv = 0;
    if (this.hp <= 0) { this.deadT = 1.4; this.stun = 1.4; }      // down for a moment, then up again with full health (a placeholder: death is not designed yet)
  }
  // an attack of class `cls` has been turned aside by the parry
  parried(cls) {
    this.parryT = 0; this.parryRecent = 0; this.parrySpam = 0; this.invuln = Math.max(this.invuln, K.PARRY_INVULN); this.canDash = true;      // (a clean parry resets the mashing penalty)
    this.vis.sq = 0.12; this.vis.sqv = 0;
  }
  // hit-stop: the world holds still, but presses made meanwhile are kept for when it moves again
  freeze() {
    this.px = this.cx; this.py = this.y;
    if (Input.hit('jump')) this.jumpBuf = K.JUMP_BUFFER;
    if (Input.hit('dash')) this.dashBuf = K.DASH_BUFFER;
    if (Input.hit('attack')) this.atkBuf = K.ATTACK_BUFFER;
    if (Input.hit('parry')) this.parryBuf = K.PARRY_BUFFER;
  }

  update(dt) {
    this.px = this.cx; this.py = this.y;
    const wasGround = this.onGround;
    const inp = this.stun > 0 || this.deadT > 0 ? NO_INPUT : Input;                // stunned or down, he does not hear the controls
    this.stun = Math.max(0, this.stun - dt); this.invuln = Math.max(0, this.invuln - dt);
    if (this.deadT > 0) { this.deadT -= dt; if (this.deadT <= 0) { this.deadT = 0; this.hp = K.HP; this.invuln = 1.5; } }
    const mx = inp.moveX(), dashHeld = inp.held('dash');
    if (this.atk) { this.atk.t += dt; if (this.atk.t >= this.atk.total - 1e-6) this.atk = null; }
    const held = inp.held('attack');
    if (this.atk && this.atk.dir === 'draw' && !this.swinging() && !this.atk.ended) { this.atk.ended = true; this.vx = this.atk.face * K.DRAW_EXIT; }   // the lunge is over

    // ---- dash: a press starts it (a press up to 0.1 s early is kept); on the ground only the cooldown limits it, in the air it is one per jump ----
    this.dashCd = Math.max(0, this.dashCd - dt);
    this.dashBuf = inp.hit('dash') ? K.DASH_BUFFER : Math.max(0, this.dashBuf - dt);
    if (this.dashBuf > 0 && this.dashCd <= 0 && this.canDash && this.dashT <= 0 && !this.committed()) { this.atk = null; this.startDash(mx); }   // a dash cuts a swing's recovery short, or its blade once it has landed, never a blade still looking for its mark
    const dashing = this.dashT > 0;

    // ---- parry: a press opens a window in which an attack that would land on him is turned aside; the next press must wait ----
    this.parryRecent = Math.max(0, this.parryRecent - dt); this.parryT = Math.max(0, this.parryT - dt);
    this.parryBuf = inp.hit('parry') ? K.PARRY_BUFFER : Math.max(0, this.parryBuf - dt);
    if (this.parryBuf > 0 && !dashing && !(this.atk && this.atk.dir === 'draw' && this.swinging())) {      // (he can press at any moment; a parry cancels a swing, even a live blade; only a lunge is committed)
      this.parrySpam = this.parryRecent > 0 ? Math.min(this.parrySpam + 1, K.PARRY_WINDOWS.length - 1) : 0;                   // pressing again within 0.5 s gets a shorter window
      this.parryT = K.PARRY_WINDOWS[this.parrySpam]; this.parryRecent = K.PARRY_SPAM_TIME; this.parryBuf = 0; this.atk = null; this.charge = 0; this.holdLock = true;
      if (this.threat !== null) this.face = this.threat >= this.cx ? 1 : -1;                                                  // and he turns to whoever is attacking him
    }

    // ---- sword: a press (kept 0.12 s) starts a strike: forward, up (hold up), or down (hold down in the air); not while dashing ----
    this.atkCd = Math.max(0, this.atkCd - dt);
    this.atkBuf = inp.hit('attack') ? K.ATTACK_BUFFER : Math.max(0, this.atkBuf - dt);
    if (this.atkBuf > 0 && this.atkCd <= 0 && !this.atk && !dashing && this.parryT <= 0) this.startAttack();

    // ---- charge and draw: hold the strike button and a charge builds, the hand on the hilt; let go once it is ready and he lunges, drawing the blade in one cut ----
    if (inp.hit('attack')) { this.holdT = 0; this.holdLock = false; }
    if (!held) { this.holdT = 0; this.holdLock = false; } else if (!this.holdLock) this.holdT += dt;
    if (this.wasHeld && !held && this.charge >= 1 && !this.atk && !dashing) this.startDraw();
    this.charge = (held && !this.holdLock && this.holdT >= K.CHARGE_START && !this.atk && !dashing) ? clamp((this.holdT - K.CHARGE_START) / (K.CHARGE_READY - K.CHARGE_START), 0, 1) : 0;
    this.wasHeld = held;
    const lunge = !!this.atk && this.atk.dir === 'draw' && this.swinging();
    let dashEnds = false, jumped = false;

    // ---- horizontal: the keys are a full push, the stick is analog; holding dash raises the top speed (sprint) ----
    if (lunge) {
      this.vx = this.atk.face * K.DRAW_SPEED;
    } else if (dashing) {
      this.dashT -= dt; this.vx = this.dashDir * K.DASH_SPEED;
      if (this.dashT <= 1e-6) { this.dashT = 0; dashEnds = true; }
    } else {
      const target = mx * (this.parryT > 0 ? K.RUN * K.PARRY_SLOW : this.charge > 0 ? K.RUN * K.CHARGE_SLOW : dashHeld ? K.SPRINT : K.RUN);        // a charge or a raised guard slows him right down
      let acc;
      if (mx === 0) acc = wasGround ? K.GROUND_DECEL : K.AIR_DECEL;
      else if (this.vx * mx < 0) acc = wasGround ? K.TURN : K.AIR_ACCEL * 1.4;
      else if (Math.abs(this.vx) > Math.abs(target)) acc = wasGround ? K.GROUND_DECEL : K.AIR_DECEL;
      else acc = wasGround ? K.GROUND_ACCEL : K.AIR_ACCEL;
      this.vx = approach(this.vx, target, acc * dt);
      if (mx && !this.committed()) this.face = sign(mx);
    }

    // ---- jump: coyote time, a buffered press, a variable height (a jump also cuts a dash short) ----
    this.coyote = wasGround ? K.COYOTE : Math.max(0, this.coyote - dt);
    this.jumpBuf = inp.hit('jump') ? K.JUMP_BUFFER : Math.max(0, this.jumpBuf - dt);
    if (this.jumpBuf > 0 && this.coyote > 0 && !lunge) {
      this.vy = -K.JUMP_V; this.jumpBuf = 0; this.coyote = 0; this.jumping = true;
      this.onGround = false; this.airStartY = this.y; this.apex = 0;
      this.vis.sq = -0.28; this.vis.sqv = 0;                          // a stretch as he leaves the ground
      jumped = true; if (dashing) { this.dashT = 0; dashEnds = true; }
    }
    if (this.jumping && this.vy < 0 && !inp.held('jump')) { this.vy *= K.JUMP_CUT; this.jumping = false; }
    if (this.vy >= 0) this.jumping = false;
    if ((dashing || lunge) && !jumped) this.vy = wasGround ? K.GRAV * dt : 0;   // a dash and a lunge are flat (on the ground they stay pressed to the floor)
    else this.vy = Math.min(this.vy + K.GRAV * dt, K.FALL_MAX);

    // ---- move and collide: x first, then y ----
    const S = Stage.solids;
    this.hitX = 0;
    this.cx += this.vx * dt;
    for (const s of S) if (Stage.overlaps(this.box(), s)) {
      if (this.vx > 0) this.cx = s.x - this.w / 2; else if (this.vx < 0) this.cx = s.x + s.w + this.w / 2;
      this.hitX = sign(this.vx); this.vx = 0;
    }
    if (this.hitX && dashing) { this.dashT = 0; dashEnds = true; }   // a wall ends a dash
    if (this.hitX && lunge) this.atk.t = this.atk.live1;               // and a lunge
    this.onGround = false; this.justLanded = false;
    this.y += this.vy * dt;
    for (const s of S) if (Stage.overlaps(this.box(), s)) {
      if (this.vy > 0) { this.y = s.y; this.landVy = this.vy; this.vy = 0; this.onGround = true; }
      else if (this.vy < 0) { this.y = s.y + s.h + this.h; this.vy = 0; this.jumping = false; }
    }
    if (dashEnds) {                                                   // it ends crisply; with dash still held and the push kept, it runs on into a sprint
      if (!this.hitX) this.vx = this.dashDir * (dashHeld && mx === this.dashDir ? K.SPRINT : K.DASH_EXIT);
      this.lastDash = Math.abs(this.cx - this.dashX0);
    }
    if (wasGround && !this.onGround && !this.jumping) { this.airStartY = this.y; this.apex = 0; }   // walked off an edge
    if (!this.onGround) this.apex = Math.max(this.apex, this.airStartY - this.y);
    if (!wasGround && this.onGround) { this.justLanded = true; this.lastJump = this.apex; this.onLand(this.landVy); }
    if (this.onGround) { this.canDash = true; this.runT += (Math.abs(this.vx) / K.RUN) * dt * 14; }

    this.t += dt;
    this.shadowY = Stage.floorBelow(this.cx, this.y);
    this.updateVis(dt);
    this.updateTrail(dt, dashing || lunge);
    this.updateScarf(dt);
  }

  box() { return { x: this.cx - this.w / 2, y: this.y - this.h, w: this.w, h: this.h }; }

  onLand(vy) { this.vis.sq = clamp(vy / 900, 0.25, 1) * 0.42; this.vis.sqv = 0; }

  // ---- the pose, eased toward the real state so nothing pops ----
  updateVis(dt) {
    const v = this.vis, speed = Math.abs(this.vx) / K.RUN, dashing = this.dashT > 0 || (!!this.atk && this.atk.dir === 'draw' && this.swinging());
    v.run = approach(v.run, this.onGround ? Math.min(speed, 1.4) : Math.min(speed, 1) * 0.4, dt * 9);
    v.air = approach(v.air, this.onGround ? 0 : 1, dt * 14);
    v.rise = approach(v.rise, this.vy < 0 ? 1 : 0, dt * 10);
    v.dash = approach(v.dash, dashing ? 1 : 0, dt * 30);                                  // the quick one: about two frames
    const sw = this.swing, a = this.atk;                                                  // the blade's progress along its arc
    if (a) { sw.dir = a.dir; sw.alt = a.alt; sw.p = clamp(a.t / (a.dir === 'draw' ? K.DRAW_TIME : 0.19), 0, 1); sw.fade = clamp((a.t - a.live1) / (a.total - a.live1), 0, 1); }
    else sw.fade = 1;
    if (this.parryT > 0) { sw.dir = 'guard'; sw.alt = false; sw.p = 1; sw.fade = 0; }       // the guard: the blade held up in front of him
    v.atk = approach(v.atk, (a && sw.fade < 0.6) || this.parryT > 0 ? 1 : 0, dt * 24);       // the arm comes up to the swing and back to the hilt
    v.charge = approach(v.charge, this.charge, dt * 10);                                  // he coils as the charge builds
    const sprintT = clamp((Math.abs(this.vx) - K.RUN) / (K.SPRINT - K.RUN), 0, 1) * (dashing ? 0 : 1) * (this.onGround ? 1 : 0.6);
    v.sprint = approach(v.sprint, sprintT, dt * 7);                                       // faster than a run, so it builds visibly
    const leanT = clamp(this.vx / K.RUN, -1, 1) * 0.13 * this.face + v.sprint * 0.2 + v.dash * 0.36 + v.atk * (sw.dir === 'up' ? -0.04 : sw.dir === 'down' ? 0.06 : 0.1) - v.charge * 0.1 - (this.deadT > 0 ? 1.3 : 0);   // into the run, further into a sprint, furthest in a dash
    v.lean = lerp(v.lean, leanT, 1 - Math.exp(-dt * 16));
    // squash and stretch: a damped spring back to rest
    v.sqv += (-380 * v.sq - 24 * v.sqv) * dt; v.sq += v.sqv * dt;
    this.blinkIn -= dt;
    if (this.blinkIn <= 0) { this.blink = 0.14; this.blinkIn = 2.2 + Math.random() * 3; }
    this.blink = Math.max(0, this.blink - dt);
  }

  // ---- the trail the speed lines are drawn from: where he was over the last 0.24 s of a dash, 0.12 s of a sprint (drawn only) ----
  updateTrail(dt, dashing) {
    const tr = this.trail;
    for (const g of tr) g.life -= dt;
    while (tr.length && tr[0].life <= 0) tr.shift();
    if (dashing || this.vis.sprint > 0.6) {                      // a sample every step, so the lines' strength is steady (never pulsing)
      const life = dashing ? 0.24 : 0.12;
      tr.push({ x: this.cx, life, max: life, k: dashing ? 0.42 : 0.16 });
    }
  }

  // ---- the scarf: the headband's tail, a chain of 8 points tied to the back of the head, in world space ----
  // Each point chases where a resting scarf would be (streaming back and down, flatter the faster he runs, trailing below him on the
  // way up and lifting on the way down, with a ripple that grows toward the tip), and the tip lags more than the root. Then the
  // segments are held to a fixed length. Chasing a target (instead of drifting) means it always comes back to a sensible shape.
  updateScarf(dt) {
    const p = this.scarf.p, f = this.face, s = Math.min(1, Math.abs(this.vx) / K.RUN), SEG = K.SCARF_SEG, v = this.vis;
    const root = kanameHeadFrame(this, this.cx, this.y).root;                    // tied to the back of the head, at the headband
    p[0].ox = p[0].x; p[0].oy = p[0].y; p[0].x = root.x; p[0].y = root.y;
    const droop = lerp(0.5, 0, s);                               // it curves down toward the tip at rest, streams flat at speed
    let thT = lerp(0.3, 0.1, s);                                 // radians below horizontal: hangs back and down at rest, flat at speed
    thT += clamp(-this.vy / 740, 0, 1) * 0.6;                    // rising: it trails below him
    thT -= clamp(this.vy / 900, 0, 1) * 0.9;                     // falling: it lifts above him
    thT -= v.sprint * 0.1 + v.dash * 0.3;                        // a sprint or a dash blows it dead straight, a little above the line of his back
    this.scarfTh = lerp(this.scarfTh, thT, 1 - Math.exp(-dt * 10));   // eased, so it swings back gracefully on a landing instead of snapping
    const th = this.scarfTh, ripple = (0.12 + 0.18 * s) * (1 - v.dash * 0.7);
    let tx = p[0].x, ty = p[0].y;
    for (let i = 1; i < p.length; i++) {
      const c = p[i], q = p[i - 1]; c.ox = c.x; c.oy = c.y;
      const a = th + droop * (i / (p.length - 1)) + Math.sin(this.t * 7 + i * 0.9) * ripple * (i / (p.length - 1));
      tx += -f * Math.cos(a) * SEG; ty += Math.sin(a) * SEG;
      const k = Math.max(0.1, 0.5 - i * 0.05);                   // the tip lags more than the root
      c.x += (tx - c.x) * k; c.y += (ty - c.y) * k;
      const dx = c.x - q.x, dy = c.y - q.y, d = Math.hypot(dx, dy) || 1e-6;
      c.x = q.x + dx / d * SEG; c.y = q.y + dy / d * SEG;
      if (c.y > this.shadowY - 0.6) c.y = this.shadowY - 0.6;    // never below the floor
    }
  }
}
