using JustDummies;

using TaxRules.Domain;

using Xunit;

using static TaxRules.Tests.Build;

namespace TaxRules.Tests;

public sealed class TaxBracketTests {

    [Fact]
    public void A_bracket_keeps_its_bounds_and_its_rate() {
        Rate rate = R(0.2m);

        TaxBracket bracket = new TaxBracket(M(10_000m), M(30_000m), rate);

        Assert.Equal(M(10_000m), bracket.Lower);
        Assert.Equal(M(30_000m), bracket.Upper);
        Assert.Same(rate, bracket.Rate);
        Assert.False(bracket.IsOpenEnded);
    }

    [Fact]
    public void A_bracket_without_an_upper_bound_is_open_ended() {
        TaxBracket bracket = Any.TaxBracket().OpenEnded().Generate();

        Assert.Null(bracket.Upper);
        Assert.True(bracket.IsOpenEnded);
    }

    [Fact]
    public void An_upper_bound_equal_to_the_lower_bound_is_refused() {
        Rate anyRate = Any.Rate().Generate();

        Assert.Throws<ArgumentException>(() => new TaxBracket(M(1_000m), M(1_000m), anyRate));
    }

    [Fact]
    public void An_upper_bound_below_the_lower_bound_is_refused() {
        Rate anyRate = Any.Rate().Generate();

        Assert.Throws<ArgumentException>(() => new TaxBracket(M(1_000m), M(999.99m), anyRate));
    }

    [Fact]
    public void One_cent_between_the_bounds_is_a_bracket() {
        Rate anyRate = Any.Rate().Generate();

        TaxBracket bracket = new TaxBracket(M(1_000m), M(1_000.01m), anyRate);

        Assert.True(bracket.Contains(M(1_000m)));
    }

    [Fact]
    public void A_bracket_needs_its_lower_bound_and_its_rate() {
        Rate anyRate = Any.Rate().Generate();
        Money anyLower = Any.Money().Generate();

        Assert.Throws<ArgumentNullException>(() => new TaxBracket(null!, null, anyRate));
        Assert.Throws<ArgumentNullException>(() => new TaxBracket(anyLower, null, null!));
    }

    [Theory]
    [InlineData("10000.00", true)]
    [InlineData("10000.01", true)]
    [InlineData("29999.99", true)]
    [InlineData("30000.00", false)]
    [InlineData("9999.99", false)]
    [InlineData("0", false)]
    public void A_band_includes_its_lower_bound_and_excludes_its_upper_bound(string income, bool expected) {
        TaxBracket bracket = new TaxBracket(M(10_000m), M(30_000m), Any.Rate().Generate());

        Assert.Equal(expected, bracket.Contains(M(decimal.Parse(income, System.Globalization.CultureInfo.InvariantCulture))));
    }

    [Fact]
    public void An_open_ended_band_contains_everything_from_its_lower_bound_up() {
        TaxBracket bracket = new TaxBracket(M(30_000m), null, Any.Rate().Generate());

        Assert.False(bracket.Contains(M(29_999.99m)));
        Assert.True(bracket.Contains(M(30_000m)));
        Assert.True(bracket.Contains(M(Money.MaxAmount)));
    }

    [Fact]
    public void Containing_nothing_is_refused() {
        TaxBracket anyBracket = Any.TaxBracket().Generate();

        Assert.Throws<ArgumentNullException>(() => anyBracket.Contains(null!));
    }

    [Theory]
    [InlineData("0", "0")]
    [InlineData("10000", "0")]
    [InlineData("10001", "0.2")]
    [InlineData("20000", "2000")]
    [InlineData("30000", "4000")]
    [InlineData("50000", "4000")]
    public void The_tax_is_the_rate_on_the_part_of_the_income_that_falls_in_the_band(string income, string expectedTax) {
        TaxBracket bracket = new TaxBracket(M(10_000m), M(30_000m), R(0.2m));

        Assert.Equal(decimal.Parse(expectedTax, System.Globalization.CultureInfo.InvariantCulture),
                     bracket.TaxOn(M(decimal.Parse(income, System.Globalization.CultureInfo.InvariantCulture))));
    }

    [Fact]
    public void The_tax_of_an_open_ended_band_has_no_ceiling() {
        TaxBracket bracket = new TaxBracket(M(30_000m), null, R(0.4m));

        Assert.Equal(0m, bracket.TaxOn(M(30_000m)));
        Assert.Equal(40_000m, bracket.TaxOn(M(130_000m)));
    }

    [Fact]
    public void The_tax_is_not_rounded() {
        TaxBracket bracket = new TaxBracket(M(0m), null, R(0.3333m));

        Assert.Equal(0.003333m, bracket.TaxOn(M(0.01m)));
    }

    [Fact]
    public void Taxing_nothing_is_refused() {
        TaxBracket anyBracket = Any.TaxBracket().Generate();

        Assert.Throws<ArgumentNullException>(() => anyBracket.TaxOn(null!));
    }

    [Fact]
    public void Two_brackets_with_the_same_bounds_and_rate_are_equal() {
        Assert.Equal(new TaxBracket(M(0m), M(100m), R(0.1m)), new TaxBracket(M(0.00m), M(100.0m), R(0.10m)));
        Assert.Equal(new TaxBracket(M(0m), M(100m), R(0.1m)).GetHashCode(), new TaxBracket(M(0m), M(100m), R(0.1m)).GetHashCode());
        Assert.Equal(new TaxBracket(M(0m), null, R(0.1m)), new TaxBracket(M(0m), null, R(0.1m)));
    }

    [Fact]
    public void Brackets_differing_by_one_bound_or_the_rate_are_not_equal() {
        TaxBracket bracket = new TaxBracket(M(0m), M(100m), R(0.1m));

        Assert.NotEqual(bracket, new TaxBracket(M(1m), M(100m), R(0.1m)));
        Assert.NotEqual(bracket, new TaxBracket(M(0m), M(101m), R(0.1m)));
        Assert.NotEqual(bracket, new TaxBracket(M(0m), null, R(0.1m)));
        Assert.NotEqual(bracket, new TaxBracket(M(0m), M(100m), R(0.2m)));
        Assert.False(bracket.Equals((object)"[0.00, 100.00)"));
    }

    [Fact]
    public void A_bracket_is_shown_with_its_bounds_and_its_rate() {
        Assert.Equal("[0.00, 100.00) at 10 %", new TaxBracket(M(0m), M(100m), R(0.1m)).ToString());
        Assert.Equal("[100.00, ∞) at 12.5 %", new TaxBracket(M(100m), null, R(0.125m)).ToString());
    }

}
