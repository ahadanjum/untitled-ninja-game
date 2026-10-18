'use strict';
// ---------------------------------------------------------------------------
// striker.js — the test attacker for the parry (rung 7): a sparring figure that stands its ground, turns to face him, and strikes in a fixed
// order of three kinds of attack, each telling you what it is by colour (D010), shape and timing, never by a marker over its head:
//   plain   no glint. Any answer works; a parry only turns it aside for a moment (no big opening).
//   yellow  a yellow glint flashes at the tip of its blade just before the cut. Parry it and it is left wide open (the big opening, D083).
//   red     a red glint flashes at its blade, and it crouches for a low sweep. It cannot be parried: jump it or dash through it.
// The glint is the whole telegraph (as in Ghost of Tsushima): no markers or circles, and the blade and eyes are never lit up.
// It cannot be hurt or killed (blows only make it flinch, and hit an opened one harder): it exists to teach the parry its timing. Not solid.
// ---------------------------------------------------------------------------
const STRIKES = {   // wind: the wind-up, arrive: how long into the cut its blade reaches him, live: how long the cut is, open: how long a parry leaves it, big: whether that is a big opening
  plain: { wind: 0.42, arrive: 0.04, live: 0.12, rec: 0.5, dmg: 1, reach: 58, y0: -44, y1: -6, open: 0.22, big: false, glow: null },     // no glint; a plain parry only turns it aside
  yellow: { wind: 0.62, arrive: 0.04, live: 0.12, rec: 0.5, dmg: 1, reach: 62, y0: -46, y1: -4, open: 1.6, big: true, glow: '255,214,74' },     // a yellow parry leaves it wide open
  red: { wind: 0.70, arrive: 0.05, live: 0.14, rec: 0.6, dmg: 2, reach: 74, y0: -14, y1: 0, open: 0, big: false, glow: '255,60,50' },          // low: a sweep along the ground
};
const STRIKE_ORDER = ['plain', 'yellow', 'red', 'yellow', 'plain', 'red'];
const STRIKER_RANGE = 150;                                            // u: it only attacks when he is this close

class Striker extends Dummy {
  constructor(x, y = 0) {
    super(x, y, 'post');
    this.w = 24; this.h = 48; this.face = 1;
    this.state = 'idle'; this.t = 0; this.idleFor = 1.0; this.cls = 'plain'; this.next = 0;   // idle | wind | strike | recover | open
    this.openT = 0; this.big = false; this.landed = false; this.parries = 0;
  }
  // one step of its behaviour; `hero` is the one it fights
  tick(dt, hero) {
    if (this.openT > 0) { this.openT = Math.max(0, this.openT - dt); this.state = 'open'; this.t = 0; return; }
    if (this.state === 'open') { this.state = 'idle'; this.idleFor = this.big ? 0.7 : 0.5; }
    this.t += dt;
    const S = STRIKES[this.cls];
    if (this.state === 'idle') {
      if (hero.cx !== this.x) this.face = hero.cx > this.x ? 1 : -1;
      if (this.t >= this.idleFor && Math.abs(hero.cx - this.x) < STRIKER_RANGE && hero.deadT <= 0) {
        this.cls = STRIKE_ORDER[this.next++ % STRIKE_ORDER.length]; this.state = 'wind'; this.t = 0; this.landed = false;
      }
    } else if (this.state === 'wind' && this.t >= S.wind) { this.state = 'strike'; this.t = 0; }
    else if (this.state === 'strike' && this.t >= S.live) { this.state = 'recover'; this.t = 0; }
    else if (this.state === 'recover' && this.t >= S.rec) { this.state = 'idle'; this.t = 0; this.idleFor = 0.9; }
  }
  // the box its blade covers while a strike is live (null otherwise, or once it has landed or been parried)
  attackBox() {
    const S = STRIKES[this.cls];
    if (this.state !== 'strike' || this.landed || this.t < S.arrive) return null;                       // (the blade reaches him a moment into the cut, so a parry pressed just late still catches it)
    const f = this.face, x0 = this.x + f * this.w / 2;
    return { x: f > 0 ? x0 : x0 - S.reach, y: this.y + S.y0, w: S.reach, h: S.y1 - S.y0, cls: this.cls, dmg: S.dmg };
  }
  // its strike has been parried: it is left open for as long as the attack's kind allows
  parried() { const S = STRIKES[this.cls]; this.openT = S.open || 0.22; this.big = S.big; this.state = 'open'; this.landed = true; this.parries++; }
  // struck by the hero's blade: it flinches as any dummy does, and a blow on one that is open lands harder (returns true)
  hit(dir, face, power = 1) {
    const open = this.openT > 0 && this.big;
    super.hit(dir, face, open ? power * 1.5 : power);
    this.vox *= 0.25; this.vl *= 0.4;                                                                    // it flinches, but does not get pushed about (it is not a dummy)
    return open;
  }
}

// A four-point sparkle: two thin crossed diamonds with a white heart and no ring or circle. The cue to parry (yellow) or to jump or dash (red), and the parry's flash.
function drawGlint(ctx, x, y, size, rgb, a) {
  if (a <= 0.01 || size <= 0.5) return;
  ctx.save(); ctx.translate(x, y); ctx.globalAlpha = Math.min(1, a);
  for (const [len, wid, rot] of [[size, size * 0.1, 0], [size * 0.72, size * 0.09, Math.PI / 2], [size * 0.5, size * 0.06, Math.PI / 4], [size * 0.5, size * 0.06, -Math.PI / 4]]) {
    ctx.save(); ctx.rotate(rot); ctx.beginPath(); ctx.moveTo(-len, 0); ctx.lineTo(0, -wid); ctx.lineTo(len, 0); ctx.lineTo(0, wid); ctx.closePath(); ctx.fillStyle = `rgb(${rgb})`; ctx.fill(); ctx.restore();
  }
  ctx.fillStyle = '#ffffff'; ctx.beginPath(); ctx.moveTo(-size * 0.22, 0); ctx.lineTo(0, -size * 0.07); ctx.lineTo(size * 0.22, 0); ctx.lineTo(0, size * 0.07); ctx.closePath(); ctx.fill();
  ctx.restore();
}
const GLINT_LEAD = 0.32, GLINT_LEN = 0.24;       // s: the glint starts this long before the cut begins, and lasts this long

// in the ink style, with a wooden blade; a glint at the blade's tip is the telegraph (yellow: parry it; red: jump or dash); nothing lights up
function drawStriker(ctx, s, t) {
  const S = STRIKES[s.cls], telling = s.state === 'wind' || s.state === 'strike';
  const wind = s.state === 'wind' ? clamp(s.t / S.wind, 0, 1) : telling ? 1 : 0, cut = s.state === 'strike' ? clamp(s.t / S.live, 0, 1) : s.state === 'recover' ? 1 : 0;
  const open = s.state === 'open', bigOpen = open && s.big, low = s.cls === 'red' && (telling || s.state === 'recover');
  const hit = s.flash > 0, ink = hit ? '#eef2ff' : '#1a2140', head = hit ? '#f6f8ff' : '#262f52';
  ctx.save(); ctx.translate(s.x + s.ox, s.y + s.oy);
  ctx.fillStyle = 'rgba(7,9,13,0.2)'; ctx.beginPath(); ctx.ellipse(-s.ox, -s.oy + 0.4, 15, 1.8, 0, 0, 7); ctx.fill();
  ctx.rotate(s.lean + (bigOpen ? 0.24 * s.face : 0)); ctx.scale(s.face, 1);                              // flinching, or doubled over toward him when wide open (never leaning away)
  const crouch = bigOpen ? 5 : low ? 4 * (s.state === 'recover' ? 1 - s.t / S.rec : wind) : 0;            // a red sweep comes from a crouch; wide open, it is crouched and bowed
  // the blade: raised behind it as it winds up, swung through as it cuts, lowered after; held low for a sweep
  const hx = 8, hy = -15 + crouch * 0.6;
  let ang = bigOpen ? 1.3 : open ? -1.7 : lerp(0.9, 1.1, 0.5);                                            // wide open: the blade hangs; turned aside: it is knocked up
  if (telling || s.state === 'recover') {
    if (low) ang = s.state === 'wind' ? lerp(0.9, 2.7, ease(wind)) : lerp(2.7, 0.1, s.state === 'strike' ? ease(cut) : 1);
    else ang = s.state === 'wind' ? lerp(0.9, -2.3, ease(wind)) : lerp(-2.3, 0.7, s.state === 'strike' ? ease(cut) : 1);
  }
  const len = 34, tipX = hx + Math.cos(ang) * len, tipY = hy + Math.sin(ang) * len;
  limb(ctx, [{ x: hx, y: hy }, { x: tipX, y: tipY }], '#07090d', 3.4); limb(ctx, [{ x: hx, y: hy }, { x: tipX, y: tipY }], '#5a4a3a', 2.4);       // the wooden blade
  if (s.state === 'strike') {                                                                                 // the cut's trail, a pale grey streak for every kind
    const a0 = low ? 2.7 : -2.3, a1 = ang, span = a1 - a0;
    ctx.save(); ctx.lineCap = 'round';
    for (let i = 0; i < 12; i++) { const u = (i + 0.5) / 12; ctx.beginPath(); ctx.arc(hx, hy, len, a0 + span * i / 12, a0 + span * (i + 1.1) / 12, span < 0); ctx.lineWidth = 1 + 7 * u; ctx.strokeStyle = `rgba(190,196,214,${0.6 * u})`; ctx.stroke(); }
    ctx.restore();
  }
  // feet, body, head and eyes (the eyes are always white)
  inkShape(ctx, ink, () => ctx.ellipse(-4, -3, 4.6, 3.7, 0, 0, 7)); inkShape(ctx, ink, () => ctx.ellipse(5, -3, 4.6, 3.7, 0, 0, 7));
  inkShape(ctx, ink, () => ctx.ellipse(0, -11 + crouch, 7.8, 9, 0, 0, 7));
  inkShape(ctx, head, () => ctx.arc(0, -26 + crouch * 1.4, 11.8, 0, 7));
  const ey = -25 + crouch * 1.4;
  glowDot(ctx, 5, ey + 1, 6.5, '207,230,255', 0.4);
  ctx.fillStyle = '#ffffff';
  ctx.beginPath(); ctx.ellipse(2.4, ey, 2.4, 1.1, -0.3, 0, 7); ctx.fill(); ctx.beginPath(); ctx.ellipse(8, ey - 0.5, 2.2, 1.05, -0.3, 0, 7); ctx.fill();
  // the telegraph: one glint at the blade's tip, flaring fast and fading, a moment before the cut lands (plain attacks have none)
  if (S.glow) {
    const g = s.state === 'wind' ? s.t - (S.wind - GLINT_LEAD) : s.state === 'strike' ? s.t + GLINT_LEAD : -1, u = g / GLINT_LEN;
    if (u >= 0 && u <= 1) { const k = u < 0.3 ? u / 0.3 : 1 - (u - 0.3) / 0.7; drawGlint(ctx, tipX, tipY, 6 + 15 * k, S.glow, k); }
  }
  ctx.restore();
}
