# 08 - Patient record / prescription

## Domain model (`Domain/`, namespace `Clinical.Domain`)

| Type | Guards (shape) | Behaviour |
| --- | --- | --- |
| `Nir(string)` | null check; `Regex.IsMatch(value, constPattern)` (sex, year, month 01-12, department incl. 2A/2B); control key = 97 - body mod 97 (2A reads 19, 2B reads 18) | `Sex`, `BirthYearTwoDigits`, `BirthMonth`, `ComputeKey` |
| `Molecule(string)` | `ThrowIfNullOrWhiteSpace`; `Length > 80`; static `Regex` field `^[a-z][a-z -]*$` | - |
| `Dosage(decimal, DoseUnit, int)` | `ThrowIfNegativeOrZero`, `ThrowIfGreaterThan(amount, 10_000m)`, `decimal.Round(amount, 3) != amount`, `Enum.IsDefined`, relational pattern `timesPerDay is < 1 or > 6` | `DailyAmount` |
| `TreatmentDuration(int)` | delegated to a private `Validate` (1..365) | `LastDayFrom(start)` |
| `PrescriptionLine(Molecule, Dosage, Route, TreatmentDuration)` | `ThrowIfNull` x3; cross-field tuple switch: a `Tablet` only by mouth/sublingual, a `Puff` only inhaled | `TotalQuantity` |
| `Patient(Nir, string, string, DateOnly)` | `ThrowIfNull`, `ThrowIfNullOrWhiteSpace` x2, birth date >= 1900-01-01 (static readonly field), cross-field: birth year/month must match the NIR digits | `AgeOn`, `IsMinorOn` |
| `Prescription(Guid, Patient, string, DateOnly, IReadOnlyList<PrescriptionLine>)` | `id == Guid.Empty`, RPPS static regex `^\d{11}$`, cross-field `issuedOn >= patient.BirthDate`, `Count == 0 \|\| Count > 10` | `Sign`, `Dispense(on)` (3-month window), `Cancel`, `LastDispensingDay`, `TreatmentEndsOn`, `RequiresParentalConsent` |
| enums | `RouteOfAdministration` (8), `DoseUnit`, `PrescriptionStatus`, `Sex` | |

## CLI recaps (verbatim, `any generate Nir Molecule Dosage TreatmentDuration PrescriptionLine Patient Prescription`)

```
Analyzing Clinical.Domain.Nir
  value  string  Any.String().NonEmpty()  to verify, unread guards
Analyzing Clinical.Domain.Molecule
  name  string  Any.String().NotBlank().WithMaxLength(80)  to verify, guard, unread guards
Analyzing Clinical.Domain.Dosage
  amount       decimal   Any.Decimal().Positive().LessThanOrEqualTo(10000m)  to verify, guard, unread guards
  unit         DoseUnit  Any.Enum<DoseUnit>()
  timesPerDay  int       Any.Int32()                                         to verify, unread guards
Analyzing Clinical.Domain.TreatmentDuration
  days  int  Any.Int32()  to verify, unread guards
Analyzing Clinical.Domain.PrescriptionLine
  molecule  Molecule               new AnyMolecule()                  AnyX
  dosage    Dosage                 new AnyDosage()                    to verify, AnyX, unread guards
  route     RouteOfAdministration  Any.Enum<RouteOfAdministration>()  to verify, unread guards
  duration  TreatmentDuration      new AnyTreatmentDuration()         AnyX
Analyzing Clinical.Domain.Patient
  nir        Nir       new AnyNir()             to verify, AnyX, unread guards
  lastName   string    Any.String().NotBlank()  guard
  firstName  string    Any.String().NotBlank()  guard
  birthDate  DateOnly  Any.DateOnly()           to verify, unread guards
Analyzing Clinical.Domain.Prescription
  id              Guid                             Any.Guid().NonEmpty()                  guard
  patient         Patient                          new AnyPatient()                       to verify, AnyX, unread guards
  prescriberRpps  string                           Any.String().NonEmpty()                to verify, unread guards
  issuedOn        DateOnly                         Any.DateOnly()                         to verify, unread guards
  lines           IReadOnlyList<PrescriptionLine>  Any.ListOf(new AnyPrescriptionLine())  to verify, AnyX, unread guards
```

Every summary line read `✓ AnyX.cs — n of n parameters inferred, k to verify.` plus "The file will not
compile until you resolve it. That is deliberate." Also run: `--dry-run`, `--format json --dry-run`,
`--entry-point any` (kept), `--entry-point static:Arbitrary --dry-run`, `static:Any` (refused, exit 2,
clear), a re-run without `--force` (exit 1, clear), `--force`, a typo'd type name (exit 1, clear).

**Verdict: the recap was honest.** Every guard the tool could not vouch for was flagged; nothing
unflagged ever threw (500-draw harness, `ScaffoldedGeneratorTests`). The emitted files compiled except
for the 13 deliberate `TODO_verify_*` lines. `Enum.IsDefined(unit)` was correctly treated as already
satisfied by `Any.Enum<T>()`. Unread (flagged) shapes: `Regex.IsMatch(value, constPattern)`, a static
`Regex` field, `decimal.Round(x, 3) != x`, `is < 1 or > 6`, a private `Validate`, a static readonly
bound, the two cross-field guards, and `Count == 0 || Count > Max` (K2).

Reconfirmed (already reported by 01-payments): `any generate RouteOfAdministration` emits
`return new RouteOfAdministration();` with "no constructor parameters to infer".

## What I fixed by hand

* `AnyNir`: base is `StringMatching(body)` + a computed control key via `.As(...)`. The base changed
  from `AnyString` to `IAny<string>`, so I split it into `AnyValidBody()` (`AnyPattern`) and
  `WithControlKey(...)` and retyped `WithValue(Func<AnyString, AnyString>)` to `AnyPattern`.
  `AnyNir.Custom.cs` adds `BornIn(year, month)`.
* `AnyPatient`: the NIR must agree with the birth date. Parameters are drawn independently, so I
  rewrote `Generate()`: draw the birth date first, then `_nir is AnyNir n ? n.BornIn(...)` (a pinned NIR
  is left as is). Birth date base `Between(1900-01-01, 2025-12-31)`.
* `AnyPrescription`: `issuedOn` base `Between(2026-01-01, 2030-12-31)` so it never predates a patient
  (born by 2025); RPPS `Numeric().WithLength(11)`; lines `WithCountBetween(1, Prescription.MaxLines)`.
* `AnyPrescriptionLine`: dosage base excludes `Tablet` and `Puff` (route depends on unit).
* `AnyMolecule`: `StringMatching("[a-z][a-z -]{0,79}")` (retyped to `AnyPattern` again).
* `AnyDosage`: `.WithScale(3)`, `timesPerDay Between(1, 6)`; `AnyTreatmentDuration`: `Between(1, MaxDays)`.

## Findings

1. **doc / medium** - `numbers.en.md` says `WithScale` "fixes the number of decimal places"; the
   IntelliSense doc says "a value lattice, not a representation contract ... not padded with trailing
   zeros". Observed: `Between(0,10).WithScale(2)` -> `Scale == 2` on 2000/2000 draws, but
   `Between(0,1).WithScale(28)` -> `Scale` 25..28, and `Between(1e27, 7e28).WithScale(2)` -> `Scale` 0/1.
   The values are on the grid, so per the XML doc this is by design; the user page is what misleads.
   (`Probes.WithScale_28_is_a_lattice_not_a_representation`.)
2. **friction / medium** - checksum formats (NIR key) and cross-parameter rules (NIR <-> birth date,
   unit <-> route, issue date <-> patient) do not fit the scaffold's one-typed-base-per-parameter shape:
   the base had to change type (losing/retyping the lambda overload), and `Generate()` had to be
   rewritten with a runtime type test on a field.
3. **analyzer / low** - JD023's message "No value satisfies this chain once GreaterThan is applied"
   names one side; the run time names both ("... because Between(1, 6) already requires ...").
4. **analyzer / low** - nothing at build time for constant crossed `DateOnly` bounds, a constant
   decimal range with no point at the scale, a constant unsupported regex (`(?!00)`), or a reversed
   constant `TimeOnly.Between` (`AnalyzerProbeChains.cs`).
5. **cli / low** - `decimal.Round(amount, 3) != amount` is not mapped to `.WithScale(3)` (flagged, safe).
6. **doc / low** - errors page: `AnyGenerationException.Seed` is set "when the draw happened inside a
   reproducible scope"; observed set (and the message offers `Any.Reproducibly(seed, ...)`) outside
   any scope too, and that seed did replay the test's earlier draws. Pleasant, but undocumented.

## Analyzer diagnostics seen

JD023, JD016, JD014 (on `AnalyzerProbeChains.cs`, correct), JD006 (discarded `GreaterThan`, correct),
JD018 (an `Any.Reproducibly` inside a `[Reproducible]` class, correct and useful; I moved the seed
probes to a class without the attribute). Silent inside `Assert.Throws` lambdas (documented).
Silent: JD028 on `Any.ListOf(Any.PrescriptionLine()).Distinct()` (class without equality; documented
false negative), nothing for a drawn value reaching `Assert.Equal`.

## Edges that behaved well

Contradictions name both sides (int, DateOnly, decimal scale, refinement vs scaffolded base, enum
`Except` all, prefix vs length); `DateOnly`/`TimeOnly` at Min/MaxValue; `Between(d, d)`; regex refusals
name construct and position (lookahead, backreference, `\b`, class subtraction, `\p{..}`);
`IgnorePatternWhitespace` refused rather than misread; `\d` only ASCII; pool inspection on a molecule
formulary pinpoints `Amoxicillin`; `[Reproducible]` failure output and `Seed =` replay reproduced the
exact NIR `247102A21295517`.

## Tests

55 tests, 55 passing: 7 harness, 18 domain, 30 probes. No `Bug_` test: the one I wrote first
(scale 28) turned out to be documented behaviour in the XML doc, see finding 1.

## Fluency: 4 / 5

Writing tests read naturally: `Any.Prescription().WithIssuedOn(...).WithLines(l => l.ContainingAny(
Any.PrescriptionLine().WithRoute(RouteOfAdministration.Intravenous)))` says exactly what matters, and
error messages are excellent. One point off for the scaffold's resistance to derived formats and
cross-parameter invariants, which is where medical identifiers live, and the WithScale doc mismatch.
