using JustDummies;

using ParimutuelBetting.Domain;

using Xunit;

using static ParimutuelBetting.Tests.Regular.TestHorses;

namespace ParimutuelBetting.Tests.Regular;

public sealed class RaceResultTests {

    [Fact]
    public void A_result_keeps_the_race_the_finishing_order_and_the_scratched_horses() {
        // Arrange
        Race race = Arbitrary.Race().WithFieldSize(10).Generate();

        // Act
        RaceResult result = new RaceResult(race, Horses(4, 2, 9), Horses(7, 1));

        // Assert
        Assert.Same(race, result.Race);
        Assert.Equal(Horses(4, 2, 9), result.FinishingOrder);
        Assert.Equal(Horses(7, 1), result.Scratched);
    }

    [Fact]
    public void A_result_may_name_only_the_winner_and_no_scratching() {
        // Arrange
        Race race = Arbitrary.Race().WithFieldSize(10).Generate();

        // Act
        RaceResult result = new RaceResult(race, Horses(4), Array.Empty<HorseNumber>());

        // Assert
        Assert.Single(result.FinishingOrder);
        Assert.Empty(result.Scratched);
    }

    [Fact]
    public void A_result_without_a_finisher_is_refused() {
        // Arrange
        Race race = Arbitrary.Race().Generate();

        // Act & Assert
        Assert.Throws<ArgumentException>(() => new RaceResult(race, Array.Empty<HorseNumber>(), Array.Empty<HorseNumber>()));
    }

    [Fact]
    public void A_result_needs_a_race_and_both_lists() {
        // Arrange
        Race race = Arbitrary.Race().Generate();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new RaceResult(null!, Horses(1), Array.Empty<HorseNumber>()));
        Assert.Throws<ArgumentNullException>(() => new RaceResult(race, null!, Array.Empty<HorseNumber>()));
        Assert.Throws<ArgumentNullException>(() => new RaceResult(race, Horses(1), null!));
    }

    [Fact]
    public void A_null_horse_cannot_finish_nor_be_scratched() {
        // Arrange
        Race race = Arbitrary.Race().WithFieldSize(10).Generate();

        // Act & Assert
        Assert.Throws<ArgumentException>(() => new RaceResult(race, new HorseNumber[] { new HorseNumber(1), null! }, Array.Empty<HorseNumber>()));
        Assert.Throws<ArgumentException>(() => new RaceResult(race, Horses(1), new HorseNumber[] { null! }));
    }

    [Fact]
    public void A_horse_cannot_finish_twice() {
        // Arrange
        Race race = Arbitrary.Race().WithFieldSize(10).Generate();

        // Act & Assert
        Assert.Throws<ArgumentException>(() => new RaceResult(race, Horses(3, 5, 3), Array.Empty<HorseNumber>()));
    }

    [Fact]
    public void A_horse_cannot_be_scratched_twice() {
        // Arrange
        Race race = Arbitrary.Race().WithFieldSize(10).Generate();

        // Act & Assert
        Assert.Throws<ArgumentException>(() => new RaceResult(race, Horses(1), Horses(6, 6)));
    }

    [Fact]
    public void A_finisher_must_be_a_runner_of_the_race() {
        // Arrange
        Race race = Arbitrary.Race().WithFieldSize(10).Generate();

        // Act & Assert
        Assert.Throws<ArgumentException>(() => new RaceResult(race, Horses(3, 11), Array.Empty<HorseNumber>()));
    }

    [Fact]
    public void A_scratched_horse_must_be_a_runner_of_the_race() {
        // Arrange
        Race race = Arbitrary.Race().WithFieldSize(10).Generate();

        // Act & Assert
        Assert.Throws<ArgumentException>(() => new RaceResult(race, Horses(3), Horses(12)));
    }

    [Fact]
    public void A_scratched_horse_cannot_finish() {
        // Arrange
        Race race = Arbitrary.Race().WithFieldSize(10).Generate();

        // Act & Assert
        Assert.Throws<ArgumentException>(() => new RaceResult(race, Horses(3, 5), Horses(8, 5)));
    }

    [Fact]
    public void The_result_is_not_changed_by_the_lists_it_was_built_from() {
        // Arrange
        Race              race      = Arbitrary.Race().WithFieldSize(10).Generate();
        List<HorseNumber> finishers = Horses(4, 2).ToList();
        List<HorseNumber> scratched = Horses(7).ToList();
        RaceResult        result    = new RaceResult(race, finishers, scratched);

        // Act
        finishers.Clear();
        scratched.Clear();

        // Assert
        Assert.Equal(2, result.FinishingOrder.Count);
        Assert.Single(result.Scratched);
    }

    [Theory]
    [InlineData(BetType.Win, "Win 4")]
    [InlineData(BetType.Quinella, "Quinella 2/4")]
    [InlineData(BetType.Exacta, "Exacta 4-2")]
    [InlineData(BetType.Trifecta, "Trifecta 4-2-9")]
    public void The_winning_combination_takes_the_first_finishers_as_many_as_the_arity(BetType betType, string expected) {
        // Arrange
        RaceResult result = Arbitrary.RaceResult().WithAtLeastRunners(9).WithFinishingOrder(Horses(4, 2, 9, 1)).WithScratched(Horses(7)).Generate();

        // Act
        Combination winning = result.WinningCombination(betType);

        // Assert
        Assert.Equal(expected, winning.ToString());
    }

    [Fact]
    public void A_place_bet_has_no_single_winning_combination() {
        // Arrange
        RaceResult anyResult = Arbitrary.RaceResult().Generate();

        // Act & Assert
        Assert.Throws<ArgumentException>(() => anyResult.WinningCombination(BetType.Place));
    }

    [Theory]
    [InlineData(BetType.Exacta)]
    [InlineData(BetType.Quinella)]
    [InlineData(BetType.Trifecta)]
    public void A_result_with_fewer_finishers_than_the_arity_has_no_winning_combination(BetType betType) {
        // Arrange
        RaceResult result = Arbitrary.RaceResult().WithAtLeastRunners(4).WithFinishingOrder(Horses(4)).Generate();

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => result.WinningCombination(betType));
    }

    [Fact]
    public void A_trifecta_needs_three_finishers_not_two() {
        // Arrange
        RaceResult result = Arbitrary.RaceResult().WithAtLeastRunners(6).WithFinishingOrder(Horses(4, 6)).Generate();

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => result.WinningCombination(BetType.Trifecta));
    }

}
