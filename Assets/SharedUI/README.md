# Together We Move shared UI

Unity 6000.3.21f1; Unity UI (uGUI) and TextMeshPro. Runtime code compiles in SharedUI.Runtime with only those two assembly references. No player, analytics, scene, localization or input-system dependency is required by the library.

## Use in Two Minds One Hero

Import Exports/TogetherWeMove-SharedUI.unitypackage. Keep its included TMP font/shader dependencies. Alternatively export Assets/SharedUI with Include Dependencies via Tools > Shared UI > Export transferable package. Do not copy only Prefabs: sprites, font and scripts are required.

Drag ResponsiveCanvas into a scene and add a single EventSystem appropriate for that game's input system. Place UI under SafeArea. Do not add a second Canvas if the destination already provides one. Reference resolution is 2048 x 1536 with a 0.5 width/height match. Anchored layouts adapt to aspect ratio; narrow portrait layouts still need a product-specific layout pass.

Prefabs live in Prefabs/Resources/SharedUI. Resources is intentional: UIFactory uses stable template names. Existing KenneyUI Resources keys are also retained. Avoid duplicate resource keys when importing into another project.

- PrimaryButton, SecondaryButton, DisabledButton: TMP label, standard Button color tint states (hover/selected/pressed/disabled). Configure Button.colors, interactable and onClick in the Inspector. Hover and pressed are states of one button, not separate prefabs.
- IconButton, Icon, CompletionIndicator: Kenney icon shapes configurable through VisualIcon. Decorative artwork does not intercept touches.
- ProgressBar, ScoreDisplay: UIProgress.SetValue or AnimateTo accepts 0..1. Configure duration, fill, optional label and star indicators. Slider uses standard Slider.onValueChanged.
- Panel, Popup, Dialog, Notification, TutorialHint, Menu, Navigation: configurable visual templates. UIPanel.Show/Hide fades with unscaled time and gates raycasts. Dialog provides confirm/cancel buttons. Connect actions in the destination game.
- ResultsScreen, HintHUD, Dashboard: extracted original compositions, with no gameplay callbacks. ResultsScreen exposes its progress and stars in the hierarchy; bind avatar art, score and navigation from the destination adapter. Dashboard retains its researcher labels as replaceable content.
- BlueTouchControls, RedTouchControls: exact original touch layouts/art. Their external player callbacks are deliberately removed in the transferable templates. Bind Jump and pointer-down/up movement from the destination game; movement must reset to zero on release.
- HintSpotlight: accepts local-space rectangular holes through Holes and Refresh. World-target lookup and tutorial timing remain in the Together We Move adapter.

Themes/TogetherWeMove.asset records the extracted design tokens. UIStyle holds a theme reference and role; use its Apply Theme context action deliberately to apply selected tokens. It does not overwrite instance overrides on enable. Edit prefab component values for persistent visual/animation overrides. Rebuilding the library regenerates base templates, so keep customizations as prefab variants.

## Existing game integration

PlaytestUI now instantiates shared panels, labels, icons, buttons and progress prefabs. It still owns hint targeting, researcher data, character lookup, retries and level navigation. Original UnityAction callbacks are attached to the instantiated Buttons. Score fill and star animation moved into UIProgress; WinSequence retains its original serialized script GUID and behavior.

Neither level scene is rewritten. Serialized touch controls, EventTrigger callbacks, WinSequence references and object IDs stay intact. Their artwork and generic scripts were relocated with .meta files, preserving GUID references. SetupOnScreenControls uses the new artwork path. The extracted full-screen compositions are reusable templates; the current game continues to assemble those compositions through its existing adapter.

## Validation

Tools > Shared UI > Validate library checks prefab scripts and Button callback isolation. Tools > Shared UI > Build library regenerates primitive templates. SharedUIExtraction.Run (project editor utility, intentionally outside the transferable library) regenerates full compositions and touch templates, exports the package, then runs existing visual, scoring/input-wiring and tutorial checks.

Logs/SharedUI/validation.txt and Logs/VisualUI/results-75.png contain generated evidence. Static/render checks do not replace a device check for simultaneous touch, hover/pressed transitions, animated timing, safe areas and all supported aspect ratios.
