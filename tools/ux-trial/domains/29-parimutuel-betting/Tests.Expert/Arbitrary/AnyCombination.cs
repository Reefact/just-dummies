// Scaffolded by `any` (JustDummies), then reshaped by hand: the number of horses is not a parameter
// of its own, it follows the bet type (Arity), so it is derived inside Generate() rather than drawn.
// Do not `any generate Combination --force` over it.

using System;
using System.Collections.Generic;

using JustDummies;

namespace ParimutuelBetting.Domain;

/// <summary>A generator of arbitrary <see cref="Combination" /> values.</summary>
public sealed partial class AnyCombination : IAny<Combination> {

    private readonly IAny<BetType>  _betType;
    private readonly AnyHorseNumber _horse;

    public AnyCombination()
        : this(betType: AnyValidBetType(),
               horse:   AnyValidHorse()) { }

    private static AnyEnum<BetType> AnyValidBetType() {
        return Any.Enum<BetType>();
    }

    private static AnyHorseNumber AnyValidHorse() {
        return new AnyHorseNumber();
    }

    private AnyCombination(IAny<BetType> betType, AnyHorseNumber horse) {
        _betType = betType;
        _horse   = horse;
    }

    public AnyCombination WithBetType(BetType value) {
        return new AnyCombination(new FixedValue<BetType>(value), _horse);
    }

    public AnyCombination WithBetType(Func<AnyEnum<BetType>, AnyEnum<BetType>> configure) {
        return new AnyCombination(configure(AnyValidBetType()), _horse);
    }

    /// <summary>Refines the recipe every horse of the combination is drawn from.</summary>
    public AnyCombination WithHorses(Func<AnyHorseNumber, AnyHorseNumber> configure) {
        return new AnyCombination(_betType, configure(AnyValidHorse()));
    }

    /// <summary>A combination of this bet type naming only runners of the race: what a pool accepts.</summary>
    public AnyCombination For(BetType betType, Race race) {
        return WithBetType(betType).WithHorses(horse => horse.RunnerOf(race));
    }

    public Combination Generate() {
        BetType           betType = _betType.Generate();
        List<HorseNumber> horses  = Any.ListOf(_horse).WithCount(betType.Arity()).Distinct().Generate();

        return new Combination(betType, horses);
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
