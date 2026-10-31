// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate Currency --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.

using System;

using JustDummies;

namespace LiquidityGap.Domain;

/// <summary>
///     A generator of arbitrary <see cref="Currency" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyCurrency : IAny<Currency> {

    private readonly IAny<string> _code;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyCurrency()
        : this(code: AnyValidCode()) { }

    private static AnyString AnyValidCode() {
        // An ISO 4217 code: three uppercase ASCII letters.
        return Any.String().Alpha().InUpperCase().WithLength(3);
    }

    private AnyCurrency(IAny<string> code) {
        _code = code;
    }

    /// <summary>Pins <c>code</c> to a fixed value.</summary>
    public AnyCurrency WithCode(string value) {
        return new AnyCurrency(new FixedValue<string>(value));
    }

    /// <summary>Draws <c>code</c> from <c>AnyValidCode()</c>, refined by <paramref name="configure" />.</summary>
    public AnyCurrency WithCode(Func<AnyString, AnyString> configure) {
        return new AnyCurrency(configure(AnyValidCode()));
    }

    /// <summary>Produces one arbitrary <see cref="Currency" />.</summary>
    public Currency Generate() {
        return new Currency(_code.Generate());
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
