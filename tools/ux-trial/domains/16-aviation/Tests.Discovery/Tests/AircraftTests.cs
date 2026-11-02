using Aviation.Domain;

using JustDummies;

using Xunit;

namespace Aviation.Tests.Discovery;

public sealed class AircraftTests {

    [Theory]
    [InlineData("A320", 186)]
    [InlineData("A321", 230)]
    [InlineData("B738", 189)]
    [InlineData("B77W", 396)]
    [InlineData("E190", 114)]
    [InlineData("AT76", 78)]
    public void An_aircraft_is_accepted_up_to_the_maximum_seats_of_its_type(string type, int maximumSeats) {
        // Arrange
        AircraftRegistration anyRegistration = Any.AircraftRegistration().Generate();

        // Act
        Aircraft full  = new Aircraft(anyRegistration, type, maximumSeats);
        Aircraft small = new Aircraft(anyRegistration, type, 1);

        // Assert
        Assert.Equal(maximumSeats, full.Seats);
        Assert.Equal(1, small.Seats);
        Assert.Equal(type, full.TypeDesignator);
        Assert.Same(anyRegistration, full.Registration);
    }

    [Theory]
    [InlineData("A320", 187)]
    [InlineData("A321", 231)]
    [InlineData("B738", 190)]
    [InlineData("B77W", 397)]
    [InlineData("E190", 115)]
    [InlineData("AT76", 79)]
    [InlineData("AT76", int.MaxValue)]
    public void More_seats_than_the_type_allows_is_refused(string type, int seats) {
        // Arrange
        AircraftRegistration anyRegistration = Any.AircraftRegistration().Generate();

        // Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => new Aircraft(anyRegistration, type, seats));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(int.MinValue)]
    public void Zero_or_negative_seats_are_refused(int seats) {
        // Arrange
        AircraftRegistration anyRegistration = Any.AircraftRegistration().Generate();
        string               anyType         = Any.String().OneOf(Aircraft.KnownTypes).Generate();

        // Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => new Aircraft(anyRegistration, anyType, seats));
    }

    [Theory]
    [InlineData("A319")]
    [InlineData("a320")]
    [InlineData("A320 ")]
    [InlineData("")]
    [InlineData("B77w")]
    public void A_type_outside_the_fleet_reference_list_is_refused(string type) {
        // Arrange
        AircraftRegistration anyRegistration = Any.AircraftRegistration().Generate();

        // Assert
        Assert.Throws<ArgumentException>(() => new Aircraft(anyRegistration, type, 10));
    }

    [Fact]
    public void A_null_type_is_refused_as_null() {
        // Arrange
        AircraftRegistration anyRegistration = Any.AircraftRegistration().Generate();

        // Assert
        Assert.Throws<ArgumentNullException>(() => new Aircraft(anyRegistration, null!, 10));
    }

    [Fact]
    public void A_null_registration_is_refused() {
        Assert.Throws<ArgumentNullException>(() => new Aircraft(null!, "A320", 10));
    }

    [Fact]
    public void The_known_types_are_the_six_of_the_fleet_reference_list() {
        Assert.Equal(6, Aircraft.KnownTypes.Count);
        Assert.Equivalent(new[] { "A320", "A321", "B738", "B77W", "E190", "AT76" }, Aircraft.KnownTypes);
    }

    [Theory]
    [InlineData("B77W", true)]
    [InlineData("A320", false)]
    [InlineData("A321", false)]
    [InlineData("B738", false)]
    [InlineData("E190", false)]
    [InlineData("AT76", false)]
    public void Only_the_B77W_is_wide_body(string type, bool wideBody) {
        // Arrange
        Aircraft aircraft = Any.Aircraft().WithTypeDesignator(type).Generate();

        // Assert
        Assert.Equal(wideBody, aircraft.IsWideBody);
    }

}
