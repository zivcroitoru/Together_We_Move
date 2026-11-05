# Together We Move audio credits

Integrated 5 November 2026. All 21 newly integrated audio files are real downloaded recordings/compositions, released under CC0 1.0. No runtime synthesis is used. CC0 permits reuse and redistribution. File-level source URLs, original names, creators, processing and SHA-256 hashes are in `Assets/SharedGameAudio/Licenses/AudioManifest.json`.

## Music

| Role | Work / creator | Primary source | License |
| --- | --- | --- | --- |
| Welcome/menu | Where was I? — yd | https://opengameart.org/content/where-was-i | CC0 1.0 |
| Level 1 | Happy Beat — burabotti | https://opengameart.org/content/happy-beat | CC0 1.0 |
| Level 2 | Party and Gameover loop, happy theme only — gilzoide | https://opengameart.org/content/party-and-gameover-loop | CC0 1.0 |
| Victory | jingles_PIZZI10 — Kenney | https://kenney.nl/assets/music-jingles | CC0 1.0 |

Each OpenGameArt work page identifies CC0; files were downloaded directly from its file links. No sad/gameover recording is included. Music is mastered toward -24 LUFS / -6 dBTP at 32 kHz. Unity loops imported samples. Musical seam and final balance listening on the actual iPad remain human acceptance checks.

## Sound effects

All by Kenney (https://kenney.nl), verified from each official pack page and the license inside its downloaded ZIP. Original license files are included in `Assets/SharedGameAudio/Licenses`.

| Pack | Original recordings included | Uses |
| --- | --- | --- |
| [Interface Sounds](https://kenney.nl/assets/interface-sounds) | click_001, switch_002, toggle_001, confirmation_001, question_001, pluck_001, maximize_003, minimize_003, tick_001, drop_001 | UI, plates, switches, bridges, goals, tutorial, hints, retry, score, stars, confetti |
| [Impact Sounds](https://kenney.nl/assets/impact-sounds) | footstep_carpet_000, footstep_carpet_001, impactSoft_medium_000, impactWood_light_000 | Movement, landing, pushing |
| [Digital Audio](https://kenney.nl/assets/digital-audio) | phaseJump1, phaseJump2 | Jump variants |
| [Music Jingles](https://kenney.nl/assets/music-jingles) | jingles_PIZZI07, jingles_PIZZI10 | Cooperation and victory |

Effects are converted to mono 32 kHz WAV with a peak ceiling, then imported as preloaded ADPCM with decompression on load. Converted samples remain CC0. Subtle runtime pitch variation affects short effects, not music or long jingles.

The panel artwork is a copy of the project's existing Kenney UI Panel, with its CC0 license copied alongside it to remove the SharedUI dependency. Unity's built-in LegacyRuntime font is referenced as a built-in resource; no font file is redistributed.

## Pixabay and export boundary

The project owner authorized Pixabay use within TWM. A suitable candidate was [Ukulele Bells Playful Fun Happy Marimba Uplifting Music by REDproductions](https://pixabay.com/music/acoustic-group-ukulele-bells-playful-fun-happy-marimba-uplifting-music-185974/). Direct download access returned HTTP 403 here. It was not downloaded, included or claimed as delivered; actual CC0 music fills every role.

[Pixabay's Content License](https://pixabay.com/service/license-summary/) restricts standalone redistribution. Future Pixabay tracks must stay outside `Assets/SharedGameAudio`, for example in `Assets/TWM/Audio/Music`, with TWM-only cue/library references and their source/license records. The export builder includes only the shared folder and rejects dependencies outside it.

These credits cover the new shared audio, not unrelated existing audio in `Assets/Audio`. Those old recordings are not in the export. [CC0 1.0 legal text](https://creativecommons.org/publicdomain/zero/1.0/).
