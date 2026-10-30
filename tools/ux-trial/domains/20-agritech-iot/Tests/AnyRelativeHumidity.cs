// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate RelativeHumidity --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.

using System;

using JustDummies;

namespace AgriTech.Domain;

/// <summary>
///     A generator of arbitrary <see cref="RelativeHumidity" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyRelativeHumidity : IAny<RelativeHumidity> {

    private readonly IAny<double> _percent;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyRelativeHumidity()
        : this(percent: AnyValidPercent()) { }

    private static AnyDouble AnyValidPercent() {
        return Any.Double().Between(0d, 100d);
    }

    private AnyRelativeHumidity(IAny<double> percent) {
        _percent = percent;
    }

    /// <summary>Pins <c>percent</c> to a fixed value.</summary>
    public AnyRelativeHumidity WithPercent(double value) {
        return new AnyRelativeHumidity(new FixedValue<double>(value));
    }

    /// <summary>Draws <c>percent</c> from <c>AnyValidPercent()</c>, refined by <paramref name="configure" />.</summary>
    public AnyRelativeHumidity WithPercent(Func<AnyDouble, AnyDouble> configure) {
        return new AnyRelativeHumidity(configure(AnyValidPercent()));
    }

    /// <summary>Produces one arbitrary <see cref="RelativeHumidity" />.</summary>
    public RelativeHumidity Generate() {
        return new RelativeHumidity(_percent.Generate());
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
