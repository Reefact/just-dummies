using ParimutuelBetting.Domain;

using Xunit;

namespace ParimutuelBetting.Tests.Regular;

public sealed class RaceResultTests {

    private static HorseNumber[] Horses(params int[] numbers) => numbers.Select(n => new HorseNumber(n)).ToArray();

    [Fact]
    public void The_trifecta_is_won_by_the_first_three_finishers_in_order() {
        // Arrange
        Race       anyRace = Arbitrary.Race().WithFieldSize(f => f.GreaterThanOrEqualTo(9)).Generate();
        RaceResult result  = new RaceResult(anyRace, Horses(7, 2, 9, 4), []);

        // Act
        Combination winner = result.WinningCombination(BetType.Trifecta);

        // Assert
        Assert.Equal(new Combination(BetType.Trifecta, Horses(7, 2, 9)), winner);
    }

    [Fact]
    public void A_place_bet_has_no_single_winning_combination() {
        // Arrange
        Race       anyRace = Arbitrary.Race().WithFieldSize(f => f.GreaterThanOrEqualTo(3)).Generate();
        RaceResult result  = new RaceResult(anyRace, Horses(1, 2, 3), []);

        // Act
        Action winner = () => result.WinningCombination(BetType.Place);

        // Assert
        Assert.Throws<ArgumentException>(winner);
    }

    [Fact]
    public void An_exacta_cannot_be_decided_when_only_the_winner_is_official() {
        // Arrange
        Race       anyRace = Arbitrary.Race().Generate();
        RaceResult result  = new RaceResult(anyRace, Horses(1), []);

        // Act
        Action winner = () => result.WinningCombination(BetType.Exacta);

        // Assert
        Assert.Throws<InvalidOperationException>(winner);
    }

    [Fact]
    public void A_scratched_horse_cannot_finish() {
        // Arrange
        Race anyRace = Arbitrary.Race().WithFieldSize(f => f.GreaterThanOrEqualTo(4)).Generate();

        // Act
        Action create = () => new RaceResult(anyRace, Horses(3, 1, 4), Horses(4));

        // Assert
        Assert.Throws<ArgumentException>(create);
    }

}
