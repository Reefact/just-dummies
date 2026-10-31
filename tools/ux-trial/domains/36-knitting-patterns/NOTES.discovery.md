# Discovery notes — 36 knitting patterns

## What I did

1. Read `guides/getting-started`, then `packages/justdummies-cli`, `guides/composition`, and the numbers,
   collections, enums and xUnit pages. Read the domain and `MODEL.md`.
2. Created `Tests.Discovery`, restored, ran
   `any generate StitchRun StitchRepeat RowInstruction PatternSection Gauge KnittingPattern --entry-point any`.
3. Fixed the 13 build errors the scaffold left on purpose, wrote `AnyRowElement` by hand, encoded three
   rules that tie parameters together, then wrote 34 test methods (41 cases with the theory rows).
   `dotnet test` passed 41/41 on five runs in a row.

## Frictions and surprises, in the order I met them

1. **A plain range guard is not read.** `if (count < 1 || count > MaxCount)`, and also
   `if (stitchesPer10Cm < 4m || stitchesPer10Cm > 60m)` with literal bounds, both came back as
   `to verify, unread guards` with the recipe `Any.Int32()` / `Any.Decimal()`. That is the most common guard
   shape in this domain (4 of the 6 types use it), so 5 of the 13 TODOs were spent typing `Between(...)` by hand.
   I expected `Between(1, 500)`, since the docs show `quantity <= 0` becoming `.Positive()`.
2. **The location a TODO points at can be unrelated to its parameter.** For `rowNumber` the TODO says
   "first at RowInstruction.cs, line 39: if (expectedStitchCount is { } expected)". `rowNumber`'s own guard
   (`rowNumber < 1`) *was* read (→ `Positive()`). I only understood the pointer after seeing that the throw's
   message interpolates `rowNumber`. Same thing for `PatternSection.name`, which points at the row-continuity loop.
   The docs (ADR-0110 paragraph) explain "marks every parameter", but the pointer still reads as "your guard is here".
3. **The scaffold names a generator for an interface:** `Any.ListOf(new AnyIRowElement())`, shown as `AnyX`.
   The docs say a missing `AnyX` is "the work list", so I ran `any generate IRowElement`. It refused (exit 1):
   "IRowElement is abstract, so Generate() has nothing to instantiate. Scaffold a concrete type that derives
   from it instead." That message is clear, but the work list sent me to a dead end; a `TODO` in the first file
   would have been more honest.
4. **No way to draw "one of these generators".** A row element is a `StitchRun` or a `StitchRepeat`. `Any.OneOf`
   takes values (JD012 warns against generators), so I wrote `AnyRowElement` with `Any.Boolean()` choosing.
   Four lines, but I searched the docs for a union/choice combinator and found none.
5. **Rules tying parameters together meant restructuring generated code.** Three in this domain: rows/10 cm
   within half to twice stitches/10 cm (`Gauge`), printed count equals what the elements produce
   (`RowInstruction`), runs x times <= 10 000 operations (`StitchRepeat`). The CLI page's "When a guard ties two
   parameters" section helped. For the first two I made the field nullable (`null` = "derive it in `Generate()`"),
   which keeps the `With…` pins working; the third I solved by narrowing both bases. Edits live in the generated
   file, so `--force` would erase them; moving them to a partial file is not possible for `Generate()`.
   Not clear from the docs which of these shapes is recommended when a `With…` pin must still win.
6. **Collection-level rules (rows consecutive and alternating, sections contiguous from row 1)** cannot be said
   by refining the list. I chose a one-element base list (`WithCount(1)` of a row 1 on the right side). Works,
   but only because the domain accepts a one-row section.
7. One arithmetic slip of mine failed a test. The output was exemplary:
   `[JustDummies] These arbitrary values were seeded with -1306247851. Reproduce this run with [Reproducible(Seed = -1306247851)].`

## What worked well

- The deliberate non-compiling TODO line: impossible to forget a parameter, and each error sits at the right factory.
- The recap's last column told me at once where to look.
- Pin / refine pairs read well in tests: `Any.Gauge().WithStitchesPer10Cm(22m)`,
  `Any.RowInstruction().WithRowNumber(2).WithSide(FabricSide.WrongSide).WithElements(...)`.
  Nested refinement `new AnyStitchRun().WithCount(c => c.LessThanOrEqualTo(20))` worked first time.
- `--entry-point any` gave `Any.KnittingPattern()` and the arrange blocks read uniformly.
- `[Reproducible]` on the class: zero ceremony, and the seed message speaks the attribute's own syntax.
- No analyzer noise on a normal build.

## Limits of this trial

Most assertions here are about stitch counts, which are load-bearing and stay literals; dummies are names,
titles, gauges, cast-ons, sides and row numbers the rule under test does not read. The domain did not block me.

## Fluency: 4 / 5

Productive within the hour, and the TODO-as-compile-error flow is genuinely good. One point off because the
scaffolder missed the most common guard shape (`x < a || x > b`), pointed some TODOs at unrelated lines, sent
me to scaffold an interface, and three cross-parameter rules needed restructuring of generated code.
