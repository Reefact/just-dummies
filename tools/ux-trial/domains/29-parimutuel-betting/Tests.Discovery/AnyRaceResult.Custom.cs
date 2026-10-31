using System;

using JustDummies;

namespace ParimutuelBetting.Domain;

public sealed partial class AnyRaceResult {

    /// <summary>
    ///     A result of <paramref name="race" />: up to three distinct runners finish (enough for any
    ///     bet type), nobody is scratched.
    /// </summary>
    public AnyRaceResult Of(Race race) {
        IAny<HorseNumber> anyRunner = Any.Int32().Between(1, race.FieldSize).As(number => new HorseNumber(number));
        AnyList<HorseNumber> anyFinishingOrder = Any.ListOf(anyRunner).WithCount(Math.Min(3, race.FieldSize)).Distinct();

        return new AnyRaceResult(new FixedValue<Race>(race), anyFinishingOrder, _scratched);
    }

}
