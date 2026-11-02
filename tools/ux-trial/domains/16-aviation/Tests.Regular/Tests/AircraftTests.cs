using JustDummies;

using Xunit;

namespace Aviation.Domain.Tests.Regular;

public sealed class AircraftTests {

    public static TheoryData<string, int> MaximumSeats => new TheoryData<string, int> {
        { "A320", 186 },
        { "A321", 230 },
        { "B738", 189 },
        { "B77W", 396 },
        { "E190", 114 },
        { "AT76", 78 }
    };

    [Theory]
    [MemberData(nameof(MaximumSeats))]
    public void An_aircraft_accepts_any_seat_count_up_to_the_maximum_of_its_type(string type, int maximumSeats) {
        // Arrange
        AircraftRegistration anyRegistration = Arbitrary.AircraftRegistration().Generate();

        // Act
        Aircraft aircraft = new Aircraft(anyRegistration, type, maximumSeats);

        // Assert
        Assert.Same(anyRegistration, aircraft.Registration);
        Assert.Equal(type, aircraft.TypeDesignator);
        Assert.Equal(maximumSeats, aircraft.Seats);
    }

    [Theory]
    [MemberData(nameof(MaximumSeats))]
    public void An_aircraft_accepts_a_single_seat(string type, int maximumSeats) {
        // Arrange
        AircraftRegistration anyRegistration = Arbitrary.AircraftRegistration().Generate();

        // Act
        Aircraft aircraft = new Aircraft(anyRegistration, type, 1);

        // Assert
        Assert.Equal(1, aircraft.Seats);
        Assert.True(maximumSeats >= aircraft.Seats);
    }

    [Theory]
    [MemberData(nameof(MaximumSeats))]
    public void An_aircraft_refuses_one_seat_more_than_the_maximum_of_its_type(string type, int maximumSeats) {
        // Arrange
        AircraftRegistration anyRegistration = Arbitrary.AircraftRegistration().Generate();

        // Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => new Aircraft(anyRegistration, type, maximumSeats + 1));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(int.MinValue)]
    public void An_aircraft_refuses_a_seat_count_below_one(int seats) {
        // Arrange
        AircraftRegistration anyRegistration = Arbitrary.AircraftRegistration().Generate();

        // Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => new Aircraft(anyRegistration, "A320", seats));
    }

    [Fact]
    public void A_seat_count_that_suits_a_wide_body_does_not_suit_a_narrow_body() {
        // Arrange
        AircraftRegistration anyRegistration = Arbitrary.AircraftRegistration().Generate();

        // Act
        Aircraft wideBody = new Aircraft(anyRegistration, "B77W", 300);

        // Assert
        Assert.Equal(300, wideBody.Seats);
        Assert.Throws<ArgumentOutOfRangeException>(() => new Aircraft(anyRegistration, "A320", 300));
    }

    [Theory]
    [InlineData("A380")]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("a320")]
    [InlineData("A320 ")]
    [InlineData("b77w")]
    public void An_unknown_or_differently_cased_type_designator_is_refused(string type) {
        // Arrange
        AircraftRegistration anyRegistration = Arbitrary.AircraftRegistration().Generate();

        // Assert
        Assert.Throws<ArgumentException>(() => new Aircraft(anyRegistration, type, 50));
    }

    [Fact]
    public void A_null_type_designator_is_refused() {
        // Arrange
        AircraftRegistration anyRegistration = Arbitrary.AircraftRegistration().Generate();

        // Assert
        Assert.Throws<ArgumentNullException>(() => new Aircraft(anyRegistration, null!, 50));
    }

    [Fact]
    public void A_null_registration_is_refused() {
        Assert.Throws<ArgumentNullException>(() => new Aircraft(null!, "A320", 50));
    }

    [Fact]
    public void The_known_types_are_the_six_of_the_fleet_reference_list() {
        Assert.Equal(6, Aircraft.KnownTypes.Count);
        Assert.Contains("A320", Aircraft.KnownTypes);
        Assert.Contains("A321", Aircraft.KnownTypes);
        Assert.Contains("B738", Aircraft.KnownTypes);
        Assert.Contains("B77W", Aircraft.KnownTypes);
        Assert.Contains("E190", Aircraft.KnownTypes);
        Assert.Contains("AT76", Aircraft.KnownTypes);
    }

    [Fact]
    public void Only_the_B77W_is_a_wide_body() {
        // Arrange
        AircraftRegistration anyRegistration = Arbitrary.AircraftRegistration().Generate();

        // Act
        Aircraft wideBody = new Aircraft(anyRegistration, "B77W", 1);

        // Assert
        Assert.True(wideBody.IsWideBody);
        foreach (string type in Aircraft.KnownTypes.Where(t => t != "B77W")) {
            Assert.False(new Aircraft(anyRegistration, type, 1).IsWideBody, type);
        }
    }

}
