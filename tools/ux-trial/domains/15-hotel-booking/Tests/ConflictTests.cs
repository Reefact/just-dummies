using Booking.Domain;
using JustDummies;
using JustDummies.Xunit;
using Xunit;

namespace Booking.Tests;

[Reproducible]
public sealed class ConflictTests {

    private static readonly IAny<DateOnly> AnyArrival = Any.DateOnly().Between(new DateOnly(2025, 1, 1), new DateOnly(2035, 12, 31));

    [Fact]
    public void Overlapping_stays_in_the_same_room_conflict() {
        // Arrange
        Room        anyRoom    = new AnyRoom().Generate();
        DateOnly    anyCheckIn = AnyArrival.Generate();
        Reservation anyFirst   = Arbitrary.Reservation().WithRoom(anyRoom).WithStay(new StayPeriod(anyCheckIn, anyCheckIn.AddDays(3))).Generate();
        Reservation anySecond  = Arbitrary.Reservation().WithRoom(anyRoom).WithStay(new StayPeriod(anyCheckIn.AddDays(2), anyCheckIn.AddDays(5))).Generate();

        // Act
        bool conflict = anyFirst.ConflictsWith(anySecond);

        // Assert
        Assert.True(conflict);
    }

    [Fact]
    public void A_departure_and_an_arrival_on_the_same_day_do_not_conflict() {
        // Arrange
        Room        anyRoom      = new AnyRoom().Generate();
        StayPeriod  anyFirstStay = new AnyStayPeriod().WithCheckIn(d => d.Before(new DateOnly(2035, 1, 1))).Generate();
        Reservation anyFirst     = Arbitrary.Reservation().WithRoom(anyRoom).WithStay(anyFirstStay).Generate();
        Reservation anyNext      = Arbitrary.Reservation()
                                            .WithRoom(anyRoom)
                                            .WithStay(stay => stay.WithCheckIn(anyFirstStay.CheckOut))
                                            .Generate();

        // Act
        bool conflict = anyFirst.ConflictsWith(anyNext);

        // Assert
        Assert.False(conflict);
    }

    [Fact]
    public void The_same_stay_in_two_different_rooms_does_not_conflict() {
        // Arrange
        StayPeriod  anyStay  = new AnyStayPeriod().Generate();
        Reservation anyFirst = Arbitrary.Reservation().WithRoom(room => room.WithNumber(new RoomNumber("101"))).WithStay(anyStay).Generate();
        Reservation anyOther = Arbitrary.Reservation().WithRoom(room => room.WithNumber(new RoomNumber("102"))).WithStay(anyStay).Generate();

        // Act
        bool conflict = anyFirst.ConflictsWith(anyOther);

        // Assert
        Assert.False(conflict);
    }

    [Fact]
    public void A_cancelled_reservation_frees_the_room() {
        // Arrange
        Room        anyRoom      = new AnyRoom().Generate();
        StayPeriod  anyStay      = new AnyStayPeriod().Generate();
        Reservation anyCancelled = Arbitrary.Reservation().WithRoom(anyRoom).WithStay(anyStay).Generate();
        Reservation anyRebooked  = Arbitrary.Reservation().WithRoom(anyRoom).WithStay(anyStay).Generate();
        anyCancelled.Cancel(anyStay.CheckIn.AddDays(-1));

        // Act
        bool conflict = anyRebooked.ConflictsWith(anyCancelled);

        // Assert
        Assert.False(conflict);
    }

}
