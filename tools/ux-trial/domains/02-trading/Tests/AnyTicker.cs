// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate Ticker --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.

using System;

using JustDummies;

namespace Trading.Domain;

/// <summary>
///     A generator of arbitrary <see cref="Ticker" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyTicker : IAny<Ticker> {

    private readonly IAny<string> _symbol;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyTicker()
        : this(symbol: AnyValidSymbol()) { }

    private static AnyPattern AnyValidSymbol() {
        // Ticker requires ^[A-Z]{1,5}(\.[A-Z])?$: the pattern is the invariant, so draw from it.
        return Any.StringMatching(@"^[A-Z]{1,5}(\.[A-Z])?$");
    }

    private AnyTicker(IAny<string> symbol) {
        _symbol = symbol;
    }

    /// <summary>Pins <c>symbol</c> to a fixed value.</summary>
    public AnyTicker WithSymbol(string value) {
        return new AnyTicker(new FixedValue<string>(value));
    }

    /// <summary>Draws <c>symbol</c> from <c>AnyValidSymbol()</c>, refined by <paramref name="configure" />.</summary>
    public AnyTicker WithSymbol(Func<AnyPattern, AnyPattern> configure) {
        return new AnyTicker(configure(AnyValidSymbol()));
    }

    /// <summary>Produces one arbitrary <see cref="Ticker" />.</summary>
    public Ticker Generate() {
        return new Ticker(_symbol.Generate());
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
