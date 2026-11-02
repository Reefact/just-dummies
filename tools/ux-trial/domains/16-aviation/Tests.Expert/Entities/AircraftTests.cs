using Aviation.Domain.Tests.Expert.Generators;

using JustDummies;

namespace Aviation.Domain.Tests.Expert.Entities;

public sealed class AircraftTests {

    [Theory]
    [InlineData("A320", 186)]
    [InlineData("A321", 230)]
    [InlineData("B738", 189)]
    [InlineData("B77W", 396)]
    [InlineData("E190", 114)]
    [InlineData("AT76", 78)]
    public void An_aircraft_may_carry_as_many_seats_as_its_type_allows(string type, int maximumSeats) {
        // Act
        Aircraft aircraft = Any.Aircraft().WithTypeDesignator(type).WithSeats(maximumSeats).Generate();

        // Assert
        Assert.Equal(type, aircraft.TypeDesignator);
        Assert.Equal(maximumSeats, aircraft.Seats);
    }

    [Theory]
    [InlineData("A320", 187)]
    [InlineData("A321", 231)]
    [InlineData("B738", 190)]
    [InlineData("B77W", 397)]
    [InlineData("E190", 115)]
    [InlineData("AT76", 79)]
    public void An_aircraft_may_not_carry_one_seat_more_than_its_type_allows(string type, int seatsOverTheMaximum) {
        // Act
        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(
            () => Any.Aircraft().WithTypeDesignator(type).WithSeats(seatsOverTheMaximum).Generate());

        // Assert
        Assert.Equal("seats", refusal.ParamName);
    }

    [Theory]
    [InlineData("A320")]
    [InlineData("A321")]
    [InlineData("B738")]
    [InlineData("B77W")]
    [InlineData("E190")]
    [InlineData("AT76")]
    public void An_aircraft_has_at_least_one_seat_whatever_its_type(string type) {
        // Act
        Aircraft aircraft = Any.Aircraft().WithTypeDesignator(type).WithSeats(1).Generate();

        // Assert
        Assert.Equal(1, aircraft.Seats);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(int.MinValue)]
    public void An_aircraft_without_a_positive_number_of_seats_is_refused(int seats) {
        Assert.Throws<ArgumentOutOfRangeException>(() => Any.Aircraft().WithSeats(seats).Generate());
    }

    [Theory]
    [InlineData("A319")]
    [InlineData("a320")]
    [InlineData("A320 ")]
    [InlineData(" A320")]
    [InlineData("B77w")]
    [InlineData("")]
    [InlineData("Airbus A320")]
    public void A_type_outside_the_fleet_reference_list_is_refused(string type) {
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => Any.Aircraft().WithTypeDesignator(type).Generate());

        Assert.Equal("typeDesignator", refusal.ParamName);
    }

    [Fact]
    public void A_missing_type_is_refused_as_missing() {
        Assert.Throws<ArgumentNullException>(() => Any.Aircraft().WithTypeDesignator((string)null!).Generate());
    }

    [Fact]
    public void An_aircraft_without_a_registration_is_refused() {
        // Arrange
        Aircraft anyAircraft = Any.Aircraft().Generate();

        // Assert
        Assert.Throws<ArgumentNullException>(() => new Aircraft(null!, anyAircraft.TypeDesignator, anyAircraft.Seats));
    }

    [Fact]
    public void An_unknown_type_is_refused_before_its_seats_are_looked_at() {
        // Both are wrong: the type is checked first, so it is the type that is reported.
        Assert.Throws<ArgumentException>(() => Any.Aircraft().WithTypeDesignator("A319").WithSeats(0).Generate());
    }

    [Fact]
    public void An_aircraft_keeps_the_registration_it_was_given() {
        // Arrange
        AircraftRegistration registration = new AircraftRegistration("F-GKXA");

        // Act
        Aircraft aircraft = Any.Aircraft().WithRegistration(registration).Generate();

        // Assert
        Assert.Same(registration, aircraft.Registration);
    }

    [Fact]
    public void Only_the_B77W_is_a_wide_body() {
        // Arrange
        Aircraft anyWideBody = Any.Aircraft().WithTypeDesignator("B77W").Generate();

        // Assert
        Assert.True(anyWideBody.IsWideBody);
    }

    [Theory]
    [InlineData("A320")]
    [InlineData("A321")]
    [InlineData("B738")]
    [InlineData("E190")]
    [InlineData("AT76")]
    public void Every_other_type_is_a_narrow_body(string type) {
        // Arrange
        Aircraft anyNarrowBody = Any.Aircraft().WithTypeDesignator(type).Generate();

        // Assert
        Assert.False(anyNarrowBody.IsWideBody);
    }

    [Fact]
    public void The_fleet_reference_list_holds_the_six_types_of_the_fleet() {
        Assert.Equivalent(new[] { "A320", "A321", "B738", "B77W", "E190", "AT76" }, Aircraft.KnownTypes);
    }

}
