# Expert notes — 05 CAD drawing
Agent: the same model and effort as every session of this trial. Library: JustDummies 1.0.0-uxtrial.1 (packed from main).

## What I did
Pages read, in order: `PROJECT-README.md`, `guides/getting-started.en.md`, `guides/core-concepts.en.md`, `guides/composition.en.md`,
`generators/README.md`, `packages/justdummies-xunit.en.md`, `packages/justdummies-cli.en.md` (whole), then looked up `generators/strings.en.md`
(patterns), `numbers.en.md`, `dates-and-times.en.md`, `collections.en.md` when a constraint was needed.
Domain read: `MODEL.md` and every file of `Domain/`.

Project: `Tests.Expert/Drafting.Domain.Tests.Expert.csproj` (the template, unchanged), `AssemblyInfo.cs` (`[assembly: Reproducible]`),
`any.json` (`output`, `namespace`, `entryPoint: any`) and, one level up, `global.json` (see friction 1).

`any` commands run (from `Tests.Expert`, after `dotnet restore`):
`any generate PartNumber DrawingReference DrawingScale IsoTolerance Dimension Fit BomItem Drawing` (defaults from `any.json`: `Generators/`, entry points `Any.X()`).

Generators completed by hand (all 8 files compile, 0 warnings):
- `AnyPartNumber`: `Any.StringMatching("P[0-9]{6}(-[A-Z0-9]{2})?")`, retyped `AnyString` -> `AnyPattern` at the four places the doc lists.
- `AnyDrawingReference`: number `Any.StringMatching("(MEC|ELE|HYD)-[0-9]{5}")` (same retype); revision `Any.Char().Alpha().InUpperCase().Except("IOQSXZ")`.
- `AnyDrawingScale`: removed the two `TODO_verify`; `Generate()` collapses one side to 1 when both terms drew above 1.
- `AnyIsoTolerance`: zone `Any.String().OneOf("H", "JS", "h", "js")`.
- `AnyDimension`: nominal `GreaterThan(0).LessThanOrEqualTo(500 / 25.4)` (valid in both units) - the scaffold had `Positive()` (friction 3).
- `AnyFit`: hole `new AnyDimension().WithTolerance(t => t.ForHole())`, shaft `...ForShaft()`; `Generate()` gives the shaft the hole's size.
- `AnyBomItem`: children recipe (0..3 leaf components), `Generate()` draws the children first and redraws the part (bounded) until it is in none of them.
- `AnyDrawing`: `issuedOn` `Any.DateOnly().AfterOrEqualTo(Drawing.EarliestIssue)`.
Neighbour `*.Custom.cs` files (survive `--force`): `ForHole()/ForShaft()`, `ValidNumber()/ValidRevision()/ValidSheet()/ValidTerm()/ValidGrade()/ValidZone()` (internal accessors to the private bases, the pattern the CLI page recommends), `WithANextRevision()`, `GenerateIn(DrawingStatus)` (walks a drawing through the real transitions).

Tests (`Tests/`, 9 classes, ~172 test methods, 521 cases, 11 methods skipped as domain defects):
- `PartNumberTests`, `DrawingReferenceTests` (formats, every allowed revision letter, the 6 forbidden ones + neighbours, sheet bounds, check order, NextRevision incl. skips and the walk A..Y, equality/hash/ToString).
- `DrawingScaleTests`, `IsoToleranceTests`, `StandardToleranceTests` (the full 78-value ISO table at each range's closed upper bound and just above it, bounds, guard order).
- `DimensionTests` (guards, 500 mm limit once converted from inches, deviations per zone/grade/range, inclusive `Accepts`, text), `FitTests` (guards and their order, size tolerance 1e-9, mixed units, H7/h6, H7/js6, JS7/h6, zero-clearance boundary).
- `DrawingTests` (guards and their order, defensive copy, whole lifecycle with every wrong source state via `[Theory]`, release date boundary, revise incl. past `Y`), `BomItemTests` (guards, cycle detection at depth, Depth/Contains/TotalQuantityOf/FlattenToLeaves on literal trees), `GeneratorsTests` (300 draws of each hand-completed generator).
Where a value is what the test is about (part numbers in a tree, revision letters, sizes, dates around a boundary) it is a literal; where it only has to be valid (descriptions, titles, sheets, the unchecked arguments of a refusal test, dimensions of a drawing) it is drawn.

Result: green with `dotnet test --project Tests.Expert`, then 12 further runs (and 5 more during development), each with fresh seeds (`[assembly: Reproducible]`): 12/12 green, 0 failures, 11 skipped. I also ran once with the 11 `Skip`s removed to prove each fails for the reason written in its skip message (13 cases failed, all as announced).

## Frictions and surprises, in order
1. **`dotnet test --project` does not run on the supplied template.** `dotnet test --project Tests.Expert` -> `MSBUILD : error MSB1001: Unknown switch ... Switch: --project`; the VSTest form `dotnet test Tests.Expert` -> `error : Testing with VSTest target is no longer supported by Microsoft.Testing.Platform on .NET 10 SDK and later. If you use dotnet test, you should opt-in to the new dotnet test experience`. I had to add a `global.json` containing `{ "test": { "runner": "Microsoft.Testing.Platform" } }`. Neither the xUnit adapter page nor the getting-started page says that an xUnit v3 `Exe` project needs it on the .NET 10 SDK (the README only says the repo pins its SDK in `global.json`). Expected: a line in `justdummies-xunit.en.md`.
2. **`any generate` before a restore** -> `✗ PartNumber: this project has not been restored, so its package references cannot be read. Restore it, then run the command again: dotnet restore` (exit 2). Clear and actionable; I only note that the "ten-minute" path never says to restore first.
3. **A silent guard in a parameterless helper.** `Dimension` checks `NominalInMillimetres > 500` in a private `Validate()` called after the assignments. The recap read `nominal  double  Any.Double().Positive()  guard`, `3 of 3 parameters inferred`, nothing `to verify`. That recipe draws up to ~1e6 and beyond, so almost every draw would throw `ArgumentOutOfRangeException ... Nominal sizes above 500 mm`. The CLI page does list this shape under "What the tool still cannot see stays silent" (a helper called below the first assignment to state), but a parameterless `Validate()` that reads a property derived from two parameters is the most idiomatic place for a cross-parameter rule, and the label `guard` makes the row look verified. I only caught it by reading the domain. (Fails loudly on the first run, so not dangerous, only misleading.)
4. **A tie of the form "one of the two must be 1"** (`DrawingScale`): correctly marked `to verify` on both parameters, message `ties numerator to denominator`. None of the four documented forms (start+duration, derive in `Generate()`, check digit, `DifferentFrom`) fits; "derive in `Generate()`" does, but the doc's own caveat (a `With…` pin then no longer reaches the draw) bit me: I chose a "collapse one side when both exceed 1" rule and documented that pins of both terms are the way to be exact. Fine, but a sample for an either/or tie would help.
5. **Recursive aggregate (`BomItem`)**: `children  IReadOnlyList<BomItem>  —  TODO, unread guards, self-referencing`, with a good message ("Write one here with a depth bound you choose"). The doc has no worked sample of a self-referencing type, and the real difficulty is not the depth but the tie with `part` (a part may not occur in its own subtree), which is again outside any per-parameter recipe. I wrote the leaf-component recipe, drew the children first, then redrew the part with a bounded loop in `Generate()`. This works, but it means three of my eight generators (`DrawingScale`, `Fit`, `BomItem`) carry hand-written `Generate()` bodies in the scaffolded file, which `--force` would erase; there is no documented place for such a body in the `partial` neighbour (see Ideas).
6. **Pinning `null` to test a guard is ambiguous**: `Any.Drawing().WithReference(null!)` -> `error CS0121: The call is ambiguous between 'AnyDrawing.WithReference(DrawingReference)' and 'AnyDrawing.WithReference(Func<AnyDrawingReference, AnyDrawingReference>)'` (same for `WithTitle`, `WithScale`, `WithDimensions`). The page explains the cast `(Customer?)null` in the nullable section; for a non-nullable parameter a refusal test needs `(DrawingReference)null!`. Small, but every null-guard test pays it.
7. **My own hand-written `Generate()` broke the pin of `null`**: `Any.BomItem().WithChildren((IReadOnlyList<BomItem>)null!)` reached my `children.Any(...)` and threw `ArgumentNullException ... (Parameter 'source')` instead of the domain's guard (`The_children_cannot_be_null` failed with ParamName `source`). Not a library defect, but it shows the cost of hand-editing `Generate()`: every pin the scaffold supports must still flow through to the constructor.
8. **Testing one argument's refusal needs the others valid, and `AnyValid…()` is private.** For ~25 refusal tests I added `internal static AnyX ValidY()` accessors in `*.Custom.cs`, exactly the one-liner the CLI page recommends under "A parameter's valid base is private". It works and survives `--force`, but I wrote six of them for six classes; it is the single most repetitive piece of glue in a guard-heavy domain.
9. **Pin doc comments mention reference types for value-type pins**: `/// Pins <c>sheet</c> to value, which every draw hands over as it is — one shared instance for a reference type ...` on `WithSheet(int value)` (and `char`, `double`, `int`). Harmless noise, repeated on every pin of the file.
10. **`Any.Char().Alpha().InUpperCase().Except("IOQSXZ")`** needed a doc lookup: `Except` with a string on `Any.Char()` is only documented in the Characters section ("a `string` is a sequence"). It worked first time.
11. **Stale test run after restoring files with an old mtime** (my own `cp`/`mv` of a backup): `dotnet test` reported the previous build's results. Not a library matter; noted because `[assembly: Reproducible]`'s seed report made me suspect nothing, the numbers (13 failed, 0 skipped) were what betrayed it.
12. **JD030** (string with no declared length) is the only diagnostic the project raises, once, on the scaffolded `title` (`Any.String().NotBlank()`); it is Info so `dotnet build` is silent (checked by raising all severities to warning in a throw-away `.editorconfig`: only JD030, reported twice for the same line, `AnyDrawing.cs(52,16)`). I left the scaffold as is, the domain declares no title length.

Domain findings (each is a `Skip = "domain defect: …"` test, verified failing when un-skipped):
- `PartNumber` and `DrawingReference`: `^…$` accepts a trailing `\n` ("P004217\n") and `\d` accepts non-ASCII digits ("P٤٢١٧٠٠"); `[0-9]` and `\z` are needed.
- `StandardTolerance.InMicrometres`: grade outside 6..11 -> `IndexOutOfRangeException` (the model documents it, but it is a leak, not a guard); `NaN` nominal answers the first range.
- `Dimension`: an undefined `LengthUnit` is silently millimetres, whereas `Drawing` refuses an undefined projection.
- `Fit.Kind` can never be `Interference` with the supported zones (hole upper limit is always > 0 and shaft lower limit always <= 0, so the maximum clearance is positive). Reachable: `Clearance` (H/h only, minimum exactly 0) and `Transition`.
- `Drawing.Dimensions` returns the private `List<Dimension>` as `IReadOnlyList`, so `(IList<Dimension>)drawing.Dimensions` can empty a checked drawing.
- `BomItem` stores the `children` list it was given (no copy, unlike `Drawing`), so the own-part guard can be bypassed after construction.
- `BomItem.TotalQuantityOf` / `FlattenToLeaves` multiply in unchecked `int`: 9 999 x 9 999 x 9 999 wraps silently.

## What worked well
- The recap table with its last column is exactly what an expert wants: every `to verify` pointed at the right guard, quoted the line and, for the regex, told me the four places to retype (`AnyString` -> `AnyPattern`), which I applied mechanically and it compiled first time.
- A build that fails at the line of the gap (`TODO_verify_the_generator_for_issuedOn`) is a good work list: I never forgot a parameter.
- Refinement lambdas that start from the valid base: `WithTolerance(t => t.ForHole())` and, on a nullable parameter, the documented "a lambda is drawn without `null`" made hole/shaft generators three lines each, and `Any.Drawing().WithIssuedOn(...).WithDimensions(d => d.NonEmpty())` reads as the scenario. Pins versus lambdas map cleanly onto "chosen versus arbitrary".
- `Any.Drawing()` entry points (`--entry-point any`) with C# 14 extension members worked on net10.0 with no setup.
- `[assembly: Reproducible]` is the right default for a suite: per-case seeds in theories, and the seed line appears in the failing test's output (seen twice).
- Bounded-both-sides doubles drawn whole, `AfterOrEqualTo` on `DateOnly`, `Except("…")` on `Any.Char()` - the constraint vocabulary covered everything the domain needed without ever fighting me.
- Contradiction messages and the guard-by-construction approach: I never hit a generation failure on a drawn value.

## Fluency: 4 / 5
Scaffolding, recipes and the reproducible attribute made a 520-case suite pleasant to write, and the dummy-versus-literal split came naturally from the docs (pins versus lambdas, `anyXxx` naming). Points lost on the set-up (the `global.json` surprise with no mention in the docs), on the places where the tool is silent or helpless for cross-parameter rules (`Dimension` private `Validate()`, the `DrawingScale` either/or tie, the self-referencing `BomItem`), which forced hand-edited `Generate()` bodies that `--force` would erase, and on the glue for guard tests (`Valid…()` accessors, `(T)null!` casts) that a guard-heavy domain needs again and again.
