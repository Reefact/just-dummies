using Insurance.Domain;

using JustDummies;

using Xunit;

namespace Insurance.Tests.Regular;

public sealed class CoveragePeriodTests {

    [Fact]
    public void A_period_keeps_its_two_days() {
        // Arrange
        DateOnly anyStart = Any.DateOnly().Between(new DateOnly(2000, 1, 1), new DateOnly(2100, 1, 1)).Generate();
        DateOnly anyEnd   = anyStart.AddDays(Any.Int32().Between(1, 365).Generate());

        // Act
        CoveragePeriod period = new CoveragePeriod(anyStart, anyEnd);

        // Assert
        Assert.Equal(anyStart, period.Start);
        Assert.Equal(anyEnd, period.End);
    }

    [Fact]
    public void A_period_cannot_end_on_the_day_it_starts() {
        DateOnly anyDay = Any.DateOnly().Between(new DateOnly(2000, 1, 1), new DateOnly(2100, 1, 1)).Generate();

        Assert.Throws<ArgumentException>(() => new CoveragePeriod(anyDay, anyDay));
    }

    [Fact]
    public void A_period_cannot_end_before_it_starts() {
        DateOnly anyStart = Any.DateOnly().Between(new DateOnly(2000, 1, 1), new DateOnly(2100, 1, 1)).Generate();
        DateOnly anyEnd   = anyStart.AddDays(-Any.Int32().Between(1, 1000).Generate());

        Assert.Throws<ArgumentException>(() => new CoveragePeriod(anyStart, anyEnd));
    }

    [Fact]
    public void A_period_of_exactly_one_calendar_year_is_accepted() {
        DateOnly anyStart = Any.DateOnly().Between(new DateOnly(2000, 1, 1), new DateOnly(2100, 1, 1)).Generate();

        CoveragePeriod period = new CoveragePeriod(anyStart, anyStart.AddYears(1));

        Assert.Equal(anyStart.AddYears(1), period.End);
    }

    [Fact]
    public void A_period_one_day_beyond_the_year_is_refused() {
        DateOnly anyStart = Any.DateOnly().Between(new DateOnly(2000, 1, 1), new DateOnly(2100, 1, 1)).Generate();

        Assert.Throws<ArgumentException>(() => new CoveragePeriod(anyStart, anyStart.AddYears(1).AddDays(1)));
    }

    [Fact]
    public void A_period_much_longer_than_a_year_is_refused() {
        DateOnly anyStart = Any.DateOnly().Between(new DateOnly(2000, 1, 1), new DateOnly(2100, 1, 1)).Generate();
        DateOnly anyEnd   = anyStart.AddYears(1).AddDays(Any.Int32().Between(1, 5000).Generate());

        Assert.Throws<ArgumentException>(() => new CoveragePeriod(anyStart, anyEnd));
    }

    [Theory]
    [InlineData("2026-01-01", "2027-01-01", true)]
    [InlineData("2026-01-01", "2027-01-02", false)]
    [InlineData("2024-01-01", "2025-01-01", true)]     // a leap year: 367 days, inclusive
    [InlineData("2024-02-29", "2025-02-28", true)]     // AddYears clamps the leap day
    [InlineData("2024-02-29", "2025-03-01", false)]
    [InlineData("2026-01-01", "2026-01-02", true)]
    public void The_one_year_ceiling_is_measured_in_calendar_years(string start, string end, bool accepted) {
        DateOnly from = DateOnly.Parse(start, System.Globalization.CultureInfo.InvariantCulture);
        DateOnly to   = DateOnly.Parse(end, System.Globalization.CultureInfo.InvariantCulture);

        if (accepted) {
            Assert.Equal(to, new CoveragePeriod(from, to).End);
        } else {
            Assert.Throws<ArgumentException>(() => new CoveragePeriod(from, to));
        }
    }

    [Theory]
    [InlineData("2026-01-01", "2026-01-02", 2)]
    [InlineData("2026-01-01", "2026-01-31", 31)]
    [InlineData("2026-01-01", "2026-12-31", 365)]
    [InlineData("2024-01-01", "2024-12-31", 366)]
    [InlineData("2024-01-01", "2025-01-01", 367)]
    public void The_days_count_both_ends(string start, string end, int expected) {
        CoveragePeriod period = new CoveragePeriod(DateOnly.Parse(start, System.Globalization.CultureInfo.InvariantCulture),
                                                   DateOnly.Parse(end, System.Globalization.CultureInfo.InvariantCulture));

        Assert.Equal(expected, period.Days);
    }

    [Fact]
    public void The_days_are_the_distance_between_the_ends_plus_one() {
        int            anyDaysAfterStart = Any.Int32().Between(1, 365).Generate();
        CoveragePeriod anyPeriod         = Any.CoveragePeriod().WithDaysAfterStart(anyDaysAfterStart).Generate();

        Assert.Equal(anyDaysAfterStart + 1, anyPeriod.Days);
    }

    [Fact]
    public void Both_ends_of_the_period_are_covered() {
        CoveragePeriod anyPeriod = Any.CoveragePeriod().Generate();

        Assert.True(anyPeriod.Contains(anyPeriod.Start));
        Assert.True(anyPeriod.Contains(anyPeriod.End));
    }

    [Fact]
    public void A_day_inside_the_period_is_covered() {
        CoveragePeriod anyPeriod = Any.CoveragePeriod().Generate();
        DateOnly       anyDay    = Any.DateOnly().Between(anyPeriod.Start, anyPeriod.End).Generate();

        Assert.True(anyPeriod.Contains(anyDay));
    }

    [Fact]
    public void The_days_around_the_period_are_not_covered() {
        CoveragePeriod anyPeriod = Any.CoveragePeriod().Generate();

        Assert.False(anyPeriod.Contains(anyPeriod.Start.AddDays(-1)));
        Assert.False(anyPeriod.Contains(anyPeriod.End.AddDays(1)));
    }

    [Fact]
    public void A_day_far_outside_the_period_is_not_covered() {
        CoveragePeriod anyPeriod = Any.CoveragePeriod().Generate();
        DateOnly       anyBefore = Any.DateOnly().Before(anyPeriod.Start).Generate();
        DateOnly       anyAfter  = Any.DateOnly().After(anyPeriod.End).Generate();

        Assert.False(anyPeriod.Contains(anyBefore));
        Assert.False(anyPeriod.Contains(anyAfter));
    }

    [Fact]
    public void The_next_period_starts_the_day_after_and_spans_as_many_days() {
        CoveragePeriod anyPeriod = Any.CoveragePeriod().Generate();

        CoveragePeriod next = anyPeriod.Next();

        Assert.Equal(anyPeriod.End.AddDays(1), next.Start);
        Assert.Equal(anyPeriod.Days, next.Days);
    }

    [Theory]
    [InlineData("2026-01-01", "2026-12-31", "2027-01-01", "2027-12-31")]
    [InlineData("2026-01-01", "2026-01-31", "2026-02-01", "2026-03-03")]
    [InlineData("2026-07-01", "2026-07-02", "2026-07-03", "2026-07-04")]
    public void The_next_period_of_a_known_period(string start, string end, string nextStart, string nextEnd) {
        CoveragePeriod next = new CoveragePeriod(DateOnly.Parse(start, System.Globalization.CultureInfo.InvariantCulture),
                                                 DateOnly.Parse(end, System.Globalization.CultureInfo.InvariantCulture)).Next();

        Assert.Equal(DateOnly.Parse(nextStart, System.Globalization.CultureInfo.InvariantCulture), next.Start);
        Assert.Equal(DateOnly.Parse(nextEnd, System.Globalization.CultureInfo.InvariantCulture), next.End);
    }

    [Fact]
    public void A_period_of_a_whole_leap_span_cannot_always_be_renewed() {
        CoveragePeriod leapSpan = new CoveragePeriod(new DateOnly(2028, 1, 1), new DateOnly(2029, 1, 1));

        Assert.Equal(367, leapSpan.Days);
        Assert.Throws<ArgumentException>(() => leapSpan.Next());
    }

}
