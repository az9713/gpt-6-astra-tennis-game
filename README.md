# Robo Open

A playable Windows tennis prototype built with Unity, Blender, Meshy and a largely autonomous Codex / GPT-6 Astra workflow.

Inspired by [Chong-U’s original YouTube game-building video](https://www.youtube.com/watch?v=DQfL_l5lRpk). The creator’s project was unavailable, so this is a new reconstruction using newly generated and authored assets. It reproduces the broad visual direction; it does not contain the creator’s original assets.

**[Read the illustrated development journey](https://az9713.github.io/gpt-6-astra-tennis-game/DEVELOPMENT-JOURNEY.html)** · [Markdown version](DEVELOPMENT-JOURNEY.md) · [Download the Windows prototype](https://github.com/az9713/gpt-6-astra-tennis-game/releases/latest)

## Watch a match

https://github.com/user-attachments/assets/177254a0-b72f-480b-b75d-bea627ec2576

The supplied recording is a silent 56.5-second preview, compressed from 45.2 MB to **2.81 MB** (94% smaller). [Repository MP4](docs/media/match.mp4). The HTML journey also includes a video player.

## Play on Windows

1. Download `RoboOpen-Windows.zip` from the [latest release](https://github.com/az9713/gpt-6-astra-tennis-game/releases/latest).
2. Extract the entire archive. Keep the executable, data folder and runtime files together.
3. Open `PLAY_ROBO_OPEN.cmd`, click **PLAY MATCH**, and press **Space** to serve.

The build is an unsigned prototype. Unity and Blender are not required to play the downloaded release.

| Control | Action |
|---|---|
| WASD | Move |
| Space / Enter / left mouse | Serve or swing |
| Arrow keys | Aim return direction and depth |
| Shift + Space | Power return |
| Z + Space | Lob |
| Escape | Pause / resume |
| R | Restart |
| Alt + F4 | Exit |

Follow the yellow landing marker; swing near the ball when **SWING NOW** appears. Return a serve after its first bounce. The inner sidelines define the singles court. Scoring includes deuce and advantage; **first to two games wins** this short exhibition.

## What is included

- Two toy robots, five animation clips, coral court, stepped crowds, trees, lighting and a mint CPU opponent.
- Serving, volleys, normal/power/lob returns, net/out/double-bounce rules, scoring, menu, pause, result and replay.
- Unity source, editable Blender sources, saved Meshy outputs, asset-generation scripts and public verification receipts.
- An evidence-based development journey: original prompts, human decisions, tool use, failed assumptions, fixes, costs and remaining gaps.

Racket contact uses a forgiving reach zone. Animation and CPU strategy are prototype quality. Multiplayer, progression, tournaments and a browser build of the tennis game are not included. The earlier WebGL diagnostic fixture is a separate preflight test.

## Edit and rebuild

Use **Unity 6000.5.7f1** with Windows build support. The project pins its packages, including URP 17.5.0, in `TennisGame/Packages/`.

1. Open `TennisGame` in Unity and allow package resolution/import to finish.
2. Open `Assets/Prototype/RoboOpen.unity` and enter Play mode.
3. To build from a terminal with that project closed in the Editor, run:

```powershell
& '<path-to-Unity.exe>' -batchmode -quit -projectPath "$PWD/TennisGame" -executeMethod PrototypeSetup.BuildWindows -logFile build.log
```

The output is `Builds/RoboOpen-Windows/RoboOpen.exe`. `PLAY_ROBO_OPEN.cmd` launches that location. The **Robo Open → Build prototype scene** Editor menu regenerates the scene and runs import/rules checks; it overwrites manual edits to that generated scene.

Blender **5.2.1 LTS** was used for `SourceAssets/Robot/RoboPlayer.blend` and `setup/rig_robot_blender.py`. Blender and Meshy are unnecessary for rebuilding the saved Unity assets. Regenerating a Meshy model is optional, requires your own `MESHY_API_KEY` in a private `.env`, and can spend credits. This run consumed **30 Meshy credits**; other service costs were not measured.

After building, run the standalone input and rally checks:

```powershell
powershell -ExecutionPolicy Bypass -File setup/verify_prototype.ps1
```

The retained run passed 17 rules checks and 17 input/UI checks. Its roughly one-minute automated rally recorded 15 player returns, 18 CPU returns, a best rally of 18 and two awarded points. These are functional checks; human enjoyment, fairness and broad hardware compatibility remain unvalidated. [Results and limits](PROTOTYPE_RESULTS.md) · [Public receipts](docs/evidence/).

## Explore the build

| Start here | What it explains |
|---|---|
| [Development journey](DEVELOPMENT-JOURNEY.md) | The complete chronology, human involvement and unknown unknowns |
| [Prototype design](PROTOTYPE_DESIGN.md) | Visual and gameplay choices |
| [Asset prompt](SourceAssets/Robot/IMAGE_PROMPT.md) | The new robot’s reference design |
| [Blender rig script](setup/rig_robot_blender.py) | Skeleton, skin weights and authored animation |
| [Unity scene generator](TennisGame/Assets/Prototype/Editor/PrototypeSetup.cs) | Court, stadium and presentation |
| [Game controller](TennisGame/Assets/Prototype/Scripts/TennisGame.cs) | Ball trajectories, opponent and match state |
| [Preflight results](PREFLIGHT_RESULTS.md) | Scale, animation, rendering and export failures |

To regenerate the standalone HTML after editing its Markdown source, install `setup/requirements-docs.txt`, then run `python setup/build_journey.py`. The published HTML needs no external libraries.

## Publication boundary

Credentials, original video/transcript copies, raw session history, private service identifiers, machine paths, build caches and local logs are excluded. Public documents and model metadata were checked before publication. The requested GitHub repository identity and the original creator attribution are retained.

No blanket license is asserted for third-party software or generated assets. Review the relevant provider terms before redistribution beyond this repository; Unity packages retain their own licenses.
