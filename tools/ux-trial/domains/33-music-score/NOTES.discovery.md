# Discovery notes — 33 Music Score
Agent: the same model and effort as every session of this trial. Library: JustDummies 1.0.0-uxtrial.1 (packed from main).

## What I did
Pages read, in order: `docs/PROJECT-README.md`, `docs/README.md` (index), `guides/getting-started.en.md`,
`guides/core-concepts.en.md`, `packages/justdummies-cli.en.md` (the `any` page, nearly whole), `packages/justdummies-xunit.en.md`
(first half), `guides/composition.en.md`, `generators/README.md`, and grep-level look-ups in `generators/strings.en.md`,
`generators/enums-and-choices.en.md`, `generators/collections.en.md`. I did not need `errors-and-conflicts`, `reproducibility` or
the analyzer pages.

Commands run (`any` = the full path of the tool):
1. `any generate Duration Pitch TimeSignature Tempo Voice VoiceEntry Measure Staff Score PerformedEvent --entry-point any`
   -> first attempt refused ("this project has not been restored"), `dotnet restore`, then the same command: 9 of 10 types written,
   `Voice` refused (`AnyVoiceEntry` declared twice: the entry point of `Voice` and the generator of `VoiceEntry`).
2. `any generate Voice` (no entry point), so `Voice` is reached with `new AnyVoice()`.
3. `dotnet test --project Tests.Discovery` (after adding a `global.json` selecting the Microsoft.Testing.Platform runner, see friction 1).

Generators completed by hand (all in `Tests.Discovery/`):
- `AnyDuration`: numerator 0..64, denominator 1..`Duration.MaxDenominator` (sentinels deleted).
- `AnyPitch`: step `Any.Char().OneOf('A'..'G')`; `Generate()` lowers octave 9 to 8 when the draw would exceed MIDI 127 (cross-field rule).
- `AnyTimeSignature`: beat value `OneOf(1,2,4,8,16,32,64)`.
- `AnyTempo`: beat unit `new AnyDuration().WithNumerator(1)` (a unit fraction, positive and <= a whole note).
- `AnyVoiceEntry`: positive duration; `Generate()` only keeps the tie on a note (a rest cannot be tied).
- `AnyVoice`: entry durations restricted to power-of-two denominators (see friction 6); `AnyVoice.Custom.cs` adds `FillingMeasure(number, timeSignature)`.
- `AnyMeasure`: repeat count `Between(0, 8).Except(1)`; the scaffolded `voices` recipe replaced by "draw voices that fill the drawn meter"
  (`AnyMeasure.Custom.cs`), pin `WithVoices` kept, refining overload removed.
- `AnyStaff`: name `Alpha().WithLengthBetween(1, 40)` (no surrounding blank), measures renumbered 1..n (`AnyStaff.Custom.cs`).
- `AnyScore`: `staves` replaced by a hand-written draw that shares measure count, meters and repeat barlines across staves, with
  non-nested closed repeats (`AnyScore.Custom.cs`); pin `WithStaves` kept.
- `AnyPerformedEvent`: length made positive.

Tests written (136 methods, 219 cases, 2 skipped with `Skip = "domain defect: ..."`), files `*Tests.cs`:
`DurationTests` (normalisation, 1/1024 limit, `+`, `* int`, comparison, `DividedBy`), `PitchTests` (MIDI table, G##9 refused,
step/alter/octave refusals, enharmonics, notation), `TimeSignatureTests`, `TempoTests` (seconds conversion, limits, NaN/Infinity),
`VoiceEntryTests`, `VoiceTests`, `MeasureTests` (every voice must fill the meter, 6/8 vs 3/4 vs 4/4, duplicate voices, repeat counts),
`StaffTests`, `ScoreConstructionTests` (all aggregate refusals), `ScorePlayedOrderTests` (8 repeat shapes, played length and seconds),
`ScoreInterpretTests` (onsets, seconds, rests, six dynamics, ties in measure / across barline / chain / across repeat, four tie refusals,
voices, staves, ordering, absent voices, repeats expanded), `PerformedEventTests`, `GeneratorSmokeTests` (200 draws of every generator).
`[assembly: Reproducible]` makes every test reproducible.

Result: green (217 passed, 2 skipped), then 12 further runs of the whole suite (fresh seeds each time), all green.

## Frictions and surprises, in order
1. **`dotnet test --project Tests.Discovery` does not work out of the box on the .NET 10 SDK.**
   `MSBUILD : error MSB1001: Unknown switch ... --project`, and `dotnet test Tests.Discovery` gives
   `Testing with VSTest target is no longer supported by Microsoft.Testing.Platform on .NET 10 SDK and later ... opt-in to the new dotnet test experience`.
   Neither the project README, the getting-started page nor the xunit page mentions it (the README only says "pinned in global.json" for the repository).
   I added `domains/33-music-score/global.json` with `{ "test": { "runner": "Microsoft.Testing.Platform" } }`. A newcomer on SDK 10 hits a wall on minute five;
   the xunit adapter page should say which `dotnet test` form to use.
2. **The docs' links are dead in the shipped doc set.** `PROJECT-README.md` points to `doc/handwritten/for-users/guides/getting-started.en.md`,
   `README.md` to `../for-maintainers/...` and `../../../CONTRIBUTING.md`, none of which exist under `docs/`. I found the guides by listing the folder.
3. **`any generate` refuses an unrestored project**, with a clear message ("this project has not been restored, so its package references cannot be read.
   Restore it, then run the command again: dotnet restore"). Fine, but the getting-started path never says "restore first"; clear enough once met.
4. **`--entry-point any` cannot be used for `Voice` and `VoiceEntry` in one run** (documented in the CLI page, but I only found it by hitting it):
   `AnyVoiceEntry in AnyVoice.Entry.cs ... AnyVoiceEntry in AnyVoiceEntry.cs`. The message is good and names the way out. A music domain naturally has
   `Voice`/`VoiceEntry`, so this is not exotic; I generated `Voice` without an entry point and use `new AnyVoice()` (inconsistent call sites:
   `Any.Measure()` next to `new AnyVoice()`). I would have liked the tool to pick `static:` for the clashing one on its own, or to say in the refusal that
   `any generate Voice` alone (no entry point) is the simplest fix.
5. **The `to verify` recap is valuable but noisy for value objects with simple closed rules.** 10 types gave 15 `to verify` rows. Most were real
   (power-of-two beat value, step in "CDEFGAB", cross-field MIDI, voice fills the meter), a few were a guard on a *derived* property of a nested value
   (`beatUnit.IsZero`, `duration.IsZero`, `length.IsZero`): the generator for that nested type (`new AnyDuration()`) cannot know it, and the fix
   is to refine the nested builder (`new AnyDuration().WithNumerator(n => n.Positive())`). The refining lambda made that a one-liner; I liked it.
   But for `PerformedEvent.length` the sentinel had to be removed *and* the base changed - deleting the two lines alone would have left a
   generator that draws a zero length on some runs (the design says so, and it was fine).
6. **A scaffolded `Any.Voice()` fails at random, with no `to verify` on it.** Smoke test (200 draws) failed on the first run:
   `System.ArgumentOutOfRangeException : A duration finer than 1/1024 of a whole note is not notatable. (Parameter 'denominator') Actual value was 745914`,
   from `Duration.operator +` inside `Voice..ctor`, seed `-375931864`. The scaffold for `Voice` is `Any.ListOf(new AnyVoiceEntry()).NonEmpty()` tagged only `guard`,
   which is accurate: the throw comes from an operator of another type, not a guard of `Voice`. I did not replay the seed (the failure was obvious from the stack trace). It is a domain issue (see domain findings), and I worked around it in the generator by restricting entry durations to powers of two.
7. **A scaffolded `Any.Measure()` / `Any.Staff()` is valid alone and wrong in a score.** `AnyMeasure` draws `repeatStart` and `repeatEndTimes` freely, so a
   measure used to fill a `Score` carries random repeat barlines (`The_played_length_counts_every_pass...` first returned 23/4 instead of 11/4 because the first and
   third measures were repeating). This is my test's mistake (the repeats were what the test was about, so they had to be pinned), but the generator looks "valid" and
   the tool gave no hint, since the cross-staff/aggregate rule lives in `Score`, not in `Measure`. I wrote `Scores.RepeatBar(...)` that pins both repeat fields.
8. **`.As(...)`-style derived draws are fine, but "voices must fill the drawn meter" had no scaffold path.** The docs show three forms for two *parameters* tied
   together ("When a guard ties two parameters"); mine was a list element rule depending on another parameter. I replaced the scaffolded recipe by a null field and a
   `DrawVoicesFilling(timeSignature)` in a `.Custom.cs` partial, drawing with ambient `Any.*().Generate()` inside `Generate()`. It worked (the 12 repeated runs stayed green; I never replayed a seed on it),
   but I was not sure it was an endorsed move; I could not tell from the docs whether `Generate()` inside a generator is reproducible under `Any.WithSeed(...)` (the composition page warns about it for `.As`; I only used `[Reproducible]`).
9. **Analyzer diagnostics:** none fired on my code (build with 0 warnings). I did not see any `JDxxx` at Info level; the getting-started page does not say how to
    see them. Not a problem for the trial.

## What worked well
- The getting-started page's "before/after" and the Arrange/Act/Assert + `anyXxx` naming convention are easy to apply; deciding what stays a literal
  (the durations, onsets, seconds the test is about) versus a dummy (staff names, dynamics, pitches whose value never reaches the assertion) became natural.
- `any generate` on ten interdependent types, in one command, gave compiling-after-edit generators with `With…` pins and refining lambdas; the sentinel
  `TODO_verify_the_generator_for_x` made the build name each gap at its line. The "AnyX" composition (`new AnyDuration()` inside `AnyTempo`) kept one
  address per invariant.
- `[assembly: Reproducible]` and the seed report on failure: when the smoke test failed, the output printed
  `[JustDummies] These arbitrary values were seeded with -375931864. Reproduce this run with [JustDummies.Xunit.Reproducible(Seed = -375931864)].`
- The draw found a real domain issue (non-closure of `Duration` addition) that no literal I would have written would have found.
- Refusal messages of the tool are precise (restore, name clash with both files).

## Fluency: 4 / 5
The path from README to a green 219-case suite was straightforward once `dotnet test --project` worked; the generator workflow (scaffold, delete sentinels,
refine nested builders, hand-write what ties parameters) is learnable and the docs on it are strong. Points lost for the `dotnet test` / MTP wall on SDK 10 that
the docs do not mention, the dead documentation links, and the lack of any guidance on aggregate-level invariants (a measure that fits a meter, staves that share structure)
where I had to design the generators by hand without a documented pattern.

## Domain findings
- **Duration addition is not closed.** `new Duration(1, 100) + new Duration(1, 101)` throws `ArgumentOutOfRangeException` ("A duration finer than 1/1024 ... not notatable",
  parameter `denominator`, actual value 10100), although both operands are valid. Consequences: `Voice` with entries 1/100 and 1/101 cannot be built (the exception carries the
  parameter name `denominator` of a constructor the caller never called), and `PerformedEvent.End` (`Onset + Length`) can throw for a valid event. Tests kept and skipped:
  `DurationTests.The_sum_of_two_notatable_durations_is_a_duration`, `VoiceTests.A_voice_of_two_valid_entries_has_a_length`.
- **Observed, not in MODEL.md:** a tied note takes the dynamic of the measure where the tie starts (not tested). And tying C#4 to Db4 (same MIDI number) is refused as "not the same pitch"
  (tested as written: `An_enharmonic_tie_is_not_the_same_pitch`).
