# Robo Tennis replication assessment

Checked September 5, 2026.

## Decision

There is enough reference information to develop a close recreation of the demonstrated visual style and gameplay. There is not yet enough material to promise the **same assets** or identical game feel. The original Unity project and Blender/model, texture, animation, UI, and audio files have not been obtained.

The creator's project is unavailable; the agreed direction is a close recreation using newly made assets. A playable Windows prototype now exists under `Builds/RoboOpen-Windows/`, with its Unity source under `TennisGame/`. One robot generation consumed 30 Meshy credits, leaving 1,237 after the verified balance check. See `PROTOTYPE_README.md` for playing instructions and `PROTOTYPE_RESULTS.md` for validation. The preflight documents below preserve the earlier pipeline checks.

## What was inspected

- Source: [Chong-U's full video](https://www.youtube.com/watch?v=DQfL_l5lRpk), titled “FABLE 5.1 Is Here And It's PERFECT For Vibe Coding Games (FULL Unity + Blender Workflow).” Metadata duration: 1,220 seconds (20:20).
- Read the supplied `transcript.txt` in full, through the closing remarks. The unrelated recommendation following the transcript is excluded.
- Downloaded the complete video to `reference/video.mp4` and metadata to `reference/video.info.json`.
- Visually inspected 244 samples at five-second intervals, covering 00:00–20:15, in 13 timestamped contact sheets under `reference/sheets/`. This is full-timeline sampled visual analysis with the complete transcript, not continuous frame-by-frame viewing or an independent listening pass over the audio.
- The video description links the playable version at [Wavedash](https://wavedash.com/games/robo-tennis/). It failed to load in this session with an SSL protocol error. No live gameplay testing was possible.
- [VibeGameDev](https://www.vibegamedev.com/) publicly advertises source projects, agent skills, and workflows. At 09:16–09:35, the creator says his resource pack will include this RoboTennis project. This statement is not verification that its current downloadable package contains every final asset.

## Observed target

| Component | What the reference establishes | What still requires original files or reconstruction |
|---|---|---|
| Main character | Small orange-and-white robot, oversized round head, top antenna, circular cyan eye, dark mechanical joints, rounded feet and hands. Close reference at ~06:25; Blender views ~12:35–14:55. | Original Meshy mesh, texture maps, topology, final rig and skin weights. |
| Opponent | Far-court CPU player; mint/cyan robot concept also appears in the turnaround sheets. | Verify whether the final opponent uses a distinct mesh or a variant; do not assume the concept sheet proves the final model. |
| Court | Red/coral playing surface, white tennis markings, turquoise surrounding surface, center net, large RT / ROBO OPEN court branding. | Exact geometry, dimensions, materials and collision setup. |
| Stadium | Stepped seating on three sides, simple multicolored crowds, sideline attendants, umpire chair, barriers, signs, floodlights, stylized trees and distant scenery. | Original Blender scene, placement data and crowd assets. |
| Camera | Elevated behind-player perspective with the full court visible; framing changes during demonstrations. | Field of view, follow behavior and camera response settings. |
| Lighting | Warm sunset sky and long shadows; dark purple/blue night sky with court floodlights. | Exact shaders, render pipeline settings, exposure and post-processing. |
| Animation | Idle, movement, serve, forehand, backhand and overhead smash. | Exact clips, contact frames, transitions and blending. |
| Gameplay | Player versus CPU, serving and rallies, directional aiming, different strokes, net/out/miss outcomes, visible tennis point/game/set counters. | Ball-flight model, hit windows, AI difficulty, aiming assistance, serve rules and match/tiebreak configuration. |
| Feedback | Ball trail, landing marker, hit particles, confetti, outcome banners and stroke/timing feedback. | Exact textures, particle settings and timing. |
| UI | Cream panels with coral/red and teal outlines, dark lettering, scoreboard upper left, instructions lower left and shot feedback lower right. | Original transparent sprite sheets, font and layout settings. |
| Sound | Creator reports ElevenLabs-generated sound effects and music (~19:23 onward). | Original music/SFX and their mix. No claim of exact audio matching from the transcript or image samples. |

The final game is the target, rather than the earlier blue-court concept art at ~05:10. The creator explicitly shows the design changing during development.

## Reusable production workflow shown

1. Generate gameplay mockups and select a visual direction (03:37–05:56).
2. Produce character turnarounds in A-pose and stadium references (05:57–07:18).
3. Establish the scale and mechanics with a Unity blockout (07:19–07:58).
4. Build the stadium in Blender and import it into Unity (07:59–10:33).
5. Generate a character from image references with Meshy (10:34–11:24).
6. Rig and author tennis-specific animation in Blender; correct joint bend direction and skin weights (11:25–15:09).
7. Integrate animation, racket contact, ball mechanics, CPU opponent and scoring; iterate using screenshots and playtesting (15:10 onward).
8. Add crowds, lighting modes, environmental detail and effects (16:38–18:39).
9. Skin the HUD with generated transparent UI assets (18:40–19:22).
10. Add and evaluate music and sound effects (19:23 onward).

Model names in the video describe the creator's workflow; they are not technical prerequisites for reproducing the game. Original prompts alone would not recreate identical generated assets.

## Local readiness

| Item | Verified result |
|---|---|
| Workspace | Unity diagnostic project created under `TennisGame/`; local Git initialized with secrets, caches, logs and builds excluded. No commit or publication. |
| Meshy | Key present, authenticated read-only balance request succeeded, balance **1,267 credits**. Key was not printed or copied into this report. |
| Unity CLI | `%LOCALAPPDATA%\Unity\bin\unity.exe`, version **1.0.0-beta.3**; command help and diagnostics work. |
| Unity Editor | CLI detects **6000.5.7f1** at `C:\Program Files\Unity\Hub\Editor\6000.5.7f1\Editor\Unity.exe`. |
| Build support | Windows and WebGL players built with zero errors and ran successfully. Browser animation and pause/resume/rotation controls verified. See `PREFLIGHT_RESULTS.md` for warnings and build timings. |
| Unity environment issue | Diagnostics initially failed because ALLUSERSPROFILE was missing from the tool process. Setting it to `C:\ProgramData` for that process allowed diagnostics to pass. No persistent environment setting changed. |
| Unity authentication | Fresh license status reports active and signed in. Actual Editor startup, package resolution and script compilation passed. |
| Blender | Subsequently installed **5.2.1 LTS** from the official portable package at `%LOCALAPPDATA%\Programs\Blender\blender-5.2.1-windows-x64\blender.exe`. Verified background execution, saving a `.blend` scene and exporting FBX. User PATH and Start menu shortcut added. See `setup/BLENDER_INSTALLATION.md`. |

## Meshy budget

The API balance confirms the stated credits are available through this key. [Current API pricing](https://docs.meshy.ai/en/api/pricing) lists Meshy-6/7 image-to-3D and multi-image-to-3D at 30 credits with standard textures, remeshing at 5 credits and auto-rigging at 5 credits. Ultra mode and 8K textures cost extra.

Illustrative reconstruction budget, not a promised total:

- Eight textured candidate generations across the player/opponent designs: 8 × 30 = 240 credits.
- Two remesh operations: 2 × 5 = 10 credits.
- Two optional auto-rigs: 2 × 5 = 10 credits.
- Subtotal: **260 credits**. A suggested first production cap of **400 credits** leaves 140 for limited retries/reference work and **867 credits untouched**.

This cap has not been activated and nothing has been generated. Image references, specialty animation, music and UI production are separate work. Blender can create court/stadium geometry and custom tennis animations without Meshy generation calls. If the original assets are supplied, Meshy may not be needed at all.

## Asset fidelity boundary

The user has confirmed that the creator's project/resource pack is unavailable. Its missing source materials include:

- Unity `Assets`, `Packages` and `ProjectSettings` directories.
- Final `.blend` files or exported meshes with texture maps.
- Character rigs and tennis animation clips.
- UI source/sprite sheets and fonts.
- Music/SFX plus the accompanying usage terms.

We will reconstruct these materials rather than wait for original files. The result should be described as a close recreation using new assets. Exact gameplay feel additionally needs testing against the live reference or inspection of its source.

After the production checks, make a matched-camera visual prototype and one complete playable rally before expanding the stadium and polishing every UI element. This tests both resemblance and racket/ball timing early.
