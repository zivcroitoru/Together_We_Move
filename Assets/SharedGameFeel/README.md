# Shared Game Feel

Import **SharedGameFeel.unitypackage** using Assets > Import Package > Custom Package. Tested with Unity 6000.3.21f1 and uGUI 2.0. No TWM/TMOH gameplay, scene, input, TMP, tween or networking dependency. Keep all three folders and their .meta files together: SharedGameAudio, SharedGameJuice and SharedGameFeel. Audio/VFX GUIDs, prefabs, materials, textures, mixer and SoundLibrary are preserved from TWM.

## Setup in another game

1. Keep your camera AudioListener and EventSystem. The shared AudioManager boots once before the first scene and persists. If your game has an audio service already, reconcile that service before enabling the shared bootstrap; do not run competing services. Do not add an extra AudioListener.
2. Call `SharedGameAudio.AudioManager.PlaySound(SoundId.Jump, 1)` on an **accepted** jump. Use `PlayMusic(clip)` or SceneMusic for looping/crossfading music. Tune the SoundLibrary asset for volume, cooldown, priority and concurrency. Music/SFX mixer sliders and channel mutes persist via PlayerPrefs. If two games share a product/preference store, call `AudioManager.ConfigurePreferences("MyGame.SharedAudio.v1.")` from a BeforeSplashScreen callback, before bootstrap. TMOH uses its own prefix; TWM retains its existing keys. Instantiate `Resources/SharedGameAudio/AudioSettings` once; it uses your EventSystem and safe area.
3. Add JumpPresentation, call `Initialize(yourSpriteRenderer)` once, `Takeoff()` when a jump is accepted, then `Observe(yourExistingGroundedResult, verticalVelocity)` every frame. It derives TWM's takeoff/stretch/landing/bounce timings but has no PlayerController dependency. It renders a child proxy, leaving the original sprite/Animator, proportions and physics transforms intact. Do not also run another rendering proxy on that sprite. Disable/reset on respawn or presentation cancellation.
4. Use `JuiceBurst.At(position, color, style, count)` for silent particles. Call `PaperConfetti.BurstWorld(position, 28, true)` once per victory. Caller owns the victory latch. SpriteJuice can add press feedback or a gentle goal halo; use `Initialize(renderer, accent, true, false)` to preserve an existing goal animation. ButtonPressFeedback adds a non-interactive press sheen alongside existing uGUI ColorTint. AudioButton adds click audio to ordinary buttons; accepted gameplay inputs should emit their own single cue.
5. Feed ScoreJuice your **existing animated** progress through `SetProgress(0..1)` and assign its fill. BridgePresentation observes existing collider/reveal state and never changes collision timing. Keep a host bridge animation when it already controls visuals; do not stack a second bridge motion. Ready prefabs live in SharedGameJuice/Resources/SharedGameJuice.

## Customization and budgets

Choose accent colors matching the host artwork. Jump deformation defaults to 10%, clamps at 15%, and returns exactly to the authored animated pose. All particles are optional. Eight pooled emitters, up to sixteen particles each, forty-eight reusable confetti pieces (TMOH emits 28), twelve pooled SFX voices and two music crossfade sources. Music streams; SFX use mono 32 kHz preloaded ADPCM with iOS settings. No lights, collision particles, postprocessing, frame-by-frame scene searches or tween library. Physical iPad 9 profiling and listening remain required acceptance checks.

## Licenses and export

All 21 packaged recordings are listed with CC0 licenses, source URLs and SHA-256 hashes in SharedGameAudio/Licenses/AudioManifest.json. Pack license texts and AUDIO_CREDITS.md travel with the package. No audio from either project's unrelated Audio/Resources folders is exported. Kenney particle artwork is CC0 (license included); Paper.png is an original CC0 cutout. Scripts and documentation may be reused and modified in these and future projects. Retain third-party license notices.

Use **Tools > Shared Game Feel > Validate and export combined package**. The exporter checks audio hashes, rejects unlisted audio and dependencies outside these folders, checks prefab scripts, and writes Exports/SharedGameFeel.unitypackage. It exports current tuning without rebuilding or overwriting assets. Individual legacy audio/VFX rebuild menus regenerate defaults; use them only deliberately.

## TMOH binding

TMOHGameFeel is intentionally outside this package. It binds all gameplay scenes, uses PlayerController's accepted-jump notification and existing grounding query, ButtonJuice and BridgeTilemapJuice presentation events, read-only LevelTwoPuzzle states and the existing CoopPlaytest UI/results presentation. Existing bridge collider scheduling, gate motion, sprite animations, scoring, controls and layouts remain authoritative. Existing success audio and the conflicting player Cheer scale pulse are replaced by shared cues/particles. No scene or prefab layout edits are needed.
