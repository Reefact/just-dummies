// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate Combination --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.

using System;
using System.Collections.Generic;

using JustDummies;

namespace ParimutuelBetting.Domain;

/// <summary>
///     A generator of arbitrary <see cref="Combination" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyCombination : IAny<Combination> {

    private readonly IAny<BetType>                    _betType;
    private readonly IAny<IReadOnlyList<HorseNumber>> _horses;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyCombination()
        : this(betType: AnyValidBetType(),
               horses:  AnyValidHorses()) { }

    private static AnyEnum<BetType> AnyValidBetType() {
        // The count of horses follows the bet type: Generate() sets it from Arity().
        return Any.Enum<BetType>();
    }

    private static AnyList<HorseNumber> AnyValidHorses() {
        // Distinct horses; their count is set from the bet type in Generate().
        return Any.ListOf(new AnyHorseNumber()).Distinct();
    }

    private AnyCombination(IAny<BetType>                    betType,
                           IAny<IReadOnlyList<HorseNumber>> horses) {
        _betType = betType;
        _horses  = horses;
    }

    /// <summary>Pins <c>betType</c> to a fixed value.</summary>
    public AnyCombination WithBetType(BetType value) {
        return new AnyCombination(new FixedValue<BetType>(value), _horses);
    }

    /// <summary>Draws <c>betType</c> from <c>AnyValidBetType()</c>, refined by <paramref name="configure" />.</summary>
    public AnyCombination WithBetType(Func<AnyEnum<BetType>, AnyEnum<BetType>> configure) {
        return new AnyCombination(configure(AnyValidBetType()), _horses);
    }

    /// <summary>Pins <c>horses</c> to a fixed value.</summary>
    public AnyCombination WithHorses(IReadOnlyList<HorseNumber> value) {
        return new AnyCombination(_betType, new FixedValue<IReadOnlyList<HorseNumber>>(value));
    }

    /// <summary>Draws <c>horses</c> from <c>AnyValidHorses()</c>, refined by <paramref name="configure" />.</summary>
    public AnyCombination WithHorses(Func<AnyList<HorseNumber>, AnyList<HorseNumber>> configure) {
        return new AnyCombination(_betType, configure(AnyValidHorses()));
    }

    /// <summary>Produces one arbitrary <see cref="Combination" />.</summary>
    public Combination Generate() {
        // A combination names exactly Arity() horses: the count is derived from the drawn bet type.
        // A pinned list is used as given; a drawn one (base or refined) takes that count.
        BetType                    betType = _betType.Generate();
        IReadOnlyList<HorseNumber> horses  = _horses is AnyList<HorseNumber> drawn
                                                 ? drawn.WithCount(betType.Arity()).Generate()
                                                 : _horses.Generate();

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
