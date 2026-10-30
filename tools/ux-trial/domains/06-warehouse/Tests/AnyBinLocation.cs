// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate BinLocation --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.

using System;

using JustDummies;

namespace Warehouse.Domain;

/// <summary>
///     A generator of arbitrary <see cref="BinLocation" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyBinLocation : IAny<BinLocation> {

    private readonly IAny<char> _aisle;
    private readonly IAny<int>  _rack;
    private readonly IAny<int>  _level;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyBinLocation()
        : this(aisle: AnyValidAisle(),
               rack:  AnyValidRack(),
               level: AnyValidLevel()) { }

    private static AnyChar AnyValidAisle() {
        return Any.Char().Alpha().InUpperCase();
    }

    private static AnyInt32 AnyValidRack() {
        return Any.Int32().Positive().LessThanOrEqualTo(99);
    }

    private static AnyInt32 AnyValidLevel() {
        // Fixed by hand: the scaffolder emitted Any.Int32() with no mark, missing the switch-expression guard (1..20).
        return Any.Int32().Between(1, 20);
    }

    private AnyBinLocation(IAny<char> aisle,
                           IAny<int>  rack,
                           IAny<int>  level) {
        _aisle = aisle;
        _rack  = rack;
        _level = level;
    }

    /// <summary>Pins <c>aisle</c> to a fixed value.</summary>
    public AnyBinLocation WithAisle(char value) {
        return new AnyBinLocation(new FixedValue<char>(value), _rack, _level);
    }

    /// <summary>Draws <c>aisle</c> from <c>AnyValidAisle()</c>, refined by <paramref name="configure" />.</summary>
    public AnyBinLocation WithAisle(Func<AnyChar, AnyChar> configure) {
        return new AnyBinLocation(configure(AnyValidAisle()), _rack, _level);
    }

    /// <summary>Pins <c>rack</c> to a fixed value.</summary>
    public AnyBinLocation WithRack(int value) {
        return new AnyBinLocation(_aisle, new FixedValue<int>(value), _level);
    }

    /// <summary>Draws <c>rack</c> from <c>AnyValidRack()</c>, refined by <paramref name="configure" />.</summary>
    public AnyBinLocation WithRack(Func<AnyInt32, AnyInt32> configure) {
        return new AnyBinLocation(_aisle, configure(AnyValidRack()), _level);
    }

    /// <summary>Pins <c>level</c> to a fixed value.</summary>
    public AnyBinLocation WithLevel(int value) {
        return new AnyBinLocation(_aisle, _rack, new FixedValue<int>(value));
    }

    /// <summary>Draws <c>level</c> from <c>AnyValidLevel()</c>, refined by <paramref name="configure" />.</summary>
    public AnyBinLocation WithLevel(Func<AnyInt32, AnyInt32> configure) {
        return new AnyBinLocation(_aisle, _rack, configure(AnyValidLevel()));
    }

    /// <summary>Produces one arbitrary <see cref="BinLocation" />.</summary>
    public BinLocation Generate() {
        return new BinLocation(_aisle.Generate(),
                               _rack.Generate(),
                               _level.Generate());
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
