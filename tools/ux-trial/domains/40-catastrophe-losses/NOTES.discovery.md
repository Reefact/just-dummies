# Discovery notes — 40 catastrophe losses

## What I did

- Read getting-started, then the `any` CLI page, numbers, collections, composition, the xUnit page.
- Created `Tests.Discovery`, restored, scaffolded 9 types in one call:
  `any generate GeoPoint Footprint VulnerabilityCurve InsuredProperty PolicyTerms Exposure CatastropheEvent Portfolio --output ./Arbitrary --entry-point any`
  then `CatastropheModel` on its own.
- Resolved every `TODO_verify_…` line, wrote 40 tests (hazard, vulnerability, terms/exposure,
  portfolio aggregate, loss distribution, Monte Carlo model), `[assembly: Reproducible]`.
- `dotnet test`: 40/40 green, rerun 15 times with fresh draws: 0 failures.

## Frictions and surprises, in the order met

1. **Every `double` guard was "unread".** `if (double.IsNaN(latitude) || latitude < -90.0 || latitude > 90.0)`
   (GeoPoint, Footprint radius/peak, event rate, model volatility: 7 parameters) all came back
   `Any.Double()` + `to verify`. The same tool read `insuredValue <= 0m` -> `Positive()` and
   `name.Length > 100` -> `WithMaxLength(100)` fine. A throwaway `--dry-run` showed the cause is the
   `||`: `if (x < -90) throw; if (x > 90) throw;` gives `Between(-90d, 90d)`, but
   `if (x < -90 || x > 90) throw` is unread for double, int and decimal alike. That one-line form is
   the most common range guard there is; I typed all seven recipes by hand.
2. The `to verify` mechanism itself worked as advertised: the build stopped at
   `TODO_verify_the_generator_for_latitude`, the comment quoted the guard and its line. No search needed.
3. The recap and the TODO comment truncate a long guard (`… severityVolatility > MaxSeverity...`):
   fine in the console, but in the file there is no width reason to cut it.
4. Regex ids (`EV-\d{4,8}`, `PRP-\d{6}`, `[A-Z]{3}`): documented as never read. The replacement
   `Any.StringMatching(...)` retypes two lines (factory and `Func<AnyString,AnyString>` overload) per
   parameter; the doc spells exactly that, so no surprise — but three files needed the same edit.
5. `VulnerabilityCurve`: intensities strictly increasing, ratios sorted and starting at 0, same count.
   The TODO only names the first unread guard (`Count != Count`); the ordering guards were found by
   reading the constructor. No "sorted" collection constraint exists, so I used
   `Any.SetOf(Any.Double().Between(0, 100)).WithCount(4).As(s => s.Order().ToArray())` and a fixed
   count for both lists, then retyped the factories `IAny<IReadOnlyList<double>>` and deleted the
   refining overloads, as the doc says to for an `.As(...)`.
6. `Exposure` cross-field rule (deductible < value, limit <= value): the "When a guard ties two
   parameters" section helped. I held it by construction: property value >= 100 000, deductible
   < 100 000, limit <= 100 000, via the lambdas `WithInsuredValue(v => v.GreaterThanOrEqualTo(...))`.
   Nice: the refining lambdas on a composed generator read very well.
7. Catalogue ids must be unique: `Distinct()` takes only an `IEqualityComparer<T>`; I wrote
   `EqualityComparer<CatastropheEvent>.Create(...)` by hand. A `DistinctBy(e => e.Id)` was what I looked for.
8. `WithCatalogue(c => ...)` cannot change the element recipe (documented); I pinned a one-event list.
9. Two independently drawn exposures may in principle share a property id (1 in 10^6), which the
   portfolio refuses. To get two *distinct* ids I wrote
   `Any.SetOf(Any.InsuredProperty().As(p => p.Id)).WithCount(2)`: works, but indirect, because the
   id recipe lives in a private `AnyValidId()` I cannot reach from a test.
10. Generated files land in namespace `CatastropheLosses.Domain` inside the test project (the
    documented default); slightly surprising, `--namespace` would fix it.
11. No test failed during the session, so I never saw the seed report in practice.
12. I never saw an analyzer message: Info-level diagnostics (JD030 is announced for
    `NotBlank()` strings) do not show in `dotnet build` output.

## What worked well

- One command scaffolded nine types, with AnyX composition (`new AnyGeoPoint()` inside `AnyFootprint`)
  and the `Any.Footprint()` entry point compiling on net10.0 with no setup.
- The pin / refine pair (`WithPeakIntensity(40.0)` vs `WithProperty(p => p.WithLocation(...))`)
  made tests read exactly as intended: load-bearing values literal, the rest drawn.
- Collection expressions as pins (`WithIntensities([0.0, 100.0])`) just work.
- Guard reading on strings/decimals (`NotBlank().WithMaxLength(100)`, `GreaterThanOrEqualTo(0m)`).
- The CLI page anticipated nearly every edit I had to make.

## Fluency: 4 / 5

Productive within the hour and the docs answered every question; one point off because every
`double` range guard and every uniqueness/ordering rule was manual work the tool could not help with,
and `Distinct` by key needs a hand-written comparer.
