# Knitting patterns — expert crafter notes

## What I did

- `Tests.Expert`: 31 test methods (38 cases with one 8-row theory) on stitch arithmetic, runs and
  repeats, row instructions, gauge, sections, the `Fabric` state machine and `KnittingPattern.Expand`.
  All pass; 25 extra runs of the binary (fresh seeds) stayed green. `[assembly: Reproducible]`.
- Scaffolded `StitchRun StitchRepeat RowInstruction Gauge PatternSection KnittingPattern` with
  `--entry-point any --output ./Arbitrary`, then resolved every `TODO(any)`.
- Recipes: `Stitches.Neutral` (knit/purl/slip: the stitches a count does not see) and
  `Stitches.NeutralRunsOver(width)` — "how a plain row of N stitches is written" (k40, or k12 p16 k12…)
  built by construction from a `SetOf` of distinct cut points. `Rows.Plain(rowNumber, width)` turns it
  into a row with or without its count printed. Counts the assertion is about stay literals.

## Frictions and surprises, in order

1. **`x < a || x > b` is never read.** Every numeric guard of this domain has that shape, so
   `count`, `times`, `castOnCount`, both gauge values came out `Any.Int32()`/`Any.Decimal()` + `to verify`:
   `count: unread guard at StitchRun.cs, line 16: if (count < 1 || count > MaxCount)`.
   The same bounds split into two `if`s are read (`Any.Int32().Positive().LessThanOrEqualTo(500)  guard`).
   It is the most common range-guard spelling in C#.
2. **An interface parameter is named as if scaffoldable.** `RowInstruction(…, IReadOnlyList<IRowElement>, …)`
   got `Any.ListOf(new AnyIRowElement())`, but `any generate IRowElement` answers
   `✗ IRowElement is abstract, so Generate() has nothing to instantiate.` The CLI page says only generic
   types are left to a `TODO`. I wrote `AnyIRowElement` by hand.
3. **Null-element guards mark lists `to verify`.** `if (runs.Any(r => r is null))` flags `runs`,
   though `Any.ListOf(new AnyStitchRun())` cannot yield a null. Noise on every collection parameter.
4. **Parameters only quoted in a message get marked.** `rowNumber` is flagged by
   `if (expectedStitchCount is { } expected)` and `name` by the rows loop: both only appear in the
   exception text. Harmless (one line to delete) but it dilutes the signal of real `to verify`s.
5. **Dependent parameters do not fit the scaffolded shape** (one `IAny<T>` per field + pin/refine).
   Rows-per-10cm depends on stitches-per-10cm; the printed stitch count is derived from the elements;
   sections must work the cast-on; a section's rows are consecutive and alternate sides. I rewrote 4 of
   6 generators by hand, using `Func<TDependency, IAny<T>>` fields. The doc's "derive inside
   `Generate()`" works, but leaves the generated `With…` overloads lying (a pin of a derived value).
   A first-class dependent draw (`Then`/`Bind`) would keep the generator shape.
6. **No union of generators.** For "a run or a repeat" I used
   `Any.Combine(Any.Boolean(), new AnyStitchRun(), new AnyStitchRepeat(), (r, a, b) => r ? a : b)`,
   which draws both sides each time. `Any.OneOf` over generators is the JD012 trap, rightly.
7. **`FixedValue<T>` is a private nested class duplicated in every scaffolded file**; hand-written
   generators re-copy it. A public `Any.Fixed(value)` would be smaller.
8. One `Entry.cs` per type: 12 files for 6 types. Fine, a bit noisy in the folder.
9. JD030 (info, via SARIF only) on the scaffolded title `Any.String().NotBlank()`; declared 3..80.
   Info diagnostics never show in `dotnet build` output, so one only sees them with an error log.

## What worked well

- The deliberate non-compiling `TODO_verify_…` lines with file and line of the guard: a precise
  work list, impossible to forget. Composed types named through their own `AnyX` (ADR-0089).
- Refining a composed base reads well: `new AnyStitchRun().WithCount(c => c.LessThanOrEqualTo(12))`
  narrows `Between(1, 500)`; with `times` in 2..200 the 10 000-operation cap holds by construction.
- `Any.SetOf(...).WithMaxCount(...)` for distinct cut points: a partition of a width, no retry loop.
- `Any.KnittingPattern().WithCastOnCount(64).WithGauge(g => g.WithStitchesPer10Cm(16m))`: the test
  states exactly the two facts it is about; title, row gauge and how the rows are written are drawn.
- `[assembly: Reproducible]`: the failure output printed
  `[JustDummies] These arbitrary values were seeded with -1322901331. Reproduce this run with [Reproducible(Seed = -1322901331)].`
  and `Any.Reproducibly(42, …)` replayed a whole pattern drawn through nested hand-written generators.

## Fluency: 4 / 5

The library surface is consistent and composes everywhere I needed it; nothing failed at run time.
One point off for the scaffolder on this domain: the `||` range guards it cannot read and the
cross-parameter rules meant most of its output was rewritten rather than refined.
