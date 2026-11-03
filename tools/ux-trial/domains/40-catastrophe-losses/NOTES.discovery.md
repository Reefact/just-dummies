# Discovery notes — 40 Catastrophe losses
Agent: the same model and effort as every session of this trial. Library: JustDummies 1.0.0-uxtrial.1 (packed from main).

## What I did
- Pages read, in order: `docs/PROJECT-README.md`, `docs/README.md` (index), `guides/getting-started.en.md`,
  `guides/core-concepts.en.md`, `packages/justdummies-cli.en.md` (all of it: it is the page I needed most),
  `packages/justdummies-xunit.en.md` (first 150 lines), `generators/collections.en.md` (to find how to draw an
  ordered list), and the "Floating point" section of `generators/numbers.en.md`. I did not need the analyzer
  pages: no `JD0xx` diagnostic ever appeared in the build output (Info-level ones do not print).
- `MODEL.md` and the 10 domain classes were read before scaffolding.
- Project `CatastropheLosses.Tests.Discovery.csproj` from the template, plus a `W/global.json` (see friction 1).
- `any` commands run (one call, all ten types, entry points requested):
  `any generate GeoPoint Footprint VulnerabilityCurve InsuredProperty PolicyTerms Exposure CatastropheEvent Portfolio LossDistribution CatastropheModel --output ./Generators --entry-point any`
  Result: 10 generators plus 10 `Any<Type>.Entry.cs`. 3 compiled as is (GeoPoint, Footprint, PolicyTerms); 7 stopped the
  build on a `TODO(any)` sentinel (VulnerabilityCurve, InsuredProperty, Exposure, CatastropheEvent, Portfolio,
  LossDistribution, CatastropheModel).
- Generators completed by hand:
  - `InsuredProperty.id` -> `Any.StringMatching("PRP-[0-9]{6}")`, retyped `AnyString` -> `AnyPattern` (field, recipe
    ctor, Func overload) with `sed`; same for `CatastropheEvent.id` (`EV-[0-9]{4,8}`) and `Portfolio.currency` (`[A-Z]{3}`).
  - `InsuredProperty.insuredValue` -> `Any.Decimal().Between(1_000m, 50_000_000m).WithScale(2)` (a domain range I
    declared myself; the scaffold only said `Positive()`).
  - `VulnerabilityCurve`: intensities `ListOf(Double().Between(0,100)).WithCountBetween(2,6).Ordered().Distinct()`;
    the ratios are derived inside `Generate()` (first 0, then an ordered list of `count - 1` values in [0, 1]).
  - `Exposure`: terms derived inside `Generate()` from the drawn property (deductible in [0, value/2], limit in
    [value/10, value]).
  - `LossDistribution`: losses in [0, 1e9] scale 2, 1 to 200 years. `CatastropheModel`: 1 to 5 events,
    `.DistinctBy(e => e.Id)`.
- Tests written (`Tests.Discovery/`, 11 files, 130 `[Fact]`/`[Theory]` methods, 225 cases, 4 skipped):
  GeoPoint (10 methods), Footprint (10), VulnerabilityCurve (15), InsuredProperty (9), PolicyTerms (6),
  Exposure (12), CatastropheEvent (8), Portfolio (17), LossDistribution (19), CatastropheModel (20),
  GeneratorTests (4: 300 draws of each completed generator). `[assembly: Reproducible]` on the assembly.
- Result: green, 221 passed / 4 skipped (domain defects below). First run after the full suite existed: 1 failure,
  my own test (volatility "spreads" the losses: wrong, the severity is clipped by the curve at both ends, so the
  deviation is not guaranteed to grow; I rewrote it to say what a run can show). Then 13 more runs, fresh seeds each
  time (3 + 10), all green.

## Frictions and surprises, in order
1. **`dotnet test --project Tests.Discovery` does not work out of the box.** Exact output:
   `MSBUILD : error MSB1001: Unknown switch. ... Switches appended by response files: Switch: --project`.
   SDK 10.0.114 runs `dotnet test` in VSTest mode unless told otherwise. I created `W/global.json` with
   `{ "test": { "runner": "Microsoft.Testing.Platform" } }` and the same command worked. Nothing in the JustDummies
   docs (nor the xUnit adapter page) mentions it; a newcomer copying the xunit.v3 4.0.2 template would hit it.
2. **PROJECT-README links do not lead anywhere in my copy.** The front page points to `doc/handwritten/for-users/...`;
   the docs folder I was given is flat (`docs/guides/...`). I found the index by listing the folder. (Probably an
   artefact of how the trial is staged; noted because the first click of a newcomer failed.)
3. **Expected "scaffold, build, test"; got a build stopped by seven sentinels.** `dotnet build` after `any generate`
   fails with CS0103 `TODO_verify_the_generator_for_...` in 7 of 10 files. The docs had told me this would happen
   ("a TODO is not a failure") and the recap printed, per parameter, which guard was not read and where
   (`first unread guard at VulnerabilityCurve.cs, line 19`). That worked well; the cost is the work itself.
4. **Rules between parameters are mine to write, and the `With…` pin silently stops working when I derive.**
   `VulnerabilityCurve` (counts and ordering tied) and `Exposure` (deductible < value, limit <= value) are
   cross-parameter. The docs offer "derive one draw from another inside `Generate()`" and warn that the pin of the
   derived parameter is then no longer read. I did exactly that. Consequence: `Any.Exposure().WithTerms(x)` and
   `Any.VulnerabilityCurve().WithDamageRatios(x)` still compile, take the argument, and ignore it. No compiler warning,
   no analyzer. I worked around it by building those two objects with their real constructors in `Samples.cs`
   when a test needs a particular value. I expected the tool, having flagged "ties terms to property", to at least
   leave the pin honoured or emit a comment on `WithTerms`.
5. **Retyping `AnyString` to `AnyPattern` is a manual, multi-site edit.** Documented precisely (field, recipe ctor,
   `Func<…>` overload, factory) and I did it with `sed -e 's/AnyString\b/AnyPattern/g'` on files where `id` was the only
   string, by hand on `Portfolio` (which also has a name). Fine once, tedious for three regex-guarded ids; it is the
   most common guard in a domain with identifiers and the scaffold already knows the pattern (`"^PRP-\\d{6}$"` is quoted in
   the TODO). I wished the tool offered to apply the one-line replacement when the regex is inside the subset.
6. **Deleting the two TODO lines leaves a stray blank line** (`private static AnyPattern AnyValidId() {` then an empty line
   then `return`). Cosmetic.
7. **Unbounded numeric scaffolds are valid but not sensible.** `Any.Decimal().Positive()` for an insured value is
   correct per the constructor, but one draw in ten reaches `decimal.MaxValue`-scale values, which would overflow the
   portfolio sums. The numbers page explained why (nine in ten ordinary, the tenth at the limit), so I declared
   a range, as it recommends. I did not hit a failure from it; I wrote it before running anything.
8. **`LossDistribution` annualLosses**: the scaffold is `Any.ListOf(Any.Decimal()).NonEmpty()` marked `to verify`
   because of `annualLosses.Any(loss => loss < 0m)`, a lambda shape the tool does not read. Correct to flag; it
   is the one place where I would have liked the tool to read the lambda (it is the same `>=0` guard as the others).
9. **A drawn value in an assertion.** I wrote two tests (`The_intensity_felt_never_exceeds_the_peak_for_this_location`,
   `The_insured_loss_never_exceeds_the_limit_for_this_event`) where the drawn footprint/exposure reaches the assertion,
   which the core-concepts page calls a property run with a sample of one. I kept them, named for what one run
   shows ("for this ..."), but I am not sure a reviewer would call them dummies. The docs are clear; the temptation is
   real in a numeric domain where invariants are the easiest assertions.
10. **Domain defects revealed by literal probes (not by a draw)**, recorded and skipped, see the findings:
    `"PRP-123456\n"`, `"EV-1234\n"` and currency `"USD\n"` are accepted ('$' matches before a final line feed), and
    `"PRP-١٢٣٤٥٦"` (Arabic-Indic digits) is accepted because `\d` is Unicode in .NET. Seeds in the Skip messages are the
    ones the runs printed (the inputs are literals, so any seed reproduces them).
11. My own mistake, caught by the draw-free run: I had invented `seed 1` in the first Skip text; I reran the tests
    unskipped to read the real seed from the reproduction line, which is quick (`[JustDummies] These arbitrary values were
    seeded with 672400060. Reproduce this run with [JustDummies.Xunit.Reproducible(Seed = 672400060)]`).

## What worked well
- The scaffolder: 10 types in one command, guards turned into ranges (`Between(-90d, 90d)`, `LessThanOrEqualTo(Footprint.MaxRadiusKm)`
  referencing my public constants), composed types drawn through the generator their type owns, and an honest
  `to verify` for what it could not read, each with the file and line of the guard.
- `--entry-point any` makes arrange blocks read uniformly (`Any.GeoPoint()` next to `Any.Double()`); with .NET 10 no setup was needed.
- `With…(value)` and `With…(lambda)` as documented: `Any.CatastropheEvent().WithId("EV-0042").WithFootprint(...)` states exactly what the test is about.
- `Any.ListOf(...).Ordered().Distinct()` and `.DistinctBy(...)` solved the strictly-increasing intensities and the unique event ids with no custom code.
- `[assembly: Reproducible]` is one line and the failure output carries the seed and the exact attribute to paste.
- A genuinely useful dummy: the `seed` parameter of `Simulate` (`Any.Int32()`), whose value the test must not care about while it needs
  the same value twice ("same seed, same distribution").
- Contradiction messages and docs on "values are built, not filtered" meant the cross-field generators never needed a retry.

## Fluency: 4 / 5
The path from front page to a running suite was short and the tool did most of the typing; the pages I needed
(CLI, composition of tied parameters, numbers) answered what I asked without guesswork. Points off for the
environment friction around `dotnet test --project`, for the manual retyping dance for regex ids, and above all for
the two `With…` methods that compile and do nothing after I derived a parameter inside `Generate()`.
