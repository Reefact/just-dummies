// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate KinshipPolicy --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.

using System;

using JustDummies;

namespace CaptiveBreeding.Domain;

/// <summary>
///     A generator of arbitrary <see cref="KinshipPolicy" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyKinshipPolicy : IAny<KinshipPolicy> {

    private readonly IAny<KinshipCoefficient> _maximumPairKinship;
    private readonly IAny<int>                _maximumPairingsPerIndividual;
    private readonly IAny<int>                _minimumBreedingAgeYears;
    private readonly IAny<int>                _maximumBreedingAgeYears;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyKinshipPolicy()
        : this(maximumPairKinship:           AnyValidMaximumPairKinship(),
               maximumPairingsPerIndividual: AnyValidMaximumPairingsPerIndividual(),
               minimumBreedingAgeYears:      AnyValidMinimumBreedingAgeYears(),
               maximumBreedingAgeYears:      AnyValidMaximumBreedingAgeYears()) { }

    private static AnyKinshipCoefficient AnyValidMaximumPairKinship() {
        return new AnyKinshipCoefficient().WithValue(v => v.LessThanOrEqualTo(0.25));
    }

    private static AnyInt32 AnyValidMaximumPairingsPerIndividual() {
        return Any.Int32().Between(1, 10);
    }

    private static AnyInt32 AnyValidMinimumBreedingAgeYears() {
        return Any.Int32().Between(0, 50);
    }

    private static AnyInt32 AnyValidMaximumBreedingAgeYears() {
        // The maximum must exceed the minimum: drawn above the minimum's whole range, so the rule
        // holds by construction. A test that cares about the window pins both.
        return Any.Int32().Between(51, 100);
    }

    private AnyKinshipPolicy(IAny<KinshipCoefficient> maximumPairKinship,
                             IAny<int>                maximumPairingsPerIndividual,
                             IAny<int>                minimumBreedingAgeYears,
                             IAny<int>                maximumBreedingAgeYears) {
        _maximumPairKinship           = maximumPairKinship;
        _maximumPairingsPerIndividual = maximumPairingsPerIndividual;
        _minimumBreedingAgeYears      = minimumBreedingAgeYears;
        _maximumBreedingAgeYears      = maximumBreedingAgeYears;
    }

    /// <summary>Pins <c>maximumPairKinship</c> to a fixed value.</summary>
    public AnyKinshipPolicy WithMaximumPairKinship(KinshipCoefficient value) {
        return new AnyKinshipPolicy(new FixedValue<KinshipCoefficient>(value), _maximumPairingsPerIndividual, _minimumBreedingAgeYears, _maximumBreedingAgeYears);
    }

    /// <summary>Draws <c>maximumPairKinship</c> from <c>AnyValidMaximumPairKinship()</c>, refined by <paramref name="configure" />.</summary>
    public AnyKinshipPolicy WithMaximumPairKinship(Func<AnyKinshipCoefficient, AnyKinshipCoefficient> configure) {
        return new AnyKinshipPolicy(configure(AnyValidMaximumPairKinship()), _maximumPairingsPerIndividual, _minimumBreedingAgeYears, _maximumBreedingAgeYears);
    }

    /// <summary>Pins <c>maximumPairingsPerIndividual</c> to a fixed value.</summary>
    public AnyKinshipPolicy WithMaximumPairingsPerIndividual(int value) {
        return new AnyKinshipPolicy(_maximumPairKinship, new FixedValue<int>(value), _minimumBreedingAgeYears, _maximumBreedingAgeYears);
    }

    /// <summary>Draws <c>maximumPairingsPerIndividual</c> from <c>AnyValidMaximumPairingsPerIndividual()</c>, refined by <paramref name="configure" />.</summary>
    public AnyKinshipPolicy WithMaximumPairingsPerIndividual(Func<AnyInt32, AnyInt32> configure) {
        return new AnyKinshipPolicy(_maximumPairKinship, configure(AnyValidMaximumPairingsPerIndividual()), _minimumBreedingAgeYears, _maximumBreedingAgeYears);
    }

    /// <summary>Pins <c>minimumBreedingAgeYears</c> to a fixed value.</summary>
    public AnyKinshipPolicy WithMinimumBreedingAgeYears(int value) {
        return new AnyKinshipPolicy(_maximumPairKinship, _maximumPairingsPerIndividual, new FixedValue<int>(value), _maximumBreedingAgeYears);
    }

    /// <summary>Draws <c>minimumBreedingAgeYears</c> from <c>AnyValidMinimumBreedingAgeYears()</c>, refined by <paramref name="configure" />.</summary>
    public AnyKinshipPolicy WithMinimumBreedingAgeYears(Func<AnyInt32, AnyInt32> configure) {
        return new AnyKinshipPolicy(_maximumPairKinship, _maximumPairingsPerIndividual, configure(AnyValidMinimumBreedingAgeYears()), _maximumBreedingAgeYears);
    }

    /// <summary>Pins <c>maximumBreedingAgeYears</c> to a fixed value.</summary>
    public AnyKinshipPolicy WithMaximumBreedingAgeYears(int value) {
        return new AnyKinshipPolicy(_maximumPairKinship, _maximumPairingsPerIndividual, _minimumBreedingAgeYears, new FixedValue<int>(value));
    }

    /// <summary>Draws <c>maximumBreedingAgeYears</c> from <c>AnyValidMaximumBreedingAgeYears()</c>, refined by <paramref name="configure" />.</summary>
    public AnyKinshipPolicy WithMaximumBreedingAgeYears(Func<AnyInt32, AnyInt32> configure) {
        return new AnyKinshipPolicy(_maximumPairKinship, _maximumPairingsPerIndividual, _minimumBreedingAgeYears, configure(AnyValidMaximumBreedingAgeYears()));
    }

    /// <summary>Produces one arbitrary <see cref="KinshipPolicy" />.</summary>
    public KinshipPolicy Generate() {
        return new KinshipPolicy(_maximumPairKinship.Generate(),
                                 _maximumPairingsPerIndividual.Generate(),
                                 _minimumBreedingAgeYears.Generate(),
                                 _maximumBreedingAgeYears.Generate());
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
