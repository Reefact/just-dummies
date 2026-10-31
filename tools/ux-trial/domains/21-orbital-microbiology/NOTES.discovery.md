# Discovery notes — 21 orbital microbiology

## What I did
- Read MODEL.md and the domain, then getting-started, composition, the CLI page, numbers,
  strings (patterns), dates-and-times, enums-and-choices and the xUnit adapter page.
- Created `Tests.Discovery`, restored, then ran in one go:
  `any generate CultureId GrowthRate Concentration Species ExperimentalConditions Culture Observation --entry-point any`.
- Filled every `AnyValid…()` factory, added `[assembly: Reproducible]`, wrote 36 test methods
  (40 cases with one 5-row theory) over growth rates, concentrations, ids, species, conditions,
  observations, predictions and the `Culture` aggregate (inoculation rules, lag, death above the
  maximum temperature, carrying capacity, uncertainty drift, inverse-variance assimilation,
  prediction without side effect, termination). All 40 pass; 25 consecutive runs stayed green.

## Frictions and surprises, in order
1. `any` read **none** of the numeric guards. Every `double` came out `Any.Double()` /
   `to verify, unread guards`, e.g. `perHour: unread guard at GrowthRate.cs, line 10:
   if (!double.IsFinite(perHour) || perHour < 0 || perHour > MaxPerHour)`. That is the idiomatic
   guard for a double; I expected `< 0 || > Max` to become `Between(0, MaxPerHour)`. 14 of 14 numeric
   parameters needed hand-written recipes. Safe (the build stops), but little was inferred.
   `ThrowIfOutside(...)` helpers (Species) are documented as unread — fine, expected.
2. Cross-parameter guards are only half surfaced. For `Species` the recap names line 37/38/39
   (`ThrowIfOutside` per temperature) but never line 40 (`min < opt < max`), because only the
   *first* unread guard per parameter is printed. Same for `ExperimentalConditions`: the
   centrifuge rule (gravity ≥ 0.5 g ⇒ volume ≤ 10 mL) is never named. I found both by reading the
   constructor. Observation's OD rule *was* named (line 39 on `method`) — good.
3. Cross-parameter rules: the CLI page's "When a guard ties two parameters" section helped. I kept
   the pins working by giving min/opt/max disjoint bands ([-20,10], (10,40), [40,80]) and capping
   volume at 10 mL, uncertainty at ≥ 10 %. Narrower than the domain, but valid by construction.
4. Replacing `Any.String().NonEmpty()` with `Any.StringMatching(...)` needs retyping the factory and
   the `Func<AnyString, AnyString>` overload. Documented exactly where I looked; painless.
5. I wrote a helper returning a prepared `AnyExperimentalConditions` and passed it to
   `Any.Culture().WithConditions(...)`. Error: `CS1503: Argument 1: cannot convert from
   'AnyExperimentalConditions' to 'ExperimentalConditions'`. The compiler message says nothing
   about why; the CLI page ("A refinement you want to reuse is a function") does. Switching to a
   `Func<AnyExperimentalConditions, AnyExperimentalConditions>` field fixed it and reads well.
6. Dependent dummy: an inoculum must be in (0, carrying capacity), which depends on the drawn
   conditions and species. Writing `c => c.GreaterThanOrEqualTo(1).LessThan(capacity)` inside the
   test worked, but it is a value computed from other draws; no doc page shows that pattern.
7. Not sure how small `Any.Double().Positive()` may draw (nutrient → capacity could fall below
   1 cell/mL and cross my inoculum bounds). numbers.en.md talks about the magnitude ceiling, not
   about how close to zero a positive draw can get. 25 runs never hit it; I left it.
8. Naming: the docs' `anyLongShipment` example helped me name chosen-but-drawn values
   (`anyMesophile`, `anySpecies10To40`), but I hesitated on the theory's species.

## What worked well
- The `TODO_verify_…` compile stopper with the exact file/line of the guard: impossible to ship a
  generator that silently draws invalid values.
- `--entry-point any`: `Any.Culture().WithConditions(c => c.WithTemperatureCelsius(50))` reads
  like the domain. Lambda refinement of a nested aggregate is the best part of the tool.
- `Any.DateTimeOffset()` stays at `+00:00` by default, and `.WithOffset(TimeSpan.Zero)` states it.
- `Enum.IsDefined` was understood (no mark on that guard).
- A deliberate failure printed
  `[JustDummies] These arbitrary values were seeded with -1770863349. Reproduce this run with [Reproducible(Seed = -1770863349)].`
  — exactly what to paste.
- No analyzer noise on ordinary code.

## Fluency: 4 / 5
The concepts and the generated shape are clear and the docs answered every question at the place
I looked. One point off because `any` inferred nothing for a domain made of finite-double guards,
and the cross-parameter rules had to be found by reading the constructors.
