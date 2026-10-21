# IMPLEMENTATION 001, task 3: the cursed katana, base blade with its glyphs

**Status:** written by Opus on 2026-10-21, checked by an independent `/design-review` and revised to its findings (see *Review* at the end).
**Built in three revisions and approved by the designer (D139).** **Authorized by the designer on 2026-10-21, with all three questions answered as recommended:** skip the sprite trial and draw in code; match the sheathed hilt; start from the first guesses and tune after playing ("we're okay with the first guesses… I can see how they look, and we can make changes accordingly"). **Not authorized:** it waits for the designer's
word (rule 1). Implementer once authorized: Sonnet (D120, D131), on branch `u4-polish`.
**Decisions:** D122 (a dark-steel katana, never a scythe) · D130 (the chosen look, `design/concepts/cursed-katana/`) · **D132 (only the
base katana with its glyphs: no flames, motes or halo)** · D133 (the glyphs will light up with the blade's charge in rung 8, not now) ·
D123, D127 (the slash) · D135 (task 2b, approved: the blade rests on surfaces) · D124 (gameplay frozen) · D087 (the sheathed hilt's glow stays).
**Inputs:** `docs/briefs/IMPLEMENTATION_001_TASK_3_NOTES.md` (the eight open points; this brief settles them, below) and the painted
reference `design/concepts/cursed-katana/Kaname_Cursed_Katana_Unity_Transparent.png` (the faithful one; the vector is a rougher tracing).

**Time box:** one Sonnet implementation pass, `/gate-check fast`, then the designer plays. At most **one** tuning round, within the
bounds marked *(tunable)* below; otherwise stop and report.

## What changes, in one paragraph
The sword he swings stops being a violet-black blade wrapped in fire. It becomes the designer's katana without its effects: a curved
dark-steel blade of nearly constant width with a lighter bevel along the cutting edge, a thin restrained violet line on the edge, an
angled point, a thick ink outline, a tall rounded steel guard, a deep-violet grip wrapped with violet X crossings and a round steel pommel,
and **a row of faint violet glyphs along the blade's flat**. The aura, the glow dots along the edge and the five fire wisps are removed. The
sheathed hilt at his hip gets the same X wrap, guard and pommel so the two match. Nothing about where the blade is, how long it is,
when it moves or what it hits changes.

## How the open points are settled
| # | Point (from the notes) | Settled as |
|---|---|---|
| 1 | Flames and halo against range | **None on the blade (D132).** Everything the blade draws lies inside its own silhouette (test 1) |
| 2 | Sprite or procedural redraw | **Proposed: procedural**, in `KanameArt.DrawBlade`, *skipping the scratch-scene sprite trial that the designer's own guard rail in D130 asks for*: **the designer's yes is needed for that** (question 1). Why: the painted PNG has the flames and halo baked in, so it is no longer the target; the game is drawn in code (D118, C07); the slash tests measure the blade's rendered triangles (`blade.body`) and task 2b's surface rest needs the blade's outline as geometry. A sprite would break both. *A build choice, not taste; the designer may still ask for a sprite trial.* |
| 3 | Flames on a swinging blade | Moot (D132) |
| 4 | What survives at about 46 u | The silhouette (constant width, angled point, tall round guard, X-wrapped grip, round pommel), the bevel and the violet edge line, and **6 to 8 glyphs drawn as simple marks** of about 1 u. At game zoom they read as a row of small violet marks, not as letters; that is expected and enough |
| 5 | The sheathed hilt must match | **Proposed: the chosen hilt wins** (it changes the approved U1 idle look, so it needs the designer's yes, question 2): the idle hilt's three diagonal stripes become X crossings, its guard the tall rounded one. Its glow (D087) and the unsheath's charge glow, motes and ring stay exactly as they are |
| 6 | Glyphs echoing △ ✕ | At about 1 u they cannot read as button icons. Keep the art's set but **simplify each to two or three strokes**, and don't put a plain triangle next to a plain X |
| 7 | Provenance | Settled before (the designer's own art) |
| 8 | Acceptance tests | Below |

## The look (in the blade's own frame: x along the blade, the hand at the origin, the cutting edge on +y as today)
Everything is drawn in `DrawBlade`, under the existing transform (`Translate(W1)`, `Rotate(Ang)`, the flip so the edge leads). Nothing
else in `DrawKaname` moves.

1. **Frozen geometry.**
   - `HeroLook.BladeLen` stays 40.
   - The **tip stays at (5 + len, −len × 0.1) = (45, −4)**.
   - The guard stays centred at x = 4.6.
   - The grip's and pommel's **fills** stay within today's `GripPart` box (x −9 to 4, |y| ≤ 2); the outline may sit just outside it, as
     today's pommel already does. `HeroLook.SurfaceMargin` stays 0.45 and is not retuned.
   - The blade's axis marks `blade.0`–`blade.4` keep today's formula exactly.
   - So the reach, the slash's radius and every task 1, 2 and 2b number stay valid.
   - **Starting values are proposals.** Every number marked *(tunable)*, and the glyph count and shapes, are first guesses for the
     designer to judge in play (rule 3); the one tuning round moves them within their bounds.
2. **The blade.**
   - **Shape.** The spine follows today's spine curve (from (5.5, −1.7) through the control point (mid, −2.2) to the tip). The cutting edge
     runs parallel to the spine at a constant width of **2.4 u** *(tunable 2.0–3.0)*, measured perpendicular to the spine, up to about
     **4 u before the tip** (x ≈ 41.5). There it turns in a short straight angled cut (the point, about 38° off the axis) to the tip.
     The offset of a quadratic is not a quadratic: sample the edge as a polyline (24 points or more) in the shared outline function. No taper to a needle and no spike: a
     katana, never a scythe or a sabre (D122).
   - **The body.** Dark blue steel, about `#2b2d4c` (sampled from the painting).
   - **The bevel.** A lighter band along the cutting edge, about `#646f92`, a third of the blade's width.
   - **The edge line.** A thin violet line on the cutting edge itself, `Palette.HiltLt` (`#6a52b0`) or close, 0.5 u wide *(tunable
     0.35–0.7)*, **drawn inset by its half-width** so it stays inside the outline.
   - **The outline.** A thick ink outline (`Palette.Blk`), 0.45 u *(tunable 0.35–0.6)*, round the whole blade, guard, grip and pommel.
   - **The slash stays the brightest thing in a cut (D132).** No colour on the blade may be lighter than the slash's band `#b79cff` in
     luminance. Today's pale `#efe6ff` edge line and `#b79cff` fire band go.
3. **The glyphs.**
   - **Count and layout.** 6 to 8 marks along the flat, between the bevel and the spine, starting about 5 u past the guard and ending
     about 6 u before the tip: **the first glyph's centre at x ≥ 10.5, the last at x ≤ 39**.
   - **Shapes.** Each is 2 or 3 strokes about 1 u tall and **0.25 u wide**, simplified from the painting's set (a curl, an X, a T, a plus, a diamond, a hook,
     an angle).
   - **How they are drawn.** Violet `Palette.CurseRgb` at alpha **0.55** *(tunable 0.4–0.75)*: faint, with no glow beyond the stroke
     (no `GlowDot`).
   - **Groups and marks.** Draw them in their own canvas group **`blade.glyphs`**, each with a mark **`glyph.0` … `glyph.N`** at its
     centre.
   - **Why the marks.** Rung 8 will light them one by one by index (D133). This task lights none: they are all equally faint.
4. **The guard.** A tall rounded oval of steel (`Palette.Steel`) with an ink outline, **radii** 1.8 × 4.6 u (today's radii 1.6 × 4.2,
   `Ellipse(4.6, 0, 1.6, 4.2)`), at x = 4.6. The shared outline function owns the guard's extent too, so `BladeParts`' guard segment
   grows from (4.6, ±4.2) to (4.6, ±4.6): the 2b rest angles can move from the guard as well as from the slimmer blade.
5. **The grip and pommel.**
   - **The grip.** Deep violet `#2a1b55`, from x −6 to 4, with **4 violet X crossings** (`#5937b0`, about 0.6 u strokes) in place of
     today's diagonal lines.
   - **The pommel.** A round steel cap at x −7 (today's arc), with its ink outline.
6. **Removed from the blade.** The aura strokes (widths 11 and 6), the six glow dots along the edge, and the five wisps with their pale
   cores; `flick` and `seed` go with them if nothing else uses them.
7. **One outline, two users.** The outline that `DrawBlade` fills and the polyline `HeroLook.BladeParts` checks against surfaces (task 2b)
   must come from **one function** (the blade polygon, the guard oval, the grip rectangle and the pommel circle), so they cannot drift.
   **Make `DrawBlade` public** (as `DrawSlash` is), so the tests can draw it alone on a fresh canvas. The 2b surface rest then uses the new, slimmer shape. If a rest angle moves
   because the blade is slimmer, that is expected; the existing floor and surface tests decide, with no tolerance loosened.
8. **The sheathed hilt (`DrawKaname`, the `wA < 0.4` block).**
   - **Replaced:**
     - the three `HiltLt` stripes become two small X crossings;
     - the steel ellipse guard becomes the same tall rounded shape, at today's scale;
     - the steel pommel circle stays.
   - **Unchanged, untouched:** the two `GlowDot`s (D087), the six charge motes and the ready ring (the unsheath, D134), and the sheath
     (`Saya`).
9. **Not this task.** No lighting of the glyphs, no charge meter, no hilt aura or electricity, no sparks at a parry (all rung 8, D133). No
   cursed hand (task 7). The unsheath's own trail (`DrawCrescent`) and the slash (`DrawSlash`) are untouched.

## Frozen (a change here is a blocker, D124)
- Nothing in `unity/Assets/Scripts/Sim/`, the tuning asset, `traces/`, `js/` or `dev/` changes.
- **Unchanged in `HeroLook`:**
  - `Swings` (pivots and angles), `SwingAngle`, `SweepTime`, `BladeLen`;
  - `UpLeanRate` and everything else in `UpdateVis`;
  - `Place` (except that `BladeParts` and `GripPart` now come from the shared outline).
- **Unchanged in `KanameArt`:** `DrawSlash`, `DrawCrescent`, `DrawTrail`, the scarf, body, head and parry glint.

## Files
- `unity/Assets/Scripts/Look/KanameArt.cs`: `DrawBlade`, the sheathed-hilt block.
- `unity/Assets/Scripts/Look/HeroLook.cs`: the shared blade outline, used by `BladeParts` and `GripPart`.
- `unity/Assets/Scripts/Look/Palette.cs`: the new colours, named (for example `BladeSteel`, `BladeBevel`, `Wrap`, `WrapX`).
- A new `unity/Assets/Tests/EditMode/KatanaTests.cs` (or a section of `LookTests.cs`).
- The docs below.

## Tests (EditMode, on the rendered triangles where they can be; nothing loosened)
**Before the first edit,** run a probe on `HEAD` and save two things in the test files with the commit hash:
- for test 2, the farthest `blade.body` vertex from W1 in every case;
- the `SlashTests` `CONTACT` lines (B, L or N for every case).

Tests 1, 4 and 5 draw the blade alone: `DrawBlade` on a fresh canvas, where the only transform is Translate(W1), Rotate(Ang) and the
flip, which invert exactly from the `Pose`. Alpha is measured at `WA` = 1.

1. **Nothing outside the blade.**
   - **What is checked:** every triangle `DrawBlade` emits, with alpha ≥ 0.04, in every group.
   - **The rule:** each lies within **0.6 u** of the union of the shared outline's shapes (the blade polygon, the guard oval, the grip
     rectangle, the pommel circle), in the blade's own frame.
   - **Where it runs:** every step of a forward (both alternations), up and down cut, standing, running and sprinting, both facings;
     plus the unsheath and the guard pose.
   - **What it guards:** no halo, aura or wisp can return to read as reach (D123, D132).
2. **The reach is unchanged.** For the same cases, the farthest vertex of `blade.body` from W1 is within **0.5 u** of the saved `HEAD`
   value (about 45.6 today, the tip plus the old edge stroke's round cap; about 45.4 expected).
3. **One outline.**
   - Every vertex of `blade.body` lies inside the union of the shared outline's shapes, grown by **max(outline, edge line) / 2 × 1.16
     + 0.05 u** (the canvas's mitres reach 1.155 × the half-width).
   - The blade polygon's tip is (45, −4) within 0.05 u.
4. **The glyphs.**
   - **Count and placement:** 6 to 8 `glyph.*` marks, in order from guard to tip. Every one lies inside the blade polygon, with the
     first centre at x ≥ 10.5 and the last at x ≤ 39.
   - **Faintness:** every triangle of `blade.glyphs` has the same alpha (none lit), at most 0.75.
5. **The slash is brighter.** No triangle of `blade.body` or `blade.glyphs` has a higher Rec. 709 luma (on the sRGB values, ignoring
   alpha) than `#b79cff`. The guard's steel `#9aa6b8` is the closest colour and passes.
6. **A katana's width,** measured on the shared outline function (not on triangles), perpendicular to the spine at 25 %, 50 % and 75 %
   of the spine's arc length: the three widths are within 10 % of each other, and the blade narrows only in the last 4 u.
7. **The colour law.** `NothingOnKanameIsRedOrYellow` keeps running over every pose, the sword, the unsheath and the guard included.
8. **Unchanged and passing:**
   - every existing EditMode test, with no tolerance, window or threshold loosened;
   - the 10 PlayMode tests;
   - the golden-replay checks: the frozen paths clean in `git status`, the 73 files, `node dev/trace.mjs --check`, and the 73
     `ReplayMatchesTheJavaScriptGame` cases passed in the result file.
9. **The contact risk, watched.** The blade loses about 1.7 u of width on its cutting-edge side, the side that leads into a target, and
   the contact tolerance is 2.05 u. Diff the `CONTACT` lines against the saved `HEAD` ones and **report every B→L or B→N change, even
   when the test still passes.** Watch these tests in particular:
   - `TheFrozenHitFrameShowsTheBlowReachingTheTargetInsideItsWindow`;
   - `EveryUpStrikePositionThatShowedItsBladeBeforeStillDoes` (the close backward up hits lean on `blade.body`);
   - the down strike near a surface.

   If one fails, **stop and report it with the numbers.** Do not move an angle or a tolerance, and do not widen the blade within its
   tunable range to rescue a test: the width is the designer's call.

## Evidence for the designer
- **Strike sheets** (`Kaname > Render the strike sheet`, and *with hitboxes*), before (task 2b, `HEAD`) and after, side by side, into
  `unity/Logs/task3/`.
- **The pose sheet**, before and after: the sheathed hilt, the guard, the charge.
- **One close-up** of the forward cut's hit frame: a crop of the strike sheet, upscaled about 6×, next to the painted reference (in the
  style of `Kaname_Katana_Asset_Comparison.png`). The shape match is judged there.
- **Night** is not rendered by the current sheet tools: say plainly that it is for the designer's Editor check (no new Editor script).
- The **before** sheets may reuse task 2b's renders if they are still in `unity/Logs/` (rule 13).

## Docs, in the same change
- **`docs/UNITY-PORT.md`:** a *Rendering differences* entry for the new blade and hilt, and the polish row.
- **`CLAUDE.md`:** the "Changing in Unity" line. Task 3 is built, awaiting the designer's playtest.
- **`design/DECISIONS.md`:** a log entry. Open question 9 is closed when the designer approves.
- **The task 3 notes:** point to this brief.
- **`LookTests`:** the message near line 168 that still mentions the aura, reworded.

**No commit** until the designer has looked.

## How the designer checks it in Unity
1. **At game zoom, by day and at night (N):** does it read as a katana in a cut, and as the painted design without its flames?
2. **Cut at the posts and the hanging dummies:** standing, running, sprinting, up and down, and a pogo on the post. Is the slash still
   what shows the reach, with the steel visible under it?
3. **The glyphs:** do the faint marks along the flat look right, or should they be stronger or weaker? That is the one tuning round.
4. **The pose reel (P):** the sheathed hilt, the guard and the unsheath's charge glow.
5. **The feel must be identical:** the same timing, hit-stop and pogo. This change is drawing only.

## Questions for the designer (before authorizing)
1. **Skip the sprite trial?** Your guard rail in D130 says to try the transparent sprite in a scratch scene first. I recommend skipping
   it: the PNG has the flames and halo baked in, which D132 removed, and drawing in code keeps the tests and the surface rest working.
2. **The sheathed hilt.** Give the hilt at his hip the same X wrap and tall guard as the new sword, so the two match? I recommend yes. It
   changes the approved idle look slightly; its glow stays.
3. **Starting values.** Width 2.4 u, glyphs at alpha 0.55, 6 to 8 of them, simplified to 2 or 3 strokes. These are first guesses for
   the one tuning round after you play it.

## Review (`/design-review`, 2026-10-21, an independent Opus pass)
**Verdict on the first draft: not ready, but close.** It found one blocker (test 3 failed by construction: `blade.body` holds the guard,
grip and pommel, and the tolerance ignored the edge line and the canvas's mitres) and these notes:
- test 1 could not isolate the blade, so `DrawBlade` is made public;
- test 2 needed a procedure;
- the slash-contact risk is real and on the cutting-edge side;
- the guard's numbers are radii, and the taller guard moves the rest angles too;
- the grip box and `SurfaceMargin`;
- test 4's x bound against the glyph layout, the stroke width, and alpha measured at `WA` = 1;
- test 5's formula, and test 6 measured on the outline;
- the evidence the sheet tools cannot render.

It also raised the three questions above. **All are folded in above.** The geometry was confirmed against the code: the tip (45, −4), the
guard at 4.6 and the `blade.0`–`blade.4` marks, which stay inside the slimmer blade. The shape is buildable. The frozen list, the colour
law and keeping rung 8 out were found sound.
