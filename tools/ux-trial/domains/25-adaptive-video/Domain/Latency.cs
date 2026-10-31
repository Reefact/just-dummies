namespace AdaptiveVideo.Domain;

/// <summary>Round-trip time to the CDN edge, paid once per segment request.</summary>
public sealed class Latency
{
    public const int MaxMilliseconds = 5_000;

    public Latency(int roundTripMilliseconds)
    {
        if (roundTripMilliseconds < 0 || roundTripMilliseconds > MaxMilliseconds)
        {
            throw new ArgumentOutOfRangeException(nameof(roundTripMilliseconds), roundTripMilliseconds,
                $"Round-trip time must be between 0 and {MaxMilliseconds} ms.");
        }

        RoundTripMilliseconds = roundTripMilliseconds;
    }

    public int RoundTripMilliseconds { get; }

    public double Seconds => RoundTripMilliseconds / 1000.0;
}
