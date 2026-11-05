# Visual juice integration — 5 November 2026

Implemented in both existing levels through the scene-load TWMJuiceAdapter; no scene layout migration is required. SharedGameJuice contains independent reusable components, eight prefabs, original CC0 paper art and Kenney CC0 sparkles/glow. Export: `Exports/SharedGameJuice.unitypackage`.

## Validation

Unity 6000.3.21f1 compiled the final changes. Both levels passed the opt-in Play Mode integration fixture (`Tools > Shared Game Juice > Verify both levels`):

- Adapter binds after scene load and again after loading the other level.
- Scene objects have no missing scripts; plate visuals, bridge collider observations and score fill references resolve.
- Pressure-plate activation emits once while held, ignores duplicate contact entries and re-arms after release. Lever activation is latched.
- Press feedback leaves physics root position and scale unchanged.
- Bridge collider remains disabled during the existing build, enables after completion, and disables immediately on hide. Solid bridge sprites are opaque; hidden bridge decks leave no solid-looking ghosts.
- Existing split-star pops, goal sparkle feedback, finite victory paper and actual results reveal execute.
- Score easing reaches its target in .5 seconds; earned-score sparkles trigger.
- Particle emitters remain within the eight-emitter cap and stop; paper images expire.
- No errors or exceptions were captured during these fixtures. Screenshots of victory and results were inspected for both levels.

The exported package was inspected for current source content, missing prefab scripts and external asset dependencies: 28 assets, eight prefabs, all asset paths inside SharedGameJuice. It excludes TWM gameplay, SharedUI, audio and player jump implementation.

Fixtures invoke the real trigger handlers and bridge methods with physical simulation disabled for player actors; they are not a substitute for touch playtesting. Physical iPad 9 frame-time/fill-rate profiling remains a device check. Effects use small textures, bounded pools, shared unlit materials and no particle collisions, lights or postprocessing.

Detailed fixture output: `Logs/Juice/results.txt`. Captures: `Logs/Juice/level-1-victory.png`, `level-2-victory.png`, `level-1-score.png`, `level-2-score.png`.

Audio, jump-animation implementation, physics settings, controls, layouts and tutorial logic were not edited by this task. Concurrent changes in those areas were preserved.
