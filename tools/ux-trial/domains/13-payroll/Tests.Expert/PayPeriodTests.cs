namespace Payroll.Tests.Expert;

public sealed class PayPeriodTests {

    [Theory]
    [InlineData(2000)]
    [InlineData(2100)]
    public void A_year_at_the_edge_of_the_supported_range_is_accepted(int year) {
        // Arrange
        int anyMonth = Any.Int32().Between(1, 12).Generate();

        // Act
        PayPeriod period = new PayPeriod(year, anyMonth);

        // Assert
        Assert.Equal(year, period.Year);
    }

    [Theory]
    [InlineData(1999)]
    [InlineData(2101)]
    [InlineData(0)]
    [InlineData(-2024)]
    public void A_year_outside_2000_to_2100_is_refused(int year) {
        // Arrange
        int anyMonth = Any.Int32().Between(1, 12).Generate();

        // Act
        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(() => new PayPeriod(year, anyMonth));

        // Assert
        Assert.Equal("year", refusal.ParamName);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(12)]
    public void A_month_at_the_edge_of_the_calendar_is_accepted(int month) {
        // Arrange
        int anyYear = Any.Int32().Between(2000, 2100).Generate();

        // Act
        PayPeriod period = new PayPeriod(anyYear, month);

        // Assert
        Assert.Equal(month, period.Month);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(13)]
    [InlineData(-1)]
    public void A_month_outside_1_to_12_is_refused(int month) {
        // Arrange
        int anyYear = Any.Int32().Between(2000, 2100).Generate();

        // Act
        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(() => new PayPeriod(anyYear, month));

        // Assert
        Assert.Equal("month", refusal.ParamName);
    }

    [Fact]
    public void A_period_whose_year_and_month_are_both_wrong_is_refused_on_the_year_first() {
        // Act
        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(() => new PayPeriod(1999, 13));

        // Assert
        Assert.Equal("year", refusal.ParamName);
    }

    [Theory]
    [InlineData(2025, 1, 31)]
    [InlineData(2025, 4, 30)]
    [InlineData(2025, 12, 31)]
    [InlineData(2024, 2, 29)]   // a leap year
    [InlineData(2023, 2, 28)]
    [InlineData(2000, 2, 29)]   // divisible by 400: leap
    [InlineData(2100, 2, 28)]   // divisible by 100 but not by 400: not leap
    [InlineData(2100, 12, 31)]  // the last month supported
    public void The_last_day_of_a_period_is_the_last_day_of_its_month(int year, int month, int lastDay) {
        // Act
        PayPeriod period = new PayPeriod(year, month);

        // Assert
        Assert.Equal(new DateOnly(year, month, lastDay), period.LastDay);
    }

    [Theory]
    [InlineData(2025, 1)]
    [InlineData(2024, 2)]
    [InlineData(2100, 12)]
    public void The_first_day_of_a_period_is_the_first_of_its_month(int year, int month) {
        // Act
        PayPeriod period = new PayPeriod(year, month);

        // Assert
        Assert.Equal(new DateOnly(year, month, 1), period.FirstDay);
    }

}
