using JustDummies;

using Xunit;

namespace Telecom.Domain.Tests;

public sealed class DataBundleTests {

    private static AnyInt32    AnyMegabytes() => Any.Int32().Between(100, 100_000).MultipleOf(100);
    private static AnyDecimal  AnyPrice()     => Any.Decimal().Between(0.01m, 500m).WithScale(2);
    private static AnyTimeSpan AnyValidity()  => Any.TimeSpan().Between(TimeSpan.FromDays(1), TimeSpan.FromDays(365));

    [Fact]
    public void Keeps_what_it_is_given() {
        // Act
        DataBundle bundle = new DataBundle(5_000, 12.50m, TimeSpan.FromDays(30));

        // Assert
        Assert.Equal(5_000, bundle.Megabytes);
        Assert.Equal(12.50m, bundle.Price);
        Assert.Equal(TimeSpan.FromDays(30), bundle.Validity);
    }

    [Theory]
    [InlineData(100)]
    [InlineData(100_000)]
    public void Accepts_the_extreme_sizes(int megabytes) {
        // Arrange
        decimal   anyPrice    = AnyPrice().Generate();
        TimeSpan  anyValidity = AnyValidity().Generate();

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
    [InlineData(100_100)]
    [InlineData(int.MaxValue)]
    public void Refuses_a_size_outside_100_MB_to_100_GB(int megabytes) {
        // Arrange
        decimal  anyPrice    = AnyPrice().Generate();
        TimeSpan anyValidity = AnyValidity().Generate();

        // Act
        ArgumentOutOfRangeException exception = Assert.Throws<ArgumentOutOfRangeException>(() => new DataBundle(megabytes, anyPrice, anyValidity));

        // Assert
        Assert.Equal("megabytes", exception.ParamName);
    }

    [Theory]
    [InlineData(101)]
    [InlineData(150)]
    [InlineData(1_999)]
    [InlineData(99_999)]
    public void Refuses_a_size_that_is_not_a_multiple_of_100_MB(int megabytes) {
        // Arrange
        decimal  anyPrice    = AnyPrice().Generate();
        TimeSpan anyValidity = AnyValidity().Generate();

        // Act: ArgumentException itself, not the out-of-range subclass
        ArgumentException exception = Assert.Throws<ArgumentException>(() => new DataBundle(megabytes, anyPrice, anyValidity));

        // Assert
        Assert.Equal("megabytes", exception.ParamName);
    }

    [Fact]
    public void Checks_the_size_range_before_the_step() {
        // 50 is out of range AND not a multiple of 100: the range wins
        Assert.Throws<ArgumentOutOfRangeException>(() => new DataBundle(50, 5m, TimeSpan.FromDays(1)));
    }

    [Theory]
    [InlineData("0.01")]
    [InlineData("0.5")]
    [InlineData("500")]
    public void Accepts_a_price_from_1_cent_to_500(string price) {
        // Arrange
        int      anyMegabytes = AnyMegabytes().Generate();
        TimeSpan anyValidity  = AnyValidity().Generate();

        // Act
        DataBundle bundle = new DataBundle(anyMegabytes, decimal.Parse(price, System.Globalization.CultureInfo.InvariantCulture), anyValidity);

        // Assert
        Assert.Equal(decimal.Parse(price, System.Globalization.CultureInfo.InvariantCulture), bundle.Price);
    }

    [Theory]
    [InlineData("0")]
    [InlineData("0.009")]
    [InlineData("-5")]
    [InlineData("500.01")]
    [InlineData("1000")]
    public void Refuses_a_price_outside_1_cent_to_500(string price) {
        // Arrange
        int      anyMegabytes = AnyMegabytes().Generate();
        TimeSpan anyValidity  = AnyValidity().Generate();
        decimal  value        = decimal.Parse(price, System.Globalization.CultureInfo.InvariantCulture);

        // Act
        ArgumentOutOfRangeException exception = Assert.Throws<ArgumentOutOfRangeException>(() => new DataBundle(anyMegabytes, value, anyValidity));

        // Assert
        Assert.Equal("price", exception.ParamName);
    }

    [Fact]
    public void Accepts_a_validity_of_exactly_one_day() {
        // Arrange
        int     anyMegabytes = AnyMegabytes().Generate();
        decimal anyPrice     = AnyPrice().Generate();

        // Act
        DataBundle bundle = new DataBundle(anyMegabytes, anyPrice, TimeSpan.FromDays(1));

        // Assert
        Assert.Equal(TimeSpan.FromDays(1), bundle.Validity);
    }

    [Fact]
    public void Accepts_a_very_long_validity() {
        // Arrange
        int     anyMegabytes = AnyMegabytes().Generate();
        decimal anyPrice     = AnyPrice().Generate();

        // Act: there is no upper bound
        DataBundle bundle = new DataBundle(anyMegabytes, anyPrice, TimeSpan.FromDays(36_500));

        // Assert
        Assert.Equal(TimeSpan.FromDays(36_500), bundle.Validity);
    }

    [Fact]
    public void Refuses_a_validity_shorter_than_one_day() {
        // Arrange
        int      anyMegabytes = AnyMegabytes().Generate();
        decimal  anyPrice     = AnyPrice().Generate();
        TimeSpan anyTooShort  = Any.TimeSpan().LessThan(TimeSpan.FromDays(1)).Generate();

        // Act
        ArgumentOutOfRangeException exception = Assert.Throws<ArgumentOutOfRangeException>(() => new DataBundle(anyMegabytes, anyPrice, anyTooShort));

        // Assert
        Assert.Equal("validity", exception.ParamName);
    }

    [Fact]
    public void Refuses_a_validity_one_tick_short_of_a_day() {
        Assert.Throws<ArgumentOutOfRangeException>(() => new DataBundle(100, 1m, TimeSpan.FromDays(1) - TimeSpan.FromTicks(1)));
    }

    [Theory]
    [InlineData(1_000, "5", "5.12")]       // 5 * 1024 / 1000
    [InlineData(100, "1", "10.24")]
    [InlineData(100_000, "500", "5.12")]
    [InlineData(2_000, "10", "5.12")]
    [InlineData(500, "3", "6.14")]         // 6.144 rounds down
    [InlineData(300, "7.5", "25.60")]
    [InlineData(700, "0.01", "0.01")]      // 0.0146... rounds up to 0.01
    public void Prices_a_gigabyte_as_1024_MB_rounded_to_cents(int megabytes, string price, string expected) {
        // Arrange
        TimeSpan   anyValidity = AnyValidity().Generate();
        DataBundle bundle      = new DataBundle(megabytes, decimal.Parse(price, System.Globalization.CultureInfo.InvariantCulture), anyValidity);

        // Act
        decimal pricePerGigabyte = bundle.PricePerGigabyte;

        // Assert
        Assert.Equal(decimal.Parse(expected, System.Globalization.CultureInfo.InvariantCulture), pricePerGigabyte);
    }

}
