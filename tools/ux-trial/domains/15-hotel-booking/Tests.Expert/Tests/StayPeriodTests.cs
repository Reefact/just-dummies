using JustDummies;

using Xunit;

namespace Booking.Domain.Tests;

public sealed class StayPeriodTests {

    [Fact]
    public void Nights_are_the_days_between_check_in_and_check_out() {
        // Arrange
        DateOnly anyCheckIn = AnyStayPeriod.ValidCheckIn().Generate();

        // Act
        StayPeriod stay = new StayPeriod(anyCheckIn, anyCheckIn.AddDays(4));

        // Assert
        Assert.Equal(4, stay.Nights);
    }

    [Fact]
    public void A_stay_across_a_month_end_counts_the_nights_of_the_calendar() {
        // Act
        StayPeriod stay = new StayPeriod(new DateOnly(2027, 1, 30), new DateOnly(2027, 2, 2));

        // Assert
        Assert.Equal(3, stay.Nights);
    }

    [Fact]
    public void A_stay_across_the_end_of_february_counts_the_leap_day_only_in_a_leap_year() {
        // Act
        StayPeriod leapYear    = new StayPeriod(new DateOnly(2028, 2, 28), new DateOnly(2028, 3, 1));
        StayPeriod commonYear  = new StayPeriod(new DateOnly(2027, 2, 28), new DateOnly(2027, 3, 1));

        // Assert
        Assert.Equal(2, leapYear.Nights);
        Assert.Equal(1, commonYear.Nights);
    }

    [Fact]
    public void A_stay_keeps_its_dates() {
        // Act
        StayPeriod stay = new StayPeriod(new DateOnly(2026, 7, 14), new DateOnly(2026, 7, 16));

        // Assert
        Assert.Equal(new DateOnly(2026, 7, 14), stay.CheckIn);
        Assert.Equal(new DateOnly(2026, 7, 16), stay.CheckOut);
    }

    [Fact]
    public void Leaving_the_day_one_arrives_is_refused() {
        // Arrange
        DateOnly anyDay = AnyStayPeriod.ValidCheckIn().Generate();

        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => new StayPeriod(anyDay, anyDay));

        // Assert
        Assert.Equal("checkOut", refusal.ParamName);
    }

    [Fact]
    public void Leaving_before_one_arrives_is_refused() {
        // Arrange
        DateOnly anyDay = AnyStayPeriod.ValidCheckIn().Generate();

        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => new StayPeriod(anyDay, anyDay.AddDays(-1)));

        // Assert
        Assert.Equal("checkOut", refusal.ParamName);
    }

    [Fact]
    public void A_one_night_stay_is_the_shortest() {
        // Arrange
        DateOnly anyCheckIn = AnyStayPeriod.ValidCheckIn().Generate();

        // Act
        StayPeriod stay = new StayPeriod(anyCheckIn, anyCheckIn.AddDays(1));

        // Assert
        Assert.Equal(1, stay.Nights);
    }

    [Fact]
    public void Thirty_nights_is_the_longest_stay() {
        // Arrange
        DateOnly anyCheckIn = AnyStayPeriod.ValidCheckIn().Generate();

        // Act
        StayPeriod stay = new StayPeriod(anyCheckIn, anyCheckIn.AddDays(StayPeriod.MaxNights));

        // Assert
        Assert.Equal(30, stay.Nights);
    }

    [Fact]
    public void Thirty_one_nights_is_too_long() {
        // Arrange
        DateOnly anyCheckIn = AnyStayPeriod.ValidCheckIn().Generate();

        // Act
        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(() => new StayPeriod(anyCheckIn, anyCheckIn.AddDays(StayPeriod.MaxNights + 1)));

        // Assert
        Assert.Equal("checkOut", refusal.ParamName);
    }

    // The relative position of two stays is what these tests are about, so the offsets are literal days;
    // the day they are all counted from is arbitrary.

    [Theory]
    [InlineData(0, 4, 2, 6)]   // the second starts inside the first
    [InlineData(0, 4, 3, 4)]   // the second shares only the last night
    [InlineData(0, 4, 0, 4)]   // identical
    [InlineData(0, 4, 1, 2)]   // the second is inside the first
    [InlineData(1, 2, 0, 4)]   // the first is inside the second
    [InlineData(2, 6, 0, 4)]   // the second starts before and ends inside
    [InlineData(0, 1, 0, 5)]   // same arrival, different departure
    [InlineData(0, 5, 4, 5)]   // same departure, different arrival
    public void Stays_sharing_a_night_overlap(int firstFrom, int firstTo, int secondFrom, int secondTo) {
        // Arrange
        DateOnly anyDay = AnyStayPeriod.ValidCheckIn().Generate();
        StayPeriod first  = new StayPeriod(anyDay.AddDays(firstFrom), anyDay.AddDays(firstTo));
        StayPeriod second = new StayPeriod(anyDay.AddDays(secondFrom), anyDay.AddDays(secondTo));

        // Act / Assert
        Assert.True(first.Overlaps(second));
        Assert.True(second.Overlaps(first));
    }

    [Theory]
    [InlineData(0, 4, 4, 8)]   // the second arrives the day the first leaves
    [InlineData(0, 4, 5, 8)]   // a gap of a day
    [InlineData(0, 4, 20, 25)] // far apart
    public void Stays_sharing_no_night_do_not_overlap(int firstFrom, int firstTo, int secondFrom, int secondTo) {
        // Arrange
        DateOnly anyDay = AnyStayPeriod.ValidCheckIn().Generate();
        StayPeriod first  = new StayPeriod(anyDay.AddDays(firstFrom), anyDay.AddDays(firstTo));
        StayPeriod second = new StayPeriod(anyDay.AddDays(secondFrom), anyDay.AddDays(secondTo));

        // Act / Assert
        Assert.False(first.Overlaps(second));
        Assert.False(second.Overlaps(first));
    }

}
