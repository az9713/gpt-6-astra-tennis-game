# Robo Open v0.2 — structural tennis motion

The original robot already had a 16-bone skeleton. This upgrade makes that skeleton do more useful work: coordinated preparation, contact, follow-through and recovery, with automatic overhead smashes and a visible serve toss/trophy sequence.

[Watch the four strokes](https://az9713.github.io/gpt-6-astra-tennis-game/MOTION-UPGRADE.html) · [Download the Windows build](https://github.com/az9713/gpt-6-astra-tennis-game/releases/latest)

## Human direction

The human identified the weak animation, supplied the creator’s four-pose screenshot, and pointed back to approximately 12:00–15:00 of the [original video](https://www.youtube.com/watch?v=DQfL_l5lRpk&t=720s). After discussing the gaps, the human chose automatic smash selection using the existing swing key and authorized implementation. No human modeling, weight painting or code changes were requested during this pass. No additional Meshy credits were spent.

The reference was re-examined through the segment’s transcript and 36 sampled frames at five-second intervals. This is a targeted structural review, not a claim of uninterrupted frame-by-frame viewing. At about 12:45 the creator explains giving joints a slight bend so inverse kinematics has a preferred direction. At about 13:47–14:59 he discusses incorrect skin weights and visible tearing. The creator explicitly describes manually adjusting joint bends in his own project; that is separate from how this reconstruction was authored.

## What changed

| Layer | Implementation |
|---|---|
| Skeleton | Retained 16 deform bones; introduced explicit elbow/knee bend offsets. |
| Blender authoring | Analytical two-bone IK places wrists and ankles using explicit bend planes. Eight guide objects retain target/pole references; the Python authoring script is the editable motion specification. |
| Skin | Removed the height-only classification that accidentally assigned parts of hands to legs. Used nearest anatomical bone segments, normalized joint smoothing and identical weights at coincident UV-seam vertices. |
| Forehand/backhand | Distinct shoulder and hip rotation, off-hand positioning, knee flexion, contact and follow-through. |
| Smash | Overhead preparation, arm extension, a small jump and recovery. Triggered automatically for a reachable high ball when the player swings. |
| Serve | Visible toss, bent-knee trophy preparation, overhead contact and recovery. The ball launches after 1.4 seconds rather than immediately when Space is pressed. |
| Runtime | A short queued strike gives the actor time to plant and finish the swing. The racket arm is fitted to the contact target, and the outgoing ball starts at the racket head. |
| CPU | Uses the same stroke system and occasionally returns a lob, creating high-ball opportunities. |
| Presentation | `SMASH NOW` feedback and a separate motion viewer with toggleable bone guides. |

There are six clips: idle, run, forehand, backhand, smash and serve. The authoring contact markers follow the screenshot’s frame references: forehand 14, backhand 15, smash 25 and serve 42 at 30 fps. Ordinary gameplay uses anticipation plus the final 0.10-second strike window, or 0.14 seconds for an overhead; it does not wait through an entire half-second windup after a late swing input. The motion viewer shows the complete authored strokes.

## Controls

The game’s controls remain the same. **Space, Enter or left click swings; an eligible high ball selects smash automatically.** No extra smash key was introduced. A ball must be reachable, at least 1.92 units high and descending or near its apex. A serve still has to bounce before it can be returned. Shift and Z retain power/lob behavior on ordinary returns; an eligible overhead takes priority.

Open `VIEW_ROBOT_MOTION.cmd` to inspect the four strokes close up. Press **B** to toggle the actual skeleton’s joint guides; **Escape** closes the viewer. `PLAY_ROBO_OPEN.cmd` starts the game normally.

## Failures that improved the result

1. The first stronger poses exposed long arm spikes. Some low hand vertices had been classified as leg vertices by the original height threshold. A measured backhand edge stretched roughly 45 times its rest length. Broadening one threshold merely moved the bad boundary. Nearest-segment assignment with anatomical head/foot guards removed the hand-to-leg influence instead.
2. Smoothing weights across topology alone gave duplicate UV-seam vertices different weights, opening cracks. Synchronizing coincident-position weights closed those seams. The verification script now measures the posed distance between duplicates.
3. A root-bone translation used its local Z axis as if it were world vertical. Converting the desired world-up offset through the bone’s rest orientation made the crouch/jump move in the intended direction.
4. Raised wrist targets passed too close to the oversized head. Moving the authored overhead wrists outward and angling the racket shaft inward gave the arm more clearance.
5. The close-up viewer showed the back of the character. A texture-based diagnostic located the cyan eye and measured it behind the actor’s logical forward axis; the racket-side shoulder was also on the wrong side. Removing an obsolete 180-degree model rotation corrected both measurements. This is now checked before the motion build.
6. Helper guide actions were initially exported as extra FBX clips. Keeping the guides as references and baking only the six deform-skeleton actions removed those unintended clips.
7. A Windows text-encoding mismatch introduced a non-UTF-8 middle dot in a generated C# file. The source was normalized to UTF-8 before further edits and successful compilation.

## Evidence and limits

[Motion evidence](docs/evidence/motion/) contains the actual import, build, skin, orientation and standalone test receipts. The updated input suite covers the delayed serve, pausing its toss, backhand selection, automatic smash selection, unreachable high balls and the prohibition on volleying a serve, alongside the existing controls, scoring and replay checks. A separate one-minute run exercises real rallies and awarded points.

Skin validation checks weight normalization, hand/leg separation, coincident UV seams and edge stretch at all four contact poses. It does not certify every frame or every possible runtime IK target. Some joint-region stretching remains; the detailed receipt retains those measurements rather than calling the asset artist-finished.

This remains an arcade tennis prototype. Racket contact uses a bounded assistance zone (maximum accepted mismatch 0.60 units), a short positioning adjustment and simplified ballistic shots. It is more coordinated than v0.1, but it is not motion capture, precise racket-surface collision or a full biomechanical simulation. Human assessment of timing and feel is still valuable.

## Reproduce the pass

1. Run Blender in background mode with `setup/rig_robot_blender.py`. It imports the saved Meshy GLB, repairs weights and calls `setup/tennis_motion.py` to bake the motions. It does not call Meshy.
2. Run Blender with `setup/verify_motion_skin.py` to check the four contact poses.
3. With the project closed in the Editor, run Unity with `-batchmode -quit -projectPath <TennisGame> -executeMethod MotionBuild.Run -logFile <log>`. This regenerates the scene, checks the model’s facing and builds Windows.
4. Run `setup/verify_prototype.ps1` for fresh standalone input and rally checks.
5. For repeatable visual review, launch the player with `--motion-showcase --motion-output <directory>`. It records 360 frames at a fixed simulation step of 1/30 second, then exits. Without the output option, the viewer loops interactively.
6. Before publishing regenerated models, rerun `setup/sanitize_asset_metadata.py` in Blender and scan the actual staged Git files. The private original reference video, credentials, capture frames and logs stay excluded.
