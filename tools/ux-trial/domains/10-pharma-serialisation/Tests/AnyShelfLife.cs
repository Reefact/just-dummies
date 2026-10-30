// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate ShelfLife --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.

using System;

using JustDummies;

namespace Serialisation.Domain;

/// <summary>
///     A generator of arbitrary <see cref="ShelfLife" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyShelfLife : IAny<ShelfLife> {

    private readonly IAny<int> _months;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyShelfLife()
        : this(months: AnyValidMonths()) { }

    private static AnyInt32 AnyValidMonths() {
        // Verified by hand: the relational pattern guard requires 1 to 60 months.
        return Any.Int32().Between(1, 60);
    }

    private AnyShelfLife(IAny<int> months) {
        _months = months;
    }

    /// <summary>Pins <c>months</c> to a fixed value.</summary>
    public AnyShelfLife WithMonths(int value) {
        return new AnyShelfLife(new FixedValue<int>(value));
    }

    /// <summary>Draws <c>months</c> from <c>AnyValidMonths()</c>, refined by <paramref name="configure" />.</summary>
    public AnyShelfLife WithMonths(Func<AnyInt32, AnyInt32> configure) {
        return new AnyShelfLife(configure(AnyValidMonths()));
    }

    /// <summary>Produces one arbitrary <see cref="ShelfLife" />.</summary>
    public ShelfLife Generate() {
        return new ShelfLife(_months.Generate());
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
