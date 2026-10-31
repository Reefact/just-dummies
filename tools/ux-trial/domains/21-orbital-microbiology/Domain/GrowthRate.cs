namespace OrbitalMicrobiology.Domain;

/// <summary>A specific growth rate, in reciprocal hours (exponential model: N(t) = N0 · e^(μt)).</summary>
public sealed class GrowthRate : IEquatable<GrowthRate>
{
    public const double MaxPerHour = 5.0;

    public GrowthRate(double perHour)
    {
        if (!double.IsFinite(perHour) || perHour < 0 || perHour > MaxPerHour)
        {
            throw new ArgumentOutOfRangeException(nameof(perHour), perHour, $"A growth rate lies in [0, {MaxPerHour}] per hour.");
        }

        PerHour = perHour;
    }

    public static GrowthRate Zero { get; } = new(0);

    public double PerHour { get; }

    /// <summary>The doubling time, ln 2 / μ. Refused for a zero rate: such a population never doubles.</summary>
    public double GenerationTimeHours =>
        PerHour > 0
            ? Math.Log(2) / PerHour
            : throw new InvalidOperationException("A zero growth rate has no generation time.");

    /// <summary>How many generations (doublings) elapse in the given time at this rate.</summary>
    public double GenerationsIn(double hours)
    {
        if (!double.IsFinite(hours) || hours < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(hours), hours, "A duration is finite and non-negative.");
        }

        return PerHour * hours / Math.Log(2);
    }

    public bool Equals(GrowthRate? other) => other is not null && PerHour.Equals(other.PerHour);

    public override bool Equals(object? obj) => Equals(obj as GrowthRate);

    public override int GetHashCode() => PerHour.GetHashCode();

    public override string ToString() => $"{PerHour:0.###} h⁻¹";
}
