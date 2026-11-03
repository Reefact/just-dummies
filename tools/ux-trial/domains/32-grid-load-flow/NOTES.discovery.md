# Discovery notes — 32 grid load flow
Agent: the same model and effort as every session of this trial. Library: JustDummies 1.0.0-uxtrial.1 (packed from main).

## What I did
Pages read, in order: `docs/PROJECT-README.md`, `guides/getting-started.en.md`, `packages/justdummies-cli.en.md`
(whole, including its "when a guard ties two parameters" section, which is where `DifferentFrom` / `OrderedPair` come from),
`guides/core-concepts.en.md` (first half; I did not open `composition.en.md`), `packages/justdummies-xunit.en.md` (first half). Also `MODEL.md` and the
domain sources (to understand the cross-field rules).

`any` commands run, from `Tests.Discovery`:
- `any generate BusId Impedance PowerInjection VoltageLimits SolverSettings Bus Line Transformer GridNetwork --entry-point any`
  (first attempt failed: project not restored; after `dotnet restore` it wrote 9 generators and 9 `.Entry.cs` files).

Generators completed by hand:
- `AnyBusId`: `Any.String()` replaced by `Any.StringMatching("[A-Z][A-Z0-9_-]{0,15}")`, retyped `AnyString` to `AnyPattern`.
- `AnyLine`, `AnyTransformer`: `to`/`lv` bus drawn with `.DifferentFrom(from)` inside `Generate()`.
- `AnyBus`: type, limits and generation drawn, then setpoint and generation derived in `Generate()` (PQ: no setpoint, zero
  active power; PV/Slack: setpoint drawn inside the drawn limits). The setpoint parameter and its `With...` were removed.
- `AnyGridNetwork`: rewritten by hand (spanning tree of lines at one base kV, bus 0 slack, distinct ids).
- `AnyImpedance`, `AnyPowerInjection`, `AnyVoltageLimits` (ordered pair), `AnySolverSettings` kept as scaffolded.
- `AssemblyInfo.cs` with `[assembly: Reproducible]`.
- `W/global.json` added to select the Microsoft.Testing.Platform runner (see friction 2).

Tests written (172 methods, 261 cases, 5 files): `ValueObjectTests.cs` (BusId, Impedance, PowerInjection,
VoltageLimits, SolverSettings: boundaries, refusals, computations), `EntityTests.cs` (Bus, Line, Transformer:
cross-field rules, Dispatch/ChangeLoad, Stamp, tap stops), `GridNetworkTests.cs` (construction refusals, lookups,
outages, SlackActiveDemandMw), `ResultTests.cs` (BranchFlow, LoadFlowResult), `SolverTests.cs` (KCL at a load bus,
conservation, PV holding |V|, tap direction, outages, divergence, violations, overloads). Electrical values stay
literals (they are what the assertions are about); ids, impedances, powers, names and whole buses/networks are drawn
only where the test does not depend on them.

Result: 261 cases, 258 passed, 3 skipped (2 domain defects, 1 test duplicating one of them), 0 failed. 40 further
runs (15 + 25, fresh seeds) all green.

## Frictions and surprises, in order
1. `any generate ...` straight after creating the csproj:
   `✗ BusId: this project has not been restored, so its package references cannot be read. Restore it, then run the command again: dotnet restore`.
   Clear message, one `dotnet restore`; the getting-started path never mentions it.
2. `dotnet test --project Tests.Discovery` (the command given in my brief) failed at first with
   `MSBUILD : error MSB1001: Unknown switch. ... --project`; the positional form `dotnet test Tests.Discovery` then gave
   `Testing with VSTest target is no longer supported by Microsoft.Testing.Platform on .NET 10 SDK and later ... opt-in to the new dotnet test experience`.
   Fix found by knowledge outside the docs: a `global.json` with `{"test":{"runner":"Microsoft.Testing.Platform"}}`. Neither the
   xUnit adapter page nor getting-started says that an xUnit v3 project on the .NET 10 SDK needs it.
3. Scaffolded `AnyBusId`: the regex guard is `to verify` and the file does not compile (`TODO_verify_the_generator_for_value`), as documented.
   The comment points at the regex; replacing with `Any.StringMatching(...)` needed the retype `AnyString` -> `AnyPattern` on four lines.
   The doc sample (Currency) shows exactly this, which made it painless, but it is a manual edit on the most common value-object shape.
4. `Bus` (8 parameters, 4 tied by cross-field rules) came out as "8 of 8 parameters inferred, 4 to verify" with three
   unrelated-looking guard messages per parameter ("ties type to voltageSetpointPu, and 2 more"). The docs list four forms
   to write such rules, but none fits "a parameter only exists for some values of another" (setpoint null for PQ, drawn inside the limits otherwise).
   I ended up deleting a parameter (setpoint) and its `With...` from the generated file and deriving it in `Generate()`.
5. `Line`/`Transformer`: `fromBus.Equals(toBus)` is reported as `to verify` ("ties fromBus to toBus"), not turned into `DifferentFrom`.
   The CLI page suggests `DifferentFrom` for equal parameters, so it was a 3-line fix, but the tool does not do it for a value-object `Equals`.
6. `GridNetwork` (collections of entities with unique ids, exactly one slack, connectivity): every one of the 5 parameters is
   marked `to verify` ("every parameter is marked here, since what decides this throw could not be traced back to one, and 5 more").
   The honest outcome: the scaffold was of no use and I rewrote the file. Not a bug, but the doc's promise "the file is yours" carried all the weight here.
7. `VoltageLimits`: minimum in [0.5, 1.0], maximum in [1.0, 1.5], `min >= max` throws. The tool paired them with a strict
   `Any.OrderedPair` and marked nothing, so min = max = 1.0 can never be drawn: good, and the recap showed `guard`. I only
   understood it by reading the generated file, because the recap does not say "paired".
8. Entry points: `--entry-point any` worked first time (C# 14 on the .NET 10 SDK); `Any.Bus()`, `Any.GridNetwork()` read nicely in the tests.
9. Documentation: the `getting-started` and the CLI page use `Any.OrderReference()` style but the CLI page is 900 lines; I read
   it end-to-end because it is where "complete the generator" lives. A "Your first scaffolded generator, end to end" section in getting-started would have saved a detour.
10. Real domain defects revealed by drawn values (not library defects):
    - `Solve` on a drawn valid network threw `ArgumentOutOfRangeException: Reactive power must be finite and within ±10000 Mvar. Actual value was 17599.82`
      from `Bus.NetInjection` (generation minus load can exceed the range of the `PowerInjection` it builds). Seed -2128493443.
      Kept as a skipped drawn test and a literal skipped test (`The_net_injection_of_a_valid_bus_is_always_available`).
    - Found by a literal: `new BusId("BUS\n")` is accepted since .NET's `$` matches before a final line feed, against MODEL.md's `^...$`.
      Skipped test `An_identifier_ending_with_a_line_feed_is_refused`.
11. The failure output printed the seed in the right place (`[JustDummies] These arbitrary values were seeded with -2128493443. Reproduce this run with [JustDummies.Xunit.Reproducible(Seed = -2128493443)]`);
    I replayed it with `Reproducible(Seed = ...)` and got the same exception message and value (17599.82...), as promised.

## What worked well
- The `anyXxxx` convention and the "is it a dummy?" question gave me a real decision rule when writing the tests: ids, names,
  impedances of a line under a topology test are drawn, voltages/powers in solver tests are literals.
- `any` output: recap table with the last column (`guard`, `AnyX`, `to verify`) is honest; constants (`Impedance.MaximumPu`, `Bus.MaximumBaseKv`) are referenced rather than copied.
- A build that refuses to compile until the `TODO_verify` lines are dealt with: it forced me to read each tied guard.
- Seed replay worked exactly and a drawn test found a genuine domain defect I had not thought of.
- Refinement lambdas (`WithType(t => t.Except(BusType.Slack))`) composed without surprises; no analyzer diagnostics were ever raised on my code (0 warnings).

## Fluency: 4 / 5
The generation of simple value objects and entities was smooth, and the docs answered almost every question when I looked in
the right page. Two things cost time: running the suite at all on the .NET 10 SDK (undocumented `global.json`), and aggregates
with cross-parameter rules (`Bus`, `GridNetwork`), where the tool correctly gives up and I wrote the generator by hand.
