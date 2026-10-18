# IMPLEMENTATION 001 — Katana & Current Combat Animation Polish

**Status:** Ready for project inspection and planning, NOT pre-authorized major refactor.
**Owner:** Solo Unity developer.
**Model assignment:** Opus inspects/plans; developer approves; Sonnet implements; Opus reviews only if complex regressions arise.

## Outcome
Make the currently working attack sprites/animations and sword VFX more readable and satisfying while preserving all existing movement and input behavior. This is a **vertical-slice polish task**, not a full combat-system build.

## Required context
- `../../CLAUDE.md` (project root; relative from this brief is `../../CLAUDE.md`)
- `../COMBAT_ANIMATION.md`
- `../references/kaname_current_attacks.png`
- `../references/kaname_combat_concept_board.png` (exploratory inspiration)

## Confirmed existing functionality (verify in project)
- Jump; ground dash; **air dash**; sprint.
- Forward sword strike, upward strike, downward strike, pogo rebound.
- Prototype has blade purple VFX and hit-stop on contact. Actual runtime integration must be inspected, not assumed from screenshot.

## Planning phase (Opus — no code yet)
1. Identify scene/prefab, input system and map, player controller scripts, Attack/Animator clips and transitions, sprite sheets/VFX, hitboxes and physics.
2. Reproduce current visual issues if Unity execution is available; else annotate screenshot-based observations and state limits.
3. Propose exact files to change and an implementation sequence; distinguish art changes from collision/damage changes.
4. State assumptions, tests, regression risk and manual Unity checks. No unrequested redesign.

## Implementation phase (Sonnet — only after approval)
A. **Katana:** preserve readable physical sword with controlled violet edge; remove scythe-like hook from oversized slash VFX; align brightest inner crescent with hitbox; soften outer afterimage.
B. **Character:** increase cursed right-arm readability without changing Kaname's identity or requiring an unnecessarily complex rig.
C. **Forward/up/down attack:** differentiate start anticipation and follow-through poses; preserve responsiveness and existing damage behavior.
D. **Pogo:** preserve collision target logic and bounce; improve impact cue, hit-stop balance, and upward recovery pose. Validate chainable downward attacks against target dummies.
E. **Movement:** maintain original jump, ground/air dash and hold-to-sprint behavior; only tweak animation appearance with permission, not core movement physics.

## Out of scope
- Essence economy; absorption/binding; new attacks/spells; rope; double jump; new enemies/bosses; level design; lore cutscenes; wholesale controller replacement; new art style; project-wide refactor.

## Acceptance criteria (measurable by test)
- Unity imports/compiles with no **new** errors; changed animation references remain valid.
- Existing ground and air movement unaffected; R2 dash/sprint behavior preserved where implemented.
- Forward/up/down strikes visually distinct and damage continues to occur at expected frames.
- Physical blade remains identifiable; active VFX visually correspond to hitboxes; cosmetic outer bloom does not mislead range.
- Valid pogo target consistently produces rebound without double-trigger or unexpected input lock; test multiple targets in sequence.
- Deliver changed-file summary, current limitations, and exact manual checks for owner in Unity.

## Manual smoke-test checklist
- [ ] Launch existing test scene; verify player spawns and Animator works.
- [ ] Jump, run, sprint, ground dash, **air dash**; confirm intended dash-to-sprint transition.
- [ ] Attack forward and upward on targets at several offsets; compare VFX range to actual contact.
- [ ] Down strike and pogo on fixed dummy; repeat, then pogo onto a second target.
- [ ] Verify hit-stop applies on confirmed hit but does not freeze input unexpectedly.
- [ ] Test attack at grounded/airborne boundaries, during dash recovery, and near walls/platform edges.
- [ ] Inspect game at actual camera zoom—not only close-up sprite editor.
- [ ] Confirm no unexpected console errors or missing assets.

## Completion report expected from Sonnet
`Files changed`, `Before/after behavior`, `Automated checks actually run`, `What needs Unity playtesting`, `Remaining issues / risks`. Never claim in-editor playtesting if unable to run the project.
