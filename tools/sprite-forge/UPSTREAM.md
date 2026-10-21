# sprite-forge: where it came from

A concept-art tool, vendored into this repo on 2026-10-20 (**D129**). It draws animated SVG sprites and seamless tileable textures
from a description and bakes them into PNG sprite sheets, GIFs and JSON. It is for exploring characters and world pieces: **Unity
owns the look** (D124), and nothing it makes is canon until the designer says yes.

| | |
|---|---|
| Source | https://github.com/gididaf/sprite-forge (`main`) |
| Commit | `16294d476ef31b5e51f8242c71bfbddac8a9f7c5` (2026-06-11, "docs: wrap-up — record baseline wrapped commit") |
| Vendored | 2026-10-20 |
| Licence | **MIT, as stated in upstream's README ("License: MIT"). Upstream has no `LICENSE` file**, so there is no copyright line to carry over. If this repo is ever distributed beyond the designer's own use, ask the author to add one, or write to them. |

## What is here

| Path | What it is | Changed from upstream? |
|---|---|---|
| `tools/sprite-forge/sprite-forge.py` | The CLI: SVG to frames (`rsvg-convert`), sprite sheet, GIF, metadata, mirror and texture tiling (Pillow) | No, byte-identical |
| `tools/sprite-forge/README.md` | Upstream's README (its install section does not apply here; see below) | No, byte-identical |
| `.claude/skills/sprite-forge/` | The Claude Code skill, `/sprite-forge`: `SKILL.md` plus its `rigs/`, `styles/`, `principles/`, `textures/` and the optional `reference/` | **`SKILL.md`** (below) and the local `compare.py`; everything else byte-identical |

**Left out on purpose:** upstream's `install.sh` (it installs globally), `showcase/` (1.1 MB of example sprites, still at the URL
above), `tests/smoke/` (the author's smoke tests), its own `CLAUDE.md` (its development notes, not this repo's rules) and
`.claude/settings.local.json` (the author's personal permissions, with their home paths).

## The patches to `SKILL.md` (and one local file)

1. **A section, "In this repo", after the intro:** the repo's rules for the skill (concept and exploration only, outputs under
   `design/concepts/sprite-forge/<name>/`, no red or yellow and no `high-saturation` style without asking, no model downloads without a yes).
2. **Phase 0 rewritten:** it no longer runs upstream's `curl … | bash` installer; it checks the vendored CLI and the two
   dependencies and asks before installing either.
3. **The CLI path:** every `sprite-forge <file>.svg …` command is now `python3 tools/sprite-forge/sprite-forge.py <file>.svg …`, run from
   the repo root. (One prose mention in `.claude/skills/sprite-forge/textures/README.md` still says `sprite-forge`; read it as the same path.)
4. **FAST STATIC mode** (2026-10-21): a section after "In this repo" and a pointer at the top of the complexity tiers, for weapons,
   props and small variants of an approved SVG (one change, two renders, a before-and-after sheet, then the designer's approval).
   It uses one local file not in upstream, `.claude/skills/sprite-forge/compare.py` (the before-and-after sheet, Pillow only).

## Using it

```
python3 tools/sprite-forge/sprite-forge.py design/concepts/sprite-forge/<name>/<file>.svg --facing left
```

or, in Claude Code from the repo root, `/sprite-forge <what you want>`. The tool writes its PNG, GIF and JSON beside the SVG. It needs
Python 3.10+, **Pillow** and **`rsvg-convert`** (librsvg: `brew install librsvg`). On 2026-10-20 librsvg 2.63.2 was installed with
Homebrew (the only thing installed outside the repo); Pillow 10.3.0 was already present.

**Not set up:** the optional reference-image subsystem (`.claude/skills/sprite-forge/reference/`: `mflux` and a model of about 9.6 GB).
Do not run it without the designer's yes to that download.

## Updating it

Clone upstream into a scratch folder, diff each path above against its copy, bring over what is wanted, and re-apply the four
`SKILL.md` patches (keep `compare.py`). Record the new commit in the table at the top. Never run upstream's `install.sh` from here: it writes to
`~/.sprite-forge` and `~/.local/bin`, and it deletes and replaces `~/.claude/skills/sprite-forge`.
