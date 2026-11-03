# Discovery notes — 43 passive components
Agent: the same model and effort as every session of this trial. Library: JustDummies 1.0.0-uxtrial.1 (packed from main).

## What I did
Pages read, in order: `docs/PROJECT-README.md`; `guides/getting-started.en.md`; `packages/justdummies-cli.en.md` (the
scaffolder, including "what the last column means", guards, "Adjusting a parameter", "When a guard ties two parameters");
`guides/core-concepts.en.md`; `packages/justdummies-xunit.en.md`; `generators/README.md` and `generators/numbers.en.md`;
`guides/composition.en.md`; `generators/strings.en.md` (patterns) and `generators/enums-and-choices.en.md`.
I never needed errors-and-conflicts, reproducibility, the FAQ or an analyzer page.

`any` commands run (from `Tests.Discovery/`, after a first `dotnet build` of the empty project):
- `any generate Capacitance Inductance Resistance Tolerance TemperatureCoefficient PreferredValue MarkingCode ReferenceDesignator Part --output Generators --entry-point any`
- `any generate BillOfMaterials Placement BomLine --output Generators --entry-point any`

Generators completed by hand:
- `AnyPreferredValue`: the `index` base became `Between(0, 5)` (valid for every series; E6 has 6 values) instead of the TODO.
- `AnyReferenceDesignator`: `Any.StringMatching("[CLR][1-9][0-9]{0,3}")`, retyped `AnyString` to `AnyPattern` (field, constructor, `Func`).
- `AnyMarkingCode`: `Any.StringMatching("[1-9][0-9][0-4]|[1-9]R[1-9]")` (a code every kind reads), same retyping.
- `AnyBillOfMaterials`: two patterns (trimmed name of 1..64 characters, `[A-Z]{1,2}`), same retyping.
- `AnyPart`, `AnyPlacement`, `AnyBomLine`: rewritten by hand. The constructor ties kind to package, to the value range and to the rating
  range (which itself depends on the package), so `Generate()` draws the kind first and builds the rest from it. Placement derives the
  designator letter from the drawn part's kind; BomLine draws distinct numbers with `Any.SetOf`.
- `Capacitance`, `Inductance`, `Resistance`, `Tolerance`, `TemperatureCoefficient`: kept exactly as scaffolded.
- `TestParts.cs`: helpers that pin the specification a test is about (`TestParts.Resistor("RES-100", 100, 5, 0.1, 100, Package.Chip0805)`)
  and leave the unit cost drawn.

Tests written (263 methods, 527 cases, 4 skipped as domain defects), one file per type:
- `CapacitanceTests`, `InductanceTests`, `ResistanceTests`: range boundaries and refusals (NaN, infinities), unit constructors, series and
  parallel combination (at least two, no null, result out of range), reactance, resonant frequency, time constant and cutoff, relative
  closeness (default and custom tolerance, tolerance outside (0, 1)), exact equality, ordering, `ToString` prefixes.
- `ToleranceTests`, `TemperatureCoefficientTests`: boundaries, `ForSeries`, band bounds, tightness, drift at -55/25/125/155 degrees and its refusals.
- `PreferredValueTests`: values per decade, mantissas, constructor refusals per series, `Nearest` (log scale, decade crossing, both ends
  refused), `IsPreferred` (relative 1e-9, outside the decades), `Next`/`Previous` across decades and at both ends, an exhaustive walk of
  all four series over all 26 decades (every value is preferred and is its own nearest), equality/ordering, `ToString`.
- `MarkingCodeTests`: numeric and letter notations per kind, four-digit resistor codes, every refusal family (shape, wrong letter for the
  kind, zero, out of range, null, undefined kind), `For` (usual codes, resistor fallback, refused values), an exhaustive E24 round trip,
  `DenotesSameValueAs`, equality.
- `ReferenceDesignatorTests`: valid and invalid shapes, kind and number, ordering R2 < R10, equality.
- `PartTests`: every constructor rule (part number, ranges per kind, package fit, rating per kind and package, unit cost, scale of the
  cost), `RangeAt`, `AsX`, `HasPreferredValueOf`, equality on every field, and the substitution rule one refusal at a time, in order,
  with the relative 1e-9 slack.
- `PlacementTests`, `BomLineTests`: kind/letter match, null/duplicate/empty refusals, sorting, `ExtendedCost`.
- `BillOfMaterialsTests` (construction, placement, release, substitution, reports): name and revision rules, placing/removing, the
  part-number consistency rule, release freezing, substitutions judged against the designed part (including the chained-drift case),
  `SubstitutionRefusedException`, `Lines`, `TotalCost`, `CountOf`, `OffSeries`, `FindByMarking`, time constant and resonant frequency
  with both tolerance bands.
- `SmokeTests`: 500 draws of every generator, to prove each one only builds values its constructor accepts.

Result: green (523 passed, 4 skipped). Then 15 more runs with `--no-build`, each under a fresh seed (`[assembly: Reproducible]`):
15 / 15 green, same counts. The 4 skips are the one domain defect below.

## Frictions and surprises, in order
1. **`dotnet test --project Tests.Discovery` does not work out of the box on the .NET 10 SDK.**
   Command: `dotnet test --project Tests.Discovery` from the domain folder. Output: `MSBUILD : error MSB1001: Unknown switch.` ...
   `Switch: --project`. I expected the documented command to work (the README only shows `dotnet test --solution JustDummies.sln` for the
   repository, nothing for a consumer). `--project` only exists in the Microsoft.Testing.Platform mode of `dotnet test`; the xunit.v3
   test project is MTP-capable but the SDK defaults to the VSTest mode. I found the fix by reasoning, not from the docs: a
   `global.json` containing `{ "test": { "runner": "Microsoft.Testing.Platform" } }` at the workspace root. Nothing in the documentation
   (xunit adapter page included) mentions this, and a newcomer on .NET 10 hits it before running a single test.
2. **The scaffold was useful immediately, but the first build already failed by design.** After `any generate` for nine types, the build
   stopped on `TODO_verify_the_generator_for_*` for PreferredValue, MarkingCode, ReferenceDesignator and Part. I expected that after
   reading the CLI page ("The file will not compile until you resolve it. That is deliberate."), and the recap tells which guard it
   could not read. Fine; but `✓ AnyPart.cs — 8 of 8 parameters inferred, 5 to verify.` reads as "all done" at first glance.
3. **Regex guards: the retyping is manual and spread over four places.** `ReferenceDesignator`, `MarkingCode`, `BillOfMaterials`: the
   TODO says "retype AnyString to AnyPattern here, on the recipe field, in the recipe constructor and in the Func of With…". I did it
   with a search-and-replace of `AnyString` by `AnyPattern` in each file. It worked at the first compile, but it is the one step of the
   scaffold where I edited generated code mechanically; an `--pattern value=...` option or an `AnyPattern`-returning default would save it.
4. **A pattern that is valid for the domain is not the same as the pattern of the guard.** The constructor's regex for `MarkingCode`
   accepts codes that some kinds then refuse (inductor `997` is 990 H). The scaffold cannot know this; I had to read the whole
   constructor to find a safe subset (`[1-9][0-9][0-4]|[1-9]R[1-9]`, valid for all three kinds). The docs say "verify against the
   guard" but give no help for a pattern whose validity depends on another parameter.
5. **The scaffolded `Between(Min, Max)` on a 25-decade quantity is almost never anything but the top decade.** The tool wrote
   `Any.Double().Between(Resistance.MinOhms, Resistance.MaxOhms)` and the recap said `guard`. I measured 20 000 draws of
   `Any.Resistance()` (temporary test, deleted): decades 8:23, 9:184, 10:1831, 11:17962, and nothing below 1e8. This is exactly what
   `numbers.en.md` documents ("uniform in value ... half the draws land below the midpoint"), but I read that page only after
   scaffolding. For a domain whose whole point is relative comparison across decades, a dummy that is always 1e11 Ω exercises
   little. I kept the scaffold for the primitives (the tests that care use literals) and did not write a log-uniform generator; the
   recap does not hint that a range spanning many decades is drawn by value.
6. **Dependent ranges are the hard case, and the docs' four forms did not quite cover it.** `Part` ties kind to package, to the value
   range, and to the rating range (which depends on the package). The CLI page's "When a guard ties two parameters" shows ordered
   pairs, a start plus a duration, a check digit and a `DifferentFrom`. I wanted "the range of B depends on the drawn A" and
   "keep `WithNominalValue(Func<AnyDouble, AnyDouble>)` working". The rewrite (~200 lines): `Generate()` draws the kind, then builds the
   `AnyDouble` base for that kind and applies the stored refinement `Func`s. It works, but I had to give up the generated shape
   (a recipe field per parameter) for stored `Func<AnyDouble, AnyDouble>` fields. A short documented pattern for "a refinement applied
   to a base that depends on another draw" would have helped; `.Then(...)` exists but returns an `IAny<T>`, which cannot be refined.
7. **Pinning a package against a drawn kind.** `WithPackage(Package)` on my `AnyPart` is `_packageRecipe.OneOf(value)`; combined with the
   kind-based `Except(...)` it fails at `Generate()` with a conflict naming both sides if the kind is also drawn and incompatible. I
   pinned `WithKind(...)` in every helper that pins a package. The failure mode (a conflict, not a silently wrong value) is exactly
   what the docs promise, but I never saw the message itself because the helpers always pinned both.
8. **xUnit analyzer, not the library:** `Assert.Equal(1e-16, tau, 20)` gave `error xUnit2016: Keep precision in range [0..15]`. I switched
   to `Assert.InRange`. Noted only because the sub-1e-15 magnitudes of this domain make `Assert.Equal(double, double, precision)` useless.
9. **Domain, found by tests (two items, see findings):** `Capacitance.FromPicofarads(4.7)` is not `Equals` to `new Capacitance(4.7e-12)` (exact
   equality, documented; `IsCloseTo` is the comparison) and `MODEL.md` says `104` is "100 µH" for an inductor where the code reads
   0.1 H. I wrote the tests to the code and recorded the discrepancy.
10. **Domain defect, trailing line break accepted.** `new ReferenceDesignator("R1\n")` does not throw; same for `new MarkingCode("4R7\n", Resistor)`,
    `new Part("ABC\n", ...)` and `new BillOfMaterials(name, "A\n")` (`$` matches before a final `\n` in .NET). Four tests are kept and
    marked `Skip = "domain defect: ..."`. They are literal inputs, so there is no seed.
11. **No analyzer diagnostic fired in the whole project.** I expected to learn the rules from them; the build output had no JD warning.
    The `Info`-level ones (JD030 on a scaffolded `Any.String()`) are hidden, so a newcomer does not see that the analyzers are working.
    I could not tell whether that means "nothing to say" or "not running".

## What worked well
- The scaffold read the constructors well: it turned `farads < MinFarads || farads > MaxFarads` into `Between(Capacitance.MinFarads, Capacitance.MaxFarads)`
  with the constants kept by name, found the `decimal.Round(unitCost, 4)` guard (`.WithScale(4)`), and composed `Tolerance` and
  `TemperatureCoefficient` through the generators it had just written (`new AnyTolerance()`).
- Every un-inferable guard was named at its line, with the first unread guard quoted ("ties package to kind, and 1 more"), and the
  build stopped there: I could not forget a TODO.
- `--entry-point any` gives `Any.Capacitance()`, so the arrange lines read like the docs (`Any.Part().WithKind(...).WithPartNumber("RES-100")...`).
- `With…(value)` / `With…(Func)` is a good vocabulary for tests: pinned values are the specification, the rest stays drawn (unit cost,
  part number when it is not compared).
- `[assembly: Reproducible]` is one line; the failing-test output carried `[JustDummies] These arbitrary values were seeded with ...
  [JustDummies.Xunit.Reproducible(Seed = ...)]`, in the right place. I used it three times on my own mistakes (a wrong expected value,
  a part rating invalid for its package) and the message pointed at the draw.
- The generator for `AnyPart` threw a useful error when my own test asked for something impossible: `A resistor in Chip0805 is rated in
  [0.01, 0.125] W. (Parameter 'rating')`, with the stack inside `AnyPart.Generate()`. The domain message did the work.
- `Any.SetOf(...).WithCountBetween(...)` made distinct designators trivial.
- The getting-started rule ("if an `anyXxxx` reaches your assertion it is not a dummy") told me where to stop: all exhaustive claims
  (every preferred value is its own nearest, every E24 resistor value round-trips through `MarkingCode.For`) are plain loops, not draws.

## Fluency: 4 / 5
The path from README to a first green test was short, the scaffolder saved real time on nine value objects, and the rules ("pin what the
test is about, draw the rest") shaped my tests in a way I liked. I lost the most time on three things the docs do not carry: running
the suite on the .NET 10 SDK (`--project` needs the MTP runner), the dependency between constructor parameters (kind, package, rating),
which means rewriting the generator by hand, and the by-value draw of wide ranges, which I only understood from the numbers page after
the fact. None blocked me; each cost a detour.
