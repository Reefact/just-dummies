# 01-payments: SEPA credit transfer (UX trial notes)

## Domain model (`Domain/`, namespace `Payments.Domain`)

| Type | Guards (shapes deliberately varied) |
| --- | --- |
| `Iban` | `ThrowIfNullOrWhiteSpace`; private `Validate`: regex `^[A-Z]{2}[0-9]{2}[A-Z0-9]{11,30}$` + ISO 13616 mod-97 check digits |
| `Bic` | combined `code is null \|\| !Regex.IsMatch(...)` (K2 shape, not re-reported) |
| `Money` | `ThrowIfNegativeOrZero`, `ThrowIfGreaterThan(amount, MaximumAmount)`, `decimal.Round(amount, 2) != amount`, `Enum.IsDefined` |
| `RemittanceInformation` | `ThrowIfNullOrWhiteSpace`, `ThrowIfGreaterThan(text.Length, 140)`, SEPA character-set regex |
| `Party` | `ThrowIfNull`, `name = name.Trim()` then `name.Length is 0 or > 70`, cross-field: agent BIC country == IBAN country |
| `CreditTransfer` (aggregate) | end-to-end id 1..35, EUR only, debtor != creditor account, TARGET2 business day, instant capped at 100 000 EUR; state machine Draft -> Submitted -> Executed/Rejected |
| `FeePolicy` | `ThrowIfNegative` + rates delegated to a private `ValidateRate`; `FeeFor` computes the fee |
| `PaymentBatch` (aggregate) | 1..100 transfers, one debtor, unique end-to-end ids; `ControlSum` |
| `BusinessCalendar` | static: weekends (switch expression) + fixed TARGET2 holidays |
| enums | `Currency`, `TransferStatus`, `TransferPriority`, `ChargeBearer` (no zero member) |

## CLI recap (verbatim, `any generate Iban Bic Money RemittanceInformation Party CreditTransfer FeePolicy PaymentBatch --entry-point static:Arbitrary`)

```
  value  string  Any.String().NotBlank()  to verify, guard, unread guards                       (Iban)
  code  string  Any.String().NonEmpty()  to verify, unread guards                               (Bic)
  amount    decimal   Any.Decimal().Positive().LessThanOrEqualTo(999999999.99m)  to verify, guard, unread guards
  currency  Currency  Any.Enum<Currency>()
  text  string  Any.String().NotBlank()  to verify, guard, unread guards                        (RemittanceInformation)
  name     string  Any.String().NonEmpty()  to verify, unread guards                            (Party)
  account  Iban    new AnyIban()            to verify, AnyX, unread guards
  agent    Bic     new AnyBic()             to verify, AnyX, unread guards
  endToEndId              string                 Any.String().NonEmpty()         to verify, guard, unread guards
  debtor                  Party                  new AnyParty()                  to verify, AnyX, unread guards
  creditor                Party                  new AnyParty()                  to verify, AnyX, unread guards
  amount                  Money                  new AnyMoney()                  to verify, AnyX, unread guards
  requestedExecutionDate  DateOnly               Any.DateOnly()                  to verify, unread guards
  priority                TransferPriority       Any.Enum<TransferPriority>()    to verify, unread guards
  remittanceInformation   RemittanceInformation  new AnyRemittanceInformation()  AnyX
  instantFlatFee         decimal  Any.Decimal().GreaterThanOrEqualTo(0m)  guard
  instantRate            decimal  Any.Decimal()                           to verify, unread guards
  freeStandardThreshold  decimal  Any.Decimal().GreaterThanOrEqualTo(0m)  guard
  standardRate           decimal  Any.Decimal()                           to verify, unread guards
  messageId  string                         Any.String().NotBlank()              guard
  createdAt  DateTime                       Any.DateTime()
  transfers  IReadOnlyList<CreditTransfer>  Any.ListOf(new AnyCreditTransfer())  to verify, unread guards
```

Every guard the CLI did not translate was flagged `to verify`: the recap was honest throughout, and no
emitted recipe it vouched for ever drew a rejected value. Also run: `--dry-run`, `--format json`
(with and without `--dry-run`), `--entry-point any` (dry-run), `--force`, and enum targets.

```
$ any generate TransferStatus
Analyzing Payments.Domain.TransferStatus
  constructor TransferStatus()
✓ AnyTransferStatus.cs — no constructor parameters to infer.
```
The emitted `Generate()` is `return new TransferStatus();` (always 0). Same for `ChargeBearer`, which
has no 0 member, so the generator always returns an undeclared value.

## What I fixed by hand

* `AnyIban`: the base is no longer an `AnyString` (mod-97 cannot be a string constraint): `Any.Combine`
  of a country and `Any.StringMatching("[A-Z0-9]{11,30}")`, check digits computed. The scaffolded
  `WithValue(Func<AnyString, AnyString>)` had to be deleted. Added `InCountry` (`AnyIban.Custom.cs`).
* `AnyBic`: `Any.StringMatching` of the guard's own regex; `WithCode` lambda retyped to `AnyPattern`; `InCountry`.
* `AnyMoney`: added `.WithScale(2)`, referenced `Money.MaximumAmount`.
* `AnyRemittanceInformation`: `WithChars(SEPA set).NotBlank().WithMaxLength(140)`.
* `AnyParty`: name `Printable().NotBlank().WithMaxLength(70)`; the cross-field country rule can only be
  encoded by giving account and agent the same default country (`DE`) and an `InCountry` that moves both.
* `AnyCreditTransfer`: amount pinned to EUR and capped at the instant ceiling; execution date drawn from
  a pool of business days 2020-2034 (`Any.DateOnly().OneOf(BusinessDays)`), because no "weekday /
  business day" constraint exists.
* `AnyPaymentBatch`: one shared debtor needs a dependent draw; there is no bind/SelectMany, so the
  composer of an `Any.Combine` calls `Generate()` on a nested generator. `WithTransfers(Func<...>)` deleted.

## Findings (details in the structured result)

1. **high, CLI** - `any generate <Enum>` emits a generator returning `new TEnum()` (always 0): never
   varies, and draws an undeclared value when the enum has no 0 member. `Bug_Scaffolded_*` tests.
2. **medium, CLI** - `--format json` on an already-existing file reports `"status": "Scaffolded"`,
   `summary.scaffolded: 1, failed: 0` while nothing was written and the exit code is 1.
3. **medium, analyzer** - JD028 fires on `Any.ListOf(x.As(...)).Distinct()` but not on
   `Any.ListOf(new AnyRemittanceInformation()).Distinct()` nor on a held `IAny<T>`: the shape the CLI emits.
4. **medium, CLI** - a nullable reference parameter (`Bic? agent`) is scaffolded as `IAny<Bic>` /
   `WithAgent(Bic value)`: pinning null warns CS8625.
5. **low, CLI** - `ThrowIfGreaterThan(text.Length, N)` (idiomatic .NET 8 length guard) is not read as `WithMaxLength(N)` (flagged, so safe).
6. **medium, missing capability** - no business-day/weekday constraint, no dependent draw; workarounds above.
7. **low, message** - `Any.Decimal().GreaterThan(100m).LessThan(10m)`: "GreaterThan(100) already requires
   values greater than or equal to 100" (it is strict; behaviour is correct, wording is not).
8. **low, doc** - distinct count above an element's cardinality (63 one-char alphanumerics) ends in a
   draw-budget `AnyGenerationException`, not the immediate cardinality refusal the collections page promises.
9. **low, analyzer** - decimal/date constant contradictions have no analyzer (JD023 is integer-only); JD014 and
   JD017 both fire on the same `OneOf((Currency)42)`.

Positives: conflict messages name both sides (decimal, date, pool, enum, scale); unsupported regex
constructs (lookahead, `\p{Lu}`, `[A-Z-[AEIOU]]`) are refused with construct + position; seeds are
reported on failure and replay a whole scaffolded batch; pool inspection is excellent; `WithScale(2)` exact.

## Fluency: 4 / 5

Writing tests with the scaffolded `Arbitrary.X().WithY(y => ...)` reads very well and the analyzers are
quiet and precise. It costs points where a payments domain is most demanding: check-digit identifiers,
business dates and cross-field invariants all needed hand-written structure the scaffold shape resists
(`AnyString`-typed bases, per-parameter factories, no dependent draw).
