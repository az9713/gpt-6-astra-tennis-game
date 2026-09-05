# Robo Open — prototype target

Build one playable Windows match against a CPU robot, using newly made assets in the reference video's style. This is a reconstruction, not the creator's original project.

## Visual reference

- Orange spherical robot head, white band and belly, cyan eye, antenna, dark ball joints and rounded shoes. Character reference around 06:25.
- Coral court, white lines, teal surrounding plaza, cream/coral/teal HUD, elevated camera behind the player, golden-hour directional lighting. Final stadium reference around 18:15.
- Simple stepped seating, colorful spectators, floodlight poles and trees establish the setting. Detailed crowds, night mode and the complete original UI are outside this milestone.

## Playable scope

- Single player versus Mint, a differently colored version of the same robot.
- WASD movement; SPACE or left mouse to serve/swing; arrow keys to adjust target; SHIFT + SPACE for power; Z + SPACE for a lob.
- Ballistic flights, diagonal service boxes, one bounce allowed, net/out/double-fault outcomes, 0/15/30/40/deuce/advantage scoring.
- Short exhibition: first to two games; serving alternates by game. This is not a full set/tiebreak format.
- Start, pause, resume, restart and match result UI. A brief input buffer and generous reach make the initial arcade prototype playable.
- One Meshy-generated model with a custom Blender skeleton and idle/run/serve/forehand/backhand clips. CPU palette changes happen in the game shader.
- Locally synthesized racket, bounce and point sounds. No copied soundtrack.

## Asset spending

Authorization: maximum 400 Meshy credits for the initial round. One textured model consumed 30. Automatic rigging was rejected with HTTP 422 and did not consume credits; rigging and animation were completed in Blender. Verified balance afterward: 1,237. The ledger is `SourceAssets/Robot/meshy-ledger.json`.

## Acceptance checks

1. Imported model contains a functioning skin and all five clips.
2. Deterministic rules checks cover deuce, game/match transitions, service boxes, lines and ballistic targeting.
3. Both players complete returns through the normal simulation, with a sustained rally and points awarded.
4. Movement, aiming, serve/swing, pause/resume and restart work through the input/UI paths.
5. Windows player builds and runs; rendered screenshots and runtime receipts are saved under `Evidence/Prototype/`.

Reference-image prompt and generation method: `SourceAssets/Robot/IMAGE_PROMPT.md`. Model generation documentation: https://docs.meshy.ai/en/api/image-to-3d. Prices verified against https://docs.meshy.ai/en/api/pricing.
