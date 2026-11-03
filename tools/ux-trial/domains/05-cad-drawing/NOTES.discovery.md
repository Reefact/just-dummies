# Discovery notes — 05 CAD drawing
Agent: the same model and effort as every session of this trial. Library: JustDummies 1.0.0-uxtrial.1 (packed from main).

## What I did
Pages read, in order: `docs/PROJECT-README.md`, `docs/README.md` (index), `guides/getting-started.en.md`,
`guides/core-concepts.en.md`, `packages/justdummies-cli.en.md` (all of it, it is long), the first half of
`packages/justdummies-xunit.en.md`, `generators/README.md`, `generators/strings.en.md` (patterns), then only the
`DateOnly` and enum parts of `dates-and-times` / `enums-and-choices`, and two sections of `numbers.en.md` when a
draw surprised me. I did not read composition, errors-and-conflicts, reproducibility or the analyzer pages.

Project: `Drafting.Tests.Discovery.csproj` from the template, plus a `global.json` at W (see friction 1).

`any` commands run, from `Tests.Discovery/`:
- `any --help`, `any generate --help`
- `any generate PartNumber DrawingReference DrawingScale IsoTolerance Dimension Fit Drawing BomItem --output Generators --entry-point any`
  (one run, eight types, exit 0; 8 generators + 8 `*.Entry.cs`, so `Any.PartNumber()`, `Any.Drawing()`...)

Generators completed by hand (the build stopped on the `TODO_verify_…` sentinels, as documented):
- `AnyPartNumber`: `Any.StringMatching("P[0-9]{6}(-[A-Z0-9]{2})?")`, retyped `AnyString` -> `AnyPattern` on the 3 lines the doc lists.
- `AnyDrawingReference`: number `Any.StringMatching("(MEC|ELE|HYD)-[0-9]{5}")` (same retype); revision `Any.Char().OneOf('A',...,'Y' minus I,O,Q,S,X)`.
- `AnyDrawingScale`: tied parameters (one side must be 1), so `Generate()` rewritten by hand (coin flip enlargement/reduction).
- `AnyIsoTolerance`: `Any.String().OneOf("H","JS","h","js")`; plus my own `AnyIsoTolerance.Custom.cs` with `Hole()` / `Shaft()` refinements (`WithZone(_ => Any.String().OneOf(...))`).
- `AnyDimension`: edited `AnyValidNominal` to `Positive().LessThanOrEqualTo(500 / 25.4)` (the tool had not flagged the 500 mm rule, friction 3).
- `AnyFit`: hole = `new AnyDimension().WithTolerance(t => t.Hole())`, shaft likewise; `Generate()` rewritten so the shaft takes the hole's size.
- `AnyDrawing`: `issuedOn` -> `Any.DateOnly().Between(Drawing.EarliestIssue, 2100-12-31)`.
- `AnyBomItem`: `children` (flagged self-referencing, TODO) -> a fixed empty list (items are leaves; trees are built with `WithChildren`).

Tests written (`Tests/`): 181 test methods, 643 cases (632 run, 11 skipped as documented domain defects).
- `PartNumberTests`, `DrawingReferenceTests` (every allowed/forbidden revision letter, check order, `NextRevision` skips and the alphabet walk, equality),
  `DrawingScaleTests`, `IsoToleranceTests`, `StandardToleranceTests` (the full 6x13 ISO table at each range's upper bound and just above it),
  `DimensionTests` (limits per zone, inch conversion, 500 mm rule after conversion, `Accepts` inclusive), `FitTests` (refusal order, mixed units,
  clearance/transition kinds), `DrawingTests` (construction guards, whole workflow, refused release leaves it Approved, `Revise` incl. past `Y`),
  `BomItemTests` (guards, cycle detection, `Depth`, `Contains`, `TotalQuantityOf`, `FlattenToLeaves`), `GeneratorTests` (300 draws each, smoke test of my hand-completed generators).
- `[assembly: Reproducible]` on the whole project; dummies are `Any.Drawing()`, `Any.Dimension()`, `Any.BomItem()`, `Any.DrawingReference().WithRevision(...)` etc.
  where a value must exist and is not what the test is about; values the test is about stay literals.

Result: green with `dotnet test --project Tests.Discovery` (632 passed, 0 failed, 11 skipped); then 12 further runs, all identical and green.
Skipped (kept, `Skip = "domain defect: …"`): newline accepted by `$` in `PartNumber` and `DrawingReference` (2), Unicode digits accepted by `\d` (2), `StandardTolerance` NaN nominal and
unchecked grade (2), `Drawing.Dimensions` leaks the mutable list, `BomItem` null child -> NRE, `BomItem` list aliasing, `TotalQuantityOf` int overflow, `FitKind.Interference` unreachable.
No drawn value ever made a test fail: every red I met was either my own expectation (a wrong IT8 value I had typed) or a real domain defect found with a literal.

## Frictions and surprises, in order
1. **`dotnet test --project Tests.Discovery` does not run out of the box.** With the template csproj (xunit.v3 4.0.2 + Microsoft.NET.Test.Sdk 18.11.0) on the .NET 10 SDK:
   `MSBUILD : error MSB1001: Unknown switch ... Switch: --project`, and plain `dotnet test Tests.Discovery` gives
   `error : Testing with VSTest target is no longer supported by Microsoft.Testing.Platform on .NET 10 SDK and later`. The fix was a `global.json` with
   `{ "test": { "runner": "Microsoft.Testing.Platform" } }`. Nothing in the JustDummies docs mentions this (getting-started says "install, write a test"); a newcomer on SDK 10 loses time
   here. Not a library bug, but `JustDummies.Xunit` page says xunit.v3 4.0.2 and gives no run instructions.
2. **First `any generate` worked well, but the batch output is long.** 8 types gave ~90 lines; the "to verify" rows and the closing "The file will not compile until you resolve it. That is deliberate."
   were clear. I only understood the `TODO_verify_the_generator_for_…` sentinel idea after reading the CLI page; the very first build error list is the work list, which the page promises, and it held.
3. **`Dimension` was scaffolded as fully inferred (`nominal  double  Any.Double().Positive()  guard`, no mark) although its constructor rejects anything above 500 mm.** The rule lives in a
   parameterless private `Validate()` called after the field assignments, which the CLI page lists as one of the shapes that "stay silent". I only noticed because I had read the domain. Measured afterwards:
   `Any.Double().Positive()` gave 9994 draws out of 10000 above 500 (min 59.4, max 1.8e308), so the untouched generator would have failed nearly every run. It is documented, but costly: the recap says
   "3 of 3 parameters inferred" with no `to verify`, and the failure arrives at the first `Generate()`, far from the constraint. I fixed `AnyValidNominal` by hand.
4. **A "to verify" through a delegating helper points at the call, not at the rule.** `PartNumber`: `value: unread guard at PartNumber.cs, line 12: Validate(value);` — the regex is in the helper (line 17) and I had to open it.
   The doc covers this ("a helper the tool cannot see into"), the message could say "inside Validate" at least.
5. **Tied parameters leave the pins meaningless.** `DrawingScale` (`numerator != 1 && denominator != 1` throws): the tool says "ties numerator to denominator" and marks both. The docs' advice (derive inside `Generate()`) is right, but
   after my rewrite `Any.DrawingScale().WithNumerator(5)` is only honoured on the draws that happen to be enlargements, and `.WithNumerator(5).WithDenominator(2)` throws. The doc warns about this in general terms
   ("a parameter you now derive no longer draws from its field"); there is no recipe for "one of two parameters equals a constant". I settled for `Generate()` by hand and literals in tests where the scale matters.
6. **Same-size tie between two composed objects (`Fit(hole, shaft)`).** No documented recipe: the doc examples tie scalars (`Booking`, `Crossing`). I rewrote `Generate()` to rebuild the shaft from the hole's size, which silently
   ignores a pinned shaft nominal. Worked, but the shape is mine, not the library's.
7. **A refinement cannot narrow a `OneOf`.** After `Any.String().OneOf("H","JS","h","js")` in the base I wanted `Hole()`/`Shaft()`. The CLI page says `OneOf` cannot be declared twice and recommends a named recipe
   that ignores its argument (`WithZone(_ => Any.String().OneOf("H","JS"))`). I used exactly that in a `Custom.cs`; it compiled and worked the first time. A little odd to be told elsewhere that `_ => …` is the thing to avoid.
8. **`any generate` on a self-referencing aggregate (`BomItem.children`)** gave a `TODO` with a clear explanation (`reaches the type being scaffolded ... Write one here with a depth bound you choose`). Good message. I chose "always a leaf" and
   built trees with `WithChildren`; nothing in the docs says how to draw a random small tree, so I did not try.
9. **The analyzers said nothing at all during the whole session** (no warning in any build). I never saw JD030 (Info, `Any.String()`) because Info does not print; fine and documented, but it means a newcomer gets no guidance from the
   analyzers unless they raise severities. The only warnings I ever saw were xUnit's own (`xUnit1025` duplicate InlineData, my mistake).
10. **Where to put a seed message.** On failure the seed is printed in "Standard output" of the failed test only: `[JustDummies] These arbitrary values were seeded with 166739806. Reproduce this run with [JustDummies.Xunit.Reproducible(Seed = 166739806)].`
    This is exactly what the docs promised; I did not have to replay anything because no failure was seed-dependent. Listed here because I wanted to see it and had to provoke a red to do it.
11. **Domain facts the draw did not reveal but the literals did.** `\d` accepts Arabic-Indic digits and `$` accepts a trailing `\n` in `PartNumber` and `DrawingReference`; `StandardTolerance` leaves NaN and the grade unchecked; `FitKind.Interference`
    cannot be reached with the four zones. Not library findings, recorded as domain findings in the answer.

## What worked well
- Getting-started's "a literal lies about what matters" and the `anyXxx`-only-in-Arrange rule made me decide, test by test, what was a dummy. It changed what I wrote: refusal tests got dummies for everything but the offending argument.
- `any generate` with several types in one run, `--output`, and `--entry-point any`: `Any.Drawing().WithIssuedOn(...).WithDimensions(l => l.WithCountBetween(1, 5))` reads exactly like the tests I wanted.
- The sentinel mechanism: the build told me, file by file, what remained. The guard inference was right wherever it spoke (`BomItem` description/quantity with `BomItem.MaximumDescriptionLength`, `IsoTolerance` grade with its constants, `DrawingReference.sheet` positive).
- `With…` pin + lambda refinement + named recipes (`Hole()`, `Shaft()`) were easy to add in a file the tool will not overwrite.
- `[assembly: Reproducible]` is one line and the output stays silent when green; 13 runs, no flake.
- Inline errors are good: `StringMatching`'s retype instructions in the TODO comment were right and complete.

## Fluency: 4 / 5
I was productive within minutes of reading getting-started, and the scaffolder plus the TODO sentinels produced a mostly correct base for 8 types in one command. Points were lost on the SDK 10 / MTP `dotnet test` stumble (not documented),
on one silently-unflagged domain rule (`Dimension` size cap in a parameterless helper) that I caught only by reading the code, and on the lack of a recipe for ties between two parameters (one scalar-with-a-constant, one between two objects), where I wrote `Generate()` bodies
by hand with unclear pin semantics.
