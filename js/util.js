'use strict';
// ---------------------------------------------------------------------------
// util.js — constants, math helpers, input (keyboard + gamepad)
// ---------------------------------------------------------------------------
const VW = 1280, VH = 720;     // the logical view; the canvas backing store scales to the real screen
const STEP = 1 / 60;           // the simulation is a fixed 60 Hz step; only drawing varies
const G = {};                  // global game state, filled in by game.js

const clamp = (v, a, b) => Math.max(a, Math.min(b, v));
const lerp = (a, b, t) => a + (b - a) * t;
const sign = v => (v < 0 ? -1 : v > 0 ? 1 : 0);
const approach = (v, t, d) => (v < t ? Math.min(v + d, t) : Math.max(v - d, t));
const ease = t => t * t * (3 - 2 * t);

// A critically damped spring (the camera): s = { x, v } chases `target` over about `smooth` seconds, with momentum.
function smoothDamp(s, target, smooth, dt) {
  const w = 2 / smooth, x = w * dt, e = 1 / (1 + x + 0.48 * x * x + 0.235 * x * x * x);
  const d = s.x - target, t = (s.v + w * d) * dt;
  s.v = (s.v - w * t) * e; s.x = target + (d + t) * e;
}

// ---------------------------------------------------------------------------
// Input. Actions are logical; each maps to several keys. "hit" = pressed this step (survives until a
// simulation step consumes it), "held" = currently down. Ported from Little Shinobi, with an analog stick added.
// ---------------------------------------------------------------------------
const PAD_DEADZONE = 0.18, PAD_FULL = 0.95;     // the stick: nothing below 0.18, full speed at 0.95
const padCurve = a => { const m = Math.abs(a); return m < PAD_DEADZONE ? 0 : sign(a) * Math.min(1, (m - PAD_DEADZONE) / (PAD_FULL - PAD_DEADZONE)); };

const Input = (() => {
  const BIND = {
    left: ['ArrowLeft', 'KeyA'],
    right: ['ArrowRight', 'KeyD'],
    up: ['ArrowUp', 'KeyW'],
    down: ['ArrowDown', 'KeyS'],
    jump: ['Space', 'KeyZ'],
    dash: ['ShiftLeft', 'ShiftRight', 'KeyX', 'KeyK'],     // tap: dash;  hold: sprint
    parry: ['KeyL', 'KeyV'],                               // the parry (L1 or L2 on the pad)
    attack: ['KeyJ', 'KeyC'],                              // the sword (hold up for an up strike; hold down in the air for a down strike)
  };
  const PREVENT = new Set(['Space', 'ArrowUp', 'ArrowDown', 'ArrowLeft', 'ArrowRight', 'Tab']);
  const down = new Set(), pressed = new Set();
  const gp = { held: {}, edge: {}, axis: 0 };

  addEventListener('keydown', e => {
    if (PREVENT.has(e.code)) e.preventDefault();
    if (!e.repeat) pressed.add(e.code);
    down.add(e.code);
    usingPad = false;
  });
  addEventListener('keyup', e => down.delete(e.code));
  addEventListener('blur', () => down.clear());

  // ---- gamepad (PlayStation DualShock 4 / DualSense, Xbox, Switch Pro…) ----
  // Browsers only reveal a controller after you press one of its buttons while the page has focus.
  // Chrome gives PlayStation pads the W3C "standard" layout: 0 ✕  1 ○  2 □  3 △  4 L1  5 R1  6 L2  7 R2  12-15 d-pad.
  // Firefox on macOS often reports the raw layout: 0 □  1 ✕  2 ○  3 △  4 L1 …, d-pad = a hat on axes[9].
  // Dash is ○, R1 or R2 (whichever the thumb or finger likes; the same buttons on both layouts apart from ○). The sword is □ (button 2; 0 on the raw layout). The parry is L1 or L2.
  let usingPad = false, padId = '';
  function readPad(p) {
    const b = i => !!(p.buttons[i] && (p.buttons[i].pressed || p.buttons[i].value > 0.5));
    const std = p.mapping === 'standard';
    const X = std ? 0 : 1;
    let dl = false, dr = false, du = false, dd = false;
    if (std) { du = b(12); dd = b(13); dl = b(14); dr = b(15); }
    else {
      const h = p.axes[9];
      if (h !== undefined && h >= -1.01 && h <= 1.01) {            // hat: -1 up, then clockwise in steps of 2/7
        const i = Math.round((h + 1) / (2 / 7)) % 8;
        du = i === 7 || i === 0 || i === 1; dr = i >= 1 && i <= 3; dd = i >= 3 && i <= 5; dl = i >= 5 && i <= 7;
      }
    }
    const ax = padCurve(p.axes[0] || 0), ay = p.axes[1] || 0;
    return {
      jump: b(X),
      dash: b(std ? 1 : 2) || b(5) || b(7),
      attack: b(std ? 2 : 0),
      parry: b(4) || b(6),
      left: ax < -0.4 || dl, right: ax > 0.4 || dr,
      up: ay < -0.55 || du, down: ay > 0.55 || dd,
      axis: dl ? -1 : dr ? 1 : ax,                                  // analog horizontal, -1..1 (the d-pad is a full push)
    };
  }

  function poll() {
    const pads = window.__fakePad ? [window.__fakePad] : (navigator.getGamepads ? navigator.getGamepads() : []);
    const list = Array.from(pads).filter(c => c && c.connected !== false);
    const live = c => c.buttons.some(x => x.pressed) || c.axes.slice(0, 2).some(v => Math.abs(v) > 0.5);
    const p = list.find(c => c.id === padId) || list.find(live) || list[0] || null;
    const cur = p ? readPad(p) : {};
    gp.axis = cur.axis || 0;
    let any = false;
    for (const k in BIND) {
      const was = !!gp.held[k], is = !!cur[k];
      gp.held[k] = is;
      if (is && !was) { gp.edge[k] = true; any = true; }
    }
    if (any) { usingPad = true; padId = p.id; }
  }

  return {
    held: a => (BIND[a] || []).some(c => down.has(c)) || !!gp.held[a],
    hit: a => (BIND[a] || []).some(c => pressed.has(c)) || !!gp.edge[a],
    // horizontal intent, -1..1: the keys are a full push; the stick is analog (so a light touch walks)
    moveX() {
      const k = ((BIND.right.some(c => down.has(c)) ? 1 : 0) - (BIND.left.some(c => down.has(c)) ? 1 : 0));
      return k || gp.axis;
    },
    rawHit: code => pressed.has(code),
    endStep() { pressed.clear(); for (const k in gp.edge) gp.edge[k] = false; },
    poll,
    get usingPad() { return usingPad; },
  };
})();
