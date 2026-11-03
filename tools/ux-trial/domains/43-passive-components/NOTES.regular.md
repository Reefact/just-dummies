# Regular notes — 43 passive components
Agent: the same model and effort as every session of this trial. Library: JustDummies 1.0.0-uxtrial.1 (packed from main).

## What I did
**Pages read, in order:** `PROJECT-README.md` (skimmed), `guides/getting-started.en.md`, `packages/justdummies-cli.en.md`
(what the last column means, the guard table, graph of aggregates, entry point, adjusting a parameter, tie between two
parameters, options), `packages/justdummies-xunit.en.md`, `generators/numbers.en.md`, `generators/enums-and-choices.en.md`,
`generators/strings.en.md` (layout, patterns, narrower-pattern recipe), `generators/collections.en.md`, and from
`guides/composition.en.md` the `Combine`, `Fixed`, `DifferentFrom`, `Then` sections. Domain: all of `Domain/` plus `MODEL.md`.

**`any` commands run** (from `Tests.Regular`, after `dotnet restore`):
`any generate Capacitance Inductance Resistance Tolerance TemperatureCoefficient PreferredValue MarkingCode ReferenceDesignator Part Placement BomLine --output Generators --entry-point any`
then `any generate BillOfMaterials --output Generators --entry-point any`. Twelve generators and twelve `Any.Xxx()` entry points.
Result: 5 value objects fully inferred with `guard`; PreferredValue, MarkingCode, ReferenceDesignator, Part, Placement, BomLine,
BillOfMaterials stopped the build with `TODO_verify_…` on purpose.

**Generators completed by hand** (`Tests.Regular/Generators/`):
* `AnyCapacitance/Inductance/Resistance`: scaffold was `Any.Double().Between(Min, Max)`; I replaced the base with a log-uniform
  helper (`Magnitudes.AcrossDecades`, decade then 3 digits, built by `double.Parse` so powers of ten are exact) and dropped the
  refining lambda (it needs an `AnyDouble`).
* `AnyReferenceDesignator`: `Any.StringMatching("[CLR][1-9][0-9]{0,3}")`, retyped `AnyString` -> `AnyPattern` as the TODO said,
  plus a neighbouring file with a named recipe `OfKind(ComponentKind)` (the doc's `French()` pattern).
* `AnyPreferredValue`: the index depends on the series; kept `WithIndex(int)` as a pin and drew the index in `Generate()`
  (`Between(0, valuesPerDecade(series) - 1)`); removed the refining overload for `index`.
* `AnyMarkingCode`: code depends on the kind; `Generate()` draws the kind, then a code from a per-kind `Any.StringMatching(...)`
  (numeric and letter notations, inductor multipliers capped at 4 because the count is in µH); `WithCode` pins.
* `AnyPart`: `partNumber` -> `AnyPattern`; kind drawn first, then package (not 2512 for C/L, not Radial for R), nominal value
  (per-kind range, log-uniform) and rating (per kind and package) drawn in `Generate()`; pins kept, refining overloads removed
  for those three. A neighbouring file adds `Capacitor()/Inductor()/Resistor()`.
* `AnyPlacement`, `AnyBomLine`: part drawn first, designator(s) derived from its kind (`OfKind`), distinct 1..6 for a line.
* `AnyBillOfMaterials`: name `[A-Za-z0-9]([A-Za-z0-9 -]{0,62}[A-Za-z0-9])?`, revision `[A-Z]{1,2}`, both retyped to `AnyPattern`.

**Tests written** (`Tests.Regular/Tests/`, 297 test methods, 593 cases): CapacitanceTests, InductanceTests, ResistanceTests
(range ends, refusals incl. NaN/inf, unit constructors, series/parallel incl. exact-boundary results, null/short/null-element
combinations, reactance, resonance, RC, closeness with relative tolerance, equality/order/ToString), ToleranceTests,
TemperatureCoefficientTests, PreferredValueTests (mantissas, ctor bounds per series, Nearest incl. log-scale ties-by-ratio and
decade edges, IsPreferred relative 1e-9, Next/Previous across decades and at both ends, equality across series),
EverySeriesValueTests (exhaustive walks over 26 decades x 4 series: self-snap, preferred, Next/Previous chain, geometric mean,
MarkingCode.For round trip), MarkingCodeTests (numeric/letter notation per kind, wrong letters, shapes, zero, range, `For`,
`DenotesSameValueAs`), ReferenceDesignatorTests, PartTests (every constructor rule, rating per package, cost, band, drift,
conversions, equality field by field), SubstitutionRuleTests (each refusal in rule order, relative 1e-9 slack), Placement/BomLine
tests, BillOfMaterials tests (construction, placement, removal, lines, substitution against the designed part, chains, revert,
release, off-series, marking search, RC and LC across bands).

**Result:** green on `dotnet test --project Tests.Regular`: 593 total, 588 passed, 5 skipped (all five are the same domain
defect, see frictions 9). Then 14 further runs, each with fresh seeds (`[assembly: Reproducible]`): 14 / 14 green, 0 failures.

## Frictions and surprises, in order
1. **`dotnet test --project Tests.Regular` does not work out of the box.** First `dotnet test --project .` gave
   `MSBUILD : error MSB1001: Unknown switch. ... --project`, then `dotnet test Tests.Regular` gave
   `Microsoft.Testing.Platform.MSBuild.targets(395,5): error : Testing with VSTest target is no longer supported by
   Microsoft.Testing.Platform on .NET 10 SDK and later. If you use dotnet test, you should opt-in to the new dotnet test experience.`
   I created `domains/43-passive-components/global.json` with `{ "test": { "runner": "Microsoft.Testing.Platform" } }`, after
   which `--project` works. Neither the xunit page nor getting-started mentions it; xunit.v3 + .NET 10 SDK needs it. Expected the
   documented install line to be enough.
2. **`any generate` before restore**: `✗ Capacitance: this project has not been restored, so its package references cannot be read.
   Restore it, then run the command again: dotnet restore` (exit 2). Clear message, one extra step; the CLI page does not list
   "restored" among the requirements.
3. **The scaffolded recipe for a decade-spanning double is uniform in value.** The tool wrote
   `Any.Double().Between(Capacitance.MinFarads, Capacitance.MaxFarads)` (1e-13..1e-2, good: the guard constants are referenced).
   Sampling 1000 draws of that recipe: decades -13..-2 gave `0,0,0,0,0,0,0,0,9,92,899`, i.e. 90 % in the last decade and nothing
   below 1e-6, so the 25-decade domain (E-series snapping, marking codes, pF..mF) is never exercised by the default. numbers.en.md
   says it ("uniform over the interval's length") but gives no way to draw across decades. I wrote my own `Magnitudes.AcrossDecades`
   with `Any.Combine(Any.Int32(), Any.Int32(), ...)`; as it returns an `IAny<double>`, I lost the scaffold's refining lambda
   (`Func<AnyDouble, AnyDouble>`) on the three value objects. A log-scale option (or a `Decades(...)` constraint) is the missing piece.
4. **Rules that depend on another parameter cost the lambda overloads.** `Part` (package fits kind; nominal range and rating depend on kind
   and package), `MarkingCode` (codes depend on kind), `PreferredValue` (index range depends on series), `Placement` and `BomLine`
   (designator letter depends on part). The tool detected them correctly ("ties package to kind; and 1 more", "ties designator to
   part") and blocked the build, which I liked. But the only way out is to hand-edit `Generate()`, keep a pin and delete the
   `Func<…>` overload of the dependent parameters (a lambda cannot know the kind). I used a "null/pinned flag" idiom
   (`_ratingPinned`) in six places; it is not described in the doc (the doc's "Derive one draw from another inside Generate()" says
   a pin no longer reaches the draw, but not how to keep it working). A dependent-draw helper in the scaffold would have saved an hour.
5. **Two overloads make pinning `null` ambiguous.** `Any.Part().WithPartNumber(null!)` -> `error CS0121: The call is ambiguous between
   'AnyPart.WithPartNumber(string)' and 'AnyPart.WithPartNumber(Func<AnyPattern, AnyPattern>)'`. Cast `(string)null!` fixes it. Same with
   `WithTolerance((Tolerance)null!)`. Small, but it hits exactly when testing the null refusals.
6. **`to verify` on a regex guard: the retype checklist is long but accurate.** The TODO said "retype AnyString to AnyPattern here, on the recipe
   field, in the recipe constructor and in the Func of WithPartNumber". A global replace did it in three files (Part, BillOfMaterials,
   ReferenceDesignator) and it compiled the first time. Afterwards the `Func<AnyPattern, AnyPattern>` overload is nearly useless (only
   `OneOf/Except/DifferentFrom`), but the "named recipe" (`OfKind`) idea from strings.en.md fitted the designator perfectly.
7. **A dependent range the tool cannot read is silently `Any.Double()` with a TODO.** For `Part.rating`/`nominalValue` the inferred base is
   `Any.Double()` (finite, within 1e6 nine times in ten): harmless because the TODO blocks the build, but the recap says "unread guard"
   without telling which of the three tables (kind range via `MarkingCode.RangeOf(kind)`) applies. Fine for me, perhaps less for a newcomer.
8. **Pinning a value the test depends on: the draw told me.** My first `Specs.Inductor(...)` did not pin tolerance; the substitution test
   `A_part_judged_on_its_current` failed with `Expected: None / Actual: ToleranceBandNotContained` on a drawn ±x %. The tolerance was
   not a dummy for that rule; I pinned it (and the temperature coefficient) in the `Specs` helpers. The reported seed line was clear.
9. **The domain accepts a trailing newline** (see findings): `new ReferenceDesignator("R12\n")`, `new Part("ABC\n", …)`, `new BillOfMaterials("x", "A\n")`
   and letter marking codes `4R7\n` / `R47\n` are all accepted, because `Regex.IsMatch("^…$")` lets `$` match before a final `\n`.
   `new MarkingCode("104\n", Resistor)` throws `ArgumentOutOfRangeException` instead of `FormatException` (the newline is read as digit
   '\n' - '0'). I kept the tests, marked `Skip = "domain defect: …"` (5 methods; inputs are literals so there is no seed). Not found by a draw:
   I wrote the literal myself, because `Any.String()` over printable ASCII rarely ends in `\n` and the pattern generators never emit one.
10. **MODEL.md arithmetic slip, not code**: "`104` = 100 nF / 100 µH / 100 kΩ". By the same rule an inductor `104` is 10 × 10⁴ µH = 0.1 H (100 mH);
    the code does that, my first test expected 100 µH (`Expected: 0.0001 / Actual: 0.10000000000000001`). I fixed my test and noted the model text.
11. **My own arithmetic in tests** caused three red runs (E96 neighbours of 1.04, E24 neighbours of 4.9, E96 off-series list). Not the library;
    the failure output (`Expected: E96 1.02e0 / Actual: E96 1.05e0`) made them quick to fix.
12. **`Any.Enum<…>()` etc. produced no analyzer noise at all.** I expected at least JD030 or JD031 hints; the build stayed at 0 warnings.
    (They are Info and do not print; the docs say so.) So I never saw an analyzer help or hinder.

## What worked well
* `any` read the guards through `Math`-free constant comparisons: `Between(Capacitance.MinFarads, Capacitance.MaxFarads)`, `Between(PreferredValue.MinDecade, …)`,
  `Between(0m, Part.MaxUnitCost).WithScale(4)` came out right, referencing my constants rather than copying numbers.
* The tool flagged ties between parameters (package-kind, designator-part) and delegated validation helpers honestly instead of guessing; the
  line numbers and quoted guards in each TODO pointed at the right statement every time.
* `--entry-point any` gave `Any.Part().Resistor().WithPackage(…).Generate()`; tests read nicely, and the constructor is the real one so each
  "refusal" test is `Any.Part().X().WithY(bad).Generate()` with everything else drawn.
* `[assembly: Reproducible]` plus the seed line printed on every failure (`Reproduce this run with [JustDummies.Xunit.Reproducible(Seed = …)]`).
* `Any.StringMatching` handled the three domain formats (part number, designator, revision) in the regular subset on the first try.
* The doc's "named recipe" idea (`OfKind`) and the "draw the body, derive the rest" advice transferred without surprises.

## Fluency: 4 / 5
Daily DSL use (`Any.Part().Resistor().WithRating(...)`, `Reproducible`, patterns, `Any.Enum`) was fluent and the scaffolder saved real time on twelve types,
with an honest stop on every guard it could not read. I lost time on three things that are outside the happy path of the docs: the `global.json`
needed for `dotnet test --project` on the .NET 10 SDK, the lack of any log-scale draw for a domain over twenty-five decades, and the hand-edits required
for every parameter whose valid range depends on another (kind-dependent ranges), where the generated lambdas become useless and I had to invent a pin-flag idiom.
