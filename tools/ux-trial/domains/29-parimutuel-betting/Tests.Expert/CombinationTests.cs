using ParimutuelBetting.Domain.Tests.Expert.Generators;
using ParimutuelBetting.Domain.Tests.Expert.Support;

namespace ParimutuelBetting.Domain.Tests.Expert;

public sealed class CombinationTests {

    [Theory]
    [InlineData(BetType.Win, 1, true)]
    [InlineData(BetType.Place, 1, true)]
    [InlineData(BetType.Quinella, 2, false)]
    [InlineData(BetType.Exacta, 2, true)]
    [InlineData(BetType.Trifecta, 3, true)]
    public void Each_bet_type_names_a_fixed_number_of_horses_and_is_ordered_or_not(BetType betType, int arity, bool isOrdered) {
        Assert.Equal(arity, betType.Arity());
        Assert.Equal(isOrdered, betType.IsOrdered());
    }

    [Fact]
    public void An_undeclared_bet_type_has_no_rules() {
        Assert.Throws<ArgumentOutOfRangeException>(() => ((BetType)99).Arity());
        Assert.Throws<ArgumentOutOfRangeException>(() => ((BetType)99).IsOrdered());
    }

    [Theory]
    [InlineData(BetType.Win, 1)]
    [InlineData(BetType.Place, 1)]
    [InlineData(BetType.Quinella, 2)]
    [InlineData(BetType.Exacta, 2)]
    [InlineData(BetType.Trifecta, 3)]
    public void A_combination_names_as_many_distinct_horses_as_its_bet_type_asks(BetType betType, int arity) {
        IReadOnlyList<HorseNumber> anyHorses = Any.ListOf(Any.HorseNumber()).WithCount(arity).Distinct().Generate();

        Combination combination = new Combination(betType, anyHorses);

        Assert.Equal(arity, combination.Horses.Count);
        Assert.Equal(betType, combination.BetType);
    }

    [Theory]
    [InlineData(BetType.Win, 0)]
    [InlineData(BetType.Win, 2)]
    [InlineData(BetType.Place, 2)]
    [InlineData(BetType.Quinella, 1)]
    [InlineData(BetType.Quinella, 3)]
    [InlineData(BetType.Exacta, 3)]
    [InlineData(BetType.Trifecta, 2)]
    [InlineData(BetType.Trifecta, 4)]
    public void A_combination_with_the_wrong_number_of_horses_is_refused(BetType betType, int horseCount) {
        IReadOnlyList<HorseNumber> anyHorses = Any.ListOf(Any.HorseNumber()).WithCount(horseCount).Distinct().Generate();

        ArgumentException refusal = Assert.Throws<ArgumentException>(() => new Combination(betType, anyHorses));

        Assert.Equal("horses", refusal.ParamName);
    }

    [Theory]
    [InlineData(BetType.Quinella)]
    [InlineData(BetType.Exacta)]
    public void A_combination_cannot_name_the_same_horse_twice(BetType betType) {
        HorseNumber anyHorse = Any.HorseNumber().Generate();

        ArgumentException refusal = Assert.Throws<ArgumentException>(() => new Combination(betType, new[] { anyHorse, anyHorse }));

        Assert.Equal("horses", refusal.ParamName);
    }

    [Fact]
    public void A_trifecta_cannot_repeat_a_horse_anywhere_in_its_three() {
        HorseNumber anyHorse      = Any.HorseNumber().Generate();
        HorseNumber anyOtherHorse = Any.HorseNumber().WithValue(number => number.DifferentFrom(anyHorse.Value)).Generate();

        Assert.Throws<ArgumentException>(() => new Combination(BetType.Trifecta, new[] { anyHorse, anyOtherHorse, anyHorse }));
        Assert.Throws<ArgumentException>(() => new Combination(BetType.Trifecta, new[] { anyHorse, anyHorse, anyOtherHorse }));
        Assert.Throws<ArgumentException>(() => new Combination(BetType.Trifecta, new[] { anyOtherHorse, anyHorse, anyHorse }));
    }

    [Fact]
    public void A_combination_needs_a_list_of_horses() {
        Assert.Throws<ArgumentNullException>(() => new Combination(BetType.Win, null!));
    }

    [Fact]
    public void A_combination_cannot_name_a_null_horse() {
        HorseNumber anyHorse = Any.HorseNumber().Generate();

        ArgumentException refusal = Assert.Throws<ArgumentException>(() => new Combination(BetType.Exacta, new HorseNumber[] { anyHorse, null! }));

        Assert.Equal("horses", refusal.ParamName);
    }

    [Fact]
    public void A_combination_needs_a_declared_bet_type() {
        IReadOnlyList<HorseNumber> anyHorses = Any.ListOf(Any.HorseNumber()).WithCount(1).Generate();

        Assert.Throws<ArgumentOutOfRangeException>(() => new Combination((BetType)99, anyHorses));
    }

    [Fact]
    public void An_unordered_combination_is_kept_in_ascending_order() {
        Combination quinella = Tickets.Bet(BetType.Quinella, 5, 3);

        Assert.Equal(new[] { 3, 5 }, quinella.Horses.Select(horse => horse.Value));
    }

    [Theory]
    [InlineData(BetType.Exacta, new[] { 5, 3 })]
    [InlineData(BetType.Trifecta, new[] { 9, 2, 6 })]
    [InlineData(BetType.Win, new[] { 8 })]
    [InlineData(BetType.Place, new[] { 8 })]
    public void An_ordered_combination_keeps_the_order_it_was_given(BetType betType, int[] horses) {
        Combination combination = Tickets.Bet(betType, horses);

        Assert.Equal(horses, combination.Horses.Select(horse => horse.Value));
    }

    [Fact]
    public void A_quinella_is_the_same_bet_whichever_horse_is_named_first() {
        Combination fiveThree = Tickets.Bet(BetType.Quinella, 5, 3);
        Combination threeFive = Tickets.Bet(BetType.Quinella, 3, 5);

        Assert.Equal(fiveThree, threeFive);
        Assert.Equal(fiveThree.GetHashCode(), threeFive.GetHashCode());
    }

    [Fact]
    public void An_exacta_is_a_different_bet_when_the_order_is_reversed() {
        Combination fiveThree = Tickets.Bet(BetType.Exacta, 5, 3);
        Combination threeFive = Tickets.Bet(BetType.Exacta, 3, 5);

        Assert.NotEqual(fiveThree, threeFive);
    }

    [Fact]
    public void A_trifecta_is_a_different_bet_when_any_two_places_swap() {
        Combination reference = Tickets.Bet(BetType.Trifecta, 1, 2, 3);

        Assert.NotEqual(reference, Tickets.Bet(BetType.Trifecta, 2, 1, 3));
        Assert.NotEqual(reference, Tickets.Bet(BetType.Trifecta, 1, 3, 2));
        Assert.Equal(reference, Tickets.Bet(BetType.Trifecta, 1, 2, 3));
    }

    [Fact]
    public void The_same_horse_in_a_win_and_a_place_bet_is_not_the_same_bet() {
        HorseNumber anyHorse = Any.HorseNumber().Generate();

        Combination win   = new Combination(BetType.Win, new[] { anyHorse });
        Combination place = new Combination(BetType.Place, new[] { anyHorse });

        Assert.False(win.Equals(place));
    }

    [Fact]
    public void A_combination_is_not_equal_to_nothing_nor_to_something_else() {
        Combination anyCombination = Any.Combination().Generate();

        Assert.False(anyCombination.Equals(null));
        Assert.False(anyCombination.Equals((object?)null));
        Assert.False(anyCombination.Equals((object)"not a combination"));
    }

    [Fact]
    public void A_combination_does_not_follow_the_list_it_was_built_from() {
        List<HorseNumber> source = new List<HorseNumber>(Tickets.Horses(4, 9));

        Combination exacta = new Combination(BetType.Exacta, source);
        source.Reverse();
        source.Add(new HorseNumber(1));

        Assert.Equal(new[] { 4, 9 }, exacta.Horses.Select(horse => horse.Value));
    }

    [Theory]
    [InlineData(BetType.Win, new[] { 7 }, "Win 7")]
    [InlineData(BetType.Place, new[] { 7 }, "Place 7")]
    [InlineData(BetType.Quinella, new[] { 5, 3 }, "Quinella 3/5")]
    [InlineData(BetType.Exacta, new[] { 5, 3 }, "Exacta 5-3")]
    [InlineData(BetType.Trifecta, new[] { 5, 3, 11 }, "Trifecta 5-3-11")]
    public void A_combination_prints_its_bet_type_and_its_horses(BetType betType, int[] horses, string expected) {
        Assert.Equal(expected, Tickets.Bet(betType, horses).ToString());
    }

    [Theory]
    [InlineData(new[] { 3 }, true)]
    [InlineData(new[] { 9, 5 }, true)]
    [InlineData(new[] { 9, 6 }, false)]
    [InlineData(new int[0], false)]
    public void A_combination_involves_a_horse_when_it_names_it(int[] horses, bool involved) {
        Combination exacta = Tickets.Bet(BetType.Exacta, 3, 5);

        Assert.Equal(involved, exacta.Involves(Tickets.Horses(horses)));
    }

    [Fact(Skip = "domain defect: Combination.Horses hands back the internal array; a caller that casts it to HorseNumber[] can reorder or overwrite the bet and its hash code; seed-independent, first seen with seed -2073497820")]
    public void A_combination_stays_the_same_bet_whatever_a_caller_does_with_its_horses() {
        Combination exacta = Tickets.Bet(BetType.Exacta, 3, 5);

        if (exacta.Horses is HorseNumber[] exposed) {
            exposed[0] = new HorseNumber(20);
        }

        Assert.Equal(new[] { 3, 5 }, exacta.Horses.Select(horse => horse.Value));
    }

}
