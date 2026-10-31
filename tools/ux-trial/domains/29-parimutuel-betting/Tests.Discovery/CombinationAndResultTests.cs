using System;

using JustDummies;

using ParimutuelBetting.Domain;

using Xunit;

using static ParimutuelBetting.Tests.Discovery.Tickets;

namespace ParimutuelBetting.Tests.Discovery;

public sealed class CombinationAndResultTests {

    [Fact]
    public void Two_quinella_selections_of_the_same_horses_are_the_same_combination() {
        // Arrange
        // Act
        Combination fiveThree = new Combination(BetType.Quinella, Horses(5, 3));
        Combination threeFive = new Combination(BetType.Quinella, Horses(3, 5));

        // Assert
        Assert.Equal(threeFive, fiveThree);
        Assert.Equal(Horses(3, 5), fiveThree.Horses);
    }

    [Fact]
    public void Two_exacta_selections_in_a_different_order_are_different_combinations() {
        // Arrange
        // Act
        Combination fiveThree = new Combination(BetType.Exacta, Horses(5, 3));
        Combination threeFive = new Combination(BetType.Exacta, Horses(3, 5));

        // Assert
        Assert.NotEqual(threeFive, fiveThree);
    }

    [Fact]
    public void A_combination_cannot_name_the_same_horse_twice() {
        // Arrange
        HorseNumber anyHorse = Any.HorseNumber().Generate();

        // Act
        Exception? refusal = Record.Exception(() => new Combination(BetType.Exacta, [anyHorse, anyHorse]));

        // Assert
        Assert.IsType<ArgumentException>(refusal);
    }

    [Fact]
    public void A_trifecta_combination_names_exactly_three_horses() {
        // Arrange
        HorseNumber anyHorse      = Any.HorseNumber().Generate();
        HorseNumber anyOtherHorse = Any.HorseNumber().WithValue(v => v.DifferentFrom(anyHorse.Value)).Generate();

        // Act
        Exception? refusal = Record.Exception(() => new Combination(BetType.Trifecta, [anyHorse, anyOtherHorse]));

        // Assert
        Assert.IsType<ArgumentException>(refusal);
    }

    [Fact]
    public void A_drawn_combination_names_as_many_horses_as_its_bet_type_requires() {
        // Arrange
        AnyCombination anyCombination = Any.Combination();

        // Act
        Combination anyTrifecta = anyCombination.WithBetType(BetType.Trifecta).Generate();

        // Assert
        Assert.Equal(3, anyTrifecta.Horses.Count);
    }

    [Fact]
    public void The_winning_exacta_is_the_first_two_finishers_in_order() {
        // Arrange
        Race       anyRace = Any.Race().WithFieldSize(f => f.GreaterThanOrEqualTo(7)).Generate();
        RaceResult result  = new RaceResult(anyRace, Horses(7, 2, 4), NobodyScratched);

        // Act
        Combination winner = result.WinningCombination(BetType.Exacta);

        // Assert
        Assert.Equal(new Combination(BetType.Exacta, Horses(7, 2)), winner);
    }

    [Fact]
    public void A_place_bet_has_no_single_winning_combination() {
        // Arrange
        RaceResult anyResult = Any.RaceResult().Generate();

        // Act
        Exception? refusal = Record.Exception(() => anyResult.WinningCombination(BetType.Place));

        // Assert
        Assert.IsType<ArgumentException>(refusal);
    }

    [Fact]
    public void A_result_with_a_single_finisher_cannot_decide_an_exacta() {
        // Arrange
        RaceResult anyOneFinisherResult = Any.RaceResult().WithFinishingOrder(f => f.WithCount(1)).Generate();

        // Act
        Exception? refusal = Record.Exception(() => anyOneFinisherResult.WinningCombination(BetType.Exacta));

        // Assert
        Assert.IsType<InvalidOperationException>(refusal);
    }

    [Fact]
    public void A_scratched_horse_cannot_appear_in_the_finishing_order() {
        // Arrange
        Race        anyRace         = Any.Race().Generate();
        HorseNumber anyRunner       = Any.HorseNumber().WithValue(v => v.LessThanOrEqualTo(anyRace.FieldSize)).Generate();

        // Act
        Exception? refusal = Record.Exception(() => new RaceResult(anyRace, [anyRunner], [anyRunner]));

        // Assert
        Assert.IsType<ArgumentException>(refusal);
    }

    [Fact]
    public void A_race_code_names_one_of_sixteen_races_at_most() {
        // Arrange
        int anyFieldSize = Any.Int32().Between(Race.MinimumFieldSize, HorseNumber.Maximum).Generate();

        // Act
        Exception? refusal = Record.Exception(() => new Race("LCH-R17", anyFieldSize));

        // Assert
        Assert.IsType<FormatException>(refusal);
    }

    [Fact]
    public void A_settlement_that_declares_dividends_carries_nothing_over() {
        // Arrange
        Dividend anyDividend = Any.Dividend().Generate();

        // Act
        Exception? refusal = Record.Exception(() => new Settlement(20m, 0m, 0m, [anyDividend], carryover: 1m));

        // Assert
        Assert.IsType<ArgumentException>(refusal);
    }

}
