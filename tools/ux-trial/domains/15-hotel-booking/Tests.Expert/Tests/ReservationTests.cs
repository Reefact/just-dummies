using JustDummies;

using Xunit;

namespace Booking.Domain.Tests;

public sealed class ReservationTests {

    // --- Construction -----------------------------------------------------------------------------------------

    [Fact]
    public void A_new_reservation_is_confirmed() {
        // Act
        Reservation reservation = Any.Reservation().Generate();

        // Assert
        Assert.Equal(ReservationStatus.Confirmed, reservation.Status);
    }

    [Fact]
    public void A_reservation_keeps_its_identity_room_stay_and_policy() {
        // Arrange
        Guid               id     = Guid.Parse("6f0c8f58-3a58-4f0a-9d0f-0c2f2d6d2a11");
        Room               room   = Any.Room().Generate();
        StayPeriod         stay   = Any.StayPeriod().Generate();
        CancellationPolicy policy = Any.CancellationPolicy().Generate();

        // Act
        Reservation reservation = Any.Reservation().WithId(id).WithRoom(room).WithStay(stay).WithPolicy(policy).Generate();

        // Assert
        Assert.Equal(id, reservation.Id);
        Assert.Same(room, reservation.Room);
        Assert.Same(stay, reservation.Stay);
        Assert.Same(policy, reservation.Policy);
    }

    [Fact]
    public void A_reservation_keeps_its_guests_in_order() {
        // Arrange
        GuestName first  = new GuestName("Camille");
        GuestName second = new GuestName("Noor");

        // Act
        Reservation reservation = Any.Reservation()
                                     .WithRoom(room => room.WithType(RoomType.Double).WithCapacity(2))
                                     .WithGuests([first, second])
                                     .Generate();

        // Assert
        Assert.Equal([first, second], reservation.Guests);
    }

    [Fact]
    public void A_reservation_without_identity_is_refused() {
        // Arrange
        AnyReservation reservations = Any.Reservation().WithId(Guid.Empty);

        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => reservations.Generate());

        // Assert
        Assert.Equal("id", refusal.ParamName);
    }

    [Fact]
    public void An_absent_room_is_refused_as_null() {
        // Arrange
        AnyReservation reservations = Any.Reservation().WithRoom((Room)null!);

        // Act
        ArgumentNullException refusal = Assert.Throws<ArgumentNullException>(() => reservations.Generate());

        // Assert
        Assert.Equal("room", refusal.ParamName);
    }

    [Fact]
    public void An_absent_stay_is_refused_as_null() {
        // Arrange
        AnyReservation reservations = Any.Reservation().WithStay((StayPeriod)null!);

        // Act
        ArgumentNullException refusal = Assert.Throws<ArgumentNullException>(() => reservations.Generate());

        // Assert
        Assert.Equal("stay", refusal.ParamName);
    }

    [Fact]
    public void An_absent_policy_is_refused_as_null() {
        // Arrange
        AnyReservation reservations = Any.Reservation().WithPolicy((CancellationPolicy)null!);

        // Act
        ArgumentNullException refusal = Assert.Throws<ArgumentNullException>(() => reservations.Generate());

        // Assert
        Assert.Equal("policy", refusal.ParamName);
    }

    [Fact]
    public void An_absent_guest_list_is_refused_as_null() {
        // Arrange
        AnyReservation reservations = Any.Reservation().WithGuests((IReadOnlyList<GuestName>)null!);

        // Act
        ArgumentNullException refusal = Assert.Throws<ArgumentNullException>(() => reservations.Generate());

        // Assert
        Assert.Equal("guests", refusal.ParamName);
    }

    [Fact]
    public void A_reservation_needs_at_least_one_guest() {
        // Arrange
        AnyReservation reservations = Any.Reservation().WithGuests(new List<GuestName>());

        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => reservations.Generate());

        // Assert
        Assert.Equal("guests", refusal.ParamName);
    }

    [Fact]
    public void A_reservation_cannot_host_more_guests_than_the_room_sleeps() {
        // Arrange
        List<GuestName> threeGuests  = Any.ListOf(new AnyGuestName()).WithCount(3).Generate();
        AnyReservation  reservations = Any.Reservation()
                                          .WithRoom(room => room.WithType(RoomType.Double).WithCapacity(2))
                                          .WithGuests(threeGuests);

        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => reservations.Generate());

        // Assert
        Assert.Equal("guests", refusal.ParamName);
    }

    [Fact]
    public void A_reservation_can_host_as_many_guests_as_the_room_sleeps() {
        // Arrange
        List<GuestName> fourGuests = Any.ListOf(new AnyGuestName()).WithCount(4).Generate();

        // Act
        Reservation reservation = Any.Reservation()
                                     .WithRoom(room => room.WithType(RoomType.Suite).WithCapacity(4))
                                     .WithGuests(fourGuests)
                                     .Generate();

        // Assert
        Assert.Equal(4, reservation.Guests.Count);
    }

    [Fact]
    public void A_single_guest_fits_in_a_room_that_sleeps_four() {
        // Arrange
        List<GuestName> oneGuest = Any.ListOf(new AnyGuestName()).WithCount(1).Generate();

        // Act
        Reservation reservation = Any.Reservation()
                                     .WithRoom(room => room.WithType(RoomType.Suite).WithCapacity(4))
                                     .WithGuests(oneGuest)
                                     .Generate();

        // Assert
        Assert.Single(reservation.Guests);
    }

    [Fact]
    public void The_guest_list_is_copied() {
        // Arrange
        List<GuestName> guests = Any.ListOf(new AnyGuestName()).WithCount(2).Generate();
        Reservation reservation = Any.Reservation()
                                     .WithRoom(room => room.WithType(RoomType.Double).WithCapacity(2))
                                     .WithGuests(guests)
                                     .Generate();

        // Act
        guests.Clear();

        // Assert
        Assert.Equal(2, reservation.Guests.Count);
    }

    // --- Total ------------------------------------------------------------------------------------------------

    [Fact]
    public void The_total_is_the_nightly_rate_times_the_nights() {
        // Arrange
        Reservation reservation = Any.Reservation()
                                     .WithRoom(room => room.WithNightlyRate(new Money(80.50m, "EUR")))
                                     .WithStay(stay => stay.WithNights(3))
                                     .Generate();

        // Act
        Money total = reservation.Total;

        // Assert
        Assert.Equal(new Money(241.50m, "EUR"), total);
    }

    [Fact]
    public void The_total_of_a_one_night_stay_is_the_nightly_rate() {
        // Arrange
        Reservation reservation = Any.Reservation()
                                     .WithRoom(room => room.WithNightlyRate(new Money(80.50m, "EUR")))
                                     .WithStay(stay => stay.WithNights(1))
                                     .Generate();

        // Act
        Money total = reservation.Total;

        // Assert
        Assert.Equal(new Money(80.50m, "EUR"), total);
    }

    // --- Check-in ---------------------------------------------------------------------------------------------

    [Theory]
    [InlineData(0)]   // the day of arrival
    [InlineData(1)]
    [InlineData(2)]   // the last night
    public void A_confirmed_reservation_checks_in_during_its_stay(int dayOfStay) {
        // Arrange
        Reservation reservation = Any.Reservation().WithStay(stay => stay.WithNights(3)).Generate();

        // Act
        reservation.CheckIn(reservation.Stay.CheckIn.AddDays(dayOfStay));

        // Assert
        Assert.Equal(ReservationStatus.CheckedIn, reservation.Status);
    }

    [Theory]
    [InlineData(-1)]  // the day before arrival
    [InlineData(-30)]
    [InlineData(3)]   // the day of departure
    [InlineData(4)]
    public void A_confirmed_reservation_does_not_check_in_outside_its_stay(int dayOfStay) {
        // Arrange
        Reservation reservation = Any.Reservation().WithStay(stay => stay.WithNights(3)).Generate();

        // Act / Assert
        Assert.Throws<InvalidOperationException>(() => reservation.CheckIn(reservation.Stay.CheckIn.AddDays(dayOfStay)));
        Assert.Equal(ReservationStatus.Confirmed, reservation.Status);
    }

    [Fact]
    public void A_reservation_already_checked_in_does_not_check_in_again() {
        // Arrange
        Reservation reservation = CheckedIn(Any.Reservation().Generate());

        // Act / Assert
        Assert.Throws<InvalidOperationException>(() => reservation.CheckIn(reservation.Stay.CheckIn));
        Assert.Equal(ReservationStatus.CheckedIn, reservation.Status);
    }

    [Fact]
    public void A_checked_out_reservation_does_not_check_in() {
        // Arrange
        Reservation reservation = CheckedOut(Any.Reservation().Generate());

        // Act / Assert
        Assert.Throws<InvalidOperationException>(() => reservation.CheckIn(reservation.Stay.CheckIn));
        Assert.Equal(ReservationStatus.CheckedOut, reservation.Status);
    }

    [Fact]
    public void A_cancelled_reservation_does_not_check_in() {
        // Arrange
        Reservation reservation = Cancelled(Any.Reservation().Generate());

        // Act / Assert
        Assert.Throws<InvalidOperationException>(() => reservation.CheckIn(reservation.Stay.CheckIn));
        Assert.Equal(ReservationStatus.Cancelled, reservation.Status);
    }

    // --- Check-out --------------------------------------------------------------------------------------------

    [Fact]
    public void A_checked_in_reservation_checks_out() {
        // Arrange
        Reservation reservation = CheckedIn(Any.Reservation().Generate());

        // Act
        reservation.CheckOut();

        // Assert
        Assert.Equal(ReservationStatus.CheckedOut, reservation.Status);
    }

    [Fact]
    public void A_confirmed_reservation_does_not_check_out() {
        // Arrange
        Reservation reservation = Any.Reservation().Generate();

        // Act / Assert
        Assert.Throws<InvalidOperationException>(() => reservation.CheckOut());
        Assert.Equal(ReservationStatus.Confirmed, reservation.Status);
    }

    [Fact]
    public void A_checked_out_reservation_does_not_check_out_again() {
        // Arrange
        Reservation reservation = CheckedOut(Any.Reservation().Generate());

        // Act / Assert
        Assert.Throws<InvalidOperationException>(() => reservation.CheckOut());
        Assert.Equal(ReservationStatus.CheckedOut, reservation.Status);
    }

    [Fact]
    public void A_cancelled_reservation_does_not_check_out() {
        // Arrange
        Reservation reservation = Cancelled(Any.Reservation().Generate());

        // Act / Assert
        Assert.Throws<InvalidOperationException>(() => reservation.CheckOut());
        Assert.Equal(ReservationStatus.Cancelled, reservation.Status);
    }

    // --- Cancellation -----------------------------------------------------------------------------------------

    [Fact]
    public void Cancelling_a_confirmed_reservation_cancels_it() {
        // Arrange
        Reservation reservation = Any.Reservation().Generate();

        // Act
        reservation.Cancel(reservation.Stay.CheckIn.AddDays(-1));

        // Assert
        Assert.Equal(ReservationStatus.Cancelled, reservation.Status);
    }

    [Fact]
    public void Cancelling_early_enough_costs_nothing() {
        // Arrange
        Reservation reservation = AHundredEurosANightWithSevenFreeDaysAndAHalfPenalty(nights: 3);

        // Act
        Money penalty = reservation.Cancel(reservation.Stay.CheckIn.AddDays(-7));

        // Assert
        Assert.Equal(new Money(0m, "EUR"), penalty);
    }

    [Fact]
    public void Cancelling_late_keeps_the_penalty_share_of_the_whole_stay() {
        // Arrange
        Reservation reservation = AHundredEurosANightWithSevenFreeDaysAndAHalfPenalty(nights: 3);

        // Act
        Money penalty = reservation.Cancel(reservation.Stay.CheckIn.AddDays(-6));

        // Assert
        Assert.Equal(new Money(150m, "EUR"), penalty);
    }

    [Fact]
    public void A_cancellation_after_check_in_date_keeps_the_penalty_share() {
        // Arrange
        Reservation reservation = AHundredEurosANightWithSevenFreeDaysAndAHalfPenalty(nights: 2);

        // Act
        Money penalty = reservation.Cancel(reservation.Stay.CheckIn.AddDays(5));

        // Assert
        Assert.Equal(new Money(100m, "EUR"), penalty);
    }

    [Fact]
    public void A_checked_in_reservation_cannot_be_cancelled() {
        // Arrange
        Reservation reservation = CheckedIn(Any.Reservation().Generate());

        // Act / Assert
        Assert.Throws<InvalidOperationException>(() => reservation.Cancel(reservation.Stay.CheckIn));
        Assert.Equal(ReservationStatus.CheckedIn, reservation.Status);
    }

    [Fact]
    public void A_checked_out_reservation_cannot_be_cancelled() {
        // Arrange
        Reservation reservation = CheckedOut(Any.Reservation().Generate());

        // Act / Assert
        Assert.Throws<InvalidOperationException>(() => reservation.Cancel(reservation.Stay.CheckIn));
        Assert.Equal(ReservationStatus.CheckedOut, reservation.Status);
    }

    [Fact]
    public void A_reservation_cannot_be_cancelled_twice() {
        // Arrange
        Reservation reservation = Cancelled(Any.Reservation().Generate());

        // Act / Assert
        Assert.Throws<InvalidOperationException>(() => reservation.Cancel(reservation.Stay.CheckIn.AddDays(-30)));
        Assert.Equal(ReservationStatus.Cancelled, reservation.Status);
    }

    // --- Conflicts --------------------------------------------------------------------------------------------

    [Fact]
    public void Two_reservations_of_the_same_room_over_overlapping_stays_conflict() {
        // Arrange
        DateOnly    anyDay = AnyStayPeriod.ValidCheckIn().Generate();
        Reservation first  = ReservationOfRoom("412", anyDay, 0, 4);
        Reservation second = ReservationOfRoom("412", anyDay, 3, 6);

        // Act / Assert
        Assert.True(first.ConflictsWith(second));
        Assert.True(second.ConflictsWith(first));
    }

    [Fact]
    public void Two_reservations_of_different_rooms_over_the_same_stay_do_not_conflict() {
        // Arrange
        DateOnly    anyDay = AnyStayPeriod.ValidCheckIn().Generate();
        Reservation first  = ReservationOfRoom("412", anyDay, 0, 4);
        Reservation second = ReservationOfRoom("413", anyDay, 0, 4);

        // Act / Assert
        Assert.False(first.ConflictsWith(second));
    }

    [Fact]
    public void The_wing_makes_a_different_room() {
        // Arrange
        DateOnly    anyDay = AnyStayPeriod.ValidCheckIn().Generate();
        Reservation first  = ReservationOfRoom("412A", anyDay, 0, 4);
        Reservation second = ReservationOfRoom("412B", anyDay, 0, 4);

        // Act / Assert
        Assert.False(first.ConflictsWith(second));
    }

    [Fact]
    public void A_stay_starting_the_day_the_other_ends_does_not_conflict() {
        // Arrange
        DateOnly    anyDay = AnyStayPeriod.ValidCheckIn().Generate();
        Reservation first  = ReservationOfRoom("412", anyDay, 0, 4);
        Reservation second = ReservationOfRoom("412", anyDay, 4, 7);

        // Act / Assert
        Assert.False(first.ConflictsWith(second));
        Assert.False(second.ConflictsWith(first));
    }

    [Fact]
    public void A_cancelled_reservation_conflicts_with_nothing() {
        // Arrange
        DateOnly    anyDay    = AnyStayPeriod.ValidCheckIn().Generate();
        Reservation cancelled = Cancelled(ReservationOfRoom("412", anyDay, 0, 4));
        Reservation other     = ReservationOfRoom("412", anyDay, 0, 4);

        // Act / Assert
        Assert.False(cancelled.ConflictsWith(other));
        Assert.False(other.ConflictsWith(cancelled));
    }

    [Fact]
    public void A_reservation_cancelled_afterwards_stops_conflicting() {
        // Arrange
        DateOnly    anyDay = AnyStayPeriod.ValidCheckIn().Generate();
        Reservation first  = ReservationOfRoom("412", anyDay, 0, 4);
        Reservation second = ReservationOfRoom("412", anyDay, 0, 4);
        Assert.True(first.ConflictsWith(second));

        // Act
        second.Cancel(anyDay.AddDays(-30));

        // Assert
        Assert.False(first.ConflictsWith(second));
    }

    [Fact]
    public void A_reservation_in_progress_still_conflicts() {
        // Arrange
        DateOnly    anyDay    = AnyStayPeriod.ValidCheckIn().Generate();
        Reservation inProgress = CheckedIn(ReservationOfRoom("412", anyDay, 0, 4));
        Reservation other     = ReservationOfRoom("412", anyDay, 2, 5);

        // Act / Assert
        Assert.True(inProgress.ConflictsWith(other));
    }

    [Fact]
    public void A_completed_reservation_still_conflicts() {
        // Arrange
        DateOnly    anyDay    = AnyStayPeriod.ValidCheckIn().Generate();
        Reservation completed = CheckedOut(ReservationOfRoom("412", anyDay, 0, 4));
        Reservation other     = ReservationOfRoom("412", anyDay, 2, 5);

        // Act / Assert
        Assert.True(completed.ConflictsWith(other));
    }

    // --- Helpers ----------------------------------------------------------------------------------------------

    private static Reservation ReservationOfRoom(string roomNumber, DateOnly anyDay, int fromDay, int toDay) {
        return Any.Reservation()
                  .WithRoom(room => room.WithNumber(new RoomNumber(roomNumber)))
                  .WithStay(new StayPeriod(anyDay.AddDays(fromDay), anyDay.AddDays(toDay)))
                  .Generate();
    }

    private static Reservation AHundredEurosANightWithSevenFreeDaysAndAHalfPenalty(int nights) {
        return Any.Reservation()
                  .WithRoom(room => room.WithNightlyRate(new Money(100m, "EUR")))
                  .WithStay(stay => stay.WithNights(nights))
                  .WithPolicy(new CancellationPolicy(7, 0.5m))
                  .Generate();
    }

    private static Reservation CheckedIn(Reservation reservation) {
        reservation.CheckIn(reservation.Stay.CheckIn);

        return reservation;
    }

    private static Reservation CheckedOut(Reservation reservation) {
        CheckedIn(reservation);
        reservation.CheckOut();

        return reservation;
    }

    private static Reservation Cancelled(Reservation reservation) {
        reservation.Cancel(reservation.Stay.CheckIn.AddDays(-1));

        return reservation;
    }

}
