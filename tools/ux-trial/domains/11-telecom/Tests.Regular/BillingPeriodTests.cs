using JustDummies;

using Telecom.Domain;

using Xunit;

namespace Telecom.Tests.Regular;

public sealed class BillingPeriodTests {

    private static readonly BillingPeriod March = new BillingPeriod(new DateOnly(2025, 3, 1), new DateOnly(2025, 4, 1));

    [Theory]
    [InlineData(1)]
    [InlineData(28)]
    [InlineData(31)]
    public void Lasts_from_one_to_31_days(int days) {
        // Arrange
        DateOnly anyStart = Any.DateOnly().Between(new DateOnly(2000, 1, 1), new DateOnly(2100, 12, 31)).Generate();

        // Act
        BillingPeriod period = new BillingPeriod(anyStart, anyStart.AddDays(days));

        // Assert
        Assert.Equal(anyStart, period.Start);
        Assert.Equal(anyStart.AddDays(days), period.End);
    }

    [Fact]
    public void Refuses_a_period_of_32_days() {
        // Arrange
        DateOnly anyStart = Any.DateOnly().Between(new DateOnly(2000, 1, 1), new DateOnly(2100, 12, 31)).Generate();

        // Act
        ArgumentException refused = Assert.Throws<ArgumentException>(() => new BillingPeriod(anyStart, anyStart.AddDays(32)));

        // Assert
        Assert.Equal("end", refused.ParamName);
    }

    [Fact]
    public void Refuses_an_empty_period() {
        // Arrange
        DateOnly anyDay = Any.DateOnly().Between(new DateOnly(2000, 1, 1), new DateOnly(2100, 12, 31)).Generate();

        // Act / Assert
        Assert.Throws<ArgumentException>(() => new BillingPeriod(anyDay, anyDay));
    }

    [Fact]
    public void Refuses_an_end_before_the_start() {
        // Arrange
        DateOnly anyStart = Any.DateOnly().Between(new DateOnly(2000, 1, 10), new DateOnly(2100, 12, 31)).Generate();

        // Act / Assert
        Assert.Throws<ArgumentException>(() => new BillingPeriod(anyStart, anyStart.AddDays(-1)));
    }

    [Fact]
    public void Accepts_any_generated_period() {
        // Act
        Exception? thrown = Record.Exception(() => Any.BillingPeriod().Generate());

        // Assert
        Assert.Null(thrown);
    }

    [Fact]
    public void Contains_the_first_instant_of_its_start_day() {
        Assert.True(March.Contains(new DateTime(2025, 3, 1, 0, 0, 0, DateTimeKind.Utc)));
    }

    [Fact]
    public void Contains_the_last_instant_of_the_day_before_its_end() {
        Assert.True(March.Contains(new DateTime(2025, 3, 31, 23, 59, 59, 999, DateTimeKind.Utc)));
    }

    [Fact]
    public void Does_not_contain_the_first_instant_of_its_end_day() {
        Assert.False(March.Contains(new DateTime(2025, 4, 1, 0, 0, 0, DateTimeKind.Utc)));
    }

    [Fact]
    public void Does_not_contain_the_last_instant_before_its_start() {
        Assert.False(March.Contains(new DateTime(2025, 2, 28, 23, 59, 59, 999, DateTimeKind.Utc)));
    }

    [Theory]
    [InlineData(DateTimeKind.Utc)]
    [InlineData(DateTimeKind.Local)]
    [InlineData(DateTimeKind.Unspecified)]
    public void Compares_the_date_part_whatever_the_kind(DateTimeKind kind) {
        Assert.True(March.Contains(new DateTime(2025, 3, 15, 12, 0, 0, kind)));
        Assert.False(March.Contains(new DateTime(2025, 4, 1, 12, 0, 0, kind)));
    }

    [Fact]
    public void Contains_an_instant_drawn_inside_the_period() {
        // Arrange: the period is the subject here, the time of day inside it is not
        TimeSpan anyTimeOfDay = Any.TimeSpan().GreaterThanOrEqualTo(TimeSpan.Zero).LessThan(TimeSpan.FromDays(1)).Generate();
        int      anyDayOffset = Any.Int32().Between(0, 30).Generate();
        DateTime anyInstant   = new DateTime(2025, 3, 1, 0, 0, 0, DateTimeKind.Utc).AddDays(anyDayOffset) + anyTimeOfDay;

        // Act
        bool contained = March.Contains(anyInstant);

        // Assert
        Assert.True(contained);
    }

}
