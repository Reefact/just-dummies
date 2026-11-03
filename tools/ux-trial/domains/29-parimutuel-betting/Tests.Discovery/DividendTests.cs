namespace ParimutuelBetting.Tests.Discovery;

public sealed class DividendTests {

    [Fact]
    public void A_dividend_pays_its_amount_on_every_winning_unit() {
        // Arrange
        Combination anyCombination = Any.Combination().Generate();

        // Act
        Dividend dividend = new Dividend(anyCombination, 12.50m, 3);

        // Assert
        Assert.Equal(37.50m, dividend.Payout);
        Assert.Equal(12.50m, dividend.AmountPerUnit);
        Assert.Equal(3, dividend.WinningUnits);
        Assert.Same(anyCombination, dividend.Combination);
    }

    [Fact]
    public void The_smallest_dividend_is_one_cent_on_one_unit() {
        Combination anyCombination = Any.Combination().Generate();

        Dividend dividend = new Dividend(anyCombination, 0.01m, 1);

        Assert.Equal(0.01m, dividend.Payout);
    }

    [Theory]
    [InlineData("0")]
    [InlineData("-1.00")]
    [InlineData("1.005")]
    [InlineData("0.001")]
    public void A_dividend_must_be_a_positive_whole_number_of_cents(string amount) {
        Combination anyCombination = Any.Combination().Generate();
        int         anyUnits       = Any.Int32().Between(1, 1_000).Generate();

        Assert.Throws<ArgumentOutOfRangeException>(() => new Dividend(anyCombination, decimal.Parse(amount, System.Globalization.CultureInfo.InvariantCulture), anyUnits));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-4)]
    public void A_dividend_is_only_declared_for_a_combination_with_winning_units(int units) {
        Combination anyCombination = Any.Combination().Generate();

        Assert.Throws<ArgumentOutOfRangeException>(() => new Dividend(anyCombination, 2.00m, units));
    }

    [Fact]
    public void A_dividend_without_a_combination_is_refused() {
        Assert.Throws<ArgumentNullException>(() => new Dividend(null!, 2.00m, 1));
    }

}
