'use strict';
// ---------------------------------------------------------------------------
// stage.js — the test stage: a flat ground and a few blocks to judge heights against. World units (u) match
// Little Shinobi's pixels: Kaname is about 34 u tall and one tile is 32 u. y = 0 is the ground and up is negative.
// ---------------------------------------------------------------------------
const Stage = (() => {
  // Pillars of 1, 2, 3 and 4 tiles, 200 u apart, to judge a jump against (a held jump clears 3 tiles but not 4), and a
  // floating ledge whose edge tests coyote time and whose underside tests the head bump.
  const PILLARS = [{ x: 420, h: 32 }, { x: 620, h: 64 }, { x: 820, h: 96 }, { x: 1020, h: 128 }];
  const LEDGE = { x: 1260, y: -96, w: 200, h: 24 };
  const solids = [
    { x: -100000, y: 0, w: 200000, h: 4000, kind: 'ground' },
    ...PILLARS.map(p => ({ x: p.x, y: -p.h, w: 60, h: p.h, kind: 'block' })),
    { ...LEDGE, kind: 'ledge' },
  ];
  const overlaps = (a, s) => a.x < s.x + s.w && a.x + a.w > s.x && a.y < s.y + s.h && a.y + a.h > s.y;
  // the top of the highest solid under a point at or below `y` (where a shadow falls)
  function floorBelow(x, y) {
    let best = Infinity;
    for (const s of solids) if (x >= s.x && x <= s.x + s.w && s.y >= y - 0.5 && s.y < best) best = s.y;
    return best === Infinity ? 0 : best;
  }
  return { solids, PILLARS, LEDGE, overlaps, floorBelow, GRID: 32, GRID_LINES: 5 };
})();
