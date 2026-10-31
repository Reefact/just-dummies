using System;
using System.Collections.Generic;

using JustDummies;

namespace ParimutuelBetting.Domain;

public sealed partial class AnyCombination {

    /// <summary>Draws distinct horses among the runners of a field of <paramref name="fieldSize" />.</summary>
    public AnyCombination WithHorsesAmong(int fieldSize) {
        IAny<HorseNumber> anyRunner = Any.Int32().Between(1, fieldSize).As(number => new HorseNumber(number));

        return new AnyCombination(_betType, Any.ListOf(anyRunner).Distinct());
    }

}
