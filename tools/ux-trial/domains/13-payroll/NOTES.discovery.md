# Discovery notes — 13 payroll
Agent: the same model and effort as every session of this trial. Library: JustDummies 1.0.0-uxtrial.1 (packed from main).

## What I did
Pages read, in order: `docs/PROJECT-README.md`; `guides/getting-started.en.md` (whole); `packages/justdummies-cli.en.md`
(whole, it is the page the getting-started table sends you to for scaffolding); `packages/justdummies-xunit.en.md`;
`guides/core-concepts.en.md` (first half); `generators/numbers.en.md`, `generators/strings.en.md` (the `StringMatching` part),
`generators/enums-and-choices.en.md` (top), `generators/dates-and-times.en.md` (the `DateOnly` part, via grep).
I did not need composition, errors-and-conflicts, reproducibility or the analyzer pages.

Commands:
- `any --help`, `any generate --help`.
- `any generate EmployeeId HourlyRate PayPeriod PayslipLine EmploymentContract Payslip --output Generators --entry-point any`
  (first attempt failed: "this project has not been restored" -> `dotnet restore`, then the same command worked).
- `dotnet build`, `dotnet test --project Tests.Discovery` (needed a `global.json`, see friction 1).

Generators completed by hand (scaffolded in `Tests.Discovery/Generators/`):
- `AnyEmployeeId`: base replaced by `Any.StringMatching("EMP-[0-9]{6}")`, retyped `AnyString` -> `AnyPattern` in the 4 places the CLI page lists.
- `AnyEmploymentContract`: four `to verify` marks all pointing at the single `Validate(...)` call. I wrote the rules myself: hiring date
  `Between(1970-01-01, 2090-12-31)`, weekly hours `Between(1, 35)` (valid for every type), and the end date derived inside `Generate()` from the
  type and the hiring date (null for `Permanent`, hiring date + 1..3650 days otherwise) unless a test pinned it. I deleted the scaffolded
  `WithEndsOn(Func<...>)` overload, which would have been silently ignored.
- `AnyPayslip`: the `period`/`contract` tie (`period.LastDay < contract.HiredOn`) is not an ordered pair, so I derive the period from the drawn
  contract in `Generate()` (a month between 0 and 120 after the hiring month, floored at January 2000) unless pinned; again deleted the lambda overload.
- `AnyHourlyRate`, `AnyPayPeriod`, `AnyPayslipLine`: kept as scaffolded (`guard` rows were right). `AnyPayslipLine` is used as scaffolded in the generated
  payslips; in the line tests I draw the amount with an explicit `Any.Decimal().Between(0.01m, 5_000m).WithScale(2)`.

Tests written (7 classes, 104 methods, 201 cases, 200 run + 1 skipped):
- `EmployeeIdTests` (7): well-formed theory, null, malformed theory (10 cases), the two documented regex quirks (final `\n` accepted, non-ASCII digits accepted), equality / hash code / non-equality.
- `HourlyRateTests` (6): accepted theory (6), ceiling constant, non-positive (3), above ceiling (2), finer than a cent (3), range error wins over scale error.
- `PayPeriodTests` (5): boundaries, year refused (4), month refused (3), year checked first, first/last day theory (8 incl. 2000 and 2100 leap/non-leap February).
- `PayslipLineTests` (12): valid line, label length 40 / 41, null, blank (4), amount non-positive (4) / sub-cent (2), label checked before amount, `SignedAmount` for both kinds, undefined kind (kind unchecked at construction, `SignedAmount` throws).
- `EmploymentContractTests` (22): kept terms, end date kept for the 3 non-permanent types, 1970 boundary, undefined type (3), weekly hours accepted (8) / refused (9) per type, permanent with end date, non-permanent without (3), end <= start, end = start + 1, null employee / rate, the documented order of the checks (4 tests), `MonthlyContractualHours` (6), `CompletedYearsOfService` (8 + 4 incl. 29 February).
- `PayslipTests` (34 methods): construction (kept values, copied lines, order, three null refusals, hours boundaries, period before hiring, month of hiring, no end-date check), 1 skipped domain defect + 1 characterisation of it, base pay / overtime / rounding half away from zero (both), totals, gross, net, net floored at zero, `AddLine`, `Issue`, issued payslip refusals.
- `SeniorityBonusPolicyTests` (17): rate by type and years (13-row theory), anniversaries minus one day, before hiring, null refusals, bonus line at 3 and 10 years, rate evaluated on `LastDay`, below threshold, apprentice/intern, zero amount -> `null`, half-cent rounding, base pay only (not overtime), line not added to the payslip.

Dummies are used where the value really is irrelevant: the employee id and the hourly rate that only have to exist in the refusal tests of `EmploymentContract`, the contract/period/lines of a payslip whose life cycle is under test, the type and hiring date of a contract used for a pay calculation, the label/kind/amount of a `PayslipLine` whose other argument is the refused one. Everything an assertion is about (rates, hours, dates, amounts) is a literal. `[assembly: Reproducible]` is declared once.

Result: green. `dotnet test --project Tests.Discovery` 3 times after the last edit, 12 times before removing one test, plus 130 direct runs of the built test executable (fresh seed each time): 0 failures. 1 skipped (domain defect, below).

## Frictions and surprises, in order
1. **`dotnet test --project Tests.Discovery` does not work out of the box.** First `dotnet test --project Tests.Discovery` ->
   `MSBUILD : error MSB1001: Unknown switch. ... --project Tests.Discovery`; `dotnet test Tests.Discovery` ->
   `error : Testing with VSTest target is no longer supported by Microsoft.Testing.Platform on .NET 10 SDK and later. If you use dotnet test, you should opt-in to the new dotnet test experience.`
   Both come from the .NET 10 SDK + xUnit v3, not from JustDummies, but none of the pages I read (README, getting-started, xunit package page) says how to run xUnit v3 tests, and the README's
   "Contributing" snippet uses `dotnet test --solution` which has the same prerequisite. Fix: a `global.json` containing `{ "test": { "runner": "Microsoft.Testing.Platform" } }` beside the project. Cost: a few minutes. Expected: one line in the xunit page.
2. **`any generate` on a project that was not restored**: `✗ EmployeeId: this project has not been restored, so its package references cannot be read. Restore it, then run the command again: dotnet restore`. Clear message, did what it says. Slight surprise only because the page's example shows `any generate Order` as the first command; "restore first" is not on the page.
3. **A void `Validate(...)` helper marks every parameter `to verify`.** `EmploymentContract` calls `Validate(type, hiredOn, endsOn, weeklyHours)`; the recap lists `type`, `hiredOn`, `endsOn`, `weeklyHours` as `to verify, unread guards`, each comment pointing at line 9. The CLI page said this would happen ("a `void` helper called for its effect, `Validate(value);`, is marked as before"), so no surprise, but it means the whole interesting aggregate of this domain (type-dependent maximum, end date iff not permanent) is entirely mine to write. The scaffold gave no hint of the dependency between `type`, `endsOn` and `weeklyHours`: I only found it by reading `Validate`. I would have liked the comment to say "involves: type, hiredOn, endsOn, weeklyHours" - it does say it per parameter, but the same text four times.
4. **A tie between parameters needs hand-written derivation and the scaffolded `With…` methods become lies.** The CLI page's "Derive one draw from another inside `Generate()`" section told me exactly what to do and I did it, but then `WithEndsOn(Func<AnyDateOnly,AnyDateOnly>)` and `WithPeriod(Func<AnyPayPeriod,AnyPayPeriod>)` stayed in the file and would silently have had no effect (the page does warn "its `With…` pin no longer reaches the draw"). I deleted them by hand together with the recipe fields, and kept the pin overloads by turning the field into a nullable "pinned or derived" marker. It took about 40 lines of editing across the constructor, the copy constructor and `Generate()`. The mechanism is right, the editing is manual and easy to get half-wrong; a documented skeleton for "derived unless pinned" would save time.
5. **Cross-aggregate tie (`period` vs `contract.HiredOn`)** is reported as `to verify` on both `contract` and `period` with "ties period to contract" - good message - but the two docs sections about ties are about parameters of the same type (`OrderedPair`) or of a start/duration; mine involved two different aggregates and a derived property (`LastDay`). I used the "derive inside `Generate()`" recipe; it worked, but I had to figure out by myself that I must also bound the hiring date (`<= 2090`) so that a valid period (<= 2100) always exists.
6. **A test that pins `WithWeeklyHours(39)` on a drawn contract fails on some seeds** (apprenticeship/internship max out at 35). I saw this coming while designing rather than from a red run, because the constructor's rule was in `MODEL.md`. The library-supported answer worked nicely: `.WithType(t => t.Except(ContractType.Apprenticeship, ContractType.Internship))`. No friction with the library, noting it because it is the exact situation the docs call "the lambda guarantees where it starts, not where you take it".
7. **My own mistake caught by a failing assertion, not the library's**: `The_amount_rounds_a_half_cent_away_from_zero` failed with `Expected: 0.05 / Actual: 0.09` because I hired the employee in 2000 (6 % tier). Seed reported as `[JustDummies] These arbitrary values were seeded with 1305014429. Reproduce this run with [JustDummies.Xunit.Reproducible(Seed = 1305014429)].` - the message was there, but the cause was a literal of mine, not a draw. Nothing to fix in the library.
8. **Entry point with `--entry-point any`** (C# 14) compiled without a flag on net10.0, so `Any.Payslip()` just worked. No friction. (Would be a surprise on an older SDK; the page does say so.)
9. **Analyzers were silent.** Build: `0 Warning(s) 0 Error(s)`. I did not see a single JDxxx, including JD030 on `Any.String()` (Info by design). I never learned whether they were running at all; I could not tell "nothing to report" from "not active".

## What worked well
- The getting-started page's distinction (dummy vs the value the assertion is about) actually drove my design: I wrote literals for every number that was in an assertion and only drew the rest, and the test names/AAA structure got shorter.
- `any generate` for six types at once with a readable recap; the `guard` rows (`Positive().LessThanOrEqualTo(HourlyRate.Ceiling).WithScale(2)`, `Between(2000, 2100)`, `NotBlank().WithMaxLength(PayslipLine.MaximumLabelLength)`) were exactly my constructors' rules, and it references `HourlyRate.Ceiling` / `PayslipLine.MaximumLabelLength` rather than copying numbers.
- Non-compiling `TODO_verify_…` sentinel: the build really did stop at the right lines; the regex message even told me which four places to retype (`AnyString` -> `AnyPattern`).
- `With…(lambda)` refinements (`WithRate(r => r.WithValue(10.00m))`, `WithType(t => t.Except(...))`) read very well in tests, and the pin of an undefined enum (`WithKind((LineKind)99)`) for the "kind is not checked at construction" test was possible.
- The seed report, and `[assembly: Reproducible]`, which needed no per-test noise.
- Stable: 142+ runs with fresh seeds, no flaky draw.

## Fluency: 4 / 5
The path from README to a green suite was short because the docs say, in this order, what a dummy is, how to scaffold, and what `to verify` means; I never had to guess the API. I lose a point for the hand-editing needed when a constructor has a helper-validated, cross-parameter rule (friction 3-5), which is exactly the shape of an employment contract and a payslip, and for the unexplained `dotnet test` prerequisite on a .NET 10 SDK (friction 1) and the silence of the analyzers (friction 9).

## Domain observations (not library)
- `Payslip` accepts null elements in `lines` (the model says so); `TotalBonuses` then throws `NullReferenceException` when read. The test expecting a refusal at construction is kept as `Skip = "domain defect: ..."`, a characterisation test pins the current NRE.
- `Payslip` does not check the contract's end date (documented): a payslip for 2023 on a fixed-term contract that ended in 2020 is accepted. Pinned by a test.
- `EmployeeId` accepts `"EMP-123456\n"` and non-ASCII digits because of `$` / `\d` (documented as "as written"); both pinned by tests. Likely accidental.
- No upper bound on a `PayslipLine` amount: with `Any.Decimal().Positive().WithScale(2)` as scaffolded, the draw may reach `decimal.MaxValue` and `TotalBonuses` would overflow; I bounded the amounts in my own draws and did not meet it.
