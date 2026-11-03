# Expert notes — 21 orbital microbiology
Agent: the same model and effort as every session of this trial. Library: JustDummies 1.0.0-uxtrial.1 (packed from main).

## What I did
**Pages read, in order:** `MODEL.md`, then the domain sources (`Domain/*.cs`), `docs/PROJECT-README.md`,
`guides/getting-started`, `guides/core-concepts`, `packages/justdummies-cli` (whole page, it is the one that
matters for the scaffold), `generators/README`, `generators/numbers`, `generators/strings` (layout, patterns),
`generators/enums-and-choices` (first half), `guides/reproducibility` (first half), `packages/justdummies-xunit`
(first half), `guides/composition` (first half), `analyzers/README`, the Info-severity section of
`packages/justdummies`.

**Setup.** `Tests.Expert/OrbitalMicrobiology.Tests.Expert.csproj` from the template, plus a `W/global.json`
(see friction 1), plus a `Tests.Expert/.editorconfig` that raises the JustDummies categories to `warning`
so the Info rules show in `dotnet build` (docs: "Info diagnostics do not appear in dotnet build output").

**`any` commands run** (from `Tests.Expert/`, after `dotnet restore`):

```
any generate CultureId GrowthRate Concentration Species ExperimentalConditions Observation EstimatedState \
    Prediction MicrobialPopulation Culture --output Generators --namespace OrbitalMicrobiology.Tests.Expert --entry-point any
```

Ten generators plus ten `*.Entry.cs` entry points (`Any.Species()`, ...). Result of the first run: 5 files compile
as is (`GrowthRate`, `Concentration`, `EstimatedState`, `Prediction`, `MicrobialPopulation` straight from guards),
5 stop the build on purpose (`CultureId`, `Species` 7 params, `ExperimentalConditions` 2, `Observation` 2, `Culture` 1).

**Generators completed by hand** (all in `Tests.Expert/Generators/`):

| Generator | What I did |
| --- | --- |
| `AnyCultureId` | regex guard: `Any.StringMatching("CH(0[1-9]|1[0-2])-C[0-9]{4}")`, retyped `AnyString`->`AnyPattern` (4 places, as the TODO said) |
| `AnySpecies` | name: `Any.StringMatching("[A-Z][a-z]{2,11} [a-z]{3,12}")` (retyped); rate: `new AnyGrowthRate().WithPerHour(r => r.Positive())`; the 3 cardinal temperatures drawn from three disjoint windows (-20..10, 15..45, 50..80) so `min < opt < max` holds by construction; lag `Between(0, 240)`, yield `Between(1e9, 1e13)` (the `ThrowIfOutside` helper was not read) |
| `AnyExperimentalConditions` | the `gravity >= 0.5 && volume > 10` tie: hand-written `Generate()` that lets the unpinned side follow the pinned one (detects a pin with `ReferenceEquals(_x, _xRecipe)`, see friction 5) |
| `AnyObservation` | `relativeUncertainty` base floor raised to the indirect minimum (0.10) so any drawn method is valid; `elapsedHours` bounded |
| `AnyCulture` | start time `Between(2000-01-01, 2100-01-01)` at offset 0 |
| `AnyEstimatedState`, `AnyMicrobialPopulation`, `AnyPrediction` | only the one-sided `GreaterThanOrEqualTo(0d)` rows replaced by `Between(0d, 10_000d)`; count `Between(0, 1_000)` |

Beside them, in files of their own so `--force` cannot touch them: `Recipes.cs` (extension methods on the
generators that name what a test chose: `Mesophilic()`, `GrowingAt(1.0)`, `OnTheControlBench()`, `Dilute()`) and
`Entities/Scenes.cs` (the growing species, the control chamber, the scalding chamber).

**Tests written** (198 methods = 138 `[Fact]` + 60 `[Theory]`, 380 cases; `[assembly: Reproducible]`):

- `ValueObjects/CultureIdTests` (format, chamber range, equality), `GrowthRateTests`, `ConcentrationTests`
  (bounds, log10, generations, ordering, equality), `SpeciesTests` (name format, cardinal order, ranges,
  temperature factor on an asymmetric 0/10/50 species, equality by name), `ExperimentalConditionsTests`
  (ranges, centrifuge rule boundaries, microgravity threshold, effective rate by hand-computed values per
  temperature/Monod/gravity, rate cap, carrying capacity, relative growth, equality), `ObservationTests`
  (bounds, one-sigma, optical-density rule boundary, refusals), `EstimatedStateTests`, `PredictionTests`
  (target, horizon, `IsConfirmedBy` by combined sigma, 0.5 h window, species), `MicrobialPopulationTests`.
- `Entities/CultureTests` (construction, UTC offset, inoculation rules and the 4-species cap, capacity
  boundary, interactions, termination), `CultureGrowthTests` (Advance limits, additivity, near-exponential
  growth, capacity ceiling, lag, death at 0.1/h outside the cardinal range incl. the exact min/max,
  extinction, competition/facilitation, uncertainty drift and its cap), `CultureEstimationTests`
  (inverse-variance blend with hand-derived expectations, 0.5 h window, per-species counts, `Predict` leaves the
  culture intact and equals the lived advance, forecast uncertainty, prediction/observation end to end,
  generations).

**Result.** Green: 376 passed, 4 skipped (all four are recorded domain defects, below). After the suite was
complete I ran it 94 more times with fresh seeds (14 + 80), all green; the fourth skipped test was added after
those runs and the suite was then run once more green. One failure on a drawn value occurred along the way
(seed -1087769823, domain defect 3).

**Domain defects found (tests kept, skipped):**
1. `CultureId("CH03-C0042\n")` is accepted (regex ends in `$`, which .NET also matches before a final `\n`).
2. Same for `Species` names (`"Escherichia coli\n"`).
3. Facilitation can drive the helped species extinct: a facilitator with yield 6.68e12 cells/g (3.3e10 cells/mL
   after 48 h) and interaction -0.5 leaves the helped species at 0 instead of above its solo level. Probable
   mechanism, read from the code and not traced step by step: with a relative load near -330 the explicit Euler step
   multiplies the helped population by about 30 per step up to the 1e13 clamp, the load then turns hugely positive and
   the next step goes negative and is clamped to 0. Seed -1087769823. Kept as a skipped test, with a sibling test pinning a facilitator of comparable yield.
4. `Advance` depends on how the caller slices time: 10 h in one call from 1e3 cells/mL gives 5.41e6, 1000 calls of
   0.01 h give 7.29e6 (steps are `min(0.1, remaining)`).

## Frictions and surprises, in order
1. **`dotnet test --project Tests.Expert` does not run on this SDK.** Exact output:
   `MSBUILD : error MSB1001: Unknown switch ... Switch: --project`. `dotnet test Tests.Expert` then fails with
   `Testing with VSTest target is no longer supported by Microsoft.Testing.Platform on .NET 10 SDK and later. If you use
   dotnet test, you should opt-in to the new dotnet test experience`. Neither the xUnit page nor the README tells a
   consumer that xunit.v3 4.0.2 on the .NET 10 SDK needs `global.json` `{"test":{"runner":"Microsoft.Testing.Platform"}}`.
   I created `W/global.json` and everything worked. Expected: a line in the adapter page's Install section.
2. **`any generate` needs a prior restore.** First run: `this project has not been restored, so its package references
   cannot be read. Restore it, then run the command again: dotnet restore`. Clear and actionable; I ran
   `dotnet restore` in the test project and re-ran. (The getting-started page does not mention it.)
3. **The most common guard idiom in this domain is not read.** `Species` validates through a
   `ThrowIfOutside(value, -20, 80, nameof(value))` void helper with constant bounds; the tool reports
   `7 of 7 parameters inferred, 7 to verify` and every double stays a bare `Any.Double()`, although each is a
   plain `Between(min, max)` once the helper is looked at. The docs say a `void` helper is marked, so this is as
   documented, but it means a domain that factors its range checks in a helper gets no bound at all from the
   scaffold and I wrote 5 ranges by hand, copying the numbers.
4. **Cosmetic defect in the TODO text:** the comment reads
   `TODO(any): verify against Species.cs, line 37: ThrowIfOutside(minTemperatureCelsius, -20, 80, nameof(minTemperatureCelsius));; and 1 more`
   (two semicolons: the statement's own `;` plus the separator). The recap line for the same parameter prints it
   the same way.
5. **Three ordered values (min < opt < max) are not scaffolded, and `OrderedPair` does not help here.** The recap
   says "first unread guard ... and 1 more" without saying that the second guard ties three parameters (the
   tie of two is called out: "ties gravityG to volumeMillilitres"). The composition page gives
   `OrderedPair(...).Then(...)` returning a tuple, which fits a free-standing `IAny<(int,int,int)>` but not the
   scaffolded `With…` pins (a pin on `WithMinTemperatureCelsius` would no longer reach the draw, as the CLI page
   warns). I chose three disjoint windows for the base, which keeps every pin and refinement working, at the price
   of never drawing, say, a species that grows from 25 to 60 degrees. A named, documented pattern for "n ordered
   parameters, pins preserved" is missing.
6. **A tie between two parameters under `&&` leaves no sanctioned way to keep pins working.** The centrifuge rule
   (`gravityG >= 0.5 && volume > 10` throws) and the optical-density rule (`method == OD && uncertainty < 0.10`)
   are `&&` guards across two parameters; both are marked `to verify`. The doc's advice (derive one draw from
   the other in `Generate()`) makes the other side's pin silently ignored; "pin both or write the tie in
   `Generate()`" is all it offers. I wanted `WithGravityG(1)` to make the volume follow, and `WithVolumeMillilitres(30)`
   to make the gravity follow, so I tested for a pin with `!ReferenceEquals(_gravityG, _gravityGRecipe)`,
   which relies on how the generated `With…` methods happen to assign private fields. It works and `Generate()`
   reads well, but it is a trick I had to invent; for Observation I took the cheap route (raise the uncertainty base
   to 0.10) and thereby lose the ability to draw a direct-method observation under 10 % uncertainty by default.
7. **The scaffold emits code its own analyzer flags.** `elapsedHours`, `ageHours`, `madeAtHours` are scaffolded as
   `Any.Double().GreaterThanOrEqualTo(0d)` (from `elapsedHours < 0` guards). I saw this in the tool output and
   replaced them before compiling, but a probe `Any.Double().GreaterThanOrEqualTo(0d).Generate()` raises
   `JD036: This floating-point dummy declares no range: one draw in ten spans the whole type` (with the
   `.editorconfig` raise) — one in ten draws of an elapsed time would have been about 1e308. The same applies to
   `Any.Int32().GreaterThanOrEqualTo(0)` for `observationCount` (no rule fires there, draws up to `int.MaxValue`).
   The scaffold cannot know a sensible ceiling, but its own recap could say "one-sided bound" the way it says
   `to verify`.
8. **`Culture.startedAtUtc` TODO is silent about the range.** The only guard named is the offset. The scaffolded
   `Any.DateTimeOffset()` spans year 1 to 9999, which the dates page documents as overflowing in `AddHours` near the
   ends; `CurrentTimeUtc` is exactly such a computation. I knew from the docs and bounded it to 2000..2100, but the
   TODO text does not mention it.
9. **Info rules are invisible until raised.** The docs say so; I put the four categories at `warning`
   (`dotnet_analyzer_diagnostic.category-JustDummies.<Constraints|Usage|Reproducibility|Composition>.severity`).
   JD030 and JD036 came through (probe files), which confirms the `Constraints` category name; I could not tell whether
   the other three names are honoured because nothing in the suite triggered them.
10. **A pinned refusal reads well but moves trust to the generator.** To test e.g. "a gravity above 2 g is refused",
    `Any.ExperimentalConditions().WithGravityG(2.1).Generate()` lets the generator draw the other three valid values
    and the constructor throws the real `ArgumentOutOfRangeException`. Pleasant, but if the generator's base were
    wrong the test would throw some other exception, hence my `Assert.Throws<ExactType>` everywhere.
11. **`Assert.Throws` is exact-type and `ArgumentOutOfRangeException` derives from `ArgumentException`:** the domain
    throws `ArgumentException` for format/ordering and `ArgumentOutOfRangeException` for ranges, which I had to
    keep apart in the tests. Not a library matter; noting it because the first red run (trailing newline, defect 1)
    was found precisely that way.
12. **Expected-value temptation.** Several scenarios want the answer the model computes (for example the Euler growth
    is 6.0 to 6.6 generations rather than the continuous 6.56). I kept expected values as hand-derived literals or
    ranges with the arithmetic in a comment; `Culture.InitialRelativeUncertainty` is used as a constant where the
    exact value is not the point (the blend expectations are written so that they hold whatever its value).

## What worked well
- **The recap and the non-compiling TODO**: five of ten generators told me exactly which guard to look at and where
  (`Species.cs, line 27: if (!BinomialFormat().IsMatch(binomialName))`), and the regex case spelled out the
  `AnyString`->`AnyPattern` retype in four places; following it literally compiled first time.
- **`Func<AnyX, AnyX>` refinements on composed parameters**: `Any.Culture().WithConditions(c => c.OnTheControlBench())`
  is the difference between a readable arrange and a wall of literals; the test says "control bench", the volume
  stays arbitrary and the centrifuge rule is honoured by the generator.
- **Distinctness through the element's own equality**: `Any.ListOf(Any.Species()).WithCount(5).Distinct()` gives five
  species that `Culture` really considers different (equality by name); and `AnyPattern.DifferentFrom/Except` for
  "another species than these".
- **`[assembly: Reproducible]` and the seed in the failure output**: the one red run on a drawn value printed
  `[JustDummies] These arbitrary values were seeded with -1087769823. Reproduce this run with
  [JustDummies.Xunit.Reproducible(Seed = -1087769823)].`; pinning that seed on the method gave the identical failure at once and
  let me add diagnostics to the assertion until the cause was clear. That is the library's promise, kept.
- **The draw found a real defect** (facilitation numerically blowing up) that no literal I would have typed
  (a species of comparable yield) reveals. Documenting it as a skipped test plus a sibling with a pinned
  comparable-yield facilitator kept the scenario covered.
- **Analyzers stayed quiet on ~380 cases of dense generator code** (no false positive), and fired as documented on the
  probes.
- Theories get one seed per case, and the entry point (`Any.Species()`) from `--entry-point any` compiled without
  fuss on net10.0 (C# 14).

## Fluency: 4 / 5
Once the SDK test-runner hurdle was cleared the loop was fast: scaffold, read the TODOs, complete, and the
tests read like the domain. The two points off are the places where the domain's rules are not per-parameter:
the ordered triple of temperatures and the two `&&` ties, where I had to design the base myself (disjoint
windows, a pin-detecting `Generate()`) with no documented pattern that keeps pins honest, plus the helper-guard
idiom that left five plain ranges to copy by hand. Nothing in the library misled me; everything I tripped on is
either environment (the MTP opt-in) or a documented limit of what a scaffold can know.
