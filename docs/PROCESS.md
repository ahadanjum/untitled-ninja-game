# Process: design first, build second

The designer's brief: spend a long time on the hero, the story and the play; then plan every area, progression, path and
shortcut slowly and methodically, each with a reason; then build. This file turns that into phases, each ended by a **gate**
the designer signs in `design/DECISIONS.md`. A later phase never starts before the earlier gate is signed. Going back is
allowed, but through the log: a decision is *superseded*, never silently edited.

## Scope: a demo first, shaped like a whole game
The target is a very good playable demo (D013) that is **Chapter 1** of a complete story (D019). The designer's order:
first **sketch everything**, the whole story, its twists, the world's shape and every ability's home, until it makes complete
sense and every clue has a place; then **plan the demo in depth** under the Purpose Rule; later chapters are planned in the
same depth as they are added. The paper checks cover the demo completely and the rest at the level of "every ability's lock
and key can be placed, and the story's clues pay off".

## How a design conversation runs
- **One topic at a time**, in the order below. Don't open the next until the last is logged.
- **Start from the designer's references and gut feelings**, not from a blank menu. They know what they want to feel; Claude
  turns that into proposals. (Last time, guessing at a boss's design cost four rebuilds in three days.)
- **Claude brings a recommendation**, says what it rules out, and says what would change its mind. The designer reacts.
- **Every decision is logged at once** in `design/DECISIONS.md`: what, why, what it rules out. Rejections go on the
  *do not re-propose* list.
- **Each step ends with a read-back**: one short paragraph in plain words that the designer confirms or corrects.
- **Research on demand.** When a topic needs it, Claude commissions focused reports (the first game's nine genre reports
  are already in `../hollow-shinobi/design-research/`). Findings reach the designer as a synthesis in plain words.
- **Taste is the designer's, craft is Claude's.** What the game *is* is asked; how to build it well is decided.

## Phase 0 — Foundations (the game on a page)
Ends at **Gate 0: a one-page "game on a page", up to five pillars, and a list of what we will not do.**

| Step | Settles | Output |
|---|---|---|
| 0.1 Feel and references | What the player should feel; the designer's touchstones (games, films, places, music); what to keep from Little Shinobi and what never again | Seed pillars; confirm / change / drop on every *inherited* item |
| 0.2 The hero | Who they are, what they want, what they fear, what they cannot do; silhouette; how a body with no dialogue shows personality; what their abilities say about them | `design/hero.md` |
| 0.3 The story | The wound in the world; the premise; the shape (acts, the turn, the endings); who the opposition is and why each boss exists; how all of it is told with no text | `design/story.md` |
| 0.4 The core play | The one verb everything hangs off; the loop at 30 seconds, 5 minutes, 1 hour; abilities as keys; combat and boss philosophy; rest, death, progression; what we explicitly reject | `design/play.md` |
| 0.5 Look, sound and tech | Palette and shape language; sound identity; whether assets stay generated in code; engine and stack (with a recommendation and a cost) | `design/look.md` |

Story, hero and mechanic are one idea seen from three sides: what the hero *can do* should say what the story is *about*,
and the world should be shaped by both. Expect to revisit 0.2–0.4 against each other before Gate 0.

**Code in this phase is limited to the feel ladder below** (D078). The world, rooms, enemies and content stay on paper.

## The feel ladder (D078, D079): the one place code is allowed in Phase 0
The designer's order: *design the character and model; test walking and a jump on a blank white stage; then the sword; then the look
and feel of a parry; build from the ground up and test each step.* So the first code is a **character test environment**: a blank
stage, the hero, a controller, and nothing else. It is not a throwaway: it is the foundation of the real game, built rung by rung, so
each rung keeps its tests and its tuned numbers.

**The rules**
1. **One rung at a time.** Each rung begins with the designer's go and ends with their sign-off, played by hand (PlayStation
   controller in Chrome).
2. **Nothing beyond the ladder is coded:** no rooms, no world, no real enemies (a training dummy is allowed). The world stays on paper.
3. **Four levels of evidence on every rung** (build, behavior, presentation, player experience). The last is always the designer's.
4. **The numbers are the design:** every tuned value is written down in `CLAUDE.md` as it is tuned.
5. **Drawing is separate from simulation,** so the hero's look can change (code-drawn 2D, sprites, or a 3D-to-2D render; D080)
   without touching how he moves.
6. **The first rung waits** for the designer's tech decision (engine and stack, and how the hero is drawn) and the rung's plan.

**Proposed rungs** (the designer reorders or cuts them)
| Rung | What it adds | What the designer judges |
|---|---|---|
| 0 | The stage (blank white) and the hero's design: silhouette, outfit, proportions, scarf, idle | Does he look like Kaname? |
| 1 | Walk and run, turn, stop | Weight, speed, the run animation |
| 2 | Jump: variable height, coyote time, buffering, landing | Floatiness, height, the landing |
| 3 | Dash (tap) and sprint (hold), and the air-dash rule (D096) *(approved, D099; invulnerability waits for real enemies)* | Distance, flow, the sprint |
| 4 | The sword: the basic strike, up and down strikes, hit-stop on a dummy *(approved, D104)* | Reach, speed, impact |
| 5 | Pogo and attack-to-reset movement *(approved, D106)* | The bounce, the chain |
| 6 | The hold-and-release strike (the draw) *(approved, D108)* | Charge feel, release |
| 7 | Parry, a health bar, and the red and yellow language, on a test dummy (a failed parry costs health, D082) *(built, D109; the designer found the parry awful, so it was rebuilt, D110, like Little Shinobi's with a glint telegraph; awaiting their hands again)* | The window, the feedback, "the big opening" |
| 8 | The blade's charge (D133): a meter filled by blows and more by parries, spent on heals and special attacks; shown by the sheathed hilt's aura, the glyphs lighting, sparks at a parry; the scar | The look of power, the reward for a parry |
| 9 | Tools (shuriken; limited use, refilled on rest) | Throw feel, use |
| 10 | The rope: zip, swing, latch | Reliability above all |
| 11 | Wall cling and double jump | Traversal feel |
| 12 | The first essence ability and its absorption beat | The cost and the thrill |

**Status (2026-10-17):** rungs 0–7 are built; the designer approved the run, the jump (D088), the dash and sprint (D099), the sword (D104), the pogo (D106) and the draw (D108) and is about to try the parry (D109). Rung 8 (the blade's charge) waits for their go. Notes
carried forward: the dark violet essence shows in the sword's swing (rung 4); the glow on the hilt stays; no smoke; the scarf is the headband's tail, a
chase-the-target chain tied to the head in `js/hero.js`. The head look is A, the purple band and short tail (D095).

## Phase 1 — The world on paper
Ends at **Gate 1: a world bible that passes the paper checks below, signed.**

| Step | Settles |
|---|---|
| 1.1 World shape | Regions, what each *is* (its identity, hue, sound, danger, people), how they join: a graph, no rooms yet |
| 1.2 Progression | Abilities and keys; the critical path; optional branches; the lock-and-key table (key placed far from its lock; the player sees a lock before finding its key) |
| 1.3 Region by region | Each room as an entry under the Purpose Rule; every door, shortcut, secret, wall and loop with its reason |
| 1.4 Teaching plan | Where each mechanic is introduced → isolated → combined → twisted, with no text |
| 1.5 Roster | Each enemy family and boss: where it lives, why, what it teaches; a figure table per boss |

World data is kept as **structured files** (`design/world/`), not only prose, so a script can check it. Decide the format at
the start of 1.1.

### The Purpose Rule
Every element has a reason written down; an element with no reason is cut. Per room:

```
### <id> <name> — <region>
Purpose:     teaches | tests | rewards | connects | tells | rests      (one primary; say what the game loses without it)
Arrive with: <abilities / keys / knowledge>        Leave with: <what changes>
Exits:       <exit> -> <room>: why these two join; what it costs; when it opens
Contains:    <enemy / pickup / secret / sign>: why it is here, what it teaches or gives
Tells:       how the player knows about each secret, shortcut or danger without being told
```

Rules for the paths themselves:
- **Door / transition**: leads to the room physically next to it. One-way only when the reason is visible (a drop, a collapse).
- **Barrier**: names what opens it (ability, key, event) and where that is. The player has seen the barrier before they can open it.
- **Shortcut**: joins two places the player has already seen; opens from the far side; saves something real (time, danger,
  both); is foreshadowed (seen but unreachable before); is checked to be shorter than the long way.
- **Secret**: has a visual tell and a reward in proportion to the effort. Starting density from Little Shinobi: about one per
  two or three rooms (reconsider).
- **Loop**: brings the player back somewhere known, changed by what they now carry.
- **Rest point**: placed by how long the player has been at risk. Starting value from Little Shinobi: within 10–20 seconds of a
  boss, or a shortcut that opens after the first attempt (reconsider).

### Paper checks (Gate 1)
1. **The graph solves.** From the start with nothing, repeatedly take what is reachable: every region, key and boss is
   reached; every lock's key is reachable *before* it is needed; no state exists where progress is impossible (no soft-lock).
2. **Nothing is unexplained.** No room without a purpose entry; no path, shortcut, barrier or secret without a reason.
3. **Everything is taught.** Every mechanic has an introduce / isolate / combine / twist location, and none is first
   demanded in a boss fight.
4. **Shortcuts earn their place.** Each joins two known places, opens from the far side, is foreshadowed, and saves real distance.
5. **Every boss has a figure table** (4–6 figures, one fixed rhythm each, in seconds) and a reason to exist in the story.

## Phase 2 — Vertical slice
Starts only when the designer says to start building the world (the feel ladder above is already allowed, D078). The smallest playable piece that proves the design: the hero's movement and core verb at final feel, a short chain of
rooms, one enemy family, one boss, one shortcut, with the test harness running. Ends at **Gate 2: the designer plays it
with the controller and says the feel is right.** The tech decision from 0.5 is acted on here; the earlier game's harness,
solver and room-building approach are ported deliberately, not copied wholesale (see `docs/REFERENCE.md`).

## Phase 3 — Build the world, region by region
For each region: design confirmed against the bible → built as independently playable pieces → evidence at four levels →
designer plays it → committed and tagged. If building shows the design is wrong, fix the design document first, in the log.

## Phase 4 — Polish, audio, balance, packaging
Then publish. (Pushing to GitHub happens whenever the designer wants; the repo is its own project from the first commit.)

## Evidence, once there is code
*Build* (suite green, nothing throws) · *behavior* (a test under repeatable inputs, one per mechanic, with measured numbers
noted) · *presentation* (a screenshot actually looked at) · *player experience* (the designer, the real controller). Say plainly
what only the designer's hands can verify. Mutation-test the important tests: break the game on purpose and confirm the
suite notices.
