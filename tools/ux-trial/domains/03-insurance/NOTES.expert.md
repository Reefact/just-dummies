# Expert notes — 03 Insurance
Agent: the same model and effort as every session of this trial. Library: JustDummies 1.0.0-uxtrial.1 (packed from main).

## What I did
Pages read, in order: `PROJECT-README.md`, `README.md`, `guides/getting-started`, `guides/core-concepts`, `packages/justdummies-xunit`,
`packages/justdummies-cli` (whole page), `generators/README`, `generators/dates-and-times`, `generators/numbers`,
`guides/composition` (first half), `generators/strings` (patterns), `generators/collections` (first half).

`any` commands (from `Tests.Expert`, after a `dotnet restore`):
`any generate PolicyNumber BonusMalus Deductible CoveragePeriod Driver Claim CarPolicy --output Generators --entry-point any`
(7 types, 14 files, `Any.PolicyNumber()` ... `Any.CarPolicy()` entry points).

Generators completed or rewritten by hand (`Generators/`):
- `AnyPolicyNumber`: `AnyString` -> `AnyPattern`, `Any.StringMatching(@"POL-\d{4}-[A-Z]{2}\d{6}")` (4 retypes, as documented); TODO lines deleted.
- `AnyBonusMalus`: `Any.Double().Between(BonusMalus.Floor, BonusMalus.Ceiling)`.
- `AnyClaim`: only the `responsibility` TODO lines deleted (the enum generator draws declared members only); `AnyClaim.During(CoveragePeriod)` added in a file of its own.
- `AnyCoveragePeriod`: rewritten as "a start in 2000..2100 + 1..365 days" (`WithStart`, `WithDaysAfterStart`); the scaffold's ordered pair cannot express the one-year ceiling.
- `AnyCarPolicy`: TODO lines deleted; the claims base is `Any.ListOf(new AnyClaim()).Empty()` because a per-parameter recipe cannot keep claims inside the period.
- `AnyDeductible`, `AnyDriver`: untouched.

Tests (`Tests/`, `[assembly: Reproducible]`): 87 test methods, 193 cases (190 pass, 3 skipped as domain defects).
- `PolicyNumberTests` (8 methods): value kept, year not validated, null, 13 malformed literals, equality by value and hash, not equal to text/null; skipped trailing line feed.
- `BonusMalusTests` (18): constants, NewDriver, accepted ends, NaN/below/above (infinities included), no-claim 5% cut towards zero and floor, responsible x1.25, partial x1.125, mixed, ceiling, int.MaxValue claims, negative counts with ParamName, immutability; two skipped theories (floating point).
- `DeductibleTests` (4): ends accepted, out of range, third decimal place, ceiling constant.
- `CoveragePeriodTests` (10): Days (leap years), end <= start, one-year ceiling (incl. 29 Feb start), Contains on both ends, Next() cases incl. the 367-day refusal, original untouched.
- `DriverTests` (8): trimming, null/blank, years 0..80 and out of range, IsYoungDriver at 2/3, birth date unrelated.
- `ClaimTests` (8): InsuredShare per responsibility, empty id, damage <= 0, tiny/huge damage, undefined responsibility, validation order.
- `CarPolicyTests` (31): null parts (theory over generator pins), base premium bounds, undefined enums accepted, claims on/over the period edges, defensive copy, AnnualPremium (rounding away from zero, surcharge threshold, order of operations), DeclareClaim (edges, outside, null, duplicates), OwnDamagePayout (levels, shares, floor at 0, foreign claim, null), Renew (carry-over, next period, claims cleared, bonus-malus per responsibility, double renewal, Next() failure propagates).

Result: green. 1 first run + 12 + 3 + 10 further runs with fresh seeds, all green (0 failed, 3 skipped). No drawn value ever failed a test.

## Frictions and surprises, in order
1. `dotnet test --project Tests.Expert` (the command of the brief) fails on the .NET 10 SDK: `MSBUILD : error MSB1001: Unknown switch. ... Switch: --project`. Without `--project`: `error : Testing with VSTest target is no longer supported by Microsoft.Testing.Platform on .NET 10 SDK and later. If you use dotnet test, you should opt-in to the new dotnet test experience.` Neither `getting-started` nor `justdummies-xunit` mentions that xunit.v3 test projects need a `global.json` with `{ "test": { "runner": "Microsoft.Testing.Platform" } }`. I created it at W (outside the domain folder). Expected: the docs for the xUnit adapter to say how to run the suite on SDK 10.
2. `any generate ...` right after creating the csproj: `✗ PolicyNumber: this project has not been restored, so its package references cannot be read. Restore it, then run the command again: dotnet restore`. Clear message, one command; the CLI page could list restore in a "before you run it" line.
3. `BonusMalus(double)`: the guard is in a `void` helper `Validate(coefficient);` -> `Any.Double()  to verify, unread guards`, and the file does not compile until I read `Validate` and write `Between(Floor, Ceiling)` myself. As documented ("a `void` helper called for its effect is marked"), but a three-line `if (x < Floor) / if (x > Ceiling) / NaN` helper full of constants is the commonest value-object shape, so this costs on a very ordinary domain. Idea: read a `void` helper whose body is the same guard shapes, like the value-returning helper.
4. `PolicyNumber`: regex guard -> `Any.String()  to verify`; as documented. The TODO comment itself tells the retype (`AnyString` -> `AnyPattern` on 4 lines) and that was exactly right; the manual retype took 5 minutes and compiled first time. It is also the only moment the test author must re-type the pattern of the domain: it now lives twice (domain regex, generator), with nothing tying them. Idea: none cheap; just noting the duplication.
5. `CoveragePeriod`: `end > start.AddYears(1)` is "unread guard ... ties end to start"; both parameters marked `to verify`, and the scaffold had silently produced an `Any.OrderedPair(..., strict: true)` for `end <= start` (good) over two unbounded `Any.DateOnly()`. If I had only deleted the TODO lines, the generator would have thrown on almost every draw (a random pair across 0001..9999 is almost never within a year). The rest of the file (pins `WithStart`/`WithEnd` that bound each other) is nice, but unusable once a one-year ceiling exists, so I rewrote the file by hand as start + days. Rewriting a 120-line scaffold to change its model is heavy; the doc's "draw a start and a duration" sample (Booking) is exactly right and I followed it, but it is shown as `Any.Combine` while I needed a `partial` generator keeping `With…`; the page does not show how to turn a scaffold into that shape. `--force` will wipe my rewrite (no `partial` side file can hold it).
6. `Claim`: `InsuredShare = responsibility switch {... _ => throw ...}` marks `responsibility` `to verify, unread guards` although `Any.Enum<Responsibility>()` (declared members only) is exactly right. I had to open the constructor to be sure, then delete the two lines. A switch-expression default arm throwing on an enum is a common shape; "nothing to add - declared members are all it draws" (as for `Enum.IsDefined`) would apply.
7. `CarPolicy`: `claim.OccurredOn` must be in `period` -> `period` and `claims` both `to verify`. This is the "rule between two parameters" case the doc says is mine to write. I found no way to express it in the per-parameter `With…` model: a `WithClaims(l => l.WithCount(2))` lambda cannot make dates depend on the drawn period. I chose a base of `Empty()` claims and added `AnyClaim.During(period)` in a side file; the tests declare claims against their pinned period. Cost: a drawn `CarPolicy` never has claims, which is right for the dummy role but means the "policy with claims" scenario is always spelled by hand. The doc covers the 2-parameter order tie and check digits, not "children must fit the parent" (the exact case the scaffold's own list names: "a claim inside a policy period").
8. Pinning to `null` to test null guards: `policy.WithNumber(null)` is ambiguous between the pin and the lambda overload; I needed `(PolicyNumber)null!`. Documented for nullable parameters, but a non-nullable parameter's null test is the first thing a test author writes. Works fine once cast.
9. Using the generator as the Act: for refusals of one argument (`Any.Driver().WithYearsLicensed(81).Generate()` throws `ArgumentOutOfRangeException`), the other constructor arguments need no declaration. This reads very well, but the Act line is `Generate()` and no longer `new Driver(...)`, which is a small lie about what is under test; I accepted it only for guard tests. Would be worth a line in the docs: it is the best use of the pins for refusal tests, and also what makes the scaffold's `With…` pin on invalid values acceptable.
10. `JD036` (warning level raised by me) points to `AnyClaim.cs`'s `Any.Decimal().Positive()`: "declares no range: one draw in ten spans the whole type". The domain says damage has no upper bound, so I kept it: ~10% of dummy claims are around 7.9e28 and my payout tests that use `Any.Claim()` (level below comprehensive) pass with them. Correct and honest, but the scaffold would have been equally defensible with a bound; no action.
11. Everything else compiled first time; no `JDxxx` warning on my tests/generators except JD030 (driver name, Info) and JD036 above.

Domain findings (kept as tests, details in the final report): trailing `\n` accepted by `PolicyNumber` (`$`); `BonusMalus.AfterYear` loses a cent in binary floating point (0.60 -> 0.56 instead of 0.57; 0.72 x 1.25 -> 0.89 instead of 0.90; both skipped as `domain defect`); `Next()` makes a 367-day policy unrenewable; the same claim can be declared twice (documented, tested as a characterisation).

## What worked well
- The scaffold got 6 of the 7 types right or nearly (Deductible and Driver compiled with no edit; the `WithScale(2)` on the cents-deductible and `Between(0, 80)` on years licensed came straight from the guards, constants named, `Deductible.Ceiling`).
- `any` found the OrderedPair itself and flagged the real hole (the one-year ceiling) rather than guessing.
- The `[assembly: Reproducible]` + seed on failure: I saw it work on my one failing run (wrong expectation of mine; a seed was printed with the exact replay attribute).
- The per-parameter `With…(literal)` pins make tests read as the specification: `Any.CarPolicy().WithDriver(d => d.WithYearsLicensed(2)).WithBasePremium(500m)` says "only these two matter".
- The lambda form `d => d.WithYearsLicensed(2)` keeps the rest of the driver arbitrary yet valid, no restating of its rules.
- Errors on the first build of the scaffold are precise (`CS0103 TODO_verify_the_generator_for_...`) and the TODO comment names file, line and the statement.
- The documentation on what is a dummy and what is not kept me honest: the pass-through test of `Renew` pins all carried values to literals instead of comparing a drawn policy with its renewal.

## Fluency: 4 / 5
The documentation and tool made the happy path fast: 5 of 7 types needed minutes, and the rules the tool cannot read are named with file and line, so I knew exactly where to stop. The score loses a point for three costs: the `dotnet test` setup on .NET 10 is undocumented (nothing in the packages pages), the cross-parameter rules (`CoveragePeriod`, `CarPolicy`) force a hand rewrite with no documented shape for "children inside a parent range", and two ordinary guards (void `Validate` helper, switch-expression default) are marked `to verify` although the recipe is right.
