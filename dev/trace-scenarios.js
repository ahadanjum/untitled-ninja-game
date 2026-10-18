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
//   script  (i) => void, called before step i (0-based) to press or release keys with T.down / T.up (or move the stick with T.pad); `at([[frame, 'down', 'ArrowRight'], ...])` builds one
// Scenarios only use what the port has reached so far: add rows here as each rung (U1…U7) is ported.
(() => {
  const set = (rung, name, note, setup, frames, script) => ({ name, rung, note, setup, frames, script });
  // a script from a list of [frame, T function, argument]: 'down' / 'up' a key, 'pad' a fake controller, 'nopad' to unplug it
  const at = list => i => { for (const [f, fn, arg] of list) if (f === i) T[fn](arg); };
  window.__scenarios = [
    // U0: the body falls, lands and bumps; the camera follows. No controls are touched (he has no run, jump or sword in U0).
    set('U0', 'u0-stand-camera', 'standing at the start: the camera eases from 0 to its 36 u lead', {}, 90),
    set('U0', 'u0-stand-camera-placed', 'placed at x=500 with the camera on him: it eases to 536', { cx: 500, camX: 500 }, 90),
    set('U0', 'u0-drop-ground', 'dropped from 200 u onto the ground', { cx: 0, y: -200, camX: 0 }, 120),
    set('U0', 'u0-drop-pillar', 'dropped onto the 1-tile pillar at x=420', { cx: 450, y: -300, camX: 450 }, 120),
    set('U0', 'u0-drop-fall-max', 'dropped from 3000 u: his fall speed is capped at 900 u/s', { cx: 0, y: -3000, camX: 0 }, 300),
    set('U0', 'u0-bump-ledge', 'thrown upward under the ledge at x=1260: he bumps his head and falls back', { cx: 1300, y: -10, vy: -900, camX: 1300 }, 150),
    set('U0', 'u0-land-on-ledge', 'dropped onto the ledge from above', { cx: 1350, y: -300, camX: 1350 }, 150),
    // U2: run and jump. Start well away from the striker (x -560) and the dummies.
    set('U2', 'u2-run-stop', 'hold right for a second, let go: the run, the top speed and the stop', { cx: -3000, camX: -3000 }, 130, at([[0, 'down', 'ArrowRight'], [60, 'up', 'ArrowRight']])),
    set('U2', 'u2-run-turn', 'run right, reverse to the left: the turn', { cx: -3000, camX: -3000 }, 140, at([[0, 'down', 'ArrowRight'], [45, 'up', 'ArrowRight'], [45, 'down', 'ArrowLeft'], [90, 'up', 'ArrowLeft']])),
    set('U2', 'u2-run-wasd', 'D then A move him the same way as the arrows', { cx: -3000, camX: -3000 }, 120, at([[0, 'down', 'KeyD'], [40, 'up', 'KeyD'], [40, 'down', 'KeyA'], [80, 'up', 'KeyA']])),
    set('U2', 'u2-jump-held', 'a held jump: about 118 u high, about 0.65 s in the air', { cx: -3000, camX: -3000 }, 140, at([[5, 'down', 'Space'], [75, 'up', 'Space']])),
    set('U2', 'u2-jump-tap', 'a tap of jump: cut short at about 45 u', { cx: -3000, camX: -3000 }, 100, at([[5, 'down', 'Space'], [8, 'up', 'Space']])),
    set('U2', 'u2-jump-mid', 'a twelve-step hold: between a tap and a hold', { cx: -3000, camX: -3000 }, 100, at([[5, 'down', 'Space'], [17, 'up', 'Space']])),
    set('U2', 'u2-jump-run', 'a running jump: air control and landing in motion', { cx: -3000, camX: -3000 }, 150, at([[0, 'down', 'ArrowRight'], [3, 'down', 'Space'], [40, 'up', 'Space'], [90, 'up', 'ArrowRight']])),
    set('U2', 'u2-jump-air-turn', 'jump, then reverse in the air', { cx: -3000, camX: -3000 }, 160, at([[0, 'down', 'ArrowRight'], [10, 'down', 'Space'], [30, 'up', 'ArrowRight'], [30, 'down', 'ArrowLeft'], [80, 'up', 'ArrowLeft'], [80, 'up', 'Space']])),
    set('U2', 'u2-jump-no-double', 'a second press in the air does nothing', { cx: -3000, camX: -3000 }, 110, at([[5, 'down', 'Space'], [23, 'up', 'Space'], [25, 'down', 'Space'], [27, 'up', 'Space']])),
    set('U2', 'u2-jump-buffer', 'a press 4 steps before landing still jumps', { cx: -3000, y: -96, camX: -3000 }, 90, at([[14, 'down', 'Space'], [15, 'up', 'Space']])),
    set('U2', 'u2-jump-buffer-early', 'a press 15 steps before landing does not', { cx: -3000, y: -96, camX: -3000 }, 90, at([[3, 'down', 'Space'], [4, 'up', 'Space']])),
    set('U2', 'u2-coyote-jump', 'a jump just after running off the ledge works', { cx: 1400, y: -96, camX: 1400 }, 110, at([[0, 'down', 'ArrowRight'], [23, 'down', 'Space'], [24, 'up', 'Space'], [60, 'up', 'ArrowRight']])),
    set('U2', 'u2-coyote-late', 'a jump long after running off the ledge does not', { cx: 1400, y: -96, camX: 1400 }, 110, at([[0, 'down', 'ArrowRight'], [32, 'down', 'Space'], [33, 'up', 'Space'], [60, 'up', 'ArrowRight']])),
    set('U2', 'u2-walk-off-ledge', 'walk off the ledge: he falls to the ground', { cx: 1400, y: -96, camX: 1400 }, 100, at([[0, 'down', 'ArrowRight'], [60, 'up', 'ArrowRight']])),
    set('U2', 'u2-pillar-1', 'a running jump onto the 1-tile pillar', { cx: 300, camX: 300 }, 140, at([[0, 'down', 'ArrowRight'], [20, 'down', 'Space'], [60, 'up', 'Space'], [100, 'up', 'ArrowRight']])),
    set('U2', 'u2-pillar-wall', 'run into the side of a pillar: he stops against it', { cx: 300, camX: 300 }, 80, at([[0, 'down', 'ArrowRight']])),
    set('U2', 'u2-runjump-pillar-3', 'a held run-jump clears the 3-tile pillar and lands on it', { cx: 740, camX: 740 }, 100, at([[0, 'down', 'ArrowRight'], [0, 'down', 'Space'], [18, 'up', 'ArrowRight'], [30, 'up', 'Space']])),
    set('U2', 'u2-runjump-pillar-4', 'but not the 4-tile pillar', { cx: 940, camX: 940 }, 100, at([[0, 'down', 'ArrowRight'], [0, 'down', 'Space'], [18, 'up', 'ArrowRight'], [30, 'up', 'Space']])),
    set('U2', 'u2-runjump-ledge', 'a held run-jump lands on the ledge', { cx: 1180, camX: 1180 }, 100, at([[0, 'down', 'ArrowRight'], [0, 'down', 'Space'], [18, 'up', 'ArrowRight'], [30, 'up', 'Space']])),
    set('U2', 'u2-head-bump', 'jumping under the ledge: he stops at 38 u and falls straight back', { cx: 1360, camX: 1360 }, 110, at([[3, 'down', 'Space'], [70, 'up', 'Space']])),
    set('U2', 'u2-stick', 'the stick: a dead zone, a light touch walks, full tilt runs', { cx: -3000, camX: -3000 }, 200,
      at([[0, 'pad', { axes: [0.1, 0, 0, 0] }], [30, 'pad', { axes: [0.5, 0, 0, 0] }], [70, 'pad', { axes: [1, 0, 0, 0] }], [110, 'pad', { axes: [-1, 0, 0, 0] }], [150, 'nopad']])),
    set('U2', 'u2-pad-jump', 'the cross button jumps on the standard layout', { cx: -3000, camX: -3000 }, 110, at([[5, 'pad', { pressed: [0] }], [60, 'pad', { pressed: [] }], [100, 'nopad']])),
    set('U2', 'u2-dpad', 'the d-pad moves him', { cx: -3000, camX: -3000 }, 130, at([[0, 'pad', { pressed: [15] }], [40, 'pad', { pressed: [14] }], [80, 'nopad']])),
    // U3: dash and sprint. A tap of the dash button bursts 128 u; holding it with a direction sprints at 336 u/s.
    set('U3', 'u3-dash-ground', 'a tap of dash on the ground: 128 u in 0.2 s along the floor, then it ends crisply', { cx: -3000, camX: -3000 }, 60, at([[5, 'down', 'ShiftLeft'], [6, 'up', 'ShiftLeft']])),
    set('U3', 'u3-dash-left', 'push left and tap dash: it goes left and he faces left', { cx: -3000, camX: -3000 }, 70, at([[0, 'down', 'ArrowLeft'], [5, 'down', 'ShiftLeft'], [6, 'up', 'ShiftLeft'], [40, 'up', 'ArrowLeft']])),
    set('U3', 'u3-dash-facing', 'push nothing and tap dash: it goes the way he faces', { cx: -3000, camX: -3000 }, 100, at([[0, 'down', 'ArrowLeft'], [10, 'up', 'ArrowLeft'], [40, 'down', 'ShiftLeft'], [41, 'up', 'ShiftLeft']])),
    set('U3', 'u3-dash-keys', 'X, K and the right Shift all dash', { cx: -3000, camX: -3000 }, 130, at([[5, 'down', 'KeyX'], [6, 'up', 'KeyX'], [40, 'down', 'KeyK'], [41, 'up', 'KeyK'], [80, 'down', 'ShiftRight'], [81, 'up', 'ShiftRight']])),
    set('U3', 'u3-dash-cooldown', 'a press after the 0.45 s cooldown dashes again', { cx: -3000, camX: -3000 }, 80, at([[0, 'down', 'ShiftLeft'], [1, 'up', 'ShiftLeft'], [40, 'down', 'ShiftLeft'], [41, 'up', 'ShiftLeft']])),
    set('U3', 'u3-dash-early-press', 'a press 0.22 s before the cooldown ends is ignored', { cx: -3000, camX: -3000 }, 70, at([[0, 'down', 'ShiftLeft'], [1, 'up', 'ShiftLeft'], [14, 'down', 'ShiftLeft'], [15, 'up', 'ShiftLeft']])),
    set('U3', 'u3-dash-buffered', 'a press 0.07 s before the cooldown ends is kept and fires when it ends', { cx: -3000, camX: -3000 }, 70, at([[0, 'down', 'ShiftLeft'], [1, 'up', 'ShiftLeft'], [23, 'down', 'ShiftLeft'], [24, 'up', 'ShiftLeft']])),
    set('U3', 'u3-dash-chain', 'it cannot be chained during the cooldown', { cx: -3000, camX: -3000 }, 70, at([[0, 'down', 'ShiftLeft'], [1, 'up', 'ShiftLeft'], [8, 'down', 'ShiftLeft'], [9, 'up', 'ShiftLeft']])),
    set('U3', 'u3-dash-air', 'in the air a dash is flat and cancels the fall; one per jump, refilled on landing', { cx: -3000, y: -1500, camX: -3000 }, 300,
      at([[20, 'down', 'ShiftLeft'], [21, 'up', 'ShiftLeft'], [60, 'down', 'ShiftLeft'], [61, 'up', 'ShiftLeft'], [250, 'down', 'ShiftLeft'], [251, 'up', 'ShiftLeft']])),
    set('U3', 'u3-dash-wall', 'a wall ends a dash: he stops against the pillar', { cx: 340, camX: 340 }, 60, at([[3, 'down', 'ShiftLeft'], [4, 'up', 'ShiftLeft']])),
    set('U3', 'u3-dash-jump-cancel', 'a jump cuts a dash short and keeps going forward', { cx: -3000, camX: -3000 }, 80, at([[0, 'down', 'ArrowRight'], [0, 'down', 'ShiftLeft'], [1, 'up', 'ShiftLeft'], [4, 'down', 'Space'], [5, 'up', 'Space'], [40, 'up', 'ArrowRight']])),
    set('U3', 'u3-jump-dash', 'a dash in the air after a jump', { cx: -3000, camX: -3000 }, 130, at([[0, 'down', 'ArrowRight'], [3, 'down', 'Space'], [15, 'down', 'ShiftLeft'], [16, 'up', 'ShiftLeft'], [40, 'up', 'Space'], [100, 'up', 'ArrowRight']])),
    set('U3', 'u3-sprint', 'hold dash and right: a burst, then 336 u/s; let go of dash and he drops to a run', { cx: -3000, camX: -3000 }, 110, at([[0, 'down', 'ArrowRight'], [0, 'down', 'ShiftLeft'], [70, 'up', 'ShiftLeft'], [90, 'up', 'ArrowRight']])),
    set('U3', 'u3-sprint-jump', 'the sprint speed carries through a jump while dash is held', { cx: -3000, camX: -3000 }, 130, at([[0, 'down', 'ArrowRight'], [0, 'down', 'ShiftLeft'], [40, 'down', 'Space'], [52, 'up', 'ShiftLeft'], [60, 'up', 'Space'], [100, 'up', 'ArrowRight']])),
    set('U3', 'u3-sprint-turn', 'sprint right, then reverse to the left', { cx: -3000, camX: -3000 }, 140, at([[0, 'down', 'ArrowRight'], [0, 'down', 'ShiftLeft'], [40, 'up', 'ArrowRight'], [40, 'down', 'ArrowLeft'], [90, 'up', 'ArrowLeft'], [90, 'up', 'ShiftLeft']])),
    set('U3', 'u3-sprint-no-direction', 'dash held with no direction: only the burst', { cx: -3000, camX: -3000 }, 70, at([[0, 'down', 'ShiftLeft'], [40, 'up', 'ShiftLeft']])),
    set('U3', 'u3-pad-dash', 'the circle, R1 and R2 dash on the standard layout', { cx: -3000, camX: -3000 }, 130, at([[5, 'pad', { pressed: [1] }], [6, 'pad', { pressed: [] }], [40, 'pad', { pressed: [5] }], [41, 'pad', { pressed: [] }], [80, 'pad', { pressed: [7] }], [81, 'pad', { pressed: [] }], [120, 'nopad']])),
    set('U3', 'u3-pad-sprint', 'stick right and R2 held sprints', { cx: -3000, camX: -3000 }, 100, at([[0, 'pad', { axes: [1, 0, 0, 0], pressed: [7] }], [80, 'nopad']])),
  ];
})();
