using JustDummies;

using Telecom.Domain;

using Xunit;

namespace Telecom.Tests.Regular;

public sealed class DataBundleTests {

    private static decimal Dec(string text) => decimal.Parse(text, System.Globalization.CultureInfo.InvariantCulture);

    [Fact]
    public void Keeps_what_it_was_given() {
        // Act
        DataBundle bundle = new DataBundle(5_000, 12.50m, TimeSpan.FromDays(30));

        // Assert
        Assert.Equal(5_000, bundle.Megabytes);
        Assert.Equal(12.50m, bundle.Price);
        Assert.Equal(TimeSpan.FromDays(30), bundle.Validity);
    }

    [Fact]
    public void Accepts_any_generated_bundle() {
        // Act
        Exception? thrown = Record.Exception(() => Any.DataBundle().Generate());

        // Assert
        Assert.Null(thrown);
    }

    [Theory]
    [InlineData(100)]
    [InlineData(100_000)]
    public void Accepts_the_bounds_of_the_size_range(int megabytes) {
        // Arrange
        decimal  anyPrice    = Any.Decimal().Between(0.01m, 500m).Generate();
        TimeSpan anyValidity = Any.TimeSpan().GreaterThanOrEqualTo(TimeSpan.FromDays(1)).Generate();

        // Act
        DataBundle bundle = new DataBundle(megabytes, anyPrice, anyValidity);

        // Assert
        Assert.Equal(megabytes, bundle.Megabytes);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(99)]
    [InlineData(100_001)]
    [InlineData(100_100)]
    [InlineData(-100)]
    public void Refuses_a_size_outside_100_MB_to_100_GB(int megabytes) {
        // Act
        ArgumentOutOfRangeException refused = Assert.Throws<ArgumentOutOfRangeException>(() => new DataBundle(megabytes, 10m, TimeSpan.FromDays(30)));

        // Assert
        Assert.Equal("megabytes", refused.ParamName);
    }

    [Theory]
    [InlineData(101)]
    [InlineData(150)]
    [InlineData(99_999)]
    public void Refuses_a_size_that_is_not_a_multiple_of_100(int megabytes) {
        // Act: ArgumentOutOfRangeException derives from ArgumentException, hence the exact-type check
        ArgumentException refused = Assert.Throws<ArgumentException>(() => new DataBundle(megabytes, 10m, TimeSpan.FromDays(30)));

        // Assert
        Assert.Equal("megabytes", refused.ParamName);
    }

    [Theory]
    [InlineData("0.01")]
    [InlineData("500")]
    public void Accepts_the_bounds_of_the_price_range(string price) {
        // Arrange
        int anySize = Any.DataBundle().Generate().Megabytes;

        // Act
        DataBundle bundle = new DataBundle(anySize, Dec(price), TimeSpan.FromDays(7));

        // Assert
        Assert.Equal(Dec(price), bundle.Price);
    }

    [Theory]
    [InlineData("0")]
    [InlineData("0.009")]
    [InlineData("-1")]
    [InlineData("500.01")]
    public void Refuses_a_price_outside_one_cent_to_500(string price) {
        // Act
        ArgumentOutOfRangeException refused = Assert.Throws<ArgumentOutOfRangeException>(
            () => new DataBundle(1_000, Dec(price), TimeSpan.FromDays(7)));

        // Assert
        Assert.Equal("price", refused.ParamName);
    }

    [Fact]
    public void Accepts_a_validity_of_exactly_one_day() {
        // Act
        DataBundle bundle = new DataBundle(1_000, 5m, TimeSpan.FromDays(1));

        // Assert
        Assert.Equal(TimeSpan.FromDays(1), bundle.Validity);
    }

    [Fact]
    public void Refuses_a_validity_of_less_than_a_day() {
        // Act
        ArgumentOutOfRangeException refused = Assert.Throws<ArgumentOutOfRangeException>(
            () => new DataBundle(1_000, 5m, TimeSpan.FromDays(1) - TimeSpan.FromTicks(1)));

        // Assert
        Assert.Equal("validity", refused.ParamName);
    }

    [Fact]
    public void Refuses_a_negative_validity() {
        // Arrange
        TimeSpan anyNegativeValidity = Any.TimeSpan().Negative().Generate();

        // Act / Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => new DataBundle(1_000, 5m, anyNegativeValidity));
    }

    [Fact]
    public void Accepts_a_very_long_validity() {
        // Arrange: there is no upper bound on how long a bundle lasts
        TimeSpan anyLongValidity = Any.TimeSpan().GreaterThanOrEqualTo(TimeSpan.FromDays(365)).Generate();

        // Act
        Exception? thrown = Record.Exception(() => new DataBundle(1_000, 5m, anyLongValidity));

        // Assert
        Assert.Null(thrown);
    }

    [Theory]
    [InlineData(1_000, "10", "10.24")]      // 10 * 1024 / 1000
    [InlineData(300, "5", "17.07")]         // 17.0666... rounded to cents
    [InlineData(100_000, "500", "5.12")]    // the largest bundle at the highest price
    public void The_price_per_gigabyte_is_rounded_to_cents(int megabytes, string price, string expected) {
        // Arrange
        DataBundle bundle = new DataBundle(megabytes, Dec(price), TimeSpan.FromDays(30));

        // Act
        decimal perGigabyte = bundle.PricePerGigabyte;

        // Assert
        Assert.Equal(Dec(expected), perGigabyte);
    }

}
