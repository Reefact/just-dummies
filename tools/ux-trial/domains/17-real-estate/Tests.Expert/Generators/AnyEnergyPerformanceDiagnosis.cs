// Scaffolded by `any` (JustDummies), then rewritten by hand: the constructor requires the declared rating to
// equal the one the figures give, so the rating is drawn first and the figures are drawn inside its band.
// The scaffold's four independent parameters could never satisfy that rule.

using System;

using JustDummies;

namespace RealEstate.Domain;

/// <summary>A generator of arbitrary <see cref="EnergyPerformanceDiagnosis" /> values whose declared rating is the computed one.</summary>
public sealed partial class AnyEnergyPerformanceDiagnosis : IAny<EnergyPerformanceDiagnosis> {

    // Exclusive upper bound of each band, A to F; G is open-ended. These are the figures of the DPE grid.
    private static readonly int[] EnergyCeilings    = [70, 110, 180, 250, 330, 420];
    private static readonly int[] EmissionsCeilings = [6, 11, 30, 50, 70, 100];

    private const int AnyEnergyForG    = 1_000;
    private const int AnyEmissionsForG = 300;

    private IAny<EnergyRating> _rating;
    private IAny<DateOnly>     _issuedOn;

    private AnyEnum<EnergyRating> _ratingRecipe;
    private AnyDateOnly           _issuedOnRecipe;

    public AnyEnergyPerformanceDiagnosis()
        : this(rating:   AnyValidRating(),
               issuedOn: AnyValidIssuedOn()) { }

    private static AnyEnum<EnergyRating> AnyValidRating() {
        return Any.Enum<EnergyRating>();
    }

    private static AnyDateOnly AnyValidIssuedOn() {
        return Any.DateOnly().Between(EnergyPerformanceDiagnosis.ReformDate, EnergyPerformanceDiagnosis.LatestIssueDate);
    }

    private AnyEnergyPerformanceDiagnosis(AnyEnum<EnergyRating> rating, AnyDateOnly issuedOn) {
        _rating         = rating;
        _ratingRecipe   = rating;
        _issuedOn       = issuedOn;
        _issuedOnRecipe = issuedOn;
    }

    private AnyEnergyPerformanceDiagnosis(AnyEnergyPerformanceDiagnosis from) {
        _rating         = from._rating;
        _ratingRecipe   = from._ratingRecipe;
        _issuedOn       = from._issuedOn;
        _issuedOnRecipe = from._issuedOnRecipe;
    }

    /// <summary>Pins the rating the figures will give.</summary>
    public AnyEnergyPerformanceDiagnosis WithRating(EnergyRating value) {
        AnyEnergyPerformanceDiagnosis copy = new AnyEnergyPerformanceDiagnosis(this);

        copy._rating = Any.Fixed(value);

        return copy;
    }

    public AnyEnergyPerformanceDiagnosis WithRating(Func<AnyEnum<EnergyRating>, AnyEnum<EnergyRating>> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyEnergyPerformanceDiagnosis copy = new AnyEnergyPerformanceDiagnosis(this);

        copy._ratingRecipe = configure(_ratingRecipe);
        copy._rating       = copy._ratingRecipe;

        return copy;
    }

    public AnyEnergyPerformanceDiagnosis WithIssuedOn(DateOnly value) {
        AnyEnergyPerformanceDiagnosis copy = new AnyEnergyPerformanceDiagnosis(this);

        copy._issuedOn = Any.Fixed(value);

        return copy;
    }

    public AnyEnergyPerformanceDiagnosis WithIssuedOn(Func<AnyDateOnly, AnyDateOnly> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyEnergyPerformanceDiagnosis copy = new AnyEnergyPerformanceDiagnosis(this);

        copy._issuedOnRecipe = configure(_issuedOnRecipe);
        copy._issuedOn       = copy._issuedOnRecipe;

        return copy;
    }

    public EnergyPerformanceDiagnosis Generate() {
        EnergyRating rating = _rating.Generate();
        int          band   = (int)rating;

        // One of the two axes carries the rating, the other stays at or under it.
        bool energyCarries = Any.Boolean().Generate();

        int carrierEnergy    = Any.Int32().Between(Floor(EnergyCeilings, band), Ceiling(EnergyCeilings, band, AnyEnergyForG)).Generate();
        int carrierEmissions = Any.Int32().Between(Floor(EmissionsCeilings, band), Ceiling(EmissionsCeilings, band, AnyEmissionsForG)).Generate();
        int otherEnergy      = Any.Int32().Between(0, Ceiling(EnergyCeilings, band, AnyEnergyForG)).Generate();
        int otherEmissions   = Any.Int32().Between(0, Ceiling(EmissionsCeilings, band, AnyEmissionsForG)).Generate();

        int energy    = energyCarries ? carrierEnergy : otherEnergy;
        int emissions = energyCarries ? otherEmissions : carrierEmissions;

        return new EnergyPerformanceDiagnosis(energy, emissions, rating, _issuedOn.Generate());
    }

    private static int Floor(int[] ceilings, int band) => band == 0 ? 0 : ceilings[band - 1];

    private static int Ceiling(int[] ceilings, int band, int openEnded) => band == ceilings.Length ? openEnded : ceilings[band] - 1;

}
