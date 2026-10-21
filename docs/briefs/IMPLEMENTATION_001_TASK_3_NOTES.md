# Task 3 (the katana): inputs on hold

**Status, 2026-10-21: this is not a brief and nothing here is authorized.** The designer chose a look for the katana (D130) and asked
that it be saved for task 3. **Task 3 starts only after task 2 and task 2b are done**; then Opus writes the brief from these notes, the
`/design-review` pass checks it, and the designer authorizes it (rule 1, D120). Until then, nothing is imported into Unity, nothing in the
drawing code changes, and the open points below stay open.

## The chosen design
`design/concepts/cursed-katana/` holds the designer's handoff as delivered (their `README.md` is there too):

| File | What it is | Use |
|---|---|---|
| `Kaname_Cursed_Katana_Unity_Transparent.png` | 1394 × 914 RGBA, extracted from the approved painted artwork. **Raster.** | The version closest to the approved look; for a scratch-scene test in Unity |
| `Kaname_Cursed_Katana_Editable_Vector.svg` | 68 KB, three groups (`soft-curse-halo`, `aura`, `katana-ink-and-curse`), a **pure-vector tracing** (no raster) | Genuine editable geometry, and the source for sprite-forge work |
| `Kaname_Cursed_Katana_Layered_Reference.svg` | 808 KB, the transparent PNG on the visible top layer over the same vector (hidden by default) | Comparing against the exact target. **Not a pure vector.** Its `<desc>` still says the PNG is not embedded; that text was copied from the vector file and is wrong |
| `..._Unity_WhitePreview.png`, `..._Unity_DarkPreview.png`, `..._VectorPreview.png`, `Kaname_Katana_Asset_Comparison.png` | Previews on white and on dark, the vector render, and original / PNG / vector side by side | Looking only |

**What the art is** (read from the images): a curved katana in thick ink outline. The blade is dark steel of nearly constant width with a
lighter bevel along the edge and an angled point; a tall rounded steel guard; a deep-violet hilt wrapped with violet X crossings and a round
steel pommel; **glowing violet glyphs along the blade's flat** (X, a triangle, a T, a plus, a diamond and a few curled marks); **five or six
violet flame tongues rising from the spine** with small floating motes; and a soft violet halo round the whole blade and hilt, strongest at
the edge and the hilt. There is no central energy stripe. It follows the proportions of sprite-forge's katana v3 (the designer's
earlier keeper: `design/concepts/sprite-forge/kaname/kaname_katana_v3.svg`) but the hilt wrap differs (X crossings, not v3's diamond lattice).

## Narrowed by the designer (D132, D133, 2026-10-21)
**Task 3 is the base katana only:** the blade, bevel, angled point, rounded guard, violet X-wrapped hilt, round pommel and **the glyphs, kept and
faint**. **No flames, no motes, no halo** on the swinging blade (the designer: they look wonky in a swing). This settles points 1 and 3 below and
eases 4. The flames' and electricity's role moves to the blade's charge (D133, rung 8): the hilt's aura, the glyphs lighting one by one as charges
build, sparks at a clean parry. Task 3 should draw the glyphs so that lighting them later is possible (one mark each), but lights none of them.

## The designer's guard rails (from their README)
- **Preserve** the approved katana geometry, the rounded guard, the violet wrapping, the glowing glyphs and the upward purple flames.
- **Do not reintroduce an unrelated central energy stripe.**
- **No additional mechanics are implied by this art.**
- **Do not replace the procedural C# sword just by importing the assets.** In a scratch scene, try the transparent sprite with the pivot at
  the guard, the exact sword reach, the sorting order and transparent rendering. **Preserve attack timing and hitboxes; this is visual-only until
  the designer approves.** Test at gameplay zoom and against the day and night backgrounds.

## Checked here (2026-10-21)
- **Colour law (rule 5): passes.** The painted PNG's visible pixels are about 57 % violet, 30 % steel blue and 13 % neutral or dark; **no red,
  orange or yellow pixels.** The vector has 21 distinct colours and none is red, orange or yellow.
- **The vector is rougher than the painting:** jagged traced edges, white fringes inside the blade, and the flames and glow only approximated
  (the README says so). The painted PNG is the faithful one.
- Not checked: how any of it looks at gameplay scale or in Unity; nothing was imported.

## Open points for the task 3 brief (Opus decides, or asks the designer)
1. *(Settled by D132: no flames or halo on the blade.)* **It amends D122** ("far fewer flame wisps and a smaller glow", never hiding the steel). The art has visible flames and a halo. *How strong they
   are in play* (all the time, only while the blade is live, only on a hit) is for the brief, within the milestone's own test: **a cosmetic
   bloom must not mislead range** (IMPLEMENTATION_001, D123); the halo and flames sit outside the blade and could read as reach.
2. **Raster or redrawn.** The game draws the sword in code (`KanameArt.cs`, the ink canvas); the package assumed sprites and that was recorded as
   conflict C07. Options: a sprite for the sword, or redraw it procedurally from the vector's geometry. The README says to test the sprite in a
   scratch scene first.
3. *(Settled by D132: no flames on the blade.)* **Flames on a swinging blade.** In the art they rise upward from the spine; the blade rotates through up, forward and down cuts. Do they rise
   in world space, stream back along the cut, or stay fixed to the blade? This interacts with task 2's inner crescent and afterimage, which must
   stay the brightest and the only guide to reach.
4. **Scale.** *(D132: the glyphs stay, at least as faint marks.)* The art is 1394 px wide; in play the blade is about 46 u. The runes and the fine ink detail will not read at that size; the brief
   should say what survives (the silhouette, the edge, the flames' count) and what is dropped.
5. **The sheathed hilt must match.** Kaname wears the sword at the hip, hilt forward with a violet glow (D087). The idle hilt's wrap is a diamond
   lattice; the chosen hilt is X crossings. Change both together, or say which wins.
6. **The glyphs include a triangle and an X,** which echo PlayStation button icons (△ ✕, D126). Likely harmless on a blade, but worth a look.
7. **Provenance: settled (2026-10-21).** The handoff says "extracted from the approved painted artwork" and the repo is public; **the designer
   confirmed the art is theirs to commit.** Nothing is left to decide here. (It is saved in the working tree; committing it waits for the designer's word.)
8. **Task 3's own acceptance tests** are not written. The sword must still read as a katana, never a scythe (D122), with its physical blade
   visible under the effect, and every existing test and the 73 golden replays stay valid (D124).

## Where this sits
- Task 1 is built and approved; task 2 is built (D127) and committed; task 2b is built and approved (D135, `IMPLEMENTATION_001_TASK_2B.md`); **task 3 is this**.
- sprite-forge concepts for the katana (v1, v2, v3, and an unfinished v3 "cursed" draft) are in `design/concepts/sprite-forge/kaname/`. The
  designer's chosen design supersedes the draft; v3 stays as the keeper for shape.
