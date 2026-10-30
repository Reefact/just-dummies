// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate Sscc --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.

using System;

using JustDummies;

namespace Warehouse.Domain;

/// <summary>
///     A generator of arbitrary <see cref="Sscc" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnySscc : IAny<Sscc> {

    private readonly IAny<string> _value;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnySscc()
        : this(value: AnyValidValue()) { }

    private static IAny<string> AnyValidValue() {
        // An SSCC is 17 digits followed by their GS1 check digit: the check digit is computed, never drawn.
        return Any.String().Numeric().WithLength(17).As(body => body + Gs1.ComputeCheckDigit(body));
    }

    private AnySscc(IAny<string> value) {
        _value = value;
    }

    /// <summary>Pins <c>value</c> to a fixed value.</summary>
    public AnySscc WithValue(string value) {
        return new AnySscc(new FixedValue<string>(value));
    }

    /// <summary>Produces one arbitrary <see cref="Sscc" />.</summary>
    public Sscc Generate() {
        return new Sscc(_value.Generate());
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
