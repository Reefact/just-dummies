// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate TemperatureRange --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.

using System;

using JustDummies;

namespace Warehouse.Domain;

/// <summary>
///     A generator of arbitrary <see cref="TemperatureRange" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyTemperatureRange : IAny<TemperatureRange> {

    private readonly IAny<decimal> _minCelsius;
    private readonly IAny<decimal> _maxCelsius;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyTemperatureRange()
        : this(minCelsius: AnyValidMinCelsius(),
               maxCelsius: AnyValidMaxCelsius()) { }

    private static AnyDecimal AnyValidMinCelsius() {
        // Storage temperatures lie in -40..40; the minimum leaves room for a maximum above it.
        return Any.Decimal().Between(-40m, 39.9m).WithScale(1);
    }

    private static AnyDecimal AnyValidMaxCelsius() {
        return Any.Decimal().Between(-40m, 40m).WithScale(1);
    }

    private AnyTemperatureRange(IAny<decimal> minCelsius,
                                IAny<decimal> maxCelsius) {
        _minCelsius = minCelsius;
        _maxCelsius = maxCelsius;
    }

    /// <summary>Pins <c>minCelsius</c> to a fixed value.</summary>
    public AnyTemperatureRange WithMinCelsius(decimal value) {
        return new AnyTemperatureRange(new FixedValue<decimal>(value), _maxCelsius);
    }

    /// <summary>Draws <c>minCelsius</c> from <c>AnyValidMinCelsius()</c>, refined by <paramref name="configure" />.</summary>
    public AnyTemperatureRange WithMinCelsius(Func<AnyDecimal, AnyDecimal> configure) {
        return new AnyTemperatureRange(configure(AnyValidMinCelsius()), _maxCelsius);
    }

    /// <summary>Pins <c>maxCelsius</c> to a fixed value.</summary>
    public AnyTemperatureRange WithMaxCelsius(decimal value) {
        return new AnyTemperatureRange(_minCelsius, new FixedValue<decimal>(value));
    }

    /// <summary>Draws <c>maxCelsius</c> from <c>AnyValidMaxCelsius()</c>, refined by <paramref name="configure" />.</summary>
    public AnyTemperatureRange WithMaxCelsius(Func<AnyDecimal, AnyDecimal> configure) {
        return new AnyTemperatureRange(_minCelsius, configure(AnyValidMaxCelsius()));
    }

    /// <summary>Produces one arbitrary <see cref="TemperatureRange" />.</summary>
    public TemperatureRange Generate() {
        // Fixed by hand: the minimum must lie below the maximum, a rule between two parameters that no
        // per-parameter recipe can carry. The maximum is drawn above the minimum whenever it is still a builder.
        decimal minCelsius = _minCelsius.Generate();
        decimal maxCelsius = _maxCelsius is AnyDecimal maxGenerator
                                 ? maxGenerator.GreaterThan(minCelsius).Generate()
                                 : _maxCelsius.Generate();

        return new TemperatureRange(minCelsius, maxCelsius);
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
