# Untitled Ninja Game (working title)

A 2D fantasy ninja game designed from scratch: new hero, new story, new world. It inherits the *process* of Little Shinobi
(`../hollow-shinobi/`): its rules, its tests, its way of working. None of its content. The designer plays in **Chrome with a
PlayStation controller** on a Mac.

## Where we are
**Phase 0, foundations, with the feel ladder under way (D078).** The design is mostly locked (`design/DECISIONS.md` is the state of
play: **read it first, every session**). The only code that exists is the character test environment: a blank white stage and
Kaname, built one tested rung at a time. **Rungs 0–7 are built** (the stage, Kaname in the Little Shinobi style, run, jump, dash and sprint, the sword, the pogo, the charge and draw, health and the parry). The
designer approved the run, the jump (D088), the dash and sprint (D099), the sword (D104), the pogo (D106) and the draw (D108); **health, the parry and the red and yellow language (D109) were tried and rebuilt after their verdict (D110: Little Shinobi's parry, a glint instead of circles, a plain parry only a deflect); the rebuild is built and tested but not yet tried by their hands, and these are still proposals: the core play is the designer's call (D081).** Rung 8 (the blade's charge, the seals and the scar) waits for their go. Nothing beyond the
ladder is coded: no rooms, no world, no real enemies. Then `design/feel.md`, `hero.md`, `story.md`, `world-sketch.md`,
`core-play-ideas.md` (options; the designer decides), and `docs/PROCESS.md` for the phases and the ladder.

**Unity (D112): the real game is being built in Unity 6.3 LTS (free Personal tier) in `unity/`; the plan and the state are in `docs/UNITY-PORT.md`. This JavaScript game is the executable spec until the port matches it: `dev/trace.mjs` records its behaviour into `traces/`, and the C# must replay it to 0.001 u (`unity test unity --mode EditMode`).** Rungs U0 (the body, D116) and U1 (Kaname's look, moved first at the designer's word, D118) are built; the mechanics U2–U6 follow, each replayed against `traces/`. In Unity press **P** to cycle his poses. The outfit is black and the band deep violet (D117). Do not download or install anything for it without the designer's yes to the specific files; Unity's terms, sign-in and licence are theirs to accept. The repository is public on GitHub (`ahadanjum/untitled-ninja-game`); plain `git push` fails here with HTTP 400, use `git -c http.postBuffer=524288000 -c http.version=HTTP/1.1 push`.

## Rules for this repo
1. **Code only on the feel ladder (D078).** Ask before starting each rung and wait for the designer's sign-off on the last. No
   rooms, no world, no enemies beyond a test dummy, no design-checker scripts, until the designer says so.
2. **The Purpose Rule.** Every room, path, door, shortcut, wall, secret, pickup and enemy placement has a written reason (what it
   teaches / tests / rewards / connects / tells / rests, and what it joins and costs). No reason, no element.
3. **Ask, don't guess, on taste.** When a choice is about what the game *is* (tone, a character, a boss's identity, what is
   rewarded) ask, starting from the designer's references. When it is about how to build it well, decide. Bring a
   recommendation, not a menu; log every decision with its *why*. Mark Claude's ideas as proposals until the designer says yes.
   The core play (0.4) is the designer's call (D081).
4. **Show, don't tell, but people talk.** No tutorials that pause, no event pop-ups. NPC dialogue and boss exchanges (portrait and
   text, press X) are wanted (D017).
5. **Telegraph by colour, shape and sound.** Red = dodge or jump; yellow = parry (D010). Never draw hitboxes or ranges; no markers
   over an enemy's head that count something. **Nothing else uses red or yellow, and nothing on Kaname is red or yellow** (a test
   checks); the blade's glow is violet-black (D037).
6. **Bosses follow Hollow Knight, Silksong and the Souls games** (D018); Castlevania/Hollow Knight-style play, a parry is a big
   opening (D083). Notes: `design/references/boss-design-philosophy.md`.
7. **The world is physically honest and branches** (D008, D011). A door leads to the room physically next to it; nothing
   teleports; keys sit far from their locks; shortcuts open from the far side; secrets have a visual tell.
8. **Words in docs matter.** Describe intent in the designer's terms. They dictate by voice, so terms can arrive garbled: state
   your reading and log it (`design/feel.md` has a glossary).
9. **The story is fair.** Foreshadowing is planned in a clue ledger (`design/story.md`).

## Run, test, and look
```
python3 serve.py                      # http://127.0.0.1:8770/   (the preview launch config "ninja-game", in ../.claude/launch.json)
node dev/run.mjs                      # acceptance tests, about 10 s: run before and after every change
node dev/run.mjs --grep jump          # a subset;  --list shows everything;  --no-evidence skips screenshots;  --seed N;  --headed
open test-results/index.html          # evidence: the screenshots and the measured numbers of every test
node dev/trace.mjs [--check]          # record (or check) the golden traces the Unity port is proved against, into traces/
unity test unity --mode EditMode --output /tmp/r.xml    # the Unity tests (Editor closed; needs the Unity CLI)
```
Needs Node 22+ and Google Chrome (`CHROME_PATH` to override). Zero dependencies: `dev/run.mjs` drives headless Chrome over the DevTools
protocol, serves the folder itself, gives every test a fresh page with a seeded `Math.random` and no real gamepad, so results are
deterministic and order-independent. You can also just double-click `index.html`.
**Keys:** arrows or WASD move, Space or Z jump, **Shift, X or K dash (tap) and sprint (hold)**, **J or C sword (hold ↑ for an up strike, ↓ in the air for a down strike; keep holding for the draw), L or V parry** · G height grid · N night · H hide hints · [ ] zoom · R reset · ` numbers.
**Pad (PlayStation):** left stick (analog: a light touch walks) or d-pad moves, ✕ jumps, **○, R1 or R2 dash (tap) and sprint (hold), □ the sword (stick up or down as with the keys; hold it for the draw), L1 or L2 the parry**. **URL flags:** `?night ?zoom=3 ?grid=0 ?hints=0
?debug ?interp=1|0 ?manual` (`?manual` loads `dev/harness.js`: deterministic stepping with `T.run/secs/tap/hold/place/pad`).

## Files
`index.html` (classic `<script>` tags, no build step) · `js/util.js` constants, math, input (keyboard + gamepad, ported from Little
Shinobi) · `js/stage.js` the test stage (ground, four pillars of 1–4 tiles, a ledge) · `js/hero.js` Kaname's physics, eased pose,
scarf · `js/art_hero.js` how he is drawn (one function) · `js/dummy.js` the test dummies for the sword and the pogo (two posts and a stair of three hanging ones) · `js/striker.js` the test attacker for the parry · `js/game.js` the loop, camera, stage and HUD drawing · `dev/harness.js`
test driver · `dev/tests.js` the acceptance tests · `dev/run.mjs` the runner (ported from Little Shinobi) · `design/concepts/` the
drawn looks (the chosen one is **C**; round 1 is archived).

## The numbers are the design (as the code has them; `dev/tests.js` checks these)
World units (u) are Little Shinobi's pixels: one tile is 32 u, Kaname's body is 16 × 34 u. **Run 240 u/s**; ground accel 3000 (0 to top
speed in 0.08 s), decel 3600 (about 6 u of slide), turn 5200; air accel 2200, air decel 1500. **Gravity 2200; jump 740 u/s: a held jump
rises about 118 u (3.7 tiles: it clears 3 tiles, not 4), a tap about 47 u** (jump cut ×0.42 on release); **coyote 0.10 s; jump buffer
0.12 s**; fall speed capped at 900 u/s. The stick has a dead zone of 0.18 and is at full speed by 0.95. The camera is a critically damped
spring (0.16 s) leading him by 36 u. The headband's tail is 7 links of 2.4 u, 17 u in all (D093, D094).
**Dash (tap): 640 u/s for 0.2 s = 128 u, flat; cooldown 0.45 s; a press 0.1 s early is kept; in the air one per jump, refilled on landing; ends at 210 u/s; a wall or a jump ends it. Sprint (hold dash + a direction): top speed 336 u/s (1.4 × run) on the ground and in the air (D096).**
**Sword (D100, all numbers proposals): the blade is live 0.03–0.14 s after the press, the swing ends at 0.2 s, cooldown 0.3 s, a press 0.12 s early is kept. Forward reach 46 u past his body; up is 60 × 52 u above him; down is 52 × 48 u below him (in the air only). No turning, dashing or second swing while the blade is live; a dash or a jump may cut the recovery. A landed blow stops the world for 4 steps (0.066 s). One blow per swing per target.**
**Health and the parry (D109, revised by D110; proposals): a bar of 5; a plain or yellow hit takes 1, a red 2; being hit throws him back, stuns him 0.22 s and leaves him untouchable 1 s; at 0 he is down 1.4 s then up with full health. The parry is Little Shinobi's: he can press at any moment (no lockout); the first press opens a 0.26 s window, each further press within 0.5 s of the last a shorter one (0.16, 0.09, 0.05 s); a pause or a clean parry resets it. The guard covers both sides, he turns to the attacker, a press made in a hit-stop is kept, and a parry cancels his own swing (not a lunge); with the guard up he moves at 30 %. A failed parry (early, late, or on red) means the hit lands. The attacker's blade arrives 0.04 s into its cut. Plain: any answer; a parry only turns it aside for 0.22 s (no big opening, no zoom). Yellow (a yellow glint): a parry leaves it wide open 1.6 s, with the big-hit zoom, and a blow then lands hard (0.14 s pause). Red (a red glint, a low sweep, cannot be parried): jump it or dash through it. A dash is untouchable throughout; a clean parry leaves him untouchable 0.35 s and refills the air dash. The striker (far left of the start) winds up 0.42 / 0.62 / 0.70 s for plain / yellow / red and attacks in the order plain, yellow, red, yellow, plain, red when he is within 150 u.**
**Charge and draw (D107): keep holding the strike button after the press and a charge builds from 0.25 s, ready at 0.62 s; let go and he lunges 820 u/s for 0.16 s (about 130 u), flat, a heavy blow (hit-stop 0.1 s) to everything it passes, once each. Early release does nothing; charging walks at 40 % speed; a dash breaks the charge, a jump does not; it works in the air; the lunge is committed and ends crisply.**
**Pogo and reset (D105): a down strike that lands bounces him up at 560 u/s (about 71 u, from where it hit, after the hit-stop); any blow landed in the air refills the air dash; once a blow has landed he may dash or turn at once (it cancels the rest of the swing), but not while the blade is still looking for its mark.** (The first game's jump of 720 measured 112 u in this integrator, so it was raised to 740.)
Retune on purpose only, and change the test in the same change.

## How Kaname is drawn (the designer's notes)
Little Shinobi's ink silhouette: a round dark hood (black, D117) with a **deep-violet headband (D091, darkened in D117) wrapped round the head just above the eyes, and the scarf is that
band's tail** (exactly as in Little Shinobi: one piece of cloth), glowing white eyes, a thin pale-blue rim, a purple sash, a plain katana at the
hip (**no white tags or wrapping**), **no arms** (D103: as in Little Shinobi, the sword simply appears in his hand), a violet glow on the sword's hilt standing in for the curse. **No tail on the hood and no
smoke** (the designer disliked both). The dark violet essence is to show in the sword's swing (rung 4 onward).
**The look is A (D091, D095): the band and tail are purple; the band-only and knight looks are retired.**
**The sword (D100–D103), drawn as Little Shinobi draws its cut:** there are no arms; the hand's place is a point at his side that moves to each swing's pivot (`SWINGS` in `js/art_hero.js`), and a curved katana turns about it (`drawBlade`: a wrapped grip, a guard, a violet-black blade with a spine, a band of pale violet fire along the cutting edge, wisps of violet fire streaming back off it, and a soft aura along the whole blade, strongest at the tip). The trail (`drawCrescent`) is the sweep of the blade's outer part: a smooth crescent hair-thin where the blade began and widest at its tip, whose outer edge is the tip's path, so it hangs off the blade and never runs ahead of it; it thins and fades as the cut ends. Forward cuts alternate direction; the up cut passes over his head from behind to the front; the down cut passes under him. He leans into the cut. The dummy (`js/dummy.js`) is a dark post with a crossbar, a round straw head and rope bands in his ink style; it flashes white-blue when struck, flinches away from the blow and springs back.
**The red and yellow language and the parry (D109, D110):** the striker is Kaname's silhouette recoloured and bare-headed, with a wooden blade. **The telegraph is one glint, as in Ghost of Tsushima: a four-point sparkle at the tip of its blade (`drawGlint` in `js/striker.js`), yellow for a parry or red for jump or dash, flashing from 0.32 s before the cut (0.24 s long); plain attacks have none. Its blade and eyes are never lit, and there are no circles, rings or markers anywhere** (a test draws the parry flash and a glint and counts arcs: none). The parry's flash at the clash is a hard four-point glint and a burst of sparks. Kaname's guard is the blade held up in front with a small pale glint at its tip while the window is open. A parried striker is turned aside (the blade knocked up) or, if it was yellow, crouched and bowed toward him; it never leans away. Untouchable after a hit, he blinks. The health bar is five pale segments at the top of the screen.
**The charge and the draw (D107):** charging, the hilt's glow swells (`v.charge`), six violet motes spiral into it, he crouches 1.6 u and leans back 0.1 rad, and at full charge a ring pulses out from the hilt; the draw reuses the dash's long, low stretch and speed lines and the sword's blade and crescent with its own, flatter arc (`SWINGS.draw`) and a wider trail.
**Dash and sprint (D096):** the sprint leans him in (0.33 rad), lengthens and quickens the stride, lowers the head and blows the tail straight back; the dash stretches him long and low (22 % wider, 8 % shorter), leans him 0.5 rad, trails the legs and arm back, and four violet speed lines stream behind him (D098: tapered wedges from head, body and feet, up to 90 u long in a dash, about 40 u and fainter in a sprint, `drawTrail` in `js/art_hero.js`, drawn from `Kaname.trail`: his position each step for the last 0.24 s of a dash or 0.12 s of a sprint). All of it reads `vis.dash` and `vis.sprint` (eased) and `trail`; `kanameHeadFrame` and `kanameCrouch` keep the head, the band and the scarf's root in agreement through every pose, and `kanamePose` holds every limb position.
The curse's look on the forearm is secondary (D092): dialogue and story carry "cursed"; the priority is the overall look.
**The scarf must never come loose from his head** (the designer's complaint about the first version). Its root is the back rim of the
head at the band's height, computed by one function, `kanameHeadFrame` in `js/hero.js`, used by both the simulation and the drawing, and a test
checks it in every pose (standing, running, jumping, landing, turning, and between steps). The chain is 8 points that chase a resting shape
(back and down, flat at speed, lifting in a fall; the orientation is eased) and are held to a fixed length; it never dips below the floor.

## Motion
The simulation is a fixed 60 Hz step; only drawing varies. Poses are eased (`vis` in `hero.js`), never switched; nothing in the
simulation reads `vis` or the scarf, and drawing is pure (a test checks that rendering changes no state). On a display faster than ~75 Hz
the last two steps are blended (measured; `?interp=1|0` forces it), on a 60 Hz display the latest step is drawn directly.

## How to work (carried over)
- **Checkpoint.** Work from a clean git tree, commit milestones when asked, tag before anything risky.
- **One rung, one change.** Each independently playable. Evidence in four levels: *build* (suite green, nothing throws), *behavior* (a
  test drives it under repeatable inputs, one per new mechanic), *presentation* (a screenshot looked at: `await snap('label')`),
  *player experience* (only the designer with the real controller; say plainly what still needs their hands).
- **One implementer, one reviewer** for anything non-trivial; parallel agents get explicit file ownership.
- **Report honestly,** and **keep docs true** in the same change that makes them false.

## Known gaps (not fixed, on purpose)
- **Feel is the designer's to judge.** The tests prove the numbers and the rules; they cannot say whether it feels good.
- **Sword feel is the designer's to judge** (reach, speed, the pause when a blow lands, the arc). **Invulnerability while dashing is not built**: nothing can hit him yet; nor is any damage number: the dummy only flinches. The pogo's bounce height and the cooldown (which sets the chain's rhythm) are proposals too.
- **Smoothness on a fast display is untested:** headless Chrome runs at 60 Hz, so the blending has never been seen on a 120 Hz screen.
- **The controller is tested with a fake pad.** The DualSense mapping comes from Little Shinobi's notes (standard layout in Chrome).
- No sound yet.

## Not decided yet: do not assume
How the world's art is made beyond the hero (the Belmont's Curse look was set aside for now) · a music piece per area and boss · the
game's name (the hero is Kaname) · the world's regions and layout · the core play beyond movement (tools, rope, parry, rest, death).

## Reference projects (read-only)
`../hollow-shinobi/`, `../hollow-shinobi-ninja-backup/`, `../console-player/` are separate. Read them freely; never edit them from here.
Index: `docs/REFERENCE.md`.
