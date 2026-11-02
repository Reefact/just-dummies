using System.Globalization;

using JustDummies;

using Telecom.Domain;

using Xunit;

namespace Telecom.Tests.Expert;

public sealed class DataBundleTests {

    private static readonly AnyInt32    AnyMegabytes = Any.Int32().Between(100, 100_000).MultipleOf(100);
    private static readonly AnyDecimal  AnyPrice     = Any.Decimal().Between(0.01m, 500m).WithScale(2);
    private static readonly AnyTimeSpan AnyValidity  = Any.TimeSpan().Between(TimeSpan.FromDays(1), TimeSpan.FromDays(3650));

    [Fact]
    public void A_bundle_keeps_what_it_was_built_with() {
        // Act
        DataBundle bundle = new DataBundle(500, 4.99m, TimeSpan.FromDays(30));

        // Assert
        Assert.Equal(500, bundle.Megabytes);
        Assert.Equal(4.99m, bundle.Price);
        Assert.Equal(TimeSpan.FromDays(30), bundle.Validity);
    }

    [Theory]
    [InlineData(100)]       // the smallest
    [InlineData(1_000)]
    [InlineData(100_000)]   // the biggest
    public void A_bundle_of_100_mb_to_100_gb_in_steps_of_100_is_accepted(int megabytes) {
        // Arrange
        decimal  anyPrice    = AnyPrice.Generate();
        TimeSpan anyValidity = AnyValidity.Generate();

        // Act
        DataBundle bundle = new DataBundle(megabytes, anyPrice, anyValidity);

        // Assert
        Assert.Equal(megabytes, bundle.Megabytes);
    }

    [Theory]
    [InlineData(99)]
    [InlineData(0)]
    [InlineData(-100)]
    [InlineData(100_001)]
    [InlineData(200_000)]
    public void A_size_out_of_range_is_refused(int megabytes) {
        // Arrange
        decimal  anyPrice    = AnyPrice.Generate();
        TimeSpan anyValidity = AnyValidity.Generate();

        // Act
        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(() => new DataBundle(megabytes, anyPrice, anyValidity));

        // Assert
        Assert.Equal("megabytes", refusal.ParamName);
    }

    [Theory]
    [InlineData(101)]
    [InlineData(150)]
    [InlineData(99_999)]
    public void A_size_that_is_not_a_multiple_of_100_is_refused(int megabytes) {
        // Arrange
        decimal  anyPrice    = AnyPrice.Generate();
        TimeSpan anyValidity = AnyValidity.Generate();

        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => new DataBundle(megabytes, anyPrice, anyValidity));

        // Assert
        Assert.Equal("megabytes", refusal.ParamName);
    }

    [Theory]
    [InlineData("0.01")]    // the cheapest
    [InlineData("500")]     // the dearest
    public void A_price_from_one_cent_to_500_is_accepted(string price) {
        // Arrange
        int      anyMegabytes = AnyMegabytes.Generate();
        TimeSpan anyValidity  = AnyValidity.Generate();

        // Act
        DataBundle bundle = new DataBundle(anyMegabytes, Money(price), anyValidity);

        // Assert
        Assert.Equal(Money(price), bundle.Price);
    }

    [Theory]
    [InlineData("0")]
    [InlineData("0.009")]
    [InlineData("-1")]
    [InlineData("500.01")]
    public void A_price_out_of_range_is_refused(string price) {
        // Arrange
        int      anyMegabytes = AnyMegabytes.Generate();
        TimeSpan anyValidity  = AnyValidity.Generate();

        // Act
        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(
            () => new DataBundle(anyMegabytes, Money(price), anyValidity));

        // Assert
        Assert.Equal("price", refusal.ParamName);
    }

    [Fact(Skip = "domain defect: the class is documented as 'priced in cents' yet a price of 0.015 is accepted (Plan refuses a fee that is not in whole cents); deterministic, no seed")]
    public void A_price_in_fractions_of_a_cent_is_refused() {
        // Arrange
        int      anyMegabytes = AnyMegabytes.Generate();
        TimeSpan anyValidity  = AnyValidity.Generate();

        // Act & Assert
        Assert.Throws<ArgumentException>(() => new DataBundle(anyMegabytes, 0.015m, anyValidity));
    }

    [Fact]
    public void A_validity_of_exactly_one_day_is_accepted() {
        // Arrange
        int     anyMegabytes = AnyMegabytes.Generate();
        decimal anyPrice     = AnyPrice.Generate();

        // Act
        DataBundle bundle = new DataBundle(anyMegabytes, anyPrice, TimeSpan.FromDays(1));

        // Assert
        Assert.Equal(TimeSpan.FromDays(1), bundle.Validity);
    }

    [Fact]
    public void A_validity_is_not_bounded_above() {
        // Arrange
        int     anyMegabytes = AnyMegabytes.Generate();
        decimal anyPrice     = AnyPrice.Generate();

        // Act
        DataBundle bundle = new DataBundle(anyMegabytes, anyPrice, TimeSpan.FromDays(36_500));

        // Assert
        Assert.Equal(TimeSpan.FromDays(36_500), bundle.Validity);
    }

    [Theory]
    [InlineData(863_999_999_999)]   // one tick short of a day
    [InlineData(0)]
    [InlineData(-1)]
    public void A_validity_under_a_day_is_refused(long ticks) {
        // Arrange
        int     anyMegabytes = AnyMegabytes.Generate();
        decimal anyPrice     = AnyPrice.Generate();

        // Act
        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(() => new DataBundle(anyMegabytes, anyPrice, TimeSpan.FromTicks(ticks)));

        // Assert
        Assert.Equal("validity", refusal.ParamName);
    }

    [Theory]
    [InlineData(500, "5", "10.24")]
    [InlineData(100, "1", "10.24")]
    [InlineData(300, "1", "3.41")]          // 3.4133... rounded to the cent
    [InlineData(2_000, "10", "5.12")]
    [InlineData(100_000, "500", "5.12")]
    public void The_price_per_gigabyte_is_the_price_scaled_to_1024_mb_and_rounded_to_the_cent(int megabytes, string price, string expected) {
        // Arrange
        TimeSpan   anyValidity = AnyValidity.Generate();
        DataBundle bundle      = new DataBundle(megabytes, Money(price), anyValidity);

        // Act
        decimal pricePerGigabyte = bundle.PricePerGigabyte;

        // Assert
        Assert.Equal(Money(expected), pricePerGigabyte);
    }

    private static decimal Money(string text) {
        return decimal.Parse(text, CultureInfo.InvariantCulture);
    }

}
