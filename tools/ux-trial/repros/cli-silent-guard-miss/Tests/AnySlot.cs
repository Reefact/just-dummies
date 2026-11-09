// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate Slot --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.

using System;

using JustDummies;

namespace GuardMiss.Domain;

/// <summary>
///     A generator of arbitrary <see cref="Slot" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnySlot : IAny<Slot> {

    private readonly IAny<int> _aisle;
    private readonly IAny<int> _rack;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnySlot()
        : this(aisle: AnyValidAisle(),
               rack:  AnyValidRack()) { }

    private static AnyInt32 AnyValidAisle() {
        return Any.Int32();
    }

    private static AnyInt32 AnyValidRack() {
        return Any.Int32().Positive();
    }

    private AnySlot(IAny<int> aisle,
                    IAny<int> rack) {
        _aisle = aisle;
        _rack  = rack;
    }

    /// <summary>Pins <c>aisle</c> to a fixed value.</summary>
    public AnySlot WithAisle(int value) {
        return new AnySlot(new FixedValue<int>(value), _rack);
    }

    /// <summary>Draws <c>aisle</c> from <c>AnyValidAisle()</c>, refined by <paramref name="configure" />.</summary>
    public AnySlot WithAisle(Func<AnyInt32, AnyInt32> configure) {
        return new AnySlot(configure(AnyValidAisle()), _rack);
    }

    /// <summary>Pins <c>rack</c> to a fixed value.</summary>
    public AnySlot WithRack(int value) {
        return new AnySlot(_aisle, new FixedValue<int>(value));
    }

    /// <summary>Draws <c>rack</c> from <c>AnyValidRack()</c>, refined by <paramref name="configure" />.</summary>
    public AnySlot WithRack(Func<AnyInt32, AnyInt32> configure) {
        return new AnySlot(_aisle, configure(AnyValidRack()));
    }

    /// <summary>Produces one arbitrary <see cref="Slot" />.</summary>
    public Slot Generate() {
        return new Slot(_aisle.Generate(),
                        _rack.Generate());
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
