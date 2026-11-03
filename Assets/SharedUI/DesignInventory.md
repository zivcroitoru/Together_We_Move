# Design inventory

Scope: both production scenes (Level_01_Tutorial and Level_02_ColoredSwitches), runtime PlaytestUI, WinSequence, SetupOnScreenControls, VisualIcon, HintSpotlight, tutorial guidance, localization adapters, UI-related editor validators, and the isolated UniText Hebrew proof prefab. TMP example scenes are third-party demonstrations, not game UI.

The production visual language uses rounded sliced Kenney panels; blue RGB (0.16,0.55,0.86), red (0.86,0.23,0.28), dark ink (0.08,0.19,0.25), warm surface (1,0.985,0.93), green progress (0.20,0.72,0.44), gold stars (1,0.72,0.12). Researcher cards use white surfaces and narrow colored stripes. Child-facing results/hints are icon-only. Researcher typography uses TMP, 32-point heading, 28-point controls, 18–24-point summaries with autosizing; minimums vary by role (14–16).

Touch controls: 160-unit buttons, 185-unit center spacing, 120-unit edge margin, 130-unit bottom centers; opposite bottom corners. Kenney bubble artwork is untinted white. The original controls use 1.08 hover tint, 0.82 pressed tint and 0.08-second tint interpolation. Runtime visual buttons retain Unity default tint transitions (0.1 seconds); these are distinct existing styles.

Results: warm card anchored (0.10,0.19) to (0.90,0.86), three gold stars, two colored player badges and character sprites, green bar from 12% to 88% card width, blue/red replay with green next action. Progress eases out cubically over 1.25 seconds; stars pulse by 12% over the same interval. WinSequence fades in over 0.35 seconds, with optional sequential letter jumps (35 units, 0.22 seconds, 0.08-second stagger).

Hints: two top corner bulb buttons, dim overlay alpha 0.62 (up to 0.76 with emphasis), transparent target windows and yellow arrows bobbing 9 units at frequency 3. Existing tutorial targeting, repetition clocks, hand-demo policy, sounds and analytics are game logic and remain outside SharedUI. Arrow/spotlight art and rendering are reusable. UIPulse is an optional scale pulse for new hint templates; live hints retain the existing bobbing motion.

There was no standalone main/pause/settings menu, notification queue or interactive slider in the production UI. Added Menu/Navigation/Dialog/Notification/Slider templates use the extracted visual language and standard Unity events; they do not introduce new gameplay flows. UniText Hebrew proof and legacy localization adapters stay outside the TMP-based reusable library.
