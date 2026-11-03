# Discovery notes — 36 knitting patterns
Agent: the same model and effort as every session of this trial. Library: JustDummies 1.0.0-uxtrial.1 (packed from main).

## What I did
Pages read, in order: `docs/PROJECT-README.md`; `docs/README.md` (index); `guides/getting-started.en.md`;
`guides/core-concepts.en.md`; `packages/justdummies-cli.en.md` (long: read it twice, the "what the last column
means" table and the "Adjusting a parameter" / "When a guard ties two parameters" sections were the ones I
actually needed); `packages/justdummies-xunit.en.md`; `guides/composition.en.md`; and, to find method names,
`generators/README.md`, `generators/collections.en.md` and `generators/enums-and-choices.en.md` (skimmed by grep).
I never opened the reproducibility or errors pages: nothing went red.

Project: `Tests.Discovery/KnittingPatterns.Tests.Discovery.csproj` from the template, plus a `global.json` at the
workspace root (see friction 1).

`any` commands run (from `Tests.Discovery`):
- `any generate StitchRun StitchRepeat RowInstruction Gauge PatternSection KnittingPattern ExpandedRow` (first call)
  was refused (project not restored); after `dotnet restore` the real one was
  `any generate StitchRun StitchRepeat RowInstruction Gauge PatternSection KnittingPattern ExpandedRow --entry-point any`. 14 files written (7 generators + 7 `.Entry.cs`). `Fabric`, `StitchKind`, `IRowElement`
  and the exception were not scaffolded: `Fabric(castOnCount)` is an entity I build in each test, `StitchKind` is
  `Any.Enum<StitchKind>()`, `IRowElement` is abstract.

Generators completed by hand (6 of the 7 scaffolds stopped the build until I did; `AnyStitchRun` compiled as is):
- `AnyStitchRepeat`: two `to verify` rows (the 10 000-operation limit). Bounded the base so the limit holds by
  construction: runs of at most 10 stitches, 1 to 3 runs, 2 to 300 repetitions.
- `AnyIRowElement` (new, hand-written, `IAny<IRowElement>`): chooses a run or a repeat with `Any.Boolean().Then(...)`.
- `AnyRowInstruction`: `elements` TODO (abstract) filled with `Any.ListOf(new AnyIRowElement()).WithCountBetween(1, 4)`;
  `expectedStitchCount` has to equal what the elements produce, so I removed its recipe and `With…` methods and
  derive it in `Generate()` (`WithPrintedStitchCount()` / `WithoutPrintedStitchCount()` replace them).
  `AnyRowInstruction.Custom.cs` (partial) adds `WithStitchNeutralElements(int stitches)`: knit/purl/slip runs that
  leave the count unchanged, so a drawn row can be worked on a fabric of that size.
- `AnyGauge`: rows must lie within half to twice the stitches; kept the stitches recipe, removed the rows recipe and
  `With…`, derived the rows in `Generate()` inside both limits.
- `AnyPatternSection`: `rows` must be consecutive and alternate; rebuilt the base as a start/side/count draw followed by
  `Then`, numbering each row (retyped the field to `IAny<IReadOnlyList<RowInstruction>>`, dropped the lambda overload).
- `AnyKnittingPattern`: `sections` must start at row 1, be contiguous, uniquely named, and (for `Expand()` to work)
  keep the stitch count; sections are drawn in `Generate()` over the drawn cast-on when none is pinned.
- `AnyExpandedRow`: `stitchesBefore` is derived from the operations; operations always include one consuming
  stitch (a lone yarn over would need `stitchesBefore` 0, which the constructor refuses).

Tests written (98 methods, 149 cases): `StitchKindTests` (1 theory, 8 cases), `StitchRunTests` (10),
`StitchRepeatTests` (11), `RowInstructionTests` (11), `GaugeTests` (11), `PatternSectionTests` (11),
`KnittingPatternTests` (18), `FabricTests` (12), `ExpandedRowTests` (11), `GeneratorTests` (2). Literals everywhere a
count, a number or a message is asserted; drawn values only for what a test does not look at (the kind when a count is
refused, the side when a row number is refused, the gauge when a width is refused, section names, titles, a whole
pattern whose title/gauge is not what the test is about). `[assembly: Reproducible]` in `AssemblyInfo.cs`.

Result: green on the first run (149 passed). Then 15 further runs (`--no-build`, fresh seeds) all green. I also ran, in
a throw-away test (deleted), 3 000 draws of each generator including `Any.KnittingPattern().Generate().Expand()`:
no refusal.

## Frictions and surprises, in order
1. **`dotnet test --project Tests.Discovery` does not work out of the box on the .NET 10 SDK.**
   `dotnet test --project Tests.Discovery` -> `MSBUILD : error MSB1001: Unknown switch. ... --project`.
   `dotnet test Tests.Discovery` -> `error : Testing with VSTest target is no longer supported by
   Microsoft.Testing.Platform on .NET 10 SDK and later. If you use dotnet test, you should opt-in to the new dotnet test
   experience` (link aka.ms/dotnet-test-mtp-error). Fix: a `global.json` with
   `{ "test": { "runner": "Microsoft.Testing.Platform" } }`. Nothing in the user docs says an xUnit v3 test project
   needs this (the xunit page covers the package floor `4.0.2` and `NU1608`, not the runner); a newcomer is stuck at
   the very first `dotnet test`. The PROJECT-README mentions `dotnet test --solution` only for the library's own build.
2. **`any generate` before restore:** `✗ StitchRun: this project has not been restored, so its package references cannot
   be read. Restore it, then run the command again: dotnet restore`. The message is clear and gives the fix, but the
   docs' install-to-first-run path never says to restore first. No harm done.
3. **`--entry-point any` is opt-in and easy to miss.** Without it the files only give `new AnyStitchRun()`; the
   getting-started page shows `Any.OrderReference()` in the "after" test without saying where it comes from (the
   answer is in the CLI page, "Reaching it as `Any.Order()`"). I read ahead, so I passed the flag from the start.
4. **The scaffolder stopped on 6 of 7 types, as designed, but the surgery that follows is large.** Every tie between
   parameters (gauge ratio, a printed count equal to a sum, `stitchesBefore` equal to a sum of consumptions, rows
   consecutive, sections contiguous) is `to verify` plus a build error `CS0103 TODO_verify_the_generator_for_...`. The
   docs' answer ("derive one draw from another inside `Generate()`") works, but turning a parameter into a derived one
   means deleting by hand, in each file: the `_xRecipe` field, its constructor assignments (two constructors), the
   `AnyValidX()` factory, the pin and the lambda overload. If any of those is left, a `With…` method still compiles and
   silently sets a field `Generate()` no longer reads (the docs warn about it, no tool does). A checklist in the docs,
   or a way to mark a parameter as "derived" in the scaffold, would have saved most of the work.
5. **Unread-guard attribution is partly misleading.** For `RowInstruction`, the recap says
   `elements: unread guard at RowInstruction.cs, line 41: if (expected < 1) ...` — line 41 concerns the printed count
   only; the guard that involves `elements` is the one at line 46 (`expected != produces`). Same pointer under the
   `to verify` comment in the file. I found the right guard by reading the constructor.
6. **JD037 on the natural way to number a list.** My first `AnyValidRows()` built a list of rows with
   `Combine(first, side, count, (…) => Enumerable.Range(0, count).Select(i => new AnyRowInstruction()…Generate()).ToList())`.
   Build warning: `JD037: This draw runs inside the lambda passed to Combine, apart from the recipe: compose an offset with
   Any.Combine, or derive the dependent draw with Then`. The diagnostic is right and its page helped, but neither
   suggested fix covers "N dependent draws where N is itself drawn". I ended with `Combine(...).Then(run => RowsFrom(...))`
   where `RowsFrom` folds N generators with nested `Combine` calls. Works, reads heavy. A documented pattern for
   "a list whose items depend on their index" would help.
7. **A `With…` helper that draws at configuration time.** `WithStitchNeutralElements(int)` (mine) draws its elements when
   it is called, then pins them. Reusing that generator draws the same elements every time. That is my design, not the
   library's, but the docs' "Adding the overload yourself" shows only `IAny<T>` parameters and nothing about a helper
   that must stay lazy (a `Func`-style refinement would be the right shape). I kept it because I only call it on a fresh
   generator.
8. **xUnit, not JustDummies:** `[InlineData]` cannot carry `decimal`, so the gauge tests pass strings and parse them. The
   docs are silent about numbers with a scale in theory data; not their job, but a one-line tip would be welcome since
   `Any.Decimal()` users will hit it.
9. **I never saw a failing seed report**, because nothing went red: I cannot say how the `[Reproducible]` output reads
    in the MTP console. I did not provoke a failure on purpose.

## What worked well
- `any generate` with `Positive().LessThanOrEqualTo(StitchRun.MaxCount)`: it reads the throw helpers and keeps the
  constant by name, so the generator follows the domain.
- Stopping the build on a guard it cannot vouch for is exactly right. Each `TODO_verify_…` told me which line of which
  constructor to read, and none of the 'unread' ones was a false alarm; deleting the two lines was all that remained
  once the rule was written.
- `Then`, `Combine`, `As`, `Any.Fixed` and `WithCountBetween` compose the way the composition page promises; I wrote the
  pattern generator in one pass and it survived 3 000 draws.
- The distinction "a drawn value in the assertion is not a dummy" was easy to apply on a domain made of counts: almost
  every assertion here is about a number, so the tests are mostly literals and the dummies are the things the code must
  not care about (kind when a count is refused, section names, titles). The refusal tests, where the dummy proves the
  check does not depend on the other arguments, are the best use I found.
- Green on the first run and over 15 repeated runs; zero warnings once the TODOs were resolved.

## Fluency: 4 / 5
The concepts and the generator API were quick to pick up and the docs' "dummy versus literal" rule fitted a numeric
domain well. I lost the most time at the very start (no way to run `dotnet test --project` without discovering the
Microsoft.Testing.Platform opt-in) and in the mechanical editing of generated files when a parameter had to become a
derived one. Everything else (restore, entry point, abstract element, the JD037 rewrite) was solved from the docs
and the tool's messages in minutes.
