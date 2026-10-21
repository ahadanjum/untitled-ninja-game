---
name: gate-check
description: Readiness verdict — PASS / CONCERNS / FAIL — for a rung or a polish task before it goes to the designer. FAST for low-risk iteration (build, affected tests, diff scope, one render); FULL at milestone completion (whole suites, the 73 traces, frozen boundaries, docs). Enforces "never call it done because it compiles."
user_invocable: true
---

# Gate check: is this ready for the designer?

A readiness verdict for one rung (U-number) or one polish task, checked against its brief and the repo's four levels of evidence, honest
about the one level only the designer can give. Advisory; the sign-off is always the designer's (rule 1, D081).

## Input
`/gate-check fast task 3` or `/gate-check full U4`. No mode given: **FAST** for an iteration inside an authorized task, **FULL** when the
task or milestone is about to be called complete. No task given: infer it from the active brief in `docs/briefs/` and say which.

## Step 0, both modes: the frozen boundary (a blocker, never a FAST shortcut)
```
git status --porcelain -- unity/Assets/Scripts/Sim traces js dev
git diff --stat <base>..HEAD -- unity/Assets/Scripts/Sim traces js dev
```
plus the tuning asset if the brief names it. During the polish milestone these must be empty for a look-only task (rule 10, D124). If
anything there moved that the brief does not authorize: **stop, FAIL, switch to FULL**, and tell the designer. Never wave it through.

## FAST (low-risk iteration)
1. **Build.** The project compiles (the Unity test run does it; `node dev/run.mjs --grep <area>` for JS).
2. **Affected tests only.** `unity test unity --mode EditMode --affected` or `--filter <Class>` for the classes the change reaches (e.g.
   `SlashTests`, `SwordTests`, `LookTests`); report the real counts. Not the full suite, not the traces, unless step 0 tripped.
3. **Diff scope.** `git diff --stat`: only the files the brief allows; no drive-by edits, no tolerance or threshold loosened.
4. **One render looked at.** The relevant sheet (Kaname > Render the strike sheet / pose sheet, or `test-results/index.html`). Say what
   was seen, not that it exists. Reuse an existing valid render of the unchanged baseline; do not re-render it.
5. **What needs the designer's Unity playtest**, in one or two lines.

Verdict line: `FAST PASS / CONCERNS / FAIL · tests X/Y (<filter>) · diff: <files> · seen: <render>`. A few lines, no essay.

## FULL (milestone or task completion)
1. **Complete suites:** `node dev/run.mjs`, `unity test unity --mode EditMode`, and PlayMode where the brief lists it. Real counts.
2. **All 73 golden traces:** `node dev/trace.mjs --check` and the Unity replay (`GoldenTraceTests`) green, 73 cases in the result file.
3. **Frozen boundaries** (step 0 over the whole task's commits) and **every acceptance criterion** in the brief ticked against what was
   built; an unmet one is a blocker.
4. **The colour law:** nothing new on Kaname is red or yellow (rule 5); its test green.
5. **Docs true:** `docs/UNITY-PORT.md`, `CLAUDE.md` and the brief's "Docs, in the same change" list updated (`/sync-decision` if a
   D-number moved).
6. **Visual evidence reviewed:** before-and-after sheets, plain and with hitboxes, at game zoom (3 px per u), by day and night where it
   matters.
7. **No open design choice made canonical** (rules 3, 10): a CONCERN to surface, not to bury.

End with two separate lines: **Technical readiness: PASS / CONCERNS / FAIL** and **Designer approval: pending the controller** (or "given
on <date>, D-number" if it was). The second is never inferred from the first.

## Both modes
- Run each suite once per gate. Re-run only with a concrete reason (a fix landed, a flaky result, inconsistent evidence).
- If a valid measurement from this change already exists (same commit, same files), cite it instead of reproducing it.
- An independent Opus re-run is for substantial changes, a failure, or evidence that disagrees, not for every Sonnet run.
- **Never claim an Editor playtest that did not happen.** Automated tests and rendered sheets are not play.
- If a step was skipped, say it was skipped and why.
