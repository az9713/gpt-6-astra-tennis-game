# Robo Open v0.3: fairer returns and explainable play reports

The human playtest exposed a gap in v0.2: an automated player could rally successfully while the human could not reliably return Mint's ball. Inspection found a short input buffer, contact prediction that ignored an upcoming bounce, and a readiness cue that checked fewer conditions than the actual strike. The human then authorized diagnostic recording alongside a return-system improvement, with the explicit goal of explaining underperformance by either player and recommending improvements.

## Play and review

- Use WASD to move. Tap Space for each shot; release between shots. An early tap is remembered for 0.48 simulation seconds. Holding the key does not repeatedly swing.
- The cream ring suggests where to stand; yellow marks the first bounce; teal marks your aim. Continue tracking the moving ball after its bounce.
- Suitable high balls still select smash automatically. A serve cannot be struck before its first legal bounce, and a second bounce still ends the point.
- Press **F8**, or select **PLAY REPORT** in the pause/result menu. Active play pauses before the report opens in your browser.
- `OPEN_PLAY_REPORT.cmd` opens the latest report after the game closes. Reports are stored in the game's local application-data `Diagnostics` folder, not the public repository.

## Return-system changes

One shared planner now drives human input, Mint's decisions and the readiness cue. It forecasts in 1/120-second steps, checks court/net rules and follows a legal bounce. It searches a short contact horizon of approximately 0.08–0.28 seconds. It previews the actual racket-arm pose and bounded planting adjustment before advertising a feasible strike. The final contact is checked throughout a small interval around the scheduled time rather than at one isolated frame.

An early swing remains buffered until a strike is scheduled, it expires, or the point ends. It is no longer discarded just because one forecast would contact below the permitted height. The existing reach limits remain: 1.85 metres for You, 1.58 for Mint, 0.38–2.75 metres ball height and at most 0.60 metres of final racket assistance. Preview acceptance reserves 0.08 metres of margin. These are arcade design choices, not measured human biomechanical limits.

## What is recorded

Each session has an anonymous identifier, UTC start time, build/policy version and a mode label distinguishing human play from validation. The recorder contains no account identifiers, machine paths, raw keyboard text or network upload code.

| File | Contents |
|---|---|
| `session.json` | Session identity, mode and configuration at launch |
| `events.jsonl` | Swing presses/releases, movement-input changes, attempts, buffer events, eligibility changes, scheduled/accepted/rejected contacts, bounces, point outcomes, focus/pause changes and match boundaries |
| `windows.jsonl` | Completed sampled court traces around missed returns |
| `summary.json` | Both players' cumulative results, cause counts, latest 200 exchanges and latest eight replay windows |
| `report.html` | Standalone readable report, evidence table, recommendations and interactive top-down replay |

Events include simulation and elapsed real time, frame duration, ball position/velocity/height, both robot and racket positions, stroke state, predicted contacts, movement input, shot aim, bounce/score state, input-buffer status and the message displayed by the game. Context is sampled at approximately 10 Hz into a three-second rolling history; a miss retains that history and up to one further second. This is sampled evidence, not a video or a deterministic replay of every frame.

## How recommendations are derived

Recommendations are transparent rules over observed exchanges, not an external model's judgment. Each missed return receives a primary cause; raw events remain available to inspect alternatives.

| Observation | Interpretation and recommendation |
|---|---|
| Press expired before the first feasible window | Try pressing later; the event trace supports an early-input diagnosis. |
| Last press followed the last feasible window | Prepare and press earlier. |
| Key held through a window without a new effective swing | Release between shots, then tap again. |
| Reachable window but no swing attempt | Practice responding to the cue; first check focus and input events. |
| No reachable window | Review positioning and recovery. The shot may also have been unusually difficult; the recorder cannot establish which alone. |
| Broad reach but no feasible racket contact | Inspect game geometry and prediction. Do not blame player timing. |
| Scheduled strike fails to contact | Investigate planting, prediction and animation reach. |
| Mint has a window but misses it | Inspect CPU decision/scheduling behavior. |
| Shot goes out or into the net | Review aim and trajectory, with more court/net margin. |
| Mint's explicitly scripted random overhit | Identify the game difficulty setting; do not infer a learned skill deficit. |
| Focus interruption | Repeat under stable focus before judging performance. |

Return-completion percentages exclude interrupted exchanges and incoming balls that ended as the opponent's outgoing error. They are distinct from points won/lost and outgoing shot errors. The report exposes the evidence and uncertainty rather than presenting every diagnosis as certain.

## Retention, failures and limits

The recorder checks a 20-session / approximately 100 MiB retention budget at launch. It deletes only recognized recorder-owned files in old, recognized session folders; unrelated files and directory links are excluded. Each event/window stream has an approximately 8 MiB limit; reaching a limit is disclosed in the report. Current-session growth can temporarily exceed the launch-time total budget.

Events are flushed as they are written. Summaries refresh at points, match boundaries, report opening and periodically during play. A crash can lose recent in-memory replay context or leave an older summary, while previously flushed events remain. Recording failures are reported and do not intentionally stop gameplay. Existing sessions are not automatically uploaded or added to Git.

The 30 rules/diagnostic checks include bounce prediction, causal classification, preservation of uncertainty, safe retention and HTML escaping. Standalone checks include moving incoming shots with varied press times, buffered returns through a serve bounce, automatic smash, explicit contact-failure reporting and an unavailable recording location. See [verification receipts](docs/evidence/diagnostics/). These tests improve coverage; human enjoyment and difficulty still need a fresh playtest.

## Rebuild

Run Unity's `PrototypeSetup.BuildDiagnosticsWindows` method to build `Builds/RoboOpen-Windows-v0.3`. Then run:

```powershell
powershell -ExecutionPolicy Bypass -File setup/verify_prototype.ps1 -BuildDirectory Builds/RoboOpen-Windows-v0.3
```

The older v0.2 build can remain open while this separate build is prepared. No new Blender assets, Meshy requests or Meshy credits are needed for this upgrade. The original [YouTube video](https://www.youtube.com/watch?v=DQfL_l5lRpk) remains the visual inspiration; this playability and diagnostic design responds to the human's experience with the reconstruction.
