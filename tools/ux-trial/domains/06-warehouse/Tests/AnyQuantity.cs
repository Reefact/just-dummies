// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate Quantity --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.

using System;

using JustDummies;

namespace Warehouse.Domain;

/// <summary>
///     A generator of arbitrary <see cref="Quantity" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyQuantity : IAny<Quantity> {

    private readonly IAny<int> _units;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyQuantity()
        : this(units: AnyValidUnits()) { }

    private static AnyInt32 AnyValidUnits() {
        return Any.Int32().Between(0, 10000);
    }

    private AnyQuantity(IAny<int> units) {
        _units = units;
    }

    /// <summary>Pins <c>units</c> to a fixed value.</summary>
    public AnyQuantity WithUnits(int value) {
        return new AnyQuantity(new FixedValue<int>(value));
    }

    /// <summary>Draws <c>units</c> from <c>AnyValidUnits()</c>, refined by <paramref name="configure" />.</summary>
    public AnyQuantity WithUnits(Func<AnyInt32, AnyInt32> configure) {
        return new AnyQuantity(configure(AnyValidUnits()));
    }

    /// <summary>Produces one arbitrary <see cref="Quantity" />.</summary>
    public Quantity Generate() {
        return new Quantity(_units.Generate());
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
