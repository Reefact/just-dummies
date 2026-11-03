namespace ParimutuelBetting.Tests.Discovery;

public sealed class StakeTests {

    [Theory]
    [InlineData(1)]
    [InlineData(10_000)]
    public void A_ticket_carries_from_one_to_ten_thousand_units(int units) {
        // Arrange
        Combination anyCombination = Any.Combination().Generate();

        // Act
        Stake stake = new Stake(anyCombination, units);

        // Assert
        Assert.Equal(units, stake.Units);
        Assert.Same(anyCombination, stake.Combination);
    }

    [Theory]
    [InlineData(-5)]
    [InlineData(0)]
    [InlineData(10_001)]
    public void A_ticket_with_no_unit_or_more_than_ten_thousand_is_refused(int units) {
        Combination anyCombination = Any.Combination().Generate();

        Assert.Throws<ArgumentOutOfRangeException>(() => new Stake(anyCombination, units));
    }

    [Fact]
    public void A_ticket_without_a_combination_is_refused() {
        int anyUnits = Any.Int32().Between(1, Stake.MaximumUnits).Generate();

        Assert.Throws<ArgumentNullException>(() => new Stake(null!, anyUnits));
    }

}
