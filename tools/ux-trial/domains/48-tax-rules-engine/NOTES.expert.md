# Expert notes — 48 tax rules engine
Agent: the same model and effort as every session of this trial. Library: JustDummies 1.0.0-uxtrial.1 (packed from main).

## What I did
**Pages read, in order:** `PROJECT-README.md`, `README.md` (index), `guides/getting-started`, `guides/core-concepts`,
`packages/justdummies-cli` (whole page, it is the one that matters), `packages/justdummies-xunit`, `guides/composition`,
`generators/numbers`, `enums-and-choices`, `collections`, `strings`, `guides/reproducibility` (first half). I skipped
errors-and-conflicts, inspecting-a-pool, design-principles and the FAQ: I did not meet a refusal I could not read.

**Domain read first:** all 34 files of `Domain/` before scaffolding, because I wanted to know which guards live in
helpers or in other types (the scaffolder's blind spots) before trusting its recap.

**`any` commands run** (from `Tests.Expert/`, after `dotnet restore`, `--output Generators --entry-point any`):
- `any generate Money Rate JurisdictionCode RuleCode TaxpayerId`
- `any generate TaxBracket EmploymentIncome SelfEmploymentIncome InvestmentIncome IncomeAboveCondition ResidentOfCondition AgeAtLeastCondition HouseholdSizeAtLeastCondition AndCondition OrCondition NotCondition`
- `any generate FlatRateRule ProgressiveRule ThresholdExemptionRule CappedRule CompositeRule TaxCreditRule Taxpayer TaxSchedule`
- `any generate NotCondition --dry-run` once more, to check what the tool does about an abstract parameter whose generator I had since written.
Result: 24 scaffolded generators plus 3 written from nothing. Six were kept untouched (Money, Rate, ResidentOf, AgeAtLeast, HouseholdSize, TaxSchedule); 18 were edited,
15 of them because the build stopped on a `TODO_...` identifier, 3 for a one-line base edit (SelfEmployment, Investment, IncomeAbove).

**Generators completed by hand** (all in `Generators/`):
- `AnyRuleCode`, `AnyJurisdictionCode`: base retyped `AnyString` -> `AnyPattern` (`Any.StringMatching`), the four-line retype of the doc. Rule codes are
  8 to 10 characters on purpose: a composite's tree needs distinct codes and I draw them independently, so the space must make a collision negligible.
- `AnyTaxpayerId`: `Any.StringMatching("[1-9][0-9]{7}").As(first => first + TaxpayerId.CheckDigitFor(first))`, retyped to `IAny<string>`, refining overload deleted.
- `AnyTaxBracket` (rewritten): `upper > lower` is a tie between two parameters; the upper bound is drawn as a positive width above the lower one, or absent.
- `AnyEmploymentIncome`, `AnyThresholdExemptionRule`, `AnyCappedRule`: the same kind of tie (deduction <= gross, ceiling > threshold, minimum <= maximum),
  solved by deriving the dependent parameter inside `Generate()` unless a test pinned it (a `bool` pinned-flag next to the field).
- `AnyProgressiveRule`: brackets built from `Ordered().Distinct()` cut points and `Ordered()` rates, so the scale is contiguous and non-decreasing by construction.
- `AnyFlatRateRule`, `AnyThresholdExemptionRule`, `AnyTaxCreditRule`: positive rates / amounts through a nested refinement (`new AnyRate().WithFraction(f => f.GreaterThan(0m))`).
- `Plausible` (static): `Amount()` (Money capped at ten million), `Label()` (not blank, <= 80), `TaxBase()` (flags, never `None`): the three rules whose guards sit in the
  internal `Guard` class and which the scaffolder did not flag.
- New, written from nothing because the types are abstract: `AnyICondition` (leaf or And/Or/Not, depth-bounded), `AnyIncomeComponent`, `AnyTaxRule`
  (+ their `Any.Condition()`, `Any.IncomeComponent()`, `Any.TaxRule()` entry points).
- Wiring: every rule's `condition` parameter is `new AnyICondition().OrNull()`; `AnyTaxpayer` gets 0 to 6 incomes, birth between 1900 and 1999.

**Tests written:** 341 methods, 497 cases (4 skipped, all four are `domain` findings), in `Tests/`: Money, Rate, JurisdictionCode, RuleCode, TaxpayerId, TaxBracket,
the three income components + the shared source guard, the four leaf conditions, And/Or/Not (depth 8 boundary, double negation, equality), the rule common behaviour
(condition, label, descendants), FlatRate, Progressive (all six structural refusals, rounding once, marginal rate), ThresholdExemption (the cliff at the ceiling), Capped
(all three outcomes, zero stays zero), Composite (every duplicate-code route), TaxCredit, BreakdownLine, TaxBreakdown (credits, effective rate rounding), Taxpayer,
TaxSchedule (creation, add/remove with every `ScheduleRefusal`, validate, reopen, evaluate, one whole-schedule scenario worked out by hand), the exception, and a
`GeneratorTests` class that draws each of my generators 300 times (this checks the test support, not the domain).
Everything is `[Fact]`/`[Theory]` under an assembly-level `[assembly: Reproducible]`.

**Result:** green on the first run once it compiled (after the one expectation I had miscounted: the depth of a Not over an And is 4), then 50 further runs with fresh seeds
(12 + 25 before the last test was added, 3 + 10 after), all green: `failed: 0`, `skipped: 4`. The final suite, 497 cases, ran 13 times.

**Domain findings kept as skipped tests:** `JurisdictionCode` and `RuleCode` accept a trailing line feed (`$`); `TaxSchedule.Rules` and the other collections are the private `List<T>`
behind an `IReadOnlyList`; a taxpayer whose incomes overflow `Money` makes `Evaluate` throw a bare `ArgumentOutOfRangeException`.

## Frictions and surprises, in order
1. **`dotnet test --project Tests.Expert` fails out of the box.** `MSBUILD : error MSB1001: Unknown switch ... --project` on the .NET 10 SDK until a `global.json` selects the
   Microsoft.Testing.Platform runner (`"test": { "runner": "Microsoft.Testing.Platform" }`). Not a JustDummies matter, but the xunit page and the getting-started page do not mention
   that `xunit.v3` + .NET 10 needs it for `--project`. I added `W/global.json`.
2. **`any generate` before `dotnet restore`**: `✗ Money: this project has not been restored, so its package references cannot be read. Restore it, then run the command again: dotnet restore`.
   Clear and actionable; I only note that the CLI page's first example does not say "restore first".
3. **The recap says "4 of 5 parameters inferred" while two of those are wrong.** `FlatRateRule`: `label  string  Any.String()` and `taxBase  IncomeKind  Any.Enum<IncomeKind>()`, no mark.
   The domain validates them in `Guard.Label(label, ...)` and `Guard.IncomeKinds(kinds, ...)`, a static class of the same assembly. `Any.String()` draws 0 to 1024 characters (label
   limit 80, blank refused) and `Any.Enum<IncomeKind>()` draws `None` one time in five (refused). The page says this shape earns no mark ("a validator of *another* type that returns the
   value it checked"), so it is documented; but a `static` guard class shared by every constructor of an assembly is the most common shape of all, and the recap's wording reads as a
   guarantee. I found it by reading the domain first. Had I trusted the recap, `Any.Enum<IncomeKind>()` would have been a 20 % intermittent failure in every rule test, each time
   blamed on the library. What I did: `Plausible.Label()` and `Plausible.TaxBase()` (`Any.Enum<IncomeKind>().AllowingCombinations().DifferentFrom(IncomeKind.None)`), edited into 6 and 4 files.
4. **Ties between two parameters: five types, five hand rewrites.** `TaxBracket`, `EmploymentIncome`, `ThresholdExemptionRule`, `CappedRule`, `TaxCreditRule` (both-zero) all got
   `to verify` on both parameters and a non-compiling `_ = TODO_verify_the_generator_for_...` line, as designed. The guards use `x.CompareTo(y) > 0` on a value object and `.IsZero`; the tool
   reads direct operators between primitives. The recipes of the CLI page (draw a width, derive inside `Generate()`) worked, but each means a 15 to 25 line edit of a ~200 line file, deleting
   the refining overload of the derived parameter, and adding a flag so a pin still wins. The scaffold has no seam for "this parameter is derived from that one", so I ended up
   with `bool _ceilingPinned` / `_minimumPinned` fields next to the generated ones. It works and the docs warn about it; it is simply the most expensive part of the trial.
5. **Abstract parameters: six files with the same hole, and the tool does not learn from my fix.** `ICondition? condition` appears in every rule; `TaxRule inner`, `IEnumerable<TaxRule>`,
   `IEnumerable<IncomeComponent>`, `IEnumerable<ICondition>` elsewhere. Each emitted `return TODO_supply_a_generator_for_condition;` and a comment naming `AnyICondition`. After I wrote
   `AnyICondition : IAny<ICondition>` (public, default constructor, in the project), `any generate NotCondition --dry-run` still emitted the same TODO, not `new AnyICondition()`.
   For a *concrete* composed type the tool writes `new AnyOrderReference()` whether or not it exists yet and lets the build name the gap; the same convention would work for an abstract
   one. As it stands I edited the same line in six files by script. Same for a collection of abstract elements (`IAny<IEnumerable<ICondition>>` needs a hand `Any.ListOf(...).As(...)`).
6. **`AnyMoney` is exactly what `Money` accepts, which is not what a test wants to sum.** Every Money parameter scaffolds `new AnyMoney()` ([0, 999 999 999 999.99]); 30 incomes drawn from it
   overflow `Money` inside `Taxpayer.TaxableIncome` almost every time. Correct (valid for the constructor), but nothing in the scaffold gives *one* place to say "plausible"; I wrote
   `Plausible.Amount()` and edited about a dozen call sites. Narrowing `AnyMoney` itself would have lied to the tests of `Money`. The CLI page's "valid base lives in `AnyValid...()`" is per parameter,
   so a cross-cutting taste (amounts of ordinary size) is N edits. Not a defect; the cost of the design.
7. **A `null` pin is ambiguous without a cast, on a non-nullable parameter too.** `Any.EmploymentIncome().WithEmployer(null!)` -> `error CS0121: The call is ambiguous between 'AnyEmploymentIncome.WithEmployer(string)' and 'AnyEmploymentIncome.WithEmployer(Func<AnyString, AnyString>)'`;
   same for `WithId(null!)`, `WithResidence(null!)`. Fix `(string)null!`. The page documents the cast for nullable parameters; testing the null *guard* of a non-nullable one through the
   generator is the same trap with no mention. I ended up calling constructors directly for those checks where the generator added nothing.
8. **Nullable abstract parameter: pin only, no `Without...()`.** `Money? minimum` got `WithoutMinimum()`; `ICondition? condition` got only `WithCondition(ICondition?)`, so every "this rule always applies" is
   `.WithCondition(null)`. Reads fine, but the CLI page promises `Without…()` for a nullable parameter and this one has none.
9. **A failing test that drew nothing still reports a seed.** With `[assembly: Reproducible]`, the literal-only theory `A_trailing_line_feed_does_not_make_a_code(value: "FR\n")` failed and printed
   `[JustDummies] These arbitrary values were seeded with 1190350775. Reproduce this run with [JustDummies.Xunit.Reproducible(Seed = 1190350775)].` There were no arbitrary values. Harmless,
   but it invites someone to pin a seed on a test it cannot change, and it makes the line meaningless as a signal ("this failure depends on a draw").
10. **`static readonly` bounds are not read.** `Taxpayer.EarliestDateOfBirth` (a `static readonly DateOnly` initialised with `new(1900, 1, 1)`) is compared in the guard, so `dateOfBirth`
    scaffolded `Any.DateOnly()` with `to verify` (full range of `DateOnly`, refused below 1900 almost always). A `const` would have been read. Documented limit; I wrote `Between(Taxpayer.EarliestDateOfBirth, new DateOnly(1999, 12, 31))`.
11. **Enabling the analyzers' Info rules is 38 lines.** I raised every `JDxxx` to warning to see what the tool thought of my tests; the analyzers README shows
    `dotnet_analyzer_diagnostic.category-JustDummies.Constraints.severity` for a category but I wanted all of them. I generated the 38 `dotnet_diagnostic.JDxxx.severity = warning` lines with a loop.
    Result: no diagnostic on 3 500 lines of tests and 3 600 of generated and hand-edited generators (checked the analyzers were alive with a deliberate `GreaterThan(100).LessThan(10)`: JD023 fired). A quiet analyzer, good.
12. **Tool output vs. my own count.** The recap says "1 of 1 parameters inferred, 1 to verify" for a type whose only parameter is to verify; the counts overlap (inferred includes to-verify),
    which makes the headline look better than the state of the file. Cosmetic.

## What worked well
- **The dummy/literal line shaped the tests, and the tests got shorter for it.** Every rule test must state `WithCondition(null)` because an arbitrary condition would make the rule
  `NotApplicable`: the condition is *not* a dummy where the amount is asserted, and the generator makes that visible at the call site instead of hiding it in a fixture. Conversely
  `Any.Taxpayer().WithHouseholdSize(3).WithIncomes(...)` says what the credit depends on and nothing else.
- **`With…(Func)` refinement and the pins** composed cleanly: `new AnyRate().WithFraction(r => r.GreaterThan(0m))` as a base, `Any.Taxpayer().WithResidence(Jurisdiction("FR-75"))`, etc.
  Refinements stacking (`LessThanOrEqualTo` then `GreaterThan` on an `AnyMoney`) did what the page says.
- **Reading through `base(...)`**: `EmploymentIncome(employer, ...) : base(employer)` -> `NotBlank().WithMaxLength(IncomeComponent.MaxSourceLength)` tagged `guard`, referencing the constant by name. Same for
  `Money.MaxAmount` and `.WithScale(2)`, `Taxpayer.MaxHouseholdSize`, `TaxSchedule.FirstYear/LastYear`, `AgeAtLeastCondition.MaxYears`. Six of my generators needed no thought.
- **`Ordered().Distinct()` and `Then`**: a contiguous progressive scale valid by construction (`Any.Int32().Between(1, 6).Then(count => Any.Combine(cuts, rates, build))`) in 8 lines;
  `Any.SetOf(Any.RuleCode()).WithCount(65)` for 65 distinct codes. `WithCount(0)` with `Ordered().Distinct()` (the one-bracket scale) just worked.
- **`AllowingCombinations().DifferentFrom(IncomeKind.None)`** is exactly the sentence the domain wants for a flags base.
- **The `TODO_...` identifiers** did their job: the build stopped on each of the 15 files whose recap carried a TODO, at the line, and never let a half-guessed generator through.
- **Seeds and the xUnit attribute**: the assembly-level attribute needed no per-test ceremony; the seed line appeared exactly on the failures I provoked while developing.
- **Analyzers stayed silent** on a large body of code (see friction 11), which is what I want from them.

## Fluency: 4 / 5
The library itself (the DSL, the refinements, `As`/`Combine`/`Then`, the pins) was fluent: once the generators existed the 341 tests were quick to write and read as specifications. The score is
held back by the generator layer for a domain like this one, where roughly two thirds of the parameters are either abstract, tied to a sibling, or guarded through a helper the tool cannot see:
that is 18 files edited by hand, and three kinds of edit (the silent `Guard.*` rows, the ties, the abstract `condition`) with the same mechanical edit repeated, and one genuine trap (the unmarked rows of
friction 3) that I avoided only because I read the domain before trusting the recap. None of it is hidden or wrong by the documents' own standard; it is the price of a model made of
polymorphic trees and shared guard classes.
