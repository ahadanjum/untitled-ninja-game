# KANAME — Copy/Paste Claude Code Prompts and Sequence

**Available models:** Claude **Opus** (planning/complex review), Claude **Sonnet** (focused build). Do not select Fable.

## 0. Setup (once)
- Save/commit a working baseline of your existing Unity project.
- Copy package's `CLAUDE.md` to the **Unity root**, alongside `Assets/`, `Packages/`, `ProjectSettings/`.
- Copy entire `Docs/` directory to Unity root. Keep `Docs/references/` images and relative paths intact.
- Start Claude Code in that Unity project directory.

## 1. Opus: Inspect and plan animation changes (NO code)
Switch to your available **Opus** model and paste:

> Read `CLAUDE.md`, `Docs/COMBAT_ANIMATION.md`, `Docs/briefs/IMPLEMENTATION_001.md`, and look at `Docs/references/kaname_current_attacks.png`. My Unity prototype already implements jump, sprint, dash including air dash, forward/up/down sword swings and pogo. Inspect current project files, animations, hitboxes, player controller and Input System **without editing yet**. Explain how to preserve them while making the physical sword look like a beautiful katana (not scythe), aligning a sharp inner purple crescent with hitbox plus softer outer afterimage, strengthening forward/up anticipation poses and pogo hit response. List specific files to change, risks, small steps and actual tests. Do not invent assets/components you haven't found.

Review the plan yourself; ask Opus to reduce scope where necessary.

## 2. Sonnet: Implement the approved first brief
Switch to your available **Sonnet** model and paste:

> Implement ONLY the approved plan and `Docs/briefs/IMPLEMENTATION_001.md`. Preserve existing jump/dash/air dash/sprint, attack input, pogo behavior and project architecture. Avoid adding new Essence/binding/spell/rope systems now. After edits, list changed files and relevant tests actually run. Give me a short in-editor Unity playtest checklist for sword silhouette, forward/up/down startup, hitboxes, hit-stop and pogo chain, dash-to-sprint and air dash. State any tests you could not perform.

## 3. You: Unity playtest
- Test at typical actual game camera zoom, not only sprite view.
- Verify no missing sprites/Animator references or console errors.
- Test grounded and aerial attacks at several angles, collisions and attack cancel windows.
- Test pogo off single and sequential targets, dash transition, sprint and air dash.
- Compare crisp inner crescent against actual hitboxes.
- Record issues with screenshots and exact reproduction steps. Send **only those issues** to Sonnet for small fixes.

## 4. Opus: Plan the next small vertical slice (NO code)
After combat is stable:

> Read `CLAUDE.md`, `Docs/GAME_DESIGN_BIBLE.md`, `Docs/WORLD_MAP.md`, and `Docs/DECISIONS_AND_OPEN_ITEMS.md`. Inspect my actual Unity project. Plan ONE small playable Crossroads/Ashen Peaks vertical slice, reusing existing controllers. Include one enemy telegraph set, one shortcut/gated route, one Essence absorption interaction, and one large corruption node that opens a blocked path. Distinguish confirmed design from proposals; don't code yet. Produce a new numbered implementation brief with acceptance criteria, risks, test plan and affected files. Keep scope small.

## 5. Sonnet: Implement a single approved next brief

> Read `CLAUDE.md` and the newly approved numbered implementation brief. Implement its scope only. Preserve working combat/controller systems, avoid new unrelated mechanics, and provide changed files, checks run and specific Unity editor QA steps.

## 6. Repeat one feature at a time
Suggested future slices: precise L1 parry + enemy telegraphs; rope anchors/traversal; simple Essence spending/heal/spell and Crimson Fragments; boss duel; contextual Made binding with in-world `Hold △` prompt; story triggers; branch connections; chapter content. Opus reviews architecture/complex bugs; Sonnet implements focused work.
