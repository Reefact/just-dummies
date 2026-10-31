# Discovery notes: 33-music-score

## What I did
- Read MODEL.md and the domain, then getting-started, the CLI page, composition, and the
  numbers / enums-and-choices / collections reference pages and the xUnit adapter page.
- Created `Tests.Discovery`, then ran `any generate Duration Pitch TimeSignature Tempo VoiceEntry --entry-point any`
  and `any generate Voice Measure Staff Score --entry-point any` (exit 0 for both).
- Resolved 22 `to verify` TODOs by hand. I derived voices from the drawn meter in
  `AnyMeasure.Generate()` and numbered measures 1..n in `AnyStaff.Generate()`, following "Derive one draw from
  another inside Generate()". A dummy score holds a single staff that plays straight through.
- Wrote 34 tests (value objects, measure/staff/score consistency, played order with repeats,
  played length and seconds, interpretation: rests, ties across barlines, repeats, velocity,
  staff ordering, onsets in seconds). `[assembly: Reproducible]`. All 34 pass, and 9 runs in a row passed.

## Frictions and surprises, in order
1. **Relational patterns are not read.** `if (alter is < -2 or > 2)`, `if (beats is < 1 or > 32)`,
   `if (number is < 1 or > MaxVoices)` all came back `Any.Int32()` + `to verify, unread guards`,
   while `if (numerator < 0)` became `GreaterThanOrEqualTo(0)`. This is the idiomatic C# 9+ guard.
   Half my TODOs were plain ranges I had to retype as `Between(...)`.
2. **Entry-point name collision (bug).** The build failed with
   `AnyVoice.Entry.cs(10,21): error CS0260: Missing partial modifier on declaration of type 'AnyVoiceEntry'`
   (and `CS0722: 'AnyVoiceEntry': static types cannot be used as return types`, ...). The entry class
   for `Voice` is named `AnyVoiceEntry`, which is also the generator for my domain type `VoiceEntry`.
   `any` wrote both in the same run and said `✓` twice. A `--dry-run` re-run with
   `AnyVoiceEntry` already compiled did not warn either. The errors point at the generator, not the cause.
   I renamed the class by hand to `AnyVoiceEntryPoint`, so `--force` will bring the clash back.
3. **Parameter marked only because it appears in a message.** `Measure.number` was flagged
   `to verify` at `foreach (Voice voice in voices)`: that throw depends on the meter and the voices.
   `number` only appears in the interpolated message. Noise that costs a read.
4. `entries.Any(entry => entry is null)` was flagged even though `Any.ListOf(new AnyVoiceEntry())`
   can never hold a null. In `Voice`, `entries.Count == 0` alone was read (`NonEmpty()`), but in
   `Score`, the same check written as `staves.Count == 0 || staves.Any(... is null)` was not.
5. Removing 22 TODO blocks (6 comment lines + `_ = TODO_...` each) is mechanical. I scripted it.
   That is by design, but nothing in the docs says how to accept a recipe in bulk.
6. **Cross-parameter rules dominate this domain** (voices fill the meter, measures numbered 1..n,
   staves aligned, repeats closed/not nested). The CLI page shows how to compute one draw from
   another, but in the scaffolded shape a derived parameter does not fit: I had to make the field
   nullable, pass `null` from the default ctor, and delete the refining `With…(Func<AnyList<…>>)` overload.
   No doc example shows a *collection* derived from another parameter.
7. The default `AnyMeasure` draws repeat barlines, which is correct for a measure but invalid in most scores.
   Almost every test therefore writes `.WithRepeatStart(false).WithRepeatEndTimes(0)`, so I wrote a `Bars` helper.
8. `Pitch` has a cross-field MIDI rule (G#9+ refused). The honest fix by construction was to
   narrow octaves to 0..8 in `AnyValidOctave()`, with a comment. Docs led me there quickly.
9. Distinct pitches for "tie onto a different pitch": `Any.ListOf(Any.Pitch()).WithCount(2).Distinct()`
   worked on a custom `IAny<Pitch>` with no friction (found in collections.en.md).
10. No `DifferentFrom` on a scaffolded generator, so the line above was the only way I found to
    get "another pitch".

## What worked well
- `ArgumentException.ThrowIfNullOrWhiteSpace(title)` was read as `NotBlank()`, `Enum.IsDefined` as
  `Any.Enum<Dynamic>()`, and `entries.Count == 0` as `NonEmpty()`.
- The recap's per-parameter "unread guard at File.cs, line N: <code>" made each TODO quick to judge.
- `With…(value)` pins and `With…(n => n.Positive())` refinements read well in tests;
  `Any.Measure().WithNumber(1).WithTimeSignature(threeFour)…` says exactly what the test chose.
- `Any.Char().OneOf("CDEFGAB")`, `Any.Int32().OneOf(1, 2, 4, …)` were exactly what I looked for.
- Zero analyzer noise in the final project. The build stayed clean once TODOs were resolved.

## Fluency: 3 / 5
The library surface was easy and the docs answered every question I had. The scaffolder gave
a correct skeleton, but in this domain it inferred little: relational patterns were unread, it
produced a name collision that broke the build, and the shape of the generated file resists the
cross-parameter derivations that make up most of the domain. That cost more time than the tests.
