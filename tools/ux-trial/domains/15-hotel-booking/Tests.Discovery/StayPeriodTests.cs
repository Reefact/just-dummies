using JustDummies;

using Xunit;

namespace Booking.Domain;

public sealed class StayPeriodTests {

    private static AnyDateOnly AnyDate() {
        return Any.DateOnly().Between(new DateOnly(2000, 1, 1), new DateOnly(2100, 1, 1));
    }

    [Fact]
    public void A_stay_keeps_its_dates_and_counts_its_nights() {
        // Arrange
        DateOnly anyCheckIn = AnyDate().Generate();

        // Act
        StayPeriod stay = new StayPeriod(anyCheckIn, anyCheckIn.AddDays(4));

        // Assert
        Assert.Equal(anyCheckIn, stay.CheckIn);
        Assert.Equal(anyCheckIn.AddDays(4), stay.CheckOut);
        Assert.Equal(4, stay.Nights);
    }

    [Fact]
    public void A_one_night_stay_is_accepted() {
        // Arrange
        DateOnly anyCheckIn = AnyDate().Generate();

        // Act
        StayPeriod stay = new StayPeriod(anyCheckIn, anyCheckIn.AddDays(1));

        // Assert
        Assert.Equal(1, stay.Nights);
    }

    [Fact]
    public void A_stay_of_the_maximum_number_of_nights_is_accepted() {
        // Arrange
        DateOnly anyCheckIn = AnyDate().Generate();

        // Act
        StayPeriod stay = new StayPeriod(anyCheckIn, anyCheckIn.AddDays(StayPeriod.MaxNights));

        // Assert
        Assert.Equal(30, stay.Nights);
    }

    [Fact]
    public void A_stay_one_night_over_the_maximum_is_refused() {
        // Arrange
        DateOnly anyCheckIn = AnyDate().Generate();

        // Act
        ArgumentOutOfRangeException error = Assert.Throws<ArgumentOutOfRangeException>(() => new StayPeriod(anyCheckIn, anyCheckIn.AddDays(31)));

        // Assert
        Assert.Equal("checkOut", error.ParamName);
    }

    [Fact]
    public void A_check_out_on_the_check_in_date_is_refused() {
        // Arrange
        DateOnly anyCheckIn = AnyDate().Generate();

        // Act
        ArgumentException error = Assert.Throws<ArgumentException>(() => new StayPeriod(anyCheckIn, anyCheckIn));

        // Assert
        Assert.Equal("checkOut", error.ParamName);
        Assert.IsNotType<ArgumentOutOfRangeException>(error);
    }

    [Fact]
    public void A_check_out_before_the_check_in_is_refused_even_when_far_before() {
        // Arrange
        DateOnly anyCheckIn = AnyDate().Generate();

        // Act / Assert
        Assert.Throws<ArgumentException>(() => new StayPeriod(anyCheckIn, anyCheckIn.AddDays(-1)));
        Assert.Throws<ArgumentException>(() => new StayPeriod(anyCheckIn, anyCheckIn.AddDays(-400)));
    }

    [Fact]
    public void A_stay_can_cross_a_month_and_a_year() {
        // Act
        StayPeriod stay = new StayPeriod(new DateOnly(2027, 12, 29), new DateOnly(2028, 1, 3));

        // Assert
        Assert.Equal(5, stay.Nights);
    }

    [Fact]
    public void A_stay_can_cross_the_29th_of_february() {
        // Act
        StayPeriod stay = new StayPeriod(new DateOnly(2028, 2, 27), new DateOnly(2028, 3, 2));

        // Assert
        Assert.Equal(4, stay.Nights);
    }

    [Fact]
    public void Two_stays_sharing_a_night_overlap() {
        // Arrange
        DateOnly   anyCheckIn = AnyDate().Generate();
        StayPeriod stay       = new StayPeriod(anyCheckIn, anyCheckIn.AddDays(5));
        StayPeriod other      = new StayPeriod(anyCheckIn.AddDays(4), anyCheckIn.AddDays(8));

        // Act / Assert
        Assert.True(stay.Overlaps(other));
        Assert.True(other.Overlaps(stay));
    }

    [Fact]
    public void A_stay_that_starts_the_day_the_other_ends_does_not_overlap_it() {
        // Arrange
        DateOnly   anyCheckIn = AnyDate().Generate();
        StayPeriod stay       = new StayPeriod(anyCheckIn, anyCheckIn.AddDays(5));
        StayPeriod next       = new StayPeriod(anyCheckIn.AddDays(5), anyCheckIn.AddDays(7));

        // Act / Assert
        Assert.False(stay.Overlaps(next));
        Assert.False(next.Overlaps(stay));
    }

    [Fact]
    public void Two_distant_stays_do_not_overlap() {
        // Arrange
        DateOnly   anyCheckIn = AnyDate().Generate();
        StayPeriod stay       = new StayPeriod(anyCheckIn, anyCheckIn.AddDays(2));
        StayPeriod later      = new StayPeriod(anyCheckIn.AddDays(100), anyCheckIn.AddDays(102));

        // Act / Assert
        Assert.False(stay.Overlaps(later));
        Assert.False(later.Overlaps(stay));
    }

    [Fact]
    public void A_stay_inside_another_overlaps_it() {
        // Arrange
        DateOnly   anyCheckIn = AnyDate().Generate();
        StayPeriod outer      = new StayPeriod(anyCheckIn, anyCheckIn.AddDays(10));
        StayPeriod inner      = new StayPeriod(anyCheckIn.AddDays(3), anyCheckIn.AddDays(4));

        // Act / Assert
        Assert.True(outer.Overlaps(inner));
        Assert.True(inner.Overlaps(outer));
    }

    [Fact]
    public void A_stay_overlaps_itself() {
        // Arrange
        StayPeriod anyStay = Any.StayPeriod().Generate();

        // Act / Assert
        Assert.True(anyStay.Overlaps(anyStay));
    }

    [Fact]
    public void Two_one_night_stays_on_the_same_night_overlap() {
        // Arrange
        DateOnly anyCheckIn = AnyDate().Generate();

        // Act
        StayPeriod first  = new StayPeriod(anyCheckIn, anyCheckIn.AddDays(1));
        StayPeriod second = new StayPeriod(anyCheckIn, anyCheckIn.AddDays(1));

        // Assert
        Assert.True(first.Overlaps(second));
    }

}
