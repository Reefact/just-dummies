// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate SlotPosition --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.

using System;

using JustDummies;

namespace VesselStowage.Domain;

/// <summary>
///     A generator of arbitrary <see cref="SlotPosition" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnySlotPosition : IAny<SlotPosition> {

    private readonly IAny<int> _bay;
    private readonly IAny<int> _row;
    private readonly IAny<int> _tier;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnySlotPosition()
        : this(bay:  AnyValidBay(),
               row:  AnyValidRow(),
               tier: AnyValidTier()) { }

    private static AnyInt32 AnyValidBay() {
        return Any.Int32().Between(1, 99);
    }

    private static AnyInt32 AnyValidRow() {
        return Any.Int32().Between(0, 20);
    }

    private static AnyInt32 AnyValidTier() {
        // Even tiers: 02-20 in the hold, 82-98 on deck.
        return Any.Int32().Between(2, 98).MultipleOf(2).Except(22, 24, 26, 28, 30, 32, 34, 36, 38, 40, 42, 44, 46, 48, 50, 52, 54, 56, 58, 60, 62, 64, 66, 68, 70, 72, 74, 76, 78, 80);
    }

    private AnySlotPosition(IAny<int> bay,
                            IAny<int> row,
                            IAny<int> tier) {
        _bay  = bay;
        _row  = row;
        _tier = tier;
    }

    /// <summary>Pins <c>bay</c> to a fixed value.</summary>
    public AnySlotPosition WithBay(int value) {
        return new AnySlotPosition(new FixedValue<int>(value), _row, _tier);
    }

    /// <summary>Draws <c>bay</c> from <c>AnyValidBay()</c>, refined by <paramref name="configure" />.</summary>
    public AnySlotPosition WithBay(Func<AnyInt32, AnyInt32> configure) {
        return new AnySlotPosition(configure(AnyValidBay()), _row, _tier);
    }

    /// <summary>Pins <c>row</c> to a fixed value.</summary>
    public AnySlotPosition WithRow(int value) {
        return new AnySlotPosition(_bay, new FixedValue<int>(value), _tier);
    }

    /// <summary>Draws <c>row</c> from <c>AnyValidRow()</c>, refined by <paramref name="configure" />.</summary>
    public AnySlotPosition WithRow(Func<AnyInt32, AnyInt32> configure) {
        return new AnySlotPosition(_bay, configure(AnyValidRow()), _tier);
    }

    /// <summary>Pins <c>tier</c> to a fixed value.</summary>
    public AnySlotPosition WithTier(int value) {
        return new AnySlotPosition(_bay, _row, new FixedValue<int>(value));
    }

    /// <summary>Draws <c>tier</c> from <c>AnyValidTier()</c>, refined by <paramref name="configure" />.</summary>
    public AnySlotPosition WithTier(Func<AnyInt32, AnyInt32> configure) {
        return new AnySlotPosition(_bay, _row, configure(AnyValidTier()));
    }

    /// <summary>Produces one arbitrary <see cref="SlotPosition" />.</summary>
    public SlotPosition Generate() {
        return new SlotPosition(_bay.Generate(),
                                _row.Generate(),
                                _tier.Generate());
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
