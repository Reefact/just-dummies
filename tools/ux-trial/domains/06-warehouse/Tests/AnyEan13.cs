// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate Ean13 --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.

using System;

using JustDummies;

namespace Warehouse.Domain;

/// <summary>
///     A generator of arbitrary <see cref="Ean13" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyEan13 : IAny<Ean13> {

    private readonly IAny<string> _code;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyEan13()
        : this(code: AnyValidCode()) { }

    private static IAny<string> AnyValidCode() {
        // A GTIN-13 is 12 digits followed by their GS1 check digit: the check digit is computed, never drawn.
        return Any.String().Numeric().WithLength(12).As(body => body + Gs1.ComputeCheckDigit(body));
    }

    private AnyEan13(IAny<string> code) {
        _code = code;
    }

    /// <summary>Pins <c>code</c> to a fixed value.</summary>
    public AnyEan13 WithCode(string value) {
        return new AnyEan13(new FixedValue<string>(value));
    }

    /// <summary>Produces one arbitrary <see cref="Ean13" />.</summary>
    public Ean13 Generate() {
        return new Ean13(_code.Generate());
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
