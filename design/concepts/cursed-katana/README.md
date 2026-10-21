# Kaname cursed katana — dual asset handoff

## Files

- `Kaname_Cursed_Katana_Unity_Transparent.png`: transparent RGBA extracted from the approved painted artwork, for maximum visible fidelity in Unity. **Raster**, not editable paths.
- `Kaname_Cursed_Katana_Editable_Vector.svg`: genuine SVG vector tracing built from editable path/color layers; **not** a raster wrapper. Close in silhouette and rune placement, but visible variation in shading, smooth flames and glow remains.
- `Kaname_Cursed_Katana_Layered_Reference.svg`: hybrid SVG with the transparent artwork PNG on the visible top layer and editable vector artwork underneath (hidden by default). Best when tracing and adjusting against the exact target, not a pure-vector deliverable. To edit the vector, hide `reference-png-fidelity` and reveal the other grouped layers.
- `Kaname_Katana_Asset_Comparison.png`: side-by-side of original, transparent Unity version and pure vector render.

## Unity integration

Your current game draws the sword procedurally in C#. **Do not replace that implementation just by importing these assets.** In a scratch scene, try the transparent sprite with correct pivot at guard, exact sword reach, sorting order, and transparent rendering. Preserve attack timing/hitboxes; treat this as visual-only until player approved. Test at gameplay zoom and with day/night backgrounds.

## Sprite Forge

Use `Kaname_Cursed_Katana_Editable_Vector.svg` when you need genuine editable geometry. The layered reference SVG is useful for direct visual comparisons, but must not be described as a pure vector: it contains the original raster. The source SVG's many individual traced shapes are editable but not a small handcrafted rig.

## Color/shape guard rails

Preserve approved katana geometry, rounded guard, violet wrapping, glowing glyphs, upward purple flames; do not reintroduce an unrelated central energy stripe. No additional mechanics are implied by this art.
