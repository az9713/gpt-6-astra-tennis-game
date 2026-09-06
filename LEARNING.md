# Robo Open v0.5 — Practice memory

## Play and review

The initial Mint mode is **Coach**. Choose **Mint: Coach / Competitive** on the welcome panel before a match. Coach seeks useful return practice; Competitive seeks more difficult placements. Neither mode changes movement speed, reach, contact tolerance or ball physics. The chosen policy stays fixed during a match.

**F8 and Ctrl+F8 show a report inside the game**, in the left margin, and pause active play. No browser is required to see the session counts, practice suggestion and current Mint policy. **Back to Court** resumes play. In **v0.5.1**, **Open Full HTML** displays the full report at a temporary, local-only HTTP address served by the game. Keep the game running while using or refreshing that page. It includes comparable history, both players' diagnostics and sampled court replays. Nothing is uploaded. `OPEN_PLAY_REPORT.cmd` can also display the latest saved HTML after the game closes, using a temporary local viewer that expires after 30 minutes.

The browser error recurred even though a nonempty report existed and Windows received an actual filename. Its exact underlying browser cause was not established. The v0.5.1 fix stops using file URLs for full-report viewing. It serves only the generated HTML snapshot on 127.0.0.1 at an automatically selected port, behind a random route. It cannot list directories, read arbitrary files or change game data. Saved HTML exports remain available, and errors still appear inside the game. Browser verification uses synthetic observations, never the private report that browser tools refused to access.

## Practice in twelve-ball blocks

From **Practice & Learning**, choose:

- **Timing:** twelve identical central feeds. Practice releasing and tapping Space at the right time.
- **Position:** alternating left and right feeds. Practice moving behind the bounce before swinging.
- **Fixed Benchmark:** the same twelve-ball sequence on every attempt, independent of Mint's learned policy.

Each feed resets the player to the same starting position. The ball starts from a fixed point on Mint's side, like a ball machine; it is not a newly authored robot stroke. The outcome is a successful racket return contact, not a winning outgoing shot. These drills do not yet measure recovery between successive shots or aiming quality.

Use **P** for normal, half or quarter speed. Changing speed anywhere during a drill marks it as mixed; it will not be used as a comparable completed attempt. Leaving or restarting marks an unfinished drill as interrupted. **Play Again** after completing a drill repeats that same drill. Completed attempts remember the suggested drill, chosen drill, speed, eligible outcomes and result. The report compares matching attempts without asserting that an intervention caused improvement. Twelve feeds are a small sample.

## What is remembered

The application creates one random local profile. Its SQLite database stores:

- Lifetime return counts and diagnostic causes, separated by player, speed, task, mode, rules version and Mint policy.
- Per-session aggregates and a transactional checkpoint, so repeated saves cannot double-count an exchange.
- Drill attempts, including interruptions and mixed speeds.
- Balanced-policy action observations, training/evaluation partitions, promoted policies and player-requested resets.

The Windows 10/11 system SQLite library is used directly; there is no extra download, account, LLM or external network service. Full-report viewing uses a loopback connection on this computer. The native persistence layer is Windows-specific. It would need a different library binding for a future macOS, Linux or browser build.

Memory is stored in the game's local application-data **Learning** folder, outside diagnostic-session pruning. The full report displays the latest 60 session summaries and 40 drill records; lifetime aggregates and older records remain in the database. Close the game before backing up the Learning folder. To start a fresh profile, back it up and move the folder aside. No reset button silently deletes records.

Existing v0.4 recordings remain available, but are not silently imported as comparable v0.5 training data: they lack the new mode, policy and drill context. Your new learning profile starts with the new version.

## Evidence quality

Successful and missed incoming flights are sampled every 0.1 **game** seconds, up to 240 samples plus the outcome. This preserves useful approach context at quarter speed. Raw traces record action choices, probabilities, policy and match seed, as well as the existing inputs, reach checks, trajectories and animation state. The trace is still sampled evidence, not a deterministic replay or complete record of human intent.

Mixed-speed shots, focus interruptions, engine/contact failures, scripted errors and incomplete exchanges are excluded from skill rates. Outcomes without an observed contact window remain ambiguous between positioning and ball difficulty. No recorded input is not proof that the human deliberately chose not to swing.

Reports show observed sample sizes and approximate Wilson intervals. Shots from the same session can be correlated; these intervals may understate uncertainty. Do not infer normal-speed improvement from quarter-speed results or causal benefits from a single before/after drill comparison. Synthetic input and autoplay validation use isolated evidence directories and never train the human profile.

Raw session files retain the existing 20-session / approximately 100 MiB budget and 8 MiB stream caps. The latest explicit HTML export is kept separately as `Diagnostics/Reports/latest.html`; ordinary session HTML is also saved. Private gameplay memory is not committed to the public repository.

## How Mint changes

Mint begins with equal probabilities for three bounded rally choices: left placement, right placement and a central lob. Forced smashes and serves are excluded from learning those choices. Human matches use a recorded random seed; fixed drills remain deterministic. The old deliberate CPU overhit is retained only in the automated validation scenario, not in human Coach or Competitive matches.

Only eligible outcomes from balanced human matches enter the first policy learner. Sessions are deterministically assigned to separate training and evaluation partitions. Evaluation uses the most recent 24 balanced sessions for that mode and speed, so very old play does not dominate forever. Before any promotion, **each action needs at least 24 eligible outcomes in each partition, with observations from at least three sessions in each partition**.

The training partition selects a candidate. A separate evaluation partition can reject it, but cannot select a different action. Coach prefers a return rate nearer a declared 70% practice target. Competitive prefers lower human return completion, a proxy for harder placement rather than proven match-winning strategy. An interval-based screen and a three-percentage-point margin must support the choice. Candidate placements also have automated court/ballistic constraint checks.

A promoted policy gives the selected action 60% probability and the others 20% each. Physical abilities remain fixed. The update is applied only at the next match boundary and saved with its evidence and reason. Sparse or contradictory observations keep Mint balanced.

**Reset Mint to Balanced** restores and holds the baseline for the selected mode and speed. **Enable Adaptation** explicitly allows evidence to be evaluated again at the next match. Every fourth match after promotion is explicitly labeled CALIBRATION and uses balanced choices to gather fresh evidence. The saved learned policy is retained for other matches. Fresh, sufficiently sampled evaluation evidence can replace the preference or automatically restore balanced play at a match boundary. This is a return-difficulty screening loop, not continuous regression detection or long-horizon reinforcement learning.

## Remaining boundaries

This is bounded statistical adaptation, not a neural player or learned motion controller. Mint still reads exact game state. The profile does not yet estimate every skill dimension discussed in the plan, such as recovery and tactical shot selection. Guidance is evidence-based but rule-generated. Automatic drill assignment, medical/biomechanical inference and cloud coaching are absent. Advanced machine learning remains an explicit later evaluation gate.

The original Blender rig, animations, Meshy assets and development journeys are unchanged. No additional Meshy credits were used.

## Verification

See the [verification receipt](docs/evidence/learning/verification.json), [learning checks](docs/evidence/learning/learning-tests.json), [standalone input checks](docs/evidence/learning/input-tests.json) and [rally check](docs/evidence/learning/runtime.json). These are functional and synthetic-evidence tests, not evidence that a real player has improved or that a live Mint policy has already been promoted.
