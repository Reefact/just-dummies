using ParimutuelBetting.Domain;

namespace ParimutuelBetting.Tests.Expert;

public sealed class CombinationTests {

    [Fact]
    public void A_quinella_ignores_the_order_its_two_horses_are_named_in() {
        // Arrange
        Combination fiveThree = new Combination(BetType.Quinella, [new HorseNumber(5), new HorseNumber(3)]);

        // Act
        Combination threeFive = new Combination(BetType.Quinella, [new HorseNumber(3), new HorseNumber(5)]);

        // Assert
        Assert.Equal(threeFive, fiveThree);
        Assert.Equal([new HorseNumber(3), new HorseNumber(5)], fiveThree.Horses);
    }

    [Fact]
    public void An_exacta_keeps_the_order_its_two_horses_are_named_in() {
        // Arrange
        Combination fiveThree = new Combination(BetType.Exacta, [new HorseNumber(5), new HorseNumber(3)]);

        // Act
        Combination threeFive = new Combination(BetType.Exacta, [new HorseNumber(3), new HorseNumber(5)]);

        // Assert
        Assert.NotEqual(threeFive, fiveThree);
    }

    [Fact]
    public void A_combination_cannot_name_the_same_horse_twice() {
        // Arrange
        HorseNumber anyHorse = Any.HorseNumber().Generate();

        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => new Combination(BetType.Exacta, [anyHorse, anyHorse]));

        // Assert
        Assert.Equal("horses", refusal.ParamName);
    }

    [Fact]
    public void A_trifecta_names_exactly_three_horses() {
        // Arrange
        HorseNumber[] anyTwoHorses = Any.ArrayOf(Any.HorseNumber()).WithCount(2).Distinct().Generate();

        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => new Combination(BetType.Trifecta, anyTwoHorses));

        // Assert
        Assert.Contains("exactly 3", refusal.Message);
    }

}
