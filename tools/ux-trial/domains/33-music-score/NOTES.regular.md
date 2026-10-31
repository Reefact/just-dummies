# Music score — regular developer notes

## What I did

- Created `Tests.Regular`, restored, scaffolded all nine types in one call:
  `any generate Duration Pitch TimeSignature Tempo VoiceEntry Voice Measure Staff Score --entry-point static:Arbitrary`.
- Wrote the bases in each `AnyValid…()` factory, deleted the `TODO_verify` lines, then reworked the
  three generators whose invariants live between parameters (Measure, Staff, Score).
- Added `AnyMeasure.Custom.cs` (`WithoutRepeats()`, `WithSingleVoice(...)`) and a small `Notes` helper.
- 35 test methods / 40 cases (value objects, structural rules, played order, length/seconds,
  interpretation: rests, ties across barlines and repeats, velocity, staff/voice ordering, seconds).
  All green, and green on 15 consecutive runs (fresh seeds each time).

## Frictions, in the order I met them

1. **Relational patterns are not read.** Every `x is < 1 or > 32` guard (Pitch, TimeSignature, Voice,
   Measure.repeatEndTimes) came out `to verify, unread guards` with a bare `Any.Int32()`, while
   `numerator < 0`, `denominator <= 0` and `ThrowIfLessThan(number, 1)` were read. `is < a or > b` is
   the idiomatic C# range guard today. `double.IsNaN(bpm) || bpm < 20 || bpm > 400` was not read either.
   On this domain 16 of 25 rows needed a hand-written base.
   The user docs do not say which guard shapes are read (only the maintainer spec §9 does).
2. **A parameter quoted in an error message is marked as involved.** `Measure.number` is flagged
   `unread guard at Measure.cs, line 30: foreach (Voice voice in voices)` though that throw depends on
   voices and timeSignature only; `number` appears only in the interpolated message.
3. **Inconsistent reading of "non-empty".** `Voice.entries` got `.NonEmpty()` (separate `if (Count == 0)`),
   `Measure.voices` and `Staff.measures` did not (`Count == 0 || Any(x => x is null)` in one `if`).
4. **Cross-parameter invariants cost the most** (as documented — "derive one draw from another in
   Generate()"). Measure: voices must fill the drawn meter, so I made `_voices` nullable and derive one
   voice of beat-long notes in `Generate()`, deleting the `WithVoices(Func<…>)` overload. Staff: measures
   numbered 1..n, so the base is `ListOf(...).As(renumber)` — which kills the refining overload (factory
   is now an `IAny<…>`, as the doc warns), so I hand-wrote `WithEachMeasure(Func<AnyMeasure, AnyMeasure>)`.
   Each change is fine, but three of nine generators were rewritten rather than refined, and the next
   `--force` will wipe them (only the `.Custom.cs` survives).
5. **No `DifferentFrom` on a scaffolded composite.** To draw "another pitch" for a tie onto a wrong
   pitch I wrote `Arbitrary.Pitch().WithStep(s => s.Except(anyPitch.Step))` — works, but I had to know
   that Pitch equality is by spelling. `Arbitrary.Pitch().DifferentFrom(anyPitch)` is what I reached for.
6. **The honest Measure base fights the Score.** The Measure base draws random repeat barlines (valid for
   a measure) so every measure fed to a Score must pin them; hence my `WithoutRepeats()` and an `AnyBar(n)`
   helper used in ~25 places. Not a defect, but a scaffolded generator for an aggregate part is rarely
   usable as-is in the aggregate.
7. My own slip, not the library's: a static `Half` field clashed with `System.Half` (CS0229).

## What worked well

- One `any generate` call for nine types, with the composed `new AnyDuration()` / `new AnyPitch()` already
  wired; the recap lines naming the exact unread guard and line saved me reading constructors twice.
- The non-compiling `TODO_verify_…` lines are the right forcing function: I read every guard once.
- `static:Arbitrary` entry points read well next to `Any.Int32()` in arrange blocks.
- Pin vs lambda (`WithDuration(beat)` vs `WithNumerator(n => n.Positive())`) is clear and short.
- `Pitch?` parameter got a `Pitch?` pin, never-null base: exactly what I wanted.
- `[Reproducible]` on `[Fact]` and on `[Theory]` just works; the failure output prints
  `[Reproducible(Seed = 764834218)]` ready to paste. Zero analyzer noise on idiomatic code.

## Fluency: 4 / 5

Fast once the bases were written and tests then stayed short. One point off because the scaffolder
leaves the commonest C# range guard unread, so on a value-object-heavy domain most of the
"inference" is manual, and because cross-parameter aggregates (meter-filling voices, numbered measures)
mean rewriting `Generate()` and losing refinement overloads I then re-add by hand.
