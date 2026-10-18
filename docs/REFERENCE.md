# Reference index: what the earlier projects hold

All paths are relative to this repo and **read-only**: never edit the old projects from here. Everything in them was
generated in code by us, so it is ours to reuse, but the default is to *redesign, not port*. Copy something across only
deliberately, in Phase 2 or later, and note its origin in the commit.

## Process and design documents (use now)
| File | What it is | Use it for |
|---|---|---|
| `../hollow-shinobi/CLAUDE.md` | The working rules, commands, "numbers are the design", the checklist of what to update when adding things | The template for our own `CLAUDE.md` once there is code |
| `../hollow-shinobi/GAME-BUILDER-PROMPT.md` | The old master design brief: the game in one breath, five pillars, technical rules, game-feel numbers, minimalism in detail, art and sound direction, roadmap, rejected list, how to work, quality bar | The *shape* of our design bible, which Phases 0–1 will fill in |
| `../hollow-shinobi/BEACH-DESIGN.md` | A map-first region design: hub, locks and keys, shortcut, secret, with the reasons | A worked example of the Purpose Rule in prose |
| `../hollow-shinobi/design-research/00-synthesis.md` … `08-combinations.md` | Nine reports: Hollow Knight and Silksong, the Metroid / Castlevania lineage, modern metroidvanias, souls-vanias and 2D ninja action, Sekiro / Nine Sols / Ghost, parry games, a menu of combinations | The genre is the same; start here before commissioning new research |
| `../hollow-shinobi/Claude-Code-Game-Development-Report.pdf` | A field guide to setting up Claude Code for Unity, Godot, Unreal and Phaser | The tech decision in step 0.5 |
| `../hollow-shinobi/README.md` | The old game's player-facing description, enemy and boss tables, "what was verified" | What a finished write-up of this kind looks like |

## Tooling (port in Phase 2)
| File | What it is |
|---|---|
| `../hollow-shinobi/dev/run.mjs` | Zero-dependency test runner: headless Chrome over the DevTools protocol, own static server, seeded RNG, fresh page per test, screenshot contact sheet |
| `../hollow-shinobi/dev/harness.js` | In-page driver for deterministic stepping (`T.run`, `T.tap`, `T.walkTo`, bots) |
| `../hollow-shinobi/dev/tests.js` | The acceptance suite: a model for how mechanics, rooms, saves and presentation are tested |
| `../hollow-shinobi/dev/reach.js`, `dev/reachability-report.md` | A solver that drives the real player physics to prove every exit, key and shrine reachable, and what it found |

## Code and craft (inspiration; redesign by default)
| File | Worth looking at for |
|---|---|
| `js/entities.js` | `Player` physics and the movement numbers (run 240 px/s, jump 720 px/s, coyote 0.1 s, buffer 0.12 s, dash 640 px/s) as a *starting point* for feel, not a binding |
| `js/art_chars.js`, `js/art_bg.js`, `js/art_beach.js` | Characters and tiles drawn as canvas paths, one `draw*` function each; pose and animation ideas |
| `js/audio.js` | WebAudio synthesis: effects, music, a formant "voice" synth |
| `js/enemies.js`, `js/enemies_coast.js` | Enemy and boss state machines, including the dance-figure boss (`Duelist.buildAttack`, `DU_TEMPO`) |
| `js/rooms.js`, `js/beach.js` | The room-building helpers (`fill/prop/ent/exit/spawn`) and tile legend |
| `js/game.js` | State machine, camera spring, interpolation, HUD, save keyed by `BUILD` |

(All under `../hollow-shinobi/`.)

## Older versions and unrelated work
- `../hollow-shinobi-ninja-backup/` — the first, feudal-ninja version of the game, before the Drowned Neo-Edo retheme. Look
  here for the original hero's look and pose ideas.
- `../console-player/` — a browser emulation experiment with the designer's own console games. Unrelated to this project.
