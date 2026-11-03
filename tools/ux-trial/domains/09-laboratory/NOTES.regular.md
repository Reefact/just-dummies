# Regular notes — 09 Laboratory
Agent: the same model and effort as every session of this trial. Library: JustDummies 1.0.0-uxtrial.1 (packed from main).

## What I did
Pages read, in order: `PROJECT-README.md`, `guides/getting-started` and `guides/core-concepts` (skimmed), `packages/justdummies-cli` (the "what the last column means" table and the guard tables), `packages/justdummies-xunit`, `guides/composition`, then as a reference `generators/strings`, `numbers`, `dates-and-times`, `collections`.

Project: `Tests.Regular/Laboratory.Domain.Tests.Regular.csproj` from the template, plus `[assembly: Reproducible]` in `AssemblyInfo.cs`.

`any` commands run (from `Tests.Regular`, after a `dotnet restore`):
`any generate SampleId AnalyteCode MeasurementUnit DilutionFactor Measurement ReferenceRange SpecimenQuality Specimen LabResult LabReport --entry-point any`
(one call, ten types, entry points `Any.SampleId()` etc.). Result: MeasurementUnit, DilutionFactor, SpecimenQuality scaffolded fully inferred (`guard`, constants such as `MeasurementUnit.MaxLength` referenced by name); SampleId, AnalyteCode, Measurement, ReferenceRange, Specimen, LabResult, LabReport came out `to verify` and did not compile until completed.

Generators completed by hand:
- `AnySampleId`: `Any.String().StartingWith("S").Numeric().WithLength(11)`.
- `AnyAnalyteCode`: `Any.StringMatching(@"\d{1,5}-\d")`, with the four `AnyString` -> `AnyPattern` retypes the docs describe (done with a replace).
- `AnyMeasurement`: `Any.Double().GreaterThanOrEqualTo(0d)`.
- `AnyReferenceRange`: kept the scaffold's strict `OrderedPair` for lower/upper; added two `*Pinned` flags and draw the critical limits in `Generate()` with `_criticalLowRecipe.LessThanOrEqualTo(lower).OrNull()` / `GreaterThanOrEqualTo(upper)`.
- `AnySpecimen`: rewritten (about 170 lines). Collection instant in 2000-2100, a transit `TimeSpan` in [0, `Specimen.StabilityWindow`], reception = collection + transit; `WithReceivedAt` derives the collection backwards.
- `AnyLabResult`: rewritten (about 220 lines). Whichever of measurement/range is not pinned is drawn in the unit of the other; `reportedAt` is a drawn delay after the reception (or the reception is derived from a pinned `reportedAt`).
- `AnyLabReport`: rewritten (about 130 lines). Count, then `Any.SetOf(new AnyAnalyteCode()).WithCount(n)`, then one result per analyte, each on the report's sample id.

Tests (164 methods, 246 cases, 4 skipped as domain defects), one class per type: `SampleIdTests`, `AnalyteCodeTests`, `MeasurementUnitTests`, `DilutionFactorTests`, `MeasurementTests` (incl. `Undiluted`, overflow), `ReferenceRangeTests` (guards and their order, classification table, critical limits equal to bounds, NaN), `SpecimenQualityTests` (bounds, potassium rule table), `SpecimenTests` (72 h window to the tick, offsets, undefined enum, guard order), `LabResultTests` (construction guards and order, flags/phone call table, Release/Amend state machine, state unchanged after a refused amend), `LabReportTests` (guards, copy of the input, Abnormal order and live view after an amendment, TurnaroundTime with literals), `GeneratorTests` (the hand-written pieces of my generators).
Not covered: `SpecimenQuality` is not referenced by any other type (per MODEL), so it is tested alone.

Result: `dotnet test --project Tests.Regular` green at the first full run (242 passed, 4 skipped), then 37 further runs (12 + 25, fresh seeds each) all green. No test was ever red on a drawn value.

Skipped, recorded as domain defects: `AnalyteCode` accepts "2345-7\n" (regex `$`); `AnalyteCode` accepts non-ASCII digits (`\d`); `ReferenceRange` accepts a NaN critical limit; `LabReport.Results` hands out its private `List<>`.

## Frictions and surprises, in order
1. **`dotnet test --project Tests.Regular` does not work as given.** First `MSBUILD : error MSB1001: Unknown switch ... --project`, and with `dotnet test Tests.Regular`: `error : Testing with VSTest target is no longer supported by Microsoft.Testing.Platform on .NET 10 SDK and later. ... opt-in to the new dotnet test experience`. I expected the adapter page to cover it (it says "xunit.v3 4.0.2" but nothing about the runner). Fix: a `global.json` at the workspace root with `{ "test": { "runner": "Microsoft.Testing.Platform" } }`. Not a JustDummies defect (xunit.v3 + SDK 10), but the packages/xunit page is the natural place to mention it, and the workspace had no `global.json`.
2. **`any generate` on a project that was not restored**: `✗ SampleId: this project has not been restored, so its package references cannot be read. Restore it, then run the command again: dotnet restore`. Clear and actionable; I ran `dotnet restore` and repeated. (Cost: one round trip.)
3. **Rules that tie three or more parameters leave the real work to me, and the pair helper does not cover the common shapes.** For `Specimen` the tool scaffolded `Any.OrderedPair(Any.DateTimeOffset(), Any.DateTimeOffset(), strict: true)` for `collectedAt`/`receivedAt` (guard `receivedAt - collectedAt > StabilityWindow` marked "ties receivedAt to collectedAt"). Deleting the two TODO lines would have compiled, but that pair draws 0001..9999 instants: I measured 200/200 draws outside the 72 h window (throwing in the constructor, so loud, not silent). The same shape recurs three times here: a bounded difference (`Specimen`), a shared unit (`LabResult`), a shared key plus distinctness (`LabReport`), and two limits each tied to a different bound (`ReferenceRange`). Each needed a hand-written `Generate()`, about 520 lines, plus re-doing the `With...` surface. The docs ("A bound taken from another draw", `Any.OrderedPair`) explain the idea at a call site but give no recipe for "derive one parameter from another inside the scaffolded generator, and keep the pins working". I settled on: draw the base, draw an offset, derive; pins of the derived value work backwards.
4. **`value switch { NaN => throw, +-inf => throw, < 0 => throw }` in `Measurement` is not read**: `value  double  Any.Double()  to verify, unread guards`. Documented (switch expressions are not in the table); a plain `if` would have given `.GreaterThanOrEqualTo(0)` plus nothing for NaN. I wrote the `GreaterThanOrEqualTo(0d)` myself. Low cost.
5. **Regex guard is `to verify`, and replacing the base changes the generator's type.** `AnalyteCode` -> `Any.StringMatching(...)` returns `AnyPattern`; I retyped `AnyString` in four places (field, private ctor, `Func<>` of `WithValue`, the factory) with a global replace, exactly as the TODO comment says. Mechanical but easy to get half-done; the compiler error on the first miss is understandable.
6. **`void Validate(value)` helper (`SampleId`) marked `to verify` with the one-line cause shown** (`unread guard at SampleId.cs, line 7: Validate(value);`). Fine, as documented; just noting the "helper that returns void" form is common in value objects.
7. **Entry point files land in the domain's namespace and need C# 14**: no problem here (net10.0), `Any.SampleId()` read well. The scaffolded entry points for a type I then hand-rewrote (`Specimen`) kept working because the class name did not change.
8. **A one-sided `Any.Double().GreaterThanOrEqualTo(0d)` is a trap for arithmetic code I did not see coming until I wrote the `Undiluted` tests**: nine draws in ten are under 1e6, the tenth can sit near `double.MaxValue`, and `Undiluted` would then overflow (domain throws `ArgumentException`). It is documented and it is a fair thing for the draw to do, but it means every test of arithmetic on `Any.Measurement()` has to bound the value itself (`.WithValue(v => v.LessThanOrEqualTo(1_000_000d))`). I did, for the three non-overflow `Undiluted` tests, and kept an explicit literal test for the overflow.
9. **`Theory(Skip = ...)`**: fine, but the xUnit skip report prints each skipped case with its reason; there is no seed to quote when the test is literal-only, so I wrote "no seed: literal" in the reasons.
10. No analyzer diagnostic fired at all on this suite (build output had no warning, I did not turn info-level diagnostics on). Quiet is good, but I do not know whether `JD030` info diagnostics (unconstrained string lengths) were silent because I always constrained them.

## What worked well
- One `any generate` call with ten types, and the recap table (`guard`, `AnyX`, `to verify, unread guards`) was an accurate to-do list; each `to verify` line quoted the guard and its line, including "ties X to Y".
- Constants from the domain are referenced by name (`MeasurementUnit.MaxLength`), so `Any.String().NotBlank().WithMaxLength(MeasurementUnit.MaxLength)` followed the domain.
- `Any.OrderedPair(..., strict: true)` for `lower`/`upper` was exactly right, including `WithLower`/`WithUpper` pins that bound the other side.
- The DSL read well for "invalid" values: `Any.Decimal().LessThan(1m)`, `Any.Int32().GreaterThan(500)`, `Any.String().StartingWith("S").Numeric().WithLengthBetween(1, 10)` say what is wrong with the value and nothing else; the exact boundary cases stayed literals.
- `Any.SetOf(new AnyAnalyteCode()).WithCount(n)` gave distinct analytes by the type's own equality with no fuss; `DifferentFrom` on a scaffolded generator's recipe (`WithValue(v => v.DifferentFrom(...))`) made "a different id" one line.
- `[assembly: Reproducible]` removed all ceremony; 37 runs without a red case.
- Messages and diagnostics when I got it wrong were readable (the pair probe, the MTP error).

## Fluency: 4 / 5
The DSL and the xUnit adapter were fluent and the scaffold saved most of the work on single-parameter value objects. The point lost is for the aggregate types: every constructor with an invariant across parameters (window, shared unit, shared key, critical limits) ended as a hand-written `Generate()`, and the documentation shows the call-site idiom but not a pattern for doing it inside the scaffolded file; I also lost a few minutes to the `dotnet test --project` / Microsoft.Testing.Platform setup, which the adapter page does not mention.
