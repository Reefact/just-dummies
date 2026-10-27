// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate Sku --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.

using System;

using JustDummies;
using Shop.Domain;

namespace Shop.Tests;

/// <summary>
///     A generator of arbitrary <see cref="Sku" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnySku : IAny<Sku> {

    private readonly IAny<string> _value;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnySku()
        : this(value: AnyValidValue()) { }

    private static AnyPattern AnyValidValue() {
        // Sku demands AAA-9999.
        return Any.StringMatching(@"[A-Z]{3}-\d{4}");
    }

    private AnySku(IAny<string> value) {
        _value = value;
    }

    /// <summary>Pins <c>value</c> to a fixed value.</summary>
    public AnySku WithValue(string value) {
        return new AnySku(new FixedValue<string>(value));
    }

    /// <summary>Draws <c>value</c> from <c>AnyValidValue()</c>, refined by <paramref name="configure" />.</summary>
    public AnySku WithValue(Func<AnyPattern, AnyPattern> configure) {
        return new AnySku(configure(AnyValidValue()));
    }

    /// <summary>Produces one arbitrary <see cref="Sku" />.</summary>
    public Sku Generate() {
        return new Sku(_value.Generate());
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
