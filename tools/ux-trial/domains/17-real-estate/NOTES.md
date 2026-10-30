# 17 — Real estate / land registry

French land-registry flavour: cadastral parcel references, Carrez-law surfaces, the 2021 DPE
energy rating (A–G) and the Climate and Resilience Act letting calendar, https document links,
rental listings and ownership transfers. Stresses regex formats, a `double` surface on a 0.01 grid,
an A–G enum tied to figures by a cross-field rule, URIs, and `DateOnly` windows.

## Domain model (`Domain/`)

| Type | Guards (shape) |
| --- | --- |
| `CadastralReference(string)` | `ArgumentException.ThrowIfNullOrWhiteSpace`, a `static readonly Regex` (`^(\d{5}\|2[AB]\d{3}) \d{3} [A-Z0-9]{2} \d{4}$`), then `EndsWith("0000")` refused |
| `Surface(double)` | `if (double.IsNaN)`, `ThrowIfNegativeOrZero`, `ThrowIfGreaterThan(x, MaxSquareMeters const)`, `Math.Round(x, 2) != x` |
| `EnergyRating` | enum `A..G` |
| `EnergyPerformanceDiagnosis(int, int, EnergyRating, DateOnly)` | `ThrowIfNegative` ×2, `issuedOn < ReformDate` (static readonly), `ThrowIfGreaterThan(issuedOn, LatestIssueDate)`, cross-field: declared rating must equal `Rate(energy, emissions)` (two switch expressions) |
| `DocumentLink(DocumentKind, Uri, DateTime)` | all delegated to a private `Validate`: `Enum.IsDefined`, `ThrowIfNull`, absolute, `https` only, `DateTimeKind.Utc` |
| `Property(CadastralReference, PropertyKind, Surface, EnergyPerformanceDiagnosis, IReadOnlyList<DocumentLink>)` | `ThrowIfNull` ×4, enum checked by a switch expression throwing, `Count > 20`, distinct locations (LINQ) |
| `RentalListing(Property, decimal, DateOnly, DateOnly)` | `ThrowIfNegativeOrZero`, `decimal.Round(x, 2) != x`, `from >= until`, `property.CanBeLetOn(from)` (a policy call) |
| `Transfer(CadastralReference, decimal, DateOnly, DateOnly)` | `ThrowIfNegativeOrZero`, `ThrowIfGreaterThan(price, 1e9m)`, cross-field `ThrowIfLessThan(registeredOn, signedOn)` |

Behaviour: department of a reference (2A/2B, overseas 97x), Carrez 8 m² threshold, surface addition on
the hundredth, worse-of-two-bands rating, 10-year validity, "energy sieve" (F/G), letting calendar
(G from 2025, F 2028, E 2034, commercial exempt), document folder (20 max, no duplicate), re-diagnosis
ordering, price per m², deposit cap, 5.80665 % transfer duty, late registration (> 30 days).

The draw paid off once before any test ran: an `issuedOn` drawn up to `DateOnly.MaxValue` made
`IssuedOn.AddYears(10)` overflow — a real defect of my domain, fixed with the `LatestIssueDate` guard.

## CLI recaps (verbatim)

```text
$ any generate CadastralReference Surface EnergyPerformanceDiagnosis --entry-point static:Arbitrary
  value  string  Any.String().NotBlank()  to verify, guard, unread guards
  squareMeters  double  Any.Double().Positive().LessThanOrEqualTo(10000000d)  to verify, guard, unread guards
  primaryEnergyKwhPerSquareMeterYear  int           Any.Int32().GreaterThanOrEqualTo(0)  guard
  emissionsKgCo2PerSquareMeterYear    int           Any.Int32().GreaterThanOrEqualTo(0)  guard
  declaredRating                      EnergyRating  Any.Enum<EnergyRating>()             to verify, unread guards
  issuedOn                            DateOnly      Any.DateOnly()                       to verify, unread guards

$ any generate DocumentLink --entry-point static:Arbitrary --format json
  kind Any.Enum<DocumentKind>() / location Any.Uri().Web() / uploadedAtUtc Any.DateTime() — all "requiresVerification": true, "unread guards"
  "summary": { "scaffolded": 1, "failed": 0, "openParameters": 0, "parametersToVerify": 3 }

$ any generate Property --entry-point any
✗ Property: several types answer to that name.
  Microsoft.ApplicationInsights.Extensibility.Implementation.Property
  Microsoft.Testing.Platform.Extensions.Messages.PropertyBag.Property
  Microsoft.Testing.Platform.ServerMode.Client.Protocol.Extensions.Messages.PropertyBag.Property
  RealEstate.Domain.Property
  Name the one you mean in full.

$ any generate RealEstate.Domain.Property --entry-point any
  parcel      CadastralReference           new AnyCadastralReference()                         AnyX
  kind        PropertyKind                 Any.Enum<PropertyKind>()                            to verify, unread guards
  livingArea  Surface                      new AnySurface()                                    AnyX
  diagnosis   EnergyPerformanceDiagnosis   new AnyEnergyPerformanceDiagnosis()                 AnyX
  documents   IReadOnlyList<DocumentLink>  Any.ListOf(new AnyDocumentLink()).WithMaxCount(20)  to verify, guard, unread guards

$ any generate RentalListing Transfer --entry-point static:Arbitrary
  property        Property  new AnyProperty()         to verify, AnyX, unread guards
  monthlyRent     decimal   Any.Decimal().Positive()  to verify, guard, unread guards
  availableFrom   DateOnly  Any.DateOnly()            to verify, unread guards
  availableUntil  DateOnly  Any.DateOnly()            to verify, unread guards
  parcel        CadastralReference  new AnyCadastralReference()                              AnyX
  price         decimal             Any.Decimal().Positive().LessThanOrEqualTo(1000000000m)  guard
  signedOn      DateOnly            Any.DateOnly()                                           to verify, unread guards
  registeredOn  DateOnly            Any.DateOnly()                                           to verify, unread guards

$ any generate Transfer --entry-point static:Arbitrary          (file exists)  -> exit 1, "Re-run with --force"
$ any generate Transfer --entry-point static:Arbitrary --force  -> rewritten, exit 0
$ any generate EnergyPerformanceDiagnosis ... --force (after adding the LatestIssueDate guard) -> same recap, issuedOn still "to verify"
```

Judgement: every recap was honest. Every guard read was honoured (`ThrowIfNegative` → `>= 0`,
`ThrowIfNegativeOrZero` + `ThrowIfGreaterThan` → `Positive().LessThanOrEqualTo(…)`, `Count > 20` →
`WithMaxCount(20)`); everything else was flagged. No emitted file compiled before resolution, as
promised. A side probe confirmed no temporal guard is read at all, even
`if (issuedOn < new DateOnly(2021, 7, 1)) throw` or `ThrowIfGreaterThan(dt, new DateTime(2030,1,1))`.

## What I fixed by hand

* `CadastralReference`: base replaced by `Any.StringMatching(...)` with the plan number written as an
  alternation (the `(?!0000)` lookahead is refused). Return type changed `AnyString` → `AnyPattern`, so
  the scaffolded `WithValue(Func<AnyString, AnyString>)` had to be retyped by hand.
* `Surface`: no `WithScale` on `AnyDouble`, so the base became
  `Any.Decimal().Positive().LessThanOrEqualTo(...).WithScale(2)` converted with `.As(ToDouble)`; the refine
  overload retyped to `Func<AnyDecimal, AnyDecimal>`.
* `EnergyPerformanceDiagnosis`: rewritten. The rating/figures rule is cross-field; the rating is drawn
  first and the figures inside its bands in `Generate()`. The four figure `With…` overloads were deleted.
* `DocumentLink`: `UsingHttps()`; `.As(AsUtc)` for the Kind (later found unnecessary, see findings).
* `Property`: `Distinct(DocumentLocationComparer)` in a neighbouring file (the comparer).
* `RentalListing`: property refined to a diagnosis issued 2022–2023 and not F/G, `availableFrom` in
  2024–2031, `availableUntil` after 2031 — a declarative partition for the cross-field rules.
* `Transfer`: `Generate()` orders the two independently drawn dates (swap). Trade-off: pinning only
  `signedOn` can be silently swapped.

## Findings

1. **CLI, low** — the generated header says ``any generate Property --force` overwrites it`` but that
   command fails (`several types answer to that name`, exit 1); the full name is needed.
2. **CLI, low** — the ambiguity list includes types from transitive test-platform/telemetry packages
   (`Microsoft.ApplicationInsights…Property`, `PropertyBag.Property`), so a common domain noun fails.
3. **friction, medium** — no path for cross-field invariants in the scaffolded shape (one independent
   generator per parameter, no dependent/bind composition in the API). 3 of 7 generators needed a
   hand-written `Generate()` or a date partition.
4. **friction, low** — replacing a base with a builder of another type (`AnyPattern`, `.As(...)`) breaks
   the scaffolded refine overload's signature, to retype by hand.
5. **CLI, low** — the rating cross-field guard reads both ints, but only `declaredRating` is flagged; the
   two figure parameters are presented as clean `guard`.
6. **missing-capability, medium** — temporal guards (`DateOnly`/`DateTime` comparisons, even against a
   literal, and `ThrowIfGreaterThan` on dates) are never read.
7. **doc, low** — `Any.DateTime()` always draws `DateTimeKind.Utc`; the dates page does not say so.
8. **missing-capability, low** — no `WithScale` on `AnyDouble` (deliberate per the numbers page); the
   decimal + `.As` workaround is fine and was verified over 5 000 draws.
9. **analyzer, low** — Info-level rules (JD030, JD031, JD033) never show in a CLI `dotnet build`, even
   `-v:d`, though the docs say they speak "at the call site".
10. **analyzer, low** — JD028 stays silent on `Any.ListOf(Arbitrary.DocumentLink()).Distinct()` (a
    scaffolded generator always builds new instances); consistent with its "provably builds" scope.
11. **analyzer, low** — no build-time check for crossed constant `double` bounds
    (`GreaterThan(100d).LessThan(10d)`); JD023 is integer-only by design. Runtime message is clear.
12. **low** — the crossed-double message prints the next representable value:
    `Cannot apply LessThan(5.25) because GreaterThan(10.5) already requires values greater than or equal to 10.500000000000002.`
13. **doc, low** — the trial brief points at `PublicAPI.Shipped.txt`, which is empty; the surface is in
    `PublicAPI.Unshipped.txt`.

Refusals seen (all clear, all naming the construct): lookahead, `\p{Lu}`, `[A-Z-[IO]]`,
`After(DateOnly.MaxValue)`, count > 1 000 000, 8 distinct of 7 enum members, `Except(all members)`,
`WithPathSegments(-1)`, a host with a space. Seed replay: a failing `[Reproducible]` test printed
`[Reproducible(Seed = -1655208591)]`; pinning it reproduced `7118-06-10T18:59:47.0966483Z` exactly.

Analyzers fired: JD013, JD014 (int and double `Between`, `WithScale(29)`), JD015, JD016, JD017,
JD018 (my nested `Any.Reproducibly` inside `[assembly: Reproducible]` — correct), JD025, JD026. Not
fired: drawn value in assertion (no rule exists), JD028, crossed double bounds, constant lookahead.

## Fluency: 3 / 5

Scalar and value-object tests read very well: `Any.Property().WithKind(k => k.Except(Commercial))
.WithDiagnosis(d => d.WithDeclaredRating(EnergyRating.G))` says exactly what matters. The CLI is
honest and the refusals are excellent. What costs a point each: every non-trivial aggregate here had a
cross-field rule the scaffold cannot express, and swapping a base's builder type means retyping the
generated overloads by hand.

Tests: 50 (7 scaffold harness, 18 domain, 25 probes), all passing; no `Bug_` test (no high-severity
defect found).
