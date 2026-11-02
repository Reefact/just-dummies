using JustDummies;

using Payroll.Domain;

using Xunit;

namespace Payroll.Tests.Regular;

public sealed class PayPeriodTests {

    [Theory]
    [InlineData(2024, 1, 31)]
    [InlineData(2024, 2, 29)]
    [InlineData(2023, 2, 28)]
    [InlineData(2100, 2, 28)]
    [InlineData(2000, 2, 29)]
    [InlineData(2025, 4, 30)]
    [InlineData(2025, 12, 31)]
    public void A_period_runs_from_the_first_to_the_last_day_of_its_month(int year, int month, int lastDayOfMonth) {
        // Act
        PayPeriod period = new PayPeriod(year, month);

        // Assert
        Assert.Equal(new DateOnly(year, month, 1), period.FirstDay);
        Assert.Equal(new DateOnly(year, month, lastDayOfMonth), period.LastDay);
    }

    [Fact]
    public void A_period_remembers_its_year_and_month() {
        // Act
        PayPeriod period = new PayPeriod(2031, 9);

        // Assert
        Assert.Equal(2031, period.Year);
        Assert.Equal(9, period.Month);
    }

    [Theory]
    [InlineData(2000)]
    [InlineData(2100)]
    public void The_first_and_the_last_year_are_accepted(int year) {
        // Arrange
        int anyMonth = Any.Int32().Between(1, 12).Generate();

        // Act
        PayPeriod period = new PayPeriod(year, anyMonth);

        // Assert
        Assert.Equal(year, period.Year);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(12)]
    public void The_first_and_the_last_month_are_accepted(int month) {
        // Arrange
        int anyYear = Any.Int32().Between(2000, 2100).Generate();

        // Act
        PayPeriod period = new PayPeriod(anyYear, month);

        // Assert
        Assert.Equal(month, period.Month);
    }

    [Fact]
    public void A_year_before_2000_is_refused() {
        // Arrange
        int anyMonth = Any.Int32().Between(1, 12).Generate();
        int anyEarlyYear = Any.Int32().LessThan(2000).Generate();

        // Act
        ArgumentOutOfRangeException exception = Assert.Throws<ArgumentOutOfRangeException>(() => new PayPeriod(anyEarlyYear, anyMonth));

        // Assert
        Assert.Equal("year", exception.ParamName);
    }

    [Fact]
    public void A_year_after_2100_is_refused() {
        // Arrange
        int anyMonth = Any.Int32().Between(1, 12).Generate();
        int anyLateYear = Any.Int32().GreaterThan(2100).Generate();

        // Act
        ArgumentOutOfRangeException exception = Assert.Throws<ArgumentOutOfRangeException>(() => new PayPeriod(anyLateYear, anyMonth));

        // Assert
        Assert.Equal("year", exception.ParamName);
    }

    [Theory]
    [InlineData(1999)]
    [InlineData(2101)]
    public void A_year_one_step_outside_the_range_is_refused(int year) {
        Assert.Throws<ArgumentOutOfRangeException>(() => new PayPeriod(year, 6));
    }

    [Fact]
    public void A_month_below_one_is_refused() {
        // Arrange
        int anyYear = Any.Int32().Between(2000, 2100).Generate();
        int anyMonth = Any.Int32().LessThan(1).Generate();

        // Act
        ArgumentOutOfRangeException exception = Assert.Throws<ArgumentOutOfRangeException>(() => new PayPeriod(anyYear, anyMonth));

        // Assert
        Assert.Equal("month", exception.ParamName);
    }

    [Fact]
    public void A_month_above_twelve_is_refused() {
        // Arrange
        int anyYear = Any.Int32().Between(2000, 2100).Generate();
        int anyMonth = Any.Int32().GreaterThan(12).Generate();

        // Act
        ArgumentOutOfRangeException exception = Assert.Throws<ArgumentOutOfRangeException>(() => new PayPeriod(anyYear, anyMonth));

        // Assert
        Assert.Equal("month", exception.ParamName);
    }

    [Fact]
    public void The_year_is_checked_before_the_month() {
        // Act
        ArgumentOutOfRangeException exception = Assert.Throws<ArgumentOutOfRangeException>(() => new PayPeriod(1990, 13));

        // Assert
        Assert.Equal("year", exception.ParamName);
    }

}
