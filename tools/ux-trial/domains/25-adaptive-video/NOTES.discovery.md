# Discovery notes — 25 Adaptive video playback control
Agent: the same model and effort as every session of this trial. Library: JustDummies 1.0.0-uxtrial.1 (packed from main).

## What I did
Pages read, in order: `docs/PROJECT-README.md`, `docs/README.md` (the index), `guides/getting-started.en.md`,
`guides/core-concepts.en.md`, `packages/justdummies-cli.en.md` (all of it, it is long but it is the page for `any`),
`packages/justdummies-xunit.en.md`, `guides/composition.en.md`, then the reference pages when I needed them:
`generators/collections.en.md` (the ladder example is almost my case), `generators/enums-and-choices.en.md`,
`generators/numbers.en.md`, and `analyzers/JD037.en.md` once the compiler pointed me at it.

`any` commands run (from `Tests.Discovery/`, after a `dotnet restore`):
`any generate Bitrate EncodingVariant Segment Latency BandwidthEstimate AdaptationPolicy PlaybackBuffer BitrateLadder PlaybackSession AdaptationDecision --entry-point any`
(the first attempt, before the restore, was refused with a clear message).
Result: 10 generators plus 10 `*.Entry.cs`. Six compiled as they were (`Bitrate`, `Segment`, `Latency`,
`BandwidthEstimate`, `AdaptationPolicy`, `PlaybackBuffer`); four had `to verify` markers that stopped the
build and had to be completed by hand:
* `AnyEncodingVariant`: the id is `"<height>p<fps>"`, tied to the height. I replaced the id parameter by a frame-rate
  draw (`Any.Int32().OneOf(24, 25, 30, 50, 60)`), made width and height `.Even()`, raised the height floor to 100, and
  spelled the id in `Generate()`. `WithId` became `WithFramesPerSecond`.
* `AnyBitrateLadder`: rewrote the recipe as `Any.Int32().Between(1, 6).Then(count => Any.Combine(ordered distinct heights,
  ordered distinct bitrates, frame rates, even width offsets, composer))`, typed `IAny<IEnumerable<EncodingVariant>>`.
* `AnyPlaybackSession`: the three marked parameters got an explicit valid base (buffer capacity at least 60 s, policy with
  panic under 30 s and comfort at most 60 s, segment duration 0.5 to 10 s) so the constructor's ties hold by construction.
* `AnyAdaptationDecision`: previous and chosen variants are drawn freely, and `Generate()` draws the reason among those the
  move allows (`WithReason(value)` still pins it; its refining overload was removed).

Tests written (185 methods, 332 cases, 4 skipped as domain defects), in `Tests.Discovery/`:
`BitrateTests`, `LatencyTests`, `SegmentTests`, `EncodingVariantTests`, `AdaptationPolicyTests`, `BandwidthEstimateTests`,
`PlaybackBufferTests`, `BitrateLadderTests`, `AdaptationDecisionTests`, `PlaybackSessionTests` (construction, first decision,
trusted decisions: climb/hold/comfort/panic/drop, warm-up, CompleteDownload, Idle, two whole-session scenarios),
`GeneratorTests` (every generator drawn 300 times, plus one session scenario), with `Fixtures.cs` (shared builders) and
`AssemblyInfo.cs` (`[assembly: Reproducible]`).

Final result: green (328 passed, 4 skipped, 0 failed), then 30 more full runs with fresh seeds, all green.

## Frictions and surprises, in order
1. **`dotnet test --project Tests.Discovery` does not run on the .NET 10 SDK as the docs' mental model suggests.**
   First `MSBUILD : error MSB1001: Unknown switch. ... --project Tests.Discovery`. Then `dotnet test Tests.Discovery` gave
   `Testing with VSTest target is no longer supported by Microsoft.Testing.Platform on .NET 10 SDK and later. If you use dotnet
   test, you should opt-in to the new dotnet test experience`. I had to write a `global.json` with
   `{ "test": { "runner": "Microsoft.Testing.Platform" } }` next to the projects. Neither the xUnit adapter page nor the README
   says so (the README only shows `dotnet test --solution ...`). Not JustDummies' fault, but a newcomer on the .NET 10 SDK
   loses ten minutes; one line in the adapter page ("on the .NET 10 SDK, opt in to Microsoft.Testing.Platform in global.json") would save them.
2. **`any generate` before a restore** answers `this project has not been restored, so its package references cannot be read.
   Restore it, then run the command again: dotnet restore`. Clear and actionable; I expected a failure and got instructions. Fine.
3. **The recap does not show the ordered pairs it wrote.** For `AdaptationPolicy` the rows read
   `panicBufferSeconds Any.Double().GreaterThanOrEqualTo(0d) guard` and `comfortBufferSeconds Any.Double() guard`. Read alone,
   comfort looks unconstrained, although the constructor requires comfort > panic. Only opening `AnyAdaptationPolicy.cs`
   showed `Any.OrderedPair(..., strict: true)`. The same for `PlaybackBuffer` (level <= capacity) and `EncodingVariant`
   (height <= width). The CLI page says the tool "draws them together through `Any.OrderedPair`", but the recap line is where I
   looked first and it said nothing; a `pair` word in the last column (or one line under the rows) would tell me the tie was read.
4. **A scaffolded generator cannot express "this value is derived from that one" and I rewrote parameters.**
   `EncodingVariant.id` is `"<height>p<fps>"`; the tool rightly marked `id`, `width`, `height` as `to verify` (regex, parity,
   id/height tie). The docs' advice ("derive one draw from another inside `Generate()`") works, but the id parameter then has
   no recipe of its own: I had to delete `WithId`, add a frame-rate recipe and rewrite the constructor/copy plumbing by hand.
   Same for `AdaptationDecision.reason` (derived from the move). It is the documented design, just a lot of manual plumbing for a
   very common domain shape (identifier computed from other fields).
5. **A test I wrote failed on a drawn value, and the draw was right.** `A_drawn_session_can_take_its_first_decision`
   (`Any.PlaybackSession().Generate()` then `DecideNext`) failed once in 40 runs:
   `InvalidOperationException : The buffer is full: wait for playback before requesting another segment.`, seed `-682981422`.
   Replaying with `[Reproducible(Seed = -682981422)]` reproduced it at once (this part of the library is excellent).
   I measured 287 of 4000 drawn sessions with a full buffer (about 7 %): the generator draws the level anywhere in
   `[0, capacity]`. The buffer level was not a dummy for that test (a full buffer refuses to decide), so I constrained it
   (`WithBuffer(b => b.WithLevelSeconds(l => l.LessThanOrEqualTo(50d)))`, capacity being at least 60 s and a segment at most 10 s).
   Not a defect of the library; recorded because it is exactly the "not a dummy" lesson of the docs, discovered the hard way.
6. **JD037 fired on my first ladder generator**:
   `AnyBitrateLadder.cs(42,38): warning JD037: This draw runs inside the lambda passed to Combine, apart from the recipe: compose an
   offset with Any.Combine, or derive the dependent draw with Then`. I had written `new AnyEncodingVariant().WithHeight(h)....Generate()`
   inside the composer to get a valid width. The message and the JD037 page told me what to do; I replaced the inner draw by a
   drawn even offset added to the height (clamped to 7680) and called the real constructor in the composer. Good diagnostic.
7. **`using` lines.** The getting-started snippets show no `using`; the test project needed `global using JustDummies; global using Xunit;`
   (and `using AdaptiveVideo.Domain;` because the scaffolded generators and their `Any.X()` entry points sit in the domain type's
   namespace). IntelliSense-like errors (`CS0246 'InlineData' / 'Fact' could not be found`, `Any` not found) made it obvious, no real cost.
8. **Domain defects found by the tests** (kept as tests, skipped with `Skip = "domain defect: ..."`; details in the findings):
   `Bitrate.DownloadSeconds(NaN, t)` and `(m, NaN)` return NaN instead of refusing; an id ending in a line feed (`"720p30\n"`)
   passes the id pattern; `BitrateLadder.IndexOf/StepUp/StepDown` accept a look-alike variant that shares an id with a rung but
   has another bitrate. Also seen with throwaway probes (not kept as tests): `Segment(int.MaxValue, d).Next()` and
   `BandwidthEstimate(.., int.MaxValue, ..).WithSample(..)` throw "cannot be negative" / "at least one sample" by overflow; a
   zero-padded id `"0720p30"` is accepted as another id of the 720p rung; a NaN `kilobits` in `WithSample` is refused as
   `throughputKbps`, not `kilobits`; an infinite predicted download time is accepted.
9. **Documentation links.** `PROJECT-README.md` and the guides link to `doc/handwritten/for-users/...` and to
   `../../for-maintainers/adr/...` pages that are not in the documentation folder I was given. I only followed relative links
   inside `docs/`, so no page was missing for me; mentioned in case a user reads the packaged pages offline.

## What worked well
* The documentation path is genuinely a ten-minute path: PROJECT-README, getting-started and core-concepts gave me the
  vocabulary (recipe, `anyXxx` names, "if an any reaches your assertion it is not a dummy") before I wrote a line, and I
  used that rule constantly to decide what stays a literal (thresholds, bitrates, buffer levels) and what is drawn
  (latency, widths, frame rates, ids).
* `any generate` is honest. Clear recap, a build that stops exactly where it could not vouch for a guard, `TODO(any)` comments
  that quote the constructor line, and ordered pairs written for `level <= capacity`, `height <= width` and `panic < comfort`
  without my asking.
* The refine/pin split of the generated `With...` methods is pleasant in tests (`Any.EncodingVariant().WithHeight(720).WithFramesPerSecond(30)`),
  and `.Ordered().Distinct()` plus `Then(count => Any.Combine(...))` expressed a bitrate ladder in a dozen lines; the collections page even had that very example.
* Seeds: the failure output names the exact attribute to paste, and replaying the seed reproduced the full-buffer failure on the first try.
* JD037 steered me from a draw-inside-a-lambda to the supported shape with a precise message.
* `.Even()`, `OneOf`, `Between` made the domain invariants readable in the generators.

## Fluency: 4 / 5
I was productive within the first half hour and the docs answered nearly every question I had. The points lost are the
.NET 10 test-runner opt-in that nothing mentions, the recap not announcing the ordered pairs it wrote, and the real cost of
completing generators for types whose fields are derived from each other (ids, reasons): the hand-written plumbing is the
slowest part of the whole exercise, even though the library's design (own the file, keep a pure `Generate()`) makes it safe.
