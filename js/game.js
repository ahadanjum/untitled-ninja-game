'use strict';
// ---------------------------------------------------------------------------
// game.js — the feel-ladder test environment: a blank stage, Kaname, a camera, and the loop.
// Fixed 60 Hz simulation; drawing happens at the display's rate. On a fast display (a 120 Hz MacBook screen) the last two
// steps are blended so motion is smooth; on a 60 Hz screen the latest step is drawn as-is (blending would only add a
// frame of lag). Developer URL flags: ?night  ?zoom=3  ?grid=0  ?hints=0  ?debug  ?interp=1|0  ?manual (test harness)
// Keys: arrows/WASD move, Space/Z jump, Shift/X/K dash (hold to sprint), J/C sword (hold to charge), L/V parry · G grid · N night · H hints · [ ] zoom · R reset · ` numbers
// ---------------------------------------------------------------------------
const Q = new URLSearchParams(location.search);
const canvas = document.getElementById('c'), ctx = canvas.getContext('2d');
let SCALE = 1;                                     // backing-store pixels per logical pixel

G.zoom = clamp(+Q.get('zoom') || 3, 1.5, 6);
G.night = Q.has('night'); G.grid = Q.get('grid') !== '0'; G.hints = Q.get('hints') !== '0'; G.debug = Q.has('debug');
G.t = 0; G.hz = 60; G.interp = false;
G.hero = new Kaname(0, 0); G.dummies = makeDummies(); G.hitstop = 0; G.fx = []; G.kick = 0;
G.cam = { x: 0, v: 0 }; G.camPrev = 0;

function resetHero() { G.hero = new Kaname(0, 0); G.dummies = makeDummies(); G.hitstop = 0; G.fx = []; G.kick = 0; G.cam.x = 0; G.cam.v = 0; G.camPrev = 0; }

function fit() {
  const dpr = window.devicePixelRatio || 1;
  const cssW = Math.min(innerWidth, innerHeight * VW / VH), cssH = cssW * VH / VW;
  canvas.style.width = cssW + 'px'; canvas.style.height = cssH + 'px';
  const bw = Math.min(3840, Math.round(cssW * dpr));
  if (canvas.width !== bw) { canvas.width = bw; canvas.height = Math.round(bw * VH / VW); }
  SCALE = canvas.width / VW;
}

// ---- one simulation step ----
function step(dt) {
  devKeys();
  G.camPrev = G.cam.x;
  if (G.hitstop > 0) { G.hitstop = Math.max(0, G.hitstop - dt); G.hero.freeze(); return; }      // hit-stop: everything holds still for a moment
  let threat = null;                                                                           // whoever is winding up or cutting at him, so a parry turns him toward it
  for (const s of G.dummies) if ((s.state === 'wind' || s.state === 'strike') && Math.abs(s.x - G.hero.cx) < STRIKER_RANGE + 30) threat = s.x;
  G.hero.threat = threat;
  G.hero.update(dt);
  strikes();
  for (const d of G.dummies) { d.update(dt); if (d.tick) d.tick(dt, G.hero); }
  attacks();
  G.kick = approach(G.kick, 0, dt * 0.5);
  for (const f of G.fx) f.t += dt;
  G.fx = G.fx.filter(f => f.t < f.life);
  smoothDamp(G.cam, G.hero.cx + G.hero.face * 36, 0.16, dt);
  G.t += dt;
}

// the sword: whatever the live blade overlaps is struck, once per swing, the world pauses for a moment, and the hero is told (pogo, dash reset)
function strikes() {
  const b = G.hero.blow(); if (!b) return;
  for (const d of G.dummies) {
    if (b.hit.includes(d) || !Stage.overlaps(b.box, d.box())) continue;
    b.hit.push(d); const crit = d.hit(b.dir, b.face, b.power); G.hero.onStrike(b.dir); G.hitstop = b.power > 1 ? K.HITSTOP_DRAW : K.HITSTOP;
    if (crit) { G.hitstop = K.HITSTOP_CRIT; G.kick = 0.1; }                 // a blow on an opened enemy: the big-hit zoom (L07) and a longer pause
  }
}

// what the enemy's blades do to him: a live attack that reaches him is turned aside by an open parry (unless it is red), or it hurts (unless he is untouchable)
function attacks() {
  const P = G.hero;
  for (const s of G.dummies) {
    const ab = s.attackBox && s.attackBox(); if (!ab) continue;
    if (P.parryT > 0 && ab.cls !== 'red' && Stage.overlaps(P.parryBox(), ab)) {
      s.parried(); P.parried(ab.cls); G.hitstop = K.HITSTOP_PARRY; if (ab.cls === 'yellow') G.kick = 0.1;
      G.fx.push({ type: 'parry', x: P.cx + P.face * 28, y: P.y - 22, t: 0, life: 0.3 });
    } else if (!P.invulnerable() && Stage.overlaps(P.box(), ab)) { s.landed = true; P.hurt(ab.dmg, s.x); G.hitstop = K.HITSTOP_HURT; }
  }
}

function devKeys() {
  if (Input.rawHit('KeyG')) G.grid = !G.grid;
  if (Input.rawHit('KeyN')) G.night = !G.night;
  if (Input.rawHit('KeyH')) G.hints = !G.hints;
  if (Input.rawHit('Backquote')) G.debug = !G.debug;
  if (Input.rawHit('BracketRight')) G.zoom = clamp(G.zoom * 1.15, 1.5, 6);
  if (Input.rawHit('BracketLeft')) G.zoom = clamp(G.zoom / 1.15, 1.5, 6);
  if (Input.rawHit('KeyR')) resetHero();
}

// ---- drawing ----
const GROUND_Y = VH * 0.78;                        // where the ground sits on screen
const STARS = Array.from({ length: 70 }, (_, i) => [(i * 137.5) % VW, (i * 71.3) % (VH * 0.62), 0.8 + (i % 3) * 0.5]);

function drawBackdrop() {
  if (!G.night) { ctx.fillStyle = '#ffffff'; ctx.fillRect(0, 0, VW, VH); return; }
  const g = ctx.createLinearGradient(0, 0, 0, VH); g.addColorStop(0, '#090c26'); g.addColorStop(0.6, '#1a1548'); g.addColorStop(1, '#2a1d5e');
  ctx.fillStyle = g; ctx.fillRect(0, 0, VW, VH);
  ctx.fillStyle = 'rgba(200,216,255,0.7)'; for (const [x, y, r] of STARS) { ctx.beginPath(); ctx.arc(x, y, r, 0, 7); ctx.fill(); }
  for (const [x, y] of [[190, 480], [1080, 520]]) glowDot(ctx, x, y, 150, '255,191,122', 0.28);
}

function drawStage(camX) {
  const z = G.zoom * (1 + G.kick), px = 1 / z, left = camX - VW / (2 * z) - 40, right = camX + VW / (2 * z) + 40, depth = (VH - GROUND_Y) / z + 20;
  const n = G.night;
  ctx.fillStyle = n ? '#120d30' : '#f2f4f8'; ctx.fillRect(left, 0, right - left, depth);
  if (G.grid) {                                     // heights to judge a jump against: every 32 u (one tile)
    ctx.strokeStyle = n ? 'rgba(143,170,255,0.13)' : '#e3e7f0'; ctx.lineWidth = px;
    for (let i = 1; i <= Stage.GRID_LINES; i++) { const y = -i * Stage.GRID; ctx.beginPath(); ctx.moveTo(left, y); ctx.lineTo(right, y); ctx.stroke(); }
  }
  for (const s of Stage.solids) {
    if (s.kind === 'ground') continue;
    ctx.fillStyle = n ? '#1d1760' : '#e1e6f0'; ctx.fillRect(s.x, s.y, s.w, s.h);
    ctx.strokeStyle = n ? '#4b3aa0' : '#aab3c8'; ctx.lineWidth = 1.5 * px; ctx.strokeRect(s.x, s.y, s.w, s.h);
  }
  ctx.strokeStyle = n ? '#3a2f66' : '#2b3350'; ctx.lineWidth = 2 * px;               // the ground line, with a tick every 64 u
  ctx.beginPath(); ctx.moveTo(left, 0); ctx.lineTo(right, 0); ctx.stroke();
  ctx.lineWidth = px; ctx.beginPath();
  for (let x = Math.ceil(left / 64) * 64; x < right; x += 64) { ctx.moveTo(x, 0); ctx.lineTo(x, x % 320 === 0 ? 9 : 5); }
  ctx.stroke();
}

// the flash of a parry, at the clash: a hard four-point glint and a burst of sparks flying out (yellow is the parry's colour); no rings or circles
function drawFx() {
  for (const f of G.fx) {
    const u = f.t / f.life; ctx.save(); ctx.translate(f.x, f.y); ctx.lineCap = 'round';
    drawGlint(ctx, 0, 0, 26 * (1 - u * 0.5), '255,226,120', 1 - u * u);
    for (let i = 0; i < 10; i++) {
      const a = i * 0.628 + 0.2, r0 = 7 + 14 * u, r1 = r0 + 7 + 8 * (1 - u);
      ctx.beginPath(); ctx.moveTo(Math.cos(a) * r0, Math.sin(a) * r0); ctx.lineTo(Math.cos(a) * r1, Math.sin(a) * r1); ctx.lineWidth = 1.8 * (1 - u) + 0.3; ctx.strokeStyle = `rgba(255,${i % 2 ? 240 : 214},${i % 2 ? 190 : 110},${1 - u})`; ctx.stroke();
    }
    ctx.restore();
  }
}

// the health bar (D082): a short row of pale segments at the top of the screen, dark where health is gone (never red)
function drawBar() {
  const n = K.HP, w = 26, gap = 4, x0 = VW / 2 - (n * w + (n - 1) * gap) / 2, y = 20, hp = G.hero.hp;
  for (let i = 0; i < n; i++) {
    ctx.beginPath(); ctx.roundRect(x0 + i * (w + gap), y, w, 8, 3); ctx.fillStyle = i < hp ? '#d7ebff' : 'rgba(27,34,64,0.88)'; ctx.fill(); ctx.lineWidth = 1; ctx.strokeStyle = '#7fa8e8'; ctx.stroke();
  }
}

function drawHud() {
  drawBar();
  ctx.font = '12px system-ui, -apple-system, sans-serif'; ctx.textBaseline = 'middle';
  if (G.grid) {                                      // the height scale, at the left edge
    ctx.fillStyle = G.night ? '#7f8ac0' : '#a5adc0'; ctx.textAlign = 'left';
    for (let i = 1; i <= Stage.GRID_LINES; i++) ctx.fillText(String(i * Stage.GRID), 14, GROUND_Y - i * Stage.GRID * G.zoom);
  }
  if (G.hints) {
    ctx.fillStyle = G.night ? '#7f8ac0' : '#9aa2b6'; ctx.textAlign = 'left';
    ctx.fillText('← → move   Space jump   Shift dash (hold: sprint)   J sword (hold ↑, ↓ in the air; hold for the draw)   L parry   G grid   N night   [ ] zoom   R reset   H hide this   ` numbers', 18, VH - 18);
  }
  if (G.debug) {
    const P = G.hero, rows = [`${G.hz.toFixed(0)} Hz  ${G.interp ? 'blended' : 'direct'}`, `vx ${P.vx.toFixed(0)}  vy ${P.vy.toFixed(0)}  ${P.onGround ? 'ground' : 'air'}`,
      `coyote ${P.coyote.toFixed(3)}  buffer ${P.jumpBuf.toFixed(3)}`, `apex ${P.apex.toFixed(1)} u   last jump ${P.lastJump.toFixed(1)} u`,
      `dash ${P.dashT > 0 ? 'ON' : '-'}  cooldown ${P.dashCd.toFixed(2)}  ${P.canDash ? 'ready' : 'spent'}   last dash ${P.lastDash.toFixed(0)} u`,
      `sword ${P.atk ? P.atk.dir + ' ' + P.atk.t.toFixed(2) : '-'}  cooldown ${P.atkCd.toFixed(2)}  blows landed ${P.strikes}  charge ${P.charge.toFixed(2)}`,
      `health ${P.hp}  parry ${P.parryT.toFixed(2)} (presses ${P.parrySpam + 1} within 0.5 s: ${P.parryRecent > 0 ? 'shorter window' : 'full window'})  untouchable ${P.invuln.toFixed(2)}`, `zoom ${G.zoom.toFixed(2)}`];
    ctx.fillStyle = G.night ? '#cfd8ff' : '#2b3350'; rows.forEach((r, i) => ctx.fillText(r, 18, 22 + i * 18));
  }
}

function render(alpha = 1) {
  fit();
  ctx.setTransform(SCALE, 0, 0, SCALE, 0, 0);
  drawBackdrop();
  const P = G.hero, z = G.zoom * (1 + G.kick), a = G.interp ? alpha : 1;
  const rx = lerp(P.px, P.cx, a), ry = lerp(P.py, P.y, a), camX = lerp(G.camPrev, G.cam.x, a);
  ctx.setTransform(SCALE * z, 0, 0, SCALE * z, SCALE * (VW / 2 - camX * z), SCALE * GROUND_Y);
  drawStage(camX);
  for (const d of G.dummies) (d instanceof Striker ? drawStriker : drawDummy)(ctx, d, G.t);
  drawKaname(ctx, P, G.t, rx, ry, a);
  drawFx();
  ctx.setTransform(SCALE, 0, 0, SCALE, 0, 0);
  drawHud();
}

// ---- the loop ----
let lastTs = 0, acc = 0; const samples = [];
function frame(ts) {
  requestAnimationFrame(frame);
  if (!lastTs) { lastTs = ts; return; }
  const dt = Math.min(0.1, (ts - lastTs) / 1000); lastTs = ts;
  if (samples.length < 40) {                        // measure the display: blend the steps only if it is faster than ~75 Hz
    samples.push(dt);
    if (samples.length === 40) { const s = samples.slice().sort((x, y) => x - y); G.hz = 1 / s[20]; G.interp = Q.has('interp') ? Q.get('interp') !== '0' : G.hz > 75; }
  }
  acc += dt; Input.poll();
  while (acc >= STEP) { step(STEP); Input.endStep(); acc -= STEP; }
  render(acc / STEP);
}
addEventListener('resize', fit);
canvas.addEventListener('pointerdown', () => canvas.focus());
fit();
if (!window.__holdRender) requestAnimationFrame(frame);
