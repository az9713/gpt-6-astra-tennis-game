# Tennis production pipeline verification

> Historical preflight record. The subsequent playable prototype is documented in [PROTOTYPE_RESULTS.md](PROTOTYPE_RESULTS.md) and [README.md](README.md).

Verified locally on September 5, 2026. This is a diagnostic scene with an animated block-shaped robot and court. It is not the recreated tennis game.

## Results

| Requested check | Result | Evidence |
|---|---|---|
| 1. Unity project, packages and compilation | PASS | `TennisGame/` uses Unity 6000.5.7f1 and URP 17.5.0. Dependencies resolved into `Packages/packages-lock.json`; project scripts compiled and ran. |
| 2. Live editor automation | PASS | Unity Pipeline 0.6.0-exp.1 connected. Listed commands, opened the scene, created and saved `AutomationCheck`, and captured a correctly lit URP view. `Evidence/pipeline-commands.json`, `Evidence/editor-urp.png`. |
| 3. Animated Blender import | PASS | Blender 5.2.1 LTS source and FBX export saved. Unity verified two bones, 168 vertices, three material slots, normalized weights, normals, orientation and a two-second animation. Height: 1.9999998 metres. Sampled skin displacement: 0.3346755 metres. `Evidence/editor-import.json`. |
| 4a. Windows build and runtime | PASS | Build succeeded with zero errors. Executable launched on the RTX 3050, rendered correctly, measured 0.3018916 metres of animated skin displacement and exited with code 0. `Evidence/build-StandaloneWindows64.json`, `Evidence/windows-runtime.json`, `Evidence/windows-runtime.png`. |
| 4b. WebGL build and browser runtime | PASS | Normal build succeeded with zero errors and two warnings. Browser rendered the lit model and measured 0.3012496 metres of animated skin displacement. Pause, resume and rotation clicks updated the runtime receipt through interaction counts 1, 2 and 3; rotation was also visually verified. `Evidence/build-WebGL.json`, `Evidence/webgl-runtime.json`, `Evidence/webgl-browser-verification.json`. |

## Open the results

- Unity project: `TennisGame/`; scene: `Assets/Preflight/Preflight.unity`.
- Blender source: `SourceAssets/Preflight/AnimatedRobot.blend`.
- Windows executable: `Builds/Windows/TennisPreflight.exe`.
- Browser preview: http://127.0.0.1:8767/ (served locally from `Builds/WebGL/`; available while the local preview server is running).
- Repeatable verification and build instructions: `TennisGame/README.md`.

## Problems found and corrected

- **FBX imported 100 times too large.** Export now uses `FBX_SCALE_UNITS`; Unity's measured height is approximately two metres.
- **Background playback stopped skin animation.** Animation culling and skinned-renderer settings now allow offscreen playback; the Windows runtime measures actual animation movement.
- **Standard automation screenshot had incorrect URP lighting.** A project-local capture helper uses `RenderPipeline.SubmitRenderRequest`. Corrected editor and player captures were visually inspected.
- **Interactive editor startup stalled on dialogs.** Automated launches use Unity's `-automated` option. A direct Editor launch resolved the initial licensing startup issue; the license is active.
- **Long builds exceeded automation's command timeout.** Build helpers queue work and return immediately. Completion is checked from the actual build report. Avoid main-thread automation requests during a build.
- **WebGL development build failed at engine linking.** The exact symbol was `unitytls_ssl_set_client_transport_id`. Pipeline's development-only link generator preserves user assemblies and engine modules wholesale, even with its runtime server disabled. Browser build configuration now uses `BuildOptions.None`, which skips that preservation; the replacement build and live browser test passed. Runtime checks remain in the game code. Original failure receipt: `Evidence/build-WebGL-development-failed.json`. No engine libraries were modified and no missing-symbol checks were suppressed.

The Windows build has one warning because the optional Pipeline server is deliberately disabled inside exported players. Editor automation remains enabled. The successful WebGL build reports two warnings, retained in the build log. Browser inspection found no console errors and one warning: the FSR edge-adaptive upscaling shader is unsupported and post-processing passes will not execute. Geometry, materials, lighting, animation and controls were visibly working; browser post-processing needs a separate production check.

The successful Windows build took 31.7 seconds. The first successful WebGL build took 3,201 seconds (approximately 53 minutes), after the initial failed development build. This is measured local build time, not a prediction for later incremental builds. Keep Windows/Editor as the initial gameplay iteration path.

## Scope and remaining production work

No Meshy generation calls were made and no Meshy credits were spent. The earlier read-only balance check reported 1,267 credits. Local Git is initialized with `.env`, Pipeline credentials, caches, logs, downloaded reference media and builds excluded. Nothing was committed or published.

These checks validate the toolchain using a small fixture. Production robot generation and tennis-pose deformation, crowd/lighting performance, complete rallies, scoring, UI and sound remain to be implemented and tested. Windows uses a development build; WebGL uses a normal build with uncompressed output for local serving. Neither is a production-performance certification. The creator's source assets are unavailable; the intended game will use newly created assets matching the reference style.
