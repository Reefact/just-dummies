namespace ParimutuelBetting.Tests.Discovery;

public sealed class CombinationTests {

    [Theory]
    [InlineData(BetType.Win, 1)]
    [InlineData(BetType.Place, 1)]
    [InlineData(BetType.Quinella, 2)]
    [InlineData(BetType.Exacta, 2)]
    [InlineData(BetType.Trifecta, 3)]
    public void Each_bet_type_names_its_own_number_of_horses(BetType betType, int arity) {
        Assert.Equal(arity, betType.Arity());
    }

    [Fact]
    public void Only_a_quinella_ignores_the_order_of_its_horses() {
        Assert.All(Enum.GetValues<BetType>().Where(b => b != BetType.Quinella), b => Assert.True(b.IsOrdered()));
        Assert.False(BetType.Quinella.IsOrdered());
    }

    [Fact]
    public void An_unknown_bet_type_has_no_rules() {
        BetType unknown = (BetType)99;

        Assert.Throws<ArgumentOutOfRangeException>(() => unknown.Arity());
        Assert.Throws<ArgumentOutOfRangeException>(() => unknown.IsOrdered());
    }

    [Fact]
    public void A_generated_combination_names_as_many_horses_as_its_bet_type_asks() {
        // Arrange
        Combination anyCombination = Any.Combination().Generate();

        // Assert
        Assert.Equal(anyCombination.BetType.Arity(), anyCombination.Horses.Count);
    }

    [Fact]
    public void An_unordered_combination_is_sorted_so_that_five_three_is_three_five() {
        // Arrange
        Combination fiveThree = Horses.Combination(BetType.Quinella, 5, 3);
        Combination threeFive = Horses.Combination(BetType.Quinella, 3, 5);

        // Assert
        Assert.Equal(threeFive, fiveThree);
        Assert.Equal(threeFive.GetHashCode(), fiveThree.GetHashCode());
        Assert.Equal(new[] { 3, 5 }, fiveThree.Horses.Select(h => h.Value));
    }

    [Fact]
    public void An_ordered_combination_keeps_the_order_it_was_given() {
        // Arrange
        Combination fiveThree = Horses.Combination(BetType.Exacta, 5, 3);
        Combination threeFive = Horses.Combination(BetType.Exacta, 3, 5);

        // Assert
        Assert.NotEqual(threeFive, fiveThree);
        Assert.Equal(new[] { 5, 3 }, fiveThree.Horses.Select(h => h.Value));
    }

    [Fact]
    public void The_same_horses_under_another_bet_type_are_another_combination() {
        Assert.NotEqual(Horses.Combination(BetType.Exacta, 3, 5), Horses.Combination(BetType.Quinella, 3, 5));
    }

    [Fact]
    public void A_combination_does_not_follow_the_list_it_was_built_from() {
        // Arrange
        List<HorseNumber> source = new List<HorseNumber>(Horses.Of(4, 2, 9));

        // Act
        Combination trifecta = new Combination(BetType.Trifecta, source);
        source[0] = new HorseNumber(20);

        // Assert
        Assert.Equal(new[] { 4, 2, 9 }, trifecta.Horses.Select(h => h.Value));
    }

    [Fact]
    public void A_combination_without_a_list_of_horses_is_refused() {
        BetType anyBetType = Any.Enum<BetType>().Generate();

        Assert.Throws<ArgumentNullException>(() => new Combination(anyBetType, null!));
    }

    [Fact]
    public void A_combination_with_an_undefined_bet_type_is_refused() {
        Assert.Throws<ArgumentOutOfRangeException>(() => new Combination((BetType)42, Horses.Of(1)));
    }

    [Fact]
    public void A_combination_naming_a_null_horse_is_refused() {
        HorseNumber[] horses = { new HorseNumber(2), null! };

        Assert.Throws<ArgumentException>(() => new Combination(BetType.Exacta, horses));
    }

    [Theory]
    [InlineData(BetType.Win, 0)]
    [InlineData(BetType.Win, 2)]
    [InlineData(BetType.Place, 2)]
    [InlineData(BetType.Quinella, 1)]
    [InlineData(BetType.Exacta, 3)]
    [InlineData(BetType.Trifecta, 2)]
    [InlineData(BetType.Trifecta, 4)]
    public void A_combination_names_exactly_as_many_horses_as_its_bet_type_needs(BetType betType, int count) {
        HorseNumber[] horses = Enumerable.Range(1, count).Select(n => new HorseNumber(n)).ToArray();

        Assert.Throws<ArgumentException>(() => new Combination(betType, horses));
    }

    [Theory]
    [InlineData(BetType.Quinella)]
    [InlineData(BetType.Exacta)]
    public void A_combination_cannot_name_the_same_horse_twice(BetType betType) {
        int anyNumber = Any.Int32().Between(1, HorseNumber.Maximum).Generate();

        Assert.Throws<ArgumentException>(() => new Combination(betType, Horses.Of(anyNumber, anyNumber)));
    }

    [Fact]
    public void A_combination_involves_a_horse_it_names_and_no_other() {
        // Arrange
        Combination trifecta = Horses.Combination(BetType.Trifecta, 2, 7, 11);

        // Assert
        Assert.True(trifecta.Involves(Horses.Of(7)));
        Assert.True(trifecta.Involves(Horses.Of(1, 24, 11)));
        Assert.False(trifecta.Involves(Horses.Of(1, 3, 24)));
        Assert.False(trifecta.Involves(Horses.Of()));
    }

    [Theory]
    [InlineData(BetType.Win, new[] { 4 }, "Win 4")]
    [InlineData(BetType.Place, new[] { 4 }, "Place 4")]
    [InlineData(BetType.Quinella, new[] { 9, 4 }, "Quinella 4/9")]
    [InlineData(BetType.Exacta, new[] { 9, 4 }, "Exacta 9-4")]
    [InlineData(BetType.Trifecta, new[] { 9, 4, 1 }, "Trifecta 9-4-1")]
    public void A_combination_prints_its_horses_in_the_notation_of_its_bet_type(BetType betType, int[] numbers, string expected) {
        Assert.Equal(expected, Horses.Combination(betType, numbers).ToString());
    }

}
