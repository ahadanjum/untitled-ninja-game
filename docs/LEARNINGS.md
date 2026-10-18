# What carries over from Little Shinobi

Little Shinobi (`../hollow-shinobi/`, "Drowned Neo-Edo") was built in four days of feedback loops, October 14–17, 2026: a
browser metroidvania with Hollow Knight movement and world, Sekiro / Nine Sols parry duels, three bosses, about 27 rooms and
~90 acceptance tests. This file is what that taught us, so this project starts ahead. Three parts: what the designer cares
about (their words), what went wrong and why, and what worked.

## 1. What the designer cares about

**Minimalism is the framework for everything.** "The gameplay should always speak for itself… Adopt that framework of
thinking into all methodology." The title is the name and how to start; nothing more. No "PARRY!", "BREAK!", "You died",
"Shortcut opened". Each moment gets its own look, sound or camera move. Text that remains is a short, listed exception.

**Bosses should feel like a dance** *(Little Shinobi's rule; the new game follows the Hollow Knight / Silksong / Souls boss
philosophy instead, D018, but the history is worth keeping)*. The references were Sekiro's Genichiro and Nine Sols. The designer wants *few* moves
(4 to 6), each with its own tempo (a figure like "1, 2, pause, 3", or "a two-hit combo that takes a second, hits, takes another
second, hits"), moves that work together, and consistency so "the player can learn the boss". Randomising speeds, or
repeating one fast combo at one speed in every phase, reads as spam. **No counting markers over a head** ("not intuitive or
fun at all"). **No drum or beat clock** in a fight that does not need one.

**Levels branch like Hollow Knight, not corridors.** "Think of the design of areas in Hollow Knight": branching from a hub,
doors you can enter, keys far from their locks, shortcuts opened from the far side, secrets, loops. Every door leads to what
is physically next to it.

**The parry is Sekiro-style: press any time, spamming has a cost** (not a lockout). A miss costs guard; a steady rhythm is
free. *(This was the old game's core verb; whether it is the new one is open.)*

**Taste history, to avoid repeating:** the designer rejected a redirect parry, Nioh-style dual stances, a full stealth AI, and
extra charge meters; chose "riding the sea" (teal things you bounce off, as in Nine Sols), a pistol charged by parries, and
standoffs with grunts. They found a plain feudal-ninja setting overused but kept the ninja
hero and the combat flow. Tone requested: "crisp, cute and cosy like Hollow Knight", with a little horror underneath.

**Platform:** Chrome, PlayStation controller, a MacBook (M2). Smooth 60 fps. Everything in the old game was generated in
code, so it ran by double-clicking `index.html`.

## 2. What went wrong, and the lesson

| What happened | Cost | Lesson |
|---|---|---|
| The setting was changed mid-build (feudal ninja → Drowned Neo-Edo, Oct 14) | Everything drawn had to be redone; the old version was kept as a backup | Decide the world's identity before any art. |
| The warship boss went through four versions in three days (a walking puppet, a drummer in a dome, a titan with a drumming monk, then a swordsman), reshaped twice more after the designer played it | Three rejected builds, then two reworks | Ask for references and design on paper first; build a boss only after its figure table is agreed. |
| Levels began as a linear run, then were redone as branching (the coast) | Redesign of the map | Draw the map first: hub, branches, locks and keys, shortcut, secret. |
| Telegraph pips over an enemy's head and a drum / beat clock were added by Claude and rejected | Rework, and trust | Check any telegraph or rhythm device against the rules before building it. |
| Claude's own notes said a boss "never repeats a speed" and has "13 figures at five speeds"; the designer read that as randomising and over-complex | Confusion in review | Write intent in the designer's words ("few figures, each fixed, learned by the player"). |
| Design by building: each world change needed a save-version bump, a world-signature update, solver entries, door alignment | A long tail of bookkeeping | Settle the graph on paper; build only what is already agreed. |

## 3. What worked (keep)

- **A visible, testable build loop mattered more than the engine.** `dev/run.mjs` is a zero-dependency Node runner that drives
  headless Chrome over the DevTools protocol, serves the folder itself, gives every test a fresh page with a seeded
  `Math.random`, audio off, rendering held — deterministic and order-independent — and writes a contact sheet of screenshots
  (`test-results/index.html`). Roughly 90 tests; mutation-tested (34 deliberate breakages, all caught).
- **Four levels of evidence**, with honest limits: a passing bot proves a decent player *can* win, not that it *feels fair*;
  sound was measured, never heard; the controller mapping was tested with a fake pad.
- **A platforming solver on the real physics** (`dev/reach.js`) found a room that could not be climbed, which nobody had noticed.
  Paper graphs say a door connects; physics says whether a body can reach it.
- **One implementer, one reviewer.** An independent reviewer found a real bug the implementer's tests had missed. Parallel
  agents with explicit file ownership, and big world changes in a git worktree merged afterwards, worked; two writers on one
  file did not.
- **The numbers are the design**, written in `CLAUDE.md` and enforced by tests, so retuning is deliberate. A save-version
  (`BUILD`) bump whenever the world changes, so old saves are ignored rather than loading into a broken world.
- **Developer URL flags** for every piece of progress (`?room=…&dash&boss`), so any room can be tested with any abilities.
- **Measure before polishing**: a bot that reacts only to the flash should lose about 8 hits and win in about 100 s; keep fights
  in that range. Hitboxes follow the *drawn* blade: draw and hit from the same pose numbers.
- **Smooth by construction**: fixed 60 Hz simulation; poses eased, never switched; drawing pure (a test checks it has no
  effect on the simulation); the camera a critically damped spring; sub-pixel placement.

## 4. Pitfalls worth knowing

- Enemies within about four tiles of a spawn point hurt the player during the spawn wait, and poison a solver's start state.
- A door trigger taller than its opening lets the player's head fire it through a ceiling.
- Plain `<script>` tags share one global scope: two files defined a function with the same name. Modules would prevent this.
- A hit-stop on parry consumes inputs about seven frames late: tune input buffers with it in mind.
- Boss rooms must have no ledge the boss cannot reach, or it becomes a perch.
- "Unreached" from a solver means *no way found*, not *no way exists*; and a frame-exact script says nothing about how hard a
  jump is for a hand.
- Docs drift: keep them true in the same change that makes them false.
