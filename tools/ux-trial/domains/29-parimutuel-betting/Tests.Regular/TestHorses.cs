using ParimutuelBetting.Domain;

namespace ParimutuelBetting.Tests.Regular;

/// <summary>Short spellings for the numbers a test chose on purpose.</summary>
internal static class TestHorses {

    public static HorseNumber[] Horses(params int[] numbers) {
        return numbers.Select(number => new HorseNumber(number)).ToArray();
    }

    public static Combination Of(BetType betType, params int[] numbers) {
        return new Combination(betType, Horses(numbers));
    }

}
