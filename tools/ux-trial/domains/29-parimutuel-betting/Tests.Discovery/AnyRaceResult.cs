// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate RaceResult --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.

using System;
using System.Collections.Generic;

using JustDummies;

namespace ParimutuelBetting.Domain;

/// <summary>
///     A generator of arbitrary <see cref="RaceResult" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyRaceResult : IAny<RaceResult> {

    private readonly IAny<Race>                             _race;
    private readonly IAny<IReadOnlyList<HorseNumber>>       _finishingOrder;
    private readonly IAny<IReadOnlyCollection<HorseNumber>> _scratched;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyRaceResult()
        : this(race:           AnyValidRace(),
               finishingOrder: AnyValidFinishingOrder(),
               scratched:      AnyValidScratched()) { }

    private static AnyRace AnyValidRace() {
        return new AnyRace();
    }

    private static AnyList<HorseNumber> AnyValidFinishingOrder() {
        // Horses 1 and 2 run in every race (two runners at least), so this order is valid by construction.
        return Any.ListOf(Any.Int32().Between(1, Race.MinimumFieldSize).As(number => new HorseNumber(number))).WithCountBetween(1, Race.MinimumFieldSize).Distinct();
    }

    private static AnyList<HorseNumber> AnyValidScratched() {
        // No horse scratched by default: a scratching is a fact a test states, not a dummy.
        return Any.ListOf(new AnyHorseNumber()).Empty();
    }

    private AnyRaceResult(IAny<Race>                             race,
                          IAny<IReadOnlyList<HorseNumber>>       finishingOrder,
                          IAny<IReadOnlyCollection<HorseNumber>> scratched) {
        _race           = race;
        _finishingOrder = finishingOrder;
        _scratched      = scratched;
    }

    /// <summary>Pins <c>race</c> to a fixed value.</summary>
    public AnyRaceResult WithRace(Race value) {
        return new AnyRaceResult(new FixedValue<Race>(value), _finishingOrder, _scratched);
    }

    /// <summary>Draws <c>race</c> from <c>AnyValidRace()</c>, refined by <paramref name="configure" />.</summary>
    public AnyRaceResult WithRace(Func<AnyRace, AnyRace> configure) {
        return new AnyRaceResult(configure(AnyValidRace()), _finishingOrder, _scratched);
    }

    /// <summary>Pins <c>finishingOrder</c> to a fixed value.</summary>
    public AnyRaceResult WithFinishingOrder(IReadOnlyList<HorseNumber> value) {
        return new AnyRaceResult(_race, new FixedValue<IReadOnlyList<HorseNumber>>(value), _scratched);
    }

    /// <summary>Draws <c>finishingOrder</c> from <c>AnyValidFinishingOrder()</c>, refined by <paramref name="configure" />.</summary>
    public AnyRaceResult WithFinishingOrder(Func<AnyList<HorseNumber>, AnyList<HorseNumber>> configure) {
        return new AnyRaceResult(_race, configure(AnyValidFinishingOrder()), _scratched);
    }

    /// <summary>Pins <c>scratched</c> to a fixed value.</summary>
    public AnyRaceResult WithScratched(IReadOnlyCollection<HorseNumber> value) {
        return new AnyRaceResult(_race, _finishingOrder, new FixedValue<IReadOnlyCollection<HorseNumber>>(value));
    }

    /// <summary>Draws <c>scratched</c> from <c>AnyValidScratched()</c>, refined by <paramref name="configure" />.</summary>
    public AnyRaceResult WithScratched(Func<AnyList<HorseNumber>, AnyList<HorseNumber>> configure) {
        return new AnyRaceResult(_race, _finishingOrder, configure(AnyValidScratched()));
    }

    /// <summary>Produces one arbitrary <see cref="RaceResult" />.</summary>
    public RaceResult Generate() {
        return new RaceResult(_race.Generate(),
                              _finishingOrder.Generate(),
                              _scratched.Generate());
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
