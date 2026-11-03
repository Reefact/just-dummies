namespace ParimutuelBetting.Tests.Discovery;

public sealed class HorseNumberTests {

    [Theory]
    [InlineData(1)]
    [InlineData(24)]
    public void The_first_and_the_last_saddle_cloth_numbers_exist(int number) {
        HorseNumber horse = new HorseNumber(number);

        Assert.Equal(number, horse.Value);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(25)]
    public void A_number_outside_the_race_card_is_refused(int number) {
        Assert.Throws<ArgumentOutOfRangeException>(() => new HorseNumber(number));
    }

    [Fact]
    public void Two_horses_wearing_the_same_number_are_equal() {
        // Arrange
        int anyNumber = Any.Int32().Between(1, HorseNumber.Maximum).Generate();

        // Act
        HorseNumber first  = new HorseNumber(anyNumber);
        HorseNumber second = new HorseNumber(anyNumber);

        // Assert
        Assert.Equal(first, second);
        Assert.Equal(first.GetHashCode(), second.GetHashCode());
        Assert.True(first.Equals((object)second));
    }

    [Fact]
    public void Two_horses_wearing_different_numbers_are_not_equal() {
        // Arrange
        (int anyLowNumber, int anyHighNumber) = Any.OrderedPair(Any.Int32().Between(1, HorseNumber.Maximum),
                                                                Any.Int32().Between(1, HorseNumber.Maximum),
                                                                strict: true).Generate();

        // Act
        HorseNumber low  = new HorseNumber(anyLowNumber);
        HorseNumber high = new HorseNumber(anyHighNumber);

        // Assert
        Assert.NotEqual(low, high);
    }

    [Fact]
    public void A_horse_is_never_equal_to_nothing_nor_to_a_bare_number() {
        HorseNumber anyHorse = Any.HorseNumber().Generate();

        Assert.False(anyHorse.Equals(null));
        Assert.False(anyHorse.Equals((object)anyHorse.Value));
    }

    [Fact]
    public void Horses_are_ordered_by_their_number() {
        // Arrange
        (int anyLowNumber, int anyHighNumber) = Any.OrderedPair(Any.Int32().Between(1, HorseNumber.Maximum),
                                                                Any.Int32().Between(1, HorseNumber.Maximum),
                                                                strict: true).Generate();
        HorseNumber low  = new HorseNumber(anyLowNumber);
        HorseNumber high = new HorseNumber(anyHighNumber);

        // Act & Assert
        Assert.True(low.CompareTo(high) < 0);
        Assert.True(high.CompareTo(low) > 0);
        Assert.Equal(0, low.CompareTo(new HorseNumber(anyLowNumber)));
    }

    [Fact]
    public void Any_horse_ranks_before_nothing() {
        HorseNumber anyHorse = Any.HorseNumber().Generate();

        Assert.True(anyHorse.CompareTo(null) > 0);
    }

    [Fact]
    public void A_horse_prints_as_its_number() {
        Assert.Equal("7", new HorseNumber(7).ToString());
    }

}
