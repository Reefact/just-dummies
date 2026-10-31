namespace MusicScore.Domain;

/// <summary>A written pitch in scientific notation: step letter, alteration in semitones, octave (C4 is middle C).</summary>
public sealed class Pitch : IEquatable<Pitch>
{
    private const string Steps = "CDEFGAB";
    private static readonly int[] SemitonesFromC = [0, 2, 4, 5, 7, 9, 11];

    public Pitch(char step, int alter, int octave)
    {
        if (!Steps.Contains(step, StringComparison.Ordinal))
        {
            throw new ArgumentOutOfRangeException(nameof(step), step, "The step must be an upper-case letter from A to G.");
        }

        if (alter is < -2 or > 2)
        {
            throw new ArgumentOutOfRangeException(nameof(alter), alter, "The alteration must lie between a double flat (-2) and a double sharp (+2).");
        }

        if (octave is < 0 or > 9)
        {
            throw new ArgumentOutOfRangeException(nameof(octave), octave, "The octave must lie between 0 and 9.");
        }

        int midi = (12 * (octave + 1)) + SemitonesFromC[Steps.IndexOf(step, StringComparison.Ordinal)] + alter;
        if (midi is < 0 or > 127)
        {
            throw new ArgumentException($"Step {step} altered by {alter} in octave {octave} falls outside the MIDI range 0-127.", nameof(alter));
        }

        Step = step;
        Alter = alter;
        Octave = octave;
        MidiNumber = midi;
    }

    public char Step { get; }

    public int Alter { get; }

    public int Octave { get; }

    public int MidiNumber { get; }

    public bool Equals(Pitch? other) => other is not null && Step == other.Step && Alter == other.Alter && Octave == other.Octave;

    public override bool Equals(object? obj) => Equals(obj as Pitch);

    public override int GetHashCode() => HashCode.Combine(Step, Alter, Octave);

    public override string ToString()
    {
        string accidental = Alter switch
        {
            -2 => "bb",
            -1 => "b",
            1 => "#",
            2 => "##",
            _ => string.Empty,
        };
        return $"{Step}{accidental}{Octave}";
    }
}
