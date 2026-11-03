# Regular notes — 25 Adaptive video playback control
Agent: the same model and effort as every session of this trial. Library: JustDummies 1.0.0-uxtrial.1 (packed from main).

## What I did
- Read: `PROJECT-README.md`, `guides/getting-started.en.md` (skimmed), `packages/justdummies-cli.en.md` (most of it: guard table,
  `Adjusting a parameter`, `When a guard ties two parameters`), `packages/justdummies-xunit.en.md`, `generators/README.md`, `generators/numbers.en.md`.
- Wrote `Tests.Regular/AdaptiveVideo.Tests.Regular.csproj` from the template, `dotnet restore`, then
  `any generate Bitrate Latency Segment EncodingVariant` and `any generate BandwidthEstimate AdaptationPolicy AdaptationDecision BitrateLadder PlaybackBuffer PlaybackSession`.
- Scaffolded cleanly (nothing to touch): Bitrate, Latency, Segment, BandwidthEstimate, AdaptationPolicy (strict `OrderedPair` panic/comfort, nice), PlaybackBuffer
  (`OrderedPair` level/capacity).
- Completed by hand (the files with `TODO(any)` / that cannot honour a cross-value rule):
  - `AnyEncodingVariant`: the id is derived in `Generate()` (`$"{height:D3}p{fps}"`), so the string recipe became a frames-per-second recipe and `WithId` became
    `WithFramesPerSecond`; width/height got `.Even()`.
  - `AnyAdaptationDecision`: `chosen` is derived from `previous` and the drawn reason (the reason must agree with the move); `previous` bitrate kept in 100..99 000 so both directions exist.
  - `AnyBitrateLadder`: built in `Generate()` from a set of distinct bitrates and a set of distinct ascending heights (unique ids, resolution never decreasing); `WithRungs(n)`.
  - `AnyPlaybackSession`: removed the three TODOs; `Generate()` draws the buffer first, then writes `comfort <= capacity`, `panic < capacity` and `segment <= capacity` onto the policy/segment recipes
    (unless the test pinned them).
- Tests (158 methods, 240 cases, 3 skipped on domain defects): `BitrateTests`, `LatencyTests`, `SegmentTests`, `EncodingVariantTests`, `BandwidthEstimateTests`, `AdaptationPolicyTests`,
  `AdaptationDecisionTests`, `PlaybackBufferTests`, `BitrateLadderTests`, `PlaybackSessionConstructionTests`, `PlaybackSessionTests` (first decision, climb/hold/drop, panic, warm-up, completion, stalls, idle),
  plus `Fixtures.cs` (literal ladder), `GlobalUsings.cs` (`global using Xunit;`), `AssemblyInfo.cs` (`[assembly: Reproducible]`). Each hand-written generator has its own "drawn value is accepted" loop test.
- Result: green with `dotnet test --project Tests.Regular` (after adding a `global.json`, see friction 1); 37 consecutive full runs green after the fix of friction 5 (25 + 12), fresh seeds each time.
  Skipped (domain defects, see below): trailing line break accepted in an id, `SampleCount` overflow, `Segment.Next()` overflow.

## Frictions and surprises, in order
1. `dotnet test --project Tests.Regular` -> `MSBUILD : error MSB1001: Unknown switch. ... --project Tests.Regular`, and plain `dotnet test Tests.Regular` ->
   `Microsoft.Testing.Platform.MSBuild.targets(395,5): error : Testing with VSTest target is no longer supported by Microsoft.Testing.Platform`. On SDK 10.0.114 with `xunit.v3` the
   MTP runner needs `{ "test": { "runner": "Microsoft.Testing.Platform" } }` in a `global.json`. The library's documentation shows `dotnet test --project ...` (CONTRIBUTING) but nothing tells a consumer
   that a `global.json` is needed (the repository has one). I created `W/global.json`. Expected: a line in the xUnit adapter page.
2. First `any generate` -> `✗ Bitrate: this project has not been restored, so its package references cannot be read. Restore it, then run the command again: dotnet restore`.
   Clear and actionable; only note that it is something the docs' install sequence does not mention (you must restore first).
3. `any generate` writes into the current directory with the domain namespace (`AdaptiveVideo.Domain`) rather than the test project's, fine here, but the files then live in a namespace that is not the test
   project's. Not a problem, just something to remember (`--namespace`).
4. Cross-value rules: `AdaptationDecision` (reason must agree with previous/chosen), `BitrateLadder` (distinct, ascending, unique ids), `EncodingVariant` (id derived from height), `PlaybackSession`
   (comfort <= capacity, segment <= capacity). The tool marked them all `to verify` and stopped the build with `TODO_verify_the_generator_for_...`, exactly as documented; the recap
   named the guard and the line. It correctly paired `height <= width` and `level <= capacity` and `panic < comfort` with `Any.OrderedPair` on its own, which saved a lot of work. But for the four
   aggregates above the work is "rewrite `Generate()` yourself", 20 to 40 lines each; the docs' four recipes (start+duration, derive in `Generate()`, check digit, `DifferentFrom`) did not cover
   "pick a reason, then a move that agrees with it" or "n distinct sorted values"; I used `Any.SetOf(...).WithCount(n)` + sort, which worked first time.
5. Not-a-dummy draws (test failures, seeds -1881500957, -1683464561, 2080790452): `The_first_decision_stays_on_the_lowest_variant...`, `The_predicted_download_time...` failed with
   `InvalidOperationException : The buffer is full: wait for playback before requesting another segment.` The drawn `PlaybackBuffer` can legitimately be full (level close to capacity), and
   `DecideNext` needs room: the buffer was not a dummy for those tests. Fix: the shared `Standard()` builder states the room (`WithBuffer(b => b.WithCapacitySeconds(100).WithLevelSeconds(l => l.LessThanOrEqualTo(50)))`).
   Not flaky, my test was wrong; the seed made it easy to see that. Also found by the way that a session generated with a full buffer is a valid session, so the scaffold is faithful.
6. Analyzer `JD009` on `private static readonly Bitrate AnyRate = new AnyBitrate().Generate();` -> `warning JD009: Hold the generator rather than the value: a static initializer draws once for the whole suite, ...`.
   Useful and fair; I turned the field into a property. It did not catch `private static readonly EncodingVariant Slow = Fixtures.Variant(...)` (the draw is hidden behind my own helper), I replaced those by
   properties too by habit.
7. `With…(Func<…>)` after a pin: `Standard().WithBuffer(new PlaybackBuffer(...))` replaces the lambda-refined base I put in `Standard()`, which is what I wanted, but I only knew from the docs ("A pin keeps the recipe ... a lambda
   written after it takes the pin's place"). Reading that paragraph twice was needed to be sure the order of `WithBuffer(lambda).WithBuffer(value)` was safe.
8. The scaffolded `AnyAdaptationPolicy.comfortBufferSeconds` is `Any.Double()` (unbounded one side through the pair), so one draw in ten can be near `double.MaxValue`, which is valid for the policy but fails the
   session. The tool did mark the session's policy parameter `to verify` ("ties policy to buffer"), so the signal was there. No surprise, just something every consumer of that generator must know.
9. Domain defects found by reading the code while choosing cases (all verified by running, kept as skipped tests, no seed involved since the input is a literal):
   - `EncodingVariant` id regex `^[0-9]{3,4}p(24|25|30|50|60)$` uses `$`, which also matches before a trailing `\n`: `new EncodingVariant("720p30\n", 1280, 720, bitrate)` is accepted (and `int.Parse` tolerates the newline).
   - `BandwidthEstimate.WithSample` on `SampleCount == int.MaxValue` throws `ArgumentOutOfRangeException: An estimate rests on at least one sample. (Parameter 'sampleCount') Actual value was -2147483648`.
   - `Segment.Next()` on `Index == int.MaxValue` throws `A segment index cannot be negative`.
   The scaffolded `Any.Int32().Positive()` for `sampleCount` could in principle draw `int.MaxValue`, so a drawn estimate may one day hit the second one in a `CompleteDownload` test; it did not in 37 runs.
10. Output noise: the failing run prints `[JustDummies] These arbitrary values were seeded with ...` for a deterministic failure that has nothing to do with the seed (the skipped/literal ones are quiet, good). Fine.

## What worked well
- `any generate` on a batch of types, with the recap saying which guards were read (`guard`) and which not, and the file stopping the build on the ones it could not vouch for. It paired the ordered
  parameters (`OrderedPair`, strict when the guard is `<=`) on its own, including the pin interactions.
- `With…(Func<…>)` refinement on nested generators (`WithBuffer(b => b.WithCapacitySeconds(100).WithLevelSeconds(l => l.LessThanOrEqualTo(50)))`) made "a session about to decide" a one-liner while keeping everything else a dummy.
- `[assembly: Reproducible]` plus the seed in the output of the failing test: three failures on drawn values were diagnosed in a minute each.
- The `JD009` analyzer, immediately actionable.

## Fluency: 4 / 5
Daily use is smooth: scaffold, complete the TODOs, refine with lambdas, one attribute for reproducibility. The deductions are the `global.json` / `dotnet test` setup that the docs never mention for a consumer and the real
amount of hand-written `Generate()` for aggregates with cross-value rules (four of eleven types), which the tool rightly refuses to guess but for which the documentation offers fewer patterns than I needed.
