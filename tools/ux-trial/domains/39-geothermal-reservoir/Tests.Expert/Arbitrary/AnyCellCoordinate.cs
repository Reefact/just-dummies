// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate CellCoordinate --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.

using System;

using GeothermalReservoir.Domain;
using JustDummies;

namespace GeothermalReservoir.Tests.Expert.Arbitrary;

/// <summary>
///     A generator of arbitrary <see cref="CellCoordinate" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyCellCoordinate : IAny<CellCoordinate> {

    private readonly IAny<int> _i;
    private readonly IAny<int> _j;
    private readonly IAny<int> _k;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyCellCoordinate()
        : this(i: AnyValidI(),
               j: AnyValidJ(),
               k: AnyValidK()) { }

    private static AnyInt32 AnyValidI() {
        return Any.Int32().GreaterThanOrEqualTo(0);
    }

    private static AnyInt32 AnyValidJ() {
        return Any.Int32().GreaterThanOrEqualTo(0);
    }

    private static AnyInt32 AnyValidK() {
        return Any.Int32().GreaterThanOrEqualTo(0);
    }

    private AnyCellCoordinate(IAny<int> i,
                              IAny<int> j,
                              IAny<int> k) {
        _i = i;
        _j = j;
        _k = k;
    }

    /// <summary>Pins <c>i</c> to a fixed value.</summary>
    public AnyCellCoordinate WithI(int value) {
        return new AnyCellCoordinate(new FixedValue<int>(value), _j, _k);
    }

    /// <summary>Draws <c>i</c> from <c>AnyValidI()</c>, refined by <paramref name="configure" />.</summary>
    public AnyCellCoordinate WithI(Func<AnyInt32, AnyInt32> configure) {
        return new AnyCellCoordinate(configure(AnyValidI()), _j, _k);
    }

    /// <summary>Pins <c>j</c> to a fixed value.</summary>
    public AnyCellCoordinate WithJ(int value) {
        return new AnyCellCoordinate(_i, new FixedValue<int>(value), _k);
    }

    /// <summary>Draws <c>j</c> from <c>AnyValidJ()</c>, refined by <paramref name="configure" />.</summary>
    public AnyCellCoordinate WithJ(Func<AnyInt32, AnyInt32> configure) {
        return new AnyCellCoordinate(_i, configure(AnyValidJ()), _k);
    }

    /// <summary>Pins <c>k</c> to a fixed value.</summary>
    public AnyCellCoordinate WithK(int value) {
        return new AnyCellCoordinate(_i, _j, new FixedValue<int>(value));
    }

    /// <summary>Draws <c>k</c> from <c>AnyValidK()</c>, refined by <paramref name="configure" />.</summary>
    public AnyCellCoordinate WithK(Func<AnyInt32, AnyInt32> configure) {
        return new AnyCellCoordinate(_i, _j, configure(AnyValidK()));
    }

    /// <summary>Produces one arbitrary <see cref="CellCoordinate" />.</summary>
    public CellCoordinate Generate() {
        return new CellCoordinate(_i.Generate(),
                                  _j.Generate(),
                                  _k.Generate());
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
