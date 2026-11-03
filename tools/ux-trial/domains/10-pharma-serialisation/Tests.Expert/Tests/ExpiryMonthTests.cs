using Xunit;

namespace Serialisation.Domain.Tests.Expert;

public sealed class ExpiryMonthTests {

    [Theory]
    [InlineData(2000, 1)]
    [InlineData(2099, 12)]
    [InlineData(2027, 3)]
    public void A_month_between_2000_and_2099_is_accepted(int year, int month) {
        // Act
        ExpiryMonth expiry = ExpiryMonth.Of(year, month);

        // Assert
        Assert.Equal(year, expiry.Year);
        Assert.Equal(month, expiry.Month);
    }

    [Theory]
    [InlineData(1999)]
    [InlineData(2100)]
    [InlineData(0)]
    [InlineData(-2027)]
    public void A_year_outside_2000_to_2099_is_refused(int year) {
        // Act
        ArgumentOutOfRangeException exception = Assert.Throws<ArgumentOutOfRangeException>(() => ExpiryMonth.Of(year, 6));

        // Assert
        Assert.Equal("year", exception.ParamName);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(13)]
    [InlineData(-1)]
    public void A_month_outside_1_to_12_is_refused(int month) {
        // Act
        ArgumentOutOfRangeException exception = Assert.Throws<ArgumentOutOfRangeException>(() => ExpiryMonth.Of(2027, month));

        // Assert
        Assert.Equal("month", exception.ParamName);
    }

    [Theory]
    [InlineData(2027, 1, 31)]
    [InlineData(2027, 2, 28)]
    [InlineData(2028, 2, 29)]  // a leap year
    [InlineData(2000, 2, 29)]  // divisible by 400
    [InlineData(2027, 3, 31)]
    [InlineData(2027, 4, 30)]
    [InlineData(2027, 5, 31)]
    [InlineData(2027, 6, 30)]
    [InlineData(2027, 7, 31)]
    [InlineData(2027, 8, 31)]
    [InlineData(2027, 9, 30)]
    [InlineData(2027, 10, 31)]
    [InlineData(2027, 11, 30)]
    [InlineData(2099, 12, 31)]
    public void The_last_day_is_the_last_day_of_that_month(int year, int month, int lastDay) {
        // Arrange
        ExpiryMonth expiry = ExpiryMonth.Of(year, month);

        // Act
        DateOnly actual = expiry.LastDay;

        // Assert
        Assert.Equal(new DateOnly(year, month, lastDay), actual);
    }

    [Fact]
    public void Two_months_of_the_same_year_and_month_are_equal_and_share_a_hash_code() {
        // Arrange
        ExpiryMonth first  = ExpiryMonth.Of(2027, 3);
        ExpiryMonth second = ExpiryMonth.Of(2027, 3);

        // Assert
        Assert.True(first.Equals(second));
        Assert.Equal(first.GetHashCode(), second.GetHashCode());
    }

    [Fact]
    public void Months_differing_only_by_month_are_different() {
        // Assert
        Assert.False(ExpiryMonth.Of(2027, 3).Equals(ExpiryMonth.Of(2027, 4)));
    }

    [Fact]
    public void Months_differing_only_by_year_are_different() {
        // Assert
        Assert.False(ExpiryMonth.Of(2027, 3).Equals(ExpiryMonth.Of(2028, 3)));
    }

    [Fact]
    public void A_month_is_not_equal_to_null() {
        // Assert
        Assert.False(ExpiryMonth.Of(2027, 3).Equals(null));
    }

}
