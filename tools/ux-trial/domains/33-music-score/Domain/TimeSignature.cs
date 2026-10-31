namespace MusicScore.Domain;

/// <summary>A meter such as 3/4 or 6/8.</summary>
public sealed class TimeSignature : IEquatable<TimeSignature>
{
    public TimeSignature(int beats, int beatValue)
    {
        if (beats is < 1 or > 32)
        {
            throw new ArgumentOutOfRangeException(nameof(beats), beats, "A measure holds between 1 and 32 beats.");
        }

        if (beatValue is < 1 or > 64 || (beatValue & (beatValue - 1)) != 0)
        {
            throw new ArgumentOutOfRangeException(nameof(beatValue), beatValue, "The beat value must be a power of two from 1 to 64.");
        }

        Beats = beats;
        BeatValue = beatValue;
    }

    public int Beats { get; }

    public int BeatValue { get; }

    /// <summary>The exact length a full measure in this meter must fill.</summary>
    public Duration MeasureLength => new(Beats, BeatValue);

    public bool Equals(TimeSignature? other) => other is not null && Beats == other.Beats && BeatValue == other.BeatValue;

    public override bool Equals(object? obj) => Equals(obj as TimeSignature);

    public override int GetHashCode() => HashCode.Combine(Beats, BeatValue);

    public override string ToString() => $"{Beats}/{BeatValue}";
}
