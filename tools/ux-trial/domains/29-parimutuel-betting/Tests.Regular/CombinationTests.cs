using JustDummies;

using ParimutuelBetting.Domain;

using Xunit;

namespace ParimutuelBetting.Tests.Regular;

public sealed class CombinationTests {

    private static HorseNumber[] Horses(params int[] numbers) => numbers.Select(n => new HorseNumber(n)).ToArray();

    [Fact]
    public void A_quinella_names_the_same_combination_whatever_the_order_of_its_horses() {
        // Arrange
        Combination fiveThree = new Combination(BetType.Quinella, Horses(5, 3));

        // Act
        Combination threeFive = new Combination(BetType.Quinella, Horses(3, 5));

        // Assert
        Assert.Equal(fiveThree, threeFive);
    }

    [Fact]
    public void An_exacta_in_reverse_order_is_another_combination() {
        // Arrange
        Combination fiveThree = new Combination(BetType.Exacta, Horses(5, 3));

        // Act
        Combination threeFive = new Combination(BetType.Exacta, Horses(3, 5));

        // Assert
        Assert.NotEqual(fiveThree, threeFive);
    }

    [Theory]
    [InlineData(BetType.Win, 2)]
    [InlineData(BetType.Quinella, 3)]
    [InlineData(BetType.Trifecta, 2)]
    public void A_combination_names_exactly_as_many_horses_as_its_bet_type(BetType betType, int horseCount) {
        // Arrange
        List<HorseNumber> anyHorses = Any.ListOf(Arbitrary.HorseNumber()).WithCount(horseCount).Distinct().Generate();

        // Act
        Action create = () => new Combination(betType, anyHorses);

        // Assert
        Assert.Throws<ArgumentException>(create);
    }

    [Fact]
    public void A_combination_cannot_name_the_same_horse_twice() {
        // Arrange
        HorseNumber anyHorse = Arbitrary.HorseNumber().Generate();

        // Act
        Action create = () => new Combination(BetType.Exacta, [anyHorse, anyHorse]);

        // Assert
        Assert.Throws<ArgumentException>(create);
    }

}
