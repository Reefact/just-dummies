// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate Portfolio --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.

using System;

using JustDummies;

namespace CatastropheLosses.Domain;

/// <summary>
///     A generator of arbitrary <see cref="Portfolio" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyPortfolio : IAny<Portfolio> {

    private readonly IAny<string> _name;
    private readonly IAny<string> _currency;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyPortfolio()
        : this(name:     AnyValidName(),
               currency: AnyValidCurrency()) { }

    private static AnyString AnyValidName() {
        return Any.String().NotBlank().WithMaxLength(100);
    }

    private static AnyPattern AnyValidCurrency() {
        return Any.StringMatching("[A-Z]{3}");
    }

    private AnyPortfolio(IAny<string> name,
                         IAny<string> currency) {
        _name     = name;
        _currency = currency;
    }

    /// <summary>Pins <c>name</c> to a fixed value.</summary>
    public AnyPortfolio WithName(string value) {
        return new AnyPortfolio(new FixedValue<string>(value), _currency);
    }

    /// <summary>Draws <c>name</c> from <c>AnyValidName()</c>, refined by <paramref name="configure" />.</summary>
    public AnyPortfolio WithName(Func<AnyString, AnyString> configure) {
        return new AnyPortfolio(configure(AnyValidName()), _currency);
    }

    /// <summary>Pins <c>currency</c> to a fixed value.</summary>
    public AnyPortfolio WithCurrency(string value) {
        return new AnyPortfolio(_name, new FixedValue<string>(value));
    }

    /// <summary>Draws <c>currency</c> from <c>AnyValidCurrency()</c>, refined by <paramref name="configure" />.</summary>
    public AnyPortfolio WithCurrency(Func<AnyPattern, AnyPattern> configure) {
        return new AnyPortfolio(_name, configure(AnyValidCurrency()));
    }

    /// <summary>Produces one arbitrary <see cref="Portfolio" />.</summary>
    public Portfolio Generate() {
        return new Portfolio(_name.Generate(),
                             _currency.Generate());
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
