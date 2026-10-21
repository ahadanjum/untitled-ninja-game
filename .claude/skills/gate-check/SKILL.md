---
name: gate-check
description: Before handing a rung or a polish task to the designer, or before calling anything done, run a readiness verdict — PASS / CONCERNS / FAIL — against the brief's acceptance criteria and the repo's four levels of evidence. Enforces "never call it done because it compiles."
user_invocable: true
---

# Gate check: is this ready for the designer?

A formal readiness verdict for one rung (U-number) or one polish task, so "is this ready to hand over?" stops being a judgment made fresh
each time. It checks the thing against its brief and against the repo's four levels of evidence, and it is honest about the one level only
the designer can give.

Borrowed in spirit from a studio-template `gate-check`, rebuilt to this repo: no production stages, no YAML config — just the brief, the
tests, the traces and the screenshots this project already keeps. The verdict is advisory; the sign-off is always the designer's (rule 1,
D081).

## When to use
- A task or rung is built and you are about to say it is ready for the designer's hands.
- Before a milestone commit.
- Any time you are tempted to call something done — run this first.

## Input
The rung or task (`/gate-check U4` or `/gate-check task 2b`). If none is given, infer from the latest `design/DECISIONS.md` entry and the
active brief in `docs/briefs/`, and say which you took.

## The four levels of evidence (the repo's own ladder)
Check each and give it a line:

1. **Build** — the suite is green and nothing throws. Run `node dev/run.mjs` (and, for Unity work, `unity test unity --mode EditMode`).
   Report the real counts; never infer green from "it compiles".
2. **Behavior** — there is a test that drives the new mechanic under repeatable inputs (one per new mechanic). Name it. For Unity work the
   73 golden traces must still replay to 0.001 u: run `node dev/trace.mjs --check` and the replay tests, and say so explicitly.
3. **Presentation** — a screenshot was actually looked at (`test-results/index.html`, or the Unity strike/pose sheets under
   `unity/Logs/`). Say what was seen, not that one exists.
4. **Player experience** — only the designer with the real controller can judge feel. State plainly what still needs their hands, and
   **never claim an Editor playtest that did not happen** (rule 10).

## Also check (the repo's guard rails)
- **Acceptance criteria.** Read the task's brief in `docs/briefs/` and tick each criterion off against what was built. Any unmet one is a
  blocker.
- **Nothing frozen was touched.** During the polish milestone, confirm `Scripts/Sim/`, the tuning asset, `traces/`, `js/` and `dev/` are
  untouched for a look-only task (`git status --porcelain` on those paths should be empty). A violation is an automatic FAIL.
- **The colour law.** Nothing new on Kaname is red or yellow; the blade's energy is violet (rule 5). If a test for this exists, it must be
  green.
- **Docs are true.** Any doc the change made stale is fixed in the same change (consider running `/sync-decision`).
- **No open design choice was made canonical.** If the build quietly decided something that is the designer's call, that is a CONCERN to
  surface, not to bury (rules 3, 10).

## Verdict
End with one of:
- **PASS** — every level has real evidence, the acceptance criteria are met, nothing frozen moved. Say what still needs the designer's
  controller (level 4 is theirs).
- **CONCERNS** — buildable and mostly there, but with named risks or gaps the designer should accept knowingly before advancing.
- **FAIL** — a blocker: a missing test, a red suite, a broken trace, a frozen file touched, or an unmet acceptance criterion.

List blockers and required artifacts explicitly. Report counts and test names as they really are; if a step was skipped, say it was
skipped.
