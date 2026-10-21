---
name: playtest-report
description: Turn the designer's loose feel verdict (dictated, often garbled) into a structured, logged report — findings, what each one means, and the candidate D-number or open question it becomes. For the one level of evidence only the designer with the real controller can give.
user_invocable: true
---

# Playtest report: structure the designer's feel

The tests prove the numbers and the rules; they cannot say whether it feels good. That is the designer's to judge with the real
controller, and it is the slowest, least-structured part of the loop. This skill captures that verdict and turns it into something
actionable and logged — so a feel session ends with clear next steps and clean DECISIONS.md material, not a wall of notes.

Borrowed in spirit from a studio-template `playtest-report`, rebuilt to this repo: findings map to the designer's own vocabulary and to
D-numbers, and nothing the designer says about feel is treated as anything but theirs (rule 1, D081). Everything Claude proposes back is
marked a proposal (rule 3).

## Two modes
- **Capture** (`/playtest-report`) — produce a blank template for a session: what was played, on what (Chrome + PlayStation pad, or the
  Unity Editor), and prompts for the designer's verdict per thing tried.
- **Analyze** (`/playtest-report <notes>` or a path) — take the designer's raw, often dictated notes and fill the template with structured
  findings.

If no mode is given, ask which.

## Capturing a verdict
The designer dictates by voice, so terms arrive garbled (the repo rule: state your reading and log it; `design/feel.md` has the glossary).
For each thing tried, record:
- **What was played** and where (the JS prototype in Chrome, or the Unity Editor — and if Unity, whether it was a real play or still
  pending; never invent a playtest).
- **The verdict in the designer's words**, quoted.
- **Your reading** of any garbled term, flagged as a reading.
- **Feel axis**: reach / speed / the pause when a blow lands / the arc / weight / responsiveness — whichever applies.

## Turning notes into findings
For each finding, give:
1. **The observation** (quoted where possible).
2. **What it means** — a feel problem, a bug, a taste call, or an approval.
3. **Where it points** — a number to retune (name it and the test that guards it), a drawing to change, or an open question for the
   designer. If it is a number, remember retuning changes the test in the same change, and a *simulation* number re-records traces.
4. **Its home** — the candidate `design/DECISIONS.md` entry (dated, in the log's voice) or the open-question list. Approvals become a
   sign-off line; complaints become a proposal (marked as such) or a question back.

## Output
A structured report plus:
- A short **verdict split**: approved · to change (proposals) · questions back to the designer.
- **Draft DECISIONS.md lines** ready to paste, in the log's dated voice, with proposals clearly marked and every taste call left to the
  designer.
- Nothing claimed as played that was not played.
