using JustDummies;

using ParimutuelBetting.Domain;

using Xunit;

namespace ParimutuelBetting.Tests.Regular;

public sealed class StakeTests {

    [Theory]
    [InlineData(1)]
    [InlineData(10_000)]
    public void A_ticket_of_1_to_10_000_units_is_accepted(int units) {
        // Arrange
        Combination anyCombination = Arbitrary.Combination().Generate();

        // Act
        Stake stake = new Stake(anyCombination, units);

        // Assert
        Assert.Equal(units, stake.Units);
        Assert.Same(anyCombination, stake.Combination);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(10_001)]
    [InlineData(-1)]
    public void A_ticket_of_no_units_or_more_than_10_000_is_refused(int units) {
        // Arrange
        Combination anyCombination = Arbitrary.Combination().Generate();

        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => new Stake(anyCombination, units));
    }

    [Fact]
    public void A_ticket_names_a_combination() {
        // Arrange
        int anyUnits = Any.Int32().Between(1, Stake.MaximumUnits).Generate();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new Stake(null!, anyUnits));
    }

}
