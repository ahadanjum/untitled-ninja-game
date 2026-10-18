'use strict';
// ---------------------------------------------------------------------------
// dummy.js — the test dummy for the sword (rung 4): a training post that stands, or hangs from above, and flinches when it is
// struck. It cannot be hurt or killed and shows nothing over its head (no counters: the debug numbers count the blows). It is the only
// thing on the feel ladder that is not Kaname or the stage, and it is not solid: he runs and dashes straight through it.
// Units as everywhere: world units, y = 0 is the ground, up is negative; (x, y) is the middle of its bottom.
// ---------------------------------------------------------------------------
class Dummy {
  constructor(x, y, kind = 'post') {
    this.x = x; this.y = y; this.kind = kind;       // 'post' stands on y; 'hanging' dangles by a rope from far above, its bottom at y
    this.w = 26; this.h = 46;
    this.hits = 0; this.flash = 0; this.last = 'fwd';
    this.ox = 0; this.vox = 0; this.oy = 0; this.voy = 0;   // how far a blow has knocked it, and the speed of the spring back
    this.lean = 0; this.vl = 0;                              // and how far it has swung round its base (or its rope)
  }
  box() { return { x: this.x - this.w / 2, y: this.y - this.h, w: this.w, h: this.h }; }
  // struck by a blow from `dir` ('fwd' | 'up' | 'down' | 'draw') swung by someone facing `face`, `power` 1 for a strike and 3 for the draw: it flinches away from it
  hit(dir, face, power = 1) {
    this.hits++; this.flash = power > 1 ? 0.13 : 0.08; this.last = dir;
    if (dir === 'draw') { this.vox = face * 330; this.vl = face * 13; }                 // the draw is a heavy blow: it flings the dummy
    else if (dir === 'fwd') { this.vox = face * 140; this.vl = face * 7; }
    else if (dir === 'up') this.voy = -130;
    else this.voy = 150;
    return false;                                                           // (a blow on an opened Striker returns true: a critical blow)
  }
  update(dt) {
    this.vox += (-300 * this.ox - 22 * this.vox) * dt; this.ox += this.vox * dt;               // springs back to rest, a little bouncy
    this.voy += (-300 * this.oy - 22 * this.voy) * dt; this.oy += this.voy * dt;
    this.vl += (-220 * this.lean - 14 * this.vl) * dt; this.lean += this.vl * dt;
    this.flash = Math.max(0, this.flash - dt);
  }
}

// the standing set the sword is judged against: two posts on the ground, a short stair of hanging ones going up to the left (for the up
// strike, the pogo, and chaining bounces and air dashes from one to the next), and far to the left the striker that fights back (for the parry)
const makeDummies = () => [new Dummy(240, 0, 'post'), new Dummy(340, 0, 'post'), new Dummy(-240, -60, 'hanging'), new Dummy(-310, -100, 'hanging'), new Dummy(-380, -140, 'hanging'), new Striker(-560, 0)];

// in the hero's ink style: a dark post and crossbar, a round straw head, rope bands; white-blue for an instant when struck (never red or yellow)
function drawDummy(ctx, d) {
  const hit = d.flash > 0, wood = hit ? '#e9efff' : '#1a2140', head = hit ? '#f4f7ff' : '#262f52';
  ctx.save(); ctx.translate(d.x + d.ox, d.y + d.oy);
  if (d.kind === 'post') {                                           // a soft shadow on the ground
    ctx.fillStyle = 'rgba(7,9,13,0.2)'; ctx.beginPath(); ctx.ellipse(-d.ox, -d.oy + 0.4, 15, 1.8, 0, 0, 7); ctx.fill();
  } else {                                                           // the rope it hangs from, running off the top of the screen
    ctx.beginPath(); ctx.moveTo(0, -d.h); ctx.lineTo(0, -d.h - 600); ctx.lineWidth = 0.9; ctx.strokeStyle = ROPE; ctx.stroke();
    ctx.translate(0, -d.h);                                          // it swings from its rope
  }
  ctx.rotate(d.lean);
  if (d.kind === 'hanging') ctx.translate(0, d.h);
  inkShape(ctx, wood, () => ctx.roundRect(-3.6, -41, 7.2, 41, 1.6));                                // the post
  inkShape(ctx, wood, () => ctx.roundRect(-14, -33, 28, 5, 2));                                      // the crossbar (the arms)
  inkShape(ctx, head, () => ctx.arc(0, -43, 9.4, 0, 7));                                             // the straw head
  for (const [x, y, w] of [[-14, -33, 3], [11, -33, 3], [-3.6, -22, 7.2]]) {                         // rope bands
    ctx.fillStyle = ROPE; ctx.fillRect(x, y, w, 5 * (y === -22 ? 0.7 : 1)); ctx.lineWidth = 0.2; ctx.strokeStyle = ROPE_DK; ctx.strokeRect(x, y, w, 5 * (y === -22 ? 0.7 : 1));
  }
  ctx.beginPath(); ctx.moveTo(-9, -36); ctx.lineTo(9, -36); ctx.lineWidth = 0.8; ctx.strokeStyle = ROPE; ctx.stroke();       // the band at its neck
  ctx.restore();
}
