using Booking.Domain;
using JustDummies;
using JustDummies.Xunit;
using Xunit;

namespace Booking.Tests;

[Reproducible]
public sealed class GuestAndRoomTests {

    [Fact]
    public void The_display_name_puts_the_last_name_first_in_capitals() {
        // Arrange
        GuestProfile anyJaneDoe = Any.GuestProfile()
                                     .WithFirstName(new GuestName("Jane"))
                                     .WithLastName(new GuestName("Doe"))
                                     .Generate();

        // Act
        string displayName = anyJaneDoe.DisplayName;

        // Assert
        Assert.Equal("DOE, Jane", displayName);
    }

    [Fact]
    public void A_guest_without_a_loyalty_number_is_not_a_member() {
        // Arrange
        GuestProfile anyNonMember = Any.GuestProfile().WithLoyaltyNumber((string?)null).Generate();

        // Act
        bool member = anyNonMember.IsLoyaltyMember;

        // Assert
        Assert.False(member);
    }

    [Fact]
    public void The_floor_is_read_from_the_room_number() {
        // Arrange
        RoomNumber anyTwelfthFloorRoom = Any.StringMatching("^12[0-9]{2}[A-D]?$").As(value => new RoomNumber(value)).Generate();

        // Act
        int floor = anyTwelfthFloorRoom.Floor;

        // Assert
        Assert.Equal(12, floor);
    }

    [Fact]
    public void A_stay_counts_its_nights() {
        // Arrange
        StayPeriod anyFourNightStay = new AnyStayPeriod().WithNights(4).Generate();

        // Act
        int nights = anyFourNightStay.Nights;

        // Assert
        Assert.Equal(4, nights);
    }

    [Fact]
    public void A_stay_longer_than_thirty_nights_is_refused() {
        // Arrange
        DateOnly anyCheckIn = Any.DateOnly().Between(new DateOnly(2025, 1, 1), new DateOnly(2035, 12, 31)).Generate();

        // Act
        Action thirtyOneNights = () => new StayPeriod(anyCheckIn, anyCheckIn.AddDays(31));

        // Assert
        Assert.Throws<ArgumentOutOfRangeException>(thirtyOneNights);
    }

}
