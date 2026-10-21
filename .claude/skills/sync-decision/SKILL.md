---
name: sync-decision
description: When a decision changes (a new or revised D-number, a retuned number, a changed rule), find every doc, test and trace it makes stale and report what each now needs. Keeps the docs true (the repo rule) without silently touching frozen physics or the golden traces.
user_invocable: true
---

# Sync a decision across the repo

A decision rarely lives in one place. A single D-number can be mirrored in `design/DECISIONS.md`, in the root `CLAUDE.md`, in
`docs/UNITY-PORT.md`, in an active brief, in a design note, and in a test's asserted numbers — and if a *simulation* number moved, in
the 73 golden traces. This skill finds all of them for one change and hands back a touch-list, so "keep docs true in the same change that
makes them false" (the repo's rule) is a checklist, not something remembered.

Borrowed in spirit from a studio-template `propagate-design-change` workflow, but rebuilt to this repo's own shapes (D-numbers, briefs,
traces, `dev/tests.js`) with no external scaffold. It **proposes**; it does not rewrite frozen physics or the recorded traces on its own
(rule 10, D120: Opus plans and reviews, Sonnet implements an approved change).

## When to use
- A new or revised D-number has just been agreed and needs to land in every doc that mentions it.
- A tuning number changed (and remember: "retune on purpose only, and change the test in the same change").
- A rule changed (the colour law, the controls, what is frozen) and you need to know what it touches.
- Before a commit, as a last check that no doc still states the old thing.

## Input
A D-number (`/sync-decision D127`), or a short description of what changed if it has no number yet. If none is given, ask which decision
or number changed — do not guess.

## Steps
1. **Find the canonical statement.** Read the decision's entry in `design/DECISIONS.md` (the state of play, authority #1). If it is a
   number, also read the matching line in `CLAUDE.md`'s *"The numbers are the design"* block. State in one line what the old value/rule
   was and what the new one is.
2. **Sweep for every reference.** Grep the repo for: the D-number itself (`D127`), the old number if one changed, and the rule's key
   words. Cover at least `design/DECISIONS.md`, `CLAUDE.md`, `docs/UNITY-PORT.md` (the *Rendering differences* table especially),
   `docs/briefs/`, the `design/*.md` notes, `dev/tests.js`, and — only if a simulation number moved — `traces/` and the Unity
   `Scripts/Sim/` and tuning asset. Do not read whole large files when a targeted grep locates the lines.
3. **Classify each hit** into one of:
   - **Doc wording** — a sentence that now states the wrong thing. Propose the exact edit.
   - **Test** — an asserted number in `dev/tests.js` (or a Unity test) that must move *in the same change* as the number (the repo rule).
     Name the test and the assertion.
   - **Trace-affecting** — the change alters the 60 Hz simulation, so some of the 73 golden traces must be re-recorded (`node
     dev/trace.mjs`) and the Unity replay re-proved (`unity test unity --mode EditMode`). Flag this loudly; it is never silent.
   - **Frozen** — during the U4 polish milestone the physics, hitboxes, attack startup, hit-stop and the traces are frozen (rule 10,
     D124). If the change would touch those, say so and stop: that needs the designer's explicit word, not a doc sync.
   - **Conflict** — the change resolves or creates a package conflict (C01–C22 in DECISIONS.md). Note which.
4. **Report** a touch-list table: file · what it says now · what it should say · class. Put frozen/trace items at the top under a clear
   warning. End with a short "safe to do as doc-only" vs "needs the designer / an implementation change" split.
5. **Do not edit in this skill.** Produce the list. Doc-only wording fixes may be applied in the same change if the user says go; anything
   frozen, trace-affecting or gameplay-changing waits for the designer and (D120) for Sonnet to implement.

## Output
A touch-list the user can act on in one pass, with the risky items (frozen, traces, tests) separated from the plain doc edits, and a plain
statement of what cannot be done without the designer.
