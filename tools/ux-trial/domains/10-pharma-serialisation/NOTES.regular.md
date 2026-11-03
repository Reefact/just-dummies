# Regular notes — 10 Pharma serialisation
Agent: the same model and effort as every session of this trial. Library: JustDummies 1.0.0-uxtrial.1 (packed from main).

## What I did
- Read, as a reminder: `PROJECT-README.md`, `packages/justdummies-cli.en.md` (the guard tables, "When a guard ties two parameters", "Adjusting a parameter"),
  `packages/justdummies-xunit.en.md`, `guides/core-concepts.en.md`, `guides/composition.en.md`, `generators/README.md`, `dates-and-times.en.md`, `strings.en.md`.
  Then `MODEL.md` and the whole `Domain/`.
- Test project `Tests.Regular/Serialisation.Domain.Tests.Regular.csproj` from the template, `dotnet restore`.
- `any generate Gtin BatchNumber SerialNumber ExpiryMonth ShelfLife ReimbursementPrice SerialisedPack Batch RecallNotice Shipment` (one run, 10 types).
  9 written, `ExpiryMonth` refused (static `Of(year, month)` has two arguments); 5 files did not compile on purpose (TODO sentinels).
- Completed by hand:
  - `AnyGtin`: base is `Any.StringMatching("[0-9]{13}").As(body => Gtin.FromBody(body).Value)` (check digit is computed, not drawn), retyped to `IAny<string>` (field, recipe ctor, `Func`).
  - `AnyBatchNumber` -> `Any.StringMatching("[A-Z0-9-]{1,20}")`, `AnySerialNumber` -> `Any.StringMatching("[A-Za-z0-9]{8,20}")`, `AnyShipment.reference` -> `Any.StringMatching("SHP-[0-9]{8}")` (all retyped to `AnyPattern`);
    `AnyShipment.serials` got `.Distinct()`.
  - `AnyRecallNotice`: `ThrowIfLessThan(effectiveFrom, issuedOn)` was not recognised as an order, so I wrote the tie in `Generate()` with `Any.OrderedPair(_issuedOnRecipe, _effectiveFromRecipe)`
    and re-created the pin fields (`_issuedOnPin`/`_effectiveFromPin`) the way `AnyBatch` does, so `WithIssuedOn(x)` / `WithEffectiveFrom(x)` still reach the draw.
  - No generator for `ExpiryMonth`: drawn inline with `Any.Int32().Between(...)`.
  - `Fixtures.cs`: `BatchLiving(made, expires)` (pins both dates, at least 50 planned packs), `DistinctSerials(n)`, `DatesOfTheModernEra()`. `[assembly: Reproducible]` in `AssemblyInfo.cs`.
- Tests (147 methods, 234 cases): `GtinTests`, `BatchNumberTests`, `SerialNumberTests`, `ExpiryMonthTests`, `ShelfLifeTests`, `ReimbursementPriceTests`, `SerialisedPackTests`, `BatchTests`
  (construction, Commission/CommissionAll incl. partial failure and duplicate-before-full ordering, Verify incl. expiry-day boundary, Dispense, Recall, RemainingShelfLifeRatio with midpoint rounding, ActiveStockValue),
  `RecallNoticeTests` (check order, class, reason length), `ShipmentTests` (reference, distinct serials, SuspiciousIn).
  Boundaries and literals are written down (expiry day, 8-day batch for away-from-zero rounding, 7/8/20/21 characters); everything the test does not care about is drawn.
- Result: first run 229 green + 5 red, all 5 domain defects (below), marked `Skip = "domain defect: ..., seed ..."`. Final: 229 passed, 5 skipped, 0 failed.
  Re-run 12 more times with fresh seeds: green every time (229 / 5 skipped).

## Frictions and surprises, in order
1. **`dotnet test --project Tests.Regular` does not work out of the box.** First `MSBUILD : error MSB1001: Unknown switch ... --project` (SDK 10.0.114, VSTest mode);
   `dotnet test Tests.Regular` then gives `Microsoft.Testing.Platform.MSBuild.targets(395,5): error : Testing with VSTest target is no longer supported by Microsoft.Testing.Platform on .NET 10 SDK and later`.
   I had to add a `global.json` with `{ "test": { "runner": "Microsoft.Testing.Platform" } }` at W's root. Neither the xunit adapter page nor the getting-started page mentions it (`grep global.json docs` finds nothing).
   Expected: the package page to say what a net10 consumer needs. Done: added the file; after that `dotnet test --project` worked.
2. **`any generate` on a not-restored project**: `✗ Gtin: this project has not been restored, so its package references cannot be read. Restore it, then run the command again: dotnet restore`.
   Clear message, one `dotnet restore`, fine. (With 10 types in the command it stopped at the first before analysing the others.)
3. **`ExpiryMonth` refused**: `✗ ExpiryMonth: nothing here constructs it. Generate() needs a public instance constructor ... or one public static Create/From/Of/Parse taking a single argument`.
   `Of(year, month)` takes two. Message is accurate and tells the rule, but no hint on what to do for a private-ctor, two-argument factory (I just drew `Any.Int32()`s inline). Low cost.
4. **Regex guards in a validated value object always end as a hand job (4 of 10 types)**: `Gtin` (`Validate(value);` helper, `to verify, unread guards`), `BatchNumber`, `SerialNumber`, `Shipment.reference`.
   Expected and documented (retype `AnyString` to `AnyPattern` on 4 lines), and the TODO comment says exactly which lines. For `SerialNumber` the guard is inside a `switch` expression: the tool printed the regex in the comment but did not read the `{ Length: < 8 or > 20 }` arm,
   so the base suggested was bare `Any.String()` (no length) — I had to notice the length myself when writing `{8,20}`. Same for `Gtin`, whose length/check-digit sit in a `Validate(value)` helper that returns void.
5. **`ThrowIfLessThan(effectiveFrom, issuedOn)` between two parameters is not read as an order**, whereas `if (expiresOn <= manufacturedOn) throw` in `Batch` is (it scaffolded an `AnyOrderedPair`, very nice).
   The doc table lists "`a > b`, `a >= b` between two parameters" and "`ArgumentOutOfRangeException.ThrowIfNegative(p)` and its siblings" but not the two-argument `ThrowIfLessThan/GreaterThan(a, b)`.
   `RecallNotice` came out `to verify, unread guards` on both dates (the sentinel names the line, good). Expected `OrderedPair` as for the `if` form. What I did: wrote the pair in `Generate()` by hand, plus pin fields to keep `WithIssuedOn(x)` working;
   that is ~15 lines of boilerplate copied by imitation from `AnyBatch.cs` — the "Derive one draw from another inside `Generate()`" doc paragraph warns that pins stop reaching the draw, and gives no recipe to keep them.
6. **Default date draws are the whole `DateOnly` range (0001..9999)** for `manufacturedOn`/`expiresOn`/`shippedOn`/`today`. Perfectly valid, and I like the honesty, but every test that does `AddDays`/`AddYears` on a drawn date needs a bound
   (`Fixtures.DatesOfTheModernEra()`), and `Any.DateOnly().Generate()` in `SerialisedPack` tests is fine only because nothing is added. Documented in dates page ("bound it before doing date arithmetic"); I still had to remember it every time.
7. **Pin that contradicts the order is refused by the generator, not by the domain**: to test "expires on the day it is made is refused" I cannot go through `new AnyBatch().WithManufacturedOn(d).WithExpiresOn(d)` (the pair refuses it first),
   so those tests call `new Batch(...)` with generated parts. Logical, and an even better guard rail, but it means the refusal tests of order-tied types don't use the generator at all.
8. **Nullable-parameter pins need casts**: `new AnyBatch().WithGtin((Gtin)null!)` — without the cast `WithGtin(null)` is ambiguous between the pin and the lambda overload (expected from the docs, still noisy for the 4-5 "null is refused" tests per type).
9. **Generic `DifferentFrom` on `IAny<string>` after retyping `AnyGtin` to `IAny<string>`**: `new AnyGtin().WithValue(v => v.DifferentFrom(other))` works (redraw), only noticing that the semantics changed from the builder's declaration-time check to a bounded redraw.
   No problem here; just something I would not see when reading the call site.
10. Seeds are printed as negative numbers (`-1867228342`) and `[Reproducible(Seed = -1867228342)]` is what the message says to use; fine, but `Skip = "... seed ..."` for literal-input defects is a bit meaningless since the seed has no influence.

Not a library friction, but the domain: **the five domain defects** found by cases a reviewer would write (none found by a drawn value; all by literals):
- `Gtin`, `BatchNumber`, `SerialNumber`, `Shipment.Reference`: `^...$` (without `\z`) accepts a trailing `\n`: `new Gtin("04012345678901\n")`, `new BatchNumber("LOT1\n")`, `new SerialNumber("ABCD1234\n")`, `new Shipment("SHP-12345678\n", ...)` are all accepted
  (the `Value` then carries the line feed, and `ToElementString()` would print it). Four skipped tests.
- `Gtin`: `\d` matches Unicode digits; `new Gtin("١٢٣٤٥٦٧٨٩٠١٢٣3")` is accepted (check digit computed on code points), whereas `Gtin.FromBody` rejects non-ASCII digits. One skipped test.

## What worked well
- The scaffold run is fast (about 4 s for ten types) and the recap table is honest: every regex/helper guard was `to verify`, nothing silently wrong. The compile error on the sentinel is a good work list.
- `Batch` came out with an `AnyOrderedPair` for `expiresOn > manufacturedOn` and `WithManufacturedOn/WithExpiresOn` pins that bound each other; `Fixtures.BatchLiving(made, expires)` is two lines.
- Composing through `new AnyGtin()` etc. meant a `Batch`'s dummies were always domain-valid; I never had a constructor refusal on a draw in 13 runs.
- `.As(body => Gtin.FromBody(body).Value)` as the base of `AnyGtin` — the doc's "draw the body, then derive the check digit" recipe maps directly on a real factory.
- `[assembly: Reproducible]` once, seeds printed on failure, replay line copy-pastable. `Distinct()` on lists, `Any.Char().Numeric().Except(c)`, `Any.DateOnly().After(x)` all behaved.
- The 5 red tests on first run were all real domain defects, with the failing input visible in the test, nothing flaky across 13 runs.

## Fluency: 4 / 5
Daily use is smooth: scaffolding, composing through the owned generators and the `[Reproducible]` adapter all worked at the first attempt, and the compile-error work list is a good design. Points lost for the test-runner setup
(`global.json` for `dotnet test --project` on net10 is undocumented in the package pages) and for the amount of by-hand boilerplate when a guard is a two-parameter `ThrowIfLessThan` or a void validation helper
(`RecallNotice`, `Gtin`, `SerialNumber` all needed a careful second read of the domain code because the tool's sentinel only quotes the first unread guard).
