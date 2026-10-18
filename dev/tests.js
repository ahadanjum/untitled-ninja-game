'use strict';
// dev/tests.js — acceptance tests for the feel ladder (rungs 0–7: the stage, Kaname, run, jump, dash, sprint, the sword, the pogo, the charge and the draw, health and the parry).
//
// Run with `node dev/run.mjs` (see that file). The runner injects this script into a fresh page per test (index.html?manual)
// *before* the game's own scripts, with Math.random seeded and rendering held, so every test is deterministic and independent.
// Tests drive the real game through dev/harness.js (T.*) and assert on game state, never on look. `await snap('label')`
// photographs the current frame as evidence for a person to look at.
//
// Four levels of evidence:
//   build         the page boots, draws, throws nothing
//   behavior      run, stop, turn, jump, coyote time, buffering, heights, dash, sprint, the sword and a dummy, the pogo, the charge and the draw, health and the parry, the pad, determinism
//   presentation  `snap()` screenshots (test-results/index.html) for the designer or a reviewer to look at
//   player        NOT covered here: only the designer, with the real controller, can say whether it FEELS right
//
// THE NUMBERS BELOW ARE THE DESIGN (see js/hero.js and CLAUDE.md). If you retune one on purpose, change it here too.
(() => {
  const tests = [];
  const notes = {};
  class Fail extends Error {}
  const fmt = v => (typeof v === 'number' ? +v.toFixed(3) : JSON.stringify(v));
  const ok = (c, m) => { if (!c) throw new Fail(m || 'expected a truthy value'); };
  const eq = (a, b, m) => { if (a !== b) throw new Fail(`${m || 'expected equal'}: got ${fmt(a)}, want ${fmt(b)}`); };
  const near = (a, b, tol, m) => { if (!(Math.abs(a - b) <= tol)) throw new Fail(`${m || 'expected near'}: got ${fmt(a)}, want ${fmt(b)} ± ${tol}`); };
  const between = (a, lo, hi, m) => { if (!(a >= lo && a <= hi)) throw new Fail(`${m || 'expected in range'}: got ${fmt(a)}, want ${fmt(lo)}..${fmt(hi)}`); };
  const note = (k, v) => { notes[k] = v; };
  function test(name, opts, fn) {
    if (typeof opts === 'function') { fn = opts; opts = {}; }
    tests.push({ name, params: opts.params || '?manual', timeout: opts.timeout, fn });
  }

  // ---- helpers --------------------------------------------------------------------------------------------------------
  const P = () => G.hero;
  const finite = (...vs) => vs.every(Number.isFinite);
  // steps until he is standing again; returns how many it took (or -1)
  const untilLanded = (max = 300) => { for (let i = 1; i <= max; i++) { T.run(1); if (P().onGround) return i; } return -1; };
  // Press jump, hold it for `frames`, let go, wait for the landing; returns the height reached and the steps spent in the air
  function jumpHeld(frames) {
    T.reset(); T.down('Space'); let air = 0, released = false;
    for (let i = 1; i <= 240; i++) {
      T.run(1); if (!P().onGround) air++;
      if (i === frames) { T.up('Space'); released = true; }
      if (released && P().onGround) break;
    }
    T.up('Space'); return { h: P().lastJump, frames: air };
  }
  // a running jump toward a pillar, letting go of right at the top of the arc (as a player would) so he lands on it
  function runJumpAt(pillarX) {
    T.place(pillarX - 80, 0); T.down('ArrowRight'); T.down('Space');
    let air = false;
    for (let i = 0; i < 200; i++) {
      T.run(1); if (!P().onGround) air = true;
      if (i === 18) T.up('ArrowRight');
      if (i === 30) T.up('Space');
      if (air && P().onGround) break;
    }
    T.up('ArrowRight'); T.up('Space'); T.run(2);
    return { x: P().cx, y: P().y, standing: P().onGround };
  }
  const hash = () => { const p = G.hero; return [p.cx, p.y, p.vx, p.vy, p.face, p.onGround ? 1 : 0, p.coyote, p.jumpBuf, p.vis.sq, p.dashT, p.dashCd, p.canDash ? 1 : 0, p.atk ? p.atk.t : -1, p.strikes, G.hitstop, p.hp, p.parryT, p.invuln].map(v => (+v).toFixed(5)).join(','); };

  // ===================================================================================================================
  // BUILD
  // ===================================================================================================================
  test('boot: Kaname stands on the ground at the origin, facing right, still', () => {
    T.reset(); const p = P();
    eq(p.cx, 0, 'x'); eq(p.y, 0, 'feet on the ground'); ok(p.onGround, 'on the ground'); eq(p.face, 1, 'facing right'); eq(p.vx, 0, 'standing still');
    eq(canvas.width, 1280, 'canvas width'); eq(canvas.height, 720, 'canvas height');
  });

  test('draw: Kaname is on the blank white stage', async () => {
    T.reset(); render();
    const px = ctx.getImageData(0, 0, 1280, 720).data;
    const at = (x, y) => { const i = (y * 1280 + x) * 4; return [px[i], px[i + 1], px[i + 2]]; };
    eq(at(20, 20).join(), '255,255,255', 'the stage is white');
    let ink = 0; for (let y = 440; y < 570; y++) for (let x = 580; x < 720; x++) { const [r, g, b] = at(x, y); if (r < 60 && g < 70 && b < 120) ink++; }
    ok(ink > 1500, `Kaname's dark body should fill the middle of the screen (found ${ink} ink pixels)`); note('inkPixels', ink);
    await snap('idle on the blank stage');
  });

  test('palette: nothing on Kaname is red or yellow, standing, sprinting or dashing (those colours are the danger and parry language)', () => {
    const count = () => {
      render(); const px = ctx.getImageData(300, 330, 680, 270).data; let red = 0, yellow = 0;
      for (let i = 0; i < px.length; i += 4) { const r = px[i], g = px[i + 1], b = px[i + 2]; if (r > 170 && g < 100 && b < 100) red++; if (r > 200 && g > 180 && b < 110) yellow++; }
      return { red, yellow };
    };
    T.reset(); G.hints = false; G.grid = false; T.run(30); let c = count(); eq(c.red, 0, 'red pixels on the hero standing'); eq(c.yellow, 0, 'yellow pixels on the hero standing');
    T.down('ArrowRight'); T.down('ShiftLeft'); T.run(5); c = count(); eq(c.red, 0, 'red pixels while dashing'); eq(c.yellow, 0, 'yellow pixels while dashing');
    T.run(30); c = count(); eq(c.red, 0, 'red pixels while sprinting'); eq(c.yellow, 0, 'yellow pixels while sprinting');
    T.up('ShiftLeft'); T.up('ArrowRight'); T.run(30); G.dummies = [new Dummy(-60, 0)]; T.tap('KeyJ', 1); T.run(3); c = count(); eq(c.red, 0, 'red pixels mid-swing'); eq(c.yellow, 0, 'yellow pixels mid-swing');
    T.run(40); T.down('KeyJ'); T.run(55); c = count(); eq(c.red, 0, 'red pixels while charging'); eq(c.yellow, 0, 'yellow pixels while charging');
    T.up('KeyJ'); T.run(4); c = count(); eq(c.red, 0, 'red pixels mid-draw'); eq(c.yellow, 0, 'yellow pixels mid-draw');
  });

  test('look: he has no arms, as in Little Shinobi: no bare skin anywhere on him, standing, running or mid-swing (D103)', () => {
    const skin = () => {
      render(); const px = ctx.getImageData(400, 300, 480, 300).data; let n = 0;
      for (let i = 0; i < px.length; i += 4) { const r = px[i], g = px[i + 1], b = px[i + 2]; if (r > 190 && r < 228 && g > 148 && g < 182 && b > 118 && b < 158) n++; }
      return n;
    };
    T.reset(); G.dummies = []; G.hints = false; G.grid = false; T.run(30); eq(skin(), 0, 'standing');
    T.down('ArrowRight'); T.run(20); eq(skin(), 0, 'running'); T.up('ArrowRight'); T.run(20);
    T.tap('KeyJ', 1); T.run(4); eq(skin(), 0, 'mid-swing'); T.run(40);
    T.down('ArrowUp'); T.tap('KeyJ', 1); T.up('ArrowUp'); T.run(4); eq(skin(), 0, 'mid up-cut');
  });

  test('look: the band, the tail and the sash are drawn in the blade\'s purple (look A, D095)', () => {
    T.reset(); G.hints = false; G.grid = false; T.down('ArrowRight'); T.run(30); render();
    const px = ctx.getImageData(200, 300, 880, 320).data; let n = 0;
    for (let i = 0; i < px.length; i += 4) if (Math.abs(px[i] - 91) < 16 && Math.abs(px[i + 1] - 60) < 16 && Math.abs(px[i + 2] - 174) < 16) n++;
    note('purplePixels', n); ok(n > 1250, `the band, tail and sash should show in purple (${n} pixels)`);
    ok(typeof LOOKS === 'undefined' && typeof G.look === 'undefined', 'the other looks are gone: there is one look');
  });

  // ===================================================================================================================
  // BEHAVIOR — rung 1: running
  // ===================================================================================================================
  test('run: holding right reaches the top speed of 240 u/s within 0.1 s and never exceeds it', () => {
    T.reset(); T.down('ArrowRight'); let n = 0, peak = 0;
    for (let i = 0; i < 90; i++) { T.run(1); peak = Math.max(peak, P().vx); if (!n && P().vx >= 239.999) n = i + 1; }
    ok(n > 0 && n <= 6, `took ${n} steps to reach top speed`); near(peak, 240, 0.001, 'top speed'); eq(P().face, 1, 'facing right');
    note('stepsToTopSpeed', n); note('topSpeed', peak); T.up('ArrowRight');
  });

  test('stop: letting go stops him within 0.1 s and about 8 u', () => {
    T.reset(); T.down('ArrowRight'); T.run(30); const x0 = P().cx; T.up('ArrowRight'); let n = 0;
    for (let i = 1; i <= 30; i++) { T.run(1); if (P().vx === 0) { n = i; break; } }
    ok(n > 0 && n <= 6, `took ${n} steps to stop`); between(P().cx - x0, 4, 12, 'slide distance'); note('stopSteps', n); note('slideU', P().cx - x0);
  });

  test('turn: reversing flips him at once and he is through zero within 0.07 s', () => {
    T.reset(); T.down('ArrowRight'); T.run(30); T.up('ArrowRight'); T.down('ArrowLeft'); T.run(1);
    eq(P().face, -1, 'faces left the instant he turns'); let n = 0;
    for (let i = 1; i <= 12; i++) { if (P().vx <= 0) { n = i; break; } T.run(1); }
    ok(n > 0 && n <= 4, `took ${n} steps to pass through zero`); T.run(10); near(P().vx, -240, 0.001, 'reaches full speed the other way'); note('turnSteps', n); T.up('ArrowLeft');
  });

  test('keys: arrows and WASD both move him', () => {
    for (const [r, l] of [['ArrowRight', 'ArrowLeft'], ['KeyD', 'KeyA']]) {
      T.reset(); T.down(r); T.run(20); ok(P().vx > 200, `${r} should run right`); T.up(r); T.down(l); T.run(40); ok(P().vx < -200, `${l} should run left`); T.up(l);
    }
  });

  // ===================================================================================================================
  // BEHAVIOR — the controller
  // ===================================================================================================================
  test('stick: a light touch walks, full tilt runs, the dead zone does nothing', () => {
    T.reset(); T.pad({ axes: [0.1, 0, 0, 0] }); T.run(30); eq(P().vx, 0, 'inside the dead zone');
    T.pad({ axes: [0.5, 0, 0, 0] }); T.run(40); const half = P().vx; between(half, 70, 130, 'half tilt walks'); near(half, 240 * padCurve(0.5), 0.5, 'follows the curve'); note('halfTiltSpeed', half);
    T.pad({ axes: [1, 0, 0, 0] }); T.run(40); near(P().vx, 240, 0.001, 'full tilt runs');
    T.pad({ axes: [-1, 0, 0, 0] }); T.run(40); near(P().vx, -240, 0.001, 'full tilt left'); eq(P().face, -1);
  });

  test('pad: ✕ jumps on the standard layout; the raw layout uses its own button', () => {
    T.reset(); T.pad({ pressed: [0] }); T.run(2); ok(P().vy < 0 || !P().onGround, '✕ (button 0, standard) should jump');
    T.reset(); T.pad({ mapping: '', pressed: [1] }); T.run(2); ok(!P().onGround, 'on the raw layout ✕ is button 1');
    T.reset(); T.pad({ mapping: '', pressed: [0] }); T.run(2); ok(P().onGround, 'on the raw layout button 0 is □, which must not jump');
  });

  test('d-pad: it moves him, on the standard buttons and on the raw hat', () => {
    T.reset(); T.pad({ pressed: [15] }); T.run(20); ok(P().vx > 200, 'd-pad right');
    T.reset(); T.pad({ pressed: [14] }); T.run(20); ok(P().vx < -200, 'd-pad left');
    T.reset(); T.pad({ mapping: '', hat: -0.4285714 }); T.run(20); ok(P().vx > 200, 'raw hat: right');
    T.reset(); T.pad({ mapping: '', hat: 0.7142857 }); T.run(20); ok(P().vx < -200, 'raw hat: left');
  });

  // ===================================================================================================================
  // BEHAVIOR — rung 2: jumping
  // ===================================================================================================================
  test('jump: a held jump rises about 118 u (3.7 tiles) and is in the air about 0.65 s', () => {
    const j = jumpHeld(70); between(j.h, 114, 122, 'held height'); between(j.frames, 36, 42, 'frames in the air');
    note('heldJumpU', j.h); note('airFrames', j.frames);
  });

  test('jump: a tap rises about 45 u, and the height is variable between a tap and a hold', () => {
    const tap = jumpHeld(3), mid = jumpHeld(12), full = jumpHeld(70);
    between(tap.h, 36, 56, 'tap height'); ok(mid.h > tap.h + 10 && mid.h < full.h - 5, `a 12-step hold (${mid.h.toFixed(1)}) should sit between a tap (${tap.h.toFixed(1)}) and a hold (${full.h.toFixed(1)})`);
    note('tapJumpU', tap.h); note('midJumpU', mid.h);
  });

  test('jump: no double jump', () => {
    T.reset(); T.down('Space'); T.run(18); ok(P().vy > -200 && P().vy < 100, 'near the top of the arc'); const y = P().y;
    T.up('Space'); T.tap('Space', 2); T.run(2); ok(P().vy > -150, `a second press in the air must not launch him again (vy ${P().vy.toFixed(0)})`); ok(P().y <= y + 10 && P().y >= y - 8, `height continues as before (was ${y.toFixed(1)}, now ${P().y.toFixed(1)})`);
  });

  test('jump: buffering: a press up to 0.12 s before landing still jumps, 0.25 s before does not', () => {
    const drop = (pressBefore) => {
      T.place(0, -96); const N = (() => { let n = 0; for (let i = 1; i < 120; i++) { T.run(1); if (P().onGround) { n = i; break; } } return n; })();   // the frames the fall takes
      T.place(0, -96); T.run(N - pressBefore); T.tap('Space', 1); T.run(pressBefore + 3);
      return { N, jumped: !P().onGround && P().y < -1 };
    };
    const a = drop(4), b = drop(15);
    ok(a.jumped, 'a press 4 steps (0.07 s) before landing should jump on landing'); ok(!b.jumped, 'a press 15 steps (0.25 s) before landing should not'); note('fallFrames', a.N);
  });

  test('jump: coyote time: a jump up to 0.08 s after running off a ledge works, 0.15 s after does not', () => {
    const walkOff = wait => {
      T.place(Stage.LEDGE.x + 140, -96); ok(P().onGround, 'standing on the ledge'); T.down('ArrowRight');
      let left = false; for (let i = 0; i < 200 && !left; i++) { T.run(1); if (!P().onGround) left = true; }
      ok(left, 'he should run off the ledge'); T.run(wait); T.down('Space'); T.run(1); T.up('Space'); T.up('ArrowRight');
      return P().vy < 0;
    };
    ok(walkOff(2), 'a jump 3 steps (0.05 s) after leaving the ledge should work'); ok(!walkOff(9), 'a jump 10 steps (0.17 s) after leaving the ledge should not');
  });

  test('heights: a held run-jump clears a 3-tile pillar and the ledge, but not a 4-tile pillar', () => {
    const p3 = runJumpAt(Stage.PILLARS[2].x), p4 = runJumpAt(Stage.PILLARS[3].x), ledge = runJumpAt(Stage.LEDGE.x);
    ok(p3.standing && Math.abs(p3.y + 96) < 0.01, `should land on the 96 u pillar (landed at y ${p3.y.toFixed(1)}, standing ${p3.standing})`);
    ok(!(Math.abs(p4.y + 128) < 0.01), `must not reach the top of the 128 u pillar (y ${p4.y.toFixed(1)})`);
    ok(ledge.standing && Math.abs(ledge.y + 96) < 0.01, `should land on the 96 u ledge (y ${ledge.y.toFixed(1)})`);
  });

  test('head bump: jumping under the ledge stops him at 38 u and he falls straight back', () => {
    T.place(Stage.LEDGE.x + 100, 0); T.down('Space'); let minY = 0, bumped = false;
    for (let i = 0; i < 20; i++) { T.run(1); minY = Math.min(minY, P().y); if (P().vy >= 0 && P().y < -1) bumped = true; }
    T.up('Space'); between(-minY, 37.5, 38.5, 'highest point under the ledge'); ok(bumped, 'vertical speed should reset on the bump');
    ok(untilLanded() > 0, 'he comes back down'); note('bumpHeightU', -minY);
  });

  test('fall: terminal speed is 900 u/s', () => {
    T.place(5000, -3000); let peak = 0; T.run(150, () => { peak = Math.max(peak, P().vy); });
    near(peak, 900, 0.001, 'terminal speed');
  });

  // ===================================================================================================================
  // BEHAVIOR — rung 3: dash (a tap) and sprint (hold)
  // ===================================================================================================================
  // press dash (and keep it down only for one step), then step until the dash is over; reports how far and how flat it was
  function doDash(key = 'ShiftLeft') {
    const x0 = P().cx, y0 = P().y; let steps = 1, minY = y0, maxY = y0, grounded = P().onGround;
    T.down(key); T.run(1); T.up(key);
    while (P().dashT > 0 && steps < 80) { T.run(1); steps++; minY = Math.min(minY, P().y); maxY = Math.max(maxY, P().y); grounded = grounded && P().onGround; }
    return { steps, dx: P().cx - x0, flat: maxY - minY, grounded };
  }
  // how many dashes start in `total` steps, given presses of ShiftLeft at the listed frames (each held for one step)
  function countDashes(total, pressAt) {
    let n = 0, was = false;
    for (let f = 0; f < total; f++) {
      if (pressAt.includes(f)) T.down('ShiftLeft'); if (pressAt.includes(f - 1)) T.up('ShiftLeft');
      T.run(1); const now = P().dashT > 0; if (now && !was) n++; was = now;
    }
    T.up('ShiftLeft'); return n;
  }

  test('dash: a tap goes 128 u (4 tiles) in 0.2 s along the floor, then ends crisply', () => {
    T.reset(); const r = doDash();
    eq(r.steps, 12, 'steps'); near(r.dx, 128, 0.5, 'distance'); ok(r.grounded, 'a ground dash keeps him on the floor'); eq(P().y, 0, 'still on the ground line');
    near(P().vx, 210, 0.001, 'it ends at 210 u/s, not sliding'); near(P().lastDash, 128, 0.5, 'measured distance'); T.run(8); eq(P().vx, 0, 'then he stops (nothing held)');
    note('dashSteps', r.steps); note('dashU', r.dx);
  });

  test('dash: it goes the way you push, and the way he faces when you push nothing', () => {
    T.reset(); T.down('ArrowLeft'); let r = doDash(); T.up('ArrowLeft'); ok(r.dx < -126, `pushing left dashes left (${r.dx.toFixed(1)})`); eq(P().face, -1, 'and faces left');
    T.run(60); r = doDash(); ok(r.dx < -126, `with nothing held he dashes the way he faces, left (${r.dx.toFixed(1)})`);
    T.run(60); T.down('ArrowRight'); r = doDash(); T.up('ArrowRight'); ok(r.dx > 126, `pushing right turns him and dashes right (${r.dx.toFixed(1)})`); eq(P().face, 1, 'and faces right');
  });

  test('dash: 0.45 s cooldown; a press up to 0.1 s early is kept, an earlier one is not', () => {
    T.reset(); eq(countDashes(60, [0, 40]), 2, 'a press after the cooldown dashes again (frames 0 and 40)');
    T.reset(); eq(countDashes(60, [0, 14]), 1, 'a press 13 steps (0.22 s) before the cooldown ends is ignored');
    T.reset(); eq(countDashes(60, [0, 23]), 2, 'a press 4 steps (0.07 s) before the cooldown ends is kept and fires as soon as it ends');
    T.reset(); eq(countDashes(60, [0, 8]), 1, 'it cannot be chained during the cooldown');
  });

  test('dash: in the air it is flat, cancels the fall, and there is one per jump (refilled on landing)', () => {
    T.place(0, -1500); ok(!P().onGround, 'in the air'); T.run(20); const vy0 = P().vy; ok(vy0 > 300, `falling fast (${vy0.toFixed(0)})`);
    const r = doDash(); ok(r.flat < 1e-6, `flat: the height changed by ${r.flat}`); near(r.dx, 128, 0.5, 'distance in the air'); ok(!P().canDash, 'the air dash is spent');
    ok(P().vy < 100, `the dash cancelled the fall (vy now ${P().vy.toFixed(0)})`);
    T.run(30); ok(!P().onGround, 'still in the air, and the cooldown is over');
    eq(countDashes(5, [0]), 0, 'a second dash in the same jump does nothing');
    ok(untilLanded(400) > 0, 'he lands'); ok(P().canDash, 'landing refills it'); T.run(30); eq(countDashes(5, [0]), 1, 'and he can dash again');
  });

  test('dash: a wall ends it', () => {
    T.place(Stage.PILLARS[0].x - 80, 0); const r = doDash();
    near(P().cx, Stage.PILLARS[0].x - K.W / 2, 0.001, 'he stops against the pillar'); ok(r.steps < 12, `it ended early (${r.steps} steps)`); eq(P().dashT, 0, 'the dash is over'); eq(P().vx, 0, 'no speed left');
  });

  test('dash: a jump cuts it short and keeps going forward', () => {
    T.reset(); T.down('ArrowRight'); T.down('ShiftLeft'); T.run(1); T.up('ShiftLeft'); T.run(3); ok(P().dashT > 0, 'mid-dash'); T.tap('Space', 1);
    eq(P().dashT, 0, 'the jump ended the dash'); ok(P().vy < -500 && !P().onGround, `he is jumping (vy ${P().vy.toFixed(0)})`); ok(P().vx >= 205, `and still moving forward (${P().vx.toFixed(0)} u/s)`);
    T.up('ArrowRight'); T.run(20);
  });

  test('sprint: holding dash and a direction reaches 336 u/s, a burst first, and letting go of dash drops him to a run', () => {
    T.reset(); T.place(-3000, 0); T.down('ArrowRight'); T.down('ShiftLeft'); let burst = 0, peakAfter = 0;
    for (let i = 1; i <= 70; i++) { T.run(1); if (P().dashT > 0) burst = i; if (i > 14) peakAfter = Math.max(peakAfter, P().vx); }
    eq(burst + 1, 12, 'the press bursts for 12 steps first'); near(P().lastDash, 128, 0.5, 'the burst covers 128 u'); near(P().vx, 336, 0.001, 'then he sprints at 336'); ok(peakAfter <= 336.001, `never faster than 336 after the burst (${peakAfter.toFixed(1)})`);
    T.up('ShiftLeft'); T.run(6); near(P().vx, 240, 0.001, 'letting go of dash brings him back to a run');
    T.up('ArrowRight'); T.run(10);
  });

  test('sprint: the speed carries through a jump while dash is held, and drops back when it is let go', () => {
    T.reset(); T.place(-3000, 0); T.down('ArrowRight'); T.down('ShiftLeft'); T.run(40); near(P().vx, 336, 0.001, 'sprinting'); T.down('Space'); T.run(12);
    ok(!P().onGround, 'in the air'); near(P().vx, 336, 0.001, 'the sprint speed carries in the air'); T.up('ShiftLeft'); T.run(8); near(P().vx, 240, 0.001, 'let go of dash: back to the run speed in the air');
    T.up('Space'); T.up('ArrowRight'); untilLanded();
  });

  test('sprint: a dash held with no direction does nothing but the burst; with nothing pressed he stands', () => {
    T.reset(); T.down('ShiftLeft'); T.run(40); eq(P().vx, 0, 'no direction: no sprint'); near(P().lastDash, 128, 0.5, 'just the one burst'); T.up('ShiftLeft');
  });

  test('sprint: the pose: leaning in, longer faster strides, a lower head; the dash is longer and lower still', () => {
    T.reset(); T.place(-3000, 0); T.down('ArrowRight'); T.run(40); const run = { lean: P().vis.lean, rate: 0, crouch: kanameCrouch(P().vis) }; let r0 = P().runT; T.run(30); run.rate = (P().runT - r0) / 30;
    T.down('ShiftLeft'); T.run(5); const dash = { d: P().vis.dash, lean: P().vis.lean, ghosts: P().trail.length, crouch: kanameCrouch(P().vis) };
    T.run(40); const sp = { s: P().vis.sprint, lean: P().vis.lean, ghosts: P().trail.length, crouch: kanameCrouch(P().vis) }; r0 = P().runT; T.run(30); sp.rate = (P().runT - r0) / 30;
    ok(dash.d > 0.9, `the dash pose is full within 5 steps (${dash.d.toFixed(2)})`); ok(dash.ghosts >= 2, `a dash records a trail to draw (${dash.ghosts} samples)`);
    ok(sp.s > 0.95, `the sprint pose is full (${sp.s.toFixed(2)})`); ok(sp.ghosts >= 1, `and so does a sprint (${sp.ghosts})`);
    ok(sp.lean > run.lean + 0.15, `he leans further into a sprint (${sp.lean.toFixed(2)} vs ${run.lean.toFixed(2)})`); ok(dash.lean > run.lean + 0.15, `and a dash (${dash.lean.toFixed(2)})`);
    near(sp.rate / run.rate, 1.4, 0.05, 'the strides cycle 1.4 times faster, at 1.4 times the speed'); ok(sp.crouch > run.crouch + 1 && dash.crouch > sp.crouch, `lower in a sprint (${sp.crouch.toFixed(1)}) and lowest in a dash (${dash.crouch.toFixed(1)})`);
    T.up('ShiftLeft'); T.up('ArrowRight'); T.run(120); ok(P().trail.length === 0, 'the trail dies away'); ok(P().vis.sprint < 0.01 && P().vis.dash < 0.01, 'and the poses ease back to standing');
    note('lean', { run: +run.lean.toFixed(2), sprint: +sp.lean.toFixed(2), dash: +dash.lean.toFixed(2) });
  });

  test('keys: Shift, X and K all dash', () => {
    for (const k of ['ShiftLeft', 'ShiftRight', 'KeyX', 'KeyK']) { T.reset(); const r = doDash(k); near(r.dx, 128, 0.5, `${k} dashes`); }
  });

  test('pad: ○, R1 and R2 dash; ✕ still only jumps; on the raw layout ○ is button 2 and button 1 (✕) is still the jump', () => {
    for (const b of [1, 5, 7]) { T.reset(); T.pad({ pressed: [b] }); T.run(2); ok(P().dashT > 0, `button ${b} (standard) should dash`); T.run(30); }
    T.reset(); T.pad({ pressed: [0] }); T.run(2); eq(P().dashT, 0, '✕ must not dash'); ok(!P().onGround, '✕ jumps');
    T.reset(); T.pad({ mapping: '', pressed: [2] }); T.run(2); ok(P().dashT > 0, 'raw layout ○ (button 2) dashes');
    T.reset(); T.pad({ mapping: '', pressed: [1] }); T.run(2); eq(P().dashT, 0, 'raw layout ✕ (button 1) does not dash'); ok(!P().onGround, 'it jumps');
    T.reset(); T.place(-3000, 0); T.pad({ axes: [1, 0, 0, 0], pressed: [7] }); T.run(60); near(P().vx, 336, 0.001, 'stick right + R2 held sprints');
  });

  test('speed lines: four are drawn behind him in a dash and a sprint, and none once he has stopped (D098)', () => {
    const lines = () => {                                          // the speed lines are the only linear gradients drawn (the backdrop is flat by day)
      let n = 0; const make = ctx.createLinearGradient; ctx.createLinearGradient = function (...a) { n++; return make.apply(this, a); };
      try { G.cam.x = G.camPrev = P().cx; G.cam.v = 0; render(); } finally { ctx.createLinearGradient = make; }
      return n;
    };
    const tinted = () => {                                         // violet-tinted pixels in the strip behind him, above the ground line
      const px = ctx.getImageData(120, 330, 440, 210).data; let n = 0;
      for (let i = 0; i < px.length; i += 4) if (px[i] < 245 && px[i + 2] - px[i] > 8) n++;
      return n;
    };
    T.reset(); T.place(-3000, 0); G.hints = false; G.grid = false; eq(lines(), 0, 'standing: no speed lines'); const still = tinted();
    T.down('ArrowRight'); eq((T.run(40), lines()), 0, 'running: none');
    T.down('ShiftLeft'); T.run(8); eq(lines(), 4, 'dashing: four lines'); const dashing = tinted();
    T.run(40); eq(lines(), 4, 'sprinting: four lines (fainter)');
    T.up('ShiftLeft'); T.up('ArrowRight'); T.run(120); eq(lines(), 0, 'stopped: none'); const after = tinted();
    ok(dashing > still + 300, `the dash's lines show in the pixels behind him (${dashing} tinted, ${still} standing)`); ok(after < still + 25, `and nothing is left once he has stopped (${after})`);
    note('tintedPixels', { still, dashing, after });
  });

  // ===================================================================================================================
  // BEHAVIOR — rung 4: the sword and the dummy
  // ===================================================================================================================
  const lone = (x, y = 0, kind = 'post') => { G.dummies = [new Dummy(x, y, kind)]; return G.dummies[0]; };   // one dummy, nothing else to hit
  // how many swings start in `total` steps, given presses of J at the listed frames (each held for one step)
  function countSwings(total, pressAt) {
    let n = 0, was = false;
    for (let f = 0; f < total; f++) {
      if (pressAt.includes(f)) T.down('KeyJ'); if (pressAt.includes(f - 1)) T.up('KeyJ');
      T.run(1); const now = !!P().atk; if (now && !was) n++; was = now;
    }
    T.up('KeyJ'); return n;
  }

  test('sword: a press starts a strike; the blade is live from 0.03 s to 0.14 s and the swing is over at 0.2 s', () => {
    T.reset(); G.dummies = []; T.down('KeyJ'); T.run(1); T.up('KeyJ'); ok(P().atk && P().atk.dir === 'fwd', 'a forward strike has started');
    let first = -1, live = 0, steps = 1;
    for (let i = 1; i <= 20; i++) { T.run(1); if (P().atk) steps = i + 1; if (P().swordBox()) { live++; if (first < 0) first = i; } }
    between(first, 1, 3, 'the blade goes live a couple of steps after the press'); between(live, 6, 8, 'it stays live about 0.11 s'); between(steps, 11, 13, 'the whole swing lasts about 0.2 s');
    note('firstLiveStep', first); note('liveSteps', live); note('swingSteps', steps);
  });

  test('sword: a forward strike reaches 46 u past his body (54 u from his centre) and the dummy takes one blow per swing', () => {
    for (const [face, key] of [[1, 'ArrowRight'], [-1, 'ArrowLeft']]) {
      T.reset(); T.down(key); T.run(2); T.up(key); T.run(10); eq(P().face, face, 'faces the way he last pushed');
      let d = lone(face * 60); T.tap('KeyJ', 1); T.run(14); eq(d.hits, 1, `a dummy 60 u ${face > 0 ? 'right' : 'left'} is struck once, though the blade is live for several steps`);
      T.run(30); d = lone(face * 75); T.tap('KeyJ', 1); T.run(14); eq(d.hits, 0, 'a dummy 75 u away is out of reach');
    }
  });

  test('sword: up (hold up) strikes above his head, down strikes below his feet in the air, and on the ground down swings forward', () => {
    T.reset(); let d = lone(0, -60, 'hanging'); T.tap('KeyJ', 1); T.run(14); eq(d.hits, 0, 'a forward strike does not reach a dummy hanging overhead');
    T.run(30); T.down('ArrowUp'); T.tap('KeyJ', 1); T.up('ArrowUp'); T.run(14); eq(d.hits, 1, 'up + strike hits it');
    T.reset(); T.place(0, -60); d = lone(0, 0); T.down('ArrowDown'); T.tap('KeyJ', 1); T.run(3); T.up('ArrowDown'); eq(P().strikes, 1, 'down + strike in the air hits the post below him'); eq(d.hits, 1, 'the post is struck');
    T.reset(); G.dummies = []; T.down('ArrowDown'); T.tap('KeyJ', 1); eq(P().atk.dir, 'fwd', 'down on the ground is an ordinary forward strike'); T.up('ArrowDown');
  });

  test('sword: hit-stop: when a blow lands the world holds still for 4 steps, and presses made then are not lost', () => {
    T.reset(); const d = lone(40); T.tap('KeyJ', 1); let n = 0; while (d.hits === 0 && n++ < 20) T.run(1);
    eq(d.hits, 1, 'the blow lands'); const t0 = P().atk.t, x0 = P().cx; let frozen = 0;
    for (let i = 0; i < 12; i++) { if (i === 1) T.down('Space'); if (i === 2) T.up('Space'); T.run(1); if (P().atk && P().atk.t === t0) frozen++; else break; }
    eq(frozen, 4, 'the hero and the dummy are held for 4 steps (0.066 s)'); eq(P().cx, x0, 'and he has not moved');
    T.run(10); ok(!P().onGround || P().y < -1, 'a jump pressed while it was frozen is still made once it moves again');
    note('frozenSteps', frozen);
  });

  test('sword: he cannot turn, dash or swing again while the blade is live; a dash cuts the recovery short; the cooldown is 0.3 s and a press up to 0.12 s early is kept', () => {
    T.reset(); G.dummies = []; T.tap('KeyJ', 1); T.down('ArrowLeft'); T.run(3); eq(P().face, 1, 'he cannot turn while the blade is live'); T.run(12); eq(P().face, -1, 'and turns once it is spent'); T.up('ArrowLeft');
    T.reset(); G.dummies = []; T.tap('KeyJ', 1); T.tap('ShiftLeft', 1); eq(P().dashT, 0, 'a dash pressed mid-blade does not start'); T.run(14);
    T.reset(); G.dummies = []; T.tap('KeyJ', 1); T.run(9); ok(P().atk && !P().swinging(), 'in the recovery'); T.tap('ShiftLeft', 1); ok(P().dashT > 0 && !P().atk, 'a dash in the recovery goes, and ends the swing');
    T.run(40); T.reset(); G.dummies = []; T.tap('ShiftLeft', 1); ok(P().dashT > 0, 'dashing'); T.tap('KeyJ', 1); ok(!P().atk, 'no swing while dashing'); T.run(20);
    T.reset(); G.dummies = []; eq(countSwings(40, [0, 8]), 1, 'a press during the cooldown, 0.13 s after the last, is ignored (the buffer is 0.12 s)');
    T.reset(); G.dummies = []; eq(countSwings(40, [0, 14]), 2, 'a press 4 steps before the cooldown ends is kept and swings as soon as it can');
    T.reset(); G.dummies = []; eq(countSwings(40, [0, 25]), 2, 'a press after the cooldown swings at once');
  });

  test('sword: J, C and □ swing it (□ is button 2, or 0 on the raw layout); ✕ and ○ do not', () => {
    for (const k of ['KeyJ', 'KeyC']) { T.reset(); G.dummies = []; T.tap(k, 1); ok(P().atk, `${k} swings`); }
    T.reset(); G.dummies = []; T.pad({ pressed: [2] }); T.run(2); ok(P().atk, '□ (standard) swings');
    T.reset(); G.dummies = []; T.pad({ mapping: '', pressed: [0] }); T.run(2); ok(P().atk, 'raw □ (button 0) swings'); ok(P().onGround, 'and does not jump');
    T.reset(); G.dummies = []; T.pad({ pressed: [0] }); T.run(2); ok(!P().atk, '✕ does not swing');
    T.reset(); G.dummies = []; T.pad({ pressed: [1] }); T.run(2); ok(!P().atk, '○ does not swing');
    T.reset(); G.dummies = []; T.pad({ axes: [0, -1, 0, 0], pressed: [2] }); T.run(2); eq(P().atk.dir, 'up', 'stick up + □ is an up strike');
  });

  test('dummy: it flinches away from a blow and settles back where it stood; it is not solid', () => {
    for (const [dir, axis, sgn] of [['fwd', 'ox', 1], ['up', 'oy', -1], ['down', 'oy', 1]]) {
      const d = new Dummy(100, 0); d.hit(dir, 1); let peak = 0; for (let i = 0; i < 12; i++) { d.update(STEP); peak = Math.max(peak, Math.abs(d[axis])); }
      ok(peak > 2, `a ${dir} blow knocks it ${peak.toFixed(1)} u`); for (let i = 0; i < 180; i++) d.update(STEP);
      ok(Math.abs(d.ox) < 0.05 && Math.abs(d.oy) < 0.05 && Math.abs(d.lean) < 0.01, 'and it settles back'); eq(d.x, 100, 'where it stood'); ok(d.flash === 0, 'the flash is over'); eq(d.hits, 1);
      const e = new Dummy(100, 0); e.hit(dir, 1); e.update(STEP); ok(sgn * e[axis] > 0 || axis === 'ox', 'it moves away from the blow');
    }
    T.reset(); lone(30); T.down('ArrowRight'); T.run(40); ok(P().cx > 80, 'he runs straight through a dummy'); T.up('ArrowRight');
  });

  test('sword: the blade and its arc are drawn during the swing and gone afterwards', () => {
    const edge = () => {                                           // pale violet edge pixels near him (the blade's edge and the arc's rim)
      G.hints = false; G.grid = false; render(); const px = ctx.getImageData(560, 380, 400, 200).data; let n = 0;
      for (let i = 0; i < px.length; i += 4) if (Math.abs(px[i] - 201) < 30 && Math.abs(px[i + 1] - 166) < 30 && Math.abs(px[i + 2] - 255) < 30) n++;
      return n;
    };
    T.reset(); G.dummies = []; T.run(30); const still = edge();
    T.tap('KeyJ', 1); T.run(4); const mid = edge(); ok(P().vis.atk > 0.9, `the arm is up (${P().vis.atk.toFixed(2)})`); ok(mid > still + 25, `the blade's edge and the arc show (${mid} violet pixels, ${still} before)`);
    T.run(40); ok(P().vis.atk < 0.02, 'the arm is back at the hilt'); ok(edge() <= still + 5, 'and the blade and arc are gone');
    note('edgePixels', { still, mid });
  });

  // ===================================================================================================================
  // BEHAVIOR — rung 5: the pogo and attack-to-reset movement
  // ===================================================================================================================
  // a scripted player over a post: strikes down whenever he is falling and within reach of its top; returns what happened
  function pogoChain(maxSteps, wantBlows) {
    const out = { blows: 0, grounded: false, risings: [] }; let last = P().strikes, y0 = 0, minY = 0, rising = false;
    T.down('ArrowDown');
    for (let i = 0; i < maxSteps && out.blows < wantBlows; i++) {
      const above = -46 - P().y;                                   // how far his feet are above the post's top (its box top is at -46)
      if (P().vy > 0 && above < 20 && !P().atk && P().atkCd <= 0 && P().strikes === last) T.tap('KeyJ', 1); else T.run(1);
      if (P().onGround) out.grounded = true;
      if (P().strikes !== last) { if (rising) out.risings.push(y0 - minY); out.blows++; last = P().strikes; y0 = P().y; minY = P().y; rising = true; }
      else if (rising) minY = Math.min(minY, P().y);
    }
    T.up('ArrowDown'); return out;
  }

  test('pogo: a down strike that lands bounces him up at 560 u/s (about 71 u) from where it hit, gives back the air dash, and the bounce starts after the hit-stop', () => {
    T.reset(); const d = lone(240, 0); T.place(240, -62); P().canDash = false; T.down('ArrowDown'); T.tap('KeyJ', 1);
    let n = 0; while (d.hits === 0 && n++ < 12) T.run(1); T.up('ArrowDown');
    eq(d.hits, 1, 'the blow lands on the post below him'); eq(P().vy, -560, 'he is thrown up at 560 u/s'); ok(P().canDash, 'and the air dash is his again'); ok(!P().onGround, 'in the air');
    const y0 = P().y; let minY = y0, frozen = 0; for (let i = 0; i < 60; i++) { T.run(1); if (i < 4 && P().y === y0) frozen++; minY = Math.min(minY, P().y); }
    between(y0 - minY, 66, 76, `the bounce rises about 71 u (${(y0 - minY).toFixed(1)})`); eq(frozen, 4, 'after the 4-step hit-stop');
    note('pogoRiseU', y0 - minY);
  });

  test('pogo: it needs something to hit: a down strike in open air does not bounce him, and a forward cut on the ground does not either', () => {
    T.reset(); G.dummies = []; T.place(0, -200); T.down('ArrowDown'); T.tap('KeyJ', 1); T.run(8); T.up('ArrowDown'); ok(P().vy > 0 && P().strikes === 0, 'nothing hit, nothing bounces');
    T.reset(); lone(50); T.tap('KeyJ', 1); T.run(8); eq(P().strikes, 1, 'the cut lands'); ok(P().onGround && P().vy >= 0, 'but a cut on the ground is not a pogo');
  });

  test('pogo: he can chain it: striking down each time he falls back keeps him bouncing over the post', () => {
    T.reset(); lone(240, 0); T.place(240, -62); const r = pogoChain(900, 6);
    ok(r.blows >= 6, `he lands ${r.blows} blows in a row`); ok(!r.grounded, 'without touching the ground'); ok(r.risings.length >= 3 && r.risings.every(h => h > 64 && h < 80), `each bounce rises about 71 u (${r.risings.map(h => h.toFixed(0)).join(', ')})`);
    note('chainBlows', r.blows);
  });

  test('attack-to-reset: any blow landed in the air (a forward cut or an up cut too) gives back the air dash, and none of them bounces him', () => {
    T.reset(); lone(50, -60, 'hanging'); T.place(0, -60); P().canDash = false; T.tap('KeyJ', 1); T.run(6); eq(P().strikes, 1, 'the forward cut lands in the air'); ok(P().canDash, 'and refills the air dash'); ok(P().vy > -100, 'without a bounce');
    T.reset(); lone(0, -110, 'hanging'); T.place(0, -60); P().canDash = false; T.down('ArrowUp'); T.tap('KeyJ', 1); T.up('ArrowUp'); T.run(6); eq(P().strikes, 1, 'the up cut lands in the air'); ok(P().canDash, 'and refills the air dash'); ok(P().vy > -100, 'without a bounce');
  });

  test('attack-to-reset: once a blow has landed he can dash (or turn) at once, cancelling the rest of the swing; before it lands, he cannot', () => {
    T.reset(); lone(50, -60, 'hanging'); T.place(0, -60); T.tap('KeyJ', 1); T.tap('ShiftLeft', 1); eq(P().dashT, 0, 'a dash pressed while the blade is still looking for its mark does not start');
    T.reset(); lone(50, -60, 'hanging'); T.place(0, -60); T.tap('KeyJ', 1); let n = 0; while (P().strikes === 0 && n++ < 10) T.run(1); eq(P().strikes, 1, 'the blow lands');
    T.tap('ShiftLeft', 1); T.run(5); ok(P().dashT > 0 && !P().atk, 'now he dashes as soon as the hit-stop lets go, and the swing is over');
    T.reset(); lone(50, -60, 'hanging'); T.place(0, -60); T.tap('KeyJ', 1); n = 0; while (P().strikes === 0 && n++ < 10) T.run(1); T.down('ArrowLeft'); T.run(6); eq(P().face, -1, 'and he can turn');
    T.up('ArrowLeft');
  });

  test('pogo: the hanging stair: bounce off one dummy, air dash, and strike the next, all without touching the ground', () => {
    T.reset(); G.dummies = [new Dummy(-240, -60, 'hanging'), new Dummy(-310, -100, 'hanging')]; T.place(-240, -112);      // above the first (its top is at -106)
    T.down('ArrowDown'); T.tap('KeyJ', 1); T.run(6); T.up('ArrowDown'); eq(G.dummies[0].hits, 1, 'the first is struck from above'); ok(P().canDash && P().vy < -500, 'he is bouncing, with his dash back');
    T.run(4); T.down('ArrowLeft'); T.tap('ShiftLeft', 1); ok(P().dashT > 0, 'he can dash at once after the blow, mid-swing'); T.up('ArrowLeft'); T.run(20); ok(!P().canDash, 'the air dash is spent crossing to the next one');
    ok(P().cx < -300, `he has crossed to the left (${P().cx.toFixed(0)})`); ok(!P().onGround, 'and is still in the air');
  });

  // ===================================================================================================================
  // BEHAVIOR — rung 6: the hold-and-release strike (charge and draw)
  // ===================================================================================================================
  test('charge: holding the strike button builds a charge from 0.25 s after the press that is ready at 0.62 s; a tap or a short hold never charges', () => {
    T.reset(); G.dummies = []; T.down('KeyJ'); T.run(6); eq(P().charge, 0, 'nothing at 0.1 s'); T.run(15); between(P().charge, 0.1, 0.9, 'building at 0.35 s'); T.run(25); eq(P().charge, 1, 'ready by 0.77 s'); T.up('KeyJ');
    T.reset(); G.dummies = []; T.tap('KeyJ', 1); T.run(60); eq(P().charge, 0, 'a tap does not charge'); ok(!P().atk, 'and there is no draw');
    T.reset(); G.dummies = []; T.down('KeyJ'); T.run(30); const c = P().charge; T.up('KeyJ'); T.run(1); ok(c > 0 && c < 1, `a half-second hold is only part-charged (${c.toFixed(2)})`); ok(!P().atk || P().atk.dir !== 'draw', 'and letting go then draws nothing'); note('chargeAt0.5s', c);
  });

  test('draw: letting go of a ready charge lunges about 130 u in 0.16 s, flat along the floor, and stops crisply', () => {
    T.reset(); G.dummies = []; T.down('KeyJ'); T.run(42); eq(P().charge, 1, 'ready'); const x0 = P().cx; T.up('KeyJ'); T.run(1); eq(P().atk.dir, 'draw', 'he draws');
    let steps = 1; while (P().swinging() && steps < 30) { T.run(1); steps++; } const dx = P().cx - x0;
    between(dx, 128, 142, `the lunge covers ${dx.toFixed(0)} u`); between(steps, 9, 11, `in ${steps} steps`); eq(P().y, 0, 'along the floor'); ok(P().onGround, 'still on it');
    ok(P().vx >= 0 && P().vx <= 110, `it ends at once, not sliding (${P().vx.toFixed(0)} u/s)`); T.run(3); eq(P().vx, 0, 'and he is still within a few steps'); note('drawU', dx); note('drawSteps', steps);
  });

  test('draw: it needs a full charge: let go at 0.5 s and nothing happens; at 0.65 s he draws', () => {
    T.reset(); G.dummies = []; T.down('KeyJ'); T.run(30); T.up('KeyJ'); T.run(30); ok(P().cx === 0 && !P().atk, 'released at 0.5 s: no draw');
    T.reset(); G.dummies = []; T.down('KeyJ'); T.run(39); T.up('KeyJ'); T.run(2); ok(P().atk && P().atk.dir === 'draw', 'released at 0.65 s: he draws');
  });

  test('draw: it strikes everything in the lunge once each, harder than a cut: both posts, a 6-step hit-stop, the dummies flung', () => {
    T.reset(); T.place(170, 0); T.down('KeyJ'); T.run(42); T.up('KeyJ'); const [a, b] = G.dummies; let n = 0, frozen = 0, vox = 0;
    while (a.hits === 0 && n++ < 20) T.run(1); eq(a.hits, 1, 'the first post is struck'); vox = Math.abs(a.vox); ok(vox > 150, `and flung (${vox.toFixed(0)} u/s a step later; a cut gives about 90)`);
    const t0 = P().atk.t; for (let i = 0; i < 12; i++) { T.run(1); if (P().atk && P().atk.t === t0) frozen++; else break; } between(frozen, 5, 7, 'the world holds for about 6 steps (0.1 s)');
    n = 0; while (b.hits === 0 && n++ < 30) T.run(1); eq(b.hits, 1, 'the second post, 100 u further on, is struck by the same lunge'); T.run(20); eq(a.hits, 1, 'and neither more than once');
    note('frozenSteps', frozen);
  });

  test('charge: he walks at 40 % speed while he charges, a dash breaks it for good, a jump does not, and he can charge and draw in the air', () => {
    T.reset(); G.dummies = []; T.down('KeyJ'); T.down('ArrowRight'); T.run(54); near(P().vx, 96, 0.001, 'charging, he walks at 96 u/s'); T.up('ArrowRight'); T.up('KeyJ'); T.run(30);
    T.reset(); G.dummies = []; T.down('KeyJ'); T.run(24); ok(P().charge > 0, 'charging'); T.tap('ShiftLeft', 1); T.run(3); eq(P().charge, 0, 'a dash breaks the charge'); T.run(50); eq(P().charge, 0, 'and it stays broken while the button is held'); T.up('KeyJ'); T.run(2); ok(!P().atk || P().atk.dir !== 'draw', 'and letting go draws nothing'); T.run(30);
    T.reset(); G.dummies = []; T.place(0, -1500); T.down('KeyJ'); T.run(42); eq(P().charge, 1, 'charged in the air'); ok(!P().onGround, 'still in the air'); const y0 = P().y, x0 = P().cx; T.up('KeyJ'); T.run(1); eq(P().atk.dir, 'draw', 'he draws in the air');
    let dy = 0; for (let i = 0; i < 30; i++) { T.run(1); if (!P().swinging()) break; dy = Math.max(dy, Math.abs(P().y - y0)); } eq(dy, 0, 'flat: no gravity during the lunge'); between(P().cx - x0, 128, 142, 'the same distance');
    T.reset(); G.dummies = []; T.down('KeyJ'); T.run(24); T.tap('Space', 3); T.run(2); ok(!P().onGround && P().charge > 0, 'a jump does not break the charge');
    T.up('KeyJ'); T.run(60);
  });

  test('charge: he coils as it builds (lower, leaning back), the glow swells, and a ready charge pulses a ring; the draw stretches him long and low with the blade out', () => {
    T.reset(); G.dummies = []; T.run(20); const rest = kanameCrouch(P().vis), lean0 = P().vis.lean; T.down('KeyJ'); T.run(48);
    ok(P().vis.charge > 0.9 && kanameCrouch(P().vis) > rest + 1.2, `he crouches (${kanameCrouch(P().vis).toFixed(1)} vs ${rest.toFixed(1)})`); ok(P().vis.lean < lean0 - 0.07, `and leans back (${P().vis.lean.toFixed(2)})`);
    T.up('KeyJ'); T.run(4); ok(P().vis.dash > 0.8 && P().vis.atk > 0.9 && P().swing.dir === 'draw', 'mid-draw he is stretched, with the blade out'); T.run(60); ok(P().vis.charge < 0.01 && P().vis.dash < 0.01, 'and he eases back to standing');
  });

  // ===================================================================================================================
  // BEHAVIOR — rung 7: health, being hit, the parry, and the red and yellow language
  // ===================================================================================================================
  // a lone Striker at x, with the next attack set by hand so a test can ask about one kind at a time
  const foe = (x = 60) => { const f = new Striker(x, 0); G.dummies = [f]; return f; };
  const setStrike = (f, cls) => { f.cls = cls; f.state = 'wind'; f.t = 0; f.landed = false; f.idleFor = 99; f.face = P().cx > f.x ? 1 : -1; };    // (facing him, as it does between attacks)
  const calm = f => { f.state = 'idle'; f.t = 0; f.idleFor = 99; f.openT = 0; };           // make it stand down (it would otherwise go on attacking by itself)
  const untilWind = (f, t, max = 120) => { for (let i = 0; i < max && !(f.state === 'wind' && f.t >= t); i++) T.run(1); };   // step until the wind-up has run for t seconds

  test('striker: it faces him, waits, then attacks in the order plain, yellow, red, yellow, plain, red, winding up 0.42, 0.62 and 0.70 s; and only when he is near', () => {
    T.reset(); const f = foe(140); const seq = [], winds = []; let wasWind = false, w = 0;      // 140 u away: in its range, but out of its reach
    for (let i = 0; i < 1500 && seq.length < 6; i++) {
      T.run(1); if (f.state === 'wind') { if (!wasWind) { seq.push(f.cls); w = 0; } w++; } else if (wasWind) winds.push(w); wasWind = f.state === 'wind';
    }
    eq(seq.join(), 'plain,yellow,red,yellow,plain,red', 'the order of its attacks'); eq(f.face, -1, 'it faces him'); eq(P().hp, 5, 'and from 140 u its blade does not reach him');
    const secs = c => winds[seq.indexOf(c)] / 60; near(secs('plain'), 0.42, 0.03, 'plain winds up 0.42 s'); near(secs('yellow'), 0.62, 0.03, 'yellow 0.62 s'); near(secs('red'), 0.70, 0.03, 'red 0.70 s');
    T.reset(); const g = foe(400); T.run(400); eq(g.state, 'idle', 'out of its range it stands and does nothing'); eq(g.next, 0, 'and has not begun');
  });

  test('hurt: a plain hit costs 1 health and a red hit 2, throws him back and up, stuns him for 0.22 s and leaves him untouchable for 1 s (D082)', () => {
    T.reset(); let f = foe(60); setStrike(f, 'plain'); let n = 0; while (P().hp === 5 && n++ < 80) T.run(1);
    eq(P().hp, 4, 'a plain hit costs 1'); ok(P().vx < -200 && P().vy < -150 && !P().onGround, `he is thrown back and up (${P().vx.toFixed(0)}, ${P().vy.toFixed(0)})`); near(P().invuln, 1, 0.02, 'untouchable for 1 s'); ok(P().stun > 0.15, 'and stunned');
    const frozen0 = G.hitstop; ok(frozen0 > 0.08, 'the world pauses as it lands');
    calm(f); T.run(20); T.place(0, 0); setStrike(f, 'plain'); T.run(30); ok(P().invuln > 0, 'still untouchable'); eq(P().hp, 4, 'a second hit while he is untouchable does nothing');
    T.reset(); f = foe(60); setStrike(f, 'red'); n = 0; while (P().hp === 5 && n++ < 90) T.run(1); eq(P().hp, 3, 'a red hit costs 2');
  });

  test('hurt: at 0 health he is down for 1.4 s and cannot act, then he is up again with full health', () => {
    T.reset(); let f = foe(60), hp = 5;
    for (const cls of ['red', 'red', 'plain']) { calm(f); T.run(90); T.place(0, 0); P().invuln = 0; setStrike(f, cls); let n = 0; while (P().hp === hp && n++ < 100) T.run(1); hp = P().hp; }
    eq(P().hp, 0, 'down to 0'); ok(P().deadT > 1.3, 'he is down'); T.down('ArrowRight'); T.run(30); ok(P().cx < 5, 'and does not answer the controls'); T.up('ArrowRight');
    T.run(80); eq(P().hp, 5, 'then he is up again with full health'); ok(P().invuln > 0, 'and untouchable for a moment'); eq(P().deadT, 0);
  });

  test('parry: L, V, L1 and L2 open a 0.26 s window; he can always press again, but a press within 0.5 s of the last gets a shorter one (0.16, 0.09, 0.05 s), and a pause resets it (Little Shinobi\'s rule)', () => {
    const win = () => { let w = 0; while (P().parryT > 0 && w < 60) { T.run(1); w++; } return w; };
    const press = () => { T.tap('KeyL', 1); return 1 + win(); };
    T.reset(); G.dummies = []; const w = [press(), press(), press(), press(), press()]; T.run(40); w.push(press());
    between(w[0], 14, 18, `the first press: ${w[0]} steps (0.26 s)`); between(w[1], 8, 12, `the second, at once: ${w[1]} (0.16 s)`); between(w[2], 4, 8, `the third: ${w[2]} (0.09 s)`); between(w[3], 2, 5, `the fourth: ${w[3]} (0.05 s)`);
    between(w[4], 2, 5, `and it stays that short: ${w[4]}`); between(w[5], 14, 18, `after a pause it is the full window again: ${w[5]}`); note('windowSteps', w);
    T.reset(); G.dummies = []; T.tap('KeyL', 1); T.run(3); T.tap('KeyL', 1); ok(P().parryT > 0, 'he can press again while the window is still open (there is no lockout)');
    T.reset(); G.dummies = []; T.tap('KeyV', 1); ok(P().parryT > 0, 'V parries');
    for (const b of [4, 6]) { T.reset(); G.dummies = []; T.pad({ pressed: [b] }); T.run(2); ok(P().parryT > 0, `pad button ${b} parries`); }
  });

  test('parry: it works wherever he stands and whichever way he faces, and he turns to the attacker by himself', () => {
    for (const [label, hx, face] of [['in front, facing it', -75, -1], ['close in front', -86, -1], ['on top of it, facing away', -96, 1], ['past it, facing away', -80, 1], ['well back, facing away', -30, 1]]) {
      T.reset(); const f = new Striker(-100, 0); G.dummies = [f]; T.place(hx, 0); P().face = face; setStrike(f, 'plain'); untilWind(f, 0.30); T.tap('KeyL', 1);
      eq(P().face, hx > -100 ? -1 : 1, `${label}: he turns toward it`); let n = 0; while (f.state !== 'open' && P().hp === 5 && n++ < 60) T.run(1); eq(P().hp, 5, `${label}: no damage`); eq(f.state, 'open', `${label}: it is turned aside`);
    }
  });

  test('parry: the press is never lost: made in a hit-stop it is kept, and made in the middle of his own swing it cancels the swing', () => {
    T.reset(); lone(40); T.tap('KeyJ', 1); let n = 0; while (G.hitstop === 0 && n++ < 10) T.run(1); ok(G.hitstop > 0, 'a blow has landed: the world is held'); T.down('KeyL'); T.run(1); T.up('KeyL'); T.run(8); ok(P().parryT > 0 || P().parryRecent > 0, 'the press made during it is kept and opens the parry');
    T.reset(); G.dummies = []; T.tap('KeyJ', 1); T.run(3); ok(P().swinging(), 'his blade is live'); T.tap('KeyL', 1); ok(P().parryT > 0 && !P().atk, 'a parry pressed then cancels the swing');
  });

  test('parry: a plain strike that lands in the window is only turned aside (0.22 s, no big opening, no zoom): no damage, the world held 0.12 s, him untouchable 0.35 s with his air dash back', () => {
    T.reset(); const f = foe(60); setStrike(f, 'plain'); P().canDash = false; untilWind(f, 0.30); T.tap('KeyL', 1);
    let n = 0; while (f.state !== 'open' && n++ < 40) T.run(1); eq(f.state, 'open', 'it is turned aside'); eq(P().hp, 5, 'no damage'); near(f.openT, 0.22, 0.03, 'for only 0.22 s'); eq(f.big, false, 'not a big opening');
    ok(G.fx.length === 1 && G.fx[0].type === 'parry', 'the parry flashes'); near(G.hitstop, K.HITSTOP_PARRY, 0.001, 'the world holds still for 0.12 s'); ok(P().invuln > 0.3, 'he is untouchable a moment'); ok(P().canDash, 'with his air dash back'); eq(G.kick, 0, 'a plain parry has no zoom');
    T.run(20); T.tap('KeyJ', 1); n = 0; while (f.hits === 0 && n++ < 20) T.run(1); near(G.hitstop, K.HITSTOP, 0.001, 'and a blow on it afterwards is an ordinary one');
  });

  test('parry: a yellow strike parried leaves it wide open for 1.6 s with the big-hit zoom, and a blow on it then lands hard', () => {
    T.reset(); const f = foe(60); setStrike(f, 'yellow'); untilWind(f, 0.50); T.tap('KeyL', 1);
    let n = 0; while (f.state !== 'open' && n++ < 40) T.run(1); eq(f.state, 'open', 'turned aside'); near(f.openT, 1.6, 0.03, 'open for 1.6 s'); eq(f.big, true, 'a big opening'); near(G.kick, 0.1, 0.01, 'the zoom kicks in');
    T.run(12); G.kick = 0; T.tap('KeyJ', 1); n = 0; while (f.hits === 0 && n++ < 20) T.run(1); eq(f.hits, 1, 'he cuts it while it is open'); near(G.hitstop, K.HITSTOP_CRIT, 0.001, 'a longer pause than a plain blow'); near(G.kick, 0.1, 0.01, 'and the zoom again');
    T.reset(); const g = foe(60); T.tap('KeyJ', 1); n = 0; while (g.hits === 0 && n++ < 20) T.run(1); near(G.hitstop, K.HITSTOP, 0.001, 'a blow on one that is not open is an ordinary one'); eq(G.kick, 0, 'with no zoom');
  });

  test('parry: too early (the window shut before the blade arrived) or too late (it had arrived) and the hit lands (D082); a press a step into the cut still catches it', () => {
    T.reset(); let f = foe(60); setStrike(f, 'plain'); untilWind(f, 0.05); T.tap('KeyL', 1); let n = 0; while (P().hp === 5 && n++ < 80) T.run(1); eq(P().hp, 4, 'a parry made 0.37 s early misses it');
    T.reset(); f = foe(60); setStrike(f, 'plain'); n = 0; while (!(f.state === 'strike' && f.t >= 0.06) && n++ < 80) T.run(1); T.tap('KeyL', 1); eq(P().hp, 4, 'one pressed after the blade has arrived is too late');
    T.reset(); f = foe(60); setStrike(f, 'plain'); n = 0; while (f.state !== 'strike' && n++ < 80) T.run(1); T.tap('KeyL', 1); n = 0; while (f.state !== 'open' && P().hp === 5 && n++ < 30) T.run(1); eq(P().hp, 5, 'one pressed as the cut begins still catches it (the blade needs 0.04 s to arrive)');
    T.reset(); f = foe(60); setStrike(f, 'plain'); untilWind(f, 0.30); T.tap('KeyL', 1); n = 0; while (f.state !== 'recover' && f.state !== 'open' && n++ < 60) T.run(1); eq(P().hp, 5, 'one pressed 0.12 s early catches it');
  });

  test('striker: his blows make it flinch but do not push it about, and it is not solid', () => {
    T.reset(); const f = foe(60); f.idleFor = 99; let peak = 0; T.tap('KeyJ', 1); for (let i = 0; i < 20; i++) { T.run(1); peak = Math.max(peak, Math.abs(f.ox)); }
    ok(f.hits === 1 && peak < 3, `a cut moves it only ${peak.toFixed(1)} u`); eq(f.x, 60, 'and it stays where it stands');
  });

  test('no circles: the parry\'s flash and its glints are drawn with no arcs at all', () => {
    const arc = ctx.arc; let n = 0; ctx.arc = function (...a) { n++; return arc.apply(this, a); };
    try { T.reset(); G.fx = [{ type: 'parry', x: 0, y: -20, t: 0.05, life: 0.3 }]; ctx.save(); ctx.setTransform(1, 0, 0, 1, 640, 400); drawFx(); drawGlint(ctx, 0, 0, 14, '255,214,74', 1); ctx.restore(); } finally { ctx.arc = arc; }
    eq(n, 0, 'no ring, no circle');
  });

  test('red: a parry does not turn a red strike aside; he must jump it or dash through it, and a dash makes him untouchable', () => {
    T.reset(); let f = foe(60); setStrike(f, 'red'); untilWind(f, 0.55); T.tap('KeyL', 1); let n = 0; while (P().hp === 5 && n++ < 60) T.run(1); eq(P().hp, 3, 'parrying a red strike takes the 2 anyway'); eq(f.state === 'open', false, 'and does not open it');
    T.reset(); f = foe(60); setStrike(f, 'red'); untilWind(f, 0.58); T.tap('Space', 3); T.run(60); eq(P().hp, 5, 'a jump over the low sweep avoids it');
    T.reset(); f = foe(60); setStrike(f, 'red'); untilWind(f, 0.64); T.tap('ShiftLeft', 1); ok(P().invulnerable(), 'a dash is untouchable'); T.run(60); eq(P().hp, 5, 'and a dash through the sweep avoids it');
  });

  test('language: a yellow glint flashes at a yellow attack\'s blade tip and a red glint at a red one, plain ones have none (D010); nothing else lights up, and nothing near him is red or yellow', () => {
    const tint = f => {
      G.cam.x = G.camPrev = f.x; G.cam.v = 0; G.hints = false; G.grid = false; render(); const px = ctx.getImageData(440, 300, 400, 300).data; let red = 0, yellow = 0;
      for (let i = 0; i < px.length; i += 4) { const r = px[i], g = px[i + 1], b = px[i + 2]; if (r > 200 && g < 90 && b < 90) red++; if (r > 215 && g > 175 && b < 100) yellow++; }
      return { red, yellow };
    };
    const seen = {};
    for (const cls of ['plain', 'yellow', 'red']) { T.reset(); T.place(-3000, 0); const f = foe(0); setStrike(f, cls); untilWind(f, STRIKES[cls].wind - 0.2); seen[cls] = tint(f); }
    note('glint', seen); eq(seen.plain.red + seen.plain.yellow, 0, 'plain: no glint'); ok(seen.yellow.yellow > 30 && seen.yellow.red === 0, `yellow: ${seen.yellow.yellow} yellow pixels, no red`); ok(seen.red.red > 30 && seen.red.yellow === 0, `red: ${seen.red.red} red pixels, no yellow`);
    for (const cls of ['yellow', 'red']) { T.reset(); T.place(-3000, 0); const f = foe(0); setStrike(f, cls); untilWind(f, 0.12); const e = tint(f); eq(e.red + e.yellow, 0, `${cls}: before the cue nothing is lit, not the blade and not the eyes`); }
  });

  test('health bar: one pale segment for each point of health and a dark one for each lost, in no red (D082)', () => {
    T.reset(); G.dummies = []; G.hints = false; G.grid = false; P().hp = 3; render(); const px = ctx.getImageData(0, 0, 1280, 60).data, at = x => { const i = (24 * 1280 + x) * 4; return [px[i], px[i + 1], px[i + 2]]; };
    const seg = i => at(640 - 73 + i * 30 + 13);
    for (let i = 0; i < 5; i++) { const [r, g, b] = seg(i); if (i < 3) ok(r > 190 && b > 230, `segment ${i + 1} is pale (${r},${g},${b})`); else ok(r < 90 && b < 120, `segment ${i + 1} is dark (${r},${g},${b})`); ok(!(r > 170 && g < 100 && b < 100), 'never red'); }
  });

  test('zoom kick: the big-hit zoom eases back to nothing within a fraction of a second', () => {
    T.reset(); G.dummies = []; G.kick = 0.1; T.run(5); ok(G.kick > 0.05 && G.kick < 0.1, 'it is easing'); T.run(30); eq(G.kick, 0, 'and gone');
  });

  // ===================================================================================================================
  // BEHAVIOR — soundness
  // ===================================================================================================================
  test('determinism: the same inputs give the same run', () => {
    const script = () => {
      T.reset(); const out = [];
      T.run(240, f => {
        if (f === 0) T.down('ArrowRight'); if (f === 10) T.tap('Space', 2); if (f === 60) T.down('Space'); if (f === 95) T.up('Space');
        if (f === 100) { T.up('ArrowRight'); T.down('ArrowLeft'); } if (f === 120) T.tap('ShiftLeft', 2); if (f === 150) T.tap('Space', 4); if (f === 160) T.down('ShiftLeft'); if (f === 190) T.up('ShiftLeft'); if (f === 200) T.up('ArrowLeft');
        if (f === 30) T.tap('KeyX', 1); if (f === 40) T.tap('KeyJ', 1); if (f === 70) { T.down('ArrowUp'); T.tap('KeyJ', 1); T.up('ArrowUp'); } if (f === 130) T.tap('KeyC', 1); if (f === 200) T.down('KeyJ'); if (f === 238) T.up('KeyJ'); if (f === 90) T.tap('KeyL', 1); if (f === 215) T.tap('KeyV', 2);
        if (f % 10 === 0) out.push(hash());
      });
      return out.join('|');
    };
    const a = script(), b = script(); eq(a, b, 'two identical scripts must match'); note('checkpoints', a.split('|').length);
  });

  test('drawing is pure: rendering changes nothing in the simulation', () => {
    T.reset(); T.down('ArrowRight'); T.run(40); T.tap('Space', 5); T.run(8); T.tap('ShiftLeft', 1); T.run(4); ok(P().trail.length > 0, 'a trail exists to be drawn'); T.run(30); G.dummies = [new Dummy(P().cx + 50, 0)]; T.tap('KeyJ', 1); T.run(5); ok(P().swordBox() || P().atk, 'a swing is in progress');
    const before = JSON.stringify([G.hero, G.dummies, G.hitstop, G.cam, G.camPrev, G.t]);
    const i0 = G.interp; G.interp = true; render(0.37); G.interp = false; render(1); G.interp = i0;
    eq(JSON.stringify([G.hero, G.dummies, G.hitstop, G.cam, G.camPrev, G.t]), before, 'state before and after drawing');
  });

  test('camera: it leads him a little and moves smoothly, never popping', () => {
    T.reset(); T.down('ArrowRight'); let prev = G.cam.x, worst = 0;
    T.run(180, () => { worst = Math.max(worst, Math.abs(G.cam.x - prev)); prev = G.cam.x; });
    ok(worst < 6, `the camera moved ${worst.toFixed(2)} u in one step`); note('worstCamStep', worst);
    const lag = P().cx + 36 - G.cam.x; ok(lag > 0 && lag < 60, `while he runs the camera trails its target by ${lag.toFixed(1)} u`);   // a spring chasing a moving target lags
    T.up('ArrowRight'); T.run(120); near(G.cam.x, P().cx + 36, 2.5, 'once he stops the camera settles 36 u ahead of him');
  });

  test('zoom: [ and ] zoom within limits', () => {
    T.reset(); for (let i = 0; i < 20; i++) T.tap('BracketRight', 1); near(G.zoom, 6, 0.001, 'upper limit');
    for (let i = 0; i < 40; i++) T.tap('BracketLeft', 1); near(G.zoom, 1.5, 0.001, 'lower limit');
  });

  test('scarf: it trails behind whichever way he runs, keeps its length, stays above the floor, and settles when he stops', () => {
    const tail = () => P().scarf.p[P().scarf.p.length - 1];
    T.reset(); T.down('ArrowRight'); T.run(60); ok(tail().x < P().cx - 15, `running right: the scarf tail is at ${(tail().x - P().cx).toFixed(1)}`);
    T.up('ArrowRight'); T.down('ArrowLeft'); T.run(70); ok(tail().x > P().cx + 15, `running left: the scarf streams the other way (${(tail().x - P().cx).toFixed(1)})`);
    T.up('ArrowLeft'); T.run(240); const t = tail(); ok(Number.isFinite(t.x) && Number.isFinite(t.y) && Math.abs(t.x - P().cx) < 45, 'settles near him, finite');
    const p = P().scarf.p; for (let i = 1; i < p.length; i++) near(Math.hypot(p[i].x - p[i - 1].x, p[i].y - p[i - 1].y), 2.4, 0.05, `segment ${i} keeps its length`);
    T.place(0, 0); T.run(10); for (const q of P().scarf.p) ok(q.y <= P().shadowY - 0.5, 'no point of the scarf goes below the floor');
    T.reset(); T.down('Space'); T.run(8); T.run(30); for (let i = 0; i < 60; i++) { T.run(1); for (const q of P().scarf.p) ok(q.y <= P().shadowY - 0.5, 'never below the floor, even landing'); }   // a full jump and landing
  });

  test('scarf: it is tied to the back of his head, at the headband, in every pose (it can never come loose)', () => {
    let checked = 0;
    const check = label => {
      const P = G.hero;
      for (const a of [0, 0.5, 1]) {                                   // as drawn between two steps, too
        const rx = lerp(P.px, P.cx, a), ry = lerp(P.py, P.y, a), f = kanameHeadFrame(P, rx, ry), pts = scarfPoints(P, rx, ry, a);
        near(pts[0].x, f.root.x, 1e-9, `${label}: the drawn scarf starts at the head (x)`); near(pts[0].y, f.root.y, 1e-9, `${label}: the drawn scarf starts at the head (y)`);
        const d = Math.hypot(f.root.x - f.head.x, f.root.y - f.head.y);
        between(d, 10.5, 14.5 + P.vis.dash * 3, `${label}: the root is ${d.toFixed(1)} u from the head's centre (it should sit on the rim, 12.4, a little more when a dash stretches him)`);
        ok((f.root.x - f.head.x) * P.face < -7, `${label}: the root is on the back of the head`);
        checked++;
      }
      const g = Math.hypot(P.scarf.p[1].x - P.scarf.p[0].x, P.scarf.p[1].y - P.scarf.p[0].y);
      ok(g < 5, `${label}: the first scarf segment is ${g.toFixed(1)} u long (about 2.4), so there is no gap at the head`);
    };
    T.reset(); check('standing');
    T.down('ArrowRight'); for (let i = 0; i < 40; i++) { T.run(1); check('running'); }
    T.down('Space'); for (let i = 0; i < 50; i++) { T.run(1); check('jumping'); } T.up('Space');
    for (let i = 0; i < 40; i++) { T.run(1); check('landing'); }
    T.up('ArrowRight'); T.down('ArrowLeft'); for (let i = 0; i < 20; i++) { T.run(1); check('turning'); }
    T.up('ArrowLeft'); T.run(30); check('stopped');
    T.place(-3000, 0); T.run(2); T.down('ArrowRight'); T.down('ShiftLeft'); for (let i = 0; i < 12; i++) { T.run(1); check('dashing'); } for (let i = 0; i < 40; i++) { T.run(1); check('sprinting'); }
    T.down('Space'); T.run(8); T.up('ShiftLeft'); T.tap('ShiftLeft', 1); for (let i = 0; i < 30; i++) { T.run(1); check('air dash'); } T.up('Space'); T.up('ArrowRight'); T.run(80); check('after');
    note('posesChecked', checked);
  });

  test('soak: random mashing for 30 s never produces a NaN, never leaves the ground plane', () => {
    T.reset(); const keys = ['ArrowLeft', 'ArrowRight', 'ArrowUp', 'ArrowDown', 'Space', 'KeyZ', 'ShiftLeft', 'KeyX', 'KeyJ', 'KeyC'], held = new Set();
    for (let f = 0; f < 1800; f++) {
      if (f % 5 === 0) for (const k of keys) { const want = Math.random() < 0.4; if (want && !held.has(k)) { T.down(k); held.add(k); } else if (!want && held.has(k)) { T.up(k); held.delete(k); } }
      T.run(1); const p = P();
      if (f % 30 === 0) { ok(finite(p.cx, p.y, p.vx, p.vy), `non-finite state at step ${f}`); ok(p.y <= 0.001, `fell through the ground (y ${p.y})`); ok(Math.abs(p.vx) <= 820.001 && p.vy <= 900.001, 'speed limits hold'); }
    }
    for (const k of held) T.up(k);
  });

  // ===================================================================================================================
  // PRESENTATION — photographs for a person to look at (the test also checks the pose state behind each one)
  // ===================================================================================================================
  test('look: standing, running, a dash early and late, a sprint and an air dash, close up (for judging the poses and the speed lines)', async () => {
    T.reset(); T.place(-3000, 0); G.hints = false; G.grid = false; G.zoom = 4;
    const centre = () => { G.cam.x = G.camPrev = P().cx; G.cam.v = 0; };
    T.run(40); centre(); await snap('standing');
    T.down('ArrowRight'); T.run(22); T.down('ShiftLeft'); T.run(3); centre(); await snap('dash, early');
    T.run(6); centre(); await snap('dash, late');
    T.run(40); centre(); await snap('sprinting');
    T.up('ShiftLeft'); T.up('ArrowRight'); T.run(60); T.down('Space'); T.run(8); T.up('Space'); T.down('ArrowRight'); T.tap('ShiftLeft', 1); T.run(5); centre(); await snap('air dash');
    T.up('ArrowRight'); T.run(120);
  });

  test('look: the sword: a forward strike mid-swing and landing, an up strike, a down strike in the air, close up (for judging reach and impact)', async () => {
    T.reset(); G.hints = false; G.grid = false; G.zoom = 3.4;
    const centre = () => { G.cam.x = G.camPrev = P().cx; G.cam.v = 0; };
    T.place(190, 0); T.run(10); T.tap('KeyJ', 1); T.run(1); centre(); await snap('forward strike, early');
    T.run(2); centre(); await snap('forward strike, landing a blow (everything holds still)');
    T.run(14); centre(); await snap('forward strike, the dummy flinching');
    T.run(40); T.place(-240, 0); T.run(10); T.down('ArrowUp'); T.tap('KeyJ', 1); T.up('ArrowUp'); T.run(3); centre(); await snap('up strike on the hanging dummy');
    T.run(40); T.place(240, -62); T.down('ArrowDown'); T.tap('KeyJ', 1); T.run(3); T.up('ArrowDown'); centre(); await snap('down strike in the air');
    T.run(60);
  });

  test('look: the swing frame by frame: a forward cut, the second cut (it alternates), an up cut and a down cut (for judging the swing)', async () => {
    T.reset(); G.dummies = []; G.hints = false; G.grid = false; G.zoom = 3.4;
    const centre = () => { G.cam.x = G.camPrev = P().cx; G.cam.v = 0; };
    const film = async (name, steps) => { let done = 0; for (const n of steps) { T.run(n - done); done = n; centre(); await snap(`${name} step ${String(n).padStart(2, '0')}`); } };
    T.run(20); T.tap('KeyJ', 1); await film('cut1', [1, 2, 4, 6, 9]);
    T.run(30); T.tap('KeyJ', 1); await film('cut2', [2, 5]);
    T.run(30); T.down('ArrowUp'); T.tap('KeyJ', 1); T.up('ArrowUp'); await film('up', [2, 5]);
    T.run(40); T.place(0, -70); T.down('ArrowDown'); T.tap('KeyJ', 1); await film('down', [2, 5]); T.up('ArrowDown');
    T.run(60);
  });

  test('look: the pogo: striking down, the hit, the bounce and striking again (for judging the bounce and the chain)', async () => {
    T.reset(); G.hints = false; G.grid = false; G.zoom = 3.2; lone(240, 0); T.place(240, -72); T.run(2);                 // (a couple of steps after the placing, so the scarf has caught up with him)
    const centre = () => { G.cam.x = G.camPrev = P().cx; G.cam.v = 0; };
    centre(); await snap('above the post');
    T.down('ArrowDown'); T.tap('KeyJ', 1); T.run(1); centre(); await snap('down strike, early'); let n = 0; while (P().strikes === 0 && n++ < 10) T.run(1);
    centre(); await snap('the blow lands (everything holds still)'); T.run(14); centre(); await snap('the bounce, rising'); n = 0; while (!(P().vy > 0) && n++ < 40) T.run(1);
    centre(); await snap('at the top'); n = 0; while (-46 - P().y > 18 && n++ < 60) T.run(1); T.tap('KeyJ', 1); T.run(3); centre(); await snap('falling back: striking again'); T.up('ArrowDown'); T.run(60);
  });

  test('look: the charge and the draw: charging, ready, the lunge frame by frame, and the dummies flung (for judging the draw)', async () => {
    T.reset(); G.hints = false; G.grid = false; G.zoom = 2.6; T.place(170, 0); T.run(3);
    const centre = () => { G.cam.x = G.camPrev = P().cx + 60; G.cam.v = 0; };
    centre(); await snap('standing');
    T.down('KeyJ'); T.run(28); centre(); await snap('charging, building'); T.run(18); centre(); await snap('charged: ready');
    T.up('KeyJ'); T.run(2); centre(); await snap('the draw, step 2'); T.run(3); centre(); await snap('the draw, step 5');
    let n = 0; while (G.dummies[0].hits === 0 && n++ < 20) T.run(1); centre(); await snap('the blow lands (everything holds still)');
    T.run(14); centre(); await snap('the draw, late'); T.run(12); centre(); await snap('the dummies flung, the trail fading'); T.run(60);
  });

  test('look: the language: a plain wind-up, the yellow glint, the red glint, the parry, the opened striker, and being hurt (for judging the telegraphs)', async () => {
    G.hints = false; G.grid = false; G.zoom = 3.4;
    const view = () => { G.cam.x = G.camPrev = 30; G.cam.v = 0; };
    T.reset(); let f = foe(70); setStrike(f, 'plain'); untilWind(f, 0.36); view(); await snap('plain wind-up (no glow)');
    T.reset(); f = foe(70); setStrike(f, 'yellow'); untilWind(f, 0.43); view(); await snap('yellow glint (parry it)');
    T.reset(); f = foe(70); setStrike(f, 'red'); untilWind(f, 0.52); view(); await snap('red glint (jump or dash)');
    T.reset(); f = foe(70); setStrike(f, 'yellow'); untilWind(f, 0.5); T.tap('KeyL', 1); let n = 0; while (f.state !== 'open' && n++ < 30) T.run(1); view(); await snap('the parry lands (everything holds still)');
    T.run(40); view(); await snap('the striker, left open');
    T.reset(); f = foe(70); setStrike(f, 'plain'); n = 0; while (P().hp === 5 && n++ < 80) T.run(1); T.run(3); view(); await snap('hurt: thrown back, blinking, one health lost');
  });

  test('look: idle, run, rise, apex, fall, landing, close-up, night', async () => {
    T.reset(); G.hints = true; T.run(30); await snap('idle (standing)');
    T.down('ArrowRight'); T.run(14); await snap('run, early'); T.run(7); await snap('run, mid-stride');
    T.down('Space'); T.run(6); T.up('ArrowRight'); await snap('jump: rising'); T.run(12); await snap('jump: near the top');
    T.up('Space'); T.run(12); await snap('jump: falling');
    let n = 0; for (let i = 0; i < 80 && !P().onGround; i++) { T.run(1); n++; } ok(P().onGround, 'he lands');
    ok(P().vis.sq > 0.05, `a landing squash should be visible (sq ${P().vis.sq.toFixed(3)})`); note('landSquash', P().vis.sq); await snap('landing: squashed');
    T.run(40); G.zoom = 5.5; await snap('close-up, standing'); G.zoom = 3;
    G.night = true; T.run(2); await snap('night mood'); G.night = false;
    T.place(Stage.PILLARS[2].x - 90, 0); T.down('ArrowRight'); T.down('Space'); T.run(20); await snap('beside the pillars, mid-jump'); T.up('ArrowRight'); T.up('Space');
  });

  // ---- runner entry points ----------------------------------------------------------------------------------------------
  window.__tests = tests.map(({ name, params, timeout }) => ({ name, params, timeout }));
  window.__runTest = async name => {
    const t = tests.find(x => x.name === name); for (const k in notes) delete notes[k];
    // The page's own loop is frozen (__holdRender), so nothing would draw during a long test. Draw every 10th simulated frame
    // instead, with the RNG saved and restored so drawing never changes what the simulation does. A draw error then fails the test.
    const step0 = window.step; let n = 0;
    window.step = dt => { const r = step0(dt); if (++n % 10 === 0) { const st = window.__rng.get(); try { render(); } finally { window.__rng.set(st); } } return r; };
    try { await t.fn(); return { ok: true, notes: { ...notes } }; }
    catch (e) { return { ok: false, message: e instanceof Fail ? e.message : (e && e.stack) || String(e), notes: { ...notes } }; }
  };
})();
