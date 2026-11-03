using JustDummies;

using ParimutuelBetting.Domain;

using Xunit;

namespace ParimutuelBetting.Tests.Regular;

public sealed class DividendTests {

    [Fact]
    public void The_payout_is_the_amount_per_unit_times_the_winning_units() {
        // Arrange
        Combination anyCombination = Arbitrary.Combination().Generate();

        // Act
        Dividend dividend = new Dividend(anyCombination, 12.40m, 3);

        // Assert
        Assert.Equal(37.20m, dividend.Payout);
    }

    [Fact]
    public void A_dividend_keeps_its_combination_amount_and_units() {
        // Arrange
        Combination anyCombination = Arbitrary.Combination().Generate();

        // Act
        Dividend dividend = new Dividend(anyCombination, 1.05m, 8);

        // Assert
        Assert.Same(anyCombination, dividend.Combination);
        Assert.Equal(1.05m, dividend.AmountPerUnit);
        Assert.Equal(8, dividend.WinningUnits);
    }

    [Fact]
    public void A_single_cent_is_the_smallest_dividend() {
        // Arrange
        Combination anyCombination = Arbitrary.Combination().Generate();

        // Act
        Dividend dividend = new Dividend(anyCombination, 0.01m, 1);

        // Assert
        Assert.Equal(0.01m, dividend.Payout);
    }

    [Fact]
    public void An_amount_that_is_not_positive_is_refused() {
        // Arrange
        Combination anyCombination = Arbitrary.Combination().Generate();
        int         anyUnits       = Any.Int32().Positive().Generate();
        decimal     anyNegative    = Any.Decimal().Negative().WithScale(2).Generate();

        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => new Dividend(anyCombination, 0m, anyUnits));
        Assert.Throws<ArgumentOutOfRangeException>(() => new Dividend(anyCombination, anyNegative, anyUnits));
    }

    [Theory]
    [InlineData("0.001")]
    [InlineData("1.005")]
    [InlineData("12.345")]
    public void An_amount_that_is_not_a_whole_number_of_cents_is_refused(string amount) {
        // Arrange
        Combination anyCombination = Arbitrary.Combination().Generate();
        decimal     amountPerUnit  = decimal.Parse(amount, System.Globalization.CultureInfo.InvariantCulture);

        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => new Dividend(anyCombination, amountPerUnit, 1));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-4)]
    public void A_dividend_needs_at_least_one_winning_unit(int winningUnits) {
        // Arrange
        Combination anyCombination = Arbitrary.Combination().Generate();
        decimal     anyAmount      = Any.Decimal().Positive().WithScale(2).Generate();

        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => new Dividend(anyCombination, anyAmount, winningUnits));
    }

    [Fact]
    public void A_dividend_names_a_combination() {
        // Arrange
        decimal anyAmount = Any.Decimal().Positive().WithScale(2).Generate();
        int     anyUnits  = Any.Int32().Positive().Generate();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new Dividend(null!, anyAmount, anyUnits));
    }

}
