# Expert notes — 36 knitting patterns
Agent: the same model and effort as every session of this trial. Library: JustDummies 1.0.0-uxtrial.1 (packed from main).

## What I did

**Pages read, in order:** `PROJECT-README.md`, `guides/getting-started`, `guides/core-concepts`, `packages/justdummies-cli`
(the whole page, it is the longest), `packages/justdummies-xunit`, `guides/composition`, `generators/README`,
`generators/numbers`, `generators/enums-and-choices`, `generators/collections`, the start of `guides/reproducibility`,
`packages/justdummies` and `analyzers/README` (to raise the Info rules).

**Project:** `Tests.Expert/KnittingPatterns.Tests.Expert.csproj` from the template, plus a `global.json` in the domain folder
(see friction 1), an `.editorconfig` raising the four JustDummies rule categories to `warning` so the Info rules show up in the
build, and `[assembly: Reproducible]` in `GlobalUsings.cs` (every test is replayable, nothing is wrapped by hand).

**`any` commands run** (after `dotnet restore`, see friction 2):

    any generate StitchRun StitchRepeat RowInstruction Gauge PatternSection KnittingPattern ExpandedRow Fabric \
        --output Generators --namespace KnittingPatterns.Tests.Expert.Generators --entry-point any

One batch, eight types, 16 files (generator + `Any.Xxx()` entry point each). Exit 0, but six generators did not compile on purpose
(`StitchRepeat`, `RowInstruction`, `Gauge`, `PatternSection`, `KnittingPattern`, `ExpandedRow`).

**Generators completed by hand** (all in `Generators/`):

* `AnyStitchRun`, `AnyFabric`: kept as scaffolded.
* `AnyStitchRepeat`: removed the two sentinels; narrowed the base to at most 3 runs of at most 3 stitches, so that
  `9 x StitchRepeat.MaxTimes = 9 000 <= 10 000` holds by construction whatever `times` is drawn (the pins stay independent).
* `AnyIRowElement` (+ entry point): hand-written, the tool cannot scaffold an interface; it draws a run or a repeat.
* `AnyRowInstruction`: `elements` from `AnyIRowElement`; the printed count is *derived* in `Generate()` (correct, or absent) because
  it is tied to the elements; added `WithCorrectlyPrintedStitchCount()`; dropped the refining overload of that parameter.
* `AnyGauge`: rows are drawn after the stitches, bounded to half..twice of them in `Generate()`; a pinned row gauge is honoured as is.
* `AnyPatternSection`: rewritten around a first row, a row count and the alternation of the sides (odd rows right side);
  `StartingAtRow`, `WithRowCount`, `WithRows`. `AnyPatternSection.Custom.cs` publishes the valid name base.
* `AnyKnittingPattern`: unless pinned, the sections are *built from the drawn cast-on* so that every row consumes what the previous
  one left (`Support/Rows.cs` builds an arbitrary row consuming exactly N stitches: decreases, increases, yarn overs, sometimes a
  rib repeat). `AnyKnittingPattern.Custom.cs` publishes the valid title base.
* `AnyExpandedRow`: `stitchesBefore` derived from the drawn operations (plus a knit when only yarn overs were drawn).
* `Support/Rows.cs`: `SideOf`, `Chunks` (runs split at 500), `PlainRow`, `ArbitraryRowConsuming`.

**Tests written:** 136 methods, 205 cases, in 11 classes.

| class | methods | what it pins |
| --- | --- | --- |
| `StitchKindTests` | 1 (8 cases) | the consume/produce table |
| `StitchRunTests` | 7 | undefined kind, count 0/-1/501/max, edges 1 and 500, arithmetic, expansion, `ToString` |
| `StitchRepeatTests` | 12 | empty/null/null-element runs, times 0,1,1001, edges 2 and 1000, the 10 000-operation boundary (exactly 20 x 500, then 21), several runs, ribbing/eyelet/increase arithmetic, expansion order, defensive copy |
| `GaugeTests` | 15 | edges of the three rules (stitch range, row range, ratio, with `ArgumentOutOfRange` vs `Argument` told apart), rounding (half up, minimum 1), refusals of non-positive width/length/stitches, `WidthOf`, a round trip with a non-exact width |
| `PatternSectionTests` | 13 | blank/null name, 60/61 characters, trimming, null/empty/null-row lists, jumps, repeated/backward numbers, same side twice (first pair and middle), single row, first/last, defensive copy |
| `RowInstructionTests` | 14 | row number, side, null/empty/null-element lists, printed count correct / absent / wrong / zero / negative, the "k2, *yo, k2tog; rep from * 8 times, k2 (20 sts)" row, arithmetic, expansion order, defensive copy |
| `ExpandedRowTests` | 13 (1 skipped) | after/increases/decreases of a mixed row, plain row, refusals (consumption, null/empty, stitches, row number, side, section name), defensive copy, the cast-back defect |
| `FabricTests` | 20 | cast-on edges, initial state, count moved by a row, the `ExpandedRow` reported, yarn overs, side alternation over three rows, out-of-sequence/repeated/skipped/wrong-side rows, mismatch with both counts, state untouched after a refusal (count and side), precedence of the sequence/side errors over the count error, null row, blank section |
| `StitchCountMismatchExceptionTests` | 2 | properties, message, is an `InvalidOperationException` |
| `KnittingPatternTests` | 29 | title/gauge/cast-on/sections refusals and edges, row-1 start, gap, shared row, duplicate names (case and spaces), row count, defensive copy, `Expand` on a literal 5-row, 3-section hat (counts, section names, increases/decreases, unrolled repeat), repeatable expansion, mismatch naming the row, patterns built but not expandable, finished width on both sides of the cast-on |
| `GeneratorSanityTests` | 10 | each generator draws something the real constructors accept, the pattern one also at the widest cast-on |

**Dummies versus literals.** Literals are the rows, counts, boundaries and gauges every assertion is about. Dummies are: the stitch
kind when only the count is counted, the gauge in the refusals of a non-positive width, the section name/title/gauge/sections in the
refusals of *another* argument, the elements of a row whose number or side is what is tested, the whole fabric when only its sequencing
is tested (rows are then sized from `fabric.LiveStitches`, never from a draw reaching the assertion), and a whole pattern in "expanding
twice gives the same rows". No `anyXxx` reaches an assertion except through a comparison between two runs of the same code.

**Result:** green on the first complete run, apart from two mistakes of mine in expected values (a `6 / 3.5` gauge row below 4, and a
`0.1 cm` width I mis-multiplied). Then 15 more runs, fresh seeds each time, all green: `total 205, failed 0, succeeded 204, skipped 1`.
I also removed the `Skip` once to check that the skipped test really fails: it does (`Expected [Knit, Purl], Actual StitchKind[]
[CentralDoubleDecrease, Purl]`). With the four rule categories at `warning`, the build has no JustDummies diagnostic left.

## Frictions and surprises, in order

1. **`dotnet test --project Tests.Expert` does not run on this setup.** Command: `dotnet test --project Tests.Expert`. Output:
   `MSBUILD : error MSB1001: Unknown switch. ... --project Tests.Expert`. Without the flag (`dotnet test Tests.Expert`):
   `Microsoft.Testing.Platform.MSBuild.targets(395,5): error : Testing with VSTest target is no longer supported by Microsoft.Testing.Platform
   on .NET 10 SDK and later. If you use dotnet test, you should opt-in to the new dotnet test experience.` I expected the documented xUnit v3
   flow to work. I wrote `{ "test": { "runner": "Microsoft.Testing.Platform" } }` in `global.json` (domain folder), after which the
   `--project` form works. Neither `justdummies-xunit.en.md` nor `getting-started` says anything about it; a newcomer on the .NET 10 SDK
   hits it before the first test (this is xunit.v3/SDK behaviour, not the library's, but the adapter page is where a reader looks).
2. **`any generate` on a fresh project:** `✗ StitchRun: this project has not been restored, so its package references cannot be read.
   Restore it, then run the command again: dotnet restore`. Clear and actionable (I ran `dotnet restore`), but the CLI page's "Requires" section
   does not list "a restored project"; the message does the job.
3. **Batch scaffolding worked well, then the tie rules took most of the effort.** Eight types in one command; entry points
   (`--entry-point any`) compile on net10.0 with no setting. The recap is honest and ordered: StitchRun/Fabric needed nothing,
   six types stop the build with a sentinel (`CS0103: The name 'TODO_verify_the_generator_for_sections' does not exist`). That is
   exactly the intended behaviour, and I never wondered what was unfinished.
4. **The sentinels ask for the right thing but the tool cannot help once a rule *ties* parameters.** `Gauge` (rows within half..twice the stitches),
   `StitchRepeat` (runs x times <= 10 000), `RowInstruction` (printed count = what the elements produce), `ExpandedRow`
   (operations consume `stitchesBefore`), `KnittingPattern` (sections contiguous from row 1 *and* every row consuming what the previous left) are
   all dependent parameters. The docs offer four forms ("start + duration", derive inside `Generate()`, check digit, `DifferentFrom`) and I used
   the second three ways. It works and the docs warned me about the cost: the `With…` pin of the derived parameter no longer reaches the draw. I had to
   re-wire pins by hand (`_rowsPinned` in `AnyGauge`, `_printsProducedCount` + a nullable `_expectedStitchCount` in `AnyRowInstruction`,
   a nullable `_sections` in `AnyKnittingPattern`, `stitchesBefore` With methods deleted in `AnyExpandedRow`). About 25 lines each, none difficult, but
   this is where the scaffold stops helping and it is the part that makes the generator trustworthy. No complaint about the design; a recipe for "a
   parameter derived from the others, still pinnable" would remove the most error-prone part (idea, below).
5. **The recap's "unread guard" pointers are not always the guard that matters.** For `RowInstruction` the tool prints
   `elements: unread guard at RowInstruction.cs, line 41: if (expected < 1) ...` and the same line for `expectedStitchCount`. Line 41 is the
   `expected < 1` check; the rule that really involves `elements` is `expected != produces` six lines further, which is nowhere cited. For `Gauge` the tool
   adds `— ties rowsPer10Cm to stitchesPer10Cm`; for `StitchRepeat` (`operations > MaxOperations`, a product of `runs` and `times`) and
   `RowInstruction` it does not say the guard ties parameters, so I had to read the constructors to find out. I read the constructors anyway, but the
   "ties X to Y" note is the most useful word of the recap.
6. **A pattern-variable guard is not read.** `expectedStitchCount` is `int?` guarded with `if (expectedStitchCount is { } expected) { if (expected < 1) throw ... }`.
   The tool gave `Any.Int32().OrNull()` marked `to verify, unread guards` instead of `.Positive()`. Documented limits (the guard sits under a condition and goes through an
   alias), so I did not expect better; the rewrite hint in the docs ("one rewrite away") applies but the domain code is not mine to change.
7. **Interface parameter:** `elements  IReadOnlyList<IRowElement>  —  TODO, unread guards, abstract` and, in the file, a sentinel naming exactly what to write
   (`AnyIRowElement : IAny<IRowElement>`). The documented sample (`Any.OneOf<IRowElement>(new Stitch(), new Increase())`) is for constant elements; mine draws a run or a repeat, each with
   its own generator, so `OneOf` did not fit and I wrote `Generate()` as `Any.Boolean().Generate() ? run : repeat`. Fine, but it is a hand-rolled choice among *generators*, and there is no
   `Any.OneOf(IAny<T>...)`-style helper: `JD012` flags generators passed to `OneOf` (it infers a pool of recipes). A `Choose(generators)` would be the missing piece (idea, low).
8. **The scaffolded `Any.String().NotBlank()` earns `JD030` in my own build as soon as the Info rules are visible.** I raised the four categories to `warning` in
   `.editorconfig` (as the docs suggest) and got three `JD030: This string dummy declares no length: it draws 1 to 1025 characters` at the scaffolded title/section-name bases
   and at my own helper. The docs announce it ("by design"); I fixed it with `WithMaxLength(...)`. Noted because the scaffold and the bundled analyzer disagree by default.
   The message says 1025 where the guide says 0 to 1024 characters (the `NotBlank` floor, so consistent).
9. **A guard the tool reads gets copied by name, which is nice.** `Any.Int32().Positive().LessThanOrEqualTo(StitchRun.MaxCount)` and `KnittingPattern.MaxCastOn`: the recipe follows the constant.
   A small catch: for `AnyKnittingPattern` the cast-on base is `Positive().LessThanOrEqualTo(2000)`, which is right for the constructor but not for what my sections builder can cope with
   (rows are built of runs of at most 500 stitches), so I narrowed the base to `Between(1, 100)` and let a test pin `MaxCastOn` explicitly (the sanity test does).
10. **`ArgumentOutOfRangeException` derives from `ArgumentException`**, so `Assert.Throws<ArgumentException>` fails against the former and the other way round. Not the library's, but
    several domain refusals use one or the other for sibling rules (Gauge ratio is `ArgumentException`, its ranges `ArgumentOutOfRangeException`) and the tests tell them apart on purpose.
11. **Replay output is good.** The two real failures I had (my mistakes) printed `[JustDummies] These arbitrary values were seeded with -1090275281. Reproduce this run with
    [JustDummies.Xunit.Reproducible(Seed = -1090275281)].` in the test's standard output, once per failing case, and nothing for the green ones. I did not need to replay a
    domain failure, so I did not exercise the replay itself.
12. **`[assembly: Reproducible]` + theories:** no surprise; each case got its own seed.

## What worked well

* `any generate` on a batch of eight types with `--entry-point any`: 16 files, compile-ready entry points, recipes that follow the domain's own constants, and sentinels that break the build exactly where
  a guard was not understood. The recap and the sentinels agree with each other.
* Reading the generated file was enough to rewrite it: `private static AnyXxx AnyValidYyy()` per parameter, one place to put a rule; `Func<...>` refinements composed.
* `[assembly: Reproducible]` gives every test a replay snippet without touching a body; the output format is precise.
* The dummies discipline in the docs pays off in the tests: once rows are sized from `fabric.LiveStitches` or from literals, no draw ever reaches an assertion, and the 15 repeated runs
  stayed green across varied cast-ons (1..2000 for the fabric, 1..100 for patterns), gauges and row shapes.
* The analyzers are quiet and accurate: after the rules were raised to warnings only the three documented `JD030` showed up.
* `Any.ListOf(...).WithCount(n).Distinct(StringComparer.OrdinalIgnoreCase)` is exactly the comparer the domain uses for section names, which made distinct names easy to draw.

## Fluency: 4 / 5
Everything the docs promise worked the first time, including compiling entry points, the sentinel design and the replay output; I never had to guess at the API. The deduction is for the part the
library deliberately leaves to the user: six of the eight types have rules between parameters, and re-wiring pins by hand around a derived parameter is where the time and the risk went. The
`dotnet test --project` setup (friction 1) cost a few minutes that the adapter page could save, and the recap's "ties" annotation is inconsistent between types.

## Domain observations (not fixed, not hidden)
* `KnittingPattern` accepts sections whose sides do not line up across a section boundary, or a first row written on the wrong side; the pattern then cannot be expanded
  (`InvalidOperationException` only at `Expand()`). Pinned as behaviour by two tests whose names say so.
* `Operations` (and by reading the code `Runs`, `Elements`, `Rows`, `Sections`) return the internal array as `IReadOnlyList`, so a cast and a write bypass the invariants
  (skipped test `An_expanded_row_cannot_be_rewritten_through_the_list_of_operations_it_exposes`, deterministic).
* `PatternSection` checks the 60-character limit on the untrimmed name: `"  " + 59 letters + "  "` is refused, though the stored name would be 59 characters.
* `ExpandedRow` accepts an undefined `StitchKind` in `operations` (`(StitchKind)99` counts as a plain 1-to-1 stitch: after = 1, increases = 0) while `StitchRun` refuses it.
