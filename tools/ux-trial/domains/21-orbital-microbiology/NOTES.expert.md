# Notes: expert crafter, 21 orbital microbiology

## What I did
- Created `Tests.Expert` and scaffolded 7 generators in one run:
  `any generate Species ExperimentalConditions CultureId GrowthRate Concentration Culture Observation --entry-point any --output ./Arbitrary --namespace ...Arbitrary`.
- Resolved every `to verify` in the `AnyValid…()` factories (domain constants, not magic numbers), retyped
  `AnyCultureId`/`AnySpecies` to `AnyPattern` for `Any.StringMatching`, and wrote these partial files beside the scaffolded ones:
  `AnySpecies.Recipes.cs` (`WithCardinalTemperatures`, `WithoutLag`) and `AnyExperimentalConditions.Recipes.cs`
  (`InMicrogravity` widens the volume base through the private constructor; `AtTheOptimumOf(species)`, `Nourished()`).
- `Lab.cs`: `AnyInoculumFor(conditions, species)` (`.As` over a capacity fraction) and
  `AnyInoculatedCulture(species, configureCulture?, inoculum?)` (an `Any.Combine` of a culture and a fraction
  that inoculates it), returning an `InoculatedCulture` record.
- 39 test methods / 44 cases: value objects, species, conditions (microgravity boosts), observations and predictions,
  culture lifecycle, growth (lag, exponential, death, stationary, competition, uncertainty drift, predict), recording.
  `[assembly: Reproducible]`. All pass. I ran the suite about 30 times and it was green every time.

## Frictions and surprises, in the order I met them
1. **`any` reads no range guard written as a disjunction.** Every bound in this domain is `if (!double.IsFinite(x) || x < 0 || x > Max)`,
   so 15 of the 17 primitive parameters came out as `Any.Double()  to verify, unread guards`. A minimal repro with
   `--dry-run`: `if (x < 0 || x > 5) throw` gives `Any.Double()  to verify` (on `int` too), while the same rule split into two
   `if`s gives `Any.Double().Between(0d, 5d)  guard`. The idiomatic one-line range guard is the one it cannot read, and
   the CLI page does not say so: it shows only `quantity <= 0`.
2. `ThrowIfOutside(x, -20, 80, …)` (a private helper) is unread. That is documented and expected. Fine.
3. Cross-parameter rules (Species `min < opt < max`; Conditions `>= 0.5 g → <= 10 mL`; Observation `OpticalDensity → u >= 10 %`)
   were all flagged `to verify`, which is right. I solved each one by **narrowing a base** (disjoint ordered temperature bands; volume <= 10 mL;
   uncertainty >= 10 %) instead of rewriting `Generate()`, because a rewritten `Generate()` does not survive `--force` and breaks the pins.
   The cost is that the bases no longer cover the whole domain, and the refining lambda cannot widen them. Widening (`InMicrogravity`)
   meant writing against the private constructor. That works, but a test can no longer ask for "a precise plate count" by refinement
   (`u => u.LessThan(0.1)` conflicts with the base), only by a pin.
4. Retyping to `AnyPattern` meant two edits per parameter (the factory's return type and the `Func<…>`), exactly as documented.
5. Build with the SARIF log: `CA1822 note Member 'WithPerHour' does not access instance data and can be marked as static`
   on the scaffolded `With…` of every single-parameter generator (GrowthRate, Concentration, CultureId). This is noise in generated code.
6. `JD031 note These two bounds are the range Between(Observation.MinIndirectRelativeUncertainty, 1)`: a fair nudge, so I took it.
   Info-level JD diagnostics show up only in an IDE or a SARIF log, not in `dotnet build` output.
7. Microgravity rate test: I constrained the species to <= 4 h^-1 because the effective rate is clamped at 5 h^-1.
   To check, I removed that constraint. The drawn dummy then failed 1 run in 15 (`Expected 1.2, Actual 1.1621755638`) and printed
   `[JustDummies] … Reproduce this run with [Reproducible(Seed = -1228967182)]`. With that seed on the method (overriding the
   assembly-level attribute), the failure came back identically on three runs. This is the library doing exactly its job.

## What worked well
- The `TODO_verify_…` compile stop with the exact guard line quoted. I could not forget a parameter.
- `--entry-point any`: `Any.Species()`, `Any.Observation()` read like the built-ins. Multi-type scaffolding in one call,
  and composed types (`new AnyCultureId()` inside `AnyObservation`) were wired automatically.
- Refining lambdas compose deep and read like the domain:
  `Any.Observation().WithCultureId(id => id.WithValue(v => v.DifferentFrom(culture.Id.Value)))` and
  `.WithSpecies(s => s.WithBinomialName(n => n.DifferentFrom(...)))`.
- `Any.ListOf(Any.Species()).WithCount(5).Distinct()` gave distinct species by value equality (by name) with no ceremony.
- `Any.DateTimeOffset().WithOffset(TimeSpan.FromHours(2))` named the "not UTC" case precisely. The UTC default made the culture base trivial.
- `Any.StringMatching("CH(0[1-9]|1[0-2])-C[0-9]{4}")` expressed the CultureId format in one line.

## What I would design differently
- Read `a < lo || a > hi` (and a leading `!double.IsFinite(a) ||`) as `Between(lo, hi)`. Doubles in a domain are guarded this way.
- An opt-in "group" for a cross-parameter rule (e.g. a scaffolded tuple factory for `min/opt/max`), so that a narrowed base is not the only
  form that survives `--force`.
- Emit `static`-safe code or `[SuppressMessage("CA1822")]` in single-parameter generators.

## Fluency: 4 / 5
The DSL, the scaffolded refine/pin pairs, `[Reproducible]` and the seed replay are excellent and felt natural at once.
I took one point off because the scaffolder could not read this domain's ordinary one-line range guards, so most of the setup
time went into hand-writing bounds the constructors already state. Cross-parameter rules also force a choice between a narrowed base
and a hand-edited `Generate()`.
