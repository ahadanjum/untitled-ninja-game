# KANAME — Claude Code project instructions

## What this project is
A solo-developed Unity 2D action Metroidvania called **Kaname**. Combat is fast, precise, readable, and parry-focused; exploration is semi-open; storytelling is primarily environmental. Inspirations: Hollow Knight, Silksong, Nine Sols, Prince of Persia: The Lost Crown, Castlevania, Souls encounter design; the themes of engineered fate and demonic bargains have Bleach and Baldur's Gate 3 influences. Do not copy protected assets, characters, code, or level layouts.

## Approved model workflow
- **Claude Opus**: plan complex systems, inspect architecture, investigate difficult bugs, review major changes.
- **Claude Sonnet**: implement focused approved tasks, make Unity changes, run checks.
- **Do not suggest/use Claude Fable**; project owner does not have access.
- Never assume an exact model version is available: select the currently offered Opus or Sonnet in Claude Code (`/model`).

## Reference hierarchy
1. `Docs/briefs/IMPLEMENTATION_001.md` — CURRENT task scope and acceptance criteria; use when assigned this milestone.
2. `Docs/COMBAT_ANIMATION.md` — current animation/art direction.
3. `Docs/GAME_DESIGN_BIBLE.md` — long-term narrative/mechanics source of truth.
4. `Docs/WORLD_MAP.md` — exploratory topology, gates and visual region references.
5. `Docs/DECISIONS_AND_OPEN_ITEMS.md` — locked vs proposed design choices.
6. `Docs/references/` — image references; concept art is **not** a pixel-perfect implementation specification.

## First action in an existing Unity project
Inspect the project before making changes: Unity version, installed packages, input system, controller scripts, prefabs, sprites, Animator controllers, relevant scripts, and tests. Summarize findings and identify exactly which files will change. Do not assume file names, directories, components, or assets.

## Non-negotiable development rules
- Do not rewrite or break working jump, sprint, dash (including air dash), attacks, downward attack/pogo, or input mappings.
- Current decisions: **L1 parry, R2 tap dash (including airborne), R2 hold sprint, Square sword attack, Cross jump, Circle tap equipped blade spell / hold heal, Triangle HOLD contextual absorb/bind/cleanse.** For unimplemented inputs, document them; do not add mechanics outside the authorized milestone.
- Prototype visual changes separately from physical attack hitboxes; katana stays recognizable, with sharp inner damage crescent and softer outer violet VFX.
- The game uses **Violet temporary combat Essence**, **Crimson permanent growth**, and **Gold/Yellow liberated soul VFX only** (not spendable).
- Binding is a game-prompted contextual interaction, not a global attempt to cleanse any enemy.
- Keep work small, reversible, testable; avoid sweeping refactors or unsolicited new systems.
- Never call success merely because code compiles; report what was built, tests, and what needs in-editor playtesting.
- Do not silently make open design decisions canonical. Ask for approval if an unmade choice blocks implementation.

## Unity checks
After implementation: list changed files, ensure compile/import health, run existing EditMode/PlayMode tests when accessible, check Animator and input references where possible, and provide in-editor manual QA steps (especially hit-stop, pogo, attack telegraphs). Do not falsely claim to have tested the Unity editor if it was not accessible.
