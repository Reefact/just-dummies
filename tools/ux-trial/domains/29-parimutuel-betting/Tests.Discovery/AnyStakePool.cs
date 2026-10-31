// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate StakePool --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.

using System;

using JustDummies;

namespace ParimutuelBetting.Domain;

/// <summary>
///     A generator of arbitrary <see cref="StakePool" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyStakePool : IAny<StakePool> {

    private readonly IAny<Race>      _race;
    private readonly IAny<BetType>   _betType;
    private readonly IAny<decimal>   _unitStake;
    private readonly IAny<PoolRules> _rules;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyStakePool()
        : this(race:      AnyValidRace(),
               betType:   AnyValidBetType(),
               unitStake: AnyValidUnitStake(),
               rules:     AnyValidRules()) { }

    private static AnyRace AnyValidRace() {
        // Four runners at least: enough for any arity (3) and more than any number of places paid (3).
        return new AnyRace().WithFieldSize(f => f.GreaterThanOrEqualTo(4));
    }

    private static AnyEnum<BetType> AnyValidBetType() {
        // Every bet type fits, since the race base has at least four runners.
        return Any.Enum<BetType>();
    }

    private static AnyDecimal AnyValidUnitStake() {
        return Any.Decimal().Between(0.01m, 100m).WithScale(2);
    }

    private static AnyPoolRules AnyValidRules() {
        // Every rule set fits: the race base has more runners than any number of places paid.
        return new AnyPoolRules();
    }

    private AnyStakePool(IAny<Race>      race,
                         IAny<BetType>   betType,
                         IAny<decimal>   unitStake,
                         IAny<PoolRules> rules) {
        _race      = race;
        _betType   = betType;
        _unitStake = unitStake;
        _rules     = rules;
    }

    /// <summary>Pins <c>race</c> to a fixed value.</summary>
    public AnyStakePool WithRace(Race value) {
        return new AnyStakePool(new FixedValue<Race>(value), _betType, _unitStake, _rules);
    }

    /// <summary>Draws <c>race</c> from <c>AnyValidRace()</c>, refined by <paramref name="configure" />.</summary>
    public AnyStakePool WithRace(Func<AnyRace, AnyRace> configure) {
        return new AnyStakePool(configure(AnyValidRace()), _betType, _unitStake, _rules);
    }

    /// <summary>Pins <c>betType</c> to a fixed value.</summary>
    public AnyStakePool WithBetType(BetType value) {
        return new AnyStakePool(_race, new FixedValue<BetType>(value), _unitStake, _rules);
    }

    /// <summary>Draws <c>betType</c> from <c>AnyValidBetType()</c>, refined by <paramref name="configure" />.</summary>
    public AnyStakePool WithBetType(Func<AnyEnum<BetType>, AnyEnum<BetType>> configure) {
        return new AnyStakePool(_race, configure(AnyValidBetType()), _unitStake, _rules);
    }

    /// <summary>Pins <c>unitStake</c> to a fixed value.</summary>
    public AnyStakePool WithUnitStake(decimal value) {
        return new AnyStakePool(_race, _betType, new FixedValue<decimal>(value), _rules);
    }

    /// <summary>Draws <c>unitStake</c> from <c>AnyValidUnitStake()</c>, refined by <paramref name="configure" />.</summary>
    public AnyStakePool WithUnitStake(Func<AnyDecimal, AnyDecimal> configure) {
        return new AnyStakePool(_race, _betType, configure(AnyValidUnitStake()), _rules);
    }

    /// <summary>Pins <c>rules</c> to a fixed value.</summary>
    public AnyStakePool WithRules(PoolRules value) {
        return new AnyStakePool(_race, _betType, _unitStake, new FixedValue<PoolRules>(value));
    }

    /// <summary>Draws <c>rules</c> from <c>AnyValidRules()</c>, refined by <paramref name="configure" />.</summary>
    public AnyStakePool WithRules(Func<AnyPoolRules, AnyPoolRules> configure) {
        return new AnyStakePool(_race, _betType, _unitStake, configure(AnyValidRules()));
    }

    /// <summary>Produces one arbitrary <see cref="StakePool" />.</summary>
    public StakePool Generate() {
        return new StakePool(_race.Generate(),
                             _betType.Generate(),
                             _unitStake.Generate(),
                             _rules.Generate());
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
