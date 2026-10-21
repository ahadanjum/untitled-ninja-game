---
name: design-review
description: Review one brief (docs/briefs/*) before it is implemented — for completeness, internal consistency, testable acceptance criteria, respect for the frozen physics and the colour law, and conflicts against the order of authority. The Opus-reviews-before-Sonnet-builds pass (D120).
user_invocable: true
---

# Design review: check a brief before it is built

D120 splits the work: Opus plans and reviews, Sonnet implements an approved brief. A brief that goes to implementation under-specified
burns a whole session. This skill is the review pass that catches that first — it reads one brief against the repo's rules and the order of
authority and reports what must be fixed before handoff.

Borrowed in spirit from a studio-template `design-review`, rebuilt to this repo: no GDD/ADR schema, no registry — just a brief in
`docs/briefs/` checked against `design/DECISIONS.md`, `CLAUDE.md`, and the milestone's art direction. It reviews; it does not rewrite the
brief (bring a recommendation, not a menu — rule 3).

## When to use
- A **substantial new feature**, an **architecture change**, or an **ambiguous brief** is about to be authorized for implementation.
- The designer asks whether a brief is ready to hand to Sonnet, or a revised brief must close earlier notes.

## When to skip
- A minor adjustment already covered by an approved brief (a tuning pass, a colour or width tweak inside its stated bounds, a bug fix
  against its acceptance criteria). Implement it and run `/gate-check fast`.
- A brief already reviewed whose only change is wording. One formal review per brief revision, not per iteration.

## Read narrowly
Read the brief, the D-numbers it cites (grep `design/DECISIONS.md` for them, plus the conflicts C01–C22 it touches), the section of
`COMBAT_ANIMATION.md` it implements, and the files it names. Open the Bible or WORLD_MAP only when the brief touches narrative or
topology. Never reread the whole package for a small task.

## Input
The brief's path (`/design-review docs/briefs/IMPLEMENTATION_001_TASK_2B.md`). If none is given, take the most recent brief in
`docs/briefs/` and say which.

## What to check
1. **Completeness.** Does the brief state its scope, what it changes, what it must *not* change, and acceptance criteria? For a look-only
   polish task, does it name exactly which files may be touched and which are frozen?
2. **Testable acceptance.** Every acceptance criterion must be checkable — by a named test, a measured number, or a screenshot to be
   looked at. A criterion no test can decide is a gap; say how it could be made measurable.
3. **Respects what is frozen.** During the polish milestone the physics, hitboxes, attack startup, hit-stop, the input mappings and the 73
   traces are frozen (rule 10, D124). A brief that would move any of them without the designer's explicit word is a blocker.
4. **The colour law.** Nothing new on Kaname is red or yellow; the blade's energy is violet-black (rule 5, D037, D122). Flag any crimson/gold
   the package carries (C10, unresolved).
5. **No open choice made canonical.** The brief must not quietly decide something that is the designer's call (tone, identity, the core
   play, the colour of growth/souls). If it does, that is the biggest finding — surface it as a question for the designer (rules 3, 10).
6. **Order of authority.** Check the brief against `design/DECISIONS.md` first (the state of play), then the milestone's art direction
   (`docs/Kaname_Documentation/Docs/COMBAT_ANIMATION.md`), then the Bible. Where it conflicts, DECISIONS.md and the built-and-tested code
   win; note the conflict (and whether it is one of C01–C22).
7. **Implementability.** Could Sonnet build this from the brief alone without guessing on taste? List every place it would have to guess.

## Output
A verdict — **ready to implement** / **ready with notes** / **not ready** — followed by:
- **Blockers** (frozen files touched, a design call made canonical, an untestable acceptance criterion).
- **Notes** (wording, missing edge cases, a criterion that needs a measurable form).
- **Questions for the designer** (anything that is a taste call).
Keep it to findings and recommendations, a screen or less for a small brief; the brief's author (Opus) revises it, the designer
authorizes it, Sonnet builds it.
