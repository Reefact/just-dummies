using JustDummies;

using Xunit;

namespace Booking.Domain.Tests.Regular;

public sealed class StayPeriodTests {

    private static AnyDateOnly AnyDate() => Any.DateOnly().Between(new DateOnly(2020, 1, 1), new DateOnly(2040, 12, 31));

    [Fact]
    public void A_stay_keeps_its_check_in_and_its_check_out() {
        // Act
        StayPeriod stay = new StayPeriod(new DateOnly(2026, 7, 1), new DateOnly(2026, 7, 8));

        // Assert
        Assert.Equal(new DateOnly(2026, 7, 1), stay.CheckIn);
        Assert.Equal(new DateOnly(2026, 7, 8), stay.CheckOut);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(7)]
    [InlineData(30)]
    public void The_number_of_nights_is_the_day_difference(int nights) {
        // Arrange
        DateOnly anyCheckIn = AnyDate().Generate();

        // Act
        StayPeriod stay = new StayPeriod(anyCheckIn, anyCheckIn.AddDays(nights));

        // Assert
        Assert.Equal(nights, stay.Nights);
    }

    [Fact]
    public void Nights_are_counted_across_a_month_and_a_leap_day() {
        // Act
        StayPeriod stay = new StayPeriod(new DateOnly(2028, 2, 27), new DateOnly(2028, 3, 2));

        // Assert: 27, 28, 29 February and 1 March
        Assert.Equal(4, stay.Nights);
    }

    [Fact]
    public void A_check_out_equal_to_the_check_in_is_refused() {
        // Arrange
        DateOnly anyDate = AnyDate().Generate();

        // Act
        ArgumentException exception = Assert.Throws<ArgumentException>(() => new StayPeriod(anyDate, anyDate));

        // Assert
        Assert.Equal("checkOut", exception.ParamName);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(30)]
    [InlineData(400)]
    public void A_check_out_before_the_check_in_is_refused(int daysBefore) {
        // Arrange
        DateOnly anyCheckIn = AnyDate().Generate();

        // Act & Assert
        Assert.Throws<ArgumentException>(() => new StayPeriod(anyCheckIn, anyCheckIn.AddDays(-daysBefore)));
    }

    [Fact]
    public void A_stay_of_thirty_one_nights_is_refused() {
        // Arrange
        DateOnly anyCheckIn = AnyDate().Generate();

        // Act
        ArgumentOutOfRangeException exception = Assert.Throws<ArgumentOutOfRangeException>(() => new StayPeriod(anyCheckIn, anyCheckIn.AddDays(StayPeriod.MaxNights + 1)));

        // Assert
        Assert.Equal("checkOut", exception.ParamName);
    }

    [Fact]
    public void A_stay_of_a_very_long_duration_is_refused() {
        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => new StayPeriod(new DateOnly(2026, 1, 1), new DateOnly(2030, 1, 1)));
    }

    [Fact]
    public void The_maximum_is_thirty_nights() {
        // Assert
        Assert.Equal(30, StayPeriod.MaxNights);
    }

    [Fact]
    public void Two_stays_that_share_a_night_overlap() {
        // Arrange
        StayPeriod first  = new StayPeriod(new DateOnly(2026, 7, 1), new DateOnly(2026, 7, 8));
        StayPeriod second = new StayPeriod(new DateOnly(2026, 7, 7), new DateOnly(2026, 7, 10));

        // Assert
        Assert.True(first.Overlaps(second));
        Assert.True(second.Overlaps(first));
    }

    [Fact]
    public void A_stay_overlaps_itself() {
        // Arrange
        StayPeriod anyStay = Arbitrary.StayPeriod().Generate();

        // Assert
        Assert.True(anyStay.Overlaps(anyStay));
    }

    [Fact]
    public void A_stay_that_starts_the_day_the_other_ends_does_not_overlap_it() {
        // Arrange: the check-out day is free for the next guest
        StayPeriod first  = new StayPeriod(new DateOnly(2026, 7, 1), new DateOnly(2026, 7, 8));
        StayPeriod second = new StayPeriod(new DateOnly(2026, 7, 8), new DateOnly(2026, 7, 10));

        // Assert
        Assert.False(first.Overlaps(second));
        Assert.False(second.Overlaps(first));
    }

    [Fact]
    public void A_stay_inside_another_overlaps_it() {
        // Arrange
        StayPeriod outer = new StayPeriod(new DateOnly(2026, 7, 1), new DateOnly(2026, 7, 20));
        StayPeriod inner = new StayPeriod(new DateOnly(2026, 7, 5), new DateOnly(2026, 7, 6));

        // Assert
        Assert.True(outer.Overlaps(inner));
        Assert.True(inner.Overlaps(outer));
    }

    [Fact]
    public void Two_stays_with_a_day_between_them_do_not_overlap() {
        // Arrange
        StayPeriod first  = new StayPeriod(new DateOnly(2026, 7, 1), new DateOnly(2026, 7, 8));
        StayPeriod second = new StayPeriod(new DateOnly(2026, 7, 9), new DateOnly(2026, 7, 12));

        // Assert
        Assert.False(first.Overlaps(second));
        Assert.False(second.Overlaps(first));
    }

    [Fact]
    public void Two_stays_far_apart_do_not_overlap() {
        // Arrange
        DateOnly   anyCheckIn = AnyDate().Generate();
        StayPeriod first      = Arbitrary.StayPeriod().WithCheckIn(anyCheckIn).Generate();
        StayPeriod later      = Arbitrary.StayPeriod().WithCheckIn(anyCheckIn.AddDays(StayPeriod.MaxNights + 1)).Generate();

        // Assert
        Assert.False(first.Overlaps(later));
        Assert.False(later.Overlaps(first));
    }

    [Fact]
    public void The_generator_only_draws_stays_of_one_to_thirty_nights() {
        // Act
        for (int i = 0; i < 200; i++) {
            StayPeriod anyStay = Arbitrary.StayPeriod().Generate();

            Assert.InRange(anyStay.Nights, 1, StayPeriod.MaxNights);
        }
    }

    [Fact]
    public void The_generator_can_pin_the_number_of_nights() {
        // Act
        StayPeriod stay = Arbitrary.StayPeriod().WithNights(12).Generate();

        // Assert
        Assert.Equal(12, stay.Nights);
    }

}
