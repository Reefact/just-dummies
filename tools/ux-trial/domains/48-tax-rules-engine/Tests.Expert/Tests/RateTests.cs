using JustDummies;

using TaxRules.Domain;

using Xunit;

namespace TaxRules.Tests;

public sealed class RateTests {

    [Theory]
    [InlineData("0")]
    [InlineData("0.0001")]
    [InlineData("0.2000")]
    [InlineData("1")]
    public void A_fraction_from_zero_to_one_to_a_hundredth_of_a_percent_is_accepted(string fraction) {
        decimal value = decimal.Parse(fraction, System.Globalization.CultureInfo.InvariantCulture);

        Assert.Equal(value, new Rate(value).Fraction);
    }

    [Theory]
    [InlineData("-0.0001")]
    [InlineData("1.0001")]
    [InlineData("2")]
    public void A_fraction_outside_zero_to_one_is_refused(string fraction) {
        Assert.Throws<ArgumentOutOfRangeException>(() => new Rate(decimal.Parse(fraction, System.Globalization.CultureInfo.InvariantCulture)));
    }

    [Theory]
    [InlineData("0.00001")]
    [InlineData("0.12345")]
    public void A_fraction_finer_than_a_hundredth_of_a_percent_is_refused_rather_than_rounded(string fraction) {
        Assert.Throws<ArgumentException>(() => new Rate(decimal.Parse(fraction, System.Globalization.CultureInfo.InvariantCulture)));
    }

    [Fact]
    public void A_rate_made_from_a_percentage_is_that_percentage_over_a_hundred() {
        Assert.Equal(0.1250m, Rate.FromPercent(12.5m).Fraction);
        Assert.Equal(1m, Rate.FromPercent(100m).Fraction);
        Assert.Equal(0.0001m, Rate.FromPercent(0.01m).Fraction);
    }

    [Fact]
    public void A_percentage_finer_than_a_hundredth_of_a_percent_is_refused() {
        Assert.Throws<ArgumentException>(() => Rate.FromPercent(0.001m));
    }

    [Fact]
    public void A_percentage_above_a_hundred_is_refused() {
        Assert.Throws<ArgumentOutOfRangeException>(() => Rate.FromPercent(100.01m));
    }

    [Fact]
    public void The_percentage_is_the_fraction_times_a_hundred() {
        Assert.Equal(12.5m, new Rate(0.125m).Percent);
    }

    [Fact]
    public void Zero_is_the_only_rate_that_is_zero() {
        Assert.True(Rate.Zero.IsZero);
        Assert.True(new Rate(0m).IsZero);
        Assert.False(new Rate(0.0001m).IsZero);
    }

    [Fact]
    public void Rates_are_ordered_by_their_fraction_and_none_is_below_nothing() {
        Assert.True(new Rate(0.1m).CompareTo(new Rate(0.2m)) < 0);
        Assert.True(new Rate(0.2m).CompareTo(new Rate(0.1m)) > 0);
        Assert.Equal(0, new Rate(0.20m).CompareTo(new Rate(0.2m)));
        Assert.True(new Rate(0.1m).CompareTo(null) > 0);
    }

    [Fact]
    public void Two_rates_of_the_same_fraction_are_equal_whatever_the_scale_they_were_written_in() {
        Rate hundredths = new Rate(0.20m);
        Rate tenths = new Rate(0.2m);

        Assert.Equal(hundredths, tenths);
        Assert.Equal(hundredths.GetHashCode(), tenths.GetHashCode());
        Assert.False(hundredths.Equals((object)0.2m));
    }

    [Fact]
    public void A_rate_is_shown_as_a_percentage_with_at_most_two_decimals() {
        Assert.Equal("12.5 %", new Rate(0.125m).ToString());
        Assert.Equal("20 %", new Rate(0.2m).ToString());
        Assert.Equal("0.01 %", new Rate(0.0001m).ToString());
    }

}
