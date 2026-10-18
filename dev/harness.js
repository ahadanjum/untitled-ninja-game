'use strict';
// dev/harness.js — loaded only when the page is opened with ?manual.
// It steps the simulation deterministically (independent of requestAnimationFrame) and drives Kaname with synthetic key
// events or a fake gamepad. Not used in normal play.
window.T = {
  down: c => dispatchEvent(new KeyboardEvent('keydown', { code: c, key: c, bubbles: true })),
  up: c => dispatchEvent(new KeyboardEvent('keyup', { code: c, key: c, bubbles: true })),
  run(n, fn) { for (let i = 0; i < n; i++) { if (fn) fn(i); Input.poll(); step(STEP); Input.endStep(); } },
  secs(s, fn) { T.run(Math.round(s * 60), fn); },
  tap(c, frames = 3) { T.down(c); T.run(frames); T.up(c); },
  hold(c, s) { T.down(c); T.secs(s); T.up(c); },
  // a clean start: no keys held, no pad, Kaname standing at the origin
  reset() { dispatchEvent(new Event('blur')); window.__fakePad = null; resetHero(); Input.endStep(); T.run(2); },
  // put Kaname somewhere (standing still, nothing held)
  place(x, y = 0) {
    const P = G.hero; P.cx = P.px = x; P.y = P.py = y; P.vx = 0; P.vy = 0; P.jumping = false; P.jumpBuf = 0;
    P.onGround = y === Stage.floorBelow(x, y - 1); P.coyote = P.onGround ? K.COYOTE : 0; G.cam.x = G.camPrev = x;
    if (P.onGround) T.run(1);                       // settle (an airborne placement must not move)
  },
  // a fake controller: standard mapping by default; axes [lx, ly, rx, ry], pressed button indices
  pad({ axes = [0, 0, 0, 0], pressed = [], mapping = 'standard', hat } = {}) {
    const buttons = Array.from({ length: 17 }, (_, i) => ({ pressed: pressed.includes(i), value: pressed.includes(i) ? 1 : 0 }));
    const a = axes.slice(); if (hat !== undefined) { a[9] = hat; }
    window.__fakePad = { id: 'test-pad', connected: true, mapping, axes: a, buttons };
  },
  nopad() { window.__fakePad = null; },
};
