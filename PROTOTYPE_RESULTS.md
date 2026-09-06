# Robo Open — prototype validation

> This document records the initial v0.1 milestone. For the current animation upgrade and controls, see [MOTION-UPGRADE.md](MOTION-UPGRADE.md) and [README.md](README.md).

Validated locally on September 5, 2026, with Unity 6000.5.7f1 and an NVIDIA GeForce RTX 3050 Laptop GPU. This records an automated prototype acceptance run, not a human playability assessment or a claim of exact fidelity to the creator's project.

## Deliverable

- Windows game: `Builds/RoboOpen-Windows/RoboOpen.exe`.
- Convenient launcher: `PLAY_ROBO_OPEN.cmd`.
- Playing instructions and limitations: `PROTOTYPE_README.md`.
- Unity scene: `TennisGame/Assets/Prototype/RoboOpen.unity`.
- Editable character: `SourceAssets/Robot/RoboPlayer.blend`.

## Verified behavior

| Check | Result | Evidence |
|---|---|---|
| Windows build | Succeeded, zero errors. One warning explains that the Unity Pipeline automation service is disabled in standalone players because no runtime configuration was supplied. The game does not need that service. | `Evidence/Prototype/windows-build.json` |
| Robot import and deformation | Passed: 16 bones, 11,813 imported vertices, five animation clips. Sampling the forehand changed the skinned mesh by approximately 0.394 Unity units at its most displaced vertex. | `Evidence/Prototype/robot-import.json` |
| Rules | All 17 checks passed: point labels, deuce/advantage, game and match transitions, server alternation, court lines, diagonal service boxes and ballistic targeting/net clearance. | `Evidence/Prototype/rules-tests.json` |
| Standalone input and UI | All 17 checks passed through queued Unity input events and actual UI pointer clicks; process exited 0. | `Evidence/Prototype/prototype-input-tests.json`, `windows-input.log` |
| Standalone rally and scoring | One-minute run completed with 15 player returns, 18 CPU returns, a longest rally of 18 shots and two points awarded through normal out-of-bounds rules; process exited 0. | `Evidence/Prototype/prototype-runtime.json`, `windows-rally.log` |
| Rendering | Inspected menu, serve, rally, pause and match-result captures from the actual renderer. The final camera keeps the serving player above the controls. Ball and markers use bright materials so lighting does not change their identification colors. | `Evidence/Prototype/prototype-*.png` |

The input checks cover menu → match, serve, movement, aim, pause/frozen physics, resume, restart, return to menu, normal return, power return, lob, result screen and replay. Return-type comparisons use controlled incoming-ball fixtures; the result-screen check uses a match-point fixture and lets the normal second-bounce rule finish the match. The separate one-minute test uses normal serving, CPU movement, shot physics and scoring with an automated player. It is not a recorded human match or a complete naturally played two-game match.

The latest receipts correspond to the executable in the delivery folder. Runtime logs were checked for exceptions and shader errors. There is no sustained frame-rate benchmark or hardware compatibility survey; the code targets 60 fps.

## Asset provenance and spending

The reference video established the visual direction; its original project was unavailable. A new reference image and a Meshy image-to-3D task produced the robot. Meshy automatic rigging returned HTTP 422 without a charge, so a custom Blender skeleton, skin weights and five clips were made locally. Stadium, court, rackets, UI and short synthesized sound effects were also made locally.

Meshy spending: **30 credits**. Balance checked after generation and the rejected rig request: **1,237**. Initial authorized ceiling: **400**. See `SourceAssets/Robot/meshy-ledger.json` and `IMAGE_PROMPT.md`.

Animation transitions, racket reach, CPU strategy and environment detail remain prototype approximations. Multiplayer, progression, tournament sets/tiebreaks, night mode and an exact reconstruction of the original animations/UI are outside this deliverable. No WebGL tennis prototype was built; the existing WebGL output is the earlier preflight demonstration.

## Repeat the standalone checks

From this workspace, run `setup/verify_prototype.ps1`. It launches two temporary player processes, exercises the input/UI checks and one-minute rally, requires fresh passing receipts and zero exit codes, and checks the logs for common runtime exceptions. The logs and screenshots remain in `Evidence/Prototype/`.
