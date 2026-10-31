// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate Species --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.

using System;

using JustDummies;

namespace OrbitalMicrobiology.Domain;

/// <summary>
///     A generator of arbitrary <see cref="Species" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnySpecies : IAny<Species> {

    private readonly IAny<string>     _binomialName;
    private readonly IAny<GrowthRate> _maxGrowthRate;
    private readonly IAny<double>     _minTemperatureCelsius;
    private readonly IAny<double>     _optimalTemperatureCelsius;
    private readonly IAny<double>     _maxTemperatureCelsius;
    private readonly IAny<double>     _lagHours;
    private readonly IAny<double>     _yieldCellsPerGram;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnySpecies()
        : this(binomialName:              AnyValidBinomialName(),
               maxGrowthRate:             AnyValidMaxGrowthRate(),
               minTemperatureCelsius:     AnyValidMinTemperatureCelsius(),
               optimalTemperatureCelsius: AnyValidOptimalTemperatureCelsius(),
               maxTemperatureCelsius:     AnyValidMaxTemperatureCelsius(),
               lagHours:                  AnyValidLagHours(),
               yieldCellsPerGram:         AnyValidYieldCellsPerGram()) { }

    private static AnyPattern AnyValidBinomialName() {
        return Any.StringMatching("[A-Z][a-z]{3,10} [a-z]{4,12}");
    }

    private static AnyGrowthRate AnyValidMaxGrowthRate() {
        // A species grows at some positive rate.
        return new AnyGrowthRate().WithPerHour(p => p.Positive());
    }

    private static AnyDouble AnyValidMinTemperatureCelsius() {
        return Any.Double().Between(-20, 20);
    }

    private static AnyDouble AnyValidOptimalTemperatureCelsius() {
        return Any.Double().Between(21, 50);
    }

    private static AnyDouble AnyValidMaxTemperatureCelsius() {
        return Any.Double().Between(51, 80);
    }

    private static AnyDouble AnyValidLagHours() {
        return Any.Double().Between(0, Species.MaxLagHours);
    }

    private static AnyDouble AnyValidYieldCellsPerGram() {
        return Any.Double().Between(Species.MinYieldCellsPerGram, Species.MaxYieldCellsPerGram);
    }

    private AnySpecies(IAny<string>     binomialName,
                       IAny<GrowthRate> maxGrowthRate,
                       IAny<double>     minTemperatureCelsius,
                       IAny<double>     optimalTemperatureCelsius,
                       IAny<double>     maxTemperatureCelsius,
                       IAny<double>     lagHours,
                       IAny<double>     yieldCellsPerGram) {
        _binomialName              = binomialName;
        _maxGrowthRate             = maxGrowthRate;
        _minTemperatureCelsius     = minTemperatureCelsius;
        _optimalTemperatureCelsius = optimalTemperatureCelsius;
        _maxTemperatureCelsius     = maxTemperatureCelsius;
        _lagHours                  = lagHours;
        _yieldCellsPerGram         = yieldCellsPerGram;
    }

    /// <summary>Pins <c>binomialName</c> to a fixed value.</summary>
    public AnySpecies WithBinomialName(string value) {
        return new AnySpecies(new FixedValue<string>(value), _maxGrowthRate, _minTemperatureCelsius, _optimalTemperatureCelsius, _maxTemperatureCelsius, _lagHours, _yieldCellsPerGram);
    }

    /// <summary>Draws <c>binomialName</c> from <c>AnyValidBinomialName()</c>, refined by <paramref name="configure" />.</summary>
    public AnySpecies WithBinomialName(Func<AnyPattern, AnyPattern> configure) {
        return new AnySpecies(configure(AnyValidBinomialName()), _maxGrowthRate, _minTemperatureCelsius, _optimalTemperatureCelsius, _maxTemperatureCelsius, _lagHours, _yieldCellsPerGram);
    }

    /// <summary>Pins <c>maxGrowthRate</c> to a fixed value.</summary>
    public AnySpecies WithMaxGrowthRate(GrowthRate value) {
        return new AnySpecies(_binomialName, new FixedValue<GrowthRate>(value), _minTemperatureCelsius, _optimalTemperatureCelsius, _maxTemperatureCelsius, _lagHours, _yieldCellsPerGram);
    }

    /// <summary>Draws <c>maxGrowthRate</c> from <c>AnyValidMaxGrowthRate()</c>, refined by <paramref name="configure" />.</summary>
    public AnySpecies WithMaxGrowthRate(Func<AnyGrowthRate, AnyGrowthRate> configure) {
        return new AnySpecies(_binomialName, configure(AnyValidMaxGrowthRate()), _minTemperatureCelsius, _optimalTemperatureCelsius, _maxTemperatureCelsius, _lagHours, _yieldCellsPerGram);
    }

    /// <summary>Pins <c>minTemperatureCelsius</c> to a fixed value.</summary>
    public AnySpecies WithMinTemperatureCelsius(double value) {
        return new AnySpecies(_binomialName, _maxGrowthRate, new FixedValue<double>(value), _optimalTemperatureCelsius, _maxTemperatureCelsius, _lagHours, _yieldCellsPerGram);
    }

    /// <summary>Draws <c>minTemperatureCelsius</c> from <c>AnyValidMinTemperatureCelsius()</c>, refined by <paramref name="configure" />.</summary>
    public AnySpecies WithMinTemperatureCelsius(Func<AnyDouble, AnyDouble> configure) {
        return new AnySpecies(_binomialName, _maxGrowthRate, configure(AnyValidMinTemperatureCelsius()), _optimalTemperatureCelsius, _maxTemperatureCelsius, _lagHours, _yieldCellsPerGram);
    }

    /// <summary>Pins <c>optimalTemperatureCelsius</c> to a fixed value.</summary>
    public AnySpecies WithOptimalTemperatureCelsius(double value) {
        return new AnySpecies(_binomialName, _maxGrowthRate, _minTemperatureCelsius, new FixedValue<double>(value), _maxTemperatureCelsius, _lagHours, _yieldCellsPerGram);
    }

    /// <summary>Draws <c>optimalTemperatureCelsius</c> from <c>AnyValidOptimalTemperatureCelsius()</c>, refined by <paramref name="configure" />.</summary>
    public AnySpecies WithOptimalTemperatureCelsius(Func<AnyDouble, AnyDouble> configure) {
        return new AnySpecies(_binomialName, _maxGrowthRate, _minTemperatureCelsius, configure(AnyValidOptimalTemperatureCelsius()), _maxTemperatureCelsius, _lagHours, _yieldCellsPerGram);
    }

    /// <summary>Pins <c>maxTemperatureCelsius</c> to a fixed value.</summary>
    public AnySpecies WithMaxTemperatureCelsius(double value) {
        return new AnySpecies(_binomialName, _maxGrowthRate, _minTemperatureCelsius, _optimalTemperatureCelsius, new FixedValue<double>(value), _lagHours, _yieldCellsPerGram);
    }

    /// <summary>Draws <c>maxTemperatureCelsius</c> from <c>AnyValidMaxTemperatureCelsius()</c>, refined by <paramref name="configure" />.</summary>
    public AnySpecies WithMaxTemperatureCelsius(Func<AnyDouble, AnyDouble> configure) {
        return new AnySpecies(_binomialName, _maxGrowthRate, _minTemperatureCelsius, _optimalTemperatureCelsius, configure(AnyValidMaxTemperatureCelsius()), _lagHours, _yieldCellsPerGram);
    }

    /// <summary>Pins <c>lagHours</c> to a fixed value.</summary>
    public AnySpecies WithLagHours(double value) {
        return new AnySpecies(_binomialName, _maxGrowthRate, _minTemperatureCelsius, _optimalTemperatureCelsius, _maxTemperatureCelsius, new FixedValue<double>(value), _yieldCellsPerGram);
    }

    /// <summary>Draws <c>lagHours</c> from <c>AnyValidLagHours()</c>, refined by <paramref name="configure" />.</summary>
    public AnySpecies WithLagHours(Func<AnyDouble, AnyDouble> configure) {
        return new AnySpecies(_binomialName, _maxGrowthRate, _minTemperatureCelsius, _optimalTemperatureCelsius, _maxTemperatureCelsius, configure(AnyValidLagHours()), _yieldCellsPerGram);
    }

    /// <summary>Pins <c>yieldCellsPerGram</c> to a fixed value.</summary>
    public AnySpecies WithYieldCellsPerGram(double value) {
        return new AnySpecies(_binomialName, _maxGrowthRate, _minTemperatureCelsius, _optimalTemperatureCelsius, _maxTemperatureCelsius, _lagHours, new FixedValue<double>(value));
    }

    /// <summary>Draws <c>yieldCellsPerGram</c> from <c>AnyValidYieldCellsPerGram()</c>, refined by <paramref name="configure" />.</summary>
    public AnySpecies WithYieldCellsPerGram(Func<AnyDouble, AnyDouble> configure) {
        return new AnySpecies(_binomialName, _maxGrowthRate, _minTemperatureCelsius, _optimalTemperatureCelsius, _maxTemperatureCelsius, _lagHours, configure(AnyValidYieldCellsPerGram()));
    }

    /// <summary>Produces one arbitrary <see cref="Species" />.</summary>
    public Species Generate() {
        return new Species(_binomialName.Generate(),
                           _maxGrowthRate.Generate(),
                           _minTemperatureCelsius.Generate(),
                           _optimalTemperatureCelsius.Generate(),
                           _maxTemperatureCelsius.Generate(),
                           _lagHours.Generate(),
                           _yieldCellsPerGram.Generate());
    }

    private sealed class FixedValue<TValue> : IAny<TValue> {

        private readonly TValue _value;

        public FixedValue(TValue value) {
            _value = value;
        }

        public TValue Generate() {
            return _value;
        }

    }

}
