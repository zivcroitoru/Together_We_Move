# Forgiving preschool playtest

Both original level layouts, artwork, objectives, mechanisms, and the existing end card/progress bar are retained. Per the playtest decision, Player A (Blue / slot 1) now only jumps vertically; Player B (Red / slot 2) only walks left/right, with gravity and falls still active. A's X position is frozen in physics. This also applies to keyboard and legacy UnityEvent callbacks: W jumps A; arrows move B. A/D and Up do not grant forbidden actions.

**Layout limitation accepted for this playtest:** both original routes were authored for two horizontally mobile characters. A cannot reach the distant goal under these roles. Keep the existing positive assisted finish enabled; these changes do not make the original routes independently completable. No missing objective or control action is credited as performed. A future route revision needs explicit design work.

## Configure

Select **GameManager** in either scene:

- **Preschool Touch Controls → Control Mode:** Buttons (default) or Dragging. In Play Mode, open the small grey dashboard icon at the top centre (or press F8); **Controls** switches methods immediately. The choice carries through scene changes/replays for this app session, resets for a new Play/app session, and every change is journaled. Buttons stay available in both modes. Hint requests and the existing tutorial remain available.
- **Gameplay Feedback:** optional Cheerful Sound, Volume (default .18), Minimum Interval (.65 seconds). An in-memory two-note chime is used if no clip is assigned. Bursts contain 18 small pieces, fade within .65 seconds, pool 36 pieces, queue at most two reactions, and never intercept touches. Verified assisted crossings, first goal arrivals, first control participation, and first plate/lever discoveries react once per target/player/attempt. Every finish celebrates, including zero-score assisted finishes. Positive reactions do not themselves add cooperation credit.
- **Level Assistance:** the existing 10/20/30/45 second assistance timing and bridge grace remain. Recovery Credit Multiplier defaults to .9: each assisted rescue discounts earned credit, bounded to a nonnegative score. The final multiplier is the smaller of the accumulated recovery multiplier and the existing mechanical/automatic multiplier. No child-facing penalty text or negative effect is added.

Select each character's existing **Player Death Controller**:

- Death Y retains its scene value (-12 in the existing scenes).
- Bounce Seconds defaults to .55. A visual bounce returns the artwork from above the safe location; physics stays paused during the short recovery. Only the fallen child is suspended; their partner continues playing.
- Safe Dwell Seconds defaults to .25. Stable grounded checkpoints are retained in a bounded recent-position history. Both feet must have enabled static support, the body must fit clear of ground geometry, and the position must be above the fall threshold. Moving edges and unsupported positions are excluded.
- Maximum Fall Seconds defaults to 3. A descending character is recovered even if the configured fall boundary was missed. Vanished bridge checkpoints are rejected in favour of an older safe point or the spawn point. If no safe support remains, a small persistent trampoline is created at the spawn column to prevent an endless fall loop. A short rearm interval and cleared input stop duplicate recoveries. Rescue travel never earns crossing, tutorial movement, or goal credit.

## Touch behavior

In Dragging mode, tap A's character once per jump. Drag B's character horizontally; the character walks toward the finger at the normal speed through normal collisions. A finger keeps its character until released/canceled, and each character accepts one world-control finger. Vertical dragging cannot move A sideways or make B jump. World gestures start only near a character, with a generous pixel radius, and never start over UI. Crossing UI pauses an existing drag; releasing, changing modes, losing focus, or pausing clears gesture movement. Held buttons track fingers separately. Android UI uses independent pointers.

The tutorial keeps its authored steps but skips unsupported controls without fabricating learning evidence: B learns left/right, then A learns jump. Gestural hints target the character and B's hand demonstration drags horizontally. Buttons remain the fallback. Existing unreachable world objectives still use the original assisted finish.

## Portrait and artwork

Android defaults to portrait. Runtime layout fits the original 4:3 camera width into the screen, retains world geometry and sprite scales, and repositions the three usable buttons for portrait safe-bottom insets. UI images retain Preserve Aspect. The original art therefore occupies a horizontal band in a tall display; this is intentional to preserve the level layouts. The dashboard is for adults.

## Analytics compatibility

The existing anonymous append-only JSONL journal, schema version 1, original events, original CSV column order, attempt IDs, hint/retry attribution, and dashboard are retained. New fields are appended; old records remain readable. New CSV columns:

`control_mode, player_1_falls, player_2_falls, recovery_count, cooperation_event_count, hint_request_count, retry_request_count, voluntary_replay_count`

Counts are running snapshots for that attempt. Events remain the authoritative source, including old journals with absent snapshot fields:

| Event | Meaning |
| --- | --- |
| `player_fell` | One physical fall for the identified player; does not finish/restart the attempt |
| `mechanical_assistance_used`, detail `fall_recovery` | Recovery assistance and discounted earned score |
| `player_recovered` | Completed rescue; detail `last_safe_bounce` or `emergency_trampoline` |
| `cooperation_succeeded` | A new tracker-confirmed assisted crossing, or the second real goal arrival |
| `hint_requested` | Existing accepted, rate-limited requests, including tutorial requests |
| `control_mode_selected` | Initial selection and every switch; detail `buttons` or `dragging` |
| `retry_requested` | Existing distinct child replay requests; both children can request the same reload |
| `voluntary_replay` | One actual replay choice per reload, or cycling from the last level to the first |

The level dashboard shows falls A/B, recoveries, cooperation count, control mode, retry requests and voluntary replays. Counts for retries/replays cover the pair's level history; fall/recovery/cooperation/hint counts cover the displayed attempt. Falls do not count as retries or start another attempt. Existing consumers should ignore unknown fields and read CSV by header.

## Verification

Run **Tools → Playtest → Test preschool recovery and touch in both levels** with saved scenes outside Play Mode. Results and isolated test journals go under `Logs/Preschool`; actual children's data is not used. The check runs real physics, button callbacks and two independently queued Input System touches in both scenes. It checks role restrictions, role-specific onboarding, UI exclusion, finger release, simultaneous recovery, checkpoint return, deduplication, retained attempt/credits, plate collision, hints, feedback limits and assisted finish. The legacy paired-tutorial test menu forwards to this updated check.

Run `Tools/Tests/RunTutorialChecks.ps1` for journal persistence, score calculations, CSV safety, analytics counters, tutorial state and guidance timing.

On an Android device, test both levels at 9:16 and a tall/notched portrait ratio:

1. In Buttons mode, hold B left/right while A repeatedly taps Jump. A's X stays fixed; B never jumps. Press opposite B buttons with separate fingers, then release each in turn. Pause/resume and ensure neither keeps moving.
2. Switch to Dragging using the dashboard. Tap A while another finger drags B. Swap hands/order, drag vertically, lift outside the screen, and cancel touches. Repeat with a drag plus a fallback button. Touching hints/dashboard/results must never capture a character.
3. Walk B off an unsupported bridge. Confirm a short visual bounce, stable safe return, continuing partner input, preserved mechanism state and the same attempt. Repeat falls, let the bridge close during recovery, and check the dashboard counts. For A, move only Y below Death Y in the paused Editor Inspector to test a fall without granting horizontal input.
4. Activate/discover mechanisms. Inspect brief confetti, the cheerful sound, no touch blocking, and no effect every frame while holding the switch. Reach/allow an assisted finish: the same happy result card and progress bar appear at every score.
5. Replay with each child's button, then both in one frame. Check two retry requests but one voluntary replay and one new attempt. Export CSV and reopen the app to check persistence. Listen for comfortable sound volume on the actual tablet.

Editor simulation verifies routing and physics; physical touchscreen usability, audible output, Android lifecycle and device performance still require the tablet test.

## Files changed

- Movement/recovery/artwork: `PlayerController`, `PlayerDeathController`, `PlayerView`; new `PreschoolTouchControls`, `PreschoolHoldButton`.
- Gameplay integration: `GameManager`, `CooperationTracker`, `LevelAssistance`; new `GameplayFeedback`, `PortraitPlaytestLayout`.
- Existing UI/onboarding/analytics: `PlaytestUI`, `PlaytestStore`, `LevelPlaytestDashboard`, `TutorialManager`, `TutorialProgress`, `TutorialDashboard`.
- Both scene managers receive configurable control/feedback components; geometry is unchanged. Project settings choose portrait.
- Validation: new `PreschoolPlaytestChecks`; legacy tutorial check entry point, analytics/tutorial regressions and runner updated for the roles.
