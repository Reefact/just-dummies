using ParimutuelBetting.Domain;

namespace ParimutuelBetting.Tests.Expert;

public sealed class RaceResultTests {

    // The race itself is a dummy here: only its card has to carry the horses the result names.
    private static readonly HorseNumber Seven = new HorseNumber(7);
    private static readonly HorseNumber Three = new HorseNumber(3);
    private static readonly HorseNumber Five  = new HorseNumber(5);

    [Fact]
    public void The_exacta_winning_combination_is_the_first_two_finishers_in_order() {
        // Arrange
        Race       anyRace = Recipes.AnyRaceWithAtLeast(7);
        RaceResult result  = new RaceResult(anyRace, [Seven, Three, Five], []);

        // Act
        Combination winning = result.WinningCombination(BetType.Exacta);

        // Assert
        Assert.Equal([Seven, Three], winning.Horses);
    }

    [Fact]
    public void The_trifecta_winning_combination_is_the_first_three_finishers_in_order() {
        // Arrange
        Race       anyRace = Recipes.AnyRaceWithAtLeast(7);
        RaceResult result  = new RaceResult(anyRace, [Seven, Three, Five], []);

        // Act
        Combination winning = result.WinningCombination(BetType.Trifecta);

        // Assert
        Assert.Equal([Seven, Three, Five], winning.Horses);
    }

    [Fact]
    public void The_quinella_winning_combination_names_the_first_two_finishers_in_ascending_order() {
        // Arrange
        Race       anyRace = Recipes.AnyRaceWithAtLeast(7);
        RaceResult result  = new RaceResult(anyRace, [Seven, Three, Five], []);

        // Act
        Combination winning = result.WinningCombination(BetType.Quinella);

        // Assert
        Assert.Equal([Three, Seven], winning.Horses);
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
    public void A_trifecta_cannot_be_decided_when_fewer_than_three_horses_finished() {
        // Arrange
        Race         anyRace          = Recipes.AnyRaceWithAtLeast(2);
        HorseNumber[] anyTwoFinishers = Recipes.DistinctRunnersOf(anyRace, 2);
        RaceResult   twoFinishers     = new RaceResult(anyRace, anyTwoFinishers, []);

        // Act
        Exception? refusal = Record.Exception(() => twoFinishers.WinningCombination(BetType.Trifecta));

        // Assert
        Assert.IsType<InvalidOperationException>(refusal);
    }

    [Fact]
    public void A_scratched_horse_cannot_appear_in_the_finishing_order() {
        // Arrange
        Race          anyRace    = Recipes.AnyRaceWithAtLeast(2);
        HorseNumber[] anyRunners = Recipes.DistinctRunnersOf(anyRace, 2);
        HorseNumber   anyFinisherAlsoScratched = anyRunners[1];

        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => new RaceResult(anyRace, anyRunners, [anyFinisherAlsoScratched]));

        // Assert
        Assert.Equal("finishingOrder", refusal.ParamName);
    }

    [Fact]
    public void A_result_can_only_name_declared_runners() {
        // Arrange
        Race        anyRace      = Any.Race().WithFieldSize(size => size.LessThan(HorseNumber.Maximum)).Generate();
        HorseNumber anyNonRunner = Any.HorseNumber().WithValue(value => value.GreaterThan(anyRace.FieldSize)).Generate();

        // Act
        Exception? refusal = Record.Exception(() => new RaceResult(anyRace, [anyNonRunner], []));

        // Assert
        Assert.IsType<ArgumentException>(refusal);
    }

}
