# Regular notes — 03 insurance
Agent: the same model and effort as every session of this trial. Library: JustDummies 1.0.0-uxtrial.1 (packed from main).

## What I did
- Pages read, in order: `MODEL.md`, `Domain/*.cs`, `PROJECT-README.md`, `guides/getting-started` (skimmed via the README), `packages/justdummies-xunit`,
  `guides/composition`, `packages/justdummies-cli` (what the tool reads, the `to verify` sentinel, the retype recipe, "When a guard ties two parameters"),
  `generators/dates-and-times`, `generators/numbers` (floating point), `generators/strings` (patterns), `guides/core-concepts` ("Arbitrary does not mean dummy"),
  the top of `guides/reproducibility`.
- Test project `Tests.Regular/Insurance.Tests.Regular.csproj` from the template, plus `global.json` at the workspace root (see friction 1).
- `any` commands: `dotnet restore` (the first call said "this project has not been restored"), then
  `any generate PolicyNumber BonusMalus Deductible CoveragePeriod Driver Claim CarPolicy --output Generators --entry-point any` (7 types, one run, 14 files).
  Recap: 4 files with a `to verify` mark (PolicyNumber regex, BonusMalus `Validate(...)` helper, Claim `switch` expression, CoveragePeriod and CarPolicy ties), the rest clean.
- Generators completed by hand:
  - `AnyPolicyNumber`: `Any.StringMatching("POL-[0-9]{4}-[A-Z]{2}[0-9]{6}")`, retyped `AnyString` -> `AnyPattern` on the three lines the doc names.
  - `AnyBonusMalus`: `Any.Double().Between(BonusMalus.Floor, BonusMalus.Ceiling)` (the guard is in a `void Validate` helper, so not read).
  - `AnyClaim`: only deleted the two sentinel lines (the `switch` just rejects undefined enum values, which `Any.Enum` never draws).
  - `AnyCoveragePeriod`: rewritten by hand as a start (2000..2100) plus 1..365 days, with `WithStart` and `WithDaysAfterStart`.
  - `AnyCarPolicy`: sentinels deleted; the default `claims` recipe is `Any.ListOf(new AnyClaim()).WithCount(0)`, because a claim must fall in the period and a list recipe cannot see the period.
- Tests (`Tests/`), 114 methods, 206 cases, `[assembly: Reproducible]` for the whole suite:
  - PolicyNumber (format, 16 refused shapes, year not validated, null, equality, generator sanity), 2 skipped (domain defects).
  - BonusMalus (range, both ends, NaN/infinities, new driver, negative counts, 6 bonus examples, 9 malus examples, ceiling, properties for both directions).
  - Deductible (range, ends, cents, range-before-cents order). Driver (trim, null/blank, ends of range, young-driver boundary, birth date unrelated).
  - Claim (round trip, refusals and their order, insured share per responsibility, no upper bound).
  - CoveragePeriod (ordering, one-year ceiling incl. leap years, Days, Contains, Next incl. the model's unrenewable example).
  - CarPolicy (each null, premium range, claims inside/outside the period, copy of the claims, AnnualPremium incl. rounding midpoint and surcharge boundary,
    DeclareClaim, OwnDamagePayout per level/responsibility/deductible, Renew: carry-over, bonus-malus per claims mix, ceiling, double renewal, unrenewable period).
- Result: green on the first run (204 passed, 2 skipped), then 12 further runs with fresh seeds, all green (204 passed, 2 skipped each time).
  The 2 skips were un-skipped once to confirm they really fail ("No exception was thrown").
- `bin/` and `obj/` removed at the end.

## Frictions and surprises, in order
1. **`dotnet test --project Tests.Regular` does not work out of the box.** On SDK 10.0.114: `MSBUILD : error MSB1001: Unknown switch ... --project`.
   `dotnet test Tests.Regular` then fails with `Microsoft.Testing.Platform.MSBuild.targets(395,5): error : Testing with VSTest target is no longer supported by Microsoft.Testing.Platform on .NET 10 SDK and later. If you use dotnet test, you should opt-in to the new dotnet test experience`.
   I expected the adapter page's template (`xunit.v3` + `Microsoft.NET.Test.Sdk` + `OutputType Exe`) to just run. Fix: a `global.json` with `{ "test": { "runner": "Microsoft.Testing.Platform" } }`
   at the workspace root. Neither `justdummies-xunit.en.md` nor the getting-started page says a project needs it on .NET 10; a regular user hits this on day one of a new repo.
2. `any generate ...` on an unrestored project: `✗ PolicyNumber: this project has not been restored, so its package references cannot be read. Restore it, then run the command again: dotnet restore`.
   Clear and actionable; I only note it because it is the first thing a fresh test project meets (and `dotnet build` would have worked too, the message could say so).
3. The scaffold of `CoveragePeriod` pairs `start`/`end` through `Any.OrderedPair(..., strict: true)` over two full-range `DateOnly`, then marks both `to verify` because of `end > start.AddYears(1)`.
   The pair itself is the right reading of `end <= start`, but a pair of full-range dates would break the one-year ceiling on practically every draw, so the base is useless as scaffolded
   and I replaced the whole file (start + length in days, the first form of "When a guard ties two parameters"). Expected: that the sentinel guided me, and it did; but I lost `WithEnd` and the whole `OrderedPair` machinery, which is a lot of generated code to throw away.
   A "start + length" scaffold for `end > start.AddYears(...)`/`AddDays(...)` guards would have saved the rewrite (idea, not a request for a solver).
4. `CarPolicy.claims` is the usual "a collection whose elements must fall in another parameter's period" tie. The tool marks `period` and `claims` `to verify`, which is right,
   but the shape of the answer is awkward: `With...` cannot take a generator, so a generated list of claims cannot be told the drawn period. I made the default an empty list
   and wrote a small `TestData.ClaimsIn(period, params Responsibility[])` helper that tests call after pinning `WithPeriod(...)`. It works and reads well in the tests, but nothing in the docs
   shows this very common aggregate case (child entities dated inside the parent's window); `Then(...)` is documented for numbers, not for building the aggregate itself.
5. `[InlineData]` cannot carry `decimal`, so premium and damage cases are strings parsed with `decimal.Parse(..., InvariantCulture)` (or `double` cast to decimal). Not a library issue, but domain tests on money hit it constantly; a one-line note on the xUnit adapter page would be nice.
6. `Any.Claim()` printed `Claim resolved to Insurance.Domain.Claim; System.Security.Claims.Claim also answers to that name.` Informative, correct, no action needed.
7. Generated files live in the domain namespace (`Insurance.Domain`) even though they are scaffolded into the test project; fine for the entry points (`using Insurance.Domain;` is needed anyway), but a reader of the test project sees test helpers in the production namespace. `--namespace` exists; I left the default.
8. Domain observations the draws and the examples surfaced (details in the findings): `PolicyNumber` accepts a trailing line feed and non-ASCII digits; a claim id declared twice is counted twice at renewal; a legitimate 367-day period cannot be renewed.

No analyzer diagnostic fired during the work (0 warnings on the test project at default severity); no failure on a drawn value in 13 runs.

## What worked well
- One `any generate` for seven types gave a working base in a minute; the `to verify` sentinels were accurate on every one that mattered (regex, helper `Validate`, tie between parameters) and silent on the ones that did not.
- `AnyDeductible` came out fully right from the guards (`Between(0m, Deductible.Ceiling).WithScale(2)`, referencing the public constant), and `AnyDriver` as well.
- `WithX(x => x.Between(...))` refinements read well in tests: `Any.Driver().WithYearsLicensed(y => y.Between(0, 2))` states exactly what the test cares about.
- `Any.Fixed`-style pins plus entry points (`Any.CarPolicy().WithPeriod(p).WithClaims(c)`) kept arrange blocks short.
- `[assembly: Reproducible]` is the right default; nothing to think about during 13 runs.
- Errors for exact exception types were easy to write because the draws for "out of range" values (`Any.Double().LessThan(Floor)`, `Any.Decimal().GreaterThan(5000m)`) include the extremes and the tests still passed.

## Fluency: 4 / 5
Everything the day-to-day DSL needed was there and the documented recipes (retype to `AnyPattern`, tie between parameters) worked on the first try. Points lost for the `dotnet test --project` setup that is not mentioned
anywhere in the docs (friction 1, the single real blocker) and for having to discard the scaffold of the period/claims ties and write the aggregate-level answer myself with little documentation for that shape.
