// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate Dividend --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.

using System;

using JustDummies;

namespace ParimutuelBetting.Domain;

/// <summary>
///     A generator of arbitrary <see cref="Dividend" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyDividend : IAny<Dividend> {

    private readonly IAny<Combination> _combination;
    private readonly IAny<decimal>     _amountPerUnit;
    private readonly IAny<int>         _winningUnits;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyDividend()
        : this(combination:   AnyValidCombination(),
               amountPerUnit: AnyValidAmountPerUnit(),
               winningUnits:  AnyValidWinningUnits()) { }

    private static AnyCombination AnyValidCombination() {
        return new AnyCombination();
    }

    private static AnyDecimal AnyValidAmountPerUnit() {
        return Any.Decimal().Positive().WithScale(2);
    }

    private static AnyInt32 AnyValidWinningUnits() {
        return Any.Int32().Positive();
    }

    private AnyDividend(IAny<Combination> combination,
                        IAny<decimal>     amountPerUnit,
                        IAny<int>         winningUnits) {
        _combination   = combination;
        _amountPerUnit = amountPerUnit;
        _winningUnits  = winningUnits;
    }

    /// <summary>Pins <c>combination</c> to a fixed value.</summary>
    public AnyDividend WithCombination(Combination value) {
        return new AnyDividend(new FixedValue<Combination>(value), _amountPerUnit, _winningUnits);
    }

    /// <summary>Draws <c>combination</c> from <c>AnyValidCombination()</c>, refined by <paramref name="configure" />.</summary>
    public AnyDividend WithCombination(Func<AnyCombination, AnyCombination> configure) {
        return new AnyDividend(configure(AnyValidCombination()), _amountPerUnit, _winningUnits);
    }

    /// <summary>Pins <c>amountPerUnit</c> to a fixed value.</summary>
    public AnyDividend WithAmountPerUnit(decimal value) {
        return new AnyDividend(_combination, new FixedValue<decimal>(value), _winningUnits);
    }

    /// <summary>Draws <c>amountPerUnit</c> from <c>AnyValidAmountPerUnit()</c>, refined by <paramref name="configure" />.</summary>
    public AnyDividend WithAmountPerUnit(Func<AnyDecimal, AnyDecimal> configure) {
        return new AnyDividend(_combination, configure(AnyValidAmountPerUnit()), _winningUnits);
    }

    /// <summary>Pins <c>winningUnits</c> to a fixed value.</summary>
    public AnyDividend WithWinningUnits(int value) {
        return new AnyDividend(_combination, _amountPerUnit, new FixedValue<int>(value));
    }

    /// <summary>Draws <c>winningUnits</c> from <c>AnyValidWinningUnits()</c>, refined by <paramref name="configure" />.</summary>
    public AnyDividend WithWinningUnits(Func<AnyInt32, AnyInt32> configure) {
        return new AnyDividend(_combination, _amountPerUnit, configure(AnyValidWinningUnits()));
    }

    /// <summary>Produces one arbitrary <see cref="Dividend" />.</summary>
    public Dividend Generate() {
        return new Dividend(_combination.Generate(),
                            _amountPerUnit.Generate(),
                            _winningUnits.Generate());
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
