# Expert notes — 01 payments
Agent: the same model and effort as every session of this trial. Library: JustDummies 1.0.0-uxtrial.1 (packed from main).

## What I did
Pages read, in order: `PROJECT-README.md`, `guides/getting-started`, `guides/core-concepts`, `packages/justdummies-cli` (whole: the
"what the last column means", guard tables, "Adjusting a parameter", "When a guard ties two parameters"), `packages/justdummies-xunit`,
`guides/composition`, `guides/reproducibility` (first half), `generators/README`, `numbers`, `dates-and-times`, `strings`,
`enums-and-choices`, `collections` (first half).

`any` commands run (from `Tests.Expert/`):

    any generate Bic Iban Money RemittanceInformation Party FeePolicy CreditTransfer PaymentBatch --entry-point any --output ./Arbitrary

One run, eight types, sixteen files (`AnyX.cs` + `AnyX.Entry.cs`). Result: 7 generators with a `to verify` mark on some parameter
(Bic, Iban, RemittanceInformation, Party, FeePolicy, CreditTransfer, PaymentBatch); only `Money` was clean.

Generators completed by hand (all edits in `Tests.Expert/Arbitrary/`):
- `AnyBic`: regex guard -> `Any.StringMatching("[A-Z]{6}[A-Z0-9]{2}([A-Z0-9]{3})?")`, retyped `AnyString` -> `AnyPattern` on 4 lines
  (as the TODO said). Beside it, `AnyBic.Recipes.cs`: `InCountry(cc)`.
- `AnyIban`: body `[A-Z0-9]{11,30}` plus a country, the two check digits *computed* with `Iban.ComputeCheckDigits` in an `Any.Combine`;
  recipe typed `IAny<string>`. `AnyIban.Recipes.cs`: `InCountry(cc)`.
- `AnyRemittanceInformation`: `Any.String().WithChars(<SEPA set>).NotBlank().WithMaxLength(MaximumLength)` (kept `AnyString`, no retype).
- `AnyParty`: name `NotBlank().WithMaxLength(70)`; the agent/account country tie written in `Generate()` (a drawn agent is
  re-homed in the account's country; a pinned agent is never moved, a disagreeing pin is refused by `Party` on every run), with
  a `_agentPinned` flag added in the generated file.
- `AnyFeePolicy`: rates `Between(0m, 0.05m)`.
- `AnyCreditTransfer`: amount `WithCurrency(EUR)`, date `Any.DateOnly().As(BusinessCalendar.NextBusinessDayOnOrAfter)` (retype
  `AnyDateOnly` -> `IAny<DateOnly>`), and the instant-ceiling tie in `Generate()` (drawn amount re-drawn under the ceiling, or
  drawn priority downgraded when the amount is pinned) with two `_…Pinned` flags.
- `AnyPaymentBatch`: transfers = `Any.Party().Then(debtor => Any.ListOf(new AnyCreditTransfer().WithDebtor(debtor)) … .DistinctBy(EndToEndId))`
  (one debtor, distinct ids, 1 to 100), recipe retyped `IAny<IReadOnlyList<CreditTransfer>>`; `WithTransferCount(n)` in `AnyPaymentBatch.Recipes.cs`.
- `internal static` accessors to the private `AnyValid…()` bases (`ValidAmounts`, `ValidRates`, `ValidEndToEndIds`, …) in `*.Recipes.cs`
  files, so constructor-refusal tests can spoil one argument and keep the others valid.

Tests (156 methods, 306 cases, in `Tests/`, `[assembly: Reproducible]` on the whole suite):
`BicTests` (format, country, primary office, refusals, equality), `IbanTests` (known IBANs, format refusals, check digits, mod 97-10
computation, equality), `MoneyTests` (bounds, decimals, check order, `Add`, equality/hash, text), `RemittanceInformationTests`
(charset, 140/141, blank, not trimmed), `PartyTests` (trim, 70/71, agent country), `FeePolicyTests` (guards and their order, instant
and standard fees, rounding away from zero), `BusinessCalendarTests`, `CreditTransferTests` (construction, every refusal and the order
they are judged in, instant ceiling, cross-border, the whole Draft/Submitted/Executed/Rejected transition table), `PaymentBatchTests`
(counts, control sum, 1/100/101, debtors, ids, what is not checked), `EnumTests`.

Result: `dotnet test --project Tests.Expert` green, 301 passed, 5 skipped (the 5 `Skip = "domain defect: …"` below), then 25 more runs
in a row (fresh seeds each, 306 total, 0 failed).

Choices a reviewer would ask about: literals are used wherever the value *is* the test (IBAN examples, amounts and rates of fee
computations, dates around holidays, 70/71, 140/141, 100/101, country codes "FR"/"DE"); drawn values are the things a test must have and
does not look at (currency in `Money` refusals, parties and ids around a transfer, message ids, creation times). No `anyXxx`
reaches an assertion, except `Assert.Same(...)` on objects that were passed in and are named without `any` for that reason.

## Frictions and surprises, in order
1. **`dotnet test --project Tests.Expert` does not run as the brief says.** Exact output:
   `MSBUILD : error MSB1001: Unknown switch. … --project .` then, with `dotnet test Tests.Expert`,
   `error : Testing with VSTest target is no longer supported by Microsoft.Testing.Platform on .NET 10 SDK and later. If you use dotnet test, you should opt-in to the new dotnet test experience.`
   The SDK is 10.0.114 and the project is an `Exe` on xunit.v3. Expected `--project` to work out of the box; the adapter docs only say
   `dotnet add package`. Fix: a `global.json` with `{ "test": { "runner": "Microsoft.Testing.Platform" } }` in the domain folder. Not a
   JustDummies defect, but the docs never say a test project on xunit.v3 needs it on .NET 10.
2. **Every scaffolded file stops the build, by design, and the fixes are not equally mechanical.** 7 of 8 types came out `to verify`
   (the 8th, `Money`, was fully inferred, with `ThrowIfNegativeOrZero`, the `MaximumAmount` constant and `decimal.Round(p, 2) != p` read
   correctly: that is the best moment of the trial). Regex guards (`Bic`, `RemittanceInformation`) and the void helper
   `Validate(value)` (`Iban`) are the documented cases. `FeePolicy.ValidateRate(rate, nameof(rate))` (a `void` helper of the type
   that throws on `rate < 0m || rate > 0.05m`) is also unread, consistent with the docs ("a `void` helper called for its effect is
   marked") but the guard is the trivial `Between(0, 0.05)` shape the tool reads when written inline: the tool could say "this helper
   only compares its parameter with constants".
3. **Cross-parameter rules have no seam short of editing `Generate()` in the generated file.** Four ties in this domain: agent/account
   country, instant priority/amount ceiling, debtor/creditor accounts, and, in the batch, one debtor + distinct ids for 1 to 100 transfers.
   The doc's advice ("derive one draw from another inside `Generate()`", "pin both") is right, but then `WithAgent(pin)` etc. no longer
   mean what they say unless I add a flag (`_agentPinned`, `_amountPinned`, `_priorityPinned`) in the private copy constructor, in the
   pin overload and in `Generate()`: 3 places per flag, all in the file `any generate --force` overwrites. The partial class cannot
   help because `Generate()` is not a partial method. Wish: a documented hook (e.g. a `partial` step between the draws and the
   constructor call) so the tie lives in my own file.
4. **Typed refinements stop when the recipe stops being a builder.** After `.As(...)` / `Any.Combine(...)`/`Then`, the field is `IAny<T>`, so
   `WithValue(Func<IAny<string>, IAny<string>>)` and `WithTransfers(Func<IAny<…>>)` have nothing to refine. For `Iban` I wrote `InCountry`
   as `WithValue(_ => IbanOf(Any.Fixed(cc), AnyBban()))`, the "ignores its argument, replaces the base" form the docs warn about; here it
   is a named recipe, as the docs allow, but it is a copy of the base minus the country. The same for `AnyBic.InCountry`
   (`AnyPattern` has no `StartingWith`/`Containing`, so the country has to be spelled in a second pattern).
5. **The private `AnyValid…()` base is what constructor-refusal tests need, and the file keeps it private.** To spoil one argument
   of `new Money(…)`/`new FeePolicy(…)`/`new CreditTransfer(…)` and keep the others valid, I added `internal static` accessors in a
   `*.Recipes.cs` file for each (the docs anticipate it: "add one line to your own file"). Seven of them here: a signal that the
   scaffold could emit them as an option (`--expose-bases`).
6. **A valid-looking arrangement can still fail on the draw, and the failure is the tie, not the library.** My first
   `CreditTransferTests` used a drawn priority with a drawn euro amount up to 999 999 999.99: `ArgumentException : An instant transfer is
   capped at 100000 EUR. (Parameter 'amount')` on `An_end_to_end_id_of_35_characters_is_accepted`, seed -622187322. Not flaky: the helper
   `AnyEuros()` was not valid for every priority. Fixed by declaring the invariant in the helper (`LessThanOrEqualTo(InstantCeiling)`)
   instead of pinning `Normal`, which would have looked deliberate.
7. **`Any.Decimal()` with only `GreaterThanOrEqualTo(0m)` (scaffolded for `instantFlatFee` and `freeStandardThreshold`) draws up to
   `decimal.MaxValue` one time in ten.** Harmless here because the tests that depend on those fees pin them, but a test using a drawn flat
   fee in an instant computation would overflow `flat + amount × rate` near the top. I kept the scaffold (the domain declares no
   ceiling) and did not hit it.
8. **Fresh seeds are negative integers** (`seeded with -1803635014`). They replay fine through `[Reproducible(Seed = -1803635014)]`; just
   noting that the docs' examples are all positive.
9. **Domain quirks found while writing the cases a reviewer expects** (none found by a drawn value; see the findings): `$` in the
   three regexes accepts a final line feed (`Bic`, `RemittanceInformation`, and `Iban`, where it escapes as a `FormatException`);
   `PaymentBatch` keeps the caller's list; `Money.ToString()` follows the machine culture; `ParamName` of the length guards is
   `"text.Length"`/`"endToEndId.Length"`; IBAN check digits `00`, `01` and `99` are accepted (ISO 13616 only issues 02 to 98).

## What worked well
- The `to verify` mechanism, again: the build named each gap at the line, with the guard quoted and, for regexes, the exact retype
  to apply. Nothing I forgot to read compiled.
- `Money`: a fully inferred generator that names my constants (`Money.MaximumAmount`) and applies `WithScale(2)` from `decimal.Round`.
- `Any.Combine` + the domain's own `ComputeCheckDigits` for IBAN, and `Then` + `DistinctBy` for the batch: the invariants are built, not
  filtered, and each reads as the rule it expresses.
- `[assembly: Reproducible]`: one line, every test reports its seed on failure, and the one failure I met (friction 6) was replayable.
- `DifferentFrom` on a scaffolded generator (`Any.Iban().DifferentFrom(other)`, `Any.Bic().WithCode(c => c.DifferentFrom(...))`) and
  on `Any.Enum<Currency>()` kept "two different X" tests free of literals.
- Nested lambdas through generated `With…` (`WithDebtor(d => d.WithAccount(a => a.InCountry("FR")))`) read as the sentence the test means.
- The test names and the literal/dummy split fell out of the docs' rule easily: refusal tests draw everything but the refused argument.

## Fluency: 4 / 5
Everything the domain's *own* guards describe came out of the tool, and everything it could not read was flagged at the line and
fixed in a few minutes with the docs open. What keeps it from 5 is the cost of cross-parameter rules: four of them needed hand edits to
`Generate()` and bookkeeping flags in a file the tool overwrites, and the one place a draw failed (friction 6) was my own missing
invariant, not the library. The one blocking surprise before the first test was the `dotnet test --project` setup (friction 1),
which is outside the library but costs the first ten minutes.
