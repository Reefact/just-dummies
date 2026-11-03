using JustDummies;

using ParimutuelBetting.Domain;

using Xunit;

namespace ParimutuelBetting.Tests.Regular;

public sealed class CombinationTests {

    [Theory]
    [InlineData(BetType.Win, 1)]
    [InlineData(BetType.Place, 1)]
    [InlineData(BetType.Quinella, 2)]
    [InlineData(BetType.Exacta, 2)]
    [InlineData(BetType.Trifecta, 3)]
    public void A_combination_with_the_arity_of_its_bet_type_is_accepted(BetType betType, int arity) {
        // Arrange
        List<HorseNumber> anyHorses = Any.ListOf(Arbitrary.HorseNumber()).WithCount(arity).Distinct().Generate();

        // Act
        Combination combination = new Combination(betType, anyHorses);

        // Assert
        Assert.Equal(betType, combination.BetType);
        Assert.Equal(arity, combination.Horses.Count);
    }

    [Theory]
    [InlineData(BetType.Win, 0)]
    [InlineData(BetType.Win, 2)]
    [InlineData(BetType.Place, 0)]
    [InlineData(BetType.Place, 3)]
    [InlineData(BetType.Quinella, 1)]
    [InlineData(BetType.Quinella, 3)]
    [InlineData(BetType.Exacta, 1)]
    [InlineData(BetType.Exacta, 3)]
    [InlineData(BetType.Trifecta, 2)]
    [InlineData(BetType.Trifecta, 4)]
    public void A_combination_with_the_wrong_number_of_horses_is_refused(BetType betType, int count) {
        // Arrange
        List<HorseNumber> anyHorses = Any.ListOf(Arbitrary.HorseNumber()).WithCount(count).Distinct().Generate();

        // Act & Assert
        Assert.Throws<ArgumentException>(() => new Combination(betType, anyHorses));
    }

    [Theory]
    [InlineData(BetType.Quinella)]
    [InlineData(BetType.Exacta)]
    public void The_same_horse_cannot_be_named_twice(BetType betType) {
        // Arrange
        HorseNumber anyHorse = Arbitrary.HorseNumber().Generate();

        // Act & Assert
        Assert.Throws<ArgumentException>(() => new Combination(betType, new[] { anyHorse, anyHorse }));
    }

    [Fact]
    public void A_trifecta_cannot_repeat_a_horse_even_apart() {
        // Arrange
        List<HorseNumber> anyTwoHorses = Any.ListOf(Arbitrary.HorseNumber()).WithCount(2).Distinct().Generate();
        HorseNumber[]     horses       = { anyTwoHorses[0], anyTwoHorses[1], anyTwoHorses[0] };

        // Act & Assert
        Assert.Throws<ArgumentException>(() => new Combination(BetType.Trifecta, horses));
    }

    [Fact]
    public void A_combination_cannot_name_a_null_horse() {
        // Arrange
        HorseNumber anyHorse = Arbitrary.HorseNumber().Generate();

        // Act & Assert
        Assert.Throws<ArgumentException>(() => new Combination(BetType.Exacta, new HorseNumber[] { anyHorse, null! }));
    }

    [Fact]
    public void A_combination_needs_a_list_of_horses() {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new Combination(BetType.Win, null!));
    }

    [Fact]
    public void An_undefined_bet_type_is_refused() {
        // Arrange
        HorseNumber anyHorse = Arbitrary.HorseNumber().Generate();

        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => new Combination((BetType)99, new[] { anyHorse }));
    }

    [Fact]
    public void A_quinella_is_sorted_so_that_5_and_3_equal_3_and_5() {
        // Arrange
        Combination fiveThree = new Combination(BetType.Quinella, new[] { new HorseNumber(5), new HorseNumber(3) });
        Combination threeFive = new Combination(BetType.Quinella, new[] { new HorseNumber(3), new HorseNumber(5) });

        // Assert
        Assert.Equal(threeFive, fiveThree);
        Assert.Equal(threeFive.GetHashCode(), fiveThree.GetHashCode());
        Assert.Equal(new[] { 3, 5 }, fiveThree.Horses.Select(horse => horse.Value));
    }

    [Theory]
    [InlineData(BetType.Exacta)]
    public void An_ordered_combination_keeps_the_order_it_was_given(BetType betType) {
        // Arrange
        Combination fiveThree = new Combination(betType, new[] { new HorseNumber(5), new HorseNumber(3) });
        Combination threeFive = new Combination(betType, new[] { new HorseNumber(3), new HorseNumber(5) });

        // Assert
        Assert.NotEqual(threeFive, fiveThree);
        Assert.Equal(new[] { 5, 3 }, fiveThree.Horses.Select(horse => horse.Value));
    }

    [Fact]
    public void A_trifecta_keeps_the_order_it_was_given() {
        // Arrange
        Combination combination = new Combination(BetType.Trifecta, new[] { new HorseNumber(9), new HorseNumber(2), new HorseNumber(5) });

        // Assert
        Assert.Equal(new[] { 9, 2, 5 }, combination.Horses.Select(horse => horse.Value));
    }

    [Fact]
    public void The_same_horses_in_two_bet_types_are_two_combinations() {
        // Arrange
        HorseNumber[] horses = { new HorseNumber(3), new HorseNumber(5) };

        Combination exacta   = new Combination(BetType.Exacta, horses);
        Combination quinella = new Combination(BetType.Quinella, horses);

        // Assert
        Assert.NotEqual(exacta, quinella);
        Assert.False(exacta.Equals((object)quinella));
        Assert.False(exacta.Equals(null));
    }

    [Fact]
    public void A_combination_is_not_changed_by_the_list_it_was_built_from() {
        // Arrange
        List<HorseNumber> horses     = new List<HorseNumber> { new HorseNumber(4), new HorseNumber(6) };
        Combination       combination = new Combination(BetType.Exacta, horses);

        // Act
        horses.Reverse();

        // Assert
        Assert.Equal(new[] { 4, 6 }, combination.Horses.Select(horse => horse.Value));
    }

    [Fact]
    public void A_combination_involves_a_horse_it_names() {
        // Arrange
        Combination combination = new Combination(BetType.Trifecta, new[] { new HorseNumber(9), new HorseNumber(2), new HorseNumber(5) });

        // Assert
        Assert.True(combination.Involves(new[] { new HorseNumber(1), new HorseNumber(2) }));
        Assert.False(combination.Involves(new[] { new HorseNumber(1), new HorseNumber(3) }));
        Assert.False(combination.Involves(Array.Empty<HorseNumber>()));
    }

    [Theory]
    [InlineData(BetType.Win, new[] { 4 }, "Win 4")]
    [InlineData(BetType.Place, new[] { 4 }, "Place 4")]
    [InlineData(BetType.Quinella, new[] { 7, 4 }, "Quinella 4/7")]
    [InlineData(BetType.Exacta, new[] { 7, 4 }, "Exacta 7-4")]
    [InlineData(BetType.Trifecta, new[] { 7, 4, 1 }, "Trifecta 7-4-1")]
    public void A_combination_prints_its_bet_type_and_its_horses(BetType betType, int[] numbers, string expected) {
        // Arrange
        Combination combination = new Combination(betType, numbers.Select(number => new HorseNumber(number)).ToArray());

        // Assert
        Assert.Equal(expected, combination.ToString());
    }

}
