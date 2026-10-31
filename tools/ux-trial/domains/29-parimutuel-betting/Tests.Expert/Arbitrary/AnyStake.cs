// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate Stake --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.

using System;

using JustDummies;

namespace ParimutuelBetting.Domain;

/// <summary>
///     A generator of arbitrary <see cref="Stake" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyStake : IAny<Stake> {

    private readonly IAny<Combination> _combination;
    private readonly IAny<int>         _units;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyStake()
        : this(combination: AnyValidCombination(),
               units:       AnyValidUnits()) { }

    private static AnyCombination AnyValidCombination() {
        return new AnyCombination();
    }

    private static AnyInt32 AnyValidUnits() {
        return Any.Int32().Between(1, Stake.MaximumUnits);
    }

    private AnyStake(IAny<Combination> combination,
                     IAny<int>         units) {
        _combination = combination;
        _units       = units;
    }

    /// <summary>Pins <c>combination</c> to a fixed value.</summary>
    public AnyStake WithCombination(Combination value) {
        return new AnyStake(new FixedValue<Combination>(value), _units);
    }

    /// <summary>Draws <c>combination</c> from <c>AnyValidCombination()</c>, refined by <paramref name="configure" />.</summary>
    public AnyStake WithCombination(Func<AnyCombination, AnyCombination> configure) {
        return new AnyStake(configure(AnyValidCombination()), _units);
    }

    /// <summary>Pins <c>units</c> to a fixed value.</summary>
    public AnyStake WithUnits(int value) {
        return new AnyStake(_combination, new FixedValue<int>(value));
    }

    /// <summary>Draws <c>units</c> from <c>AnyValidUnits()</c>, refined by <paramref name="configure" />.</summary>
    public AnyStake WithUnits(Func<AnyInt32, AnyInt32> configure) {
        return new AnyStake(_combination, configure(AnyValidUnits()));
    }

    /// <summary>Produces one arbitrary <see cref="Stake" />.</summary>
    public Stake Generate() {
        return new Stake(_combination.Generate(),
                         _units.Generate());
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
