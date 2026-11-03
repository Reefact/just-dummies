# Discovery notes — 21 Orbital microbiology
Agent: the same model and effort as every session of this trial. Library: JustDummies 1.0.0-uxtrial.1 (packed from main).

## What I did
Pages read, in order: `docs/PROJECT-README.md`, `docs/README.md`, `guides/getting-started.en.md`, `guides/core-concepts.en.md`,
`packages/justdummies-cli.en.md` (the whole page, it is long), `packages/justdummies-xunit.en.md`, then, when I met a need,
`guides/composition.en.md` (Combine, `OrderedPair`, `Then`, `Fixed`, `DifferentFrom`), the "Patterns" part of
`generators/strings.en.md`, and the numbers / dates pages by grep only (limits of one-sided bounds, the offset of `DateTimeOffset`).
I did not read the errors page or the analyzers pages: no diagnostic ever showed up.

`any` commands run (from `Tests.Discovery`, one batch):
`any generate CultureId GrowthRate Concentration Species ExperimentalConditions Observation EstimatedState Prediction MicrobialPopulation Culture --entry-point any --output Generators`
-> 10 generators + 10 `*.Entry.cs` (`Any.Species()` etc.). 5 files stopped the build on purpose (CultureId, Species, ExperimentalConditions,
Observation, Culture), the other 5 compiled as written.

Generators completed or changed by hand:
* `AnyCultureId`: the `to verify` regex guard became `Any.StringMatching("CH(0[1-9]|1[0-2])-C[0-9]{4}")`; retyped `AnyString` -> `AnyPattern` on the field, the factory, the constructor and the Func (a global replace in the file).
* `AnySpecies`: 7 `to verify` parameters, all bounds written from the constructor (`[A-Z][a-z]{3,9} [a-z]{3,10}`, a positive `AnyGrowthRate`, temperatures -20..80, lag 0..240, yield 1e9..1e13); `Generate()` rewritten so min < optimum < max holds by construction (`Any.OrderedPair(..., strict: true)` for the outer two, `GreaterThan(min).LessThan(max)` for the optimum); `AnySpecies.Custom.cs` adds `WithCardinalTemperatures(min, optimum, max)`; the three generated pin overloads for the temperatures were deleted (they would be ignored).
* `AnyExperimentalConditions`: the centrifuge rule (gravity >= 0.5 g => volume <= 10 mL) ties two parameters under a `&&`; `Generate()` redraws the pair (at most 1000 times) until legal. `AnyExperimentalConditions.Custom.cs` adds `Centrifuged()`.
* `AnyObservation`: same shape for `OpticalDensity => uncertainty >= 0.1`.
* `AnyCulture`: the start time is restricted to 2000..2100 UTC (the scaffold base `Any.DateTimeOffset()` spans the whole type; `CurrentTimeUtc` adds the elapsed hours).
* `Tests/Scenario.cs`: shared recipes (`Mesophile()` species pinned to 10/30/50 °C, 1 per hour, no lag; a favourable chamber; a dilute inoculum).

Tests written (195 methods, 334 cases, one file per type, Arrange / Act / Assert, `[assembly: Reproducible]`):
CultureId (8), GrowthRate (13), Concentration (14), Species (17), ExperimentalConditions (29), Observation (14), EstimatedState (9),
Prediction (15), MicrobialPopulation (12), Culture (64: construction, inoculation rules, interactions, advance, lag, stationary,
death and extinction, uncertainty drift, observation blending, estimate, prediction, generations, termination).
Rule I applied: what a test is about is a literal (bounds, temperatures, coefficients, the Euler step); what only has to exist is drawn
(ids, species names, the species of a prediction, the inoculum, the rest of the chamber).

Result: green. 3 final runs + 12 + 40 more runs with fresh seeds (55 runs in total): 334 cases, 0 failed, 2 skipped (domain defect, below).

## Frictions and surprises, in order
1. **`dotnet test --project Tests.Discovery` does not run on a bare project.** Exact output: `MSBUILD : error MSB1001: Unknown switch ... Switch: --project`, and plain `dotnet test` gives `error : Testing with VSTest target is no longer supported by Microsoft.Testing.Platform on .NET 10 SDK and later ... opt-in to the new dotnet test experience`. xUnit v3 is a Microsoft.Testing.Platform runner, so on the .NET 10 SDK a `global.json` containing `{ "test": { "runner": "Microsoft.Testing.Platform" } }` is required. The JustDummies pages never say it (the xunit page only talks of `xunit.v3` >= 4.0.2). I found the hint in the error text and created `W/global.json`.
2. **`Species` came out with seven `to verify`, all for one reason.** The constructor calls a `void` helper `ThrowIfOutside(x, -20, 80, nameof(x))`; the docs say a void helper is "marked as before", and the recap indeed said `first unread guard ... ThrowIfOutside(...); and 1 more`. The bounds were right there as constants, so I retyped them by hand seven times. Expected: a static throw helper taking a parameter and constants, whose body is a plain range check, read like `Guard`. Did: wrote them.
3. **A guard with `&&` (and a tie between two parameters) has no recipe in the docs.** `if (gravityG >= 0.5 && volumeMillilitres > 10) throw` and `if (method == OpticalDensity && relativeUncertainty < 0.1) throw` were correctly flagged `to verify ... ties gravityG to volumeMillilitres`, but "When a guard ties two parameters" offers: start+duration, derive one inside `Generate()`, check digit, `DifferentFrom`. None fits a conditional rule. I wrote a bounded redraw loop in `Generate()`. It works (acceptance is 40 % or more), it honours pins, and contradictory pins throw my own exception, but it is exactly the "draw and retry" the library says it avoids, and I had to invent the shape. Also: three values ordered (min < optimum < max) are described in the composition guide only (found there by searching for "OrderedPair"), not in the CLI page where I was looking.
4. **A field added in the `partial` class is silently dropped by the next `With…` call.** I stored the pinned temperatures in a field of `AnySpecies.Custom.cs`; `Scenario.Mesophile()` did `.WithCardinalTemperatures(10, 30, 50).WithMaxGrowthRate(...)`. The generated copy constructor `AnySpecies(AnySpecies from)` knows nothing about my field, so the species silently reverted to random temperatures. Symptoms: `A_species_does_not_grow_outside_its_cardinal_temperatures(temperature: 5)` failed with `Expected: 0, Actual: 0.89042248934881263` and 4 others. The page "Adding the overload yourself" says to change one field of the copy, which holds for fields the file already has, and never warns that a new field is lost. I rebuilt `WithCardinalTemperatures` on the three recipe fields the generated copy constructor already carries (`Any.Double().Between(x, x)` works as a pin).
5. **The scaffold's one-sided bounds draw astronomically large hours.** `elapsedHours`, `madeAtHours`, `ageHours` were scaffolded as `Any.Double().GreaterThanOrEqualTo(0d)`. I measured 21 draws out of 200 above 1e6 for `Any.Prediction().MadeAtHours` (the docs say a one-sided bound reaches the type's limit one draw in ten). It is documented, and none of my tests broke, but a duration in hours of 1e300 is not a plausible station value and the recap shows it as a plain `guard`, i.e. as inferred and finished. I left it.
6. **Everything that is not a plain range needs a retype of `AnyString` to `AnyPattern`** (CultureId, binomial name): documented and clear, but four places and a global replace; it was the only edit where I made the file stop compiling by hand.
7. **A silent trap of the `Generate()` rewrite.** The docs warn that a derived parameter ignores its pin; I did it for the temperatures (deleted the pins) and for the centrifuge pair/optical density (kept, the redraw loop respects them). It worked, but nothing tells you which overloads of the file are now dead; the compiler is silent.
8. **`$` in the domain's own regexes** (domain defect, see below) was found by an ordinary literal test, not by a draw: `"CH03-C0042\n"` is accepted.

## What worked well
* The first scaffold on 10 types was one command; 5 of 10 compiled immediately and the recap column (`guard`, `AnyX`, `to verify`) was truthful: every non-trivial rule was either read (bounds via the domain's public constants such as `GrowthRate.MaxPerHour`, `Species.MaxLagHours`) or marked, and I never found a generator that drew an illegal value silently.
* Composition through `AnyX` worked: `Observation`, `Culture`, `Prediction` reused the generators of `Species`, `CultureId`, `Concentration` with no extra work, and a refinement such as `Any.Species().WithBinomialName(n => n.DifferentFrom(other.BinomialName))` or `Any.GrowthRate().WithPerHour(r => r.DifferentFrom(x))` reads exactly like the intent.
* `[assembly: Reproducible]` plus the printed seed: every failure during development carried a replay line.
* The idea of the docs ("what the test is about stays a literal") was easy to apply: the temperature factor, Euler steps, tolerances, bounds are literals; ids, names, the second chamber parameters are dummies. Nothing flaked in 55 runs.
* `Any.OrderedPair(..., strict: true)` on doubles and the temperature recipe via `Between(x, x)` did what the composition page said.
* No analyzer diagnostic at any point: zero warnings in the build, no noise.

## Fluency: 4 / 5
The getting-started path and the CLI page were enough to get a green 334-case suite in one sitting, and the scaffolder's honesty (a non-compiling file instead of a guess) saved me from drawing illegal species. Points lost for the test-runner opt-in nobody mentions, for the conditional-guard cases where I had to design the generator without a documented pattern, and for the copy-constructor trap, which cost a debugging round before I understood why the pinned temperatures vanished.

## Domain observations (not library)
* `CultureId` and `Species` use `^...$`: `$` also matches before a final `\n`, so `"CH03-C0042\n"` and `"Bacillus subtilis\n"` are accepted and stored with the newline. Tests kept and skipped: `CultureIdTests.A_text_with_a_trailing_newline_is_refused`, `SpeciesTests.A_name_with_a_trailing_newline_is_refused` (literal inputs, hence no seed). `\z` or `RegexOptions` would fix it.
* `Culture.GenerationsOf(species)` throws `InvalidOperationException` for an extinct species (its concentration is 0): consistent with `Concentration.GenerationsTo` but a survivable reading ("all lost") has no value; tested as is.
* `Culture.CurrentTimeUtc` is `StartedAtUtc.AddHours(ElapsedHours)`: a start near `DateTimeOffset.MaxValue` would throw on the property, and `Culture` does not refuse such a start; not reproduced from a draw (the unconstrained generator did not reach it in 500 draws), so I only bounded the start to 2000..2100.
