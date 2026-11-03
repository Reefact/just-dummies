using ParimutuelBetting.Domain.Tests.Expert.Generators;
using ParimutuelBetting.Domain.Tests.Expert.Support;

namespace ParimutuelBetting.Domain.Tests.Expert;

public sealed class RaceResultTests {

    // The field size is what these tests are about (10 runners); the race code is not.
    private static Race TenRunners() {
        return Any.Race().WithFieldSize(10).Generate();
    }

    [Fact]
    public void A_result_names_the_finishers_in_order_and_the_horses_scratched() {
        RaceResult result = Tickets.Result(TenRunners(), new[] { 7, 2, 5 }, 1, 9);

        Assert.Equal(new[] { 7, 2, 5 }, result.FinishingOrder.Select(horse => horse.Value));
        Assert.Equal(new[] { 1, 9 }, result.Scratched.Select(horse => horse.Value));
    }

    [Fact]
    public void A_race_can_be_won_by_its_only_finisher_with_nothing_scratched() {
        RaceResult result = Tickets.Result(TenRunners(), new[] { 10 });

        Assert.Single(result.FinishingOrder);
        Assert.Empty(result.Scratched);
    }

    [Fact]
    public void Every_runner_may_finish_or_be_scratched() {
        Race threeRunners = Any.Race().WithFieldSize(3).Generate();

        RaceResult result = Tickets.Result(threeRunners, new[] { 3 }, 1, 2);

        Assert.Equal(3, result.FinishingOrder.Count + result.Scratched.Count);
    }

    [Fact]
    public void An_official_result_names_at_least_the_winner() {
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => Tickets.Result(TenRunners(), Array.Empty<int>(), 4));

        Assert.Equal("finishingOrder", refusal.ParamName);
    }

    [Fact]
    public void A_horse_cannot_finish_twice() {
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => Tickets.Result(TenRunners(), new[] { 3, 8, 3 }));

        Assert.Equal("finishingOrder", refusal.ParamName);
    }

    [Fact]
    public void A_horse_cannot_be_scratched_twice() {
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => Tickets.Result(TenRunners(), new[] { 3 }, 6, 6));

        Assert.Equal("scratched", refusal.ParamName);
    }

    [Fact]
    public void A_scratched_horse_cannot_appear_in_the_finishing_order() {
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => Tickets.Result(TenRunners(), new[] { 3, 6, 8 }, 6));

        Assert.Equal("finishingOrder", refusal.ParamName);
    }

    [Theory]
    [InlineData(11)]
    [InlineData(24)]
    public void A_finisher_must_be_a_runner_of_the_race(int horse) {
        Assert.Throws<ArgumentException>(() => Tickets.Result(TenRunners(), new[] { 3, horse }));
    }

    [Theory]
    [InlineData(11)]
    [InlineData(24)]
    public void A_scratched_horse_must_be_a_runner_of_the_race(int horse) {
        Assert.Throws<ArgumentException>(() => Tickets.Result(TenRunners(), new[] { 3 }, horse));
    }

    [Fact]
    public void The_last_runner_of_the_field_is_still_a_runner() {
        RaceResult result = Tickets.Result(TenRunners(), new[] { 10 }, 9);

        Assert.Equal(10, result.FinishingOrder.Single().Value);
    }

    [Fact]
    public void A_result_needs_its_race_and_both_lists() {
        IReadOnlyList<HorseNumber> anyFinishers = Tickets.Horses(1);
        IReadOnlyList<HorseNumber> anyScratched = Tickets.Horses();

        Assert.Throws<ArgumentNullException>(() => new RaceResult(null!, anyFinishers, anyScratched));
        Assert.Throws<ArgumentNullException>(() => new RaceResult(TenRunners(), null!, anyScratched));
        Assert.Throws<ArgumentNullException>(() => new RaceResult(TenRunners(), anyFinishers, null!));
    }

    [Fact]
    public void A_result_cannot_name_a_null_horse() {
        HorseNumber anyHorse = Any.HorseNumber().WithValue(number => number.LessThanOrEqualTo(10)).Generate();

        Assert.Throws<ArgumentException>(() => new RaceResult(TenRunners(), new HorseNumber[] { anyHorse, null! }, Array.Empty<HorseNumber>()));
        Assert.Throws<ArgumentException>(() => new RaceResult(TenRunners(), new[] { anyHorse }, new HorseNumber[] { null! }));
    }

    [Fact]
    public void A_result_does_not_follow_the_lists_it_was_built_from() {
        List<HorseNumber> finishers = new List<HorseNumber>(Tickets.Horses(4, 2));
        List<HorseNumber> scratched = new List<HorseNumber>(Tickets.Horses(9));

        RaceResult result = new RaceResult(TenRunners(), finishers, scratched);
        finishers.Clear();
        scratched.Add(new HorseNumber(1));

        Assert.Equal(2, result.FinishingOrder.Count);
        Assert.Single(result.Scratched);
    }

    [Theory]
    [InlineData(BetType.Win, new[] { 7 })]
    [InlineData(BetType.Quinella, new[] { 2, 7 })]
    [InlineData(BetType.Exacta, new[] { 7, 2 })]
    [InlineData(BetType.Trifecta, new[] { 7, 2, 5 })]
    public void The_winning_combination_is_made_of_the_first_finishers(BetType betType, int[] expectedHorses) {
        RaceResult result = Tickets.Result(TenRunners(), new[] { 7, 2, 5, 1 }, 9);

        Combination winning = result.WinningCombination(betType);

        Assert.Equal(Tickets.Bet(betType, expectedHorses), winning);
    }

    [Fact]
    public void A_place_bet_has_one_winning_horse_per_paid_place_and_so_no_winning_combination() {
        RaceResult anyResult = Any.RaceResult().Generate();

        ArgumentException refusal = Assert.Throws<ArgumentException>(() => anyResult.WinningCombination(BetType.Place));

        Assert.Equal("betType", refusal.ParamName);
    }

    [Theory]
    [InlineData(BetType.Quinella, 1)]
    [InlineData(BetType.Exacta, 1)]
    [InlineData(BetType.Trifecta, 1)]
    [InlineData(BetType.Trifecta, 2)]
    public void A_result_with_fewer_finishers_than_the_arity_has_no_winning_combination(BetType betType, int finishers) {
        int[] finishingOrder = Enumerable.Range(1, finishers).ToArray();
        RaceResult result    = Tickets.Result(TenRunners(), finishingOrder);

        Assert.Throws<InvalidOperationException>(() => result.WinningCombination(betType));
    }

    [Fact]
    public void Exactly_as_many_finishers_as_the_arity_is_enough() {
        RaceResult result = Tickets.Result(TenRunners(), new[] { 4, 8, 6 });

        Assert.Equal(Tickets.Bet(BetType.Trifecta, 4, 8, 6), result.WinningCombination(BetType.Trifecta));
    }

}
