# Expert notes — 45 healthcare claims
Agent: the same model and effort as every session of this trial. Library: JustDummies 1.0.0-uxtrial.1 (packed from main).

## What I did
**Pages read, in order:** `PROJECT-README.md`, `guides/getting-started`, `guides/composition`, `packages/justdummies-xunit`,
`packages/justdummies-cli` (whole page, it is the one that decides the afternoon), `generators/numbers`, `enums-and-choices`,
`dates-and-times`, `guides/core-concepts`, `generators/collections`. Skimmed only: `strings` (headings; I used
`Any.StringMatching` and `Any.String().Alpha().WithLengthBetween` from the other pages), `reproducibility`, `errors-and-conflicts`.

**`any` commands run** (from `Tests.Expert`, output in `Generators/`, always `--entry-point any`):
1. `any generate ReimbursementRate ReimbursementSchedule ClaimReference ClaimAssessment DispensedLine SupplementaryPolicy ReimbursementClaim --output Generators --entry-point any`
2. `any generate PolicyNumber CoveragePeriod Deductible Nir Patient Money Iban Party Bic Molecule Dosage TreatmentDuration PrescriptionLine Prescription --output Generators --entry-point any`
   (the three referenced domains' types, because a claim needs a dispensed prescription and a payee; 21 generators in all, `any` found the foreign
   types through the project references with no extra flag).

**Generators completed by hand** (the tool's own work list: the 31 `CS0103 TODO_verify_the_generator_for_*` errors of the first build):
- one-liners once I had read the guard: `PolicyNumber`, `Bic`, `Molecule` (`Any.StringMatching`, retyped `AnyString` to `AnyPattern` exactly as the
  `TODO` comment told me), `TreatmentDuration`, `Prescription.prescriberRpps`, `Nir`, `Iban`, `ClaimReference` (`.As(...)` / `Any.Combine` over the domain's own
  `Nir.ComputeKey` and `Iban.ComputeCheckDigits`; `IAny<string>` retype because the replacement ends in `.As`).
- ties between parameters, written once in the generator and kept out of the tests:
  `CoveragePeriod` (both ends drawn inside one calendar year, which the scaffolded `Any.OrderedPair` then orders, so the one-year cap holds by construction),
  `Patient` (NIR follows the birth date; a pinned NIR derives its birth date), `Party` (agent drawn in the account's country), `ClaimAssessment`
  (deductible and reduction share what is covered, pins respected), `DispensedLine` (no statutory refund on an `Unlisted`, never above the price paid),
  `PrescriptionLine` (units that go with every route, so the tablet/puff switch never fires), `Prescription` (issue date after the birth date, plus a hand-written
  `Dispensed()` that signs and dispenses), `SupplementaryPolicy` (rewritten: deductible strictly below ceiling through `Any.OrderedPair(..., strict: true)`,
  number issued no later than the period's year), `ReimbursementClaim` (rewritten: draws the prescription first, then a dispensing day inside its window, then
  lines of that very prescription; `Dispensing(Category, UnitPrice, Packs, Statutory)` records let a test state its lines in euros).
- custom partial files beside the scaffold (`AnyNir.BornOn`, `AnyBic.InCountry`, `AnyPolicyNumber.IssuedBetween`, `AnyPrescription.Dispensed`).
- `.editorconfig` switches the `JustDummies.Constraints` category to warning so JD030 is visible in the build.

**Tests written:** 12 classes, 179 test methods, 226 cases (theory rows included).
`ReimbursementRateTests` (10), `ReimbursementScheduleTests` (11: coverage, refusals, defensive copy, equality), `ClaimReferenceTests` (9 + 2 skipped defects),
`ClaimAssessmentTests` (17 + 1 skipped defect), `DispensedLineTests` (16: every guard on both sides of its boundary), `SupplementaryPolicyTests` (15),
`ReimbursementClaimTests` (21: prescription status, dispensing window both ends, line rules, totals, benefit year across a year end),
`ClaimFileSubmissionTests` (17: the three refusals and their order, the rejected-prescription exception, all four occupying statuses),
`ClaimFileAssessmentTests` (28: rate, remaining-charge cap and its boundary, rounding per line, deductible, ceiling after deductible, which statuses hold the ceiling, two-year period),
`ClaimFileDecisionTests` (16: approve/reject incl. NothingToReimburse vs AnnualCeilingExhausted vs stale assessment, every refusal reason),
`ClaimFilePaymentTests` (12: the transfer field by field, closed days, same account, stays Approved on refusal), `GeneratorsTests` (7: my own generators, incl. replay from a seed).

**Result:** green: 223 passed, 3 skipped (domain defects below), 0 failed. After the first green run I ran the suite 72 more times with fresh seeds
(12 interactively, 60 in a loop): every run green, no seed ever reported.
Command: `dotnet test --project Tests.Expert` (needs the `global.json` of friction 1).

## Frictions and surprises, in order
1. **`dotnet test --project Tests.Expert` does not run on the .NET 10 SDK as the brief and the adapter page imply.** First output:
   `MSBUILD : error MSB1001: Unknown switch.` (`--project .`), then with the path form
   `error : Testing with VSTest target is no longer supported by Microsoft.Testing.Platform on .NET 10 SDK and later. If you use dotnet test, you should opt-in to the new dotnet test experience`.
   I had to add `{ "test": { "runner": "Microsoft.Testing.Platform" } }` as a `global.json` at the workspace root. Nothing in `justdummies-xunit.en.md` says
   the xUnit v3 adapter needs that on a current SDK. Docs, low (the test template could carry it).
2. **The scaffolded set is a work list, and a good one, but it is long for a transverse domain.** After the two `any generate` runs the first `dotnet build` gave 31
   `CS0103` errors, one per `to verify` parameter, each at its line. That is the design and it worked: I never forgot a gap. But 14 of the 21 generators were for types
   of other domains, and half of the 31 are the same decision (a regex value object: `Nir`, `Bic`, `PolicyNumber`, `Iban`, `Molecule`) taken five times.
3. **Regex guards are never translated, by design, and each costs a manual retype.** `PolicyNumber`: `if (!Format.IsMatch(value))` produced
   `Any.String()` + sentinel, and the comment told me exactly what to do ("retype AnyString to AnyPattern here, on the recipe field, in the recipe constructor and in the Func"). I did it with a
   `sed 's/AnyString/AnyPattern/g'`, five times. It works, but nothing in the tool lets me say "this regex is in the supported subset, translate it" once. Idea, low.
4. **The `to verify` mark on tied parameters points at one line for several parameters.** `DispensedLine`: `category` and `packs` both say
   `unread guard at DispensedLine.cs, line 35: if (category == ReimbursementCategory.Unlisted)`; the guard that really involves the pack count is the line-39 comparison with `pricePaid`.
   `statutoryReimbursement` carries the same pointer. I read the constructor myself to find the three real ties (Unlisted, currency, price paid). ADR-level behaviour ("marks every parameter
   it may depend on") is documented, so this is a small defect in how helpful the message is rather than a bug. Defect, low.
5. **Ties between parameters of different types are invisible to the tool, and each one cost me edits in 4–5 places of a generated file.** `CoveragePeriod` start/end was recognised
   (`Any.OrderedPair(...).WithFirst/WithSecond` came out of the box, nice), but the one-year cap was left as `to verify` over two unbounded `Any.DateOnly()`, which would also overflow
   `start.AddYears(1)` near 9999. For `Patient` (NIR must carry the birth year/month), `Party` (agent in the account's country), `DispensedLine`, `ClaimAssessment` and
   `SupplementaryPolicy` I derived one draw from another in `Generate()`, as the page recommends, and then every `With…` pin on the derived parameter went inert, as the page warns.
   To keep `WithNir(...)`, `WithAgent(...)`, `WithStatutoryReimbursement(...)`, `WithDeductibleApplied(...)` honest I added a `_xxxPinned` flag, copied it in the copy constructor, set it in the pin
   method and branched in `Generate()`, five times. That boilerplate is what a generator with derived parameters needs and the scaffold gives no seam for it. Friction/idea, medium: a documented
   "derived parameter" shape (or a `Func<previous draws, IAny<T>>` recipe) would remove most of it.
6. **A composed domain (policy, claim) cannot be scaffolded into something usable at all.** `ReimbursementClaim` marks three of four parameters `to verify`, with five ties to the prescription (status,
   dispensing window, line count, line identity, no repeat). I threw the scaffold away and wrote a generator whose API is the claims vocabulary (`ForInsured`, `DispensedOn`, `Dispensing(...)`). The recap said as much
   (`to verify`), so this is not a surprise, just the honest price of a rich aggregate. Docs: the "When a guard ties two parameters" section gave me the right four forms; none shows a tie *through an object* (a line must be a line of *that* prescription).
7. **`WithAnnualCeiling`/`WithDeductible` through `Any.OrderedPair(strict: true)` worked first time**, including both pins together (`250` / `250.01`). I mention it as friction only because the domain test for "deductible equal to the ceiling is refused" cannot go
   through the generator, which (rightly) refuses the contradiction itself with its own `ConflictingAnyConstraintException`: those domain refusals are tested with the constructor and a drawn valid policy's parts (`Rebuilt(...)`), which reads well but is a pattern the docs do not name.
8. **A pinned invalid value flows through derived draws and can turn into the library's exception instead of the domain's.** `Any.DispensedLine().WithPacks(0)` first threw
   `ConflictingAnyConstraintException: Cannot apply LessThanOrEqualTo(0.00) because Positive() already requires values greater than 0.` (from my own `Generate()` bounding the statutory refund by the price paid), not the
   `ArgumentOutOfRangeException` the test was about. The message is accurate and names both sides; the surprise was mine, but it shows how a derived draw hides a refusal. Fixed in the generator (`pricePaid <= 0m` leaves the constructor to refuse).
9. **`[assembly: Reproducible]` prints a seed for a failing test that draws nothing.** The two skipped defect tests, unskipped to prove they fail, each reported
   `[JustDummies] These arbitrary values were seeded with 626885724...` although no dummy is drawn in them. Harmless noise, low.
10. **JD030 (Info) is the only analyzer I saw, and only after raising the category.** `dotnet_analyzer_diagnostic.category-JustDummies.Constraints.severity = warning` (the documented line) made it appear for the two `Any.String().NotBlank()` of the `Patient` names; a
    guess at `category-JustDummies.severity` showed nothing. No JD diagnostic ever fired in the tests themselves, which is either the quality of the tests or the narrowness of the rules; I could not tell which.
11. **Pinning the value a test is about versus a neutral literal needed a decision every time.** In the claim-file tests the deductible and the ceiling are never what the test is about, yet an approved claim needs something payable, so a random
    deductible (0 to 5000) makes a random claim unpayable: the first run of the "already claimed" theory failed with `ClaimRefusedException: Claim CLM-2091-666994 leaves nothing to reimburse` in two of the four theory rows. The draw was right and my test was wrong: the deductible was not a dummy there. I pinned `WithDeductible(0m).WithAnnualCeiling(...)` in the scenario helper. Good example of the library doing its job, but it is also the sort of thing a `[Reproducible]` seed makes cheap to debug.

## What worked well
- **The recap table and the build-as-work-list.** `to verify` plus a non-compiling sentinel at the line is exactly right for a domain with this many regex value objects and ties; at no point did a plausible-but-wrong generator hide.
- **`Any.OrderedPair(..., strict: true)` with `WithFirst/WithSecond`** for deductible-below-ceiling, and its scaffolded use for `CoveragePeriod`. I did not have to write a retry loop anywhere.
- **`IAny<T>.DifferentFrom(value)` on any generator**, used for "a NIR that is not the insured's", "an account that is not the payee's", "a claim reference different from this one" (via `Equals`).
  It made three precondition-heavy tests read as one line.
- **`.As(...)` / `Any.Combine` over the domain's own helpers** (`Nir.ComputeKey`, `Iban.ComputeCheckDigits`): the dummies are valid by the real code, not by a copy of it.
- **Refinement lambdas and pins on scaffolded generators** (`Any.Prescription().WithIssuedOn(...).WithLines(l => l.WithCount(3)).Dispensed()`): once the base is right, the test states only what it cares about.
- **Replay.** `Any.UseSeed(n)` drew the same policy and the same claim twice (my `GeneratorsTests` pins it), and the failure messages carried a `[Reproducible(Seed = ...)]` snippet to paste.
- **Library error messages** (`Cannot apply LessThanOrEqualTo(-3996.92) because Positive() already requires values greater than 0.`) always named both sides.
- Domain defects found *by reading with the dummies in mind* (not by a draw): three, all skipped with a reason: `ClaimReference` accepts a trailing line feed; accepts an all-zero sequence in non-ASCII digits; `ClaimAssessment.ToString()` is culture-dependent (`"75,00 EUR payable"` under fr-FR).

## Fluency: 4 / 5
The path from `any generate` to a green, replayable suite was short for the 60 % of types that are value objects and flat records, and the tool's refusal to guess made every gap visible and local. It dropped to "I am writing the generator myself" for exactly the
types where the domain is interesting (ties between parameters and through objects), and there the scaffold gave me the shape but also five flag fields to maintain by hand; that is the single place where the expert experience still costs real boilerplate. The test-writing side
(pins for what the test is about, `Dispensing` records for amounts, literals on every boundary, 72 fresh-seed runs green) was fluent, and the library never once surprised me with a draw that was not a legitimate counter-example.
