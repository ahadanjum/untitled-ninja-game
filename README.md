# Untitled Ninja Game

A 2D ninja game, designed on paper before it is built: a new hero, a new story, a new world. The working title will be
replaced once the story has a name.

**Status: Phase 0, foundations, with the first code written:** the character test environment (the "feel ladder"): a blank white stage and
Kaname, who runs, jumps, dashes, sprints, swings a sword, pogoes off test dummies, charges and draws, and fights back against a test attacker with a parry and a health bar (rungs 0–7). Play it with `python3 serve.py` (then open http://127.0.0.1:8770/) or by double-clicking `index.html`
(keyboard or PlayStation pad); `node dev/run.mjs` runs the tests. Everything else is still design: we are deciding how the game plays, then every area,
path and shortcut is planned on paper, each with a reason. Then we build.

**Next: the Unity port (D112).** The real game will be built in Unity on the free tier; this JavaScript prototype stays as its spec. The plan is in `docs/UNITY-PORT.md`.

| Where | What |
|---|---|
| `design/DECISIONS.md` | The state of play: what is locked, what is inherited and awaiting a yes, what is open |
| `design/feel.md` | Step 0.1: the three feelings, the touchstones, the combat sketch, the scope, draft pillars |
| `design/hero.md`, `design/story.md` | Steps 0.2 and 0.3: the designer's pitch for the hero and the story, with Claude's proposals kept separate |
| `design/concepts/` | Drawn examples of Kaname: round 2 is C (like Little Shinobi) and D (more realistic); round 1 is archived in `round1-rejected/` |
| `design/core-play-ideas.md` | Step 0.4: games we have not named, and options for the core play. Options only; the designer decides |
| `design/world-sketch.md` | A first pass at how the whole world interconnects: home as the hub, hell beneath it |
| `design/references/` | Research notes on the games we draw from (so far: Castlevania: Belmont's Curse) |
| `docs/PROCESS.md` | The phases, the gate that ends each, and the Purpose Rule |
| `docs/LEARNINGS.md` | What carries over from the first game, Little Shinobi — and what to do differently |
| `docs/REFERENCE.md` | Index of the earlier projects' reusable parts |
| `CLAUDE.md` | Working rules for Claude Code in this repo |

Built with Claude Code. The designer plays in Chrome with a PlayStation controller.
