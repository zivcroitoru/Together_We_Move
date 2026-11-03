# Visual playtest interface (ages 4+)

Both enabled levels use a child-facing interface with no text or numeric results. The previous Hebrew/TMP/UniText experiments are not used by this interface. No Hebrew image generation is needed.

## Controls and feedback

- Blue and Red each have a large lightbulb button. Their existing left/right/jump pads and keyboard controls remain unchanged.
- Results show the two character sprites, happy faces, three golden celebration stars and an animated bar. The actual cooperation percentage determines the bar fill; there is no failure message, written explanation or numeric score.
- Each player has a large replay button in their own color. The green forward arrow advances. Replays are recorded separately by player slot. Shared next-level navigation is not counted as a child's replay.
- One player reaching the star qualifies for completion. A configurable six-second grace period allows their friend to join. Both reaching the star finishes immediately; a partner's fall during the grace period preserves the partial finish. No score threshold can block progression. Falls before the star retain the existing restart mechanic.

## Visual hints

The hint planner uses the positions, allowed-player rules and actual occupancy of the current level's pressure plates and bridges. First it highlights a reachable plate and the appropriate player's movement button. Once a plate is held, a hint can highlight that plate and the partner's crossing control. After the bridges it highlights the finish star and movement/jump controls. Both provided levels travel left to right; this planner uses that route direction. For future levels with another route, extend the planner rather than reusing this direction assumption.

The entire view dims except for clear windows over the real target and relevant controls. Colored pulse rings and a gently bouncing arrow remain above the dimming. The overlay and all decorations have raycastTarget disabled, so all existing touch controls remain available. Input is not simulated: children still move their own characters. A plate hint clears when the plate is occupied; a crossing hint clears when an actual assisted crossing is recorded or the helper releases the plate; goal/progress events also clear it.

Automatic hints are optional and appear after inactivity. Position changes reset inactivity; automatic hints have their own analytics event and do not inflate player-requested hint counts. Inspector settings on CooperationHints: Automatic Hints, Stuck After Seconds (25), Request Cooldown Seconds (2). On PlaytestUI: Hint Dim Opacity (.62), Score Animation Seconds (1.25), player colors. GameManager: Partial Completion Grace Seconds (6).

## Researcher-only data

Press F8 while results are visible to reveal export and new-pair controls. This panel is hidden by default and has no child-facing menu button. Scores, attempts, per-slot replays, manually requested hints, automatic hints and timestamps continue to use the existing local JSONL journal and CSV export. The score remains 100 × earned credits / (2 × scored bridges + 2): one credit for each player helping their partner cross each bridge and one per star arrival. Hints, elapsed time and replays do not deduct points. Button colors identify the requesting player slot, not the physical child's hand; keep color assignments stable during a pair's session.

## Validation and manual checklist

Tools > Playtest > Validate and render visual UI creates Logs/VisualUI/results-75.png. It checks that child results contain no visible text, both replay buttons and next exist, researcher tools are hidden, custom icons have CanvasRenderer, decorations cannot intercept touches, and overlapping spotlight holes leave the correct clear area. Existing isolated score/save/CSV tests also remain applicable.

Before testing with children:

1. In both levels use Blue and Red touch pads simultaneously; request each lightbulb while holding movement. Confirm each bulb/arrow/replay icon is visible and easy to recognize on the actual screen.
2. Follow a plate hint, then request a crossing hint. Confirm the real plate and correct player's controls are bright, their touches still work, and the overlay clears after the actual action. Check both colors and every colored bridge.
3. Stop moving for 25 seconds: expect one gentle visual hint, not repeated flashes or counted manual requests. Turn Automatic Hints off and repeat.
4. Finish with one child at the star, then with both. Both outcomes must celebrate and offer the next arrow. Check the bar reflects saved cooperation and no score blocks progression.
5. Press each colored replay button on separate attempts, then R and Enter together on one result. Confirm per-player CSV rows and one reload. A fall restart must not add a replay request.
6. Continue between levels and return from the last level; verify hint overlays, input locks and results reset. Relaunch and confirm persistent counts. F8 should be the only route to researcher controls.
7. Open an exported CSV and confirm level, pair, slot, attempt, exact score and UTC time. Test the actual Android/WebGL build for touch, CSV download and persistence; desktop rendering alone does not verify those platforms.

## Workspace verification

The corrected results screen was rendered and visually inspected in the running Unity 6000.3.21f1 Editor. Stars, smile icons, both replay arrows and the next arrow were visible; the bar was set to 75% for the captured check. The native editor checks passed, including CanvasRenderer presence and overlapping spotlight hole area. Desktop/Editor C# compilation passed with existing Inspector-field/deprecated-API warnings. Isolated score, per-slot counts, persistence replay and CSV checks passed. Live simultaneous touch, automatic-hint timing, action-based hint dismissal, scene transitions and target-device builds still need the checklist above.
