// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate Pressure --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.

using System;

using GeothermalReservoir.Domain;
using JustDummies;

namespace GeothermalReservoir.Tests.Expert.Arbitrary;

/// <summary>
///     A generator of arbitrary <see cref="Pressure" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyPressure : IAny<Pressure> {

    private readonly IAny<double> _pascals;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyPressure()
        : this(pascals: AnyValidPascals()) { }

    private static AnyDouble AnyValidPascals() {
        // Absolute pore pressure in (0, 100 MPa].
        return Any.Double().GreaterThan(0.0).LessThanOrEqualTo(Pressure.MaximumPascals);
    }

    private AnyPressure(IAny<double> pascals) {
        _pascals = pascals;
    }

    /// <summary>Pins <c>pascals</c> to a fixed value.</summary>
    public AnyPressure WithPascals(double value) {
        return new AnyPressure(new FixedValue<double>(value));
    }

    /// <summary>Draws <c>pascals</c> from <c>AnyValidPascals()</c>, refined by <paramref name="configure" />.</summary>
    public AnyPressure WithPascals(Func<AnyDouble, AnyDouble> configure) {
        return new AnyPressure(configure(AnyValidPascals()));
    }

    /// <summary>Produces one arbitrary <see cref="Pressure" />.</summary>
    public Pressure Generate() {
        return new Pressure(_pascals.Generate());
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
