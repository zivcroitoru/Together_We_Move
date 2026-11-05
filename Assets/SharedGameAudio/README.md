# Shared Game Audio

Unity 6.3 and uGUI. This assembly has no TWM, SharedUI, Netcode, TMP or Input System dependency. All included recordings and Panel artwork are CC0; see AUDIO_CREDITS.md and Licenses/AudioManifest.json.

Import SharedGameAudio.unitypackage into Two Minds One Hero. The Resources AudioManager prefab starts before the first scene and persists across scenes. Keep the host camera's AudioListener and the host EventSystem/input module.

Add SceneMusic to a scene object and assign a clip to crossfade music on Start. Call `SharedGameAudio.AudioManager.PlaySound(SharedGameAudio.SoundId.Jump, playerId)` for effects. Player IDs 1 and 2 have independent cooldowns. Omit the ID for shared UI/world cues. Call `AudioManager.Instance.PlayMusic(clip)` for a loop, `PlayMusic(clip, .25f, false)` for a music jingle, or pass null to fade out.

Instantiate Resources/SharedGameAudio/AudioSettings.prefab once, or drag it into your UI scene. It provides safe-area layout, two sliders, individual channel mutes and a master mute toggle. It uses the host EventSystem. Add AudioButton to other buttons, and PushAudio to existing movable Rigidbody2D boxes to observe side-contact pushing without changing physics.

Two music sources and twelve pooled effect sources route through Music/SFX mixer groups. Exposed parameters are MusicVolume and SFXVolume. Preferences use SharedGameAudio.v1.* keys and save after a short debounce, on panel close, focus loss, pause or destruction. Channel mute preserves slider volumes. Mute-all toggles both channel mutes. Give each game its own PlayerSettings product name for separate preference stores.

Music streams as 32 kHz Vorbis. Effects are mono 32 kHz ADPCM, preloaded and decompressed on load, with iOS overrides. Per-cue concurrency/cooldowns bound overlap. Higher-priority cues can replace less important voices in a full pool. The manager does not allocate objects each frame.

Tools > Shared Game Audio > Build and export recreates the library/prefabs and Exports/SharedGameAudio.unitypackage. Edit builder defaults before rebuilding if you want to retain custom tuning. The builder includes only Assets/SharedGameAudio and rejects external asset dependencies. TWM's scene adapter and editor tests are intentionally outside the package.

TWM has two gameplay scenes and no main-menu scene or pushable box. The welcome recording, SceneMusic and PushAudio are ready for those features. No menu, puzzle objects, gameplay rules or level layouts were added. Both existing levels bind audio automatically after gameplay/UI initialization.
