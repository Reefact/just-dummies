namespace AgriTech.Domain;

/// <summary>Relative humidity in percent, 0 to 100 inclusive.</summary>
public sealed class RelativeHumidity : IEquatable<RelativeHumidity> {

    public RelativeHumidity(double percent) {
        if (percent < 0.0) {
            throw new ArgumentOutOfRangeException(nameof(percent), percent, "Humidity cannot be negative.");
        }
        if (percent > 100.0) {
            throw new ArgumentOutOfRangeException(nameof(percent), percent, "Humidity cannot exceed 100 %.");
        }
        Percent = percent;
    }

    public double Percent { get; }

    public bool IsBelow(double thresholdPercent) => Percent < thresholdPercent;

    public bool Equals(RelativeHumidity? other) => other is not null && Percent.Equals(other.Percent);

    public override bool Equals(object? obj) => Equals(obj as RelativeHumidity);

    public override int GetHashCode() => Percent.GetHashCode();

}
