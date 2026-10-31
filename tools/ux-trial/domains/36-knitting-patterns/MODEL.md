# Knitting Pattern Stitch Expansion — model

Namespace `KnittingPatterns.Domain`. A flat pattern is expanded row by row into single-stitch
operations while a `Fabric` tracks the live stitch count; every row must consume exactly the
stitches the previous one left.

## Types and invariants

- `StitchKind` (enum) + `StitchKindExtensions.Consumes/Produces`: knit, purl, slip (1→1),
  yarn over (0→1), kfb (1→2), k2tog and ssk (2→1), central double decrease (3→1).
- `IRowElement`: `Consumes`, `Produces`, `OperationCount`, `Expand()`.
- `StitchRun(kind, count)`: defined kind; count 1..500.
- `StitchRepeat(runs, times)`: at least one non-null run; times 2..1000; expands to at most
  10 000 operations (refused otherwise).
- `RowInstruction(rowNumber, side, elements, expectedStitchCount?)`: row >= 1; defined
  `FabricSide`; at least one non-null element; a printed count is >= 1 **and equals** what the
  elements produce.
- `Gauge(stitchesPer10Cm, rowsPer10Cm)`: stitches 4..60, rows 4..80, and rows within
  half to twice the stitches. `StitchesForWidth`, `RowsForLength` (rounded, >= 1), `WidthOf`.
- `PatternSection(name, rows)`: name non-blank, <= 60 chars; at least one row; rows
  consecutive and alternating sides.
- `KnittingPattern(title, gauge, castOnCount, sections)`: title non-blank; cast-on 1..2000;
  at least one section; first section starts at row 1; sections contiguous; names unique
  (case-insensitive).
- `ExpandedRow(sectionName, rowNumber, side, stitchesBefore, operations)`: operations
  non-empty and consuming exactly `stitchesBefore`; `StitchesAfter`, `Increases`, `Decreases`
  derived.
- `Fabric(castOnCount)`: entity; `LiveStitches`, `RowsWorked`, `NextSide` (RS first).
- `StitchCountMismatchException(rowNumber, liveStitches, requiredStitches)`.

## Behaviour

- `Fabric.Work(sectionName, row)`: refuses a row out of sequence or on the wrong side
  (`InvalidOperationException`), refuses a row whose consumption differs from the live count
  (`StitchCountMismatchException`), otherwise returns the `ExpandedRow` and moves to its
  `StitchesAfter`.
- `KnittingPattern.Expand()`: works every row of every section on a fresh fabric.
- `KnittingPattern.FinishedWidthCm()`: width of the last row's stitches at the gauge.
