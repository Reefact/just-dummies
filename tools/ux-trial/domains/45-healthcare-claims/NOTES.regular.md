# Regular notes — 45 healthcare claims
Agent: the same model and effort as every session of this trial. Library: JustDummies 1.0.0-uxtrial.1 (packed from main).

## What I did
- Read, in order: `MODEL.md`, the whole `Domain/` of 45 plus the three referenced domains (01 payments, 03 insurance, 08 patient record),
  then as a reminder `PROJECT-README.md`, `guides/getting-started`, `packages/justdummies-cli` (the long page, in full: the
  adjusting and the "guard ties two parameters" sections were the ones I used), `packages/justdummies-xunit`, `guides/composition`,
  `generators/README`, `dates-and-times`, `numbers` (decimal/scale), `enums-and-choices`, `collections`, and the pattern part of `strings`.
- Project `Tests.Regular/HealthcareClaims.Tests.Regular.csproj` from the template; `any.json` beside it
  (`output ./Generators`, `namespace ...Generators`, `entryPoint any`) so that every `any generate` call is the short form.
- `any` commands, in two batches after a first `dotnet build`:
  `any generate Nir Molecule Dosage TreatmentDuration Money Iban Bic PolicyNumber CoveragePeriod Deductible` and
  `any generate Party Patient PrescriptionLine Prescription ReimbursementRate ReimbursementSchedule ClaimReference DispensedLine SupplementaryPolicy ReimbursementClaim ClaimAssessment`
  (21 types, 42 files with the entry points). `AnyClaimAssessment` deleted again (no test needs a dummy assessment).
- Generators completed by hand:
  - string patterns retyped to `AnyPattern`: `AnyMolecule`, `AnyBic`, `AnyPolicyNumber`, `AnyPrescription` (RPPS `\d{11}`);
  - derived strings, factory typed `IAny<string>` with the refining overload deleted: `AnyIban` (check digits through `Iban.ComputeCheckDigits`),
    `AnyNir` (key through `Nir.ComputeKey`), `AnyClaimReference` (`Any.Combine` year + sequence 1..999 999);
  - one-line recipes: `AnyTreatmentDuration`, `AnyPatient` names (`Alpha().WithLengthBetween`), `AnyDispensedLine` (EUR, unit price <= 5 000), `AnySupplementaryPolicy`
    (policy numbers issued 2010-2015, ceiling EUR above 5 000.01 so it always exceeds any deductible, birth dates <= 2005, default periods from 2015: the
    cross-parameter rules hold by disjoint bases);
  - `Generate()` rewritten: `AnyPatient` (NIR drawn from the birth date), `AnyPrescriptionLine` (route drawn from the dosage unit), `AnyDispensedLine`
    (statutory reimbursement drawn from the price paid, never for `Unlisted`);
  - written by hand, scaffold deleted: `AnyCoveragePeriod` (start + length), `AnyReimbursementClaim` (`ForPolicy`, `ForPrescription`, `DispensedOn`, `WithLines`, `WithLineCount`);
  - neighbour `*.Custom.cs` files: `AnyNir.BornOn`, `AnyBic.InCountry`, `AnyIban.InCountry`, `AnyPolicyNumber.IssuedIn/IssuedBetween`,
    `AnyPrescription.Signed/Dispensed/Cancelled`, `AnyDispensedLine` statutory draw.
  - `Support/Scenarios.cs`: `ScheduleOf`, `PolicyWithin2026`, `ClaimOf/ClaimOn` for the tests where the figures are the point.
- Tests (160 methods, 213 cases; `[assembly: Reproducible]`): `ReimbursementRateTests` (range, one decimal, round-down table, equality),
  `ReimbursementScheduleTests` (entries, absent category, refusals, snapshot, equality), `ClaimReferenceTests`, `ClaimAssessmentTests` (payable, boundaries,
  1 000 000, cents), `DispensedLineTests` (price paid, remaining charge, Unlisted, currency, packs 1..12, price 5 000), `SupplementaryPolicyTests` (issue year,
  birth date, ceiling, deductible strictly below), `ReimbursementClaimTests` (dispensed prescription, window boundaries, lines, totals, copy), `ClaimFileSubmitTests`
  (the three refusals and their order, rejected claim re-claimable, duplicates), `ClaimFileAssessTests` (rates, remaining charge, uncovered, deductible, ceiling, per year),
  `ClaimFileDecisionTests` (approve/reject/refusals), `ClaimFilePaymentTests` (transfer content, business days, before dispensing, same account), `ClaimFileCeilingTests`.
- Result: green, 210 passed + 3 skipped (domain defects below); 1 run + 25 repeated runs, then 12 more after the last edit, all identical. No drawn value ever failed a test.

## Frictions and surprises, in order
1. **`dotnet test --project Tests.Regular` failed on the first call** with `MSBUILD : error MSB1001: Unknown switch ... --project`, and without `--project`
   `Microsoft.Testing.Platform.MSBuild.targets(395,5): error : Testing with VSTest target is no longer supported by Microsoft.Testing.Platform on .NET 10 SDK and later`.
   xunit.v3 4.0.2 is a Microsoft Testing Platform runner, and the .NET 10 SDK needs `{ "test": { "runner": "Microsoft.Testing.Platform" } }` in a `global.json`.
   Neither `justdummies-xunit` nor `getting-started` says so (the README mentions `global.json` only as the repository's own pin). I added `W/global.json`; the error links to aka.ms, so it
   cost five minutes rather than an hour. Expected: a line on the xunit page for .NET 10 SDK users.
2. **First `any generate` on a fresh project**: every type answered `no type by that name in this compilation, and nothing close to it. Check --project: the type has to be reachable from the
   project being analyzed.` (even `Nir`, `Money`...). The cause was only that I had not restored/built the new project, so the three referenced projects were not resolved. After `dotnet build` it worked.
   The hint points at `--project`, not at "build once first" — and the `Did you mean Cer, Oid?` suggestions for `Nir`/`Bic` read as noise. Expected: say "restore/build the project first" when the project has no assets file.
3. **The recap says "N of N parameters inferred" while a `to verify` string stays `Any.String()`**: for `Nir`, `Bic`, `PolicyNumber`, `ClaimReference` the "inference" is a plain `Any.String()` and the build is blocked on purpose
   (documented). Replacing it with `Any.StringMatching(...)` means editing five places per file (field type, constructor parameter, factory return type, `Func<>` of the refining overload, the sentinel lines). I wrote a small script to do it
   for the four pattern files; for the three whose recipe ends in `.As(...)` (Iban, Nir, ClaimReference) I also had to delete the refining overload, as the page says. It is documented and mechanical, but it is the slowest part of
   the day: 7 files, about 15 minutes of mechanical edits. A `--regex`-aware mode would be welcome (the page explains why not; idea only).
4. **A guard inside a `foreach` marks the parameter although the docs table says it needs nothing.** `ReimbursementSchedule`:
   `rates  IReadOnlyDictionary<ReimbursementCategory, ReimbursementRate>  Any.DictionaryOf(...).NonEmpty()  to verify, guard, unread guards` with
   `rates: unread guard at ReimbursementSchedule.cs, line 17: if (!Enum.IsDefined(entry.Key)) { ...`. The table of the CLI page says `!Enum.IsDefined(p)` → "nothing to add"; here it is inside a loop, which the page
   explains elsewhere (under something deciding whether it runs), but the message does not say that. I read the line, saw it is satisfied by `Any.Enum`, and deleted the sentinel. Expected: the message could say "inside a loop".
5. **"unread guard" attribution can look wrong.** `DispensedLine`: `packs: unread guard at DispensedLine.cs, line 35: if (category == ReimbursementCategory.Unlisted) { ...` — the statement named
   does not mention `packs`; the real tie is `pricePaid = unitPrice.Amount * packs` compared with the statutory amount two lines later. Same for `unitPrice`. Marking is right (the rule involves them), the quoted line
   sent me to the wrong place first.
6. **Cross-parameter rules are where the scaffold stops, and its `With…` surface then goes stale.** `Patient` (NIR ↔ birth date), `PrescriptionLine` (dosage unit ↔ route), `Prescription` (issue date ↔ birth date),
   `CoveragePeriod` (`end > start.AddYears(1)`), `SupplementaryPolicy` (4 ties), `ReimbursementClaim` (3 of 4 parameters tied to the prescription) all came out `to verify` on every parameter. For the leaves I used the documented
   "base owns the rule" forms (disjoint bases, derive in `Generate()`), which worked well; but once `Generate()` derives a parameter, the generated `WithNir(...)` / `WithRoute(...)` pins and refiners silently do nothing for it
   (the page warns about it). I deleted those methods by hand rather than leave traps. For `CoveragePeriod` the scaffold had even paired the bounds with `Any.OrderedPair(strict: true)` — correct for `end <= start`, but it cannot
   honour the one-year limit, so I replaced the whole file with start + length. `ReimbursementClaim` I wrote by hand entirely. Net: for this domain the scaffold saved the boilerplate of ~14 leaf generators and little else for the
   3 aggregates, which is what the page promises.
7. **No help with an entity that has a lifecycle.** A dispensed prescription needs `Sign()` then `Dispense(on)`; `any` can only call the constructor. I added `Signed()/Dispensed()/Cancelled()` to `AnyPrescription` myself
   (a status field copied in the private copy constructor, transitions applied in `Generate()`). It worked, but nothing in the docs shows the idiom (a field you add must be copied by hand in the generated copy constructor, otherwise the next `With…` drops it — I added the line in the generated file, which `--force` would erase).
8. **JD030 never showed up in the build.** The scaffolded names are `Any.String().NotBlank()` (guard of the constructor); JD030 is Info, so `dotnet build` stays silent (documented). When I raised all 38 rules to `warning` in a throw-away `.editorconfig`, the only
   hit was those two names in `AnyPatient` (`draws 1 to 1025 characters`), which I fixed with `Alpha().WithLengthBetween(1, 40)`. No other analyzer diagnostic on 5 000 lines of tests and generators.
9. **42 generated files for 21 types** (`Any*.cs` + `Any*.Entry.cs` each): fine functionally, noisy in the tree; `--entry-point static:X` would write the same number of files. Idea only.
10. Small ones: `Any.DateOnly().Between(a, b)` inside a refinement narrowing a base `Between` (issue dates) worked exactly as the page says; `Any.Fixed`-based pins make it impossible to know in `Generate()` whether a field was pinned, hence my "no pin on derived parameters" choice. `TheoryData<decimal, ...>` + `[MemberData]` was the clean way to pass decimals to a theory (attributes cannot carry `decimal`); no library involvement.

## What worked well
- `[assembly: Reproducible]`: one line, and a failing test prints its seed (I saw the line in the failing run of the first test, `A_reference_followed_by_a_line_break_is_refused`). No test failed on a drawn value in 38 runs.
- The refinement lambdas: `Any.SupplementaryPolicy().WithPeriod(p => p.StartingOn(...).LastingDays(60)).WithSchedule(...)`, `Any.Prescription().Dispensed().WithIssuedOn(i => i.Between(...))` read exactly like the intent, and "refinement narrows the base" held every time.
- `Any.Decimal().Between(0.01m, price).WithScale(2)` for money and `Any.Enum<Currency>().DifferentFrom(Currency.EUR)` were one-liners; `Any.PermutationOf(...)` for picking distinct lines.
- The scaffolded leaf generators (`Money`, `Dosage`, `Deductible`, `ReimbursementRate`) needed no edit at all and the `guard` column was right every time (including the named constants: `Money.MaximumAmount`, `DispensedLine.MaxPacks`, `Deductible.Ceiling`).
- The `TODO`-as-build-error design: every unresolved guard stopped the build at the right line; I never shipped a generator that could draw an invalid value (0 domain exceptions from a generator in 38 runs of 213 cases).
- Dummy discipline stayed natural: amounts and rates that the assertion is about are literals (`Scenarios.ScheduleOf((Major, 65m))`), everything else (names, ids, IBANs, reference, dispensing day inside a window) is drawn.

## Fluency: 4 / 5
Day-to-day writing was fast and the drawn values never got in the way, but the day was dominated by completing scaffolds for a heavily tied graph (retyping for regexes, hand-writing the claim and period generators, removing dead `With…` methods) and by two start-up hiccups (MTP `global.json` on the .NET 10 SDK, `any` before the first build). None blocked me for long and every error pointed at the cause; the cost is mechanical editing, not understanding.

## Domain defects found (tests kept, skipped)
- `ClaimReference`: the format `^...$` also matches before a trailing `\n`, so `"CLM-2026-000001\n"` is accepted and kept as `Value` (`ClaimReferenceTests.A_reference_followed_by_a_line_break_is_refused`, no draw involved).
- `ClaimReference`: `\d` matches any Unicode digit; `"CLM-2026-" + six Arabic-Indic zeros` passes the "sequence 000000 is refused" rule and is accepted (`A_sequence_written_in_non_ascii_digits_is_refused`).
- `Insurance.Domain.PolicyNumber` (03, used by `SupplementaryPolicy`): same `\d` issue and same trailing-newline acceptance (probed, not kept as a test); an Arabic-Indic year is accepted by `PolicyNumber` and then `new SupplementaryPolicy(...)` throws a bare `FormatException: The input string '٢٠٢٦' was not in a correct format.` from `int.Parse`
  (`SupplementaryPolicyTests.A_policy_number_written_in_non_ascii_digits_is_refused_when_it_is_created`).
