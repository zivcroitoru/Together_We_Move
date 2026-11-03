# Flexible completion in both levels

Both existing levels keep their art, controls, tutorial targets and real mechanics. Level 1 scores its one bridge; Level 2 scores all three bridges and retains each pressure plate's Blue/Red restriction. Existing permanent levers still latch normally. No new levels or dependencies were added.

## Assistance

GameManager's **LevelAssistance** component is configured separately in each scene:

- **Highlight After = 10 seconds:** reuse the visual hint planner's correct pressure plate, bridge or star and matching control. During onboarding retain its current control targets instead of revealing the objective early.
- **Hand After = 20:** demonstrate that action with the shared animated hand. Before this threshold automatic hints show rings/arrows without hands. A deliberate lightbulb request can show the hand immediately. Tutorial targets remain visible from the start; their automatic hand timing now follows these level thresholds.
- **Ease After = 30:** enable **Bridge Release Grace = 8 seconds**. A correctly activated plate can be released while the bridge stays open briefly. Player color restrictions, collider detection and lever rules are preserved. Actual use is recorded only when the release grace supplies an otherwise closed bridge; merely enabling it costs no score.
- **Finish After = 45:** repeat visual guidance, then after **Final Visual Seconds = 1.5** finish with assistance if still unresolved. This presents the normal positive results screen, replay and Next. It does not move characters, fabricate goal arrivals, claim unperformed control learning, or add cooperative credits.

Only the FIRST occurrence of a milestone in the run resets the timer: each player's observed movement/jump participation, each player's valid plate activation, lever activation, unique bridge/helper crossing credit, genuine star arrival, and tutorial action learned. Movement after first participation, random presses, hint requests and cycling the same plate do not keep postponing help. Progress resets the staged timer; an enabled forgiving bridge window remains available for the rest of the run. Falls retain the run's seen milestones, timing and assistance history so repeated reloads cannot prevent fallback. Replay starts fresh; entering a new Play/app session clears assistance sessions.

Timers use unscaled time. Configured thresholds are forced into increasing order. State is scoped to anonymous pair + scene. Completion time measures the level run, including fall reloads, until results; replay starts a new run. Historical event rows and attempt numbers remain separate.

## Score calculation

**Score = 100 × (P × participation fraction + C × cooperative fraction + G × goal fraction) / (P+C+G) × earned-credit multiplier.**

Defaults: **Participation Weight 20, Cooperation Weight 60, Goal Weight 20**. These fields are independently configurable per scene. No speed bonus exists. All-zero weights safely produce zero and still allow Next.

- Participation fraction: distinct children observed successfully moving at least .1 world units or jumping, legitimately operating a scored crossing, or genuinely reaching the star, divided by two. An unidentified/shared input is never assigned to a child. Blue/Red slots identify controllers, not physical hands.
- Cooperative fraction: unique actual bridge/helper credits divided by **2 × bridges**. The crossing observer requires a different child helping, contact with the active bridge, and safe arrival on the opposite bank. Repeated presses/crossings cannot farm credit. Level 1 has two possible helper credits; Level 2 has six.
- Goal fraction: actual distinct star arrivals divided by two.
- Earned-credit multiplier: 1 for independent mechanics; **Eased Credit Multiplier .8** after the release grace actually supplies the bridge; **Automatic Credit Multiplier .45** for automatic finish. These are adjustable per level. Visual hints alone do not reduce earned credit. Scores always remain within 0–100.

Both children earning all helper and goal credits independently yields 100. Partial scores reflect the missing actions, rather than a fixed timer category. For example, one helper credit and both real goal arrivals on Level 1 yields 70 with the defaults. A completely inactive automatic finish yields 0 but receives the same positive celebration and Next button. The suggested 75/45 examples are not forcibly assigned: observed behavior determines the actual value.

One genuine star arrival still qualifies after the existing GameManager **Partial Completion Grace Seconds = 6**; two arrivals finish immediately. During initial onboarding ordinary completion waits for learned controls; automatic assistance can end an unresolved tutorial without falsely marking those controls learned. No score threshold blocks Next. The last level returns to the first, as before.

## Results and analytics

The existing animated bar uses the new score. Celebration stars, character feedback and the two player-colored replay icons remain; the single green Next arrow always works. Two replay buttons are retained so each child can request a replay independently. No new child-facing text or numbers are added.

**F8** shows per-level results above the existing tutorial metrics: score, duration, outcome, both-player participation, manual hints by player, automatic visual stages, mechanical help, automatic finish and per-child replay counts. Prior results and best-score comparisons now include assisted completions.

JSONL and CSV add `completion_seconds`, `participant_mask` (1 Blue, 2 Red, 3 both), `assistance_stage`, `assistance_used` (mechanical help or auto finish), and `automatic_completion`. Automatic visual help is recorded separately as `automatic_highlight`, `automatic_demonstration`, and `final_visual_assistance`. Further events: `assistance_eased`, `mechanical_assistance_used`, `automatic_completion`, and `scoring_config` with the active weights, multipliers, grace and thresholds. `attempt_finished.outcome` is full, partial or assisted. Existing pair, level, attempt, player, hints/replay counters, score and UTC fields remain. No personal data is collected. Event schema additions are backward compatible with existing journals.

## Verification

Standalone scoring tests cover both one- and three-bridge denominators, independent 100, partial credit, mechanical/automatic discounts, no idle credit, duplicate protection, zero weights and CSV columns. Existing persistence, per-child replay and tutorial regressions remain.

**Tools > Playtest > Test flexible completion in both levels** runs native Unity controller/ledger fixtures for both scenes, with shortened test-only thresholds: full, partial, actual bridge release-window activation, automatic finish with no fabricated goals, zero-action completion, both-player replay/reload, Next availability, dashboard data and CSV. Fixture cooperative credits and star registrations are explicitly injected for deterministic controller tests; they are not evidence of a full physical playthrough. Test data is isolated under `Logs/FlexibleCompletion/Data`. It refuses unsaved scenes/active Play Mode and restores the original scene afterward.

Before children play, manually verify on BOTH levels:

1. Cooperate independently through all bridges and the star; expect maximum credit when both helper slots and both goals were earned. Replay and check each child's separate count.
2. Complete with one child at the star; expect a positive result and Next after the normal grace, regardless of score.
3. Stay idle: rings/arrows at 10s, hands at 20s, easier bridge release at 30s, final demonstration at 45s and results after 1.5s. Move randomly or repeat a plate: the timer must not restart. A genuinely new activation/crossing must restart it.
4. After easing, activate and leave a plate. Check the bridge stays up for eight seconds, keeps color rules and clears after expiry. A permanent lever or another held plate must not be counted as using grace unnecessarily.
5. Fall repeatedly without new progress: fallback must still arrive. Replay must start clean. Check both lightbulbs, simultaneous touch, tutorial completion, input locks and scene transitions.
6. Inspect F8 and exported CSV for both levels, participation, distinct independent/assisted outcomes, exact score and per-child hints/replays. Check the actual Android/WebGL playtest device for rendering, touch and persistence.
