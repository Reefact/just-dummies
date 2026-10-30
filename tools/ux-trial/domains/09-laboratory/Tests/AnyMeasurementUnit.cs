// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate MeasurementUnit --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.

using System;

using JustDummies;

namespace Laboratory.Domain;

/// <summary>
///     A generator of arbitrary <see cref="MeasurementUnit" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyMeasurementUnit : IAny<MeasurementUnit> {

    private readonly IAny<string> _symbol;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyMeasurementUnit()
        : this(symbol: AnyValidSymbol()) { }

    private static AnyString AnyValidSymbol() {
        return Any.String().NotBlank().WithMaxLength(16);
    }

    private AnyMeasurementUnit(IAny<string> symbol) {
        _symbol = symbol;
    }

    /// <summary>Pins <c>symbol</c> to a fixed value.</summary>
    public AnyMeasurementUnit WithSymbol(string value) {
        return new AnyMeasurementUnit(new FixedValue<string>(value));
    }

    /// <summary>Draws <c>symbol</c> from <c>AnyValidSymbol()</c>, refined by <paramref name="configure" />.</summary>
    public AnyMeasurementUnit WithSymbol(Func<AnyString, AnyString> configure) {
        return new AnyMeasurementUnit(configure(AnyValidSymbol()));
    }

    /// <summary>Produces one arbitrary <see cref="MeasurementUnit" />.</summary>
    public MeasurementUnit Generate() {
        return new MeasurementUnit(_symbol.Generate());
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
