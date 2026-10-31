namespace AdaptiveVideo.Domain;

/// <summary>An encoding or network rate, in kilobits per second.</summary>
public sealed class Bitrate : IEquatable<Bitrate>, IComparable<Bitrate>
{
    public const int MinKbps = 50;
    public const int MaxKbps = 100_000;

    public Bitrate(int kbps)
    {
        if (kbps < MinKbps || kbps > MaxKbps)
        {
            throw new ArgumentOutOfRangeException(nameof(kbps), kbps, $"A bitrate must be between {MinKbps} and {MaxKbps} kbps.");
        }

        Kbps = kbps;
    }

    public int Kbps { get; }

    /// <summary>Seconds needed to download <paramref name="mediaSeconds"/> of media encoded at this rate over <paramref name="throughputKbps"/>.</summary>
    public double DownloadSeconds(double mediaSeconds, double throughputKbps)
    {
        if (mediaSeconds <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(mediaSeconds), mediaSeconds, "Media duration must be positive.");
        }

        if (throughputKbps <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(throughputKbps), throughputKbps, "Throughput must be positive.");
        }

        return Kbps * mediaSeconds / throughputKbps;
    }

    public int CompareTo(Bitrate? other) => other is null ? 1 : Kbps.CompareTo(other.Kbps);

    public bool Equals(Bitrate? other) => other is not null && Kbps == other.Kbps;

    public override bool Equals(object? obj) => Equals(obj as Bitrate);

    public override int GetHashCode() => Kbps;

    public override string ToString() => $"{Kbps} kbps";
}
