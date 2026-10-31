using System;
using System.Collections.Generic;
using System.Linq;

using ParimutuelBetting.Domain;

namespace ParimutuelBetting.Tests.Discovery;

/// <summary>Spells the load-bearing literals of a test: chosen horses and chosen stakes.</summary>
internal static class Tickets {

    public static IReadOnlyList<HorseNumber> Horses(params int[] numbers) {
        return numbers.Select(number => new HorseNumber(number)).ToArray();
    }

    public static Stake On(BetType betType, int units, params int[] horses) {
        return new Stake(new Combination(betType, Horses(horses)), units);
    }

    public static readonly IReadOnlyCollection<HorseNumber> NobodyScratched = Array.Empty<HorseNumber>();

}
