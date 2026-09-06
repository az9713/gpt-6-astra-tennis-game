# Robo Open — playable tennis prototype

> This document records the initial v0.1 milestone. For the current animation upgrade and controls, see [MOTION-UPGRADE.md](MOTION-UPGRADE.md) and [README.md](README.md).

After downloading and extracting the Windows release, double-click **PLAY_ROBO_OPEN.cmd** in this folder, then click **PLAY MATCH**. No Unity or Blender installation is needed to play the built game.

The executable is `Builds/RoboOpen-Windows/RoboOpen.exe`. Keep its entire folder together: the executable needs the accompanying data and runtime files. This Windows build is the tennis prototype; older `Builds/Windows` and `Builds/WebGL` folders contain the earlier preflight demonstrations.

## Play your first point

1. Press **Space** to serve. The game positions you behind the baseline and aims the serve into the diagonal service box.
2. Use **WASD** to move during rallies. Follow the yellow marker showing where the incoming ball will land.
3. Get near the ball and press **Space** when the callout says **SWING NOW**. You must return a serve after its first bounce. Later shots can be volleyed.
4. Use the **arrow keys** to change your return target. Left/right changes direction; up/down changes depth. The teal marker shows your target.
5. Win points by making Mint miss. A second bounce, a shot into the net, or a first bounce outside the singles lines ends the point.

The outer sidelines are doubles markings. This match uses the inner singles sidelines.

| Control | Action |
|---|---|
| WASD | Move during a rally or while receiving serve |
| Space / Enter / left mouse button | Serve or swing |
| Arrow keys | Aim returns left/right and shorter/deeper |
| Shift + Space | Flatter, faster power return |
| Z + Space | Higher lob return |
| Escape | Pause or resume |
| R | Restart the match |
| Alt + F4 | Close the game |

Scoring uses 0, 15, 30, 40, deuce and advantage. Win by two points to take a game. The server changes each game. **First to two games wins this short exhibition**; there are no sets or tiebreaks.

## What was recreated

An orange-and-white toy robot, mint CPU opponent, coral tennis court, teal surrounds, stepped crowds, trees, warm lighting and an elevated camera recreate the reference video's general visual direction. The robot was generated through Meshy from a newly generated reference image, then rigged and animated in Blender. There are idle, run, serve, forehand and backhand clips. Court, stadium, rackets, interface and sound effects were created locally.

This is a new reconstruction. The creator's original project and exact assets were unavailable. Animation, timing, CPU strategy and environment detail are prototype quality; racket contact uses a forgiving reach zone rather than precise mesh collision. There is no multiplayer, progression system, full tournament format or day/night cycle.

## Assets and cost

- Meshy generation consumed **30 credits**. Balance verified afterward: **1,237**.
- Meshy's automatic rig request was rejected without a charge. A custom Blender rig and five clips replaced it.
- The authorized initial ceiling was 400 credits. No additional models were generated.
- `SourceAssets/Robot/` contains the reference image, prompt, generated model, Blender source and imported sources; the public spending receipt is in `docs/evidence/spending.json`.
- `TennisGame/Assets/Prototype/` contains the scene, game scripts, imported model, materials and shader.
- `PROTOTYPE_RESULTS.md` records build and gameplay validation; public receipts and screenshots are in `docs/evidence/` and `docs/media/`; raw local evidence is excluded from Git.

## Edit or rebuild

Open `TennisGame` with Unity **6000.5.7f1**, then open `Assets/Prototype/RoboOpen.unity`. The menu **Robo Open → Build prototype scene** regenerates the court scene and reruns the rig/rules checks; it replaces edits to that generated scene. The editable Blender file is `SourceAssets/Robot/RoboPlayer.blend`.

The environment/model generation procedures are retained in `setup/rig_robot_blender.py` and `setup/meshy_robot.py`. Running Meshy generation can spend credits; it is unnecessary for playing or rebuilding the current prototype. Keep `.env` private.
