// Scaffolded by dum (JustDummies). This file is yours: read it, edit it, commit it.
// `dum generate Money --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.

using JustDummies;

namespace Shop.Domain;

/// <summary>
///     A generator of arbitrary <see cref="Money" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins it; to draw from an isolated
///     <c>Dummy.WithSeed(...)</c> context, pass that context's generators through the
///     <c>With…</c> overloads.
/// </summary>
public sealed partial class DummyMoney : IDummy<Money> {

    private readonly IDummy<decimal> _amount;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public DummyMoney()
        : this(amount: Dummy.Decimal()) { }

    private DummyMoney(IDummy<decimal> amount) {
        _amount = amount;
    }

    /// <summary>Pins <c>amount</c> to a fixed value.</summary>
    public DummyMoney WithAmount(decimal value) {
        return WithAmount(new FixedValue<decimal>(value));
    }

    /// <summary>Draws <c>amount</c> from <paramref name="generator" />.</summary>
    public DummyMoney WithAmount(IDummy<decimal> generator) {
        return new DummyMoney(generator);
    }

    /// <summary>Produces one arbitrary <see cref="Money" />.</summary>
    public Money Generate() {
        return new Money(_amount.Generate());
    }

    private sealed class FixedValue<TValue> : IDummy<TValue> {

        private readonly TValue _value;

        public FixedValue(TValue value) {
            _value = value;
        }

        public TValue Generate() {
            return _value;
        }

    }

}
