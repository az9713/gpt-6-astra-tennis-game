# Robo Open v0.5 — Practice memory and bounded learning

The next version follows the agreed delivery order. Both Mint modes are included; Coach is the initial default. This is a local Windows feature. The original development journeys remain unchanged.

1. **Persistent profiles, better shot records and progress reports.** Store an anonymous local profile, comparable session summaries and successful as well as missed exchanges. Separate speed, drill, mode, game rules and Mint policy. Repair report saving/opening first.
2. **Targeted drills and measured follow-up.** Offer repeatable timing, positioning and benchmark feeds. Remember completed and interrupted attempts. Compare like conditions, report sample size and uncertainty, and do not claim that a before/after change proves a drill caused improvement.
3. **Mint's bounded adaptation, evaluation and rollback.** Coach seeks productive return practice; Competitive seeks more challenging placement. Learn only from eligible human gameplay. Freeze decisions' probability distribution during a match. Require independent evaluation evidence before promoting a different distribution. Preserve reach, movement limits, contact rules and ball physics. Expose policy history and a reset to the balanced baseline.
4. **Advanced machine learning only when justified.** This is an explicit evaluation gate, not a promise to add a neural network. First establish that bounded statistical adaptation helps. Long-horizon tactical learning, human-like perception and motion learning remain separate future work.

## Acceptance criteria

- F8 and Ctrl+F8 reach the same report handler; a complete saved report exists before opening. A failed open remains visible and does not affect gameplay.
- Memory survives application restart. Repeated saves do not double-count shots. Validation and autoplay never update the human profile.
- Raw traces remain bounded; persistent aggregates live outside diagnostic-session pruning. Database operations are transactional and have a schema version.
- Full incoming-shot traces include successes and failures at game-time sampling intervals, with an explicit cap for unusually long exchanges.
- Mixed speed, lost focus, engine contact failures and interrupted exchanges cannot silently become evidence of human underperformance or Mint improvement.
- Drills use fixed feeds and record their exact context. Incomplete attempts are labeled and excluded from completed-drill comparisons.
- Mint's policy changes only at match boundaries; sparse or contradictory evidence leaves it balanced. Learning is transparent and reversible.
- The two-bar match HUD and court margins are preserved. Learning controls belong in the menu and reports, not a central gameplay modal.
- Verify persistence, exclusions, statistical gates, rollback, report generation, real keyboard input, standalone Windows build and existing return/animation behavior.

## Scope and privacy

No new assets or Meshy credits are required. Unity implements the runtime; the Windows system SQLite library stores local gameplay memory. No cloud account, LLM, uploaded logs or personal identifiers are required. A profile identifier is random and local. Reports can be shared deliberately; raw personal play records are not included in the public repository.

## Report failure observed

The supplied screenshot shows a browser file-access error after opening a report in the diagnostics directory. Inspection found a nonempty report at that path. This establishes that a missing file alone does not explain the observation; the original browser failure has not yet been conclusively reproduced. F8 / Ctrl+F8 now shows a report inside the game, in the left margin, and saves a complete HTML export. Optional browser opening uses the operating system's file association. Verification does not claim unobserved browser success.

## Delivered scope

The first implementation of stages 1–3 is the local Windows **v0.5.0** build. Stage 4 remains the agreed evaluation gate. [LEARNING.md](LEARNING.md) explains the controls, storage, statistical gates, periodic calibration, automatic/manual rollback and remaining limits. The existing GitHub release is unchanged until a new archive is explicitly published. Original journey files are unchanged.

The **v0.5.1** follow-up repairs full HTML viewing after the browser file-access failure recurred. It serves one generated report on a temporary loopback HTTP address. Synthetic browser verification covers Chrome rendering, replay playback and the game's own browser-launch function. [Report repair verification](docs/evidence/report-fix/verification.json).
