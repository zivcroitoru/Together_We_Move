# Shared Game Juice

Portable, silent visual effects for Together We Move and Two Minds One Hero. Requires Unity 6000.3 (or compatible), Particle System and Unity UI (uGUI). No tween library, audio system, TWM code, input, tutorial, or networking dependency.

Import `Exports/SharedGameJuice.unitypackage` into TMOH. Assets are under `Assets/SharedGameJuice`; all prefab dependencies stay inside that folder (plus Unity built-ins and uGUI).

## Use

- `JuiceBurst.At(worldPosition, color, style, count)` emits a small burst. At most eight emitters, sixteen particles each; busy requests are dropped. No loops, lights, trails, physics collisions or postprocessing.
- Instantiate `PaperConfetti`, call `BurstWorld(position, 36, true)` once for victory. Call with 7 pieces for occasional cooperation. Forty-eight pooled paper images, gravity, varied spin, flutter and a 1.1–1.55 second tail. No input capture. Caller latches victory; normal bursts have a cooldown.
- `SpriteJuice.Initialize(renderer, color)` then `Press()` on the rising activation edge. A rendering proxy squashes for .3 seconds; collider transforms remain unchanged. Use `Initialize(renderer,color,true,false)` for a separate, gentle goal halo while retaining existing animations.
- Put `BridgePresentation` on the visual bridge parent and assign `surface` to the existing collider. Existing reveal/scale animations remain authoritative. Unfinished logs fade in translucently; solid logs become fully opaque exactly when the collider enables. Hide the original bridge immediately when collision ends; the observer emits shrinking, fading sparkles instead of leaving a misleading ghost deck. It never writes collision state.
- `ScoreJuice.fill` takes the existing bar fill. Call `SetProgress(0..1)` as your bar animates. Optional `stars` adds a warm earned-star accent. Existing score UI can retain its own pop animation. The TWM adapter uses a .5 second eased score fill with the existing star pulse.
- Ready prefabs: PaperConfetti, SparkleBurst, SwitchBurst, BridgeSparkles, PressFeedback, GentleGoalGlow, BridgePresentation, ScoreJuice. Configure renderer/collider/UI references for the target game's visuals.

## TWM integration

`Assets/Scripts/VisualUI/TWMJuiceAdapter.cs` automatically binds both loaded levels, including scene reloads. It observes pressure-plate occupancy and lever state, emits only on rising edges, adds goal halos, preserves existing bridge and split-star pops, observes genuine goal/cooperation milestones and enhances the results bar. This TWM-specific adapter is deliberately outside the portable package. `GameplayFeedback` retains existing victory and cooperation deduplication and sound calls, replacing its flat rectangles with the shared paper system.

No physics, controls, layout, tutorials, jump animation or audio implementation is changed. New bridge presentation only observes the pre-existing collider schedule. All effects run without changing timeScale.

## Performance / authoring

128px maximum textures; shared unlit sprite particle material; small bounded pools; no per-frame scene searches. UI pieces are allocated on first use and reused. Validate frame time and fill rate on a physical iPad 9: desktop checks do not establish device performance. Rebuild and export through **Tools > Shared Game Juice > Build and export**. TWM verification: **Tools > Shared Game Juice > Verify both levels** (uses isolated playtest storage).

## Assets and license

Sparkle.png and Glow.png: Kenney Particle Pack 1.1, CC0, https://kenney.nl/assets/particle-pack . Original license included in `Licenses/Kenney-Particle-Pack.txt`.

Paper.png: original small angled paper cutout with a shaded folded edge, created for this package; dedicated to CC0 1.0, https://creativecommons.org/publicdomain/zero/1.0/ . Scripts and documentation may be reused and modified in TWM and TMOH.
