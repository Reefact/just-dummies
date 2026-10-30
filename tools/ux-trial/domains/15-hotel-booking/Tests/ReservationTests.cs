using Booking.Domain;
using JustDummies;
using JustDummies.Xunit;
using Xunit;

namespace Booking.Tests;

[Reproducible]
public sealed class ReservationTests {

    private static readonly IAny<DateOnly> AnyArrival = Any.DateOnly().Between(new DateOnly(2025, 1, 1), new DateOnly(2035, 12, 31));

    [Fact]
    public void The_total_is_the_nightly_rate_times_the_nights() {
        // Arrange
        Money       nightlyRate = new Money(120m, "EUR");
        Reservation anyThreeNightStay = Arbitrary.Reservation()
                                                 .WithRoom(room => room.WithNightlyRate(nightlyRate))
                                                 .WithStay(stay => stay.WithNights(3))
                                                 .Generate();

        // Act
        Money total = anyThreeNightStay.Total;

        // Assert
        Assert.Equal(new Money(360m, "EUR"), total);
    }

    [Fact]
    public void Cancelling_before_the_free_window_closes_costs_nothing() {
        // Arrange
        DateOnly    anyCheckIn  = AnyArrival.Generate();
        Reservation anyReservation = Arbitrary.Reservation()
                                              .WithStay(stay => stay.WithCheckIn(anyCheckIn))
                                              .WithPolicy(policy => policy.WithFreeCancellationDays(7))
                                              .Generate();

        // Act
        Money penalty = anyReservation.Cancel(anyCheckIn.AddDays(-10));

        // Assert
        Assert.Equal(0m, penalty.Amount);
    }

    [Fact]
    public void Cancelling_exactly_on_the_last_free_day_still_costs_nothing() {
        // Arrange
        DateOnly    anyCheckIn     = AnyArrival.Generate();
        Reservation anyReservation = Arbitrary.Reservation()
                                              .WithStay(stay => stay.WithCheckIn(anyCheckIn))
                                              .WithPolicy(policy => policy.WithFreeCancellationDays(7))
                                              .Generate();

        // Act
        Money penalty = anyReservation.Cancel(anyCheckIn.AddDays(-7));

        // Assert
        Assert.Equal(0m, penalty.Amount);
    }

    [Fact]
    public void Cancelling_late_keeps_the_penalty_share_of_the_total() {
        // Arrange
        DateOnly    anyCheckIn  = AnyArrival.Generate();
        Reservation anyLateCancellable = Arbitrary.Reservation()
                                                  .WithRoom(room => room.WithNightlyRate(new Money(100m, "EUR")))
                                                  .WithStay(stay => stay.WithCheckIn(anyCheckIn).WithNights(2))
                                                  .WithPolicy(new CancellationPolicy(freeCancellationDays: 3, penaltyRate: 0.5m))
                                                  .Generate();

        // Act
        Money penalty = anyLateCancellable.Cancel(anyCheckIn.AddDays(-1));

        // Assert
        Assert.Equal(new Money(100m, "EUR"), penalty);
        Assert.Equal(ReservationStatus.Cancelled, anyLateCancellable.Status);
    }

    [Fact]
    public void A_guest_checks_in_on_the_arrival_day() {
        // Arrange
        DateOnly    anyCheckIn     = AnyArrival.Generate();
        Reservation anyReservation = Arbitrary.Reservation().WithStay(stay => stay.WithCheckIn(anyCheckIn)).Generate();

        // Act
        anyReservation.CheckIn(anyCheckIn);

        // Assert
        Assert.Equal(ReservationStatus.CheckedIn, anyReservation.Status);
    }

    [Fact]
    public void A_guest_cannot_check_in_before_the_arrival_day() {
        // Arrange
        DateOnly    anyCheckIn     = AnyArrival.Generate();
        Reservation anyReservation = Arbitrary.Reservation().WithStay(stay => stay.WithCheckIn(anyCheckIn)).Generate();

        // Act
        Action checkInTheDayBefore = () => anyReservation.CheckIn(anyCheckIn.AddDays(-1));

        // Assert
        Assert.Throws<InvalidOperationException>(checkInTheDayBefore);
        Assert.Equal(ReservationStatus.Confirmed, anyReservation.Status);
    }

    [Fact]
    public void A_checked_in_guest_checks_out() {
        // Arrange
        DateOnly    anyCheckIn     = AnyArrival.Generate();
        Reservation anyReservation = Arbitrary.Reservation().WithStay(stay => stay.WithCheckIn(anyCheckIn)).Generate();
        anyReservation.CheckIn(anyCheckIn);

        // Act
        anyReservation.CheckOut();

        // Assert
        Assert.Equal(ReservationStatus.CheckedOut, anyReservation.Status);
    }

    [Fact]
    public void A_checked_in_reservation_can_no_longer_be_cancelled() {
        // Arrange
        DateOnly    anyCheckIn     = AnyArrival.Generate();
        Reservation anyReservation = Arbitrary.Reservation().WithStay(stay => stay.WithCheckIn(anyCheckIn)).Generate();
        anyReservation.CheckIn(anyCheckIn);

        // Act
        Action cancel = () => anyReservation.Cancel(anyCheckIn);

        // Assert
        Assert.Throws<InvalidOperationException>(cancel);
    }

    [Fact]
    public void A_single_room_refuses_a_second_guest() {
        // Arrange
        Room             anySingleRoom = new AnyRoom().WithType(RoomType.Single).Generate();
        List<GuestName>  anyTwoGuests  = Any.ListOf(new AnyGuestName()).WithCount(2).Generate();

        // Act
        Action book = () => Arbitrary.Reservation().WithRoom(anySingleRoom).WithGuests(anyTwoGuests).Generate();

        // Assert
        Assert.Throws<ArgumentException>(book);
    }

}
