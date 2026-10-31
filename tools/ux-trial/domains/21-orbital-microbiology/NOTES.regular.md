# Orbital microbiology — notes from a regular developer

## What I did
- Created `Tests.Regular`, restored, then ran one command:
  `any generate Culture CultureId ExperimentalConditions Species GrowthRate Concentration Observation --entry-point static:Arbitrary`.
- Filled the 18 `TODO_verify` bases by hand (ranges from the guards, `Any.StringMatching` for the id and the binomial name,
  disjoint min/opt/max temperature bands so `min < opt < max` holds by construction, OD's 10 % floor as the uncertainty base,
  and the 10 mL centrifuge cap as the volume base).
- Wrote 36 test methods (42 cases) in four files plus a `Lab` helper of shared refinements: value objects, conditions
  (microgravity boosts of +20 % rate and +30 % capacity), observations and predictions, and the `Culture` aggregate (inoculation
  rules, lag, drift, growth in orbit against the centrifuge, stationary phase, die-off, competition, assimilation, prediction, termination).
- `[assembly: Reproducible]`. All 42 pass; I reran the suite 15 times with no failures.

## Frictions and surprises, in the order I met them
1. **The usual double guard is not read at all.** `if (!double.IsFinite(x) || x < 0 || x > Max) throw …` came back as
   `Any.Double()  to verify, unread guards` for every one of the 13 double parameters. That is the guard every numeric
   value object I write uses. So for this domain the scaffolder inferred nothing numeric: 18 of 22 parameters were
   `to verify`. The `ThrowIfOutside(v, -20, 80, …)` helper and `maxGrowthRate.PerHour <= 0` (on a composed parameter) were not read either.
   I would rather have a partly inferred `Between(0, Max)` plus the `to verify` mark than a bare `Any.Double()`.
2. **"First unread guard" hid the guard that mattered.** For `optimalTemperatureCelsius` the recap and the TODO quote
   `ThrowIfOutside(...)` (line 38) and never mention the cross-parameter `min < opt < max` check on line 40. If I had only read
   the quoted line, I would have written `Between(-20, 80)` three times and got flaky tests.
3. Switching to `Any.StringMatching` meant retyping the factory and the refining overload (`AnyString` -> `AnyPattern`).
   That is documented, and a script handled it, but by hand it is two edits per parameter.
4. `Arbitrary.Culture().WithConditions(Lab.FedConditions())` (passing a generator) gave
   `error CS1503: Argument 1: cannot convert from 'AnyExperimentalConditions' to 'ExperimentalConditions'`.
   The docs explain why (no `With…` takes a generator; a reusable refinement is a `Func<AnyX, AnyX>`). After I turned my helpers into
   `Func`s, `WithConditions(Lab.Fed)` reads well. But passing a generator was the first thing I tried, and the compiler error does not point you to the `Func` form.
5. Each `AnyValid…()` is private, so a test that calls a constructor directly and needs "any valid temperature" has nowhere
   to get it from. I drew a whole `ExperimentalConditions` and read `.TemperatureCelsius` from it, which is a small hack.
6. A cross-parameter rule shows up in tests: to pin a 5 % uncertainty on an observation I also had to keep the method away from
   OpticalDensity (`WithMethod(m => m.Except(ObservationMethod.OpticalDensity))`). This is honest and reads fine. You just have to remember it.
7. A "twin" of a drawn value with one parameter changed (same conditions at 0 g instead of 1 g) has no generator form, so I
   called the domain constructor with three properties from the drawn value. That is acceptable.

## What worked really well
- The TODO lines that stop the build, with the exact guard line quoted: nothing slipped through silently.
- Nested refinements read like the domain: `Lab.Mesophile().WithMaxGrowthRate(r => r.WithPerHour(p => p.LessThanOrEqualTo(4)))`
  and `WithCultureId(id => id.WithValue(v => v.DifferentFrom(culture.Id.Value)))`. Bounds added in a lambda combine
  with the base without any trouble.
- `Any.ListOf(Arbitrary.Species()).WithCount(5).Distinct()` respected `Species.Equals`, which gave me five distinct species in one line.
- `Any.DateTimeOffset()` defaults to `+00:00`, so the UTC guard was already honoured. `WithOffset(2h)` gave me the
  non-UTC case in one call.
- `static:Arbitrary` entry points, and the `[Reproducible]` seed line printed on a failing test (checked by breaking an assertion on purpose).
- The draws exposed one wrong dependency: the "+20 % in microgravity" ratio is only exact while μmax × 1.2 stays under the 5 h⁻¹ cap,
  so the test has to declare `≤ 4`. This is the "the test not caring is not the code not caring" point in practice.

## Fluency: 4 / 5
Once the bases were written, the tests were quick to write and short, and I trust them. I took a point off because the scaffold inferred
nothing from this domain's (very ordinary) numeric guards: about 20 minutes of filling in bases that the tool could have
pre-filled, plus one cross-parameter guard that only shows up if you open the constructor.
