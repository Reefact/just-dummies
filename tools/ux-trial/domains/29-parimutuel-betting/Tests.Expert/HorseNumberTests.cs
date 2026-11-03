using ParimutuelBetting.Domain.Tests.Expert.Generators;

namespace ParimutuelBetting.Domain.Tests.Expert;

public sealed class HorseNumberTests {

    [Theory]
    [InlineData(1)]
    [InlineData(24)]
    public void The_lowest_and_highest_saddle_cloth_numbers_exist(int number) {
        HorseNumber horse = new HorseNumber(number);

        Assert.Equal(number, horse.Value);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(25)]
    [InlineData(-1)]
    [InlineData(int.MinValue)]
    [InlineData(int.MaxValue)]
    public void A_number_outside_1_to_24_is_refused(int number) {
        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(() => new HorseNumber(number));

        Assert.Equal("value", refusal.ParamName);
    }

    [Fact]
    public void Two_horses_with_the_same_number_are_equal_and_hash_alike() {
        int anyNumber = Any.Int32().Between(1, HorseNumber.Maximum).Generate();

        HorseNumber first  = new HorseNumber(anyNumber);
        HorseNumber second = new HorseNumber(anyNumber);

        Assert.True(first.Equals(second));
        Assert.True(first.Equals((object)second));
        Assert.Equal(first.GetHashCode(), second.GetHashCode());
    }

    [Fact]
    public void Two_horses_with_different_numbers_are_not_equal() {
        HorseNumber anyHorse      = Any.HorseNumber().Generate();
        HorseNumber anyOtherHorse = Any.HorseNumber().WithValue(number => number.DifferentFrom(anyHorse.Value)).Generate();

        Assert.False(anyHorse.Equals(anyOtherHorse));
        Assert.False(anyHorse.Equals((object)anyOtherHorse));
    }

    [Fact]
    public void A_horse_is_not_equal_to_nothing_nor_to_a_bare_number() {
        HorseNumber anyHorse = Any.HorseNumber().Generate();

        Assert.False(anyHorse.Equals(null));
        Assert.False(anyHorse.Equals((object?)null));
        Assert.False(anyHorse.Equals((object)anyHorse.Value));
    }

    [Fact]
    public void Horses_order_by_their_number() {
        HorseNumber three     = new HorseNumber(3);
        HorseNumber seventeen = new HorseNumber(17);

        Assert.True(three.CompareTo(seventeen) < 0);
        Assert.True(seventeen.CompareTo(three) > 0);
        Assert.Equal(0, three.CompareTo(new HorseNumber(3)));
    }

    [Fact]
    public void Any_horse_ranks_after_nothing() {
        HorseNumber anyHorse = Any.HorseNumber().Generate();

        Assert.True(anyHorse.CompareTo(null) > 0);
    }

    [Fact]
    public void A_horse_prints_as_its_number() {
        Assert.Equal("7", new HorseNumber(7).ToString());
    }

}
