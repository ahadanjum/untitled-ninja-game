# IMPLEMENTATION 001, task 1: the slash arcs match the hitboxes

> **Revision 1 (2026-10-18, approved by the designer). It overrides the sections below where they differ.**
> **Why:** the first build matched the boxes but froze the wrong picture. Most blows land on the first live step, and the hit-stop
> freezes that frame. With the original values the blade sat at the near edge of the box there: on the up strike it pointed behind him,
> away from the dummy above, and the forward hit lay along the floor at the post's foot. Eight floor tests also failed, all while
> sprinting, on the faint fade-out frames: the hand slides back to the hilt and drags the fading blade and trail 0.3–0.8 u under the floor.
>
> 1. **Five angles per cut, not four:**
>    - **C** cocked at the press.
>    - **N** where the cut enters the box: the trail starts here.
>    - **I** where the blade is on the first live step: mid-box, the frame a hit freezes.
>    - **O** where the sweep ends.
>    - **E** where the follow-through ends.
>
>    The phases and their timing are unchanged:
>    - The wind-up runs from C to I (passing N) between the press and `Live0`.
>    - The sweep runs from I to O over `SweepTime`.
>    - The blade holds at O until `Live1`.
>    - The follow-through runs from O to E.
>
>    **Starting values** (from a model of the drawing; validate them against the real tests and adjust where they fail, reporting every change):
>
>    | Cut | Pivot | C | N | I | O | E |
>    |---|---|---|---|---|---|---|
>    | Forward, high to low | (11, −15) | −1.50 | −0.68 | −0.22 | 0.18 | 0.20 |
>    | Forward, low to high (`Alt`) | (11, −15) | 0.12 | 0.12 | −0.30 | −0.76 | −1.05 |
>    | Up | (14, −22) | −2.85 | −2.60 | −1.80 | −1.22 | −1.00 |
>    | Down | (12, −10) | 0.80 | 1.20 | 1.75 | 2.45 | 2.60 |
>
>    The low-to-high floor margin is thin in the model: if it fails, move C and N up (toward 0.09). Do not move I or O for it.
> 2. **The trail runs from N** to the current angle, capped at O. From the first live step it covers what the blade has crossed. There is
>    still no trail before the box goes live.
> 3. **The trail stays where the cut happened.** It is drawn about the swing's pivot `(Px, Py)`, not about the hand, so it does not slide
>    down as the hand returns to the hilt during the fade. Arcs stay pinned to the world (the lean is taken off), as before.
> 4. **Tests:**
>    - **Floor:** keep it strict, at every frame from the press to the end of the fade, faint frames included.
>    - **Coverage:** replace "the tip's path covers 80 % of the box" with "the trail from N to O, at the tip's radius and clamped to the
>      box, covers at least 80 % of the box's height (forward) or width (up, down)".
>    - **New, the hit frame:**
>      - On the first live step, the tip is between 25 % and 75 % of the way across the box's long axis (forward: height; up and down:
>        width).
>      - In every hit case, on the frozen hit-stop frames, at least one of `blade.2`–`blade.4` lies within 4 u of the struck dummy's box.
>        The blade is visibly at the target.
>    - **Unchanged:** the 8 u tip tolerance and the outer-half overlap.
> 5. **Unchanged:** timing, hitboxes, reach, movement, the pogo, hit-stop, the draw and the guard, the blade's and the trail's look.

**Status:** written by Opus on 2026-10-18; **waits for the designer's authorization.** Implementer: Sonnet (D120).
**Decisions:** D123 (arcs redrawn to match the hitbox, timing unchanged), D124 (look only; gameplay frozen), D125 (the polish milestone).
**Read first:** `CLAUDE.md` (rules 5, 10, 11, 12), `design/DECISIONS.md` D100–D104 and D119–D126, `docs/UNITY-PORT.md` (U1, U4, the polish
milestone). Background only: `docs/Kaname_Documentation/Docs/COMBAT_ANIMATION.md` §3–4.

## The goal
The visible damaging part of every forward, up and down cut lies over the area its live hitbox really covers, and nothing is drawn
under the floor, **without changing anything the player can feel**. The whole blade tip need not sit inside the box on every live frame
(the designer's words: "ensure that the visible damaging portion corresponds to the active collision area").

## Frozen: do not touch
- **The simulation:** everything in `unity/Assets/Scripts/Sim/` (`Hero`, `World`, `Tuning`, `Dummy`, ...), `Settings/KanameTuning.asset`,
  `traces/`, `js/`, `dev/`. This task needs nothing from them. Timing, hitboxes, hit-stop and the pogo stay exactly as they are.
- **The draw** (`SwingDir.Draw`, U5) and **the guard** (`SwingDir.Guard`, U6): their arcs and easing are unchanged.
- **The look of the blade and the trail** (colours, widths, wisps, glow): tasks 2 and 3. **Body poses** (lean amounts, crouch): task 6.
- **Controls** (D126).

## What exists (measured)
- `Look/HeroLook.cs`: `Swings()` gives each cut a pivot `(Px, Py)`, a start and end angle `A0 → A1` and a radius `R = 45`;
  `SwingEase` is front-loaded and finishes the arc 0.112 s after the press; `Place()` moves the hand from the hilt to the pivot as `Vis.Atk`
  steps 0.4, 0.8, 1.0 over the first three steps, and sets `Ang` in **the body's frame** (it tilts with the lean).
- `Look/KanameArt.cs`: `DrawBlade` turns the blade about the hand by `Q.Ang`; `DrawCrescent` draws the trail from `A0` to the current
  angle at the tip's radius. Both sit inside `DrawKaname`'s `Scale(face …)` and `Rotate(v.Lean)`.
- The real boxes (`Hero.SwordBox`, relative to his feet, facing right; y up is negative): **forward** x 8…54, y −40…−2 (knee to head);
  **up** x −30…30, y −70…−30; **down** x −26…26, y −6…34. The blade is live from 0.03 s to 0.14 s after the press (steps 2–8, the press
  being step 0); the swing ends at 0.2 s (step 12).
- **The mismatch** (a model of the drawing, standing still):
  - Forward, high to low: the tip is at or under the floor from step 3 on, down to 27 u under the floor. Only step 2 is over the box.
  - Forward, low to high: the cut starts 29 u under the floor.
  - Up: the tip is 15–16 u in front of its box on steps 6–8.
  - Down: already matches.
  - Running and sprinting make the forward cut worse, because the arc tilts with the lean.

## What to build
### 1. Swing phases tied to the simulation's own timing (`HeroLook.cs`)
For **Fwd, Up and Down only**, replace the single eased `A0 → A1` with four angles (cock **C**, in **I**, out **O**, end **E**) and four
phases read from the swing's own times:

| Phase | When | Angle |
|---|---|---|
| Wind-up | press → `Live0` | `C + (I − C)·u²`, u = T / Live0 (it accelerates into the cut) |
| Sweep | `Live0` → `Live0 + SweepTime` | `I + (O − I)·(1 − (1 − u)²)` (ease-out) |
| Hold | → `Live1` | `O` |
| Follow-through | `Live1` → `Total` | `O + (E − O)·(1 − (1 − u)²)`, while the existing fade runs |

- `SweepTime = 0.08` s is a **look constant** in `HeroLook` (beside `BladeLen`), not a tuning number. The old arc finished 0.112 s
  after the press; the new sweep finishes at 0.11 s.
- Add `Live0` to `LookAtk`. Fill it in `HeroView.Fill` from `Hero.Atk.Live0`, and in `PoseReel`'s own swing (0.03).
- **Forward cuts keep alternating** (`Alt`): high-to-low and low-to-high are two arcs of their own, not one arc reversed.

**Starting values** (initial tuning from a model of the drawing; the tests below decide, and any change from these is reported):

| Cut | Pivot (Px, Py) | C | I | O | E |
|---|---|---|---|---|---|
| Forward, high to low | (11, −15) | −1.50 | −0.68 | 0.22 | 0.26 |
| Forward, low to high (`Alt`) | (11, −15) | 0.10 | 0.10 | −0.72 | −1.05 |
| Up | (14, −22) | −2.85 | −2.60 | −1.22 | −1.00 |
| Down | (12, −10) | 0.80 | 1.20 | 2.45 | 2.60 |

Radians; 0 = straight ahead, positive = down (as now). The blade's flip (`Scale(1, −1)` so the edge leads) follows the direction I → O.

### 2. Arcs pinned to the world, not to the lean
The arc angle is the blade's angle **in the world**. Subtract the body's current lean (`Vis.Lean`) when the blade and the trail are
placed, so leaning into a run or a sprint moves his hand but does not tilt the cut. Otherwise a sprinting forward cut dips up to 15 u
under the floor. The hand's place still moves with the body.

### 3. The trail covers only the damaging sweep (`KanameArt.DrawCrescent`)
Draw it from **I** to the current angle, capped at **O**: no trail over the wind-up (C → I) or the follow-through (O → E). Keep its
current look (widths, colours, fade); task 2 redraws it.

### 4. Marks: test exactly what is drawn (`Ink/InkCanvas.cs`)
- Add `public readonly Dictionary<string, V2> Marks` and `public void Mark(string name, double x, double y)`. A mark stores the point
  through the current transform, draws nothing, and is cleared by `Clear()`.
- In `DrawBlade`, mark the blade's axis (the curve from (5.5, 0) through (mid, −1.2) to the tip) at 0, ¼, ½, ¾ and 1 of the way from
  the guard to the tip: `blade.0` … `blade.4` (`blade.4` is the tip).
- In `DrawCrescent`, mark the trail's two ends at the tip's radius: `trail.from` and `trail.to`.

### 5. A developer-only hitbox outline
- With the debug view on (the **`** key, `World.View.Debug`), draw the live sword box (`Hero.Blow()?.Box`) as a thin outline after
  Kaname, in `HeroView`. Draw the dummies' boxes fainter, in `DummyView`.
- Use pale blue (`Palette.Rim`), **never red or yellow**. It is off by default and never part of play: rule 5 forbids drawn hitboxes in
  the game, and this is a test aid. `GameDriver.Present` passes the flag.
- The strike sheet gets the same overlay as an option: `StrikeSheet.Render(folder, boxes: true)`, plus a menu item
  **Kaname > Render the strike sheet with hitboxes** (into `Logs/strike-sheet-boxes/`).

### 6. Keep the docs true, in the same change
- `docs/UNITY-PORT.md`: under *Rendering differences*, replace "None yet" with this task's differences (the four-angle phases and their
  values, world-pinned arcs, the trail's span, the overlay). The JavaScript drawing keeps the old arcs. Mark task 1 built in the polish
  table.
- `CLAUDE.md` "How Kaname is drawn": the "Changing in Unity" line says the arcs are built and the rest is still to come.
- `design/DECISIONS.md`: a log entry.

## Tests
**Update:** `LookTests.TheSwingsHaveTheDesignedArcs`: the new C, I, O, E and pivots for each cut, `SweepTime` 0.08; the draw and the
guard unchanged.

**New** (EditMode, `Tests/EditMode/SlashTests.cs`). Drive the real `World` with scripted presses the way `StrikeSheet` does: step the
look as `GameDriver` does (the kicks, `Fill`, `Update`, and nothing on a frozen step). Draw every step on an `InkCanvas` at alpha 1 and
read the marks.
- **Cases:**
  - forward (both alternations) and up, standing, running at 240 u/s, and sprinting at 336 u/s
  - down in the air, over the post and in open air
  - facing right and left
  - one whiff and one hit each (a hit holds 4 frozen steps)
1. **The visible damaging sweep lies over the live hitbox.** On every step where `Hero.Blow()` is not null:
   - (a) at least one of `blade.2`, `blade.3` and `blade.4` (the outer half) lies inside the box grown by 3 u;
   - (b) the tip `blade.4` is at most 8 u outside the box.

   Over the live steps of one cut, the tip's path, clamped to the box, covers at least 80 % of the box's height (forward) or width (up,
   down).
2. **Nothing is drawn under the floor.** On the ground, for forward and up cuts, from the press to the end of the fade, no blade or
   trail mark is more than 0.5 u below his feet.
3. **The trail covers only the live sweep.** There is no trail on the steps before the box goes live, and `trail.to` never passes O.
4. **Marks change nothing:** drawing with marks leaves the simulation and the look as they were (extend `DrawingChangesNoState` if
   needed).

**Must still pass, unchanged:**
- every other EditMode test, including `SwordTests` and the 73 golden replays
- the scarf, red-or-yellow, finite-geometry and blade-only-in-a-cut look tests
- the PlayMode tests

Run them with `unity test unity --mode EditMode`, and run PlayMode too.

## Risks
- **The new arcs replace ones the designer approved (D104).** The cut looks different: a 103° sweep instead of 132°, and the blade
  raised at the press. The timing and the reach are the same.
- **The first two frames now show only the cocked blade, with no trail.** That is the start of the wind-up that task 6 builds on.
- **A sprinting cut now stays level** instead of tilting with him.
- **The pose reel builds its own swing:** give it `Live0`, or its cuts break.
- **The strike and pose sheets change.** Expected: keep the old ones for the before-and-after.

## How the designer verifies it in Unity
1. Render the strike sheet before (from `main`) and after. Compare the first five shots: nothing under the floor, and the trail over the
   struck area.
2. Render the strike sheet with hitboxes: the bright part of each cut lies over its box.
3. Play `TestStage` and press **`** to show the boxes:
   - Strike the post at x 240 from several distances: touching, 20 u, 45 u and 60 u away. It is struck exactly when the drawn cut
     reaches it.
   - Up-strike the hanging dummy at x −240 from below and to each side.
   - Pogo the post, and climb the hanging stair.
   - Cut while running and while sprinting: the cut stays level, and nothing passes through the floor.
   - Repeat at night (N) and at normal zoom.
4. **The feel must be identical:** the same timing, hit-stop and pogo. Anything that feels different is a bug to report.

## Report expected from Sonnet
- **Files changed.**
- **Tests run, with counts:** EditMode, PlayMode, and the 73 traces.
- **The before-and-after sheets:** where they are.
- **Any change from the starting angles,** and why.
- **What needs the designer's hands.** Never claim an Editor playtest that did not happen.
