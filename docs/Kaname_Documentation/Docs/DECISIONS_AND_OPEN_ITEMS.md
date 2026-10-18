# KANAME — Decision Register & Remaining Questions

Use this to prevent Claude Code from mistaking attractive suggestions for implemented features. **Locked** means user affirmed direction; **Proposed** means good working hypothesis; **Unknown** means inspect prototype or ask developer.

## Locked narrative and geography
- Six chapters: Pact → Exile → Homecoming → Descent → Other Side → Reckoning.
- Semi-open Chapter 2; all three faction regions accessible early (Ashen Peaks / Sealed Archives / Hollow Market); deeper routes ability-locked; key revelations still tied to chapter beats.
- Chapter 3 gradual return: father's memorial, tonic ledger, guilty letter, hidden shaft under bedroom, council, laboratories, Elder's apparently real death, demon-gate fall.
- Chapter 4 Gatekeeper = first failed vessel, both cages on linked mechanism; Kaname refuses to choose and saves brother, mother dies despite victory.
- Chapter 5 conflicting biased histories and revelation Elder is Patron; Chapter 6 reveals personal manipulation, mother's one-note lullaby change and final encounter.
- Elder primordial from Hell; limited mortal manifestation; believes forcibly merging realms ends human mortality/grief; needs Kaname's matured vessel for full real-world power.
- Elder orchestrated many external events but **not Kaname's moral choices**; he did not understand who Kaname would become.
- Father complicit then too-late dissenter, killed by Elder; brother gets small acceptance arc and genuine crane.
- Let Go ends curse/releases Made souls but can't restore lost bodies. Carry retains Elder's essence/risk and *possibility*, not guarantee, of restoring fully transformed surviving Made.
- Mother altered ritual lullaby note; mental resistance remains Kaname's own achievement.

## Locked gameplay direction
- Unity 2D combat-focused Metroidvania; environmental lore, NPC dialogue tablets and short cutscenes; no large branching RPG system.
- **L1 Parry; R2 tap Dash including in air / R2 hold Sprint**. Square Sword; Cross Jump. Circle tap Spell / hold Heal proposed as input plan. **Triangle HOLD** only: contextual Essence absorption and explicitly prompted binding/cleansing; no tap-to-absorb separate function.
- Normal/parry-special/unblockable dodge attack taxonomy, distinct anticipation sound+shape+color.
- Violet = temporary Essence for healing/spells; Crimson = permanent growth (health, resilience, ability awakenings); Gold/Yellow = soul-liberation narrative VFX **not spendable currency**.
- Boss Crimson Core and violet-crimson fusion as ability unlock presentation; explored Crimson Fragments and corruption-node barriers.
- Blade: elegant katana visible under effects, sharp inner damage crescent + softer outer violet afterimage, cursed right arm visible; never scythe-shaped VFX as primary read.
- Existing Unity prototype: jump, dash, air dash, sprint, forward/up/down strike, pogo; preserve function while polishing.
- Movement unlocks: rope grapple, double jump/Spirit Step. Spells: Crescent Slash, Abyssal Descent, Spirit Reversal (manual parry follow-up, not autoparry).
- Main boss concepts and ideological conflict as documented in Design Bible; Warden Keeper duel can end nonlethally; not every faction figure must be a major boss.
- Reuse regions heavily in Ch. 5, compound in Ch. 3/6; solo-developer scope discipline.

## Locked Claude workflow
- User has access to **Claude Opus and Sonnet, not Fable**.
- Use Opus for difficult reasoning, planning and architecture; Sonnet for focused implementation.
- Plan via brief before writing code; protect functional controllers; build one slice then test in Unity.

## Proposed (not approved per-frame/per-code specs)
- Exact boss ability reward placements; Warden Keeper chapter placement; number of major bosses (target ~6 plus smaller duel).
- Shrine/Archive/Market room graph specifics and shortcut locations; early rope mastery; Ch. 4 double jump.
- Outer FX dimensions, hit-stop ms, precise anticipation frames and pogo physics tuning.
- Controlled fragment collection threshold for +1 health; temporary Essence cost per spell/heal; Essence gain per enemy.
- Crescent Slash Ch. 2, Abyssal Descent Ch. 2–3, Spirit Reversal Ch. 4.
- Partial restore encounter in Sealed Archives with a lucid Made who remembers name; used to foreshadow Carry.
- Exact script dialogue, Elder's final line, father's letter phrasing, brother's folded paper crane presentation.

## Needs Unity inspection / playtesting
- Which Input System and animation technique are actually used.
- Existing Air Dash reset, momentum, dash-to-sprint timing, pogo target collision, attack state cancellation and frame rates.
- Whether Circle's tap/hold dual-use creates healing/attack ambiguity, and if spell-select needed.
- Whether normal attacks can be avoided in multiple ways without parry-mandatory moments feeling unfair.
- Which mechanics require hit-stop and how particles/sound cue them at camera zoom.
- How much binding is scripted vs state-machine authored; avoid context override during combat.

## Source imagery caveat
- `references/hollow_market.png` is a **placeholder** from generated concept board, not the exact original preferred Hollow Market image seen in earlier UI.
- Only user-provided source image is `kaname_current_attacks.png`; concept board and world map are supplementary ideation assets.

## Future design questions (nonblocking for current animation milestone)
1. Exact geographical gates and room count.
2. Upgrade placement and health fragment threshold.
3. Exact spell cycling / input management in Unity.
4. Final encounter binding animation and explicit Let Go/Carry UI.
5. Detailed NPC dialogue, boss tells and audio compositions.
