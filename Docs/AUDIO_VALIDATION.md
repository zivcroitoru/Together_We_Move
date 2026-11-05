# Audio validation — 5 November 2026

Implemented and checked with Unity 6000.3.21f1. The two existing gameplay scenes integrate automatically; their level layouts and gameplay rules were not edited by the audio implementation.

| Check | Result |
| --- | --- |
| Downloaded audio | 21 actual audio files: 18 Kenney recordings and 3 CC0 music tracks |
| Licensing/provenance | Every new audio file has a source URL, creator, CC0 designation and verified SHA-256 in the manifest; Kenney archive licenses included |
| Unity compilation | Shared package compiled in a separate Unity 6.3 project with only uGUI and audio/physics2d/ui modules; full TWM snapshot also compiled |
| Missing references | All 19 sound IDs have valid clips; manager mixer groups and settings prefab references resolve; no missing runtime scripts found in either gameplay scene |
| Package isolation | 43 current packaged files; all package asset paths stay under Assets/SharedGameAudio; no TWM/SharedUI/Netcode/TMP asset dependency |
| Both gameplay scenes | Fresh isolated Editor run passed Level_01_Tutorial and Level_02_ColoredSwitches |
| Music | Correct track selection; both sources overlap during crossfade; old source stops; seeking near clip end wraps while playback continues; victory uses non-looping music |
| Preferences/settings | Independent mute and volume, mixer dB values, slider event binding, persistence on save and reload after recreating the service |
| Simultaneous feedback | Both player IDs can play jump feedback together; repeat from the same player is cooled down |
| Voice budget | Repeated mixed cue requests stay within the fixed pool |
| Gameplay connections | Real player Jump calls, existing plate trigger handlers, bridges, switches, accepted hints, goals, victory, stars and confetti exercised in both scenes |
| UI inspection | Settings panel captured and inspected at 2160×1620; copied Kenney artwork and all controls resolve |
| Existing regressions | TutorialRegression and PlaytestDataRegression passed |

The final scene run used a separate snapshot because another task was using the open Editor. Its journal and preference product name were isolated. An unrelated UnityEditor.Search startup indexing exception was recorded separately and excluded from gameplay error assertions; the harness continued after the Editor's Error Pause. No runtime audio exceptions were captured. Unity also emitted allocator warnings during Editor shutdown; this was not a device memory profile.

Evidence is saved in `Logs/Audio/tests.txt`, `Logs/Audio/package-smoke.txt`, `Logs/Audio/isolated-playmode-final.log`, and `Logs/Audio/Level1-settings.png` / `Level2-settings.png`. The package audit is reproducible with `python Tools/Tests/AudioPackageAudit.py`. Editor scene checks are available under Tools > Shared Game Audio > Test both TWM levels.

## Remaining device acceptance checks

- Listen on the iPad 9th generation speakers at a comfortable physical volume: compare music and effects, check musical loop seams and the brief victory jingle.
- Check two-child multitouch while opening/closing settings, slider comfort, safe areas and portrait/landscape behavior.
- Background/resume, lock/unlock and interrupt audio; check iPadOS mute/output routing and saved settings after a full application restart.
- Profile CPU, memory and audio voices on the iPad. The system has a bounded voice budget and streaming music, but desktop checks do not establish device performance.

An iPad application was not built or run here. Final iOS compilation/signing requires Xcode on macOS and deployment to the device. No claim of an on-device test is made.

## Scope and source limitations

There is no main-menu scene or pushable box in the current two levels. The welcome clip, SceneMusic and PushAudio components are supplied for those features without adding gameplay or level objects. Cooperation/tutorial/retry hooks are present; the automated scenario does not simulate an entire natural two-child playthrough or every assistance branch.

Pixabay downloads returned HTTP 403. The delivered project uses actual CC0 music in every role instead. No Pixabay audio is included in the shared export. See AUDIO_CREDITS.md for sources and the boundary to use for any future TWM-only Pixabay tracks.
