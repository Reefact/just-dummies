# Expert notes — 10 pharma serialisation
Agent: the same model and effort as every session of this trial. Library: JustDummies 1.0.0-uxtrial.1 (packed from main).

## What I did

**Pages read, in order:** `PROJECT-README.md` (front page, skimmed), `README.md` (index), `guides/getting-started.en.md`,
`guides/core-concepts.en.md`, `packages/justdummies-cli.en.md` (all of it), `packages/justdummies-xunit.en.md`,
`generators/README.md`, `numbers.en.md`, `dates-and-times.en.md`, `strings.en.md`, `enums-and-choices.en.md`,
`collections.en.md` (first half), `guides/composition.en.md` (first half), `guides/reproducibility.en.md` (first half),
`analyzers/README.md`. I read the domain (`MODEL.md` + every `Domain/*.cs`) before touching the tool.

**Project:** `Tests.Expert/Serialisation.Domain.Tests.Expert.csproj` from the template, unchanged. I had to add a
`global.json` at the workspace root to be able to run `dotnet test --project` (friction 1).

**`any` commands run** (from `Tests.Expert`):
1. `any generate Gtin BatchNumber SerialNumber ShelfLife ReimbursementPrice SerialisedPack Batch RecallNotice Shipment ExpiryMonth --output Generators`
   (first look: 9 written, `ExpiryMonth` refused).
2. Deleted `Generators/` and re-ran the same nine types with `--output Generators --entry-point any`, so the tests read
   `Any.Batch()` instead of `new AnyBatch()`. Nothing had been edited yet, so no `--force` was needed.

**Generators completed by hand** (the scaffold left four of the nine unbuildable, on purpose):
* `AnyGtin` — the `Validate(value);` helper was `to verify`. A GTIN is thirteen free digits plus a derived check digit, so
  the recipe became `Any.StringMatching("[0-9]{13}").As(body => Gtin.FromBody(body).Value)`, which is an `IAny<string>`:
  I retyped the factory, the recipe field and the recipe constructor, and deleted the refining overload (there is nothing
  to refine on an `IAny`).
* `AnyBatchNumber` — regex guard; kept `AnyString` with `WithChars("A-Z0-9-").WithLengthBetween(1, 20)`, so no retype.
* `AnySerialNumber` — `switch`-expression guard unread; `Any.String().AlphaNumeric().WithLengthBetween(8, 20)`.
* `AnyShipment` — the reference regex became a hand-written `AnyShipmentReference : IAny<string>` (with its own
  `Any.ShipmentReference()` entry point) so the rule lives once; the `Distinct()` guard on serials became
  `Any.ListOf(new AnySerialNumber()).NonEmpty().Distinct()`.
* `AnyRecallNotice` — `ArgumentOutOfRangeException.ThrowIfLessThan(effectiveFrom, issuedOn)` was not recognised as an order
  between two parameters (friction 2). I copied the pin + `Any.OrderedPair` machinery `AnyBatch` had been given for its
  `<=` guard (non-strict this time) into the generated file by hand.
* Dates: every scaffolded `Any.DateOnly()` was bounded to 2000-01-01..2099-12-31 (the century `ExpiryMonth` and the
  `yyMMdd` of the element string can express). Not a constraint the constructors declare, a domain fact I chose.
* `AnyBatch.Custom.cs` (a `partial` beside the generated file): `WithRoomFor(int packs)`, because "at least n packs
  planned" is said in thirteen tests.

**Tests written (173 test methods, 271 cases, 8 skipped), in `Tests.Expert/Tests/`:**
* `GtinTests` — four real check-digit vectors incl. the sum-is-a-multiple-of-ten case (`0`, not `10`), "only digit 1 is
  right for this body", wrong check digit, null, 13/15 digits, empty, letter, spaces, equality/hash, `ToString`, `FromBody`
  vectors, null/12/14/letter/non-ASCII-digit bodies. 2 skipped (domain defects).
* `BatchNumberTests`, `SerialNumberTests` — accepted ranges, both length boundaries, length judged before alphabet,
  blank judged before length, null vs blank, equality (case-sensitive serials). 1 skipped each.
* `ExpiryMonthTests` — range of year and month with `ParamName`, `LastDay` for every month length, leap years incl. 2000,
  equality on each component.
* `ShelfLifeTests` — bounds, `ExpiryFor` (month-end clamping, leap day, year wrap, 60 months), equality.
* `ReimbursementPriceTests` — to-the-cent rule (`1.500` accepted, `1.005` refused), 0 / ceiling / ceiling+0.01 split between
  range and scale refusals, equality across scales (`5.3m` vs `5.30m`).
* `SerialisedPackTests` — starts Active, keeps its parts, three null guards with `ParamName`, expiry unchecked,
  element string. 1 theory (3 cases) skipped (culture).
* `BatchConstructionTests`, `BatchCommissioningTests` (duplicate by value, case, full, duplicate-before-full,
  `CommissionAll` order / empty / null / stops at first failure / fills midway / null element),
  `BatchVerificationTests` (expiry day itself authentic, day after expired, status wins over expiry, expired stays Active),
  `BatchDecommissioningTests` (dispense, recall incl. expired-but-active, second recall = 0),
  `BatchComputationTests` (`RemainingShelfLifeRatio` on an eight-day life so 0.125 and 0.625 expose the midpoint rule,
  clamping on both sides, thirds; `ActiveStockValue`).
* `RecallNoticeTests` — fields, equal date accepted, day before refused, undeclared classes, reason null / empty / blank /
  500 / 501, **the three-way check order**, `ReachesPatients` per class.
* `ShipmentTests` — reference matrix, null reference is an `ArgumentException` (not the null subtype), serials null / empty /
  repeated by value / case-different, check order, `SuspiciousIn` (unknown + dispensed + recalled in shipment order,
  expired not listed, dispensed-and-expired listed, shipping day is the verification day). 3 skipped.

**Dummies vs literals (my reading of the rule):** a value drawn with `Any.*` is one the assertion cannot depend on: the
batch/GTIN/price of a batch under a verification test, the *content* of a too-long serial (only its length is the
point), the class of a notice whose date is wrong. Everything an assertion names stays a literal: all dates in
verification and ratio tests, serials whose order or duplication is the story, prices, expected strings. Equality tests
are on literals because "equal whatever the value" is a property and JustDummies runs it with a sample of one.

**Result:** green on the first full run (`dotnet test --project Tests.Expert`); then 15 + 12 + 12 further runs (fresh seeds
each), all `failed: 0`, 271 cases, 8 skipped. The 8 skipped cases were each un-skipped once and confirmed to fail for the
stated reason; none of them involves a drawn value (no seed to quote). I also made one test fail on purpose to see the seed
report: `[JustDummies] These arbitrary values were seeded with 1599355521. Reproduce this run with
[JustDummies.Xunit.Reproducible(Seed = 1599355521)].` appeared under "Standard output" of the failing test, as documented.

**Domain defects found (all skipped with a reason):**
1. `Gtin`, `BatchNumber`, `SerialNumber`, `Shipment.Reference` accept a trailing `\n` (`$` also matches before a final newline).
2. `Gtin(string)` and the shipment reference accept non-ASCII decimal digits (`\d`); `Gtin.FromBody` refuses them.
3. `SerialisedPack.ToElementString()` depends on the current culture (`th-TH`: `(17)700331` for 2027-03-31).
4. `Shipment.SuspiciousIn(null)` throws `NullReferenceException`, not `ArgumentNullException`.

## Frictions and surprises, in order

1. **`dotnet test --project Tests.Expert` fails out of the box.** Command: `dotnet test --project Tests.Expert` →
   `MSBUILD : error MSB1001: Unknown switch ... Switch: --project`. Without `--project`, `dotnet test` in the project folder
   gives `error : Testing with VSTest target is no longer supported by Microsoft.Testing.Platform on .NET 10 SDK and later.
   If you use dotnet test, you should opt-in to the new dotnet test experience.` (xunit.v3 4.0.2 is an MTP runner).
   Expected: the xunit adapter page to say how to run the suite. It says how to install and use `[Reproducible]` and
   nothing about running. Did: added `{ "test": { "runner": "Microsoft.Testing.Platform" } }` as `global.json`; then the
   command worked. A newcomer on .NET 10 loses the first ten minutes here, and the library docs are the natural place for the hint.
2. **`any` does not read the order guard `ArgumentOutOfRangeException.ThrowIfLessThan(effectiveFrom, issuedOn)`.**
   Tool output: `issuedOn  DateOnly  Any.DateOnly()  to verify, unread guards` and
   `effectiveFrom ... to verify, unread guards` / `unread guard at RecallNotice.cs, line 8: ArgumentOutOfRangeException.ThrowIfLessThan(effectiveFrom, issuedOn);`.
   The equivalent `if (expiresOn <= manufacturedOn) throw` in `Batch` is paired automatically into `Any.OrderedPair`
   (`AnyOrderedPair<DateOnly> _manufacturedOnAndExpiresOn`, strict). The docs list `ThrowIfNegative` and siblings *on one parameter*; the
   two-argument `ThrowIfLessThan/GreaterThan(a, b)` is the exact modern spelling of the same rule and goes unrecognised.
   Expected: the same pairing (non-strict, since `effectiveFrom == issuedOn` is accepted). Did: the doc's "When a guard ties
   two parameters" section offers three forms, none of which keeps the generated `With…` pins working; I copied `AnyBatch`'s
   pin/pair mechanism into `AnyRecallNotice` by hand (about 30 lines touched in a generated file). `--force` would drop it.
3. **A scaffold that "writes the neutral recipe" for a derived value is the wrong shape for the fix.** For `Gtin`
   (`Validate(value);` helper, check digit derived from the digits) the file says
   `TODO(any): verify against Gtin.cs, line 11: Validate(value);` — correct and honest. But the cheap fix (`.As(...)` to derive
   the check digit) returns an `IAny<string>`, so the typed `AnyString` field, constructor parameter and the `Func<AnyString, AnyString>`
   overload all have to be rewritten/deleted. The docs describe this retype for `AnyPattern` and for `.As`; it is
   accurate, just four edits for that one parameter (`Gtin`); for `BatchNumber`, `SerialNumber` and `Shipment.reference` I picked
   `WithChars` / `AlphaNumeric` / `StartingWith(...).Numeric()` precisely because they keep the `AnyString` type and spare the retype.
4. **`ExpiryMonth` is refused: `nothing here constructs it`.** Output: `Generate() needs a public instance constructor whose
   parameters are all passed by value, or one public static Create/From/Of/Parse taking a single argument and returning the type.`
   `ExpiryMonth.Of(int year, int month)` is the idiomatic shape of a value object with a private constructor and a two-argument
   factory. The message is clear and tells me exactly what is missing, so I hand-wrote nothing (every `ExpiryMonth` test uses
   literals), but a two-parameter `Of` is common enough in domain code to deserve scaffolding.
5. **Scaffolded date ranges are the whole calendar.** `AnyBatch`, `AnySerialisedPack`, `AnyRecallNotice`: `Any.DateOnly()`
   draws 0001-01-01..9999-12-31, so a freshly scaffolded `Any.Batch()` can be manufactured in year 1 and expire in 9999;
   `ShelfLife.ExpiryFor` on such a date, or a `yyMMdd` element string, would behave strangely. This is documented in
   `dates-and-times.en.md` ("An unconstrained draw spans the whole type"), and the tool cannot know my century, so I bounded
   the four factories by hand. The recap shows `Any.DateOnly()` with no flag, as for any unconstrained row; I would not have noticed
   without having read the dates page first.
6. **Info diagnostics are silent, and the one that mattered was only visible after I raised the severity.** `dotnet build` printed
   nothing; with `dotnet_analyzer_diagnostic.category-JustDummies.Constraints.severity = warning` it reported
   `JD033: Numeric() cannot draw the 'S' this literal holds, so it appears only where you wrote it` on
   `Any.String().StartingWith("SHP-").Numeric().WithLength(12)`. That is exactly what I intended, and the message explains it
   well, but it was a second-guess moment: "is this a bug?". Documented behaviour (the analyzers page says Info rules are
   hidden by default). I removed the `.editorconfig` afterwards and replaced the call with `Any.StringMatching("SHP-[0-9]{8}")`.
7. **The generated namespace is the domain's own (`Serialisation.Domain`), inside the test assembly.** Default per the tool
   (`in namespace Serialisation.Domain (--namespace to change)`). It is convenient (tests in `Serialisation.Domain.Tests.Expert`
   see `Any.Batch()` with no `using`), but it puts types in a namespace the domain assembly also owns; the day the domain adds
   an `AnyGtin` the compiler will say `CS0436`-style ambiguity. Not an issue today; I kept the default.
8. **Reading `Assert.Throws` exactness against the model.** Not a library matter, but the combination is worth noting: xunit's
   `Assert.Throws<ArgumentException>` is exact, so the MODEL's "null → `ArgumentException`" (`Shipment`, `Gtin.FromBody`) versus
   "null → `ArgumentNullException`" (`Gtin(string)`) was testable without extra ceremony. The drawn-content / literal-length
   split worked naturally here (`Any.String().Numeric().WithLength(13)` for "a 13-digit body").
9. **Thirteen identical `WithPlannedQuantity(q => q.GreaterThanOrEqualTo(n))` call sites.** The lambda is the right shape
   (it builds on the base `Positive().LessThanOrEqualTo(500000)` and cannot widen it), but a test that says "the batch must
   have room for n packs" says it as a mechanism. I added `WithRoomFor(int)` in `AnyBatch.Custom.cs`, as the CLI page
   suggests; it survives `--force`. This is the pattern working as documented, I am only logging that every aggregate with a
   capacity will want it.
10. **`Any.SerialNumber().DifferentFrom(anySerial)`** worked on a scaffolded generator (it is an `IAny<SerialNumber>`) and reads
    exactly as the intent ("another serial than the commissioned one"). No friction, mentioned because I expected to need
    `Any.ListOf(...).WithCount(2).Distinct()`.

## What worked well

* **The `Batch` scaffold was nearly perfect.** `OrderedPair` for `manufacturedOn < expiresOn`, with `WithManufacturedOn` /
  `WithExpiresOn` pinning one side and bounding the other, made every date-sensitive test a two-literal sentence:
  `Any.Batch().WithManufacturedOn(...).WithExpiresOn(...)`. Nothing about the batch's gtin, price or lot number needs saying.
* **Composition of owned generators** (`new AnyGtin()` inside `AnySerialisedPack`, `AnyBatch`) and the reuse of the type's own
  validity rule meant a bad base recipe is a build failure in one file, not forty tests failing at random.
* **The `to verify` build stop.** All three regex-guarded value objects and the `Distinct()` list refused to compile until I
  looked, and every guard they pointed at was a real one. The line number and the quoted statement saved a search each time.
* **`[assembly: Reproducible]` plus the seed report** — one line for the whole project, and the failing-test output names
  the attribute to paste. The failure I forced replayed as documented.
* **Doc samples were copy-adaptable**: `Any.OrderedPair` (non-strict), the `Gtin` check-digit shape from the `Luhn` sample,
  `DifferentFrom` on a scaffolded generator, `WithChars` for a regex alphabet.
* **The dummy rule shaped the suite.** Applying "the value the assertion is about is not a dummy" removed most of my
  instinct to draw: dates, serials in orderings, prices and expected strings stayed literals; only batch plumbing and the
  content of malformed inputs are drawn. Reading the tests afterwards, every `any…` is visibly something that does not matter.

## Fluency: 4 / 5
The library and the tool let me go from nothing to a 271-case green suite quickly, and the batch-style date pairing, `to
verify` stops and seed report were genuinely good. I lose a point because the first command a newcomer types
(`dotnet test --project …`) fails on .NET 10 with nothing in the docs about it, and another for the handful of places the
scaffold's help ran out and I edited a generated file by hand (two-argument `ThrowIfLessThan`, two-argument static factory,
retyping to `IAny<string>`), each of which was documented as possible but not as cheap.
