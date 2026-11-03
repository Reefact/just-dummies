# Expert notes — 25 Adaptive Video Playback Control
Agent: the same model and effort as every session of this trial. Library: JustDummies 1.0.0-uxtrial.1 (packed from main).

## What I did
**Pages read, in order:** `PROJECT-README.md`, `README.md` (index), `guides/getting-started`, `guides/core-concepts`,
`packages/justdummies-cli` (all of it: the scaffolder is where this domain lives), `packages/justdummies-xunit`,
`guides/reproducibility` (first half), `generators/README`, `generators/numbers`, `guides/composition` (first half).
I did not need strings, dates, collections or the analyzer pages, except the analyzer index (severity tuning).

**Project:** `Tests.Expert/AdaptiveVideo.Domain.Tests.Expert.csproj` from the template, plus `any.json`
(`{ "output": "./Generators", "entryPoint": "any" }`) so that every scaffold lands in `Generators/` with its `Any.X()` entry point.
I added `global.json` at the workspace root (see friction 1).

**`any` commands run** (one batch, then no re-scaffold):
`any generate Bitrate EncodingVariant Segment Latency BandwidthEstimate AdaptationPolicy AdaptationDecision BitrateLadder PlaybackBuffer PlaybackSession`
Recap: 6 types compiled as written (Bitrate, Segment, Latency, BandwidthEstimate, AdaptationPolicy, PlaybackBuffer), 4 stopped
the build on purpose (EncodingVariant, AdaptationDecision, BitrateLadder, PlaybackSession: "3 to verify" / "1 to verify" / "3 to verify").

**Generators completed by hand:**
- `AnyEncodingVariant`: the id is derived from height + frame rate (`"{height:D3}p{fps}"`), so `WithId`/`_id` were replaced by
  `WithFramesPerSecond`; height and width got `.Even()`; `AnyEncodingVariant.Custom.cs` adds `WithBitrateKbps(int)`.
- `AnyBitrateLadder`: rewritten (draws distinct bitrates, distinct `(height, fps)` shapes sorted by height, zips them;
  `WithBitratesKbps(params int[])` pins the part a ladder test is about).
- `AnyAdaptationDecision`: rewritten (reason drawn first, both variants drawn to agree with it; pins for each part).
- `AnyPlaybackSession`: the two TODO ties (comfort threshold <= capacity, segment <= capacity) are applied in `Generate()` once the
  buffer is known; `AnyAdaptationPolicy.Custom.cs` adds an internal `WithComfortBufferSecondsAtMost` that keeps a test's pin (see friction 5).
- Untouched scaffolds: Bitrate, Segment, Latency, BandwidthEstimate, AdaptationPolicy, PlaybackBuffer (the last two with their `OrderedPair`).

**Tests written** (152 methods, 311 test cases, 5 methods skipped as domain defects), `Tests/*.cs`:
- `BitrateTests` (range edges, `DownloadSeconds` arithmetic and refusals, equality/comparison/ToString)
- `EncodingVariantTests` (ids by frame rate and height, 14 malformed ids, width/height ranges, parity, landscape, id/height agreement, nulls)
- `SegmentTests`, `LatencyTests`
- `BandwidthEstimateTests` (ctor ranges, warm-up boundary, EWMA arithmetic, cap at 1 000 000, immutability, sample refusals)
- `AdaptationPolicyTests`, `AdaptationDecisionTests` (reason/move table: 8 accepted + 7 refused combinations, message content, `IsSwitch`)
- `BitrateLadderTests` (sorting, every refusal, equal-height/higher-fps accepted, `HighestWithin` boundaries, steps clamped, defensive copy)
- `PlaybackBufferTests` (ctor, `Drain` with/without stall, `Append` fit/no-fit/brim)
- `PlaybackSessionConstructionTests`, `PlaybackSessionDecisionTests` (startup cap, safety factor, prediction, warm-up vs trusted,
  panic/hold/climb thresholds at their exact boundaries, immediate multi-rung drop, refusals), `PlaybackSessionDownloadTests`
  (drain/append/stall accounting, bandwidth learning, `Idle`, a 25-segment steady-network loop)
- `GeneratorsTests` (300 draws of every generator, every decision reason, hand-written generators replay from a seed)
- Helpers: `Playback.cs` (steers a session to the state a test is about: startup done, level drained to an exact value, climbed to a rung),
  `Variants.cs`.

**Result:** green. `dotnet test --project Tests.Expert`: 306 passed, 0 failed, 5 skipped. First run green; then 25 + 12 + 12 further runs,
all green with fresh seeds (49 repeated runs). I also un-skipped the 5 skipped tests once to check each really fails for the reason written.

**Skipped on purpose (domain defects, literal inputs so no seed):**
`BitrateTests.A_NaN_duration_or_throughput_is_refused...`, `BandwidthEstimateTests.A_NaN_sample_is_refused_naming_the_argument_at_fault`,
`BandwidthEstimateTests.An_estimate_that_has_seen_every_countable_sample_still_takes_one_more`,
`EncodingVariantTests.An_id_followed_by_a_line_feed_is_a_format_error`, `SegmentTests.Nothing_follows_the_last_representable_segment`.

## Frictions and surprises, in order
1. **`dotnet test --project Tests.Expert` does not run on the .NET 10 SDK as is.** Exact output:
   `MSBUILD : error MSB1001: Unknown switch ... --project Tests.Expert`; with the positional form `dotnet test Tests.Expert`:
   `Testing with VSTest target is no longer supported by Microsoft.Testing.Platform on .NET 10 SDK and later ... opt-in to the new dotnet test experience`.
   Expected: the xUnit adapter page (which pins `xunit.v3` 4.0.2) to say how to run the tests. Did: added `global.json`
   `{ "test": { "runner": "Microsoft.Testing.Platform" } }` at the workspace root. Nothing in the user documentation mentions it.
2. **A dependency between two parameters of different types leaves the scaffold in the way, not under it.** `EncodingVariant` requires
   `id` to name the height (`"720p30"` for height 720). `any` reported `id string Any.String() to verify, unread guards`,
   `width ... to verify, guard, unread guards`, `height ... to verify, guard, unread guards`. The docs' "derive one draw from another in
   `Generate()`" is the right answer, but then `WithId(...)` (and its lambda form) is a lie that I had to delete by hand, together with
   `_id`, `_idRecipe`, and the constructor parameters: about a third of the file. Same story for `BitrateLadder` (a whole rewrite) and
   `AdaptationDecision`. It works and the docs warn about it, but it is the single costliest step of this domain.
3. **`width % 2 != 0 || height % 2 != 0` is not read.** The table of recognised guards has no modulo row, although the library has
   `.Even()` / `.MultipleOf(n)`. The tool wrote `Between(128, 7680)` marked `to verify`; I added `.Even()` myself. A `p % N != 0` row
   (-> `.MultipleOf(N)`, `.Even()` for 2) looks like a natural, safe addition (idea).
4. **The scaffolder and JD036 disagree.** Raising the analyzers to warning (`dotnet_diagnostic.JD0xx.severity = warning`, to see the Info
   rules which `dotnet build` hides, as the docs say) gives 8 warnings, 4 of them in files `any` wrote:
   `Generators/AnyAdaptationPolicy.cs(39,16): warning JD036: This floating-point dummy declares no range: one draw in ten spans the whole type`
   (for `Any.Double().GreaterThanOrEqualTo(0d)` and `Any.Double()`). The domain declares no upper bound there, so the fix JD036 asks for
   ("declare the range the code accepts") would invent an invariant. My own `Any.Double().Positive()` dummies (media seconds, predicted time)
   are flagged for the same reason. I left them: the code under test really accepts any positive value. Removed my `.editorconfig` afterwards.
5. **There is no seam to tie two composed parameters while keeping the test's pins.** For `PlaybackSession` the comfort threshold of the policy must
   not exceed the capacity of the buffer. First idea: in `Generate()`, `_policyRecipe.WithComfortBufferSeconds(c => c.LessThanOrEqualTo(capacity))`.
   The docs state the trap ("a lambda written after a pin takes the pin's place"), and it would silently discard `WithComfortBufferSeconds(20)` from
   a test. I wrote `WithComfortBufferSecondsAtMost` in a partial of `AnyAdaptationPolicy` touching the private `_comfortBufferSecondsRecipe` and
   `PanicBufferSecondsAndComfortBufferSeconds()`. It works (documented as "adding the overload yourself"), but it is welded to the scaffold's private
   names: a `--force` re-scaffold that renames a field breaks it (loudly, at least).
6. **A drawn session is not ready to decide.** `Any.PlaybackSession().Generate()` may have a buffer too full for the next segment, so `DecideNext`
   throws "The buffer is full". That is not the library's fault (the constructor accepts it), but it shows that the "valid base" is the constructor's
   validity, not the readiness a test needs, hence `Playback.Recipe(...)`: every scenario test pins ladder, capacity, level, segment length and estimate
   explicitly (literals that are significant), and leaves the rest drawn.
7. **`Any.Int32().Positive()` never produced `int.MaxValue` or `1`** (20 000 draws: 0 and 0; 10 742 of them above 10^9). Consistent with the numbers page
   (integers: uniform over the whole range; the limit-value tenth is described for floating point and `OrNull`), but I had read `ADR-0117`'s
   title ("draw a limit value the type permits one draw in ten") in the numbers page links and expected integer extremes too. It means that
   unbounded integer dummies cannot reveal off-by-one-at-the-limit defects such as `BandwidthEstimate.WithSample` overflowing the sample count
   (see findings): I had to write that case with a literal.
8. `dotnet build` prints **no JD diagnostic at all** on my project, even though some Info rules apply. Documented, and I understand the choice, but as a
   heavy user I only learned what the analyzers think of my tests by switching all 38 on in an `.editorconfig`.
9. Small: `A pinned object is shared` bites the aggregate domain (`PlaybackBuffer` is mutable): `WithBuffer(new PlaybackBuffer(...))` would be reused by
   every `Generate()`. The docs warn; I used lambdas (`WithBuffer(b => b.WithCapacitySeconds(60).WithLevelSeconds(30))`), which draw a fresh buffer per session.

## What worked well
- The scaffolder read most guards correctly and even reused my constants (`Any.Int32().Between(Bitrate.MinKbps, Bitrate.MaxKbps)`).
  `levelSeconds <= capacitySeconds` and `comfort > panic` came out as `Any.OrderedPair(...)` (the second one `strict: true`) with pins that bound the other
  side: `Any.PlaybackBuffer().WithLevelSeconds(10)` yields a capacity of at least 10 with no code from me.
- `to verify` stopping the build at the exact line is what I want from a scaffolder: nothing wrong ever compiled silently.
- `Any.X().WithY(...)` plus lambdas read very well in the scenario tests: `Playback.Recipe(...)` is one fluent expression naming the ladder, the
  segment length, the estimate, the policy thresholds and the buffer, and everything else stays arbitrary.
- `[assembly: Reproducible]` + MTP: a failing test prints
  `[JustDummies] These arbitrary values were seeded with 1329166644. Reproduce this run with [JustDummies.Xunit.Reproducible(Seed = 1329166644)].`
  and `Reproducible(Seed = 12345)` reproduced the same draw (checked with a throw-away failing test, then deleted). My hand-written generators
  (sets, `Then`-free but multi-draw) replay value for value from a seed (`GeneratorsTests`).
- The "dummy or literal?" discipline was easy to follow with this API: every refusal test pins the invalid value as a literal and draws the rest;
  every arithmetic test pins what the assertion is about (rates, thresholds, durations) and draws the remainder (width, id, sample count, segment index).
- Analyzers: 0 warnings in the build; with everything raised to warning, only JD036 (above) had anything to say.

## Fluency: 4 / 5
The vocabulary is small and regular, the docs answered every question I had (pins vs lambdas, shared pinned objects, the "pin is replaced by a
later lambda" trap, derived draws in `Generate()`), and for the value objects `any` gave me a finished generator in one command. I lose a
point because the three types with cross-parameter rules (variant id/height, ladder, decision, plus the session ties) needed a hand rewrite of
the scaffold, because tying two composed generators while preserving pins relies on private names of the generated file, and because the
test runner command of the adapter does not work on the current SDK without an undocumented opt-in.
