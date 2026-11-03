# Discovery notes — 04 Manufacturing
Agent: the same model and effort as every session of this trial. Library: JustDummies 1.0.0-uxtrial.1 (packed from main).

## What I did
Pages read, in order: `docs/PROJECT-README.md`, `guides/getting-started.en.md`, `packages/justdummies-cli.en.md` (the whole page, it is long),
`packages/justdummies-xunit.en.md` (first half), `generators/README.md`, `generators/strings.en.md` (Patterns section), `generators/dates-and-times.en.md`,
`generators/numbers.en.md` (first half), `guides/composition.en.md` (first half). I did not open core-concepts, errors-and-conflicts, reproducibility or the analyzer pages.

Project: `Tests.Discovery/Manufacturing.Tests.Discovery.csproj` from the template. `dotnet restore`, then:
`any generate BatchCode Cadence CycleTime ShiftWindow Tolerance Machine WorkOrder --output Arbitrary --namespace Manufacturing.Tests.Discovery.Arbitrary --entry-point any`
(first try without a prior restore was refused: "this project has not been restored").
Scaffold result: Cadence and WorkOrder were complete (WorkOrder draws its parts through `new AnyBatchCode()` etc.); BatchCode, CycleTime, ShiftWindow, Tolerance and Machine
were `to verify` and did not compile until completed.

Generators completed by hand:
- `AnyBatchCode`: regex guard -> `Any.StringMatching(@"[A-Z]{3}-\d{2}(0[1-9]|[1-4]\d|5[0-3])-\d{4}")`, retyped `AnyString` to `AnyPattern` (textual replace, as the TODO comment says).
- `AnyMachine`: id -> `Any.StringMatching(@"M-\d{3}")` (retype), state -> `Any.Enum<MachineState>().OneOf(Idle, Stopped, Maintenance)`.
- `AnyCycleTime`: `Any.TimeSpan().Between(1 s, 1 h).WithGranularity(1 s)`.
- `AnyTolerance`: three independent recipes (nominal 1..5000, minus 0..0.999, plus 0.001..100, all `WithScale(3)`) chosen so the cross-parameter rules hold by construction. This narrows the real domain (a nominal under 1 mm is never drawn).
- `AnyShiftWindow`: the scaffold (an `OrderedPair`) covered `start < end` but not the 12 h cap; I rewrote the whole class as "a start and a length", losing `WithEnd`.

Tests written (91 methods, 233 cases): `BatchCodeTests` (10), `CadenceTests` (10), `CycleTimeTests` (10), `ShiftWindowTests` (7), `ToleranceTests` (14), `MachineTests` (14, including the whole
6x6 state/event matrix: 14 allowed transitions and 22 refused ones, the latter checking the state is unchanged), `WorkOrderTests` (26: guards and their order, copy of the operators,
duration/fits-in-window boundaries, good/scrap/limits, refusal for each non-running state, completion, yield rounding including the 6.25 -> 6.3 midpoint cases).
`[assembly: Reproducible]` in `AssemblyInfo.cs`; no per-test attribute.

Final result: green, 233 cases, 228 passed, 5 skipped (domain defects, below). 12 further runs with `--no-build`, fresh seeds each time: 12 green.

## Frictions and surprises, in order
1. `dotnet test --project Tests.Discovery` (the command given in the brief; the docs never mention how to run tests) fails on this SDK (10.0.114):
   `MSBUILD : error MSB1001: Unknown switch. ... Switch: --project`. I had to add a `global.json` at the workspace root with
   `{ "test": { "runner": "Microsoft.Testing.Platform" } }` before it worked. Not a JustDummies matter, but xunit.v3 + the adapter page say nothing about it; a newcomer would hit it.
2. `any generate ...` before restoring: `✗ BatchCode: this project has not been restored, so its package references cannot be read. Restore it, then run the command again: dotnet restore`.
   Clear message, one wasted run; the docs' "Run it from your test project" does not say restore first.
3. The CLI page is 53 KB and I read all of it to learn what `to verify` and `TODO(any)` mean. The getting-started guide only points to it with one row of a table; the useful, short
   path ("run `any`, fix what does not compile, delete the two TODO lines") only becomes visible after reading the section "What the last column means".
4. `Validate(nominalMm, minusMm, plusMm);` in `Tolerance` is a `void` helper, so all three parameters came back `Any.Decimal()` with `to verify, unread guards`. The comment only quotes
   the call site (`Tolerance.cs, line 7`), not the guards inside `Validate`; I had to read the domain to know what to write. The docs do say a `void` helper is "marked as before".
   Expected, but the scaffold is of no help for the most common value-object style (guards in a private `Validate`). The generated `Any.Decimal()` also draws up to decimal extremes on one draw in ten, so leaving it would have failed immediately.
5. `ShiftWindow`: the tool recognised `start >= end` and generated an `OrderedPair`, but flagged both parameters `to verify` because of `end - start > LongestShift` ("ties end to start").
   There is no documented way to add a bound to an `OrderedPair` (a maximum gap). The docs' "start and a duration" recipe works but means throwing the scaffolded class away: I lost `WithEnd(...)`
   and the pins. A scaffold that gave `WithStart/WithLength` for this shape would have been welcome.
6. Tolerance: `minus < nominal` ties parameters in a way the docs list as "a rule between two parameters that is not an order"; I solved it by narrowing the ranges (nominal >= 1), which means the generator
   can never draw a sub-millimetre tolerance. The documented alternative (`.Then(...)`) would work but needs the whole `Generate()` rewritten; I chose the narrower and wrote that in the notes. The tests that care use literals.
7. `Any.WorkOrder().WithOperators(null!)` -> `CS0121: The call is ambiguous between ... WithOperators(IReadOnlyList<string>) and ... WithOperators(Func<AnyList<string>, AnyList<string>>)`.
   The docs do cover this ("the cast telling the value apart from a lambda") but only for a *nullable* parameter; here the parameter is non-nullable and I wanted to pass null to test the guard. I found the fix from the error,
   `(IReadOnlyList<string>)null!`.
8. Naming collision note at scaffold time: `Machine resolved to Manufacturing.Domain.Machine; System.Reflection.PortableExecutable.Machine also answers to that name.` Informational, fine, and it resolved correctly.
9. An observation on the entry points: with `--entry-point any` every type gets two files (`AnyX.cs`, `AnyX.Entry.cs`); 14 files for 7 types. Fine, but a newcomer might wonder why the entry point is not in the generator file.
10. No analyzer diagnostic ever appeared in `dotnet build` (no warnings at all), including for `Any.String()` rows I did not touch. The docs say Info never prints, so I could not tell whether the analyzers were active. I never saw one fire.
11. Dummies versus properties. I caught myself twice writing tests where the drawn value ended in the assertion (a round-trip `Assert.Equal(anyDuration, cycle.Duration)`, a `A_drawn_window_is_valid` check on a generator's output); the getting-started guide
    "Careful not to misuse a dummy" is what made me notice, and I replaced them with literals. For value objects whose whole contract is the number (Cadence, CycleTime, Tolerance, BatchCode) most tests ended up with literals and the dummy
    is only a background ingredient (the machine id, the batch, the window of a work order). I think that is what the library intends, but it means those five test classes use `Any` very little.

Domain defects found by the tests (kept, marked `Skip = "domain defect: ..."`; the inputs are literals, so no seed applies):
- `BatchCode` and `Machine`: the regex ends with `$`, which .NET matches before a final `\n`; `"LYN-2614-0042\n"` and `"M-123\n"` are accepted (the batch keeps the newline in `Value`). `BatchCodeTests.A_code_ending_with_a_line_break_is_refused`, `MachineTests.An_id_ending_with_a_line_break_is_refused`.
- `WorkOrder` constructor with a null `operators` list and `WorkOrder.RecordPart(null, ...)` throw `NullReferenceException`; every other argument of the same constructor throws `ArgumentNullException` (MODEL.md states this on purpose).
- `Cadence.TimeToProduce` with a quantity above about 256 million at 1 unit/hour throws `ArgumentOutOfRangeException` from `TimeSpan.FromMinutes`, without naming the parameter; the contract only says quantity >= 0. Unreachable through `WorkOrder` (capped at 100 000).

## What worked well
- The entry-point + `With...` pin/lambda API read very well in tests: `Any.WorkOrder().WithCadence(new Cadence(100)).WithPlannedQuantity(800)...` says exactly what the test cares about, everything else being arbitrary.
- The `to verify` mechanism did its job: the build stopped on every generator that could have drawn invalid values, at the exact line, with the guard quoted. The regex-to-`AnyPattern` retype instructions in the TODO comment were accurate on the first try.
- `WorkOrder`, whose parameters are all domain types, was scaffolded complete from the generators of the others.
- `[assembly: Reproducible]` and `Assert.Throws` over `With...` pins made guard tests very short. The seed is printed with a `Reproducible(Seed = ...)` snippet for every failure, and the 5 first failures all had one.
- The first-time experience: from csproj to a compiling, green suite in about a dozen tool calls.

## Fluency: 4 / 5
The library did what the docs said and the scaffold plus the documented recipes got me to a green 233-case suite quickly, with no surprise at run time and no flaky draw over 13 runs. Points lost for the length of the one page I
needed (the CLI page), for the scaffold being little help on the two most typical value-object shapes (a private `Validate` helper, and a gap bound between two dates), and for the test runner invocation not being explained.
