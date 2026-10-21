# IMPLEMENTATION 001, task 2: the two-layer slash

**Status:** written by Opus on 2026-10-18 and **revised the same day at the designer's request (revision 1, below)**; **waits for the
designer's authorization.** Implementer: Sonnet (D120). It builds on task 1 (commit `6f4785b`, branch `u4-polish`).
**Decisions:** D123 (a sharp inner crescent over the real damage, a soft outer afterimage that never implies range), D124 (look only;
gameplay frozen), D125 (the polish milestone), **D127** (the inner crescent shows the whole damaging sweep from the first live frame).
**Read first:** `CLAUDE.md` (rules 5, 10–12), `design/DECISIONS.md` D101, D102, D119–D127, `docs/UNITY-PORT.md` (the polish milestone and
*Rendering differences*), `docs/briefs/IMPLEMENTATION_001_TASK_1.md` (its revision 1 is how the arcs work now). Background:
`docs/Kaname_Documentation/Docs/COMBAT_ANIMATION.md` §3 and §8.

> **Revision 1 (2026-10-18, the designer's review), what changed from the first version of this brief:**
> 1. **The afterimage has an exact life.** It is drawn only while the blade can hit, it fades out over the hold so it reaches zero at
>    `Live1`, and nothing of it remains in the follow-through or after an early end.
> 2. **The cut follows the same rule.** The inner crescent is also gone at `Live1`; the first version let it fade for 2 more steps,
>    showing reach after the blow could no longer land.
> 3. **Contact is measured on the rendered triangles.** Instead of a few sampled marks with a 4 u tolerance, it uses the exact distance
>    with a 2 u tolerance. Each contact is classed as *blade or bright cut*, *the thin leading line only*, or *none*.
> 4. **No halo outside the tip's path.** Every part of both layers lies on or inside the cut's own outline (radius ≤ R, angle within
>    N…O).
> 5. **The 73 golden replays are verified explicitly.** That means the file count, the per-case results, where the copy reads its
>    traces, and `node dev/trace.mjs --check`.
> 6. **The up strike's gap behind him stays an open issue,** measured and reported. It is not accepted, and gameplay is not changed for
>    it.
>
> The contact windows also moved: up from 15 to 10 u behind, down from 35 to 30 u behind. That is because a model with the exact
> geometry and the tighter tolerance gives slightly smaller windows than the sampled one did.

> **Revision 2 (Opus's review of the first build, 2026-10-18). It overrides everything below where they differ.**
>
> **Every angle the slash uses is the blade *tip's* angle, not its axis's.** The curved blade's tip trails its axis by
> `lag = (O > N ? −1 : 1) · atan2(BladeLen · 0.1, 5 + BladeLen)` ≈ 0.0886 rad, about 4 u at the tip. This is exactly the lag task 1's
> trail used, so "the tip's path" below means the axis angle plus `lag`:
> - **The cut** runs from **N + lag** to **O + lag**.
> - **The bright part** ends at the blade's tip: its current angle plus `lag`, capped at O + lag.
> - **The afterimage** spans from tail + lag to head + lag, clamped to (I…O) + lag.
> - **The marks** (`inner.*`, `after.*`) sit at these angles.
> - **The tests** compare them with these angles, as task 1's trail test compared `trail.from` with N + lag.
> - **The range check** (test 4) uses the span (N…O) + lag.
>
> **Why:** the first build drew the cut at the axis angles, so it ran about 4 u past anywhere the blade's tip ever reaches. That is the
> range the afterimage and the cut must never imply. The axis angles also lost task 1's trail coverage margin, and they left the down
> strike's front end short of a post 30–35 u ahead when running.
>
> **What does not change:** task 1's angles, the timing, the windows and the tolerances.

> **Revision 3 (2026-10-19, the designer's approval).** The contact tolerance is **2.05 u**, not 2 u. The sprinting down strike at a post
> 35 u ahead measured 2.02 u from the cut's pointed front end; the designer approved the 0.05 u allowance for that 0.02 u miss and said
> not to narrow the attack's range or change its gameplay, hitboxes, timing or animations to satisfy the test. It is a test number
> (`SlashTests.ContactTolerance`), not a look or gameplay number. The windows and the 0.2 visibility alpha are unchanged.

## The goal
Replace the dark, scythe-like trail of the forward, up and down cuts with two layers:
- **An inner crescent, "the cut".** It is sharp and bright, and from the first live step it covers **the whole damaging sweep (N to O)**,
  so whatever frame a hit freezes shows where the blow reaches.
- **An outer afterimage.** It is soft and translucent violet and only follows the blade: a motion trail, never ahead of it, never beyond
  the cut's own outline.

Both layers exist **only while the blade can hit**. The blade stays on top and visible. Nothing a player can feel changes.

## Frozen: do not touch
- **The simulation:** everything in `unity/Assets/Scripts/Sim/`, `Settings/KanameTuning.asset`, `traces/`, `js/`, `dev/`, scenes,
  ProjectSettings and packages.
- **Task 1's geometry:** the five angles C, N, I, O, E, the pivots, `SweepTime`, the world pinning and the blade's placement. If a value
  looks wrong, report it; don't change it.
- **The blade itself** (`DrawBlade`: its shape, colours, aura, glow dots, wisps) is task 3. You may only add the test groups described
  below, which draw nothing. The hilt glow and every pose are untouched.
- **The draw** (`SwingDir.Draw`) keeps its current trail until it is ported in U5. The guard has no trail.
- **No spark at the point of contact** (task 4), **no pogo changes** (task 5), **no new rings, circles or markers** (rule 5).
- **The hitboxes, including the up strike's,** are frozen (D124). The open issue in "Known limits" is not to be fixed here.

## What exists (after task 1)
`KanameArt.DrawCrescent`, for a forward, up or down cut:
- From the first live step, it draws from N to the blade's current angle, capped at O, about the swing's pivot, with the lean taken off.
- It has four bands. Three of them are opaque **violet-black** (`28,12,64` at 0.5 and 0.45, `18,6,44` at 0.4) and up to 14 u wide at the
  tip. Under them is a soft violet glow, 1.4 u outside the tip's path, and along that path runs a pale edge.
- It fades from `Live1` to the end of the swing.
- It marks `trail.from` and `trail.to`.

The dark bands are what read as the head of a scythe on the blade's shaft. On the frozen hit frame the trail covers only N to I, so a
target ahead of the blade isn't touched by anything drawn.

## Timing: when each layer is drawn
`T` is the swing's time. `Live0` (0.03 s) and `Live1` (0.14 s) come from the swing. `SweepEnd = Live0 + min(SweepTime, Live1 − Live0)`,
which is 0.11 s, exactly as `HeroLook.SwingAngle` computes it.

| Interval | The cut (inner) | The afterimage (outer) |
|---|---|---|
| `T < Live0` (the wind-up, steps 0–1) | none | none |
| `Live0 ≤ T < SweepEnd` (the sweep) | full | full strength (factor 1) |
| `SweepEnd ≤ T < Live1` (the hold) | full; it may thin to half its width by `Live1` | strength factor `k = (Live1 − T) / (Live1 − SweepEnd)`, falling linearly to 0 at `Live1` |
| `T ≥ Live1` (the follow-through and after) | **none** | **none** |
| The swing ended early (`Atk` is null, for example a dash after a hit) | none | none |

- **Hit-stop:** the look freezes, so the frozen frames repeat the hit step's drawing exactly.
- **Steps at 60 Hz:**
  - The afterimage's factor is 0.78 on step 7 and 0.22 on step 8.
  - Step 9 (T = 0.15 s) is the first step with nothing.
  - The afterimage fades out smoothly: under a quarter of its strength on its last frame. The cut disappears whole, at the moment the
    blow stops being able to land.

## What to build (`Look/KanameArt.cs`, `Look/Palette.cs`, `Ink/InkCanvas.cs`)
Replace the forward, up and down path of `DrawCrescent` with `public static void DrawSlash(InkCanvas ctx, HeroLook P, Pose Q)`. It is
public so tests can draw it alone. `DrawKaname` calls it where it called `DrawCrescent`, before `DrawBlade`, so the blade stays on top.
Keep the old code path for the draw only. All angles are task 1's (radians, world-pinned, about the pivot, tip radius R = 45).
**Draw every part as a filled polygon, not a stroke.** Strokes and round caps spill half their width past the path; fills can be kept
exactly inside the cut's outline.

### Layer 1: the inner crescent ("the cut")
- **Where:** always the **whole span from N to O**. Its outer edge is the tip's path at radius R. It is pointed (zero width) at both
  ends, and lies entirely at radius ≤ R.
- **Behind the blade** (N to the blade's current angle, capped at O): the full cut.
  - a bright core, `SlashCore` `#f4efff`, from radius R − 1.2 to R;
  - a pale violet band inside it, `SlashBand` `#b79cff`, up to **4 u** wide at most (radius R − 4 to R − 1.2 at its widest), widest about
    two-thirds of the way from N;
  - a thin ink line along the band's inner edge, `SlashInk` `#24143e` at 0.5, 0.3 u wide, so it reads on the white test stage.
- **Ahead of the blade** (its current angle to O): **only a thin, dim line**: the core colour at alpha 0.5, from radius R − 0.6 to R,
  with no violet band and no ink. It shows where the cut reaches without a purple mass in front of the blade (D127 amends D102 for this
  line only).

### Layer 2: the outer afterimage
- **What:** soft translucent violet, `Palette.CurseRgb`, alpha **0.22 × k at most** at the blade, fading to 0 at its tail.
- **Where:** a band from radius **R − 8 to R**, inside the cut's outline. **Nothing beyond the tip's path:** there is no halo.
- **Span:** from its tail to its head, both clamped to **I…O**.
  - Its head is the blade's current angle.
  - Its tail is the blade's angle at `max(Live0, T − 0.05)`, using `HeroLook.SwingAngle`.
  - It never covers the wind-up's path (N to I).
  - It never runs ahead of the blade.
- It is drawn first, under the inner crescent, and only in the intervals the timing table allows.

### Test hooks in `InkCanvas` (they draw nothing; `Clear()` resets them)
- **Groups:** `BeginGroup(string name)` and `EndGroup()` record the range of triangles drawn between them, as `Groups`, a list of
  (name, first index, index count).
  - `DrawSlash` wraps each part: `cut.bright` (core, band and ink behind the blade), `cut.lead` (the thin line ahead) and `after` (the
    afterimage).
  - `DrawBlade` wraps only the blade's physical body as `blade.body`: the grip, pommel, guard, blade fill, spine, edge band and edge line.
    It does **not** include the aura strokes, glow dots or wisps.
- **Marks** (as in task 1):
  - `slash.pivot`: the pivot in the world.
  - `inner.from` and `inner.to`: N and O at radius R.
  - `inner.lead`: where the bright part ends.
  - `after.from` and `after.to`: the afterimage's tail and head at radius R.
  - **Remove `trail.from` and `trail.to`.** Update task 1's trail tests to read `inner.from` and `inner.to`, keeping what they assert: it
    starts at N, never passes O, and is absent before the box goes live.

### Remove
The three violet-black bands and the old glow band outside the tip's path, for the forward, up and down cuts.

## Tests (`Tests/EditMode/SlashTests.cs`; extend it, keep task 1's tests passing)
Contact and range are measured on the **rendered triangles** of the groups above:
- **Exact distance:** between a triangle and an axis-aligned box it is 0 if they overlap (a separating-axis test), otherwise the smallest
  distance between the triangle's edges and the box.
- **Visible triangles only:** a triangle counts as visible when its largest vertex alpha (after the canvas's global alpha) is at least
  0.2. Fainter ones are ignored for contact but still checked for range.
1. **The cut covers the whole damaging sweep while the blade can hit, and only then.**
   - On every step with `Live0 ≤ T < Live1`, `inner.from` is at N and `inner.to` at O (to 1e-6 in angle about `slash.pivot`, the lean
     taken off as task 1's test does).
   - On every other step there are no `cut.*` triangles and no `inner.*` marks. Hit-stop frames repeat the drawing of the step the blow
     landed on, which is a live step. So a late hit's frozen frames do show the cut; nothing may remain on the first step after the
     freeze once `T ≥ Live1`.
2. **The bright part ends at the blade.** On every live step, `inner.lead` is at the blade's current angle (capped at O) within
   0.02 rad.
3. **The afterimage's life and span.**
   - There are no `after` triangles on any step with `T < Live0` or `T ≥ Live1`.
   - There are none after an early end (a dash cancelling the swing after a hit), and none in the follow-through.
   - On hold steps, its largest vertex alpha is ≤ 0.22·k + 1e-6.
   - On every step, `after.to` is at the blade's angle (never ahead), `after.from` is the clamped tail, and both lie within I…O.
4. **Nothing beyond the cut's outline, nothing under the floor.**
   - Every vertex of `cut.bright`, `cut.lead` and `after` lies at a distance ≤ R + 0.05 from `slash.pivot`, and within the N…O span of
     angles (± 0.01 rad).
   - Every such vertex on a live step is within 8 u of the live box (task 1's tolerance for the tip).
   - Task 1's strict floor test also covers these vertices, at every step.
5. **The hit frame shows the blow reaching the target.**
   - Setup:
     - Standing, running and sprinting, facing both ways, for every hit position the simulation registers.
     - The positions are in 5 u steps of the target's centre from his centre, across each cut's whole hittable range.
     - Every case asserts `Hits` went up, so the check is never vacuous.
   - On the frozen hit-stop frames, class the contact with the struck dummy's box (2.05 u tolerance, revision 3) as:
     - **B:** a visible `blade.body` or `cut.bright` triangle is within 2.05 u;
     - **L:** only a `cut.lead` triangle is within 2.05 u;
     - **none:** nothing within 2.05 u (report the gap).
   - **Assert B or L inside these windows** (a failure inside a window means stop and report the measurements, not loosen the window):

     | Cut | Target | Window (target centre from his centre) |
     |---|---|---|
     | Forward | The post | 5 u ahead to the reach limit (about 65 u) |
     | Up | The lowest hanging dummy | 10 u behind to 40 u ahead |
     | Down | The post below him | 30 u behind to 35 u ahead |

   - **Print a table** of the class for every hittable position, inside the windows and out, for standing, running and sprinting. It
     goes in the report and in `docs/UNITY-PORT.md`.
   - Expected from a model with this geometry (the real triangles decide):
     - **Forward:** B from 5 u to the reach limit; a post centred on him that he stands inside has a gap of 3–7 u.
     - **Up:**
       - B above him (from about 25, 20 and 10 u behind standing, running and sprinting, to about 15–20 u ahead);
       - L only, 20 or 25 to 40 u ahead;
       - none from about 30, 25 and 15 u behind, out to 43 u behind (gaps 3–18 u).
     - **Down:** B from about 5–10 u behind to 35 u ahead; L only behind that.
   - **The L-only positions are the remaining visual mismatches.** Report them; they are not failures.
6. **No dark mass, nothing too strong.** Draw `DrawSlash` alone on a fresh canvas for each cut on a mid-sweep step and read the canvas
   colours:
   - none of the old band colours (`28,12,64`, `18,6,44`) appears;
   - every `after` vertex has alpha ≤ 0.22;
   - the only dark colour present is `SlashInk`.
7. **Unchanged and still passing:**
   - every other EditMode test, the 10 PlayMode tests, and task 1's tests;
   - the look tests: red or yellow, finite geometry, drawing changes no state, the scarf.

## Regression: the 73 golden replays, verified explicitly
A green suite is not enough on its own. The report must show each of these:
1. **The recordings are untouched.**
   - `git status --porcelain -- traces js dev unity/Assets/Scripts/Sim unity/Assets/Settings` prints nothing.
   - `ls traces/*.json | wc -l` is **73** (7 `u0-`, 23 `u2-`, 18 `u3-`, 25 `u4-`).
2. **The JavaScript game still records the same traces.** `node dev/trace.mjs --check` passes (it records every scenario again in
   headless Chrome and fails if any file in `traces/` would change).
3. **Where the replays read from.**
   - `GoldenTraceTests` reads `<project>/../../traces`.
   - For a headless run on the scratch copy, show that the copy's `traces` resolves to the repository's `traces/` (`readlink`, and
     `diff -rq` against it).
4. **The result file, not the summary line.** In the NUnit XML of the full EditMode run, count the `test-case` elements whose full
   name contains `GoldenTraceTests.ReplayMatchesTheJavaScriptGame`:
   - there must be **exactly 73**, all `Passed`, with none skipped, ignored or inconclusive;
   - their names must equal the 73 file names in `traces/`, compared as sets;
   - `TheTracesExist` and `TheActionOrderMatchesTheRecordings` must also have passed.

   Give the XML's path and the counts in the report.

## How to run it
**The live Editor:**
- EditMode only: `unity command recompile`, poll `recompile_status`, check `console_status`, then
  `unity command run_tests --mode editmode --timeout 600`.
- **Never** run PlayMode tests there, enter Play mode, or call `editor_stop` or `editor_play`.
- If the Editor is in Play mode or a command times out, don't assume it is frozen. Check `console_status --timeout 15` once, then go
  headless and report the timeout separately.

**Headless:**
- Sync `unity/Assets` exactly into the scratch copy (show `diff -rq` is empty) and run `unity test <copy> --mode EditMode` and
  `--mode PlayMode`, each with `--output` to an XML file.
- One complete run of the final code is required either way, and the regression checks above use its XML.

**Sheets:**
- Copy task 1's `unity/Logs/task1/after-*` to `unity/Logs/task2/before-*`.
- Render `unity/Logs/task2/after-plain/` and `after-boxes/`.
- Look at shots 01, 03 and 04 yourself.

## Docs, in the same change
- **`docs/UNITY-PORT.md`:**
  - Add a *Rendering differences* entry for the two layers: colours, widths, timing, and D127.
  - Replace the frozen-frame limit with the measured contact table.
  - Mark task 2 built in the polish table.
- **`CLAUDE.md`:** in "How Kaname is drawn", task 2 moves from "still to come" to "built".
- **`design/DECISIONS.md`:** a dated log entry with the test counts and the golden-replay checks.

**Do not commit.**

## Known limits (open; measure and report, do not fix here)
**The up strike behind him.**
- The up box reaches 30 u behind his centre and up to 70 u above his feet. A dummy whose centre is about 20–43 u behind him (about 15 u
  and more when sprinting) is struck while no part of the blade or the cut comes near it.
- No arc of this blade about this pivot reaches the box's back-top corner: the corner is 65 u from his hand, against a tip radius of 45.
- This is **unresolved** and listed among the open questions in `design/DECISIONS.md`. Two ways out exist, and both are the designer's
  call, not this task's:
  - move the up strike's pivot or arc (a look change that reopens task 1's angles);
  - reshape the up hitbox (a gameplay change, frozen by D124).
- This task only measures it, in test 5's table.

## Risks
- **The cut ahead of the blade is new** (D127 amends D102, which disliked purple in front of the blade). It is kept to a thin, dim line;
  if the designer finds even that too much, its alpha is the one number to lower.
- **On the white test stage a pale slash is faint.** The ink line carries it there. Check at night (N) and at normal zoom too.
- **The cut now disappears whole at `Live1`.** It is honest about range, but it may look abrupt. If the designer wants a softer end, a
  fade *inside* the live window (the hold) is the way, not after it.
- **The blade's own violet aura and wisps stay until task 3,** so the slash still sits next to a glowing blade.
- **Thin margins carried over from task 1:** the high-to-low trail coverage (+0.2 u) and the low-to-high sprinting floor (+0.83 u). The
  new geometry must not break them; if it does, report rather than move task 1's angles.

## How the designer checks it in Unity
1. Compare the strike sheets before and after (plain and with hitboxes), especially the hit-stop frames (shots 01, 03 and 04).
2. Play `TestStage` with **`** on:
   - Strike the post from touching distance out to the reach limit: on the frozen frame, the bright cut or the blade should reach it.
   - Up-strike the hanging dummy from below, a little behind, and from 20–40 u ahead (where only the thin line reaches it).
   - Pogo, and climb the stair.
   - Cut while running and sprinting.
   - Repeat at night (N) and at normal zoom.
3. **Taste:**
   - Does it read as a katana's cut rather than a scythe?
   - Is the thin line ahead of the blade welcome, or too much?
   - Is the violet afterimage soft enough?
   - Is the cut's sudden end at `Live1` acceptable?
4. **The feel must be identical:** the same timing, hit-stop and pogo.

## Report expected from Sonnet
- **Files changed.**
- **Every test run:** where it ran, passed, failed and total.
- **The four golden-replay checks** above, with their outputs.
- **The contact table** (B, L, none, with gaps) for standing, running and sprinting.
- **Sheet folders,** and a one-line verdict each for shots 01, 03 and 04.
- **Any Editor timeouts,** reported separately.
- **Anything not checked.** Never claim an Editor playtest that did not happen.
