// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate GrowthRate --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.

using System;

using JustDummies;

namespace OrbitalMicrobiology.Domain;

/// <summary>
///     A generator of arbitrary <see cref="GrowthRate" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyGrowthRate : IAny<GrowthRate> {

    private readonly IAny<double> _perHour;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyGrowthRate()
        : this(perHour: AnyValidPerHour()) { }

    private static AnyDouble AnyValidPerHour() {
        return Any.Double().Between(0, GrowthRate.MaxPerHour);
    }

    private AnyGrowthRate(IAny<double> perHour) {
        _perHour = perHour;
    }

    /// <summary>Pins <c>perHour</c> to a fixed value.</summary>
    public AnyGrowthRate WithPerHour(double value) {
        return new AnyGrowthRate(new FixedValue<double>(value));
    }

    /// <summary>Draws <c>perHour</c> from <c>AnyValidPerHour()</c>, refined by <paramref name="configure" />.</summary>
    public AnyGrowthRate WithPerHour(Func<AnyDouble, AnyDouble> configure) {
        return new AnyGrowthRate(configure(AnyValidPerHour()));
    }

    /// <summary>Produces one arbitrary <see cref="GrowthRate" />.</summary>
    public GrowthRate Generate() {
        return new GrowthRate(_perHour.Generate());
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
