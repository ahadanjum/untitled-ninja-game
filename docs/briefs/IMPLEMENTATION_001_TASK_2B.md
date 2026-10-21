# IMPLEMENTATION 001, task 2b: hit-frame readability (proposal)

**Status:** authorized 2026-10-19 and built 2026-10-21 by Sonnet (uncommitted); **waits for the designer's playtest.** What was built and measured: `docs/UNITY-PORT.md`, *Task 2b*. (Originally a proposal written by Opus on 2026-10-19.) Implementer once authorized:
Sonnet (D120). It builds on task 2 (uncommitted on branch `u4-polish`).
**Decisions:**
- D128: the up strike may hit slightly behind him where the blade visibly connects; substantial invisible hits farther behind are
  undesirable.
- D123 and D127: the slash.
- D124: gameplay frozen.
- Open questions 7 and 8.

> **Authorized 2026-10-19, with these notes from the designer (they override R3 below where they differ):**
> - **Real surfaces.** The floor clip must account for the stage's actual surfaces, the pillars and the ledge, not only the floor under
>   his centre. Use `Stage.FloorBelow` at the x of what is being drawn (for example the hand and the blade's tip), and test near the
>   ground, on top of a pillar, and beside a pillar's edge.
> - **The katana must not look cut off.**
>   - Clip only the slash layers (`cut.*`, `after`) at the surface.
>   - Do not clip the physical blade. Instead, for a forward, up or down cut, limit the blade's drawn angle so its tip stays at or above
>     the surface. The blade comes to rest on the ground rather than sinking into it.
>   - This is a look-only angle limit that applies only near a surface; the hitbox is unchanged.
>   - Report how it changes the down strike's hit-frame contact table.
> - **Unchanged:** the up-strike hitbox, the sword's length, the slash's size, the timing, the hit-stop, the pogo and the controls.

**Evidence:** `design/concepts/task2-investigation/` (hit-stop frames, day and night, at game zoom, with the collision region shown;
`measurements.tsv`).

**Time box:** one Sonnet implementation pass and one Opus review.
- If the targets below are not met with the values given, Sonnet may make **one** tuning round, within these bounds only:
  - the up strike's hand height, y between −26 and −30;
  - the straightening rate, between 30 and 50 per second.
- Otherwise it stops and reports the measurements. No open-ended art iteration. Task 3 (the katana) follows.

## The problems this addresses (and the ones it doesn't)
| Issue | Meaningful? | In 2b? |
|---|---|---|
| **Up strike, invisible hits behind him.** The collision region is the box's top-back corner. The frozen frame shows the slash 11–37 px short standing, up to 63 px sprinting. It covers 23–34 % of up-strike positions. | Yes (D128) | **Yes**, rendering only, for most of it |
| **Down strike drawn through the floor** whenever he is within about 35 u of the ground, an ordinary pogo included | Yes, very visible at night | **Yes**, rendering only |
| Down strike, late and edge grazes: the post overlaps 0.2–6.5 u of the box's edge, and the slash is 3–8 u away | Minor; the bounce shows the hit | **No** (low priority, recorded) |
| The thin leading line ahead of the blade reads weakly at game zoom (D127) | Taste | **No.** One number (its alpha) if the designer asks after reviewing task 2 |

## Rendering-only changes (2b)
Nothing in `Scripts/Sim/`, the tuning asset, `traces/`, `js/` or `dev/` changes. Hitboxes, timing, hit-stop, movement, the pogo and
controls are untouched.

### R1. He straightens up for an up strike (`Look/HeroLook.cs`, `UpdateVis`)
**Why:** the lean of a run or a sprint (up to about 0.4 rad) carries his hand forward and down. That more than doubles the invisible
region behind him: today it starts 27.5 u behind standing, 22.5 u running and 12.5 u sprinting.

**Change:**
- While an up strike is in progress (the press to the end of the swing), the lean target drops the run and sprint parts and keeps only
  the up strike's own −0.04. He straightens to cut upward, as COMBAT_ANIMATION asks for the up strike: "body compresses and rotates
  upward".
- It eases at about **40/s** instead of 16/s, so about a quarter of the run or sprint lean is left by the hit frame (step 2).
- It eases back the same way when the swing ends.
- The lean goes through the shared head frame, so the scarf stays tied (the existing test).

### R2. The up strike's hand is raised (`Look/HeroLook.cs`, `Swings(Up)`)
**Why:** the up box reaches 70 u above his feet and 30 u behind him; the blade's tip circles 45 u about a hand at (14, −22). Raising the
hand moves the whole arc up. The cut stays the same size, so the slash does not grow.

**Change:**
- The up strike's pivot moves from **(14, −22) to (13, −28)**: 6 u higher, 1 u back. The hand stays clear of his head's circle (by about
  0.8 u).
- Task 1's up angles (C −2.85, N −2.60, I −1.80, O −1.22, E −1.00) stay as they are, unless a task 1 test fails. If one does, make the
  smallest change to N, I or O that passes, and report it.

### R3. The cut stops at the floor (`Look/KanameArt.cs`, `DrawKaname`)
**Why:** a down strike close to the ground draws its blade and slash into the floor. It is very visible at night.

**Change:**
- While a forward, up or down cut is drawn, clip the blade (`DrawBlade`), the slash (`DrawSlash`) and the afterimage at the floor line
  directly under him (`HeroLook.ShadowY`), using the ink canvas's one convex clip.
- Set the clip in **world** coordinates, before the body's transform, as a rectangle from far above down to the floor line. Draw his
  shadow before the clip.
- Nothing else changes: the body, scarf, sheath and hilt are already above the floor, and the draw and the guard are untouched.
- The blade then reads as striking the ground. Known edge: over a pillar's edge the clip uses the floor under his centre.

### Expected result (modelled; the real triangles decide)
| Up strike, dummy behind him | Today: first invisible position | After R1 + R2 |
|---|---|---|
| Standing | 27.5 u behind (gaps 2–12 u) | about 37.5 u behind (gaps 3–7 u) |
| Running | 22.5 u behind (gaps 2–16 u) | about 35 u behind (gaps 2–8 u) |
| Sprinting | 12.5 u behind (gaps 3–21 u) | about 35 u behind (gaps 4–10 u) |

The cut's highest point rises about 3.5 u above the up box. That is inside task 1's 8 u tolerance, and the slash is no larger.

**What rendering cannot fix:** the box's last ~8 u at the back. Targets centred 35–43 u behind him are still struck with nothing drawn
within 2–10 u. Covering them by drawing would take a slash about 20 u longer than the blade, which is the oversized scythe D128 rules out.
See the gameplay option below.

## Gameplay option (NOT part of 2b; listed so the choice is clear)
**Trim the up box's back edge**, from 30 u behind his centre to about 22 u. Targets centred more than about 35 u behind would no longer
be hit, matching what is drawn after R1 and R2. The close-range backward hits D128 wants stay.

It requires:
- the designer's explicit decision, lifting D124 for this one number;
- the change in `js/hero.js` (`swordBox`) first, as the JavaScript game is the behaviour spec (D124);
- re-recording the traces it changes with `node dev/trace.mjs` and reviewing each diff;
- the same change in `Sim/Hero.cs`;
- updating the JS and C# sword tests in the same change.

My recommendation: decide it after trying 2b in Unity, since the remaining gap is the box's far corner (targets 35–43 u behind and above
him).

## Tests (measured on the rendered triangles, `SlashTests.ContactTolerance` 2.05 u; nothing weakened)
1. **Up strike, behind him.**
   - On the hit frame, B or L contact for every hittable dummy centred from **30 u behind** to 40 u ahead, standing, running and
     sprinting, facing both ways. The window grows from 10 u behind to 30 u: stricter, not weaker. The model clears it with about
     2.5 u to spare, since the model has been about 1 u optimistic before.
   - Report the gaps beyond 30 u.
2. **No regression of close-range backward coverage (D128).** Every up-strike position that is **B** today stays B.
3. **Range.** The up cut's outer edge and the blade tip stay within **4 u** of the up box (task 1's 8 u test still runs). No slash part
   lies outside radius R of its pivot (task 2's test).
4. **The floor.**
   - For down strikes at every height from just above the ground to the post's top (including a pogo on the post), and for every forward
     and up cut, no visible vertex of `blade.body`, `cut.*` or `after` lies below the floor line under him (within 0.05 u).
   - Task 1's strict floor test keeps running.
5. **The straightening.**
   - By the hit frame, at most 30 % of the run or sprint lean is left.
   - The lean returns afterwards.
   - The scarf stays tied to his head (the existing look tests).
6. **Unchanged and passing:**
   - every existing EditMode test, with no tolerance, window or threshold loosened;
   - the 10 PlayMode tests;
   - the four golden-replay checks: the frozen paths, the 73 files, `node dev/trace.mjs --check`, the copy's traces link, and the 73
     cases in the result file.

## Evidence the review needs
- **Strike sheets** before (task 2) and after, plain and with hitboxes.
- **Opus re-runs the investigation harness** (its own scratch copy) for the same up-behind, up-ahead, down-late and down-near-ground
  frames, by day and night, at game zoom, and compares them with `design/concepts/task2-investigation/`.

## Docs, in the same change
- **`docs/UNITY-PORT.md`:** the rendering-differences entry for R1–R3 and the measured table; the polish row.
- **`CLAUDE.md`:** the "Changing in Unity" line.
- **`design/DECISIONS.md`:** a log entry; open questions 7 and 8 updated with what is fixed and what remains.

**No commit.**

## How the designer checks it in Unity (after it is built)
1. **The up strike.**
   - Up-strike the hanging dummy from directly below, then step under it and past it by about 10, 20, 30 and 40 u, and strike again
     without turning. Up to about 30 u behind, the blade should visibly reach it; beyond 35 u, any hit is the remaining corner.
   - Repeat while running and sprinting.
2. **The pogo near the ground:**
   - Pogo on the post from low jumps, at night (N): nothing should pass through the floor.
3. **The look:**
   - Does straightening up for the up strike look natural while running?
   - Is the raised hand's arc still a good up cut?
4. **The feel must be identical:** the same timing, hit-stop and pogo.
