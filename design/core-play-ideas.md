# 0.4 — The core play: ideas, not decisions

You asked for ideas and my view, and said **you** decide (D081). So everything here is an option. Where I lean, I say so and why. Marks:
**[V]** verified today, or in the first game's research (`../hollow-shinobi/design-research/`); **[R]** recalled from my own knowledge, not
re-checked today. Settled already: Hollow Knight-style melee, dash, spacing, pogo, a hold-and-release strike (D009); red = dodge or jump, yellow =
parry, parry opens a big window (D010); a rope in the left hand, the blade in the right (D025); limited-use tools (D027); essence abilities (D022).

## Answers so far (the designer's, 10-17)
- **A missed parry costs health,** and there is a **health bar** (D082). *Still open:* a dedicated parry button; what unmarked attacks do.
- **Bosses play like Castlevania and Hollow Knight:** you swing, jump, pogo and move; you can use tools; **a parry is a big opening** (D083).
- **The big-hit zoom** from Little Shinobi is wanted for a parry's punish (L07). The old one: +50% zoom toward the impact in 0.2 s, a flash, shake
  and freeze, slow-motion at 16%, black bars, eased out by 1.4 s. Its blood-red particles must change.
- **The face is covered, he is sleeveless, the curse is only the right forearm** (D084).

## Games you have not named, and what each teaches
**A 2D action game with a parry and a red-yellow language**
- **Prince of Persia: The Lost Crown (2024)** [V]. The closest living proof of D010. Basic attacks parry for a brief stun; attacks with
  **yellow-glowing eyes** parry into a big counter; **red-glowing** attacks cannot be parried and must be dodged. An Athra gauge fills as you hit
  and drains when you are hit. Lessons: three classes of attack, colour paired with shape for colour-blind players, a parry-timing slider.
- **Nine Sols (2024)** [V]. A forgiving parry input with strict consequences; red attacks answered by a counter or a dodge; charge from
  parries. **Solateria (March 2026)** [V]: a 2D metroidvania built on the parry; perfect parries add slow-motion counters; an easy mode where
  *holding guard counts as a parry*.
- **Metal Gear Rising: Revengeance (2013)** [V]. The parry is the attack button aimed at the enemy's attack, and a **zandatsu** cuts a marked weak
  point and takes the enemy's core to refill health. *Taking something from an enemy as a precise act* is the nearest cousin of our essence.
- **Metroid Dread (2021)** [V]. A counter-parry, **dash-melee** that cuts through weak enemies while moving, and stalker zones.
- **Clair Obscur: Expedition 33 (2025)** and **Sifu (2022)** [V]. Each defensive verb is the *only* answer to something: dodge, parry and jump are
  never redundant.
- **Sekiro (2019)** [V]. Deflect and posture, and a counter for thrusts.

**Flow, speed and the ninja fantasy**
- **Shinobi: Art of Vengeance (2025)** [V]. Dash-cancelled attacks, kunai that fill an execution gauge, a short freeze when *you* are hit. Flaw
  to avoid: iffy wall and hazard collision, blind leaps.
- **Ninja Gaiden: Ragebound (2025)** [V]. The **Guillotine Boost** (hopping off enemies is movement and damage at once); **Hypercharge** from
  killing marked enemies *or sacrificing health*, a one-hit strike with a coloured aura. Health-for-power is a possible price for the blade.
- **Katana ZERO (2019)** [V], **Dead Cells (2018)** [V] (30–40 invisible assists; coyote time, ledge help), **Celeste** [V] (coyote 0.1 s, variable
  jump, corner correction), **Ori** [V] (bashing off enemies and projectiles), **Hyper Light Drifter (2016)** [R] (sword hits recharge the gun).

**Taking powers from the defeated (the essence family)**
- **Aria and Dawn of Sorrow (2003, 2005)** [V]: the Soul System, taking abilities from enemies. **Bloodstained** [V]: duplicates upgrade a power.
- **GRIME II (August 2026)** [V]: absorb foes with tendrils and summon molds in their shape; **GRIME (2021)** [V]: parry-absorbing heals.
- **Mega Man** [R]: defeat a boss, take its weapon. The oldest form of the idea.

**The traversal tool**
- **Hollow Knight: Silksong's Clawline** [R], **The Messenger's rope dart** [V], **Sekiro's grapple** [V], **Bionic Commando** [R] (swing is the *only*
  way to move), **Belmont's Curse's whip** [V] (with the unreliable-latch warning), **Ori's bash** [V].

**Healing, death and cost**
- **Bloodborne's rally** [R] (regain lost health by hitting back), **Sifu's aging** [V] (death costs age), **Hollow Knight's shade** [R], **Sekiro's
  resurrection** [V]. **Hades** [R]: short, characterful dialogue as a reward for trying again, which suits D017.

## The loop, in one line (my suggestion)
**Read → Parry → Open → Cut → Take.** Read the enemy's tell; parry the yellow; the enemy opens; you cut; on the elites and bosses, the finishing cut
**takes the essence**, and Kaname's face shows what it costs (D062). It joins D009, D010, D022 and D039 into one repeating beat, and Metal Gear
Rising's *zandatsu* shows it can feel great. Plain fodder stays Hollow Knight: hit, space, pogo.

## Topics, with options

### 1. The shape of a fight
- **A. Hit and read (Hollow Knight, Silksong):** spacing and pogo; parry is seasoning. **B. Duel first (Lost Crown, Sekiro):** every enemy is a
  duel. **C. Flow (Shinobi, Ragebound, Dead Cells):** cancels and momentum.
- *My view:* **two registers.** Fodder is A (fast, readable, pogo-friendly). Elites and bosses are B with the red and yellow language. That is
  what D009 plus D010 already imply.

### 2. Attack classes and their answers
- **Three classes (Lost Crown):** *plain* (any answer; a parry gives a small reward), *yellow* (parry only, for the big reward), *red* (dodge or
  jump only). Or **two classes:** unmarked attacks simply hit.
- **Exclusive answers (Sifu, Clair Obscur):** each verb is the only answer to something. Red split into **dodge** (a horizontal sweep) and **jump**
  (a low sweep) keeps both alive. Colour is always paired with shape and sound.
- *My view:* three classes. It gives fodder a gentle parry and keeps yellow special. **Question:** what do unmarked attacks do?

### 3. What a parry gives (D010: "a big opening")
- **A.** A stagger window with a bonus (Lost Crown). **B.** Charge for the blade (the Executor: deflects charge it, D021). **C.** A resource
  (Dead Cells, Nine Sols). **D.** A posture bar that breaks (Sekiro).
- *My view:* **A + B.** The parry opens the enemy *and* feeds the blade, so the blade's glow is a record of how well you read. Posture bars are
  heavy machinery (an inherited rejection of extra meters leans against them). **Questions:** a dedicated parry button, or attack-toward (Metal
  Gear Rising)? What does a *miss* cost (the first game: 10 guard; Dead Cells: a lockout)?

### 4. The blade's charge, and what it is for
- Charge builds from hits and parries; the seals burn (D021, D036). **Options for what it buys:** (a) only the hold-and-release strike; (b) also
  the essence abilities (one resource, like Rose's Arcana drawing on mana); (c) also healing (see 9).
- **Idea: the seals are also the build slots.** Found seal-tags wrapped on the blade each give a passive (like Hollow Knight's charms), and burn
  visibly as the charge builds. The look and the build system become one object.
- *My view:* **one resource** (b), because it keeps the screen quiet (D009's minimalism) and makes every parry matter to everything.

### 5. Essence abilities
- **How you get them:** (i) automatic after the kill, with a wordless beat (D062); (ii) **an execution cut** at the stagger, as in Metal Gear
  Rising; (iii) a dialogue-box moment in which the defeated speaks as the essence goes in (Belmont's Curse). They combine well: *cut, then the
  voice.*
- **How you use them:** 2–3 slots swapped at rest (Belmont's Curse, the Soul System); active spells or **transformations** (GRIME II).
- **What they do:** Chapter 1's two essences should be *different kinds* of thing, so the player learns the range. **Dual-use** (every ability is a
  combat move that also changes traversal) is the strongest teaching principle in the old research [V].
- **Tiers:** boss essences are big; *elites* could give small ones, duplicates upgrading them (Bloodstained).
- *My view:* (ii) + (iii), 3 slots, and boss essences big and few. **Question:** do you want small elite essences too, or only bosses (D022)?

### 6. The left hand: tools (D027)
- **Candidates:** shuriken (utility first, damage second: hit switches, cut ropes, interrupt), kunai (stagger), smoke bomb (break a line of sight),
  firecracker (stun, Sekiro), paper bomb (a seal tag that bursts), a sealing tag that briefly holds a made.
- **Refill:** at rest only (D027), or also by kills or executions (Shinobi, Silksong's shards).
- *My view:* rest only, with a small refill on a parry kill so tools feel earned. Keep the list to 4–5 for the demo.

### 7. The rope (D011, D025)
- **Verbs:** zip to an anchor; swing; latch onto an enemy and pull yourself to it (Belmont's Curse); pull a light enemy or object to you. Pair
  with a pogo-like air reset (Ragebound).
- **Reliability is the whole game here.** Make anchors visible, magnetic within a cone, forgiving in timing, and on their own button. If it fails
  once in ten, exploration and boss fights both suffer (Belmont's Curse's latch complaint [V]).
- **Name candidates:** kusari (chain), kaginawa (grappling rope), kusarigama (chain-sickle). The Elder gave it (D065), so a rope.

### 8. Movement and its unlocks
- **Base:** run, jump, dash (the first game: run 240 px/s, jump 720 px/s, coyote 0.1 s, buffer 0.12 s, dash 640 px/s for 0.2 s [V], a *starting
  point*). **Unlocks to place:** the rope, wall cling, double jump (an "air step"), a shadow-dash that breaks things, wall run.
- **Idea (P08b):** split them by hand. **Taught** moves (left hand, the Elder's craft: wall cling, air step, the rope) and **made** moves (right
  hand, from essence: a shadow-dash that breaks demon-sealed walls). The world then asks which hand you are using.

### 9. Healing, rest, death, currency, progression
- **Heal:** flasks refilled at rest (Souls, Dead Cells); **spend charge to mend** (Hollow Knight's Focus; the curse healing him, which he hates);
  rally (Bloodborne).
- **Rest:** a place you pray or sit, restoring tools and healing and resetting enemies, with a fast-travel link later.
- **Death:** a loss you can recover (Hollow Knight's shade); a made *echo* of you holds it (made versus chosen, in miniature); a cost (Sifu's
  aging); or nothing (Dead Cells).
- **Currency and growth:** a currency (geo, rosary) or none; upgrades from found items and bosses (Sekiro) or levels (Souls).
- *My view:* mend by spending charge (it is on-theme and cuts a meter); a recoverable echo; **no levels**, growth from bosses and found items.
  **Questions:** a currency at all? What is lost on death?

### 10. Bosses
- **D018 stands** (Hollow Knight, Silksong, Souls; Ghost of Tsushima duels).
- **Idea:** the stagger that opens the finishing cut comes from **parrying the yellow signature** (3 clean parries), but a boss can *also* be
  killed by plain damage (Hollow Knight-style), with the essence taken either way. The parry route is faster and richer (a voice line, a bonus).
  That keeps the game mostly-hitting (D009) without hiding the parry's reward.

### 11. Dialogue in play (D017)
- **Beats:** a pre-boss exchange in the box (hero left, boss right, X to advance), and a line after the kill as the essence goes in. Optional:
  short barks during a fight (Hornet's attack calls), never pausing the action.
- **Question:** do NPCs use the same box, or a quieter one?

### 12. Difficulty and accessibility
- A **parry-window slider** (Lost Crown [V]), **guard counts as a parry** in an easy mode (Solateria [V]), reduced flashes and shake, a generous
  tutorial-by-design. Make the assists *options*, not stigma.

### 13. How the hero is drawn (needed before rung 0; the 0.5 decision)
| Route | What it needs | Cost and look |
|---|---|---|
| **a. Code-drawn 2D rig** (the first game's approach) | Nothing external | Fastest to judge movement and silhouette; flatter than Belmont's Curse; fully ours |
| **b. Hand-painted 2D sprite sheets** (Shinobi: Art of Vengeance) | An illustrator and animator | Beautiful and expensive; many frames to redo when the design changes |
| **c. 3D cel-shaded models, 2.5D** (Belmont's Curse) | A 3D engine, models, rigs, animations | The closest to D012; the heaviest pipeline |
| **d. 3D rendered to 2D sprites** (the Dead Cells way) | A 3D tool for model and animation, then a render step | Smooth animation, 2D game; still needs the 3D work |
- *My view:* **start with (a) for rungs 0–3** and keep drawing separate from simulation (PROCESS rule 5), so the look can be upgraded to b, c or d
  without touching the movement. You get a first Kaname to look at and judge, and a feel test, in the shortest time.

## Questions I need from you
1. **Fight shape (1) and attack classes (2):** two registers and three classes, or something else?
2. **What a parry gives (3):** a dedicated parry button? (Answered: a missed parry costs health, D082. *Built as Claude's proposals in rung 7, D109, then rebuilt after the designer's verdict, D110: a dedicated button, Little Shinobi's shrinking windows (no lockout), three attack classes with plain attacks answerable any way and only a yellow parry a big opening, a glint (not circles) as the telegraph; the blade's charge from parries comes with rung 8. Still the designer's call.*)
3. **One resource (4):** should the blade's charge also pay for essence abilities and mending? Should the seals double as build slots?
4. **Essence (5):** an execution cut plus the voice? Small elite essences, or bosses only?
5. **Heal, death and currency (9):** mend by spending charge? A recoverable echo? A currency, and levels, or neither?
6. **The hero's face (answered, D084):** covered. In the drawings I kept a narrow eye slit so the eyes can carry the expression and the dialogue
   portrait (D017); tell me if you want it fully covered.
7. **How the hero is drawn (13):** start with (a)? And is it fine to use the first game's approach (plain JS, Chrome, a PlayStation pad, no
   dependencies) for the feel ladder?
