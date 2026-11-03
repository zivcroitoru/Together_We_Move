# Movement-first onboarding

Level 1 starts automatically with **both left buttons → both right buttons → both jump buttons → real plate and cooperative crossing**. Each child must perform each control in the current phase. The first child's highlight disappears after success; their progress remains while the other finishes. Existing keyboard and touch input, movement, physics and two-player layout are reused. No tutorial text, input locking, automatic movement or next-step buttons are added.

The objective points Blue toward the actual left pressure plate, then keeps the plate highlighted while Red crosses its bridge. Releasing the plate redirects the guidance to its holder. The tracker must observe a real assisted crossing; the tutorial never opens the bridge or grants score. Ordinary hints resume after onboarding. A real level finish still accepts partial cooperation, after both children have learned the controls.

## Inspector

On the first level's GameManager, TutorialManager has eight configured entries. Phases 0, 1 and 2 each contain one entry per player: Move left, Move right, Jump. Their Control references point at the six existing buttons. Phases 3 and 4 use the existing plate and bridge. IDs and child/phase pairs must be unique, and phases contiguous. Bad configuration reports an error instead of silently waiting forever.

- **Move Distance**: .65 world units of actual travel in the required direction. Pressing against an obstacle does not count. Each phase snapshots fresh counters; earlier movement cannot satisfy later controls.
- **Repeat Hand After Seconds**: 5. Each guided child has their own inactivity clock. Their partner's completion or movement does not reset it.
- **Positive Sound / Feedback Volume**: existing gem sound at .18, with a brief character bounce.
- **Start Automatically**: on. **Repeat On Every Visit**: off. Normal scene reloads retain learned phases and their original times; a new app/Play session or new anonymous pair starts fresh. Repeat On Every Visit intentionally starts a fresh run each time this scene initializes, including fall reloads.
- PlaytestUI retains existing player colors and hint dim opacity. Overlay, rings, arrows and hands do not intercept pointer events. Targets follow actual transforms and RectTransforms; bottom-edge hands point down from above to stay visible.

## Developer Playtest Dashboard

Press **F8 at any time** to view the hidden dashboard and export CSV. New anonymous pair is available after level completion. This panel is never shown automatically to children.

Every event retains the existing anonymous pair, player slot, session, level, attempt, score and UTC timestamp. New fields: `tutorial_run_id`, `tutorial_action`, `tutorial_elapsed_seconds`, `tutorial_phase_seconds`. JSONL records are written immediately; CSV includes these fields. No child names or personal information are collected. Player identity follows the established Blue/Red slots; a shared keyboard or touch device cannot verify whose physical hand pressed a control.

For each player and left/right/jump, the dashboard shows:

- **First**: seconds from phase start until the first successful result. Movement requires >.015 world units of travel attributable to that direction; jump requires the existing grounded jump method to accept the input and the child to become airborne. Failed or blocked presses do not count. This is successful-action latency, not the time of any attempted press.
- **Learned**: seconds from phase start until .65 units of movement or the successful jump. This is an observable tutorial criterion, not a psychological assessment of learning.
- **Hints**: that slot's lightbulb/key requests, separately from **repeats**, which count repeated hand demonstrations. A hint-triggered demonstration has attribution `hint`; inactivity repeats have attribution `system`. The first demonstration is not a repeat. Completed children do not receive inactivity repeats.
- **Controls total**: independently recorded when that child finishes all three controls.
- **Tutorial total**: recorded for each player when the shared objective completes. Both have the same joint finish time; controls totals preserve individual completion times. A genuine level finish may also end onboarding after controls are learned.

Times use Unity unscaled time, including time awaiting the other child and scene reloads within the run. They are unaffected by timeScale. Interrupted runs remain distinguishable from completed runs. A fresh run ID separates repetitions; historical rows remain available in CSV.

## Checks

`Tools > Playtest > Validate onboarding and render hand` checks scene references, action order, existing input wiring, feedback and hand rendering. `Tools > Playtest > Test paired tutorial in Play Mode` runs existing input methods through real physics, waits after one child succeeds, checks the objective gate and performs a real assisted crossing. It refuses unsaved scenes/active Play Mode, restores the previous scene, and writes test analytics separately under `Logs/Tutorial/PlayModeData`. Captures and reports go to `Logs/Tutorial`. It does not test physical touch hit-testing or keyboard hardware.

`Tools/Tests/RunTutorialChecks.ps1` runs standalone regression tests for independent phase progress, first-input deduplication, clocks, session reset, scoring, journal reload, CSV and dashboard counts. Its Unity substitutes exist only in Tools, never in the shipped game.

Before the playtest:

1. Start a fresh Play session. Blue completes left; wait. Only Red should remain highlighted. Wrong-direction movement must not complete left. Finish Red, then repeat for right and jump using both touch and keyboard.
2. Leave one child inactive for 5 seconds while the other moves/completes. Only the idle child's hand repeats; continued inactivity gently strengthens its highlight. Press each lightbulb and check that child's hint/demo counts.
3. Follow the plate/crossing guidance. Release and reoccupy the plate. The bridge and tutorial must respond to actual gameplay, without artificial completion or loss of control.
4. Use F8 during gameplay and at results. Check per-child times/counts and exported rows. Verify a fall reload retains completed phases, and a new Play session/new pair restarts onboarding. Toggle Repeat On Every Visit to check the override.
5. Check hand visibility, arrows, audio, simultaneous touches, portrait/landscape sizing where supported, and final controls on the actual playtest device. Children still need a usability observation; automated gates do not establish that an icon is understood.

## Verified in this workspace

Runtime and Editor compilation passed. Standalone progress, timing, storage, CSV and dashboard regressions passed. The native Unity Play Mode test passed with actual left/right/jump results for both children, waiting after only one succeeds, an uncompleted objective until real plate/bridge interaction, a real assisted crossing, both completion records, and no runtime errors. Native captures for controls, cooperation and the dashboard were visually inspected. The latest report and captures are under `Logs/Tutorial`; test data is isolated from the playtest journal. Physical simultaneous touch, sound playback, other resolutions, restart behavior in live gameplay and Android/WebGL device builds remain manual checks above.

Automatic hand demonstrations now follow LevelAssistance's 20-second threshold, and eventual automatic completion can end unlearned onboarding without granting learning credits. See [FLEXIBLE_COMPLETION.md](FLEXIBLE_COMPLETION.md) for current assistance timing and scoring.
