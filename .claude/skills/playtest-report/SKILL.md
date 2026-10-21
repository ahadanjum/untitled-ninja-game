---
name: playtest-report
description: The centre of gameplay iteration. Capture the designer's playtest verdict (dictated, often garbled) in their own words and sort it into approved behaviour, bugs, requested improvements and unresolved design choices, each with its next step. Their play with the real controller is the final authority on combat feel and visual taste.
user_invocable: true
---

# Playtest report: the designer's verdict drives the next step

Tests prove the numbers and the rules; only the designer playing with the PlayStation controller says whether it feels and looks right.
Their experience is the **final authority on combat feel and visual taste** (rule 1, D081): no test result, render or reasoning of
Claude's outranks it. This skill turns a play session into the next iteration's to-do list, fast.

## The loop it anchors
build (Sonnet) → `/gate-check fast` → **the designer plays** → `/playtest-report` → fix or tune inside the brief, or ask, or sign off.
Ask for a playtest as soon as a FAST gate passes; do not stack more polish on an unplayed build.

## Modes
- **Analyze** (`/playtest-report <notes>` or a path): the usual one. Take the designer's notes and sort them.
- **Capture** (`/playtest-report` with nothing): give a short prompt list for the session: what to try, on what (the Unity Editor or
  the JS game in Chrome), and what to look for. Keep it to the build's changes.

## Capture the verdict
- **Quote the designer's own words** for every finding. They dictate by voice: where a term looks garbled, give your reading next to the
  quote, marked as a reading (`design/feel.md` has the glossary). Never paraphrase a verdict into something stronger or softer.
- **Where it was played:** Unity Editor or Chrome, which build (commit). Only the designer plays. **Claude never reports an Editor
  playtest it did not perform**; automated tests and rendered sheets are not play.

## Sort every finding into one of four
1. **Approved:** behaviour or look the designer accepted. Becomes a sign-off line; it is now protected (rule 10).
2. **Bug:** it does not do what the brief or the code says it should. Fix inside the current task; a bug needs no D-number.
3. **Requested improvement:** works as specified, the designer wants it different. If inside the brief's bounds (a tuning, a width, a
   strength), it goes straight to implementation with one tuning round. If it moves a frozen number, the hitboxes, timing or the traces,
   it needs the designer's explicit word and `/sync-decision`.
4. **Unresolved design choice:** a taste or core-play question the notes do not settle. Ask it back as one question with a
   recommendation (rule 3); do not decide it.

## Output (short)
- A four-heading list in that order, each finding: the quote · (reading) · the next step (file or number, the test that guards it).
- **Draft DECISIONS.md lines** only for what is formal: an approval that signs off a task or rung, a behaviour or rule change. Minor
  artistic adjustments inside an approved brief go in the brief's notes or the commit message, not a D-number.
- Nothing claimed as played that was not played.
