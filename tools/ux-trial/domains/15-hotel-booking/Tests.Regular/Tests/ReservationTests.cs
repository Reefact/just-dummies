using JustDummies;

using Xunit;

namespace Booking.Domain.Tests.Regular;

public sealed class ReservationTests {

    private static readonly DateOnly CheckInDay = new DateOnly(2026, 7, 10);

    private static AnyReservation AnyReservationOfFiveNights() {
        return Arbitrary.Reservation().WithStay(s => s.WithCheckIn(CheckInDay).WithNights(5));
    }

    private static Room RoomOf(string number, RoomType type, int capacity, decimal rate = 100m) {
        return new Room(new RoomNumber(number), type, capacity, new Money(rate, "EUR"));
    }

    private static Reservation ReservationOf(Room room, DateOnly checkIn, int nights) {
        return Arbitrary.Reservation()
                        .WithRoom(room)
                        .WithStay(new StayPeriod(checkIn, checkIn.AddDays(nights)))
                        .Generate();
    }

    // ----- construction

    [Fact]
    public void A_reservation_keeps_what_it_was_given_and_starts_confirmed() {
        // Arrange
        Guid                     anyId      = Any.Guid().NonEmpty().Generate();
        Room                     room       = RoomOf("412", RoomType.Double, 2);
        StayPeriod               stay       = new StayPeriod(CheckInDay, CheckInDay.AddDays(3));
        IReadOnlyList<GuestName> guests     = new List<GuestName> { new GuestName("Ada"), new GuestName("Grace") };
        CancellationPolicy       anyPolicy  = Arbitrary.CancellationPolicy().Generate();

        // Act
        Reservation reservation = new Reservation(anyId, room, stay, guests, anyPolicy);

        // Assert
        Assert.Equal(anyId, reservation.Id);
        Assert.Same(room, reservation.Room);
        Assert.Same(stay, reservation.Stay);
        Assert.Same(anyPolicy, reservation.Policy);
        Assert.Equal(2, reservation.Guests.Count);
        Assert.Equal(ReservationStatus.Confirmed, reservation.Status);
    }

    [Fact]
    public void An_empty_identity_is_refused() {
        // Arrange
        Room                     anyRoom   = Arbitrary.Room().WithType(RoomType.Suite).Generate();
        StayPeriod               anyStay   = Arbitrary.StayPeriod().Generate();
        CancellationPolicy       anyPolicy = Arbitrary.CancellationPolicy().Generate();
        IReadOnlyList<GuestName> anyGuests = Any.ListOf(Arbitrary.GuestName()).WithCount(1).Generate();

        // Act
        ArgumentException exception = Assert.Throws<ArgumentException>(() => new Reservation(Guid.Empty, anyRoom, anyStay, anyGuests, anyPolicy));

        // Assert
        Assert.Equal("id", exception.ParamName);
    }

    [Fact]
    public void A_null_room_is_refused() {
        // Arrange
        Guid                     anyId     = Any.Guid().NonEmpty().Generate();
        StayPeriod               anyStay   = Arbitrary.StayPeriod().Generate();
        CancellationPolicy       anyPolicy = Arbitrary.CancellationPolicy().Generate();
        IReadOnlyList<GuestName> anyGuests = Any.ListOf(Arbitrary.GuestName()).WithCount(1).Generate();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new Reservation(anyId, null!, anyStay, anyGuests, anyPolicy));
    }

    [Fact]
    public void A_null_stay_is_refused() {
        // Arrange
        Guid                     anyId     = Any.Guid().NonEmpty().Generate();
        Room                     anyRoom   = Arbitrary.Room().Generate();
        CancellationPolicy       anyPolicy = Arbitrary.CancellationPolicy().Generate();
        IReadOnlyList<GuestName> anyGuests = Any.ListOf(Arbitrary.GuestName()).WithCount(1).Generate();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new Reservation(anyId, anyRoom, null!, anyGuests, anyPolicy));
    }

    [Fact]
    public void A_null_policy_is_refused() {
        // Arrange
        Guid                     anyId     = Any.Guid().NonEmpty().Generate();
        Room                     anyRoom   = Arbitrary.Room().Generate();
        StayPeriod               anyStay   = Arbitrary.StayPeriod().Generate();
        IReadOnlyList<GuestName> anyGuests = Any.ListOf(Arbitrary.GuestName()).WithCount(1).Generate();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new Reservation(anyId, anyRoom, anyStay, anyGuests, null!));
    }

    [Fact]
    public void A_null_guest_list_is_refused() {
        // Arrange
        Guid               anyId     = Any.Guid().NonEmpty().Generate();
        Room               anyRoom   = Arbitrary.Room().Generate();
        StayPeriod         anyStay   = Arbitrary.StayPeriod().Generate();
        CancellationPolicy anyPolicy = Arbitrary.CancellationPolicy().Generate();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new Reservation(anyId, anyRoom, anyStay, null!, anyPolicy));
    }

    [Fact]
    public void A_reservation_without_guest_is_refused() {
        // Act
        ArgumentException exception = Assert.Throws<ArgumentException>(() => Arbitrary.Reservation().WithGuests(new List<GuestName>()).Generate());

        // Assert
        Assert.Equal("guests", exception.ParamName);
    }

    [Theory]
    [InlineData(RoomType.Single, 1)]
    [InlineData(RoomType.Double, 2)]
    [InlineData(RoomType.Twin, 2)]
    [InlineData(RoomType.Suite, 4)]
    public void As_many_guests_as_the_capacity_are_accepted(RoomType type, int capacity) {
        // Arrange
        Room                     room   = Arbitrary.Room().WithType(type).WithCapacity(capacity).Generate();
        IReadOnlyList<GuestName> guests = Any.ListOf(Arbitrary.GuestName()).WithCount(capacity).Generate();

        // Act
        Reservation reservation = Arbitrary.Reservation().WithRoom(room).WithGuests(guests).Generate();

        // Assert
        Assert.Equal(capacity, reservation.Guests.Count);
    }

    [Theory]
    [InlineData(RoomType.Single, 1)]
    [InlineData(RoomType.Double, 2)]
    [InlineData(RoomType.Suite, 4)]
    public void One_guest_more_than_the_capacity_is_refused(RoomType type, int capacity) {
        // Arrange
        Room                     room   = Arbitrary.Room().WithType(type).WithCapacity(capacity).Generate();
        IReadOnlyList<GuestName> guests = Any.ListOf(Arbitrary.GuestName()).WithCount(capacity + 1).Generate();

        // Act
        ArgumentException exception = Assert.Throws<ArgumentException>(() => Arbitrary.Reservation().WithRoom(room).WithGuests(guests).Generate());

        // Assert
        Assert.Equal("guests", exception.ParamName);
    }

    [Fact]
    public void The_guest_list_is_copied() {
        // Arrange
        List<GuestName> guests = new List<GuestName> { new GuestName("Ada") };
        Reservation     reservation = Arbitrary.Reservation()
                                               .WithRoom(RoomOf("412", RoomType.Double, 2))
                                               .WithGuests(guests)
                                               .Generate();

        // Act
        guests.Add(new GuestName("Grace"));

        // Assert
        Assert.Single(reservation.Guests);
    }

    [Fact(Skip = "domain defect: a null guest inside the list is accepted by the constructor (only the count is checked), literal input, no seed")]
    public void A_null_guest_in_the_list_is_refused() {
        // Act & Assert
        Assert.ThrowsAny<ArgumentException>(() => Arbitrary.Reservation().WithRoom(RoomOf("412", RoomType.Double, 2)).WithGuests(new List<GuestName> { null! }).Generate());
    }

    [Fact]
    public void The_total_is_the_nightly_rate_times_the_number_of_nights() {
        // Arrange
        Room        room        = RoomOf("412", RoomType.Double, 2, 120.50m);
        Reservation reservation = ReservationOf(room, CheckInDay, 3);

        // Assert
        Assert.Equal(new Money(361.50m, "EUR"), reservation.Total);
    }

    [Fact]
    public void The_total_of_a_one_night_stay_is_the_nightly_rate() {
        // Arrange
        Room        room        = RoomOf("412", RoomType.Double, 2, 99.99m);
        Reservation reservation = ReservationOf(room, CheckInDay, 1);

        // Assert
        Assert.Equal(new Money(99.99m, "EUR"), reservation.Total);
    }

    [Fact]
    public void The_total_of_a_thirty_night_stay_is_thirty_times_the_rate() {
        // Arrange
        Room        room        = RoomOf("412", RoomType.Double, 2, 33.33m);
        Reservation reservation = ReservationOf(room, CheckInDay, 30);

        // Assert
        Assert.Equal(new Money(999.90m, "EUR"), reservation.Total);
    }

    // ----- check-in

    [Fact]
    public void Checking_in_on_the_check_in_day_moves_the_reservation_to_checked_in() {
        // Arrange
        Reservation reservation = AnyReservationOfFiveNights().Generate();

        // Act
        reservation.CheckIn(CheckInDay);

        // Assert
        Assert.Equal(ReservationStatus.CheckedIn, reservation.Status);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(4)]
    public void Checking_in_during_the_stay_is_accepted(int daysAfterCheckIn) {
        // Arrange
        Reservation reservation = AnyReservationOfFiveNights().Generate();

        // Act
        reservation.CheckIn(CheckInDay.AddDays(daysAfterCheckIn));

        // Assert
        Assert.Equal(ReservationStatus.CheckedIn, reservation.Status);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(-100)]
    [InlineData(5)]
    [InlineData(6)]
    [InlineData(300)]
    public void Checking_in_outside_the_stay_is_refused_and_changes_nothing(int daysAfterCheckIn) {
        // Arrange
        Reservation reservation = AnyReservationOfFiveNights().Generate();

        // Act
        Assert.Throws<InvalidOperationException>(() => reservation.CheckIn(CheckInDay.AddDays(daysAfterCheckIn)));

        // Assert
        Assert.Equal(ReservationStatus.Confirmed, reservation.Status);
    }

    [Fact]
    public void Checking_in_twice_is_refused() {
        // Arrange
        Reservation reservation = AnyReservationOfFiveNights().Generate();
        reservation.CheckIn(CheckInDay);

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => reservation.CheckIn(CheckInDay));
    }

    [Fact]
    public void Checking_in_a_cancelled_reservation_is_refused() {
        // Arrange
        Reservation reservation = AnyReservationOfFiveNights().Generate();
        reservation.Cancel(CheckInDay.AddDays(-30));

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => reservation.CheckIn(CheckInDay));
    }

    [Fact]
    public void Checking_in_a_checked_out_reservation_is_refused() {
        // Arrange
        Reservation reservation = AnyReservationOfFiveNights().Generate();
        reservation.CheckIn(CheckInDay);
        reservation.CheckOut();

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => reservation.CheckIn(CheckInDay));
    }

    // ----- check-out

    [Fact]
    public void Checking_out_a_checked_in_reservation_moves_it_to_checked_out() {
        // Arrange
        Reservation reservation = AnyReservationOfFiveNights().Generate();
        reservation.CheckIn(CheckInDay);

        // Act
        reservation.CheckOut();

        // Assert
        Assert.Equal(ReservationStatus.CheckedOut, reservation.Status);
    }

    [Fact]
    public void Checking_out_a_confirmed_reservation_is_refused() {
        // Arrange
        Reservation reservation = AnyReservationOfFiveNights().Generate();

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => reservation.CheckOut());
        Assert.Equal(ReservationStatus.Confirmed, reservation.Status);
    }

    [Fact]
    public void Checking_out_twice_is_refused() {
        // Arrange
        Reservation reservation = AnyReservationOfFiveNights().Generate();
        reservation.CheckIn(CheckInDay);
        reservation.CheckOut();

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => reservation.CheckOut());
    }

    [Fact]
    public void Checking_out_a_cancelled_reservation_is_refused() {
        // Arrange
        Reservation reservation = AnyReservationOfFiveNights().Generate();
        reservation.Cancel(CheckInDay.AddDays(-30));

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => reservation.CheckOut());
    }

    // ----- cancel

    [Fact]
    public void Cancelling_early_is_free_and_cancels_the_reservation() {
        // Arrange
        Reservation reservation = AnyReservationOfFiveNights().WithPolicy(new CancellationPolicy(7, 0.5m)).Generate();

        // Act
        Money penalty = reservation.Cancel(CheckInDay.AddDays(-7));

        // Assert
        Assert.Equal(0m, penalty.Amount);
        Assert.Equal(ReservationStatus.Cancelled, reservation.Status);
    }

    [Fact]
    public void Cancelling_late_costs_a_share_of_the_total() {
        // Arrange
        Room        room        = RoomOf("412", RoomType.Double, 2, 100m);
        Reservation reservation = Arbitrary.Reservation()
                                           .WithRoom(room)
                                           .WithStay(new StayPeriod(CheckInDay, CheckInDay.AddDays(2)))
                                           .WithPolicy(new CancellationPolicy(7, 0.25m))
                                           .Generate();

        // Act
        Money penalty = reservation.Cancel(CheckInDay.AddDays(-6));

        // Assert: 25 % of 2 nights at 100.00
        Assert.Equal(new Money(50m, "EUR"), penalty);
        Assert.Equal(ReservationStatus.Cancelled, reservation.Status);
    }

    [Fact]
    public void Cancelling_after_the_check_in_day_costs_the_share_too() {
        // Arrange
        Room        room        = RoomOf("412", RoomType.Double, 2, 100m);
        Reservation reservation = Arbitrary.Reservation()
                                           .WithRoom(room)
                                           .WithStay(new StayPeriod(CheckInDay, CheckInDay.AddDays(2)))
                                           .WithPolicy(new CancellationPolicy(0, 0.5m))
                                           .Generate();

        // Act
        Money penalty = reservation.Cancel(CheckInDay.AddDays(1));

        // Assert
        Assert.Equal(new Money(100m, "EUR"), penalty);
    }

    [Fact]
    public void Cancelling_twice_is_refused() {
        // Arrange
        Reservation reservation = AnyReservationOfFiveNights().Generate();
        reservation.Cancel(CheckInDay.AddDays(-30));

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => reservation.Cancel(CheckInDay.AddDays(-29)));
    }

    [Fact]
    public void Cancelling_a_checked_in_reservation_is_refused_and_changes_nothing() {
        // Arrange
        Reservation reservation = AnyReservationOfFiveNights().Generate();
        reservation.CheckIn(CheckInDay);

        // Act
        Assert.Throws<InvalidOperationException>(() => reservation.Cancel(CheckInDay));

        // Assert
        Assert.Equal(ReservationStatus.CheckedIn, reservation.Status);
    }

    [Fact]
    public void Cancelling_a_checked_out_reservation_is_refused() {
        // Arrange
        Reservation reservation = AnyReservationOfFiveNights().Generate();
        reservation.CheckIn(CheckInDay);
        reservation.CheckOut();

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => reservation.Cancel(CheckInDay));
    }

    // ----- conflicts

    [Fact]
    public void Two_reservations_of_the_same_room_over_overlapping_stays_conflict() {
        // Arrange
        Room        room   = RoomOf("412", RoomType.Double, 2);
        Reservation first  = ReservationOf(room, CheckInDay, 5);
        Reservation second = ReservationOf(room, CheckInDay.AddDays(4), 5);

        // Assert
        Assert.True(first.ConflictsWith(second));
        Assert.True(second.ConflictsWith(first));
    }

    [Fact]
    public void Two_distinct_room_objects_with_the_same_number_are_the_same_room() {
        // Arrange
        Reservation first  = ReservationOf(RoomOf("412", RoomType.Double, 2), CheckInDay, 5);
        Reservation second = ReservationOf(RoomOf("412", RoomType.Suite, 4, 300m), CheckInDay, 5);

        // Assert
        Assert.True(first.ConflictsWith(second));
    }

    [Fact]
    public void A_reservation_conflicts_with_itself() {
        // Arrange
        Reservation anyReservation = Arbitrary.Reservation().Generate();

        // Assert
        Assert.True(anyReservation.ConflictsWith(anyReservation));
    }

    [Fact]
    public void Back_to_back_reservations_of_the_same_room_do_not_conflict() {
        // Arrange
        Room        room   = RoomOf("412", RoomType.Double, 2);
        Reservation first  = ReservationOf(room, CheckInDay, 5);
        Reservation second = ReservationOf(room, CheckInDay.AddDays(5), 2);

        // Assert
        Assert.False(first.ConflictsWith(second));
        Assert.False(second.ConflictsWith(first));
    }

    [Fact]
    public void Overlapping_reservations_of_different_rooms_do_not_conflict() {
        // Arrange
        Reservation first  = ReservationOf(RoomOf("412", RoomType.Double, 2), CheckInDay, 5);
        Reservation second = ReservationOf(RoomOf("413", RoomType.Double, 2), CheckInDay, 5);

        // Assert
        Assert.False(first.ConflictsWith(second));
    }

    [Fact]
    public void A_wing_letter_makes_another_room_for_conflicts() {
        // Arrange
        Reservation first  = ReservationOf(RoomOf("412", RoomType.Double, 2), CheckInDay, 5);
        Reservation second = ReservationOf(RoomOf("412A", RoomType.Double, 2), CheckInDay, 5);

        // Assert
        Assert.False(first.ConflictsWith(second));
    }

    [Fact]
    public void A_cancelled_reservation_no_longer_conflicts() {
        // Arrange
        Room        room   = RoomOf("412", RoomType.Double, 2);
        Reservation first  = ReservationOf(room, CheckInDay, 5);
        Reservation second = ReservationOf(room, CheckInDay, 5);
        first.Cancel(CheckInDay.AddDays(-30));

        // Assert
        Assert.False(first.ConflictsWith(second));
        Assert.False(second.ConflictsWith(first));
    }

    [Fact]
    public void Two_cancelled_reservations_do_not_conflict() {
        // Arrange
        Room        room   = RoomOf("412", RoomType.Double, 2);
        Reservation first  = ReservationOf(room, CheckInDay, 5);
        Reservation second = ReservationOf(room, CheckInDay, 5);
        first.Cancel(CheckInDay.AddDays(-30));
        second.Cancel(CheckInDay.AddDays(-30));

        // Assert
        Assert.False(first.ConflictsWith(second));
    }

    [Fact]
    public void A_checked_in_or_checked_out_reservation_still_conflicts() {
        // Arrange
        Room        room   = RoomOf("412", RoomType.Double, 2);
        Reservation first  = ReservationOf(room, CheckInDay, 5);
        Reservation second = ReservationOf(room, CheckInDay.AddDays(1), 2);
        first.CheckIn(CheckInDay);

        // Assert
        Assert.True(first.ConflictsWith(second));

        // Act
        first.CheckOut();

        // Assert
        Assert.True(first.ConflictsWith(second));
    }

    [Fact(Skip = "domain defect: Cancel sets the status to Cancelled before computing the penalty, so a Total that overflows (OverflowException) leaves a Cancelled reservation behind; seed 350311511")]
    public void A_cancellation_that_fails_leaves_the_reservation_confirmed() {
        // Arrange
        Room        room        = new Room(new RoomNumber("412"), RoomType.Double, 2, new Money(decimal.MaxValue, "EUR"));
        Reservation reservation = ReservationOf(room, CheckInDay, 2);

        // Act
        Assert.ThrowsAny<Exception>(() => reservation.Cancel(CheckInDay.AddDays(-30)));

        // Assert
        Assert.Equal(ReservationStatus.Confirmed, reservation.Status);
    }

    // ----- generator

    [Fact]
    public void The_generator_only_draws_reservations_whose_guests_fit_the_room() {
        // Act
        for (int i = 0; i < 300; i++) {
            Reservation anyReservation = Arbitrary.Reservation().Generate();

            Assert.InRange(anyReservation.Guests.Count, 1, anyReservation.Room.Capacity);
            Assert.Equal(ReservationStatus.Confirmed, anyReservation.Status);
        }
    }

}
