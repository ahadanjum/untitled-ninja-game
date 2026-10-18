'use strict';
// ---------------------------------------------------------------------------
// art_hero.js — Kaname, drawn in Little Shinobi's ink-silhouette style: dark round shapes (a head, a body, two feet), a thin pale rim,
// glowing eyes, and no arms at all (D103): the sword simply appears in his hand, as it does in Little Shinobi.
// (design/concepts/kaname-C-little-shinobi.svg). Units are world units (head radius 12.4). One draw function so the look can be swapped
// later; it reads state and changes none. No red or yellow anywhere on him (those mean danger and parry); the blade's glow is violet-black.
// The look is the designer's pick "A" (D091, D095): Little Shinobi's band and tail, in the blade's dark purple. (The band-only and
// knight-silhouette looks they turned down are drawn in design/concepts/kaname-looks-A-B-C.png.)
// ---------------------------------------------------------------------------
const INKC = '#0b1236', RIM = '#7fa8e8', BLK = '#07090d';
const ROPE = '#b79f78', ROPE_DK = '#4a3e28';                                       // (the dummy's rope bands)
const BAND = '#5b3cae', BAND_EDGE = '#1f0f45';                                     // the headband, its tail and the sash
const CURSE_RGB = '143,77,255';
const HILT = '#3a2a70', HILT_LT = '#6a52b0', STEEL = '#9aa6b8', SAYA = '#241a46', SAYA_LT = '#4a3a80';

function inkShape(ctx, fill, path, rim = RIM, lw = 0.32) {
  ctx.beginPath(); path(); ctx.fillStyle = fill; ctx.fill();
  if (rim) { ctx.lineWidth = lw; ctx.lineJoin = 'round'; ctx.strokeStyle = rim; ctx.stroke(); }
}
function limb(ctx, pts, color, w) {
  ctx.beginPath(); ctx.moveTo(pts[0].x, pts[0].y); for (let i = 1; i < pts.length; i++) ctx.lineTo(pts[i].x, pts[i].y);
  ctx.lineCap = 'round'; ctx.lineJoin = 'round'; ctx.lineWidth = w; ctx.strokeStyle = color; ctx.stroke();
}
function glowDot(ctx, x, y, r, rgb, a) {
  const g = ctx.createRadialGradient(x, y, 0, x, y, r);
  g.addColorStop(0, `rgba(${rgb},${a})`); g.addColorStop(1, `rgba(${rgb},0)`);
  ctx.fillStyle = g; ctx.beginPath(); ctx.arc(x, y, r, 0, 7); ctx.fill();
}
const hash2 = (a, b) => { const s = Math.sin(a * 127.1 + b * 311.7) * 43758.5453; return s - Math.floor(s); };      // a steady pseudo-random 0..1 (flicker)

// the scarf points as drawn: the chain, blended between the last step and this one, with its first point pinned to the head exactly
function scarfPoints(P, rx, ry, alpha) {
  const pts = P.scarf.p.map(q => ({ x: lerp(q.ox, q.x, alpha), y: lerp(q.oy, q.y, alpha) }));
  const r = kanameHeadFrame(P, rx, ry).root; pts[0].x = r.x; pts[0].y = r.y;
  return pts;
}
// the headband's tail: tapered strokes along the chain (a dark outline pass, then a lighter one), the first game's scarf
function drawScarf(ctx, P, rx, ry, alpha) {
  const pts = scarfPoints(P, rx, ry, alpha);
  ctx.save(); ctx.lineCap = 'round'; ctx.lineJoin = 'round';
  for (let pass = 0; pass < 2; pass++) {
    ctx.strokeStyle = pass ? BAND : BAND_EDGE;
    for (let i = 1; i < pts.length; i++) {
      ctx.lineWidth = (5 - i * 0.52) * (pass ? 0.72 : 1);
      ctx.beginPath(); ctx.moveTo(pts[i - 1].x, pts[i - 1].y); ctx.lineTo(pts[i].x, pts[i].y); ctx.stroke();
    }
  }
  ctx.restore();
}

// The arcs a strike cuts: the blade turns from angle a0 to a1 about his hand, which goes to the pivot (px, py) in his own frame (facing +x,
// y down, the feet at the origin; angle 0 = straight ahead, positive = down), and R is how far its tip is from the pivot (the blade is
// BLADE_LEN long, curved a little). The trail is the sweep of the blade's outer part, so it always hangs off the blade's tip. A forward
// cut alternates direction each time. The sword's live box is in hero.js (swordBox) and matches these reaches.
const BLADE_LEN = 40;
const SWINGS = {
  fwd: { px: 11, py: -15, a0: -1.15, a1: 1.15, R: 45 },     // across the front, from above the head to the knee
  up: { px: 14, py: -22, a0: -2.5, a1: -0.64, R: 45 },      // from behind and above, over his head, to the front
  down: { px: 12, py: -10, a0: 0.64, a1: 2.5, R: 45 },      // from the front, down and under, to behind
  draw: { px: 14, py: -15, a0: -0.5, a1: 0.2, R: 45 },      // the draw: one long, flat cut across the front as he lunges
  guard: { px: 14, py: -18, a0: -1.1, a1: -1.1, R: 45 },    // the parry: the blade held up and forward in front of him, not moving
};
const swingAngles = sw => { const g = SWINGS[sw.dir]; return sw.alt && sw.dir === 'fwd' ? [g.a1, g.a0] : [g.a0, g.a1]; };
const swingEase = p => 1 - (1 - Math.min(1, p * 1.7)) ** 2;      // quick through the cut, settling at the end (Little Shinobi's)

// Every number that places his body, for a pose { vis, runT, t, face }: used by the drawing and by the speed lines (for their heights).
function kanamePose(P) {
  const v = P.vis, run = v.run, air = v.air, rise = v.rise, ph = P.runT, d = v.dash, sp = v.sprint;
  // stride: the legs scissor as he runs, reach further as he sprints, and trail straight out behind in a dash
  const stride = Math.sin(ph) * 6 * run * (1 + 0.2 * sp) * (1 - d);
  const l1 = stride - d * 7, l2 = -stride - d * 2.5;
  const lift = 3.5 * run * (1 + 0.4 * sp) * (1 - d);
  const lift1 = Math.max(0, Math.sin(ph)) * lift + d * 2.5, lift2 = Math.max(0, -Math.sin(ph)) * lift + d * 1.2;
  const crouch = kanameCrouch(v);
  const bob = Math.sin(P.t * 3) * 0.6 * (1 - run * 0.5) + crouch, step = Math.abs(Math.sin(ph)) * run, hb = bob * 0.8;
  const hy = kanameHeadFrame(P, 0, 0).hy;                      // the head's height: one definition, shared with the scarf's root
  // the katana in its sheath at the hip (it streams back, level, in a dash)
  const hx = 12, hyy = -13.2 + hb, tx = -19.6 - run * 0.8 - d * 4 - sp * 1.5, ty = -5.9 - run * 2.2 - air * 2.5 - d * 2.5 - sp;
  // the hand's place, on the hilt at his side; in a cut it goes to the swing's pivot and the blade turns about it (there is no arm to draw)
  let W1 = { x: 12.6, y: -12.9 + hb - air * 0.5 }, kx = 18, ky = -15 + hb, ang = 0;
  const wA = v.atk || 0, sw = P.swing;
  if (wA > 0.001 && sw) {
    const g = SWINGS[sw.dir], [a0, a1] = swingAngles(sw); ang = lerp(a0, a1, swingEase(sw.p));
    W1 = { x: lerp(W1.x, g.px, wA), y: lerp(W1.y, g.py, wA) };
    kx = lerp(kx, W1.x - Math.cos(ang) * 3, wA); ky = lerp(ky, W1.y - Math.sin(ang) * 3, wA);
  }
  return { run, air, rise, d, sp, l1, l2, lift1, lift2, crouch, bob, step, hb, hy, hx, hyy, tx, ty, W1, kx, ky, ang, wA };
}

// ---- the speed lines of a dash and a sprint (D098): four tapered violet wedges from where the trail began to him, each fading to nothing at its far end ----
function drawTrail(ctx, P, rx, ry) {
  const tr = P.trail; if (!tr.length) return;
  const last = tr[tr.length - 1], a = Math.min(1, last.k * 1.6) * (last.life / last.max);
  const dx = rx - tr[0].x, len = Math.min(Math.abs(dx), 90), dir = sign(dx); if (len < 3 || a <= 0.01) return;
  const Q = kanamePose(P), headY = kanameHeadFrame(P, rx, ry).head.y;
  const lines = [{ y: headY - 4, w: 2.6, f: 1 }, { y: headY + 7, w: 1.5, f: 0.6 }, { y: ry + Q.bob - 12, w: 3.2, f: 0.85 }, { y: ry - 4, w: 2, f: 0.5 }];
  ctx.save();
  for (const l of lines) {
    const x1 = rx, x0 = rx - dir * len * l.f, gr = ctx.createLinearGradient(x0, 0, x1, 0);
    gr.addColorStop(0, 'rgba(91,60,174,0)'); gr.addColorStop(1, `rgba(91,60,174,${a})`);
    ctx.fillStyle = gr; ctx.beginPath(); ctx.moveTo(x0, l.y); ctx.lineTo(x1, l.y - l.w / 2); ctx.lineTo(x1, l.y + l.w / 2); ctx.closePath(); ctx.fill();
  }
  ctx.restore();
}

function drawKaname(ctx, P, t, rx, ry, alpha) {
  const v = P.vis, run = v.run, air = v.air, rise = v.rise, d = v.dash;
  const blink = P.invuln > 0 && P.dashT <= 0 && P.deadT <= 0 && Math.floor(t * 22) % 2 === 0;       // untouchable after a hit: he blinks
  if (blink) { ctx.save(); ctx.globalAlpha = 0.45; }

  // the shadow on whatever is below him, fainter the higher he is
  const sy = P.shadowY, hgt = Math.max(0, sy - ry), sh = clamp(1 - hgt / 150, 0.2, 1);
  ctx.save(); ctx.fillStyle = `rgba(7,9,13,${0.2 * sh})`;
  ctx.beginPath(); ctx.ellipse(rx, sy + 0.4, 13 * (0.62 + 0.38 * sh), 1.7, 0, 0, 7); ctx.fill(); ctx.restore();

  drawTrail(ctx, P, rx, ry);
  drawScarf(ctx, P, rx, ry, alpha);

  ctx.save();
  ctx.translate(rx, ry);
  ctx.scale(P.face * (1 + v.sq * 0.4 + d * DASH_STRETCH), 1 - v.sq * 0.4 - d * DASH_SQUASH);   // squash and stretch (a dash is long and low)
  ctx.rotate(v.lean);                                          // leaning into the run, further into a sprint, furthest in a dash
  const Q = kanamePose(P), { l1, l2, lift1, lift2, bob, step, hy, hx, hyy, tx, ty, W1, kx, ky, wA } = Q;   // where everything goes
  const pulse = 0.5 + 0.5 * Math.sin(t * 2.4);

  // the katana in its sheath at the hip: plain, steel fittings, nothing white
  const sa = Math.atan2(hyy - ty, hx - tx);
  limb(ctx, [{ x: hx, y: hyy }, { x: tx, y: ty }], BLK, 2.9);
  limb(ctx, [{ x: hx, y: hyy }, { x: tx, y: ty }], SAYA, 2.3);
  limb(ctx, [{ x: hx - 1, y: hyy + 0.2 }, { x: tx + 1, y: ty + 0.3 }], SAYA_LT, 0.35);
  for (const [fx, fy] of [[lerp(hx, tx, 0.1), lerp(hyy, ty, 0.1)], [tx, ty]]) {
    ctx.save(); ctx.translate(fx, fy); ctx.rotate(sa); ctx.fillStyle = STEEL; ctx.fillRect(-0.75, -1.15, 1.5, 2.3); ctx.lineWidth = 0.18; ctx.strokeStyle = BLK; ctx.strokeRect(-0.75, -1.15, 1.5, 2.3); ctx.restore();
  }

  // feet and body
  inkShape(ctx, BLK, () => ctx.ellipse(-4 + l1, -3 - air * 2 - lift1, 4.6, 3.7, 0, 0, 7));
  inkShape(ctx, BLK, () => ctx.ellipse(5.5 + l2, -3.4 - air * lerp(1, 4, rise) - lift2, 4.6, 3.7, 0, 0, 7));
  inkShape(ctx, INKC, () => ctx.ellipse(0, -11 + bob - step * 1.1, 7.8, 9, 0, 0, 7));
  // the sash across the body, its tail flicking behind
  const sb = bob - step * 1.1;
  inkShape(ctx, BAND, () => { ctx.moveTo(-7.5, -8.8 + sb); ctx.bezierCurveTo(-3, -6.6 + sb, 3, -6.6 + sb, 7.5, -8.8 + sb); ctx.lineTo(7.2, -6.4 + sb); ctx.bezierCurveTo(3, -4.2 + sb, -3, -4.2 + sb, -7.2, -6.4 + sb); ctx.closePath(); }, BLK, 0.2);
  inkShape(ctx, BAND, () => { ctx.moveTo(-7, -7.6 + sb); ctx.quadraticCurveTo(-9.4 - run * 2 - d * 2, -6.4 + sb, -10.4 - run * 3 - d * 4, -3 + sb + air * -2); ctx.quadraticCurveTo(-8.4 - run, -4.4 + sb, -6.2, -4.8 + sb); ctx.closePath(); }, BLK, 0.2);

  // the head: a round dark hood with a headband and glowing eyes (no hood tail)
  ctx.save(); ctx.translate(0, hy); ctx.rotate(0.08 * run); ctx.translate(0, -hy);
  inkShape(ctx, INKC, () => ctx.arc(0, hy, 12.4, 0, 7));
  // the headband: a cloth band wrapped round the head just above the eyes; its tail is the scarf (the first game's look)
  ctx.save(); ctx.beginPath(); ctx.arc(0, hy, 12.3, 0, 7); ctx.clip();
  ctx.fillStyle = BAND; ctx.fillRect(-14, hy - 3.6, 28, 5.2);
  ctx.fillStyle = BAND_EDGE; ctx.fillRect(-14, hy - 3.6, 28, 0.32); ctx.fillRect(-14, hy + 1.28, 28, 0.32);
  ctx.restore();
  ctx.beginPath(); ctx.arc(0, hy, 12.4, 0, 7); ctx.lineWidth = 0.32; ctx.strokeStyle = RIM; ctx.stroke();      // the pale rim, over the band's ends
  const eyeS = 1 - clamp(v.run, 0, 1) * 0.12 - d * 0.2, blinkK = v.air > 0.5 && rise > 0.5 ? 0.85 : 1, bl = P.blink > 0 ? 0.1 : 1;
  glowDot(ctx, 5.4, hy + 3.4, 6.6, '207,230,255', 0.62);
  ctx.fillStyle = '#ffffff';
  ctx.beginPath(); ctx.ellipse(2.6, hy + 3.5, 2.8, 1.2 * eyeS * blinkK * bl, -0.31, 0, 7); ctx.fill();
  ctx.beginPath(); ctx.ellipse(8.6, hy + 2.8, 2.6, 1.15 * eyeS * blinkK * bl, -0.31, 0, 7); ctx.fill();
  ctx.restore();

  // the hilt at his side, out of the sheath's mouth (the unseen hand is on it): the cursed blade's glow shows here while it is sheathed
  if (wA < 0.4) {
    limb(ctx, [{ x: W1.x, y: W1.y - 0.1 }, { x: kx, y: ky }], BLK, 2.5); limb(ctx, [{ x: W1.x, y: W1.y - 0.1 }, { x: kx, y: ky }], HILT, 1.9);
    ctx.lineWidth = 0.27; ctx.strokeStyle = HILT_LT;
    for (let i = 0; i < 3; i++) { const f = 0.28 + i * 0.24, px = lerp(W1.x, kx, f), py = lerp(W1.y, ky, f); ctx.beginPath(); ctx.moveTo(px, py + 0.9); ctx.lineTo(px + 0.4, py - 0.9); ctx.stroke(); }
    inkShape(ctx, STEEL, () => ctx.ellipse(W1.x - 0.5, W1.y - 0.4, 0.9, 2.1, -0.3, 0, 7), BLK, 0.2);
    const ch = v.charge || 0;                                                        // the charge: the glow swells, motes gather into the hilt, and when it is ready a ring pulses out
    glowDot(ctx, (W1.x + kx) / 2, (W1.y + ky) / 2, 7 + ch * 9, CURSE_RGB, 0.22 + 0.2 * pulse + ch * 0.45);
    glowDot(ctx, kx + 0.3, ky - 0.1, 5.4 + ch * 7, CURSE_RGB, 0.2 + 0.24 * pulse + ch * 0.4);          // the glow on the sword (no smoke)
    inkShape(ctx, STEEL, () => ctx.arc(kx + 0.3, ky - 0.1, 1, 0, 7), BLK, 0.2);
    if (ch > 0.03) for (let i = 0; i < 6; i++) {
      const ph = (t * 1.6 + i / 6) % 1, r = (1 - ph) * (10 + 16 * ch), th = i * 1.047 + t * 2;
      glowDot(ctx, W1.x + Math.cos(th) * r, W1.y + Math.sin(th) * r * 0.8, 2.4, '205,175,255', 0.85 * ch * ph);
    }
    if (P.charge >= 1) { const ph = (t * 2.6) % 1; ctx.beginPath(); ctx.arc(W1.x, W1.y, 6 + ph * 16, 0, 7); ctx.lineWidth = 1.2; ctx.strokeStyle = `rgba(217,200,255,${0.9 * (1 - ph)})`; ctx.stroke(); }
  }
  drawCrescent(ctx, P, Q);
  drawBlade(ctx, P, Q);
  if (P.parryT > 0) drawGlint(ctx, Q.W1.x + Math.cos(Q.ang) * 40, Q.W1.y + Math.sin(Q.ang) * 40, 7 + 2 * Math.sin(t * 40), '225,215,255', 0.9);   // the open parry: a small pale glint at the blade's tip
  ctx.restore();
  if (blink) ctx.restore();
}

// The trail of a cut: the sweep of the blade's outer part, a crescent hair-thin where the blade began and widest at its tip, whose outer edge
// is the path of the blade's tip itself (so the trail hangs off the blade, never ahead of it). Violet-black with a pale violet edge, thinning
// and fading as the cut ends (D087: the essence shows in the swing). Drawn in his own frame about his hand, so it moves with him exactly as
// the blade does.
function drawCrescent(ctx, P, Q) {
  const sw = P.swing, al = 1 - sw.fade; if (Q.wA < 0.02 || al < 0.02) return;
  const g = SWINGS[sw.dir], [b0, b1] = swingAngles(sw), e0 = lerp(b0, b1, swingEase(sw.p));
  if (Math.abs(e0 - b0) < 0.03) return;
  const lag = (b1 > b0 ? -1 : 1) * Math.atan2(BLADE_LEN * 0.1, 5 + BLADE_LEN);          // the curved blade's tip trails its axis a little: so does the trail's tip
  const a0 = b0 + lag, span = e0 - b0, W = (sw.dir === 'draw' ? 22 : 14) * (1 - 0.4 * sw.fade), px = Q.W1.x, py = Q.W1.y, M = 40;
  const pt = (t, r) => [px + Math.cos(a0 + span * t) * r, py + Math.sin(a0 + span * t) * r];
  // a crescent from t0 to the tip: its outer edge is the tip's path (radius rOut), its inner edge comes in by a width that grows from nothing at t0
  const band = (t0, wMul, rOut, fill) => {
    ctx.beginPath();
    for (let i = 0; i <= M; i++) { const [x, y] = pt(t0 + (1 - t0) * i / M, rOut); i ? ctx.lineTo(x, y) : ctx.moveTo(x, y); }
    for (let i = M; i >= 0; i--) { const t = t0 + (1 - t0) * i / M, ramp = (t - t0) / (1 - t0), [x, y] = pt(t, rOut - (W * Math.pow(t, 1.2) * ramp + 0.3) * wMul); ctx.lineTo(x, y); }
    ctx.closePath(); ctx.fillStyle = fill; ctx.fill();
  };
  ctx.save();
  band(0, 1.5, g.R + 1.4, `rgba(${CURSE_RGB},${0.2 * al})`);                  // a soft violet glow round a violet-black body that darkens toward the tip
  band(0, 1, g.R, `rgba(28,12,64,${0.5 * al})`); band(0.45, 1, g.R, `rgba(28,12,64,${0.45 * al})`); band(0.75, 1, g.R, `rgba(18,6,44,${0.4 * al})`);
  ctx.lineCap = 'round'; ctx.lineWidth = 0.9;                                                                                 // the pale edge along the tip's path, fading toward where it began
  for (let i = 0; i < M; i++) { const t = (i + 0.5) / M, [x0, y0] = pt(i / M, g.R), [x1, y1] = pt((i + 1) / M, g.R); ctx.beginPath(); ctx.moveTo(x0, y0); ctx.lineTo(x1, y1); ctx.strokeStyle = `rgba(217,200,255,${t * t * 0.95 * al})`; ctx.stroke(); }
  ctx.restore();
}

// The blade in his hand during a cut, drawn as Little Shinobi draws its cutlass but cursed: a wrapped grip, a guard and a curved blade of
// violet-black with a spine and a band of pale violet fire along the cutting edge, wisps of violet fire streaming back off it, and a soft
// aura hugging the whole blade, strongest toward the tip. The aura is the blade's own: it moves with it and ends at its tip.
function drawBlade(ctx, P, Q) {
  const al = Q.wA; if (al < 0.02) return;
  const [a0, a1] = swingAngles(P.swing), len = BLADE_LEN, tipX = 5 + len, tipY = -len * 0.1, mid = 5 + len * 0.6, ecx = 5 + len * 0.66;
  const edge = u => { const w = 1 - u; return { x: w * w * tipX + 2 * w * u * ecx + u * u * 5.5, y: w * w * tipY + 2 * w * u * 4.2 + u * u * 2.1 }; };   // u: 0 at the tip, 1 at the guard
  const seed = Math.floor(P.t * 24), flick = 0.88 + 0.12 * hash2(seed, 99);
  ctx.save(); ctx.translate(Q.W1.x, Q.W1.y); ctx.rotate(Q.ang); if (a1 < a0) ctx.scale(1, -1);                            // the cutting edge always leads
  ctx.globalAlpha = al;
  // the aura: a soft halo along the blade and glow where the edge burns
  ctx.lineCap = 'round'; ctx.beginPath(); ctx.moveTo(5.5, 0); ctx.quadraticCurveTo(mid, -1.2, tipX, tipY);
  ctx.lineWidth = 11; ctx.strokeStyle = `rgba(${CURSE_RGB},${0.16 * flick})`; ctx.stroke(); ctx.lineWidth = 6; ctx.strokeStyle = `rgba(${CURSE_RGB},${0.3 * flick})`; ctx.stroke();
  for (let i = 1; i <= 6; i++) { const u = (i - 0.5) / 6.5, e = edge(u); glowDot(ctx, e.x, e.y + 1, 5 + hash2(seed, i) * 3, CURSE_RGB, (0.16 + (1 - u) * 0.3) * flick); }
  // the grip, the pommel and the guard
  ctx.fillStyle = '#24143e'; ctx.fillRect(-6, -1.7, 10, 3.4);
  ctx.strokeStyle = HILT_LT; ctx.lineWidth = 0.7; ctx.beginPath(); for (let i = 0; i < 4; i++) { ctx.moveTo(-5 + i * 2.4, -1.7); ctx.lineTo(-3.6 + i * 2.4, 1.7); } ctx.stroke();
  inkShape(ctx, STEEL, () => ctx.arc(-7, 0, 2, 0, 7), BLK, 0.2);
  inkShape(ctx, STEEL, () => ctx.ellipse(4.6, 0, 1.6, 4.2, 0, 0, 7), BLK, 0.2);
  // the curved blade, its spine, and the band of pale violet fire along the cutting edge
  ctx.beginPath(); ctx.moveTo(5.5, -1.7); ctx.quadraticCurveTo(mid, -2.2, tipX, tipY); ctx.quadraticCurveTo(5 + len * 0.66, 4.2, 5.5, 2.1); ctx.closePath();
  ctx.fillStyle = '#1b1034'; ctx.fill(); ctx.lineWidth = 0.35; ctx.strokeStyle = BLK; ctx.stroke();
  ctx.strokeStyle = '#5a4a8c'; ctx.lineWidth = 0.9; ctx.beginPath(); ctx.moveTo(5.5, -1.4); ctx.quadraticCurveTo(mid, -1.9, tipX - 1, tipY + 0.3); ctx.stroke();
  ctx.beginPath(); ctx.moveTo(tipX, tipY); ctx.quadraticCurveTo(ecx, 4.2, 5.5, 2.1); ctx.lineTo(6, 0.4); ctx.quadraticCurveTo(ecx, 2.2, tipX - 1.5, tipY + 0.2); ctx.closePath();
  ctx.fillStyle = '#b79cff'; ctx.fill();
  ctx.beginPath(); ctx.moveTo(tipX, tipY); ctx.quadraticCurveTo(ecx, 4.2, 5.5, 2.1); ctx.lineWidth = 0.8; ctx.strokeStyle = '#efe6ff'; ctx.stroke();
  // wisps of violet fire streaming back off the blade (away from the way it is cutting), each with a pale core: they fade into the trail
  for (let i = 1; i <= 5; i++) {
    const u = (i - 0.35) / 5.4, e = edge(u), x = e.x, y = -1.2, fh = 3 + hash2(seed, i + 30) * 5, sway = (hash2(seed, i + 50) - 0.5) * 2.2, w = 1.5 + hash2(seed, i + 70) * 0.8;
    ctx.fillStyle = `rgba(${CURSE_RGB},0.42)`;
    ctx.beginPath(); ctx.moveTo(x - w, y); ctx.quadraticCurveTo(x - w + sway * 0.4 - 0.8, y - fh * 0.55, x + sway - 1.2, y - fh); ctx.quadraticCurveTo(x + w + sway * 0.4, y - fh * 0.5, x + w, y); ctx.closePath(); ctx.fill();
    ctx.fillStyle = 'rgba(224,208,255,0.5)';
    ctx.beginPath(); ctx.moveTo(x - w * 0.4, y); ctx.quadraticCurveTo(x + sway * 0.3 - 0.4, y - fh * 0.45, x + sway * 0.8 - 0.6, y - fh * 0.7); ctx.quadraticCurveTo(x + w * 0.4 + sway * 0.2, y - fh * 0.4, x + w * 0.4, y); ctx.closePath(); ctx.fill();
  }
  ctx.restore();
}
