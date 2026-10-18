# The Unity port: plan (D112)

**Status (2026-10-18): setup done, project created.** Done: the JavaScript prototype is committed and tagged (`db0475d`, `js-prototype-rung7`); Unity Hub 3.22.3, the Unity CLI 1.0.0-beta.13 (Homebrew) and Unity's Claude Code plugin 0.1.8-beta (32 skills, user scope) are installed. The Editor **6000.3.26f1** is installed (Apple silicon, 9.3 GB; D114); the Hub's offer of 6000.6.6f1 was declined and removed. The designer has signed in to Unity Hub. The repository is public on GitHub (D113): [ahadanjum/untitled-ninja-game](https://github.com/ahadanjum/untitled-ninja-game), with the milestone and issues [U0–U7](https://github.com/ahadanjum/untitled-ninja-game/milestone/1). The Unity project now exists in `unity/` (Universal 2D template, Editor 6000.3.26f1, Unity Pipeline package 0.8.0-exp.1, Input System 1.20, Test Framework 1.6, URP 17.3, 2D Animation and PSD Importer already in the manifest; no Unity Cloud), and the Personal entitlement is on this Mac. Work happens in a local session rooted in this repository, because the Editor, the licence and the PlayStation-pad playtests only exist on the designer's Mac; GitHub holds the branches, issues and pull requests. **U0 (the body, stage, loop, controls, camera) and U1 (Kaname's look, moved up from U6 at the designer's request, D118) are built and tested: 135 EditMode and 6 PlayMode tests, 7 golden replays of the JavaScript game. They wait for the designer's eyes: press Play on `Assets/Scenes/TestStage.unity` and P cycles Kaname through his poses.** The designer wants the real game built in Unity on the free
tier, and wants it started now, while the character's core mechanics are being built. The JavaScript feel ladder (rungs 0–7, 82 tests) stays as
the **executable spec** until Unity matches it.

## What was checked today
| Question | Answer | Source |
|---|---|---|
| Is the free tier enough? | **Unity Personal**: free for individuals and small organizations with under **$200K** revenue and funds raised in the last 12 months. The splash screen is optional in Unity 6. | [unity.com/pricing-updates](https://unity.com/pricing-updates), [Personal](https://unity.com/products/unity-personal) |
| Which version? | **Unity 6.3 LTS**, newest patch **6000.3.26f1** (released 2026-10-08), supported to December 2027. Unity 6.0 LTS support ends this month, so we start on 6.3. | [Unity 6 releases](https://unity.com/releases/unity-6), Unity's release API |
| How big? | Editor for macOS Apple silicon: **5.19 GB** to download, **9.57 GB** installed (docs module 0.49 GB, optional). Unity Hub installer: **165 MB**. | Unity's release API; the Hub installer's headers |
| How do we install? | `brew install --cask unity-hub` (3.22.3) and `brew install --cask unity-cli` (1.0.0-beta.13). The Unity CLI is a standalone program that installs editors, creates and opens projects, and lets Claude Code drive a running Editor. | [Unity CLI](https://docs.unity.com/en-us/unity-cli/use-unity-cli), Homebrew |
| What is "the Claude Code plugin"? | **Unity's official plugin**, `Unity-Technologies/unity-agent-plugin`: about 30 Unity skills (2D and tilemaps, UI, Shader Graph, audio, and so on). It ships skills only, no MCP server. Skills that control the Editor run Unity CLI commands (needs the Unity Pipeline package in the project and the Editor open). A separate Unity MCP server is optional (`unity mcp configure`). Third-party options exist (Coplay's MCP for Unity) and are the fallback. | [Unity's docs](https://docs.unity.com/en-us/ai/unity-plugin/claude-code), [Claude marketplace](https://claude.com/marketplace/plugins/unity) |
| This Mac | Apple silicon (arm64), macOS 26.3.1, 568 GB free, git, node, Homebrew. No Unity yet. | checked |

## The setup, in order (who does what)
1. **Checkpoint the JavaScript prototype in git** (no commit exists yet): one commit, tagged `js-prototype-rung7`, before anything risky. *(Needs your OK: I commit only when asked.)*
2. **Install the tools:** `brew install --cask unity-hub unity-cli` (about 165 MB plus the CLI). *(I run it after your OK.)*
3. **Install the Editor:** `unity install 6000.3.26f1` (**5.19 GB**), Apple silicon build. *(I run it after your OK; it takes a while.)*
4. **You sign in:** open Unity Hub, sign in with your Unity ID (or create one), accept Unity's terms, and activate a free **Personal** license.
   *I cannot enter your credentials or create the account for you.*
5. **Create the project:** `unity/` inside this repo, from the **2D (URP)** template (Unity 6.3), version control set to Force Text and Visible Meta Files.
6. **Connect the Editor:** `unity pipeline install`, then `unity status` should reach the open Editor.
7. **Install the plugin:** `claude plugin marketplace add Unity-Technologies/unity-agent-plugin`, then `claude plugin install unity@unity-agent-plugin --scope user`
   (or `/plugin` in a session, or Plugins in the desktop app). Check with `/unity:`. *(It changes Claude Code's configuration, so I ask first.)*
8. **Commit the empty Unity project** with a Unity `.gitignore` (Library, Temp, Logs, UserSettings, builds), so the repo stays small. Git LFS is not needed yet.

## How the port works
**The JS game is the spec, and the port is proven by replaying it.** Nothing is "retuned by eye" in Unity until it matches.

- **Pure C# simulation, no Unity physics.** A line-by-line port of `hero.js`, `stage.js`, `dummy.js` and `striker.js` into an assembly with no Unity
  dependencies, using `double` so the numbers match. No Rigidbody2D, no colliders for the hero: his own AABB collision, exactly as now. It is stepped at a fixed 60 Hz
  by an accumulator, with rendering blended between steps on fast displays (as now).
- **The numbers live in one place:** a `KanameTuning` ScriptableObject that mirrors the `K` table, so the designer can tune in the Inspector. Units stay Little Shinobi's
  pixels (a tile is 32 u; Kaname is 16 × 34 u); one Unity unit is one tile.
- **Input:** Unity's Input System, actions mirroring the current bindings (move, up, down, jump, dash, attack, parry), keyboard and gamepad (PlayStation pad through the standard gamepad layout).
- **Hit-stop and the zoom kick** are done in the simulation (a freeze that keeps buffered presses), never with `Time.timeScale`. The camera is a critically damped spring
  (Unity's `SmoothDamp` is the same maths) leading him by 36 u.
- **The look is drawn by a view layer** (`KanameView`) that reads the simulation's state (`vis`, `swing`, `trail`) and poses child objects: the head, body, feet and sash as
  sprites; the scarf as a tapered line along the same 8-point chain; the blade, its crescent trail, the speed lines and the glints as small generated meshes; the glow as
  soft sprites or URP 2D bloom. The pose maths (`kanamePose`, `kanameHeadFrame`) is ported as it is, so the scarf stays tied to his head.
- **Real art comes later and slots in:** Unity's 2D Animation and PSD Importer packages take layered Photoshop files (`.psb`) and rig them, so the Adobe tools the
  designer mentioned feed straight in. The procedural silhouette is the placeholder until then.
- **Tests:** Unity Test Framework. The 82 JavaScript acceptance tests become EditMode tests on the simulation. On top of that, **golden traces**: a script runs scripted
  inputs in the JS game (deterministic, seeded) and records the state every step; the C# replays the same inputs and must match to a small tolerance. If the traces
  match, the feel matches. PlayMode tests cover drawing and take the evidence screenshots for a contact sheet.

## The port, in rungs (each ends with the designer's sign-off, by hand)
| | What | Proven by |
|---|---|---|
| U0 | The project, the stage, the body (a box), the loop, input, the camera | the project opens and runs; the traces for falling, landing and the camera match |
| **U1** | **Kaname's look** (moved up from U6, D118): the ink silhouette, band, scarf, sash, eyes, hilt glow, blade, crescent trail, speed lines, glints, as the JavaScript game draws them, with a pose reel (P) to see every pose | the designer's eyes on the poses; unit tests (the scarf is tied to the head, nothing red or yellow, drawing changes no state); look traces replayed from the JavaScript game |
| U2 | Run and jump | the traces for run, stop, turn, jump, coyote and buffering match |
| U3 | Dash and sprint | the dash and sprint traces match |
| U4 | The sword, the pogo, the dummies, hit-stop | the sword and pogo traces match |
| U5 | The charge and the draw | the charge and draw traces match |
| U6 | Health, the parry, the striker, the red and yellow language (and the look of the dummies, the striker, the health bar and the glints) | the parry and hurt traces match |
| U7 | The designer plays it in Unity and compares it with the JS version | their sign-off; then the JS game moves to `prototype/` as a reference |

After U7 the feel ladder continues (rung 8, the blade's charge, and on) **only in Unity**, and the world, rooms and bosses are built there.

## Rung U0, as built (D116)
**Layout** (`unity/Assets/`):
| Folder | What |
|---|---|
| `Scripts/Sim/` (`Kaname.Sim`, **no Unity references**) | `Tuning` (the `K` table, `[Serializable]`), `Mathx` (the helpers and the camera spring), `Controls` (`Act`, `Dev`, `ControlState`), `Stage`, `Hero` (the body), `World` (the step, the camera, the test keys), `FixedClock` (the 60 Hz loop and the display probe) |
| `Scripts/Game/` (`Kaname.Game`) | `GameDriver` (the loop: read, step, draw), `InputReader` (Input System actions, in code), `StageView`, `HeroView`, `CameraRig`, `DebugHud` (immediate-mode text), `Pixel` (the white sprite everything is made of), `KanameTuning` (the asset) |
| `Editor/` | `ProjectSetup`: menu Kaname > Set up the test stage (rebuilds the `TestStage` scene, keeps the tuning asset) |
| `Tests/EditMode/` | `SimulationTests` (the facts), `InputReaderTests` (keyboard and pad through virtual devices), `GoldenTraceTests` (the replay) |
| `Settings/KanameTuning.asset`, `Scenes/TestStage.unity` | the numbers and the scene (a camera and one `GameDriver`; the stage and the box are built in code) |

**Run it:** open `unity/` in Unity 6.3 (`unity open unity`), open `Assets/Scenes/TestStage.unity`, press Play. Keys: arrows or WASD, Space/Z, Shift/X/K, J/C, L/V (all read; only turning and falling show until U2), G grid, N night, H hints, ` numbers, [ ] zoom, R reset, **T toss him 200 u up and let go, P the next pose of the pose reel**. The pad: stick or d-pad, ✕ ○ □ R1 R2 L1 L2.
**Test it:** `unity test unity --mode EditMode --output /tmp/results.xml` (about 30 s with the Editor closed); with the Editor open, drive it with `unity command run_tests` (EditMode) and `unity command run_tests --mode playmode --async_tests true` then `test_status` (PlayMode; the Editor must be focused or the run stalls). **Re-record the spec** after any change to the JavaScript game: `node dev/trace.mjs` (and `node dev/trace.mjs --check` fails if a trace would change).
**How the proof works:** `dev/trace-scenarios.js` lists scenarios; `dev/trace.mjs` runs each in the real JS game one 60 Hz step at a time and records the controls and the state; `GoldenTraceTests` replays the controls through the C# and requires every number to match to 0.001 u (the first run matched far tighter: a change of one part in 2200 in gravity fails all seven). Each later rung adds scenarios and adds its name to `Ported` in `GoldenTraceTests`.
**Differences from the JavaScript game, on purpose:** the stick goes through the Input System's own small dead zone before ours (0.18 to 0.95); the view follows the window's width instead of letterboxing to 16:9; the night backdrop is a flat colour (the gradient and stars come with the look in U6); the **T** key is new.

## Rung U1, the look, as built (D117, D118)
The designer saw the U0 box and asked for the look before the mechanics. **How it is drawn:** `js/art_hero.js` is a canvas drawing (paths, curves, gradients, a transform stack), so the port brought a small canvas with it:
| Folder | What |
|---|---|
| `Scripts/Ink/` (`Kaname.Ink`, **no Unity references**) | `InkCanvas`: the HTML-canvas calls (paths, arcs, ellipses, curves, fill, stroke with round caps, linear and radial gradients, save/restore, translate/rotate/scale, global alpha, one convex clip) recorded as triangles in drawing order; `Rgba`, `Gradient` |
| `Scripts/Look/` (`Kaname.Look`, **no Unity references**) | `HeroLook` (the eased pose `Vis`, the 8-link scarf, the speed-line trail, the squash spring: `updateVis`, `updateScarf`, `updateTrail`, `kanameHeadFrame`, `kanamePose` ported), `KanameArt` (`drawKaname`, `drawBlade`, `drawCrescent`, `drawTrail`, `drawScarf` ported line for line), `Brush` (ink shapes, limbs, glow dots, glints), `Palette` |
| `Scripts/Game/` | `InkRenderer` (the canvas's triangles as one mesh, one draw call), `HeroView` (steps the look with the simulation, draws it every frame, blended between steps), `PoseReel` (the P key: makes him run, sprint, dash, jump, cut forward, up and down, draw, charge, guard, be hurt and be down, on a loop, by driving his body and telling the look what the mechanics would say; a test aid until the real mechanics replace it) |
| `Editor/PoseSheet.cs` | Kaname > Render the pose sheet: every pose run for a fixed number of 60 Hz steps and photographed by a camera through the game's own ink material: deterministic. The result is `design/concepts/unity-look-poses.png` |
**Decisions and facts:** the outfit is **black** and the band, tail and sash a **deep violet** (`#3a2273`, edge `#150a30`) at the designer's word (D117, also changed in the JavaScript game); the project uses **Gamma colour space** (the canvas blends in gamma, so a linear project lightened every colour and glow) and **4x MSAA** on the render pipeline for smooth edges (`ProjectSetup` sets both). The look reads the simulation and changes none of it (a test checks that drawing leaves the state untouched). What is NOT yet proved against the JavaScript game: the look's numbers (the pose, the scarf, the trail) are not yet replayed from recorded traces, and the look of the dummies, the striker and the health bar waits for their mechanics (U4, U6).

## Decisions
1. **A git checkpoint** of the JavaScript prototype: **yes, done** (one local commit and a tag, nothing pushed).
2. **Unity 6.3 LTS, 6000.3.26f1**, the newest patch of the newest LTS: **yes**.
3. **Where the project lives:** **`untitled-ninja-game/unity/`** in this repo: one repo, the spec and the port side by side.
4. **Template:** 2D with the Universal Render Pipeline (my recommendation, not yet confirmed: it has 2D lights and bloom for the blade's glow).
5. **The MCP server:** skip it for now (the plugin's CLI route is enough); add it later only if needed.
6. **The plugin:** **yes, installed** (user scope; `claude plugin disable unity@unity-agent-plugin` turns it off).

## Risks and fallbacks
- The Unity CLI is in beta (1.0.0-beta.13) and the official plugin is new (September 2026). *Fallback:* Unity Hub for installing, and Coplay's MCP for Unity for driving the Editor.
- A Unity ID, its terms and the Personal license are the designer's to enter. Nothing can proceed without that step.
- The 5 GB download takes a while on a slow connection.
- **The procedural ink art is the biggest visual job** to rebuild (about 300 lines of drawing code). It is a placeholder for hand-made art anyway.
- Floating-point differences between JavaScript and C#: the simulation uses `double` and the traces allow a tolerance of 0.001 u.
- Unity 6.0 LTS support ends this month: we deliberately start on 6.3.
