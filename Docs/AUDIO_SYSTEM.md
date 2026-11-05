# Together We Move audio system

The system starts automatically in both gameplay scenes. `Assets/SharedGameAudio` is the independent reusable module; `Assets/Scripts/Audio/TWMGameAudio.cs` connects it to TWM. Resources contain configured manager/settings prefabs, a mixer and the ScriptableObject sound library. Audio references are already assigned.

The adapter observes each player's successful jumps, actual grounded transitions and horizontal movement. It observes plate occupancy, colored lever state and bridge surface activation, and listens to CooperationTracker milestones. Audio-only hooks in GameManager's existing event journal connect tutorial progress, accepted hints, recovery and retries. GameplayFeedback sends its real confetti bursts to the sound manager. The results observer follows the existing score bar and celebration indicators. Gameplay rules, physics and control decisions are unchanged by the audio integration.

Music selections are in the sound library. The menu cue is available for a future menu; no menu scene currently exists. Level 1 and Level 2 select their own tracks and crossfade through the persistent manager. Completion crossfades to a non-looping victory jingle on the Music channel. Retry sounds survive scene reload because their source pool persists.

The blue Audio button opens a cream settings card using the existing UI palette and a self-contained copy of the Kenney panel. Music and Sounds have independent sliders and mute toggles, plus Mute all and Done buttons. Closing saves preferences; focus loss/pause also saves them. The overlay respects the screen safe area and does not pause or change gameplay.

The manager uses two music sources and twelve effect sources, all routed through the mixer. Cue cooldowns, voice caps, low default gains and subtle pitch variation control repetition. Player-specific cooldowns allow simultaneous feedback. Important cues may replace less important voices when the pool is full. The audio service uses its own random generator so choosing sound variants does not consume gameplay's random sequence.

Music streams; short effects are preloaded mono ADPCM with decompression on load. Import settings include iOS overrides at 32 kHz. Device profiling and listening are still required before treating this as a finished iPad mix.

The reusable export is `Exports/SharedGameAudio.unitypackage`. It contains no TWM gameplay scripts, SharedUI scripts, Netcode or TMP dependency. Its own README explains use in Two Minds One Hero. All included audio is CC0. Pixabay downloads returned HTTP 403 here, so no Pixabay recording is included. Future Pixabay music belongs outside the shared folder with TWM-only references. See [audio credits](../AUDIO_CREDITS.md).

There are no pushable boxes in the existing levels. `PushAudio` is supplied and automatically attached to existing dynamic bodies named box/crate when present; it only observes side contacts and movement. No new puzzle objects were added.

Rebuild/export with Tools > Shared Game Audio > Build and export. Run the scene checks with Tools > Shared Game Audio > Test both TWM levels. The checks isolate the playtest journal and restore audio preferences. Do not edit scripts during a Play Mode test session. Package/file audits run with `python Tools/Tests/AudioPackageAudit.py`.
