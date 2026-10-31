namespace MusicScore.Domain;

/// <summary>A metronome marking: so many beat units per minute (e.g. a dotted quarter = 60).</summary>
public sealed class Tempo
{
    public Tempo(double beatsPerMinute, Duration beatUnit)
    {
        if (double.IsNaN(beatsPerMinute) || beatsPerMinute < 20 || beatsPerMinute > 400)
        {
            throw new ArgumentOutOfRangeException(nameof(beatsPerMinute), beatsPerMinute, "A tempo must lie between 20 and 400 beats per minute.");
        }

        ArgumentNullException.ThrowIfNull(beatUnit);
        if (beatUnit.IsZero || beatUnit > new Duration(1, 1))
        {
            throw new ArgumentOutOfRangeException(nameof(beatUnit), beatUnit, "The beat unit must be a positive duration of at most a whole note.");
        }

        BeatsPerMinute = beatsPerMinute;
        BeatUnit = beatUnit;
    }

    public double BeatsPerMinute { get; }

    public Duration BeatUnit { get; }

    /// <summary>Converts an exact musical duration into seconds at this tempo.</summary>
    public double SecondsFor(Duration duration)
    {
        ArgumentNullException.ThrowIfNull(duration);
        return duration.DividedBy(BeatUnit) * 60.0 / BeatsPerMinute;
    }
}
