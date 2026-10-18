# KANAME — documentation package: start here

This folder contains the narrative/gameplay reference material and practical briefs for the existing Unity prototype. It is a **reference package**, not a request to generate the whole game in one autonomous run.

## What's inside

| File | Use |
|---|---|
| `CLAUDE.md` | Short persistent instructions Claude Code reads from the project root |
| `Docs/GAME_DESIGN_BIBLE.md` | Complete six-chapter narrative, lore, factions, mechanics, abilities, endings, scope and boss design |
| `Docs/COMBAT_ANIMATION.md` | **Immediate** character, katana, attack animation and VFX polish prompt/spec |
| `Docs/briefs/IMPLEMENTATION_001.md` | **Immediate** bounded coding task and validation checklist |
| `Docs/WORLD_MAP.md` | Region topology, chapter reuse, ability-gated paths and embedded images |
| `Docs/DECISIONS_AND_OPEN_ITEMS.md` | Confirmed decisions, provisional ideas, and unresolved design parameters |
| `Docs/references/` | Current user combat frames, generated concept board, region crops and world map PNG/SVG |

## Recommended order: do this, not everything at once

1. **Back up / commit your current Unity project first.** It already has functional jumping, sprinting, dash/air dash, sword attacks, upward strike, downward strike and pogo.
2. Copy `CLAUDE.md` to the **Unity project root**; copy `Docs/` into that project. Put image references with their relative paths intact. Do not replace project code, artwork or scene assets with these design reference files.
3. **Opus — inspection/planning pass:** say: `Read CLAUDE.md, Docs/COMBAT_ANIMATION.md, and Docs/briefs/IMPLEMENTATION_001.md. Inspect my current Unity project and actual animation assets. Produce a minimal change plan, with affected files and risks; do not code yet.`
4. Review the plan yourself. **Sonnet — implementation pass:** say: `Implement only the approved first animation/combat-polish milestone in Docs/briefs/IMPLEMENTATION_001.md. Preserve my working movement and inputs. Show changed files, tests, and exact Unity playtest steps.`
5. **Playtest yourself in Unity**: sword silhouette, hitbox clarity, attack anticipation, air dash, dash-to-sprint, pogo bounce, animation recovery and hit-stop. File bugs or iterate with Sonnet.
6. **Opus — review as needed**, especially if fixing animations requires architectural changes or breaks working behavior. Otherwise keep using Sonnet for focused fixes.
7. Once that slice feels good, use **Opus** with `Docs/GAME_DESIGN_BIBLE.md` and `Docs/WORLD_MAP.md` to plan the next implementation brief (Essence absorption + one corruption node + one room). Do not ask it to implement all six chapters.
8. **Sonnet** implements that next brief; you test it in Unity. Repeat with one bounded milestone at a time.

## Visual limitations / attribution
- `kaname_current_attacks.png` is the user's provided eight-frame screenshot.
- `kaname_combat_concept_board.png` is generated concept/reference imagery and **is not a current Unity screenshot** or final canon.
- `ashen_peaks.png`, `sealed_archives.png`, `hollow_market.png` are **crops of the generated concept board**.
- The Hollow Market crop is **a placeholder**: the exact first market image the user preferred in the earlier conversation is not available as a verified local asset. Replace `Docs/references/hollow_market.png` with the preferred reference when it becomes available. Until then treat it only as a mood/color reference.
- `rough_world_map.png` and `.svg` are a *schematic* created for this package, not a finalized level map. Red dashed mini-routes denote locked **within-region** routes; white routes are primary and turquoise dashed routes are later shortcuts.

## Design authority
**Approved** = user-confirmed direction. **Proposed / Playtest** = a suggestion, not an instruction to code without approval. See `Docs/DECISIONS_AND_OPEN_ITEMS.md` for distinctions.
