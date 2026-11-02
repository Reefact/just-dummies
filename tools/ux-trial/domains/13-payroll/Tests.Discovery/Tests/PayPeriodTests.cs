using Payroll.Domain;

using Xunit;

namespace Payroll.Tests.Discovery;

public sealed class PayPeriodTests {

    [Theory]
    [InlineData(2000, 1)]
    [InlineData(2100, 12)]
    [InlineData(2024, 6)]
    public void A_year_between_2000_and_2100_and_a_month_between_1_and_12_are_kept(int year, int month) {
        PayPeriod period = new PayPeriod(year, month);

        Assert.Equal(year, period.Year);
        Assert.Equal(month, period.Month);
    }

    [Theory]
    [InlineData(1999)]
    [InlineData(2101)]
    [InlineData(0)]
    [InlineData(-2024)]
    public void A_year_outside_2000_to_2100_is_refused(int year) {
        ArgumentOutOfRangeException exception = Assert.Throws<ArgumentOutOfRangeException>(() => new PayPeriod(year, 6));

        Assert.Equal("year", exception.ParamName);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(13)]
    [InlineData(-1)]
    public void A_month_outside_1_to_12_is_refused(int month) {
        ArgumentOutOfRangeException exception = Assert.Throws<ArgumentOutOfRangeException>(() => new PayPeriod(2024, month));

        Assert.Equal("month", exception.ParamName);
    }

    [Fact]
    public void The_year_is_checked_before_the_month() {
        ArgumentOutOfRangeException exception = Assert.Throws<ArgumentOutOfRangeException>(() => new PayPeriod(1999, 13));

        Assert.Equal("year", exception.ParamName);
    }

    [Theory]
    [InlineData(2024, 1, 31)]
    [InlineData(2024, 2, 29)]
    [InlineData(2023, 2, 28)]
    [InlineData(2000, 2, 29)]
    [InlineData(2100, 2, 28)]
    [InlineData(2024, 4, 30)]
    [InlineData(2024, 12, 31)]
    [InlineData(2100, 12, 31)]
    public void The_first_day_is_the_1st_and_the_last_day_closes_the_month(int year, int month, int lastDayOfMonth) {
        PayPeriod period = new PayPeriod(year, month);

        Assert.Equal(new DateOnly(year, month, 1), period.FirstDay);
        Assert.Equal(new DateOnly(year, month, lastDayOfMonth), period.LastDay);
    }

}
