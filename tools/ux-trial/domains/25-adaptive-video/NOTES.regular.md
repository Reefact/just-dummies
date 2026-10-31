# Notes — regular developer, 25-adaptive-video

## What I did
- Created `Tests.Regular`, restored, added an `any.json` (`output ./Generators`, `entryPoint static:Arbitrary`,
  generator namespace), then scaffolded all nine constructible types in one `any generate` call.
- Refined every `AnyValid…()` base, rewrote `AnyEncodingVariant` around the id/height link and wrote
  `AnyBitrateLadder.Custom.cs` (`WithBitrates(params int[])` plus an `IAny<IEnumerable<EncodingVariant>>`
  that keeps bitrates distinct and heights rising).
- 33 test methods (37 cases with theories) in 4 classes: value objects, ladder, buffer, and the
  feedback loop in `PlaybackSession` (startup one-rung cap, prediction, switch down, one-rung climb at
  comfort, hold below comfort, panic, no panic while warming up, stalls, refusals). All `[Reproducible]`.
- `dotnet test --project Tests.Regular`: 37/37 pass; re-ran the suite 30 more times, no failure.

## Frictions and surprises, in order
1. **Range guards not read.** 13 of 15 primitive parameters came back `to verify, unread guards`. Only
   `index < 0` and `sampleCount < 1` were read. Plain two-sided guards were not:
   `if (width < 128 || width > 7680)`, `if (kbps < MinKbps || kbps > MaxKbps)`,
   `if (double.IsNaN(x) || x <= 0 || x > 1_000_000)`. These are the most common guard shape in a domain
   model, so almost every factory needed a manual edit. The TODO line naming the guard and its line made
   that edit quick, though.
2. **Correlated parameters break the one-generator-per-parameter shape.** The `EncodingVariant` id must
   repeat the height (`"720p30"` with height 720). I replaced the `id` parameter with a frame-rate draw,
   dropped `WithId` and rewrote `Generate()`. `--force` would throw that work away. The docs warn about
   this ("When a guard ties two parameters"). For level <= capacity and comfort > panic I picked disjoint
   ranges (level 0..4 and capacity 4..300, panic 0..10 and comfort (10, 30]), which works without
   touching `Generate()`.
3. **Ladder = a collection with rules between its elements** (distinct bitrates, height rising with
   bitrate). The scaffolded `Any.SequenceOf(new AnyEncodingVariant())` cannot express that, so I wrote a
   ~30-line `IAny<>` by hand. Once I retyped the factory to `IAny<…>`, I deleted the `Func<AnySequence…>`
   overload as the CLI page says.
4. **`Arbitrary` became unreachable (my own mistake, but the docs led me there).** I put the generators
   and the root in `AdaptiveVideo.Tests.Regular.Arbitrary`. In tests in `AdaptiveVideo.Tests.Regular`,
   `Arbitrary.Bitrate()` then binds to the namespace, not the class:
   `error CS0234: The type or namespace name 'Bitrate' does not exist in the namespace 'AdaptiveVideo.Tests.Regular.Arbitrary'`.
   The CLI page suggests exactly this layout twice (`--entry-point-namespace Shop.Tests.Arbitrary` and
   the `any.json` sample). I renamed the namespace to `…Generators`.
5. **My own base was wrong, and the seed caught it.** I drew heights from 96, as the height guard
   allows, but the id regex `\d{3,4}` needs 100 or more. About 1 run in 10 failed with
   `FormatException: Variant id '98p30' …`. The report gave
   `Reproduce this run with [Reproducible(Seed = 1654580792)]`. Pasting that seed replayed the exact
   failure, and moving the base to 100 fixed it. This is the draw catching a bad assumption, as intended.
6. **Nested refinement can drop the outer rule.** Reading `AnyPlaybackSession`: its buffer base is
   `new AnyPlaybackBuffer().WithCapacitySeconds(c => c.GreaterThanOrEqualTo(30))`. In a test,
   `.WithBuffer(b => b.WithCapacitySeconds(c => c.LessThan(50)))` restarts from the buffer's own base
   (4..300) and loses the ">= 30" rule. The session then fails only on some draws. The docs say so
   ("a lambda guarantees where it starts"), but in a 3-level aggregate this is easy to miss. I avoided it
   by always narrowing upward (`>= 40`).
7. **A pinned mutable aggregate is shared.** `WithBuffer(new PlaybackBuffer(60, 60))` wraps one
   instance in `FixedValue`, so two `Generate()` calls share one mutable buffer. This is harmless in a
   one-shot test but surprising in a shared builder. Nothing warns about it.

## What worked really well
- The recap names the unread guard with file and line, and the build stops on a `TODO_verify_…` line.
  I always knew what was left to do.
- `With…(lambda)` composes nicely through the aggregate:
  `.WithBandwidth(b => b.WithThroughputKbps(10_000).WithSampleCount(5))`. Load-bearing numbers stay
  literals and everything else stays arbitrary.
- `static:Arbitrary` in `any.json`, then one multi-type `any generate` call, was zero ceremony.
- `[Fact, Reproducible]` and `[Theory, Reproducible]` worked as advertised: seed in the output, and
  exact replay.
- The analyzers stayed silent on idiomatic code (no noise).

## Fluency: 4 / 5
Daily flow is fast and the safety net (blocking TODOs, seed replay) is excellent. I lose a point because
plain range guards are not inferred, which turns "scaffold" into "scaffold, then hand-edit 13 factories",
and because of the `Arbitrary` namespace trap the docs set up.
