# Discovery notes — 10 pharma serialisation
Agent: the same model and effort as every session of this trial. Library: JustDummies 1.0.0-uxtrial.1 (packed from main).

## What I did
Pages read, in order: `docs/PROJECT-README.md`, `docs/README.md` (index), `guides/getting-started.en.md`,
`guides/core-concepts.en.md`, `packages/justdummies-cli.en.md` (the `any` page: recap columns, "what the tool reads",
"when a guard ties two parameters", "when the replacement is another kind of generator"), `packages/justdummies-xunit.en.md`
(first 150 lines), `generators/strings.en.md` (for `StringMatching`), parts of `generators/dates-and-times.en.md` and
`generators/collections.en.md` (grep for `Distinct`), the `Combine`/`OrderedPair` parts of `guides/composition.en.md` (grep only).
I did not read errors-and-conflicts, reproducibility, the FAQ or the analyzer pages.

`any` commands run, from `Tests.Discovery/`:
1. `any generate Gtin BatchNumber SerialNumber ExpiryMonth ShelfLife ReimbursementPrice SerialisedPack Batch RecallNotice Shipment`
   (first attempt before `dotnet restore` failed with a clear message; second attempt wrote 9 files, refused `ExpiryMonth`).

Generators completed by hand:
- `AnyGtin`: replaced the `to verify` base by `Any.StringMatching("[0-9]{13}").As(body => Gtin.FromBody(body).Value)`, retyped the
  recipe field / constructor parameter / `Func` to `IAny<string>` (the scaffold's own TODO text only talks about `AnyPattern`, the
  `.As` case is in the docs, one section further).
- `AnyBatchNumber`: `Any.StringMatching("[A-Z0-9-]{1,20}")`, retyped `AnyString` -> `AnyPattern` everywhere (as the TODO says).
- `AnySerialNumber`: `Any.StringMatching("[A-Za-z0-9]{8,20}")`, same retype.
- `AnyShipment`: reference `Any.StringMatching("SHP-[0-9]{8}")`; serials `.NonEmpty().Distinct()`.
- `AnyRecallNotice`: the two-parameter `ThrowIfLessThan(effectiveFrom, issuedOn)` was not understood (`to verify`); I deleted the
  sentinels and drew the pair in `Generate()` with `Any.OrderedPair(_issuedOnRecipe, _effectiveFromRecipe, strict: false)`
  (so the `WithIssuedOn`/`WithEffectiveFrom` members are now dead, as the docs warn).
- `AnyExpiryMonth`: written from scratch (`Any.Combine(year, month, ExpiryMonth.Of)`), because `any` refused the type.
- `AnyBatch`, `AnyShelfLife`, `AnyReimbursementPrice`, `AnySerialisedPack`: kept as scaffolded.

Tests written (6 files + helper, all `[assembly: Reproducible]`, 114 test methods, 213 test cases, 4 skipped):
- `GtinTests` (valid, `FromBody` check digit x3, wrong check digit, null, malformed x6, `FromBody` refusals x6, equality, inequality, trailing LF).
- `BatchNumberTests` (valid x4, 20 chars, 21 chars, length-before-alphabet, null, blank x3, bad characters x5, equality, inequality, trailing LF).
- `SerialNumberTests` (lengths 8/13/20, out of range 0/7/21/500, null, bad characters x4, case sensitivity, equality, trailing LF).
- `ExpiryMonthTests` (window edges, out of range x5, param name, `LastDay` x6 incl. leap/2000, equality, inequality).
- `ShelfLifeTests` (bounds, refusals x5, `ExpiryFor` x8 incl. month-end clamping and year wrap, equality).
- `ReimbursementPriceTests` (accepted x6 incl. 100 000, range refusals x5, scale refusals x3, range-before-scale x3, `12.5m` vs `12.50m` equality and hash).
- `SerialisedPackTests` (state after construction, element string literal, `yyMMdd` zero padding, three null refusals).
- `BatchTests` (construction, dates, quantity bounds, nulls, `Commission` incl. duplicate/full/duplicate-first, `CommissionAll` incl. partial failure,
  `Verify` for every outcome and the expiry day, `Dispense`, `Recall`, `RemainingShelfLifeRatio` clamping and midpoint rounding, `ActiveStockValue`).
- `RecallNoticeTests` (properties, `ReachesPatients` x3, dates, undefined class x4, null batch/reason, blank reason accepted, 500/501 limit, order of checks).
- `ShipmentTests` (properties, reference refusals x10, null/empty/duplicate serials, case-sensitive distinctness, check order, `SuspiciousIn` x4, trailing LF).

Result: green with `dotnet test --project Tests.Discovery` (209 passed, 0 failed, 4 skipped), then 12 further runs, all green with 209 passed.
The 4 skipped tests are the same domain defect (see friction 7); I removed the `Skip` temporarily to confirm they fail, then restored it.

## Frictions and surprises, in order
1. **`dotnet test --project Tests.Discovery` does not work out of the box on the .NET 10 SDK.** First `dotnet test --project ...` ->
   `MSBUILD : error MSB1001: Unknown switch ... --project Tests.Discovery`. Then `dotnet test Tests.Discovery` ->
   `Microsoft.Testing.Platform.MSBuild.targets(395,5): error : Testing with VSTest target is no longer supported by Microsoft.Testing.Platform on .NET 10 SDK and later. ... opt-in to the new dotnet test experience`.
   Fixed by adding `global.json` with `{ "test": { "runner": "Microsoft.Testing.Platform" } }` in the domain folder. Not a JustDummies defect, but
   the docs (getting started, xunit page) never tell a newcomer that `JustDummies.Xunit` needs xUnit v3 *and* this runner opt-in on the .NET 10 SDK.
   The only hint at the SDK is one line in the README ("pinned in global.json").
2. **`any generate` before `dotnet restore`.** Output: `✗ Gtin: this project has not been restored, so its package references cannot be read. Restore it, then run the command again: dotnet restore`.
   Clear and actionable; I only note that the CLI page's first example does not mention the restore prerequisite.
3. **`any` refuses `ExpiryMonth`**: `✗ ExpiryMonth: nothing here constructs it. Generate() needs a public instance constructor whose parameters are all passed by value, or one public static Create/From/Of/Parse taking a single argument and returning the type.`
   The message is exact and I understood it (`Of(year, month)` takes two arguments), but a two-argument static `Of`/`Create` is the commonest value-object
   shape after the constructor. I wrote the generator by hand with `Any.Combine(...)` (5 lines). The run exit summary (`1 of 10 types refused`) was good.
4. **The scaffolded file for a value-object string wrapper does not compile and the TODO text only half-fits.** `AnyGtin` was marked `to verify, unread guards` because the
   guard is in a private `Validate(value)` method (a `void` helper). The comment just says `verify against Gtin.cs, line 11: Validate(value);` — no hint about what to put, and
   the rule is a *check digit*, which the docs mention ("a check digit computed from the digits before it") but in the section about two parameters. I found `.As(body => ...)` there;
   the retype to `IAny<string>` was spelled out in another section ("When the replacement is another kind of generator") — I had to combine three places.
5. **Regex-guarded types: the scaffold tells exactly what to do.** For `BatchNumber`, `SerialNumber`, `Shipment.reference` the TODO quoted the pattern and said "retype AnyString to AnyPattern here, on the recipe field, in the recipe constructor and in the Func of WithValue".
   I followed it literally and it compiled first time. Worth a mention as good. Slight surprise: `BatchNumber` was scaffolded `Any.String().NotBlank().WithMaxLength(20)` with the
   regex guard *and* two real guards read, and the row carries `to verify, guard, unread guards` — fine.
6. **`ThrowIfLessThan(effectiveFrom, issuedOn)` is not recognised as an order between two parameters** (`RecallNotice`), while the `if (expiresOn <= manufacturedOn) throw` in `Batch` was
   (it became an `AnyOrderedPair`). Output: `issuedOn: unread guard at RecallNotice.cs, line 8: ArgumentOutOfRangeException.ThrowIfLessThan(effectiveFrom, issuedOn);`, both parameters `to verify`.
   The docs list `ThrowIfNegative` "and its siblings" as read; a reader of that table would expect the two-argument `ThrowIfLessThan/GreaterThan` to be read too. I wrote the pair by hand
   (`Any.OrderedPair(..., strict: false)` in `Generate()`) as the docs describe; the doc page for it was good.
7. **DOMAIN defects (4 occurrences, one cause): `$` in the validation regexes accepts a trailing line feed.** `Gtin` (`^\d{14}$`), `BatchNumber` (`^[A-Z0-9-]+$`), `SerialNumber` (`^[A-Za-z0-9]*$`) and
   `Shipment.Reference` (`^SHP-\d{8}$`) all accept `value + "\n"`: e.g. `new Gtin("04006381333931\n")` does not throw (15 characters, `Value` keeps the LF), `new BatchNumber("LOT-1\n")`,
   `new SerialNumber("ABCD1234\n")`, `new Shipment("SHP-12345678\n", ...)` likewise. Realistic: data scanned from a DataMatrix or read from a file often carries a trailing LF.
   I kept the four tests and marked them `Skip = "domain defect: ..."`. Unskipped they fail with `Assert.Throws() Failure: Expected: typeof(System.ArgumentException) Actual: No exception was thrown`.
   (`\d` also matches non-ASCII digits in .NET; I did not probe that.) Drawn values never exposed this because `Any.StringMatching` draws inside the pattern's language; only a literal finds it.
8. **Dummy range surprise with `Any.DateOnly()`**: it draws over `0001-01-01 .. 9999-12-31`. Tests that do date arithmetic on a drawn date (`AddDays(±n)`) would overflow at the extremes, so I
   never did arithmetic on a fully drawn date; I pinned the batch expiry to a literal and drew `today` with `Any.DateOnly().Before(Expiry)` / `.After(Expiry)`. It's documented (dates page) but easy to forget —
   and the scaffolded `AnyBatch` yields batches whose manufacture/expiry span centuries, which no real batch has. I did not constrain it in the generator (domain accepts it).
9. **`AnyBatch` with a drawn `PlannedQuantity`**: the scaffold draws `1..500000`, so a test commissioning several serials fails on the draws where the batch is too small (e.g. planned = 1).
   I caught it by reasoning before running (no failure seen), and made a helper `AnyBatchWithRoomFor(n)` using `.WithPlannedQuantity(p => p.GreaterThanOrEqualTo(n))`. The lambda refinement worked nicely.
10. **Pinning both sides of an ordered pair equal is refused by the generator, not the domain**: `new AnyBatch().WithManufacturedOn(d).WithExpiresOn(d).Generate()` throws
   `ConflictingAnyConstraintException: Cannot apply WithSecond(06/14/4813) because WithFirst(06/14/4813) already requires values greater than 06/14/4813.` — correct and readable, but it means the
   domain's own refusal (`expiresOn <= manufacturedOn`) can only be tested by calling `new Batch(...)` directly. My test first used the generator and got the wrong exception type; I switched to the constructor.
   (Also: the dates in the message print in `MM/dd/yyyy` regardless of ISO convention; minor.)
11. **My own slips, not the library's**: a wrong expectation (0.995 rounds to 1.00, not 0.99) and a test that recalled the whole batch and therefore also the "genuine" serial. Both were caught by the first run.
   The seed was printed for each failure in the standard output, as documented.
12. **No analyzer diagnostic ever surfaced** in `dotnet build` (infos do not print in the terminal, as the docs say), so I cannot report on them either way.

## What worked well
- The `any` recap table: the `to verify` / `guard` / `AnyX` words and the one-line "unread guard at File.cs, line N: <the statement>" made the work list obvious.
- Sentinel `TODO_verify_...` lines: the build told me exactly which generators were unfinished; nothing was drawn silently.
- Guards were turned into constraints correctly for `ShelfLife`, `ReimbursementPrice` (`.WithScale(2)`, `.Positive().LessThanOrEqualTo(100000m)`), `Batch` quantity and the date order (`AnyOrderedPair`, with `With…` pins working in both directions).
- Composition through `new AnyGtin()` etc. across the aggregate worked immediately; `AnySerialisedPack().WithExpiresOn(...)` / `AnyBatch().WithPlannedQuantity(p => ...)` read well.
- `Any.StringMatching` with `.As(...)` for the check digit was a one-liner, and `Any.ListOf(...).WithCount(n).Distinct()` gave distinct value-object serials with no ceremony.
- `[assembly: Reproducible]`: one line, and failures printed `[JustDummies] These arbitrary values were seeded with N. Reproduce this run with [JustDummies.Xunit.Reproducible(Seed = N)]`.
- The "is it a dummy?" rule from getting-started is genuinely actionable: it made me pin the dates, price and class when the assertion was about them, and draw serials, GTINs, batch numbers where they only had to exist.

## Fluency: 4 / 5
From docs to a green 213-case suite took one scaffold run, six hand edits to generators and no wrestling with the library; the scaffolder's diagnostics told me what was left to do. Points lost for the
.NET 10 `dotnet test` / Microsoft.Testing.Platform opt-in that the newcomer docs never mention, the refusal of a two-argument `Of` factory, the unrecognised two-parameter `ThrowIfLessThan` guard, and
the need to stitch three doc sections together for a check-digit value object.
