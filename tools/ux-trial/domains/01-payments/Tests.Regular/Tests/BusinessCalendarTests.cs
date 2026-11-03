using JustDummies;

using Xunit;

namespace Payments.Domain.Tests;

public sealed class BusinessCalendarTests {

    [Theory]
    [InlineData(2026, 3, 2)]    // Monday
    [InlineData(2026, 3, 4)]    // Wednesday
    [InlineData(2026, 3, 6)]    // Friday
    [InlineData(2026, 12, 24)]  // Christmas Eve is not a closing day
    [InlineData(2026, 12, 31)]
    [InlineData(2026, 4, 30)]
    [InlineData(2026, 5, 4)]
    public void An_ordinary_weekday_is_a_business_day(int year, int month, int day) {
        Assert.True(BusinessCalendar.IsBusinessDay(new DateOnly(year, month, day)));
    }

    [Theory]
    [InlineData(2026, 3, 7)]    // Saturday
    [InlineData(2026, 3, 8)]    // Sunday
    public void A_weekend_day_is_not_a_business_day(int year, int month, int day) {
        Assert.False(BusinessCalendar.IsBusinessDay(new DateOnly(year, month, day)));
    }

    [Theory]
    [InlineData(2026, 1, 1)]    // Thursday
    [InlineData(2025, 5, 1)]    // Thursday
    [InlineData(2024, 12, 25)]  // Wednesday
    [InlineData(2024, 12, 26)]  // Thursday
    [InlineData(2022, 1, 1)]    // Saturday as well
    [InlineData(2023, 12, 25)]  // Monday
    public void A_fixed_closing_day_is_not_a_business_day(int year, int month, int day) {
        Assert.False(BusinessCalendar.IsBusinessDay(new DateOnly(year, month, day)));
    }

    [Fact]
    public void The_first_of_May_is_closed_whatever_the_year() {
        // Arrange
        int anyYear = Any.Int32().Between(2000, 2100).Generate();

        // Assert
        Assert.False(BusinessCalendar.IsBusinessDay(new DateOnly(anyYear, 5, 1)));
    }

    [Theory]
    [InlineData(2026, 3, 4, 2026, 3, 4)]       // a business day stays put
    [InlineData(2026, 3, 7, 2026, 3, 9)]       // Saturday -> Monday
    [InlineData(2026, 3, 8, 2026, 3, 9)]       // Sunday -> Monday
    [InlineData(2024, 12, 25, 2024, 12, 27)]   // Wednesday holiday, then Thursday holiday
    [InlineData(2025, 12, 25, 2025, 12, 29)]   // 25 and 26 Thursday/Friday, then the weekend
    [InlineData(2023, 12, 31, 2024, 1, 2)]     // Sunday, then 1 January
    [InlineData(2026, 5, 1, 2026, 5, 4)]       // Friday holiday -> Monday
    public void The_next_business_day_is_the_first_one_on_or_after_the_date(int year, int month, int day, int expectedYear, int expectedMonth, int expectedDay) {
        // Act
        DateOnly next = BusinessCalendar.NextBusinessDayOnOrAfter(new DateOnly(year, month, day));

        // Assert
        Assert.Equal(new DateOnly(expectedYear, expectedMonth, expectedDay), next);
    }

    [Fact]
    public void The_next_business_day_of_any_date_is_a_business_day_within_four_days() {
        // Arrange
        DateOnly anyDate = Any.DateOnly().Between(new DateOnly(2000, 1, 1), new DateOnly(2100, 12, 31)).Generate();

        // Act
        DateOnly next = BusinessCalendar.NextBusinessDayOnOrAfter(anyDate);

        // Assert
        Assert.True(BusinessCalendar.IsBusinessDay(next));
        Assert.InRange(next.DayNumber - anyDate.DayNumber, 0, 4);
    }

}
