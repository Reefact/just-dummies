# Discovery notes — 45 healthcare claims
Agent: the same model and effort as every session of this trial. Library: JustDummies 1.0.0-uxtrial.1 (packed from main).

## What I did
Pages read, in order: `docs/PROJECT-README.md`; `docs/guides/getting-started.en.md`; `docs/packages/justdummies-cli.en.md` (almost entirely: what the
recap columns mean, the guard table, "Adjusting a parameter", "When a guard ties two parameters"); `docs/guides/composition.en.md`;
`docs/generators/collections.en.md` and `dates-and-times.en.md` (skimmed); the first half of `docs/packages/justdummies-xunit.en.md`; a grep of
`strings.en.md` for the pattern subset. Not read: core-concepts, reproducibility, errors-and-conflicts, FAQ, any analyzer page.
Then `MODEL.md`, the domain and the three domains it references (insurance, patient-record, payments).

`any` commands run (from `Tests.Discovery`):
- `any --help`, `any generate --help`
- `any generate ReimbursementRate` (then deleted the file, to redo it in the batch)
- `any generate ReimbursementRate ReimbursementSchedule ClaimReference ClaimAssessment DispensedLine SupplementaryPolicy ReimbursementClaim PolicyNumber CoveragePeriod Deductible Nir Patient Prescription PrescriptionLine Molecule Dosage TreatmentDuration Money Iban Party Bic CreditTransfer RemittanceInformation --entry-point any`
  (23 types, one command; I then deleted `CreditTransfer` and `RemittanceInformation`, which the aggregate builds itself).
  Result: 11 files "not compile until you resolve it" (`to verify`), 12 clean.

Generators completed by hand (all the files that carried a `TODO(any)` sentinel):
- regex guards -> `Any.StringMatching(...)` retyped to `AnyPattern`: `AnyPolicyNumber` (years 2015-2025 so that it is consistent with the default period), `AnyBic`,
  `AnyMolecule`, `AnyPrescription` (RPPS `[0-9]{11}`).
- `AnyTreatmentDuration`: `Between(1, TreatmentDuration.MaxDays)` (guard was in a void `Validate` helper).
- rewritten from scratch because the value is derived, not drawn: `AnyIban` (check digits via the domain's own `Iban.ComputeCheckDigits`), `AnyNir` (key via `Nir.ComputeKey`, plus
  `BornOn(DateOnly)`), `AnyClaimReference` (`Any.Combine` of year and a non-zero sequence), `AnyCoveragePeriod` (start + 1..365 days).
- cross-parameter rules written in `Generate()`: `AnyPatient` (NIR agrees with birth date), `AnyParty` (BIC agent in the account's country), `AnyPrescriptionLine` (tablet/puff unit vs route),
  `AnyClaimAssessment` (deductible + reduction <= covered), `AnyDispensedLine` (no statutory reimbursement on `Unlisted`, never above the price paid, EUR),
  `AnySupplementaryPolicy` (deductible < ceiling), `AnyReimbursementClaim` (prescription signed and dispensed, `dispensedOn` inside its window, distinct lines of that very prescription).
- `AnyReimbursementSchedule`: sentinel deleted, recipe kept as scaffolded.

Tests written (159 methods, 197 cases, all in `Tests.Discovery`; `[assembly: Reproducible]` in `AssemblyInfo.cs`):
- `ReimbursementRateTests` (11): bounds, one decimal, round-down theory, equality.
- `ReimbursementScheduleTests` (13): refusals, Covers/RateFor, defensive copy, equality by entries.
- `ClaimReferenceTests` (9): format theory, year, sequence 000000, equality.
- `ClaimAssessmentTests` (11): Payable, IsCappedByCeiling, sum guard, ranges, cents, equality.
- `DispensedLineTests` (15): PricePaid, RemainingCharge, statutory rules, packs/price/currency/category bounds.
- `SupplementaryPolicyTests` (15): Covers/Insures, issue year, birth date, ceiling, deductible < ceiling, nulls.
- `ReimbursementClaimTests` (16): prescription status, dispensing window, line count / ownership / duplicates, totals, benefit year.
- `ClaimFile*Tests` (65): submission refusals and their order; assessment (rate, remaining charge, deductible, rounding, ceiling, per calendar year);
  approve / reject / pay and their wrong-status refusals; the failed `Pay` leaving the claim Approved; ceiling bookkeeping.
- `GeneratorTests` (4): the hand-completed generators only draw valid values (200-500 draws each).

Result: `dotnet test --project Tests.Discovery` -> 197 total, 195 passed, 2 skipped (domain defects below), 0 failed.
Repeated: 12 further `dotnet test` runs and 40 runs of the built test executable, fresh seeds each, all green (52 repeated runs).

## Frictions and surprises, in order
1. **`dotnet test --project Tests.Discovery` does not run.** The command I was told to use gives
   `MSBUILD : error MSB1001: Unknown switch. ... --project Tests.Discovery`; plain `dotnet test` in the project gives
   `Microsoft.Testing.Platform.MSBuild.targets(395,5): error : Testing with VSTest target is no longer supported by Microsoft.Testing.Platform on .NET 10 SDK and later. If you use dotnet test, you should opt-in to the new dotnet test experience.`
   I expected one of them to work out of the box with the template csproj. Fix: a `global.json` at W with `{"test":{"runner":"Microsoft.Testing.Platform"}}`. No page of the docs I read says that
   a project using `xunit.v3` on the .NET 10 SDK needs it (the xunit page only talks about the `xunit.v3` 4.0.2 floor).
2. **The recap is honest but the work is large for a transverse domain.** One batch command scaffolded 23 types; 11 files refused to compile (by design). Reading the recap I could tell
   which parameters were ties (`— ties period to insured`), which was very useful. But each sentinel is two lines in ~20 files; I scripted their removal. Not a defect, a cost.
3. **A void validation helper is not read.** `TreatmentDuration(int days) { Validate(days); }` (a `private static void Validate` that throws on `days < 1 || days > 365`) and `Iban`'s `Validate(value)` are `to verify`
   with `Any.Int32()` / `Any.String().NotBlank()`. The docs say so ("a `void` helper called for its effect ... is marked as before"), so I expected it; but the guard in `Validate` is a plain two-sided
   range that the tool reads fine in a constructor, so rewriting it by hand into the constructor was the only way to get `.Between(1, 365)` scaffolded.
4. **`Enum.IsDefined(entry.Key)` inside a `foreach` marks the whole dictionary parameter `to verify, unread guards`** (`ReimbursementSchedule`), although the recipe `Any.DictionaryOf(Any.Enum<ReimbursementCategory>(), ...)` can only ever draw defined keys.
   I deleted the sentinel without changing the recipe. Harmless, but one more manual step for a guard that cannot fail.
5. **A cross-parameter rule has no seam for "was this parameter pinned?".** For `Patient` (NIR carries birth year/month), `Party` (BIC country = IBAN country), `PrescriptionLine` (unit vs route),
   `DispensedLine`, `SupplementaryPolicy` and `ReimbursementClaim`, the doc's advice is "derive one draw from another inside `Generate()`", and warns that a `With...` pin on the derived parameter then does not reach the draw.
   To avoid silently overriding a test's pin I compared the private fields: `ReferenceEquals(_nir, _nirRecipe)` ("still the default") — a hack that depends on how the scaffold wires `_x`/`_xRecipe`.
   Even so, a `WithLines(lambda)` on `AnyReimbursementClaim` compiles and is silently ignored by my `Generate()` (I only honour a pin). I would have liked a documented, supported way to write "derived unless pinned".
6. **Entry points live in the type's own namespace, so a transverse test needs one `using` per source domain, and the error does not say so.** Forgetting `using Payments.Domain;` gives
   `error CS0117: 'Any' does not contain a definition for 'Party'` — nothing hints at the extension member being in another namespace. I found it by thinking, not from the message.
   (`--entry-point-namespace` would have collected them; I did not know that before reading the page's later section.)
7. **A check-digit value (`Iban`, `Nir`) has no scaffold path that keeps the `With...` lambdas.** The recap says `to verify, unread guards` and the sentinel text proposes `Any.StringMatching`, which is fine for the shape but cannot produce the
   key. The doc's "Draw the body, then derive the check digit" (`Any.StringMatching(...).As(body => body + Luhn(body))`) solves the draw, but then the scaffolded `AnyString`/`Func<AnyString,AnyString>` structure no longer fits, and the doc's
   retyping recipe covers `AnyPattern`, not `IAny<string>`. I replaced both files with hand-written ones (no `With...(Func)` any more), and used the domain's own public `ComputeCheckDigits`/`ComputeKey` — which means the dummy can only be as right as the helper it calls.
8. **`any generate` reports `Any.String()` for a regex-guarded `string`, and `AnyPolicyNumber`'s default would have made every `SupplementaryPolicy` invalid on its own.** The tie between the number's year and the period (`period.Start.Year < issueYear`) is only noticed
   on the policy, as `unread guard ... — ties period to insured`. I chose year ranges (number 2015-2025, period from 2026, birth <= 2014) so the defaults are consistent by construction; a test that pins a number or a birth date outside those ranges is refused by the domain on every run.
9. **Everything else was smooth**, including: `Any.SetOf(Any.ElementOf(prescription.Lines)).WithCount(k)` for "a subset of lines" (found in the collections page), `IAny<T>.DifferentFrom(x)` for a second claim reference, `Any.Enum<Currency>().Except(Currency.EUR)`, and the seed line printed by a failing test.
10. **Domain findings** (see the findings list): `ClaimReference` accepts a trailing line feed; `ReimbursementSchedule.Rates` exposes the mutable dictionary. Both found by ordinary tests, not by a drawn value.

## What worked well
- The recap table: `guard`, `AnyX`, `to verify` and the `— ties A to B` suffix told me exactly where a rule between parameters lived, and the "file will not compile until you resolve it" stance meant I never trusted an unchecked default.
- Guard reading was accurate on the plain value objects (`ReimbursementRate` -> `Positive().LessThanOrEqualTo(100m).WithScale(1)`, `Deductible` -> `Between(0m, Deductible.Ceiling).WithScale(2)`, `Dosage`, `Money`) and it referenced the domain's own constants (`DispensedLine.MaxPacks`, `Money.MaximumAmount`).
- The compound `OrderedPair` for `CoveragePeriod` was a nice touch (even though the missing "at most one year" rule needed a rewrite).
- The `With...(lambda)` refinement made the tests read well: `Any.SupplementaryPolicy().WithPeriod(p => p.WithStart(...).WithDays(364))`, `WithLines(l => l.WithCount(3))`.
- `[assembly: Reproducible]` is one line, and a failing run printed `Reproduce this run with [JustDummies.Xunit.Reproducible(Seed = ...)]`. The two failures I met on the way (my own bad test data, and a real domain defect) were both replayable.
- Drawing the dummies made two of my own setup mistakes visible quickly (a schedule/deductible combination leaving nothing payable).

## Fluency: 4 / 5
Once the runner was configured, going from nothing to 197 green cases was quick: the scaffolder did most of the plumbing and its recap told me where to think. The two points lost are for the test command not working as given
(nothing in the docs I read mentions the .NET 10 runner opt-in) and for the transverse case: rules tying several parameters (and the check-digit types) always end in hand-written `Generate()` code with no supported way to honour a pin or a lambda on the derived parameter,
plus one `using` per source namespace that the compiler error does not suggest.
