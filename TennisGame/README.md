# Robo Open — Unity project

Open `Assets/Prototype/RoboOpen.unity` for the playable tennis game. See the [main README](../README.md) for controls and rebuilding. The guide below records the earlier diagnostic scene. Its scope statements describe that earlier milestone.

Unity **6000.5.7f1**, Universal Render Pipeline **17.5.0**, Unity Pipeline **0.6.0-exp.1**. Resolved dependencies are recorded in `Packages/packages-lock.json`.

This scene verifies the production tools. The block-shaped robot is a deliberately simple, two-bone diagnostic fixture, not the finished tennis character or a claim of visual reproduction.

## Open

Open this folder in Unity 6000.5.7f1 and load `Assets/Preflight/Preflight.unity`.

For automated work, launch the Editor with `-automated -projectPath <this-folder>`. The installed Pipeline package starts a local authenticated server. Plain interactive launches can stall automation on modal dialogs.

If the Unity CLI reports that `ALLUSERSPROFILE` is missing, set `$env:ALLUSERSPROFILE = 'C:\ProgramData'` in that PowerShell process. This is not a request to replace any existing system value.

## What the fixture checks

- Blender FBX units: a two-metre-tall model imports at approximately two Unity units. Export uses `FBX_SCALE_UNITS`.
- Up/forward orientation: Blender Z-up maps to Unity Y-up; the cyan eye faces Unity +Z. The verification camera views that front side.
- Skin: two bones, normalized weights, imported normals and material slots.
- Animation: the imported two-second clip deforms the skin when sampled in the Editor and during actual Player playback.
- Rendering: URP Lit materials, directional lighting and shadows.
- Player interaction: pause/resume animation and rotate the model.

## Verification and builds

`Preflight/Prepare and verify scene` rebuilds the diagnostic scene and writes an import receipt under the workspace's `Evidence/` directory. It replaces that diagnostic scene, so save any intended manual changes elsewhere first.

`Preflight/Build Windows` and `Preflight/Build WebGL` produce outputs under the workspace's `Builds/` directory. Windows uses a development build. WebGL uses a normal build with compression disabled for straightforward local serving. Pipeline's development-only hot-reload preservation pulled unused engine modules into the browser build and triggered a missing Unity TLS symbol; a normal build skips that preservation while retaining the diagnostic runtime checks.

For CLI-triggered builds, use `run_script` with `Automation/BuildJobs.cs` and entry `PreflightBuildJobs.QueueWindows` or `PreflightBuildJobs.QueueWebGL`. Wait for compilation/import to finish first; these helpers refuse to queue while either is active. They return promptly and queue the work. Inspect the corresponding build receipt's modification time and result to confirm completion; a queue acknowledgement is not build success. Avoid issuing main-thread commands while Unity is building.

For URP screenshots, use `run_script` with `Automation/Capture.cs` and entry `PreflightCapture.Capture`. The bundled Pipeline screenshot command used `Camera.Render()` and produced an incorrectly lit capture in this project. This helper uses `RenderPipeline.SubmitRenderRequest` instead.

The Windows player accepts `--preflight-output <absolute-evidence-directory> --preflight-autoquit` to write its runtime receipt and rendered PNG, then exit with 0 on a passed skin-motion check. Without those arguments it remains open for interaction. Animation is configured to continue offscreen so background testing is valid.

The WebGL player exposes its actual runtime receipt in a visible status strip beneath the canvas. Its status updates when the in-game controls are used.

## Scope

No Meshy assets or credits are used by these checks. No remote repository or public deployment was created. The optional Pipeline server is intentionally not enabled inside exported players; its build warning does not prevent local Editor automation.
