# Tennis recreation: pre-flight status

> Historical preflight record. The subsequent playable prototype is documented in [PROTOTYPE_RESULTS.md](PROTOTYPE_RESULTS.md) and [README.md](README.md).

Historical preflight snapshot from September 5, 2026, before the prototype was built. The playable Windows prototype, asset spending and latest validation are now documented in `PROTOTYPE_README.md` and `PROTOTYPE_RESULTS.md`. The findings below describe the earlier diagnostic stage.

## Resolved

- Original creator files are unavailable. The agreed direction is newly created assets closely matching the video. Exact asset identity is not an attainable acceptance criterion from the available inputs.
- Blender 5.2.1 LTS runs in background mode, saves a scene and exports FBX. See `setup/BLENDER_INSTALLATION.md`.
- Unity CLI 1.0.0-beta.3 and Unity Editor 6000.5.7f1 are installed.
- Fresh `unity license status --json` reports `active: true` and `signedIn: true`. The earlier stale-session diagnostic is not evidence of a present licensing blocker.
- Windows standalone and WebGL build-support folders exist; WebGL BuildTools exists.
- A local 3D cross-platform project template is present.
- Meshy authenticated balance check earlier in this task confirmed 1,267 API credits. No generation has been submitted by this task.
- Hardware check: 31.7 GiB reported RAM, NVIDIA GeForce RTX 3050 Laptop GPU, and approximately 261.1 GiB free on C:. No actual rendering/performance benchmark has been run.

## Checks before substantial asset generation

| Check | Evidence / gap | Completion criterion |
|---|---|---|
| Unity project and package resolution | **PASS.** Unity 6000.5.7f1 project created; URP 17.5.0 and dependencies resolved, lockfile saved, diagnostic scripts compiled. | Evidence: `TennisGame/Packages/packages-lock.json`, `Evidence/editor-import.json`. |
| Live Unity automation | **PASS.** Unity Pipeline 0.6.0-exp.1 connected; commands listed, scene opened, `AutomationCheck` object created and saved. Correctly lit URP screenshot captured and visually inspected. | Evidence: `Evidence/pipeline-commands.json`, `Evidence/editor-urp.png`, saved `Assets/Preflight/Preflight.unity`. |
| Blender-to-Unity import | **PASS.** Two-metre model, two bones, three materials, normalized skin weights, normals and orientation verified. Two-second imported clip deforms the skin in Editor and actual Windows playback. | Evidence: `SourceAssets/Preflight/AnimatedRobot.blend`, imported FBX, `Evidence/editor-import.json`, `Evidence/windows-runtime.json`. |
| Actual builds | **PASS.** Windows and WebGL builds succeeded with zero errors. Windows player started, rendered and passed its animated-skin check with exit code 0. Browser player rendered and passed animation, pause/resume and rotation checks. | Evidence: `Evidence/build-StandaloneWindows64.json`, `Evidence/build-WebGL.json`, `Evidence/windows-runtime.json`, `Evidence/webgl-runtime.json`, `Evidence/webgl-browser-verification.json`. |
| Runtime visuals and performance | Simple lit court and robot render correctly in Editor, Windows and browser. Production frame-time measurement remains open. Browser reports an unsupported FSR upscaling shader and skipped post-processing passes. | Measure an initial crowd/lighting scene and verify the intended browser post-processing before committing to detail and shadow settings. |
| Generated character pipeline | Meshy authentication works; reference sheets, model generation, download/import and deformation quality are untested. | After the free pipeline checks pass, generate one candidate from a coherent reference sheet, import it and test a representative tennis pose. Track every charge against the proposed initial 400-credit cap. |

**The first four checks are complete and passed without spending Meshy credits.** See `PREFLIGHT_RESULTS.md` for exact evidence, fixes, warnings and runnable outputs. WebGL uses a normal build because the development build exposed a missing Unity TLS symbol. The first successful browser build took approximately 53 minutes on this machine; faster incremental builds have not been measured.

## Production decisions and content gaps

- **Reference coverage:** Full transcript plus sampled images across the whole video are available. Continuous motion/audio review and live-reference gameplay testing are incomplete. The published game previously failed with an SSL protocol error. Do not claim exact matching of timing, physics or sound.
- **Visual references:** Final robot turnarounds, court dimensions, camera matching, color/material targets and UI reference sheets need to be assembled before asset generation. The video provides enough guidance to begin; it does not supply final editable assets.
- **Gameplay specification:** Exact keyboard bindings, hit windows, CPU difficulty, ball speed and match rules are not fully established. Initial defaults can be single-player versus CPU, keyboard controls, arcade aiming and configurable short matches. These are implementation choices, not verified original behavior.
- **Output priority:** Windows-first is a practical default; retain browser delivery as a separate build gate. Mobile/touch and online multiplayer have not been requested.
- **Audio:** The workspace `.env` contains a Meshy key, not an ElevenLabs key. No equivalent music/SFX generation route has been tested. The original soundtrack cannot be recovered from the provided assets. Locally synthesized effects or appropriately licensed replacements are viable options; another paid service is not a prerequisite for the first playable version.
- **2D imagery:** An image-generation tool is available in this session, but reference/UI generation has not yet been tested for this project. Code-drawn UI is also possible. No additional key is currently established as necessary.
- **Project hygiene:** Local Git initialized. `.gitignore` excludes `.env`, downloaded installers/reference media, generated caches, build outputs, local logs and the Pipeline token descriptor. No commit or publication.

## Recommended execution order

1. **Complete:** Create the Unity project and establish project hygiene.
2. **Complete:** Connect automation and prove script compilation plus screenshot capture.
3. **Complete:** Import a small animated Blender fixture and verify playback.
4. **Complete:** Build/run a minimal Windows player and validate WebGL separately.
5. Assemble matching visual reference sheets and generate one robot candidate.
6. Develop one complete rally with tuned animation/contact timing.
7. Expand crowds, lighting, UI and sound while measuring performance.

There is no currently identified need for more creator materials or another paid subscription to start these checks.
