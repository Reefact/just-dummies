# Knitting patterns — regular developer notes

## What I did

- Created `Tests.Regular`, restored, scaffolded six types in one run:
  `any generate StitchRun StitchRepeat RowInstruction Gauge PatternSection KnittingPattern --entry-point static:Arbitrary`.
- Resolved every `TODO_verify_…` line, then wrote 30 test methods (37 cases with the theory rows)
  over stitch arithmetic, repeats, rows, the fabric, gauge maths, sections and pattern expansion.
- `[assembly: Reproducible]`, a small `Rows` helper (`K(2)`, `Rep(8, Yo(1), K2Tog(1))`, `Row(5, …)`)
  so written patterns stay readable; drawn values are titles, section names, gauges the assertion
  does not read, row contents in the sequencing tests.
- `dotnet test --project Tests.Regular`: 37/37 green, eight consecutive runs.

## Frictions and surprises, in the order I met them

1. **The commonest range guard is not read.** `if (count < 1 || count > MaxCount) throw …` (StitchRun),
   `if (stitchesPer10Cm < 4m || stitchesPer10Cm > 60m)` (Gauge), `times < 2 || times > MaxTimes`,
   `castOnCount < 1 || castOnCount > MaxCastOn` all came out as `Any.Int32()` / `Any.Decimal()`
   marked `to verify, unread guards`. Single comparisons (`rowNumber < 1`, `name.Length > MaxNameLength`)
   were read fine, constants included. Five of my six generators needed a hand fix for a guard that is
   a plain `Between`. Safe (the build stops), but it is most of the scaffolding work.
   The user docs never list which guard shapes are read; they point to the maintainer spec.
2. **Over-marking.** `RowInstruction.rowNumber` was flagged "first at RowInstruction.cs, line 39:
   `if (expectedStitchCount is { } expected)`" and `PatternSection.name` at the `for` loop over rows:
   the parameter appears only inside the exception *message*. The TODO points at a line that does not
   test the parameter, so I had to read the constructor to decide it was noise.
3. **An interface parameter is emitted as `new AnyIRowElement()`.** `IReadOnlyList<IRowElement>` gave
   `Any.ListOf(new AnyIRowElement()).NonEmpty()` with recap word "to verify, guard" — no `TODO`.
   Following the docs' work list (`any generate IRowElement`) fails with
   "✗ IRowElement is abstract, so Generate() has nothing to instantiate." (exit 1). The recap should
   say so up front, as it does for generics.
4. **No "one of these generators".** I wanted elements to be a run *or* a repeat. There is
   `Any.OneOf(values)` but nothing choosing between `IAny<T>`s; I settled for runs only in the base
   and pinned repeats in tests. (`Any.ListOf<IRowElement>(new AnyStitchRun())` compiles: `IAny` is
   covariant — nice.)
5. **Cross-parameter guards** (Gauge rows within ½–2× stitches; repeat ops ≤ 10 000; printed count =
   produced count; consecutive alternating rows; contiguous sections) — all mine to write, as the
   docs announce. I chose disjoint ranges by construction for Gauge and repeats, derived the printed
   count in `Generate()` (checking `is FixedValue<int?>` so a pinned wrong count survives — a bit
   hacky), and built rows/sections with `Any.Combine` in an `AnyPatternSection.Custom.cs`
   (`StartingAtRow(int)`). The "When a guard ties two parameters" section was exactly the right read.
6. Replacing a builder base with an `IAny<IReadOnlyList<…>>` meant deleting the refining overload by
   hand, as documented. Fine, but a hand-edited `Generate()` / factory is lost on `--force`; I moved
   what I could into the `.Custom.cs` partial.
7. Small: my first `WithCount(c => …)` on a nested generator (`new AnyStitchRun().WithCount(c => c.LessThanOrEqualTo(10))`)
   inside another generator's base worked first time — refinements compose well.

## What worked really well

- The recap table + one-line "unread guard at File.cs, line N: <code>" — I never hunted for a guard.
- A file that refuses to compile until I read it: no silently-invalid dummy ever reached a test.
- `Arbitrary.Gauge().WithStitchesPer10Cm(22m)` / `.WithStitchesPer10Cm(s => s.GreaterThanOrEqualTo(15m))`:
  pin what the assertion reads, refine what a pinned neighbour constrains, keep the rest drawn.
- `static:Arbitrary` entry point across six types, no C# 14 needed, no file written twice.
- `[assembly: Reproducible]` — one line, then forget about seeds.
- No analyzer noise at all in this suite.

## Fluency: 4 / 5

Scaffold → fix → test went fast and nothing ever drew an invalid value. One point off because the
`a < min || a > max` guard — the most common guard there is — is not inferred, so the "scaffold" step
turned into "rewrite five recipes by hand", and because of the over-marking and the interface dead end.
