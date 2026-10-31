// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate Temperature --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.

using System;

using GeothermalReservoir.Domain;
using JustDummies;

namespace GeothermalReservoir.Tests.Expert.Arbitrary;

/// <summary>
///     A generator of arbitrary <see cref="Temperature" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyTemperature : IAny<Temperature> {

    private readonly IAny<double> _kelvin;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyTemperature()
        : this(kelvin: AnyValidKelvin()) { }

    private static AnyDouble AnyValidKelvin() {
        // Liquid water only: above freezing, at most 350 °C.
        return Any.Double().GreaterThan(Temperature.FreezingKelvin).LessThanOrEqualTo(Temperature.MaximumKelvin);
    }

    private AnyTemperature(IAny<double> kelvin) {
        _kelvin = kelvin;
    }

    /// <summary>Pins <c>kelvin</c> to a fixed value.</summary>
    public AnyTemperature WithKelvin(double value) {
        return new AnyTemperature(new FixedValue<double>(value));
    }

    /// <summary>Draws <c>kelvin</c> from <c>AnyValidKelvin()</c>, refined by <paramref name="configure" />.</summary>
    public AnyTemperature WithKelvin(Func<AnyDouble, AnyDouble> configure) {
        return new AnyTemperature(configure(AnyValidKelvin()));
    }

    /// <summary>Produces one arbitrary <see cref="Temperature" />.</summary>
    public Temperature Generate() {
        return new Temperature(_kelvin.Generate());
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
