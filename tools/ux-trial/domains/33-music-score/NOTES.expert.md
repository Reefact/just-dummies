# Music score — expert crafter notes

## What I did

- Scaffolded all nine types in one run (`any.json`: output `./Arbitrary`, entry point `any`), then
  wrote every `AnyValid…()` base by hand: notatable durations, steps `OneOf("CDEFGAB")`, octave
  0..8 (honours the MIDI cross-field rule by construction), meters with power-of-two beat values,
  beat units `1|3 / 4|8|16` (never above a whole note), untied entries and no repeat barlines by
  default (a tie or a repeat is a choice the test makes).
- The domain's core invariant — *every voice fills exactly its meter* — lives between parameters.
  `AnyVoice.Filling(length)` cuts the length on a grid with a `SetOf` of distinct cut points, so
  the pieces sum to the length by construction. `AnyMeasure` now draws its meter first and derives
  its voices from it (field retyped to `Func<TimeSignature, IAny<…>>`, voices `Distinct` by number
  through a comparer).
- `AnyStaff.WithMeasures(params Func<AnyMeasure, AnyMeasure>[])` numbers measures 1..n; `AnyScore`
  defaults to one staff, so `Any.Score().WithMeasures(AnyBar, m => m.ClosingRepeat(2), AnyBar)` reads
  as the barlines the test is about while every bar's content stays arbitrary.
- 35 test methods (42 cases): exact time, measure/staff/score invariants, played order with repeats,
  interpretation (rests, ties across barlines and repeats, ordering, velocity, seconds).
  `[assembly: Reproducible]`. All pass; 60 consecutive runs under fresh seeds were green.

## Frictions and surprises, in order

1. **Relational-pattern guards are not read.** `if (alter is < -2 or > 2) throw` marks `alter`
   `to verify` with `Any.Int32()`, while `if (numerator < 0)` becomes `GreaterThanOrEqualTo(0)`.
   Most of this domain guards with `is < a or > b`, so 15 of 21 parameters came back `to verify`.
2. **`Count == 0 || Any(null)` loses the `NonEmpty()`.** `Voice` (two separate ifs) got
   `.NonEmpty()`; `Measure`, `Staff`, `Score` (one `||` guard) got a bare `Any.ListOf(...)`.
3. **`Measure.number` marked `to verify` by the voice-length loop**, which only uses `number` in
   the exception message. Harmless but noisy.
4. **Bug — entry-point name clash.** Scaffolding `Voice` and `VoiceEntry` with `--entry-point any`
   writes `public static class AnyVoiceEntry` (entry point of `Voice`) beside the generator
   `public sealed partial class AnyVoiceEntry`. Build: `error CS0260: Missing partial modifier on
   declaration of type 'AnyVoiceEntry'; another partial declaration of this type exists` followed by
   a cascade of CS0708/CS0710/CS0722. The tool scaffolded both in the same run and said nothing;
   `any generate Voice --dry-run --force` still emits the clash. Worked around by renaming the
   entry class to `AnyVoiceEntryPoint`.
5. **No dependent draw.** A voice depends on the meter just drawn; there is no bind/`SelectMany`,
   nor an `Any.From(() => …)`. I wrote an 8-line `Drawn<T> : IAny<T>` and retyped the scaffold's
   voices field. The CLI page shows "derive inside `Generate()`", which works but is erased by
   `--force` and leaves the `With…(Func<…>)` refinement unable to see the meter.
6. **Collection bases of composed types cannot be refined usefully.** `AnyList<Measure>` and
   `AnyList<Staff>` refinements would break numbering / cross-staff agreement, so I deleted them
   (as the docs advise for `IAny` bases) and added shape-based `WithMeasures` overloads.
7. `using static Notation` + a member named `Half` → `CS0229: Ambiguity between 'Notation.Half' and
   'Half'` (`System.Half`). Mine, not the library's — renamed to `HalfNote`.
8. JD030 (as notes, visible only via SARIF — not in `dotnet build` output) on score titles: fair;
   moved titles to one recipe with a declared length.

## What worked well

- `TODO_verify_…` lines that stop the build, each naming the exact guard and line: a perfect work
  list. Refining lambdas compose with the base (`WithBeats(b => b.Between(2, 32))` intersects
  `Between(1, 32)`; `OneOf` + a later narrowing on a pool works).
- `Any.ListOf(Any.Pitch()).WithCount(2).Distinct()` and `Any.ListOf(Any.TimeSignature())…Distinct()`
  read exactly like the scenario ("two different pitches"); `Distinct(comparer)` gave distinct staff
  names and voice numbers on entity types without fuss.
- `Func<AnyMeasure, AnyMeasure>` shapes as the reusable vocabulary (`AnyBar`, `ClosingRepeat(2)`):
  played-order tests are one line of arrange where only barlines are literal.
- The `[Reproducible]` seed line appeared in the failing test's output on the first failure.
- The partial-class pattern (`*.Custom.cs`) kept every hand-written member out of `--force`'s way.

## What I would design differently

- Read C# relational/`or` patterns as ranges, and split `||` guards into their clauses.
- Detect generator/entry-point type-name clashes across a run (and against the compilation).
- A first-class dependent draw (`anyMeter.Then(meter => AnyVoicesFilling(meter))`), and an option to
  scaffold a parameter as dependent on another, so the scaffold's shape survives cross-field rules.
- Surface Info-level JD diagnostics in the default build output, or say on the analyzers page how to
  see them.

## Fluency: 4 / 5

The library surface did everything a real test needed, and the docs predicted each move (the
`AnyValid…` base, the partial file, deleting the refining overload). One point off for the
entry-point clash that broke the build, the unread `is < a or > b` guards that turned most of the
scaffold into hand work, and the missing dependent-draw combinator in a domain built on one.
