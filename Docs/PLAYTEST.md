> Superseded UI guide: use [the visual playtest guide](VISUAL_PLAYTEST.md) for the current non-reading interface.

# Cooperation playtest guide

Both enabled levels are wired: `Level_01_Tutorial` and `Level_02_ColoredSwitches`. No new packages are required. The existing movement, jumping, pressure plates, bridge animations, colored switch restrictions, split star and fall restart remain in use.

## Hebrew presentation for ages 4–6

Child-facing copy is now short, cheerful Hebrew: רמז, שוב!, קדימה!, and brief encouragement. Keyboard shortcuts remain active but are omitted from child-facing labels. Hint headings and the extra HUD slogan were removed. History and export/new-pair controls are hidden behind למבוגרים. The bundled Hebrew-capable Liberation Sans source generates a dynamic font atlas, with bold captions and TMP right-to-left reading order; percentages remain left-to-right. Check Hebrew wrapping and punctuation visually in the licensed Editor before the playtest.

## What changes for players

- One player reaching the star can finish the level after a 12-second grace period. Their friend can still join during that time. If their friend falls during this period, the earned partial finish is preserved. Falls before anyone qualifies still restart the level.
- Two players reaching the star finish immediately. Finishing and achieving full cooperation are separate: the score can remain below 100% even when both reach the star.
- Results remain on screen until the players choose Try Again or continue. The bar animates to the actual score, with encouraging feedback, the previous completed score and the best score for this anonymous pair and level.
- Blue and Red each have their own Hint and Try Again buttons. Existing movement pads and keys are unchanged.

| Action | Blue / player 1 | Red / player 2 |
| --- | --- | --- |
| Move / jump | A, D / W; existing Blue touch pad | Left, Right / Up; existing Red touch pad |
| Hint | Q or Blue Hint | / or Red Hint |
| Try Again on results | R or Blue Try Again | Enter or Red Try Again |

Keyboard retry shortcuts become active only when results are visible. On-screen hints work while the movement pads are held. Each player has an independent hint bubble and cooldown. After 25 seconds without a new cooperation milestone, a gentle message draws attention to Hint; it does not automatically log a request.

## Score calculation

The score uses two existing mechanics: helping a partner cross a bridge, and collecting the two star halves.

`score = 100 × earned credits / (2 × number of scored bridges + 2)`

Each bridge offers one credit for Blue helping Red cross and one for Red helping Blue cross. Each player's first arrival at the star earns one more credit. The tutorial therefore has 4 available credits (25% each); the colored-switch level has 8 (12.5% each).

A bridge assist requires the crossing player's actual collider to touch the enabled bridge, then reach the opposite bank safely. The other player's identified pressure-plate or lever support must remain available from bridge contact through arrival. A lever's credit belongs to the player who actually activated it. Walking beneath a bridge, falling past it, operating your own switch, repeated switch tapping and repeated crossings of an already credited role do not earn additional credits. A bank must be reached with the character grounded and above the bridge surface; airborne exits wait for landing. Conservative detection may omit an assist if the helper releases before that landing.

**Full cooperation = every bridge/helper role plus both star halves = 100%.** All other accepted finishes are partial. Hints, retries, elapsed time and falls do not deduct points. The visible whole-number percentage rounds down; CSV retains the exact score (for example, 12.5%). This is an observable gameplay measure, not a measure of children's conversation or feelings.

The tutorial's existing lever is inactive in the supplied scene. It stays inactive; players can help each other using the two existing plates. Lever hints are eligible only if the lever is active. The colored-switch hints refer to the nearest bridge that still has an uncredited helping role.

## Inspector setup

Select **GameManager** in either scene. All required scene references and sample hints are already assigned.

| Component / value | Default | Purpose |
| --- | --- | --- |
| GameManager / Minimum Players At Goal | 1 | Minimum number of distinct star arrivals to accept a finish. Set 2 for a stricter playtest. Arrivals latch, matching the split star. |
| GameManager / Minimum Cooperation Score | 0 | Minimum score required to finish, from 0–100. A high value can prevent partial completion; leave 0 for the forgiving setup. |
| GameManager / Partial Completion Grace Seconds | 12 | Time allowed for the second player to join before accepting a partial finish. Both arriving bypasses this delay; a qualifying partial also survives a partner's fall. |
| GameManager / Transition Delay | 0.5 | Brief celebration before results appear; it no longer advances scenes automatically. |
| CooperationTracker / Bridges | Tutorial: 1; colored: 3 | Actual bridge controllers included in the score. Each needs its existing BridgeJuice surface collider. Add new gameplay bridges here to include them. Empty arrays discover active bridges and players at runtime. |
| CooperationTracker / Players | Blue and Red | Existing PlayerController references. Each level needs exactly one with Is Player Two off and one with it on. |
| CooperationHints / Level Hints | Configured per scene | Ordered contextual hints. Set a stable anonymous ID, context, bridge/lever reference where applicable, optional player slot (0 = either), and child-friendly text. First eligible context wins; repeated requests cycle through hints for that context and bridge. Put Always hints last. |
| CooperationHints / Request Cooldown Seconds | 2 | Independent per player; duplicate requests during cooldown are ignored and are not counted as delivered hints. |
| CooperationHints / Display Seconds | 9 | How long that player's hint bubble stays visible. |
| CooperationHints / Stuck After Seconds | 25 | Time since the last new milestone/request before the Hint nudge appears. |
| CooperationHints / Fallback Hint | Configured | Used when no context matches. |
| PlaytestUI / Canvas, Font, colors | Existing canvas, Liberation Sans, Blue/Red palette | Results and hints are built at runtime in the existing canvas using its rounded UI sprite. Adjust these values for styling. |
| PlaytestUI / Score Animation Seconds | 1.25 | Bar animation duration, using unscaled time. |
| PlaytestUI / Partial Feedback, Full Feedback | Short, encouraging Hebrew copy | Editable outcome messages. Hint text is also editable per scene. |

The original WinPanel and WinSequence celebration are reused. Its invisible CanvasGroup no longer blocks gameplay controls. Existing SplitStar and WinPanel script files were renamed to match their MonoBehaviour classes, preserving their `.meta` GUIDs and scene references.

## Playtest data and export

Data is local, with no names, ages, accounts, device identifiers or network upload. Player IDs are only **1 = Blue**, **2 = Red**, **0 = shared/system/unknown**. A random anonymous pair ID separates participant pairs; a new session ID identifies each app launch. Keep the same pair when returning for another session. Use **New pair (researcher)** on results, or **Tools > Playtest > Start New Anonymous Pair** outside Play Mode, when changing participants. Old records are retained; the new pair starts with fresh attempt numbers, retry counts and comparisons.

Events are appended immediately to `Application.persistentDataPath/Playtest/events.jsonl`. On this Windows project the usual directory is `%USERPROFILE%\AppData\LocalLow\DefaultCompany\TMOH\Playtest`. Changing Unity's company/product settings changes the save location. The source of truth is the JSONL journal; CSV exports are snapshots.

Use **Save playtest data** on results or **Tools > Playtest > Export CSV** in the Editor. Native builds show the saved CSV's full path. WebGL offers a browser download and syncs the journal to browser storage. Browser persistence requires storage permission and the same browser/site profile; clearing site data removes it. Use the actual playtest browser to check both download and persistence before testing with children.

CSV columns include schema/event IDs, anonymous pair/session/attempt IDs, scene path, player ID, attempt number, event type/outcome, exact cooperation score, earned/available credits, that player's cumulative retry count for the level and pair, that player's hints used in the attempt, UTC timestamp, attribution source and milestone/hint ID. Text is quoted/escaped and spreadsheet formula prefixes are neutralized.

- `attempt_started` reserves the next persistent attempt number for this pair and level.
- `assisted_crossing` and `goal_reached` save the score as it changes; the crossing record identifies the helper and the bridge.
- `hint_requested` records the requesting slot, stable hint ID and that slot's running hint count for this attempt.
- `attempt_finished` records `partial`, `full`, `fall_restart`, `scene_exit` or `quit`. Completed-score comparisons use only `partial`/`full` results. A crash can leave a started attempt without a finish record; earlier saved events remain available.
- `retry_requested` records the finished attempt's score and increments only the requesting slot's level/pair retry count. Two requests in the same frame produce separate player records and one reload. Duplicate requests from the same slot during that reload are ignored. Fall restarts and shared Continue/New pair buttons do not count as child retry presses.
- `player_fell` and `continue` distinguish automatic restarts and shared navigation from requested retries.

Every event has a unique ID, so a write retried after an interrupted append can be deduplicated on load. Inaccessible storage shows a visible warning. Failed writes are retained in memory and retried at the next event; export also includes in-memory records. Damaged journal lines are skipped without overwriting other records. Export before closing if storage reports a failure.

**Attribution limits:** Player-specific keys and labeled buttons identify the intended player slot, not the physical child. A shared touchscreen/mouse cannot prove whose hand pressed a button; someone could press their friend's control. CSV labels the source as `player_key` or `player_slot_button`. Shared actions use player 0 and `shared_button`/`researcher`; unidentified legacy bridge activations can open the path but earn no guessed assist credit. Keep children assigned to the same color within a pair, and record any control sharing separately in observer notes.

The project currently uses local two-player keyboard/touch input. The bridge's existing NetworkBehaviour and server authority are preserved, with a local fallback when unspawned. This change does not implement remote-player input ownership, network-wide results, synchronized hint UI or multiplayer scene transitions; those were absent from the supplied game.

## Short manual checklist

1. Open both enabled scenes in turn. Confirm both movement pads, keyboard movement/jump, colored restrictions and bridge show/hide behavior work. The invisible result panel must not intercept touches. Run **Tools > Playtest > Validate** outside Play Mode to check score/save rules and scene references.
2. In the tutorial, hold the left plate while the partner crosses; then let that partner hold the far-side plate while the first crosses. Bring both to the star. Expect the animated bar to reach **100%**, positive full feedback and no automatic advance. Repeat both helping roles on each of the three colored bridges for **100%** in level 2.
3. Complete with just one player at the star. Expect a partial result after the grace period and a partially filled bar (normally **50%** in the tutorial after one assisted crossing plus one star half). Let the second player join before the grace period ends, and separately test that the waiting player's partial finish survives their partner falling.
4. Press Blue Try Again, then Red Try Again on a later attempt. Verify each reload happens once, attempt number increases, the result compares previous/best scores, and only that requesting player's retry count changes in CSV. Also press R and Enter together on results: both slots should log a request and only one restart should occur. A fall before the star must log `fall_restart`, without a retry request.
5. Request each player's Hint using its button and keyboard key, including while holding a movement button on the playtest touchscreen. Check independent bubbles/cooldowns, appropriate hints near different bridges/at the star, rotation of hints, no complete solution, and one correctly attributed CSV row per accepted hint. Wait for the stuck nudge; it must not increase hint counts by itself.
6. Tap switches repeatedly and recross an already credited role. The score must not grow. Change Minimum Players At Goal to 2 and Minimum Cooperation Score to a nonzero value to verify both constraints, then restore **1 / 0 / 12 seconds** for the forgiving setup.
7. Continue from level 1 to level 2; finish level 2 and return to the first level. Confirm results/input reset and each attempt ends once. Quit/relaunch and verify attempt numbers, retry counts and previous/best scores survive. Start a new anonymous pair and verify counters reset for that pair while old CSV rows remain.
8. Export and open the CSV. Check the expected scene, slot, attempt, score, hint ID/count, retry count and UTC time; compare two attempts. In WebGL, verify the download, reload the browser and verify persistence. Inspect the UI on the actual playtest screen size and check text fits and buttons are comfortably usable.

## Validation performed in this workspace

C# compilation against the installed Unity 6000.3.21f1 assemblies passed for desktop, WebGL and Editor code. Isolated scoring and storage/CSV regression cases passed; the isolated storage check substitutes a public-field JSON serializer for Unity's native JsonUtility. Both scene files were checked for component wiring, local references, script identities and player slots. JavaScript helper checks passed for queued saves, error callbacks and CSV download creation using a mocked browser.

Unity batch validation could not start because the installed Editor reported **“No valid Unity Editor license found”** (exit 198). Live physics, touch behavior, visuals, WebGL JavaScript integration and a player build therefore still require the manual checks above on a licensed Editor. No verified player build was produced here.
