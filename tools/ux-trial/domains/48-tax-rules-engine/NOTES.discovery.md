# Discovery notes — 48 tax rules engine
Agent: the same model and effort as every session of this trial. Library: JustDummies 1.0.0-uxtrial.1 (packed from main).

## What I did
Pages read, in order: `PROJECT-README.md`; `guides/getting-started.en.md`; `packages/justdummies-cli.en.md` (the whole page, long);
`packages/justdummies-xunit.en.md`; `guides/composition.en.md` (As / Combine / OrderedPair / OrNull / Then up to `Any.Fixed`);
`generators/strings.en.md` (Patterns section); `generators/collections.en.md`; `generators/enums-and-choices.en.md`;
the head of `generators/numbers.en.md`; the head of `guides/core-concepts.en.md`; `analyzers/JD012.en.md` (after a warning).

`any` commands run (all from `Tests.Discovery/`, tool called by its full path):
- `any generate Money` before restoring: refused with "this project has not been restored" (clear), then `dotnet restore`.
- `any generate Money --output Generators`, then in one batch: `Money Rate JurisdictionCode RuleCode TaxpayerId TaxBracket
  EmploymentIncome SelfEmploymentIncome InvestmentIncome --output Generators --entry-point any --force`.
- `any generate IncomeAboveCondition ResidentOfCondition AgeAtLeastCondition HouseholdSizeAtLeastCondition AndCondition OrCondition
  NotCondition Taxpayer TaxSchedule --output Generators --entry-point any`.
- `any generate FlatRateRule ProgressiveRule ThresholdExemptionRule CappedRule CompositeRule TaxCreditRule BreakdownLine TaxBreakdown
  TaxRule IncomeComponent ...`: `TaxRule` and `IncomeComponent` refused as abstract (clear message), the rest scaffolded;
  I deleted `AnyBreakdownLine` and `AnyTaxBreakdown` because those are results the tests build by hand.

Generators (49 files under `Tests.Discovery/Generators/`: 24 scaffolded generators with their entry points, plus `HandWritten.cs`), completed by hand where needed:
- `AnyJurisdictionCode`, `AnyRuleCode`: `Any.StringMatching(...)`, retyped `AnyString` to `AnyPattern` (3 places) as the CLI page says.
- `AnyTaxpayerId`: `Any.StringMatching("[1-9][0-9]{7}").As(firstEight => firstEight + TaxpayerId.CheckDigitFor(firstEight))`, retyped to `IAny<string>`,
  refining overload deleted.
- Tied parameters resolved inside `Generate()`: `AnyTaxBracket` (upper above lower), `AnyEmploymentIncome` (deduction <= gross),
  `AnyThresholdExemptionRule` (ceiling above threshold), `AnyCappedRule` (minimum <= maximum, code new to the wrapped tree),
  `AnyCompositeRule` (own code new to the tree), `AnyTaxCreditRule` (not both zero).
- Abstract types, by hand in `HandWritten.cs`: `AnyLeafCondition`, `AnyICondition`, `AnyIncomeComponent`, `AnyLeafTaxRule`,
  `AnyBracketScale` (a contiguous scale with ascending rates, from `Any.ListOf(...).Ordered().Distinct()`), `AnyLabel`.
- Label / tax base / condition / rate defaults in every rule (see frictions 3, 4, 5); `AnyTaxpayer` (birth between 1900-01-01 and 2000-12-31,
  0 to 5 incomes); `And`/`Or`/`Not` operands from leaf conditions.

Tests (`Tests.Discovery/Tests/`, 321 methods, 475 cases): `MoneyTests`, `RateTests`, `JurisdictionCodeTests`, `RuleCodeTests`,
`TaxpayerIdTests`, `TaxBracketTests`, the three income kinds and the base class, the four leaf conditions, And/Or/Not (depth limit 8,
double negation, equality), `FlatRateRuleTests`, `RuleLabelTests`, `ProgressiveRuleTests`, `ThresholdExemptionRuleTests`,
`TaxCreditRuleTests`, `CappedRuleTests`, `CompositeRuleTests`, `TaxpayerTests`, `BreakdownLineTests`, `TaxBreakdownTests`, and four
`TaxSchedule` classes (construction, add/remove/find, validation incl. foreign residency at every depth, evaluation with a complete
schedule). `[assembly: Reproducible]` in `Support.cs`. Amounts, rates, thresholds and codes whose value the assertion is about stay literals;
sources, labels, residences, taxpayer ids, birth dates and household sizes the assertion does not read are drawn.

Result: green, 473 passed, 2 skipped (domain defect, below). After the first green run I ran the suite 12 times, then 40 more times,
with no failure (`dotnet test --project Tests.Discovery --no-build`, fresh seeds each time).
With all 38 `JD` rules raised to warning through a `.globalconfig` the project builds with 0 warning.

## Frictions and surprises, in order
1. **`dotnet test --project Tests.Discovery` does not work out of the box.** Output: `MSBUILD : error MSB1001: Unknown switch ... --project`.
   The docs never say how to run the tests. A `global.json` with `{"test": {"runner": "Microsoft.Testing.Platform"}}` at the workspace root fixed it
   (xunit.v3 + .NET 10 SDK). Not the library's fault, but a newcomer following the xUnit adapter page hits it with no hint.
2. **No `using Xunit;` with the template csproj.** `error CS0246: The type or namespace name 'Fact' could not be found` on the first build; I added
   `global using Xunit;`. The adapter page's samples show the `using`, but nothing says the template does not provide it.
3. **The scaffold stays silent on guards delegated to another type, and the generator draws values the domain refuses.** `FlatRateRule`'s `label` is
   scaffolded `Any.String()` with an empty last column, `taxBase` `Any.Enum<IncomeKind>()`, and `IncomeAboveCondition.kinds` the same, because the
   constructor calls `Guard.Label(label, ...)` / `Guard.IncomeKinds(kinds, ...)` (an internal static helper of another class) or chains to `base(...)`.
   The CLI page does list this case ("a validator of another type that returns the value it checked ... earns no mark"), but I only found it after
   reading the whole page, and the recap said `AnyX`/blank, i.e. nothing to check. A scratch probe showed what the silence costs:
   `Any.Enum<IncomeKind>()` over 300 draws returned `[None, All, Investment, SelfEmployment, Employment]` (None is refused by the domain) and
   `Any.String()` fed to the label guard was refused 276 times out of 300 (seed -1137049296). I fixed it by hand (`.OneOf(...)` on the enum,
   `AnyLabel.Create()` = `Any.String().NotBlank().WithMaxLength(80)`). I expected at least a `to verify` on a parameter that visibly goes through a
   throwing helper, since the helper is a method of the same assembly.
4. **Nullable `ICondition?` parameter: a random condition makes a rule's outcome random.** The tool turned `condition` into a TODO (abstract),
   which is fine, but when I wrote a hand generator the first instinct (a random condition) would make `Evaluate` return NotApplicable on most
   draws. I settled on `Any.Fixed<ICondition?>(null)` as the base (a rule applies unless the test says otherwise) and pin conditions with
   `.WithCondition(...)` in the tests that are about them. Worked, but the docs' advice for nullable ("draws null one in ten") does not help for the
   opposite need (null always, not-null on demand); `Without...()` exists only for a nullable parameter the tool inferred, which it did not here.
5. **A tied-parameter fix inside `Generate()` silently repairs a pin.** Following "Derive one draw from another inside `Generate()`" for
   `upper > lower`, `deduction <= gross`, `ceiling > threshold`, `minimum <= maximum`: it works, but then
   `Any.ThresholdExemptionRule().WithWithdrawalCeiling(new Money(5m))` below the threshold no longer reaches the constructor, so my first refusal tests
   (`A_ceiling_not_above_the_threshold_is_refused`, `A_credit_that_grants_nothing_is_refused`) failed with "No exception was thrown". I switched those
   tests to call the constructor directly. The docs do mention that `WithEnd(...)` no longer reaches the draw, but not that a pin that breaks the
   rule is replaced rather than refused; there is no built-in way to say "refuse a contradictory pin" short of writing the comparison twice.
6. **No first-class way to choose among generators of different types.** For `ICondition` / `TaxRule` / `IncomeComponent` I first wrote
   `Any.OneOf<IAny<ICondition>>(new AnyA().As(...), new AnyB().As(...)).Then(g => g)`. Build output:
   `warning JD012: Call Generate() on each pooled generator, or choose one with Then: Any.OneOf inferred a pool of recipes ...` on every such line, even though
   I did flatten with `Then`. JD012's own text says to choose with `Then`, but its sample is `Any.Boolean().Then(positive => ...)` for two choices; for
   n choices I ended up with `Any.Int32().Between(0, n - 1).Generate() switch { ... }` inside `Generate()`. It works and replays, but the abstract-type case
   is the central one of this scaffolder (the CLI page's own `AnyIRowElement` sample uses `Any.OneOf<IRowElement>(values)` on values, not generators), so a documented
   idiom (or an `Any.OneOfGenerators` / `Any.Choose`) would save the detour.
7. **`Any.StringMatching` and anchors: no surprise, but the domain's `^...$` hides a bug** (not a library issue, see Domain findings). Writing the regex-to-pattern
   translation for `JurisdictionCode` and `RuleCode` was one line each and the generated values always passed the factory. Noting it because the same
   `^...$` is accepted by the generator and by .NET alike, so only a hand-written negative test finds the trailing-newline case.
8. **Retyping after replacing a base** (`AnyString` to `AnyPattern`, `AnyString` to `IAny<string>`): the page is accurate (three places plus the `Func`), but it is a
   multi-line manual edit in a file regenerated by `--force`; I did it with `sed` and a short script. Two XML-doc `<remarks>` still mention `Func<AnyString,...>`
   after the retype (cosmetic).
9. **`any` writes `bin/`/`obj/` into the test project while analysing** (seen right after the first call, before any build of mine). Harmless, noted for the clean-up step.
10. Domain-level surprise found by the tests, not the library: summing incomes beyond `Money.MaxAmount` throws `ArgumentOutOfRangeException(paramName "amount")`
    from `Money`'s constructor, deep in `Taxpayer.TaxableIncome`, which a caller cannot tell from a bad input. Recorded as a domain finding.

## What worked well
- The recap table of `any generate` is genuinely useful: `guard` rows (`Between(0m, Money.MaxAmount).WithScale(2)`, `.Positive().LessThanOrEqualTo(Taxpayer.MaxHouseholdSize)`,
  `WithMaxLength(IncomeComponent.MaxSourceLength)`) named my constants, and `to verify` + `ties upper to lower` pointed at exactly the right six constructors.
  The deliberate non-compiling `TODO_verify...` lines made the build my checklist.
- `.As(firstEight => firstEight + TaxpayerId.CheckDigitFor(firstEight))` for the Luhn id: the CLI page's "draw the body, then derive the check digit" recipe was
  copy-and-adapt, and the domain's own `CheckDigitFor` made it one line.
- `Ordered().Distinct()` on `ListOf` made a contiguous progressive scale (ascending bounds, ascending rates) a ten-line generator.
- `DistinctBy(code => code.Value)` produced 65 distinct rule codes for the "64 rules" test without any retry code of mine.
- `[assembly: Reproducible]` plus the seed line in the failure output: verified once on a probe (`Reproducible(Seed = 12345)` gave the same value in two tests,
  and a failing probe printed the replay snippet). JD019 ("seed pinned") warned on the pinned probe, as it should.
- Entry points (`--entry-point any`) read well in tests: `Any.Taxpayer().WithHouseholdSize(3).WithIncomes(...)`. The `With...` pin vs lambda distinction was clear on the CLI page.
- Analyzers stayed quiet on the real suite even with the 38 rules raised to warning.

## Fluency: 4 / 5
The path from the README to a working suite was smooth: the getting-started page gives the philosophy and the CLI page answers almost every concrete question,
and most of my time went into the domain's tied parameters, which the tool flags and the docs solve. I lose a point for three things a newcomer pays for:
the silent scaffold on guards that go through a helper or a base constructor (the generators looked right and were not, only a probe showed it), the missing idiom for
choosing among generators (my first attempt drew a JD012 warning), and the pin-repair behaviour of a hand-tied `Generate()`. Running the tests themselves
(`global.json`, `using Xunit`) is outside the library but is the first wall.
