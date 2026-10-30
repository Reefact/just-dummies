namespace RealEstate.Domain;

/// <summary>
/// A DPE as reformed on 1 July 2021: the rating is the worse of the energy band and the
/// greenhouse-gas band, and the diagnosis is valid ten years.
/// </summary>
public sealed class EnergyPerformanceDiagnosis {

    public static readonly DateOnly ReformDate = new DateOnly(2021, 7, 1);

    /// <summary>The validity end must stay a representable date.</summary>
    public static readonly DateOnly LatestIssueDate = DateOnly.MaxValue.AddYears(-10);

    public EnergyPerformanceDiagnosis(int primaryEnergyKwhPerSquareMeterYear, int emissionsKgCo2PerSquareMeterYear, EnergyRating declaredRating, DateOnly issuedOn) {
        ArgumentOutOfRangeException.ThrowIfNegative(primaryEnergyKwhPerSquareMeterYear);
        ArgumentOutOfRangeException.ThrowIfNegative(emissionsKgCo2PerSquareMeterYear);
        if (issuedOn < ReformDate) {
            throw new ArgumentOutOfRangeException(nameof(issuedOn), "A diagnosis issued before the 2021 reform is no longer accepted.");
        }
        ArgumentOutOfRangeException.ThrowIfGreaterThan(issuedOn, LatestIssueDate);
        EnergyRating computed = Rate(primaryEnergyKwhPerSquareMeterYear, emissionsKgCo2PerSquareMeterYear);
        if (computed != declaredRating) {
            throw new ArgumentException($"The figures give {computed}, not {declaredRating}.", nameof(declaredRating));
        }
        PrimaryEnergy = primaryEnergyKwhPerSquareMeterYear;
        Emissions = emissionsKgCo2PerSquareMeterYear;
        Rating = declaredRating;
        IssuedOn = issuedOn;
    }

    public int PrimaryEnergy { get; }

    public int Emissions { get; }

    public EnergyRating Rating { get; }

    public DateOnly IssuedOn { get; }

    public DateOnly ValidUntil => IssuedOn.AddYears(10);

    public bool IsValidOn(DateOnly day) => day >= IssuedOn && day < ValidUntil;

    /// <summary>The "passoire thermique" label: F or G.</summary>
    public bool IsEnergySieve => Rating >= EnergyRating.F;

    public static EnergyRating Rate(int primaryEnergy, int emissions) {
        EnergyRating byEnergy = primaryEnergy switch {
            < 70  => EnergyRating.A,
            < 110 => EnergyRating.B,
            < 180 => EnergyRating.C,
            < 250 => EnergyRating.D,
            < 330 => EnergyRating.E,
            < 420 => EnergyRating.F,
            _     => EnergyRating.G
        };
        EnergyRating byEmissions = emissions switch {
            < 6   => EnergyRating.A,
            < 11  => EnergyRating.B,
            < 30  => EnergyRating.C,
            < 50  => EnergyRating.D,
            < 70  => EnergyRating.E,
            < 100 => EnergyRating.F,
            _     => EnergyRating.G
        };
        return byEnergy > byEmissions ? byEnergy : byEmissions;
    }
}
