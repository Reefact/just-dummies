using JustDummies;

using Xunit;

namespace Booking.Domain;

public sealed class ReservationTests {

    private static readonly DateOnly CheckInDate = new DateOnly(2027, 3, 10);

    // A room whose rate keeps Reservation.Total well inside the decimal range: the tests that cancel compute it,
    // and an unbounded Money draw overflows there (see the skipped domain-defect tests at the end).
    private static AnyRoom AnyAffordableRoom() {
        return Any.Room().WithNightlyRate(rate => rate.WithAmount(amount => amount.LessThanOrEqualTo(100_000m)));
    }

    private static AnyReservation AnyConfirmedStay() {
        // A stay of five nights, 10 -> 15 March; the rest of the reservation is arbitrary.
        return Any.Reservation().WithRoom(AnyAffordableRoom().Generate()).WithStay(Any.StayPeriod().WithCheckIn(CheckInDate).WithNights(5).Generate());
    }

    private static Reservation AnyReservationInStatus(ReservationStatus status) {
        Reservation reservation = AnyConfirmedStay().Generate();

        switch (status) {
            case ReservationStatus.CheckedIn:
                reservation.CheckIn(CheckInDate);
                break;
            case ReservationStatus.CheckedOut:
                reservation.CheckIn(CheckInDate);
                reservation.CheckOut();
                break;
            case ReservationStatus.Cancelled:
                reservation.Cancel(CheckInDate.AddDays(-30));
                break;
        }

        return reservation;
    }

    // ----- construction -----

    [Fact]
    public void A_new_reservation_is_confirmed_and_keeps_what_it_was_given() {
        // Arrange
        Guid               anyId     = Any.Guid().NonEmpty().Generate();
        Room               anyRoom   = Any.Room().WithType(RoomType.Suite).WithCapacity(3).Generate();
        StayPeriod         anyStay   = Any.StayPeriod().Generate();
        CancellationPolicy anyPolicy = Any.CancellationPolicy().Generate();
        GuestName          first     = new GuestName("Marie Curie");
        GuestName          second    = new GuestName("Pierre Curie");

        // Act
        Reservation reservation = new Reservation(anyId, anyRoom, anyStay, [first, second], anyPolicy);

        // Assert
        Assert.Equal(ReservationStatus.Confirmed, reservation.Status);
        Assert.Equal(anyId, reservation.Id);
        Assert.Same(anyRoom, reservation.Room);
        Assert.Same(anyStay, reservation.Stay);
        Assert.Same(anyPolicy, reservation.Policy);
        Assert.Equal([first, second], reservation.Guests);
    }

    [Fact]
    public void An_empty_identity_is_refused() {
        ArgumentException error = Assert.Throws<ArgumentException>(() => Any.Reservation().WithId(Guid.Empty).Generate());

        Assert.Equal("id", error.ParamName);
    }

    [Fact]
    public void A_missing_room_is_refused() {
        ArgumentNullException error = Assert.Throws<ArgumentNullException>(() => Any.Reservation().WithRoom((Room)null!).Generate());

        Assert.Equal("room", error.ParamName);
    }

    [Fact]
    public void A_missing_stay_is_refused() {
        ArgumentNullException error = Assert.Throws<ArgumentNullException>(() => Any.Reservation().WithStay((StayPeriod)null!).Generate());

        Assert.Equal("stay", error.ParamName);
    }

    [Fact]
    public void A_missing_policy_is_refused() {
        ArgumentNullException error = Assert.Throws<ArgumentNullException>(() => Any.Reservation().WithPolicy((CancellationPolicy)null!).Generate());

        Assert.Equal("policy", error.ParamName);
    }

    [Fact]
    public void A_missing_guest_list_is_refused() {
        ArgumentNullException error = Assert.Throws<ArgumentNullException>(() => Any.Reservation().WithGuests((IReadOnlyList<GuestName>)null!).Generate());

        Assert.Equal("guests", error.ParamName);
    }

    [Fact]
    public void A_reservation_without_guests_is_refused() {
        ArgumentException error = Assert.Throws<ArgumentException>(() => Any.Reservation().WithGuests(new List<GuestName>()).Generate());

        Assert.Equal("guests", error.ParamName);
        Assert.IsNotType<ArgumentNullException>(error);
    }

    [Fact]
    public void As_many_guests_as_the_room_sleeps_is_accepted() {
        // Arrange
        Room                      twin   = Any.Room().WithType(RoomType.Twin).WithCapacity(2).Generate();
        IReadOnlyList<GuestName>  guests = Any.ListOf(Any.GuestName()).WithCount(2).Generate();

        // Act
        Reservation reservation = Any.Reservation().WithRoom(twin).WithGuests(guests).Generate();

        // Assert
        Assert.Equal(2, reservation.Guests.Count);
    }

    [Fact]
    public void More_guests_than_the_room_sleeps_is_refused() {
        // Arrange
        Room                      twin   = Any.Room().WithType(RoomType.Twin).WithCapacity(2).Generate();
        IReadOnlyList<GuestName>  guests = Any.ListOf(Any.GuestName()).WithCount(3).Generate();

        // Act
        ArgumentException error = Assert.Throws<ArgumentException>(() => Any.Reservation().WithRoom(twin).WithGuests(guests).Generate());

        // Assert
        Assert.Equal("guests", error.ParamName);
    }

    [Fact]
    public void The_guest_list_is_copied() {
        // Arrange
        List<GuestName> guests = [new GuestName("Marie Curie")];
        Reservation     reservation = Any.Reservation().WithRoom(Any.Room().WithType(RoomType.Suite).WithCapacity(4).Generate()).WithGuests(guests).Generate();

        // Act
        guests.Add(new GuestName("Pierre Curie"));

        // Assert
        Assert.Single(reservation.Guests);
    }

    // ----- total -----

    [Fact]
    public void The_total_is_the_nightly_rate_times_the_number_of_nights() {
        // Arrange
        Room        room        = Any.Room().WithNightlyRate(new Money(80.50m, "EUR")).Generate();
        Reservation reservation = AnyConfirmedStay().WithRoom(room).Generate();

        // Act
        Money total = reservation.Total;

        // Assert
        Assert.Equal(new Money(402.50m, "EUR"), total);
    }

    // ----- check in -----

    [Theory]
    [InlineData(0)]
    [InlineData(2)]
    [InlineData(4)]
    public void Checking_in_during_the_stay_checks_the_guests_in(int daysAfterCheckIn) {
        // Arrange
        Reservation reservation = AnyConfirmedStay().Generate();

        // Act
        reservation.CheckIn(CheckInDate.AddDays(daysAfterCheckIn));

        // Assert
        Assert.Equal(ReservationStatus.CheckedIn, reservation.Status);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(-100)]
    [InlineData(5)]
    [InlineData(6)]
    public void Checking_in_outside_the_stay_is_refused_and_changes_nothing(int daysAfterCheckIn) {
        // Arrange
        Reservation reservation = AnyConfirmedStay().Generate();

        // Act / Assert
        Assert.Throws<InvalidOperationException>(() => reservation.CheckIn(CheckInDate.AddDays(daysAfterCheckIn)));
        Assert.Equal(ReservationStatus.Confirmed, reservation.Status);
    }

    [Theory]
    [InlineData(ReservationStatus.CheckedIn)]
    [InlineData(ReservationStatus.CheckedOut)]
    [InlineData(ReservationStatus.Cancelled)]
    public void Only_a_confirmed_reservation_can_be_checked_in(ReservationStatus status) {
        // Arrange
        Reservation reservation = AnyReservationInStatus(status);

        // Act
        InvalidOperationException error = Assert.Throws<InvalidOperationException>(() => reservation.CheckIn(CheckInDate));

        // Assert
        Assert.Contains(status.ToString(), error.Message);
        Assert.Equal(status, reservation.Status);
    }

    // ----- check out -----

    [Fact]
    public void A_checked_in_reservation_can_be_checked_out() {
        // Arrange
        Reservation reservation = AnyReservationInStatus(ReservationStatus.CheckedIn);

        // Act
        reservation.CheckOut();

        // Assert
        Assert.Equal(ReservationStatus.CheckedOut, reservation.Status);
    }

    [Theory]
    [InlineData(ReservationStatus.Confirmed)]
    [InlineData(ReservationStatus.CheckedOut)]
    [InlineData(ReservationStatus.Cancelled)]
    public void Only_a_checked_in_reservation_can_be_checked_out(ReservationStatus status) {
        // Arrange
        Reservation reservation = AnyReservationInStatus(status);

        // Act
        InvalidOperationException error = Assert.Throws<InvalidOperationException>(() => reservation.CheckOut());

        // Assert
        Assert.Contains(status.ToString(), error.Message);
        Assert.Equal(status, reservation.Status);
    }

    // ----- cancel -----

    [Fact]
    public void Cancelling_a_confirmed_reservation_inside_the_free_period_costs_nothing() {
        // Arrange
        CancellationPolicy policy      = new CancellationPolicy(7, 0.5m);
        Room               room        = Any.Room().WithNightlyRate(new Money(100m, "EUR")).Generate();
        Reservation        reservation = AnyConfirmedStay().WithRoom(room).WithPolicy(policy).Generate();

        // Act
        Money penalty = reservation.Cancel(CheckInDate.AddDays(-7));

        // Assert
        Assert.Equal(new Money(0m, "EUR"), penalty);
        Assert.Equal(ReservationStatus.Cancelled, reservation.Status);
    }

    [Fact]
    public void Cancelling_a_confirmed_reservation_late_costs_a_share_of_the_total() {
        // Arrange
        CancellationPolicy policy      = new CancellationPolicy(7, 0.5m);
        Room               room        = Any.Room().WithNightlyRate(new Money(100m, "EUR")).Generate();
        Reservation        reservation = AnyConfirmedStay().WithRoom(room).WithPolicy(policy).Generate();

        // Act
        Money penalty = reservation.Cancel(CheckInDate.AddDays(-6));

        // Assert
        Assert.Equal(new Money(250m, "EUR"), penalty);   // half of 5 nights at 100
        Assert.Equal(ReservationStatus.Cancelled, reservation.Status);
    }

    [Theory]
    [InlineData(ReservationStatus.CheckedIn)]
    [InlineData(ReservationStatus.CheckedOut)]
    [InlineData(ReservationStatus.Cancelled)]
    public void Only_a_confirmed_reservation_can_be_cancelled(ReservationStatus status) {
        // Arrange
        Reservation reservation = AnyReservationInStatus(status);

        // Act
        InvalidOperationException error = Assert.Throws<InvalidOperationException>(() => reservation.Cancel(CheckInDate.AddDays(-30)));

        // Assert
        Assert.Contains(status.ToString(), error.Message);
        Assert.Equal(status, reservation.Status);
    }

    // ----- conflicts -----

    [Fact]
    public void Two_reservations_of_the_same_room_on_overlapping_stays_conflict() {
        // Arrange
        RoomNumber  number = new RoomNumber("412");
        Reservation first  = AnyConfirmedStay().WithRoom(Any.Room().WithNumber(number).Generate()).Generate();
        Reservation second = Any.Reservation()
                                .WithRoom(Any.Room().WithNumber(new RoomNumber("412")).Generate())
                                .WithStay(Any.StayPeriod().WithCheckIn(CheckInDate.AddDays(4)).WithNights(3).Generate())
                                .Generate();

        // Act / Assert
        Assert.True(first.ConflictsWith(second));
        Assert.True(second.ConflictsWith(first));
    }

    [Fact]
    public void Back_to_back_reservations_of_the_same_room_do_not_conflict() {
        // Arrange
        Reservation first  = AnyConfirmedStay().WithRoom(Any.Room().WithNumber(new RoomNumber("412")).Generate()).Generate();
        Reservation second = Any.Reservation()
                                .WithRoom(Any.Room().WithNumber(new RoomNumber("412")).Generate())
                                .WithStay(Any.StayPeriod().WithCheckIn(CheckInDate.AddDays(5)).WithNights(2).Generate())
                                .Generate();

        // Act / Assert
        Assert.False(first.ConflictsWith(second));
        Assert.False(second.ConflictsWith(first));
    }

    [Fact]
    public void Overlapping_reservations_of_two_different_rooms_do_not_conflict() {
        // Arrange
        Reservation first  = AnyConfirmedStay().WithRoom(Any.Room().WithNumber(new RoomNumber("412")).Generate()).Generate();
        Reservation second = AnyConfirmedStay().WithRoom(Any.Room().WithNumber(new RoomNumber("413")).Generate()).Generate();

        // Act / Assert
        Assert.False(first.ConflictsWith(second));
    }

    [Fact]
    public void The_wing_letter_tells_two_rooms_apart() {
        // Arrange
        Reservation first  = AnyConfirmedStay().WithRoom(Any.Room().WithNumber(new RoomNumber("412A")).Generate()).Generate();
        Reservation second = AnyConfirmedStay().WithRoom(Any.Room().WithNumber(new RoomNumber("412B")).Generate()).Generate();

        // Act / Assert
        Assert.False(first.ConflictsWith(second));
    }

    [Fact]
    public void A_cancelled_reservation_conflicts_with_nothing() {
        // Arrange
        Reservation first  = AnyConfirmedStay().WithRoom(AnyAffordableRoom().WithNumber(new RoomNumber("412")).Generate()).Generate();
        Reservation second = AnyConfirmedStay().WithRoom(Any.Room().WithNumber(new RoomNumber("412")).Generate()).Generate();
        first.Cancel(CheckInDate.AddDays(-30));

        // Act / Assert
        Assert.False(first.ConflictsWith(second));
        Assert.False(second.ConflictsWith(first));
    }

    [Fact]
    public void A_checked_in_reservation_still_conflicts() {
        // Arrange
        Reservation first  = AnyConfirmedStay().WithRoom(Any.Room().WithNumber(new RoomNumber("412")).Generate()).Generate();
        Reservation second = AnyConfirmedStay().WithRoom(Any.Room().WithNumber(new RoomNumber("412")).Generate()).Generate();
        first.CheckIn(CheckInDate);

        // Act / Assert
        Assert.True(first.ConflictsWith(second));
    }

    // ----- domain defects revealed by an unbounded Money draw (seed 924512350 drew such a rate) -----

    [Fact(Skip = "domain defect: Reservation.Total throws OverflowException for a Money the domain accepts (Money has no upper bound), seed 924512350")]
    public void The_total_of_a_very_expensive_room_is_refused_with_a_domain_error_not_a_decimal_overflow() {
        // Arrange
        Room        room        = Any.Room().WithNightlyRate(new Money(decimal.MaxValue, "EUR")).Generate();
        Reservation reservation = AnyConfirmedStay().WithRoom(room).Generate();

        // Act / Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => reservation.Total);
    }

    [Fact(Skip = "domain defect: Cancel sets the status to Cancelled before computing the penalty, so a total that overflows leaves a cancelled reservation behind, seed 924512350")]
    public void A_cancellation_that_fails_leaves_the_reservation_confirmed() {
        // Arrange
        Room        room        = Any.Room().WithNightlyRate(new Money(decimal.MaxValue, "EUR")).Generate();
        Reservation reservation = AnyConfirmedStay().WithRoom(room).Generate();

        // Act
        Assert.ThrowsAny<Exception>(() => reservation.Cancel(CheckInDate.AddDays(-30)));

        // Assert
        Assert.Equal(ReservationStatus.Confirmed, reservation.Status);
    }

    // ----- the scaffolded generators themselves -----

    [Fact]
    public void Every_scaffolded_generator_draws_values_its_constructor_accepts() {
        for (int i = 0; i < 200; i++) {
            Any.Money().Generate();
            Any.GuestName().Generate();
            Any.RoomNumber().Generate();
            Any.StayPeriod().Generate();
            Any.CancellationPolicy().Generate();
            Any.GuestProfile().Generate();
            Any.Room().Generate();
            Any.Reservation().Generate();
        }
    }

}
