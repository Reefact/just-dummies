# Expert notes — 43 passive components
Agent: the same model and effort as every session of this trial. Library: JustDummies 1.0.0-uxtrial.1 (packed from main).

## What I did

**Pages read, in order:** `PROJECT-README.md`, `guides/getting-started`, `guides/core-concepts`, `packages/justdummies-cli`
(whole page, including "Adjusting a parameter" and "When a guard ties two parameters"), `packages/justdummies-xunit`,
`generators/README`, `generators/numbers`, `generators/strings` (layout, patterns), `generators/enums-and-choices`,
`guides/composition`, `guides/errors-and-conflicts`, the top of `guides/reproducibility`, `analyzers/README`.
Then `MODEL.md` and every file of `Domain/` (1 500 lines): I read the code to find where a reviewer would look for defects.

**`any` commands run** (from `Tests.Expert`, after `dotnet restore`):

```
any generate Capacitance --dry-run
any generate Capacitance Inductance Resistance Tolerance TemperatureCoefficient PreferredValue MarkingCode \
    ReferenceDesignator Part Placement BomLine BillOfMaterials \
    --output Generators --namespace PassiveComponents.Domain.Tests.Expert.Generators --entry-point any
```

12 types, 24 files (`AnyX.cs` + `AnyX.Entry.cs`), so that tests read `Any.Part()`, `Any.Capacitance()`.

**What the tool inferred alone:** `Capacitance`, `Inductance`, `Resistance`, `Tolerance`, `TemperatureCoefficient`
(`Any.Double().Between(Capacitance.MinFarads, Capacitance.MaxFarads)` — it read through `!double.IsFinite(x) || x < Min || x > Max`
and referenced my public constants), and `unitCost` (`Between(0m, Part.MaxUnitCost).WithScale(4)`, from `decimal.Round(unitCost, 4) != unitCost`).
**What it stopped on** ("The file will not compile until you resolve it", 7 of 12 files): `PreferredValue`, `MarkingCode`,
`ReferenceDesignator`, `Part`, `Placement`, `BomLine`, `BillOfMaterials`.

**Generators completed or rewritten by hand** (`Generators/`):
- `AnyCapacitance/Inductance/Resistance`: base redrawn per decade (`LogScale.Between`), recipe retyped `IAny<double>`, refining overload removed (friction 4).
- `AnyPreferredValue`: the index range depends on the drawn series (6/12/24/96 values per decade): the index recipe is a `Func<ESeries, ...>`.
- `AnyMarkingCode`: a four-digit code is a resistor's and the multiplier must keep an inductor under 1 H, so the code pattern is chosen by the drawn kind.
- `AnyReferenceDesignator`: `Any.StringMatching("[CLR][1-9][0-9]{0,3}")` (retyped to `AnyPattern`), plus `WithKind` that restates the pattern for one letter.
- `AnyBillOfMaterials`: board name `[A-Za-z0-9]([A-Za-z0-9 ._-]{0,62}[A-Za-z0-9])?` (untrimmed names are refused), revision `[A-Z]{1,2}`.
- `AnyPart`: rewritten (kind and package drawn as a valid pair, nominal value per kind, rating per kind and package, part number ≥ 10 characters so two dummies never share one).
  Plus `PartArguments`, a record of the 8 constructor arguments, so that a test about one refusal does `valid with { Rating = ... }`.
- `AnyBomLine`: designators drawn as a set of the part's kind (distinct by construction).
- `AnyPlacement`: scaffolded, rewritten, then **deleted**: no test of the domain needs a dummy placement (BOM tests place explicitly, which is the behaviour under test).
- `Specimens` (test helper): `OriginalResistor()` / `OriginalCapacitor()`, the pinned starting points of the substitution tests.

**Tests written:** 318 methods (91 theories), 694 cases, in 11 files: `Capacitance`, `Inductance`, `Resistance`, `Tolerance`, `TemperatureCoefficient`
(range ends and just-outside, non-finite, unit conversions, series/parallel combinations and their out-of-range results, reactance/resonance/RC numbers,
relative closeness at several magnitudes, equality, ordering, text), `PreferredValue` (construction per series, `Nearest` on the log grid incl. the geometric-mean border,
refusals outside the decades, an exhaustive sweep of the 4 series × 26 decades for `Nearest`/`IsPreferred`, a walk with `Next()` from the bottom to the top, `Previous`),
`MarkingCode` (numeric/four-digit/letter notation per kind, multiplier digits 8 and 9, every refusal family, `For` incl. the 4.7 mF refusal, two sweeps `For` → `Value`, `DenotesSameValueAs`),
`ReferenceDesignator`, `Part` (every constructor rule, rating by kind and package, bounds, `RangeAt`, `As*`, the whole `CheckSubstitute` order with precedence and 1e-9 slack),
`Placement`/`BomLine`, and `BillOfMaterials` (construction, `Place`, `Remove`, `Substitute` incl. chained-substitution drift and part-number clashes, `CheckSubstitution`, `RevertSubstitution`,
`Release` and every refusal after it, `Lines` ordering/grouping, `CountOf`, `TotalCost`, `OffSeries`, `FindByMarking`, `TimeConstantSeconds`, `ResonantFrequencyHertz`).
`[assembly: Reproducible]`; `.editorconfig` raises the JustDummies "Constraints" category to warning so the Info rules print in the terminal (0 warnings).

**Result:** `dotnet test --project Tests.Expert` → 689 passed, 0 failed, **5 skipped** (5 domain defects, see below: `-0.0`, and the trailing line feed in four places),
then **12 further runs, all green** (fresh seeds each time; 689 passed / 5 skipped every time). I also confirmed that each skipped test fails when un-skipped.
(Earlier, while developing, 13 runs of the then-682 cases were green too.)

**Domain defects kept as skipped tests** (all deterministic, no seed needed): `TemperatureCoefficient(-0.0)` throws; the `$` of four regexes accepts a final `\n`
(`ReferenceDesignator "R12\n"`, part number `"ABC\n"`, revision `"A\n"`) and, for `MarkingCode("104\n", Resistor)`, the code is read with a multiplier of `'\n' - '0'` and fails as `ArgumentOutOfRangeException` instead of `FormatException`.

## Frictions and surprises, in order

1. **`dotnet test --project Tests.Expert` does not work out of the box.** Exact output:
   `MSBUILD : error MSB1001: Unknown switch. ... --project Tests.Expert -restore ... Switch: --project`.
   With .NET 10 SDK 10.0.114 the `--project` form exists only in Microsoft.Testing.Platform mode; the template's `xunit.runner.visualstudio` reference leaves `dotnet test` in VSTest mode.
   Fix I applied (outside `Tests.Expert`, inside W): `global.json` with `{"test":{"runner":"Microsoft.Testing.Platform"}}`. The xUnit adapter page and the README only say `dotnet test --solution` (a maintainer command) and never mention this.
   A first-time consumer reading only the user docs would meet it as soon as they follow the brief's command.

2. **`any` on a project that was not restored:** `✗ Capacitance: this project has not been restored, so its package references cannot be read. Restore it, then run the command again: dotnet restore`.
   The message is excellent and I fixed it in one step, but the CLI page does not list "restore first" among the prerequisites.

3. **Twelve types in one command, seven stop the build.** The recap is the best part of the tool: each `to verify` row names the guard and its line (`Part.cs, line 41: if (!double.IsFinite(nominalValue) || ...`).
   But `Part` (5 of 8 parameters), `PreferredValue`, `MarkingCode`, `Placement`, `BomLine`, `ReferenceDesignator`, `BillOfMaterials` all carry `_ = TODO_verify_the_generator_for_x;`, so the test project cannot compile until all of them are settled — I could not even run
   a first test against the 5 clean generators. I temporarily moved on by resolving the files one by one. Not a defect (it is the documented design), but with a whole domain scaffolded at once it blocks the loop; scaffolding in two commands (clean ones first) would have been smarter, which the recap does not suggest.

4. **A range spanning 25 decades, drawn "uniformly in value", is a one-decade generator.** `Any.Double().Between(Capacitance.MinFarads, Capacitance.MaxFarads)` is what the tool inferred, it is correct (every draw is valid), and the numbers page says so
   ("uniform over the interval's *length*"). Measured on 20 000 draws: 18 032 are above 1e-3 F (90 %) and **2 are below 1e-6 F**; no picofarad is ever met. For a domain whose whole point is the decades
   (relative comparisons, SI prefixes, `Magnitude.Format`) the scaffolded dummies cannot reveal a code that wrongly depends on the decade. JD036 stays silent (both bounds are declared), so nothing hints at it.
   What I did: `LogScale.Between(min, max)` = `Any.Double().Between(log10 min, log10 max).As(e => Math.Clamp(Math.Pow(10, e), min, max))`, then retyped the recipe `IAny<double>` (3 lines, as the CLI page prescribes) and
   **dropped the lambda-refining overload** since an `IAny<double>` has nothing to refine. After the change: 12 712/20 000 below 1e-6, 1 838 above 1e-3. A `.Logarithmic()`-style constraint on the numeric builders would have kept the scaffolded shape.

5. **Cross-field ties made me discard most of the scaffold for `Part`.** Kind decides the nominal-value range, kind + package decide the rating range, and some (kind, package) pairs are forbidden.
   The tool saw all of it (`kind: first unread guard ... Part.cs line 41 ... and 2 more`, `package: ... ties package to kind`) and refused to guess — correct. The documented way out ("Derive one draw from another inside `Generate()`") worked, but every
   scaffolded `With…(Func<AnyX, AnyX>)` for those parameters becomes meaningless, so I rewrote `AnyPart` from scratch (210 lines): a valid-pairs list, `WithKind`/`WithPackage` that filter it, `WithNominalValue`/`WithRating` that pin.
   It is the file the tests use 91 times, so it was worth it; but it is "the generator is yours" taken literally. The `PartArguments` record (8 arguments, `with`-able) is something I invented; the docs have nothing like it, and it is what made the refusal tests (`valid with { Package = Chip2512 }`) readable.

6. **The `ReferenceDesignator` / `MarkingCode` marks do not say "regex".** For `Part` and `BillOfMaterials` the comment quotes the pattern and gives the retype recipe
   (`a regex, "^[A-Z0-9][A-Z0-9.\\-/+#]{2,39}$": Any.StringMatching(pattern) replaces the base and returns an AnyPattern, so retype ...`).
   For `ReferenceDesignator` the same kind of guard (`Format().Match(value)` then `if (!match.Success)`) gets only `verify against ReferenceDesignator.cs, line 21: if (!match.Success) ... — then delete these two lines`, and the left-behind recipe is `Any.String().NotBlank()`.
   `MarkingCode` has two regexes (`NumericNotation().IsMatch`, `LetterNotation().Match`) and reads `code: first unread guard ... line 30: if (code.Length == 4 && kind != ComponentKind.Resistor) ...; and 2 more`. I knew what to do (I had read the domain), but a user who had not would not learn from the mark that a
   `[GeneratedRegex]` is involved, nor that `Any.StringMatching` fits. The build error does protect them (the sentinel), so this is only a missed teaching moment.

7. **Distinctness is on me.** Two dummy `Part`s must not share a part number (the board refuses "a different specification under the same number"), two dummy designators must not collide. Neither the library nor the tool knows. I made it true by construction
   (part numbers drawn 10–40 characters long: 36^10 possibilities; designators single, or literal when the position matters) instead of `DifferentFrom`, because `Any.StringMatching(...)` is an `AnyPattern` whose `DifferentFrom` I would have had to thread through
   `WithPartNumber(p => p.DifferentFrom(...))` at every site. It works; I just wanted to record that "the test cares that these two differ" showed up in almost every BOM test and the answer is always a generator design decision.

8. **My own tests failed on draws, and each time the draw was right** (seeds replayed with `[Reproducible(Seed = ...)]`; I also checked that a pinned seed reproduces the same values — `1.54 pF | U56#2P1D877-XUMEIE7R-4T7QBW-XVHH (91.112 Ω ±13.222 %, Chip0603)` came back identically twice):
   - seeds `172589783`, `2110602912` — `A_part_number_clashes_with_...`: `SubstitutionRefusedException ... TemperatureCoefficientWorse`. My helper pinned tolerance and value but left the temperature coefficient drawn, and the substitution rule reads it.
     The coefficient was not a dummy in a test that substitutes. I pinned it in the helper (comment above the helper says every field `Equals` and the rule read is pinned).
   - seed `1209010234` — `Two_equal_parts_built_apart_share_a_part_number_without_clashing`: two parts "built apart" with a drawn rating/package/coefficient are not equal parts, so the board rightly refused. Same fix: for equality every field is significant.
   - (A fourth, seed-free: I wrote `2.7690m` where the sum is `2.8190m`.)
   None of these is flaky; each moved a literal where the test cared. This is the loop the library promises and it cost two minutes each because the seed line is in the failure output.

9. **A dummy that reaches the assertion by identity.** `Assert.Same(part, placement.DesignedPart)` and `Assert.Same(resistor, line.Part)`: the test checks that the object it passed in is the object stored, which does not depend on the value of the part.
   The docs' rule ("if an `anyXxx` reaches your assertion it is not a dummy") makes me rename the variable `part` rather than `anyPart`, which feels like lying either way. I kept the draw (the part must be well-formed) and dropped the `any` prefix, with a comment.
   A sentence in *core concepts* about pass-through/identity assertions would settle it.

10. **Most scaffolded value-object generators found no honest use.** Of the 12, `AnyPlacement` (deleted), `AnyPreferredValue` and `AnyMarkingCode` ended with one use each (`CompareTo(null)`, `DenotesSameValueAs(null)`), because a test of a value object is about its value, so
    its input is a literal. The dummies live in the entity and the aggregate tests (`Any.Part()` 91 times, `Any.ReferenceDesignator()` 5, `Any.BillOfMaterials()` 4). That is the library's philosophy working as documented, and it is a data point for how much of a `any generate` run is worth keeping.

11. Smaller things that went fine but I wrote down: `Any.PermutationOf(IEnumerable)` accepted a LINQ projection and a tuple array; the C# 14 entry point `Any.Part()` worked on net10.0 at once; the documented `Any.StringMatching("[A-Z0-9][A-Z0-9.\\-/+#]{9,39}")`
    (escaped hyphen in a class, bounded quantifier) was in the supported subset; an `AnyPattern` has no `StartingWith`, so `WithKind` restates the pattern for one letter — exactly the "named recipe" the CLI page describes; no analyzer fired on my ~5 000 lines
    (I checked they are alive: a deliberate `Between(10, 1)`, a discarded `Positive()` and `Any.String()` gave JD014, JD006, JD030).

12. **Domain observations** (not library): see the list under "Result" and in the structured findings: `-0.0` temperature coefficient, `$` matching before `\n` in four places, `MODEL.md` says `104` = "100 µH" for an inductor while the code (correctly, µH counting) reads 10 × 10⁴ µH = **100 mH**,
    `MarkingCode.For(1e-3, Resistor)` is `"R0010"` (a trailing zero; `R001` is what is printed on shunts), and `PreferredValue.CompareTo` is 0 for the same value in two series while `Equals` is false (ordering inconsistent with equality).
    `CheckSubstitution` stays callable on a released bill (not in the model; I tested it as observed).

## What worked well

- **The recap table and the "unread guard" marks.** Each row said which line of my own code the tool did not trust. I never had to guess where a rule hid.
- **Reading through constants and `||`.** `!double.IsFinite(x) || x < Min || x > Max` became `Between(Capacitance.MinFarads, Capacitance.MaxFarads)` referencing my constants, so changing a limit follows.
- **Seeds are in the failure output, and replay is exact.** Four failures of mine, four seeds, one pinned replay verified identical.
- **`Any.Fixed` behind `With…(value)`** makes "this one field is the subject" a single readable call (`Specimens.OriginalResistor().WithTolerance(new Tolerance(10))`), and the lambda/pin split in the scaffold is a good vocabulary even where I had to drop the lambda.
- **Both-sided `Between` is drawn whole**, so boundaries are reachable; I still wrote them as literals, which the library's own guidance told me to do.
- **The analyzers are quiet and right.** No false positive on a large, unusual file set; the three I provoked fired with a readable message.
- **`[assembly: Reproducible]`** removed every wrapping lambda; theory rows each get their own seed.

## Fluency: 4 / 5

I was productive quickly: the generators that the tool could read were right, the `With…` surface made the substitution and BOM tests read like specifications, and a failing draw taught me something three times. The point I lost is the middle of the road: the domain's two
distinctive traits (25 decades, rules that tie fields together) are exactly the two things the scaffold cannot express, so the central generators (`AnyPart`, the three magnitudes, `PreferredValue`, `MarkingCode`) were rewritten rather than completed, I had to find the
`global.json` trick for `dotnet test --project` on my own, and the build was blocked until all seven marked files were settled. None of this is hostile — each stop was explained — but it is more hand-work than "scaffold, complete where asked" suggests.
