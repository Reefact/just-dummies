using System;

using JustDummies;

namespace RealEstate.Domain;

/// <summary>
///     A generator of arbitrary <see cref="EnergyPerformanceDiagnosis" /> values. The declared rating is not drawn:
///     it is the one the drawn figures give, since the constructor refuses any other.
/// </summary>
public sealed partial class AnyEnergyPerformanceDiagnosis : IAny<EnergyPerformanceDiagnosis> {

    private IAny<int>      _primaryEnergyKwhPerSquareMeterYear;
    private IAny<int>      _emissionsKgCo2PerSquareMeterYear;
    private IAny<DateOnly> _issuedOn;

    private AnyInt32    _primaryEnergyKwhPerSquareMeterYearRecipe;
    private AnyInt32    _emissionsKgCo2PerSquareMeterYearRecipe;
    private AnyDateOnly _issuedOnRecipe;

    public AnyEnergyPerformanceDiagnosis()
        : this(primaryEnergyKwhPerSquareMeterYear: AnyValidPrimaryEnergyKwhPerSquareMeterYear(),
               emissionsKgCo2PerSquareMeterYear:   AnyValidEmissionsKgCo2PerSquareMeterYear(),
               issuedOn:                           AnyValidIssuedOn()) { }

    private static AnyInt32 AnyValidPrimaryEnergyKwhPerSquareMeterYear() {
        // Spans every band, G included (420 and above).
        return Any.Int32().Between(0, 600);
    }

    private static AnyInt32 AnyValidEmissionsKgCo2PerSquareMeterYear() {
        return Any.Int32().Between(0, 150);
    }

    private static AnyDateOnly AnyValidIssuedOn() {
        return Any.DateOnly().Between(EnergyPerformanceDiagnosis.ReformDate, EnergyPerformanceDiagnosis.LatestIssueDate);
    }

    private AnyEnergyPerformanceDiagnosis(AnyInt32    primaryEnergyKwhPerSquareMeterYear,
                                          AnyInt32    emissionsKgCo2PerSquareMeterYear,
                                          AnyDateOnly issuedOn) {
        _primaryEnergyKwhPerSquareMeterYear       = primaryEnergyKwhPerSquareMeterYear;
        _primaryEnergyKwhPerSquareMeterYearRecipe = primaryEnergyKwhPerSquareMeterYear;
        _emissionsKgCo2PerSquareMeterYear         = emissionsKgCo2PerSquareMeterYear;
        _emissionsKgCo2PerSquareMeterYearRecipe   = emissionsKgCo2PerSquareMeterYear;
        _issuedOn                                 = issuedOn;
        _issuedOnRecipe                           = issuedOn;
    }

    private AnyEnergyPerformanceDiagnosis(AnyEnergyPerformanceDiagnosis from) {
        _primaryEnergyKwhPerSquareMeterYear       = from._primaryEnergyKwhPerSquareMeterYear;
        _primaryEnergyKwhPerSquareMeterYearRecipe = from._primaryEnergyKwhPerSquareMeterYearRecipe;
        _emissionsKgCo2PerSquareMeterYear         = from._emissionsKgCo2PerSquareMeterYear;
        _emissionsKgCo2PerSquareMeterYearRecipe   = from._emissionsKgCo2PerSquareMeterYearRecipe;
        _issuedOn                                 = from._issuedOn;
        _issuedOnRecipe                           = from._issuedOnRecipe;
    }

    public AnyEnergyPerformanceDiagnosis WithPrimaryEnergyKwhPerSquareMeterYear(int value) {
        AnyEnergyPerformanceDiagnosis copy = new AnyEnergyPerformanceDiagnosis(this);

        copy._primaryEnergyKwhPerSquareMeterYear = Any.Fixed(value);

        return copy;
    }

    public AnyEnergyPerformanceDiagnosis WithPrimaryEnergyKwhPerSquareMeterYear(Func<AnyInt32, AnyInt32> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyEnergyPerformanceDiagnosis copy = new AnyEnergyPerformanceDiagnosis(this);

        copy._primaryEnergyKwhPerSquareMeterYearRecipe = configure(_primaryEnergyKwhPerSquareMeterYearRecipe);
        copy._primaryEnergyKwhPerSquareMeterYear       = copy._primaryEnergyKwhPerSquareMeterYearRecipe;

        return copy;
    }

    public AnyEnergyPerformanceDiagnosis WithEmissionsKgCo2PerSquareMeterYear(int value) {
        AnyEnergyPerformanceDiagnosis copy = new AnyEnergyPerformanceDiagnosis(this);

        copy._emissionsKgCo2PerSquareMeterYear = Any.Fixed(value);

        return copy;
    }

    public AnyEnergyPerformanceDiagnosis WithEmissionsKgCo2PerSquareMeterYear(Func<AnyInt32, AnyInt32> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyEnergyPerformanceDiagnosis copy = new AnyEnergyPerformanceDiagnosis(this);

        copy._emissionsKgCo2PerSquareMeterYearRecipe = configure(_emissionsKgCo2PerSquareMeterYearRecipe);
        copy._emissionsKgCo2PerSquareMeterYear       = copy._emissionsKgCo2PerSquareMeterYearRecipe;

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
        int primaryEnergy = _primaryEnergyKwhPerSquareMeterYear.Generate();
        int emissions     = _emissionsKgCo2PerSquareMeterYear.Generate();

        return new EnergyPerformanceDiagnosis(primaryEnergy,
                                              emissions,
                                              EnergyPerformanceDiagnosis.Rate(primaryEnergy, emissions),
                                              _issuedOn.Generate());
    }

}
