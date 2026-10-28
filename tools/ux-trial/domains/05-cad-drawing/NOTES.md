# 05 — Industrial drawing / CAD (`Drafting`)

## Domain model (`Domain/`)

| Type | Guards (shape) | Behaviour |
|---|---|---|
| `IsoTolerance(string zone, int grade)` | `ThrowIfNull`; `ThrowIfLessThan/GreaterThan(grade, const)`; zone checked by a **switch expression** | ISO 286 class (H7, h6, JS9, js6) |
| `Dimension(double nominal, LengthUnit unit, IsoTolerance? tolerance)` | `if (double.IsNaN)`, `if (nominal <= 0)`, cross-field cap (≤ 500 mm after inch conversion) in a **private `Validate()`** | upper/lower limits from the ISO 286 IT6–IT11 table, `Accepts(measured)` |
| `Fit(Dimension hole, Dimension shaft)` | `ThrowIfNull`, **property patterns** (`is not { IsHole: true }`), same-nominal cross-field check | clearance / transition / interference |
| `DrawingScale(int numerator, int denominator)` | two `if (< 1)`, cross-field `numerator != 1 && denominator != 1` | `PaperLengthOf` |
| `DrawingReference(string number, char revision, int sheet)` | `ThrowIfNullOrWhiteSpace`, `[GeneratedRegex]` check, **relational/or pattern** on the revision letter, `ThrowIfNegativeOrZero` | `NextRevision()` skipping I, O, Q, S, X |
| `PartNumber(string value)` | `ThrowIfNullOrWhiteSpace` + regex in a **private static `Validate`** | value equality |
| `BomItem(PartNumber, string, int, IReadOnlyList<BomItem>)` — **recursive** | null checks, `ThrowIfNullOrWhiteSpace`, length ≤ 80, `ThrowIfNegativeOrZero` + `ThrowIfGreaterThan(quantity, 9_999)`, cross-field "a part is not its own component" (LINQ) | `TotalQuantityOf`, `FlattenToLeaves`, `Depth` |
| `Drawing(reference, title, scale, ProjectionMethod, DateOnly issuedOn, dimensions)` | `ThrowIfNullOrWhiteSpace`, `Enum.IsDefined`, `issuedOn < static readonly DateOnly` | Draft → Checked → Approved → Released → Revise() |
| enums `LengthUnit`, `DrawingStatus`, `FitKind`, `ProjectionMethod { FirstAngle = 1, ThirdAngle = 3 }` (no zero member) | | |

## CLI recaps (verbatim)

```
Analyzing Drafting.Domain.IsoTolerance
  zone   string  Any.String().NonEmpty()     to verify, unread guards
  grade  int     Any.Int32().Between(6, 11)  guard
Analyzing Drafting.Domain.Dimension
  nominal    double        Any.Double().Positive()  to verify, guard, unread guards
  unit       LengthUnit    Any.Enum<LengthUnit>()
  tolerance  IsoTolerance  new AnyIsoTolerance()    AnyX
Analyzing Drafting.Domain.Fit
  hole   Dimension  new AnyDimension()  to verify, AnyX, unread guards
  shaft  Dimension  new AnyDimension()  to verify, AnyX, unread guards
Analyzing Drafting.Domain.DrawingScale
  numerator    int  Any.Int32().Positive()  to verify, guard, unread guards
  denominator  int  Any.Int32().Positive()  to verify, guard, unread guards
Analyzing Drafting.Domain.PartNumber
  value  string  Any.String().NotBlank()  to verify, guard, unread guards
Analyzing Drafting.Domain.DrawingReference
  number    string  Any.String().NotBlank()  to verify, guard, unread guards
  revision  char    Any.Char()               to verify, unread guards
  sheet     int     Any.Int32().Positive()   guard
Analyzing Drafting.Domain.BomItem
  part         PartNumber              new AnyPartNumber()                             to verify, AnyX, unread guards
  description  string                  Any.String().NotBlank().WithMaxLength(80)       guard
  quantity     int                     Any.Int32().Positive().LessThanOrEqualTo(9999)  guard
  children     IReadOnlyList<BomItem>  Any.ListOf(new AnyBomItem())                    to verify, unread guards
Analyzing Drafting.Domain.Drawing
  reference   DrawingReference          new AnyDrawingReference()       AnyX
  title       string                    Any.String().NotBlank()         guard
  scale       DrawingScale              new AnyDrawingScale()           AnyX
  projection  ProjectionMethod          Any.Enum<ProjectionMethod>()
  issuedOn    DateOnly                  Any.DateOnly()                  to verify, unread guards
  dimensions  IReadOnlyList<Dimension>  Any.ListOf(new AnyDimension())
Analyzing Drafting.Domain.ProjectionMethod
  constructor ProjectionMethod()
✓ AnyProjectionMethod.cs — no constructor parameters to infer.
✗ StandardTolerance: nothing here constructs it.
✗ Bom: no type by that name in this compilation, and nothing close to it.
```

Also run: several types at once, `--dry-run`, `--entry-point any`, `--entry-point static:Arbitrary`,
`--format json` (clean JSON on stdout, `parametersToVerify` matched the rows), `--force` (Drawing
re-scaffolded after a constructor change), a re-run without `--force` (clear refusal, exit 1).

Every guard the tool did read was honoured (constants `FinestGrade`/`MaximumQuantity` resolved, `<= 0`
→ `Positive()`, `IsNullOrWhiteSpace` → `NotBlank()`, length → `WithMaxLength(80)`, `Enum.IsDefined`
covered by `Any.Enum`). Every guard it could not read was flagged `to verify` — honest recaps.

## What I fixed by hand

* `IsoTolerance.zone` → `OneOf("H", "h", "JS", "js")`.
* `Dimension.nominal` → `Positive().LessThanOrEqualTo(500 / 25.4)` — the 500 mm cap depends on `unit`,
  and a per-parameter base cannot see another parameter, so the whole mm range above 19.7 is lost.
* `Fit`: hole/shaft zones via `WithTolerance(t => t.WithZone(z => z.Except(...)))` — my first try,
  `z.OneOf("H", "JS")`, was refused at construction (see F4). Same nominal: **edited `Generate()`**.
* `DrawingScale`: ISO 5455 pools, and **edited `Generate()`** for "1 on one side".
* `DrawingReference.number` / `PartNumber.value` → `Any.StringMatching(...)`, which forced changing the
  base's return type **and** the `With…(Func<AnyString, AnyString>)` signature to `AnyPattern`.
* `revision` → `Any.Char().Alpha().InUpperCase().Except('I', 'O', 'Q', 'S', 'X', 'Z')`.
* `Drawing.issuedOn` → `AfterOrEqualTo(Drawing.EarliestIssue)`.
* `BomItem.children` — the emitted recipe overflowed the stack (F1); replaced by one level of leaves plus
  a hand-written `WithDepth(int)` in `AnyBomItem.Custom.cs`.
* `AnyProjectionMethod` left as emitted to show F2; `AnyLengthUnit.cs` (same defect) deleted.

## Findings

* **F1 (high, cli)** — `any generate BomItem` emits `Any.ListOf(new AnyBomItem())` inside
  `AnyValidChildren()`, which the public constructor calls: following the TODO's own advice ("keep the
  recipe … delete the line") gives infinite recursion on `new AnyBomItem()`. Observed: `Stack overflow.`
  and exit 134 from the test host; `dotnet test` only says *"A test session start event was received
  without a corresponding test session end … Zero tests ran"*. The recap blames "unread guards", not
  the self-reference. No `Bug_` test: a stack overflow kills every test in the run.
* **F2 (high, cli)** — `any generate <Enum>` emits `return new ProjectionMethod();`: always the default
  value, `0`, which is not even a declared member when the enum has no zero. Recap: "no constructor
  parameters to infer", exit 0. `Bug_Scaffolded_AnyProjectionMethod_draws_an_undeclared_enum_value`.
* **F3 (medium, cli)** — a nullable reference parameter (`IsoTolerance? tolerance`) is scaffolded as
  `IAny<IsoTolerance>` with `WithTolerance(IsoTolerance value)`: `WithTolerance(null)` is ambiguous with
  the lambda overload, and an untoleranced dimension needs `(IsoTolerance)null!`.
* **F4 (medium, friction)** — narrowing a `OneOf` base with a smaller `OneOf` (the natural refinement
  lambda) throws `ConflictingAnyConstraintException: Cannot apply OneOf("H", "JS") because OneOf("H",
  "h", "JS", "js") is already defined.` although values remain; the exception's documented meaning is
  "the constraints admit no value". JD034 explains it is deliberate, but the CLI page's "a lambda narrows
  a leaf" invites it, and the analyzer cannot see inside the lambda. `Except` works.
* **F5 (medium, friction)** — cross-parameter invariants (fit = same size, scale = 1 on one side, nominal
  cap depends on unit) have no home in the scaffolded shape: the only route is editing `Generate()`,
  which then silently ignores a pinned `WithDenominator(...)` etc.
* **F6 (medium, friction)** — replacing a `string` base by `Any.StringMatching` (the obvious move for a
  regex guard) does not compile until the `With…` lambda overload signature is hand-edited too.
* **F7 (medium, doc/errors)** — outside any scope, an `AnyGenerationException` says "reproduce this run
  with `Any.Reproducibly(S, ...)`", but `S` is the flow's lazily picked seed: two failures at different
  points report the same `S`, and replaying `S` restarts at the first draw
  (`Probes.Outside_a_scope_the_reported_seed_names_the_flow_not_the_failing_draw`). Diagnosed by reading
  `AmbientRandomSource.cs` after observing it.
* **F8 (low, doc)** — the cardinality gate fires at `Generate()`, not at declaration
  (`ListOf(Enum<FitKind>()).WithCount(4).Distinct()` returns a generator); JD016's page says "Both throw at
  declaration time". The `SetOf` message ("The generator can produce only 3 distinct value(s), not enough
  for the 4 elements it must still supply.") names neither call.
* **F9 (low, analyzer)** — no build-time diagnostic for `Any.Double().GreaterThan(10).LessThan(5)`,
  `Any.DateOnly().After(DateOnly.MaxValue)`, `Any.StringMatching(@"^P\d{6}$").OneOf("X")`, for an
  `anyNominal` flowing into `Assert.Equal`, nor JD028 on `Distinct()` over a scaffolded generator of a type
  without `Equals` (`DrawingScale`). JD023 is documented as integer-only.
* **F10 (low, friction)** — double bound conflicts are worded in next-representable terms: "GreaterThan(10)
  already requires values greater than or equal to 10.000000000000002", "Positive() … ≥ 5E-324".
* **F11 (low, doc)** — `PublicAPI.Shipped.txt` is empty; the surface lives in `PublicAPI.Unshipped.txt`.
  `[Reproducible]` needs `using JustDummies.Xunit;`, which no sample shows. `\uXXXX`/`\xXX` escapes work
  but are missing from the supported-constructs table. `any generate Bom` says "nothing close to it"
  while `BomItem` exists.

Clear, correct refusals (not bugs): lookahead, `[A-Z-[IOQSXZ]]` subtraction, `\p{Lu}`, `\b`, atomic
groups, `IgnorePatternWhitespace`, `WithScale(29)`, counts > 1 000 000, `After(DateOnly.MaxValue)`.
Class subtraction is what a drawing office would write for revision letters; the explicit ranges
`[A-HJ-NPRT-WY]` are an acceptable workaround.

## Tests

`dotnet test --project Tests`: **58 total, 57 passed, 1 failed** (the intended
`Bug_Scaffolded_AnyProjectionMethod_draws_an_undeclared_enum_value`).

## Fluency: 3 / 5

The core DSL is a pleasure for scalars and patterns: `StringMatching` for drawing/part numbers,
`Between`, `Except`, seeded replay (a whole BOM tree replays from `UseSeed(7)`) and the
`[Reproducible]` seed report all worked first time, and the refusals are clear. What costs a point and
a half is the scaffolded layer in this domain: a recursive aggregate crashes the host, an enum is
scaffolded to a constant, every cross-field rule forces hand-editing `Generate()`, and the `With…`
refinement lambda trips on `OneOf`. A BOM tree needed a hand-written depth-bounded generator; the
library offers nothing for recursive shapes (reasonable under "the name is the scope", but the CLI
should then refuse or leave a TODO rather than emit a self-recursive recipe).

## After #331

The CLI fixes merged in [#331](https://github.com/Reefact/just-dummies/pull/331) were re-checked on this domain by re-scaffolding with `--dry-run`. `any generate ProjectionMethod` is now refused, naming `Any.Enum<ProjectionMethod>()` (#298). `BomItem` no longer recurses: `children  IReadOnlyList<BomItem>  —  TODO, unread guards, self-referencing` (#299). `AnyProjectionMethod.cs`, its entry point and its `Bug_` test are removed.
