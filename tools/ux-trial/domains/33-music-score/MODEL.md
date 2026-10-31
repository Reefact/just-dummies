# Music Score Interpretation — domain model

Namespace `MusicScore.Domain`. Turns a symbolic score into the notes to perform.

## Value objects
- `Duration(numerator, denominator)` — exact rational length in whole notes, normalised by GCD.
  Denominator > 0, numerator >= 0, normalised denominator <= 1024. `+`, `* int`, comparisons, `DividedBy`.
- `Pitch(step, alter, octave)` — step 'A'..'G', alter -2..+2, octave 0..9, and the resulting
  `MidiNumber` must lie in 0..127 (cross-field: G##9 is refused).
- `TimeSignature(beats, beatValue)` — beats 1..32, beatValue a power of two 1..64; `MeasureLength`.
- `Tempo(beatsPerMinute, beatUnit)` — 20..400 bpm, beat unit positive and <= a whole note;
  `SecondsFor(Duration)` converts exact time into seconds.
- `Dynamic` — enum pp..ff, mapped to a MIDI velocity (33..112) on performance.
- `VoiceEntry(duration, pitch?, tiedToNext)` — a note, or a rest when pitch is null; duration > 0;
  a rest cannot be tied.

## Entities and aggregate
- `Voice(number, entries)` — number 1..4, at least one entry, no null; `Length` is the exact sum.
- `Measure(number, timeSignature, dynamic, voices, repeatStart, repeatEndTimes)` — number >= 1,
  defined dynamic, at least one voice, distinct voice numbers, **every voice fills exactly the
  meter's length**; repeatEndTimes is 0 (no end repeat) or 2..8.
- `Staff(name, measures)` — trimmed name of 1..40 chars; measures numbered 1..n in order.
- `Score(title, tempo, staves)` (aggregate root) — non-blank title, distinct staff names, all staves
  have the same measure count, and per measure the same time signature and repeat barlines;
  repeats are not nested and every start repeat is closed.
- `PerformedEvent(staffName, voiceNumber, pitch, velocity, onset, length, startSeconds, durationSeconds)`
  — output: velocity 1..127, length > 0, seconds finite (start >= 0, duration > 0).

## Behaviour (on `Score`)
- `PlayedMeasureOrder()` — written order is not played order: an end repeat replays back to the
  latest start repeat, else to the measure after the previous end repeat, else to measure 1, as
  many times as `repeatEndTimes`.
- `PlayedLength()` / `PlayedSeconds()` — exact played length, repeats included, then in seconds.
- `Interpret()` — events in played order with exact rational onsets; rests are silent; tied notes
  merge into one event (also across barlines and repeats); voices and staves overlap in time.
  Ordered by onset, staff, voice. Throws `InvalidOperationException` when a tie does not resolve
  onto the same pitch in the same voice, or ends the piece.
