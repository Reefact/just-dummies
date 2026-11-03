namespace ParimutuelBetting.Tests.Discovery;

public sealed class RaceResultTests {

    [Fact]
    public void A_result_keeps_the_finishing_order_and_the_scratched_horses() {
        // Arrange
        Race race = Horses.RaceOf(10);

        // Act
        RaceResult result = new RaceResult(race, Horses.Of(7, 2, 9), Horses.Of(4, 1));

        // Assert
        Assert.Same(race, result.Race);
        Assert.Equal(new[] { 7, 2, 9 }, result.FinishingOrder.Select(h => h.Value));
        Assert.Equal(new[] { 4, 1 }, result.Scratched.Select(h => h.Value).OrderByDescending(n => n));
    }

    [Fact]
    public void A_result_may_name_only_a_winner_and_no_scratched_horse() {
        Race race = Horses.RaceOf(6);

        RaceResult result = new RaceResult(race, Horses.Of(3), Horses.Of());

        Assert.Single(result.FinishingOrder);
        Assert.Empty(result.Scratched);
    }

    [Fact]
    public void A_result_does_not_follow_the_lists_it_was_built_from() {
        // Arrange
        Race              race      = Horses.RaceOf(10);
        List<HorseNumber> finishers = new List<HorseNumber>(Horses.Of(7, 2));
        List<HorseNumber> scratched = new List<HorseNumber>(Horses.Of(4));

        // Act
        RaceResult result = new RaceResult(race, finishers, scratched);
        finishers.Clear();
        scratched.Clear();

        // Assert
        Assert.Equal(2, result.FinishingOrder.Count);
        Assert.Single(result.Scratched);
    }

    [Fact]
    public void A_result_without_a_race_a_finishing_order_or_scratched_horses_is_refused() {
        Race race = Horses.RaceOf(8);

        Assert.Throws<ArgumentNullException>(() => new RaceResult(null!, Horses.Of(1), Horses.Of()));
        Assert.Throws<ArgumentNullException>(() => new RaceResult(race, null!, Horses.Of()));
        Assert.Throws<ArgumentNullException>(() => new RaceResult(race, Horses.Of(1), null!));
    }

    [Fact]
    public void A_result_names_at_least_the_winner() {
        Race race = Horses.RaceOf(8);

        Assert.Throws<ArgumentException>(() => new RaceResult(race, Horses.Of(), Horses.Of(2)));
    }

    [Fact]
    public void A_result_cannot_name_a_null_horse() {
        Race race = Horses.RaceOf(8);

        Assert.Throws<ArgumentException>(() => new RaceResult(race, new HorseNumber[] { new HorseNumber(1), null! }, Horses.Of()));
        Assert.Throws<ArgumentException>(() => new RaceResult(race, Horses.Of(1), new HorseNumber[] { null! }));
    }

    [Fact]
    public void A_horse_cannot_finish_twice_nor_be_scratched_twice() {
        Race race = Horses.RaceOf(8);

        Assert.Throws<ArgumentException>(() => new RaceResult(race, Horses.Of(3, 5, 3), Horses.Of()));
        Assert.Throws<ArgumentException>(() => new RaceResult(race, Horses.Of(1), Horses.Of(6, 6)));
    }

    [Fact]
    public void A_scratched_horse_cannot_finish() {
        Race race = Horses.RaceOf(8);

        Assert.Throws<ArgumentException>(() => new RaceResult(race, Horses.Of(3, 5), Horses.Of(5)));
    }

    [Fact]
    public void Every_horse_of_a_result_is_a_runner_of_its_race() {
        Race race = Horses.RaceOf(8);

        Assert.Throws<ArgumentException>(() => new RaceResult(race, Horses.Of(3, 9), Horses.Of()));
        Assert.Throws<ArgumentException>(() => new RaceResult(race, Horses.Of(3), Horses.Of(12)));
    }

    [Fact]
    public void The_winning_combination_of_a_win_bet_is_the_winner() {
        RaceResult result = new RaceResult(Horses.RaceOf(10), Horses.Of(7, 2, 9, 1), Horses.Of());

        Assert.Equal(Horses.Combination(BetType.Win, 7), result.WinningCombination(BetType.Win));
    }

    [Fact]
    public void The_winning_combination_of_a_quinella_is_the_first_two_in_either_order() {
        RaceResult result = new RaceResult(Horses.RaceOf(10), Horses.Of(7, 2, 9, 1), Horses.Of());

        Assert.Equal(Horses.Combination(BetType.Quinella, 2, 7), result.WinningCombination(BetType.Quinella));
    }

    [Fact]
    public void The_winning_combination_of_an_exacta_is_the_first_two_in_order() {
        RaceResult result = new RaceResult(Horses.RaceOf(10), Horses.Of(7, 2, 9, 1), Horses.Of());

        Assert.Equal(Horses.Combination(BetType.Exacta, 7, 2), result.WinningCombination(BetType.Exacta));
    }

    [Fact]
    public void The_winning_combination_of_a_trifecta_is_the_first_three_in_order() {
        RaceResult result = new RaceResult(Horses.RaceOf(10), Horses.Of(7, 2, 9, 1), Horses.Of());

        Assert.Equal(Horses.Combination(BetType.Trifecta, 7, 2, 9), result.WinningCombination(BetType.Trifecta));
    }

    [Fact]
    public void A_place_bet_has_no_single_winning_combination() {
        RaceResult result = new RaceResult(Horses.RaceOf(10), Horses.Of(7, 2, 9), Horses.Of());

        Assert.Throws<ArgumentException>(() => result.WinningCombination(BetType.Place));
    }

    [Theory]
    [InlineData(BetType.Quinella, 1)]
    [InlineData(BetType.Exacta, 1)]
    [InlineData(BetType.Trifecta, 2)]
    public void A_result_with_fewer_finishers_than_the_bet_type_names_has_no_winning_combination(BetType betType, int finishers) {
        // Arrange
        RaceResult result = new RaceResult(Horses.RaceOf(10), Horses.Of(Enumerable.Range(1, finishers).ToArray()), Horses.Of());

        // Assert
        Assert.Throws<InvalidOperationException>(() => result.WinningCombination(betType));
    }

}
