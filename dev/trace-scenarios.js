'use strict';
// dev/trace-scenarios.js — the golden traces for the Unity port (docs/UNITY-PORT.md).
//
// Each scenario here is run in the real JavaScript game, one fixed 60 Hz step at a time, and every step is recorded: what the
// controls said (held and pressed, as bit masks of ACTIONS, plus the horizontal push) and the state that came out. dev/trace.mjs
// writes them to traces/<name>.json. The C# simulation in unity/ replays the recorded controls and must reproduce the recorded
// state to within 0.001 u (unity/Assets/Tests/EditMode/GoldenTraceTests.cs). If the traces match, the feel matches.
//
// A scenario is { name, rung, note, setup, frames, script }:
//   setup   where he starts: { cx, y, vx, vy, camX } (any left out are the usual start: standing at the origin)
//   frames  how many steps to record
//   script  (i) => void, called before step i (0-based) to press or release keys with T.down / T.up
// Scenarios only use what the port has reached so far: add rows here as each rung (U1…U7) is ported.
(() => {
  const set = (rung, name, note, setup, frames, script) => ({ name, rung, note, setup, frames, script });
  window.__scenarios = [
    // U0: the body falls, lands and bumps; the camera follows. No controls are touched (he has no run, jump or sword in U0).
    set('U0', 'u0-stand-camera', 'standing at the start: the camera eases from 0 to its 36 u lead', {}, 90),
    set('U0', 'u0-stand-camera-placed', 'placed at x=500 with the camera on him: it eases to 536', { cx: 500, camX: 500 }, 90),
    set('U0', 'u0-drop-ground', 'dropped from 200 u onto the ground', { cx: 0, y: -200, camX: 0 }, 120),
    set('U0', 'u0-drop-pillar', 'dropped onto the 1-tile pillar at x=420', { cx: 450, y: -300, camX: 450 }, 120),
    set('U0', 'u0-drop-fall-max', 'dropped from 3000 u: his fall speed is capped at 900 u/s', { cx: 0, y: -3000, camX: 0 }, 300),
    set('U0', 'u0-bump-ledge', 'thrown upward under the ledge at x=1260: he bumps his head and falls back', { cx: 1300, y: -10, vy: -900, camX: 1300 }, 150),
    set('U0', 'u0-land-on-ledge', 'dropped onto the ledge from above', { cx: 1350, y: -300, camX: 1350 }, 150),
  ];
})();
