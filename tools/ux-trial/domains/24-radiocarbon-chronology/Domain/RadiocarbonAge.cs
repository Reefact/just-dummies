namespace RadiocarbonChronology.Domain;

/// <summary>A conventional radiocarbon age, in years BP (before 1950), with its one-sigma laboratory uncertainty.</summary>
public sealed class RadiocarbonAge : IEquatable<RadiocarbonAge>
{
    public const int MaximumYearsBp = 55_000;
    public const int MaximumUncertainty = 5_000;

    public RadiocarbonAge(int yearsBp, int uncertainty)
    {
        if (yearsBp < 0 || yearsBp > MaximumYearsBp)
        {
            throw new ArgumentOutOfRangeException(nameof(yearsBp), yearsBp, $"A radiocarbon age must lie between 0 and {MaximumYearsBp} years BP.");
        }

        if (uncertainty < 1 || uncertainty > MaximumUncertainty)
        {
            throw new ArgumentOutOfRangeException(nameof(uncertainty), uncertainty, $"A radiocarbon uncertainty must lie between 1 and {MaximumUncertainty} years.");
        }

        YearsBp = yearsBp;
        Uncertainty = uncertainty;
    }

    public int YearsBp { get; }

    public int Uncertainty { get; }

    /// <summary>Returns the age shifted by a reservoir offset (the uncertainty is kept, a deliberate simplification).</summary>
    public RadiocarbonAge CorrectedFor(int reservoirOffset)
    {
        int corrected = YearsBp - reservoirOffset;
        if (corrected < 0)
        {
            throw new InvalidOperationException($"A reservoir offset of {reservoirOffset} years would make {YearsBp} BP negative.");
        }

        return new RadiocarbonAge(corrected, Uncertainty);
    }

    public bool Equals(RadiocarbonAge? other) => other is not null && YearsBp == other.YearsBp && Uncertainty == other.Uncertainty;

    public override bool Equals(object? obj) => Equals(obj as RadiocarbonAge);

    public override int GetHashCode() => HashCode.Combine(YearsBp, Uncertainty);

    public override string ToString() => $"{YearsBp} ± {Uncertainty} BP";
}
