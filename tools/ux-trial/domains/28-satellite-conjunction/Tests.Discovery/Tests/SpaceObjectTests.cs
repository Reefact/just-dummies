using JustDummies;

using SatelliteConjunction.Domain;

using Xunit;

namespace SatelliteConjunction.Tests.Discovery;

public sealed class SpaceObjectTests {

    [Fact]
    public void A_space_object_keeps_its_identity_and_characteristics() {
        // Act
        SpaceObject station = new SpaceObject(25544, "ISS (ZARYA)", ObjectKind.Payload, 55.5, true);

        // Assert
        Assert.Equal(25544, station.CatalogNumber);
        Assert.Equal("ISS (ZARYA)", station.Name);
        Assert.Equal(ObjectKind.Payload, station.Kind);
        Assert.Equal(55.5, station.HardBodyRadiusMeters);
        Assert.True(station.IsActive);
    }

    [Fact]
    public void A_space_object_is_written_as_its_catalogue_number_and_name() {
        // Arrange
        SpaceObject station = Any.SpaceObject().WithCatalogNumber(25544).WithName("ISS (ZARYA)").Generate();

        // Act / Assert
        Assert.Equal("25544 ISS (ZARYA)", station.ToString());
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(1_000_000_000)]
    [InlineData(int.MaxValue)]
    [InlineData(int.MinValue)]
    public void A_space_object_refuses_a_catalogue_number_outside_one_to_999_999_999(int catalogNumber) {
        // Act
        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(() => Any.SpaceObject().WithCatalogNumber(catalogNumber).Generate());

        // Assert
        Assert.Equal("catalogNumber", refusal.ParamName);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(999_999_999)]
    public void A_space_object_accepts_the_edges_of_the_catalogue(int catalogNumber) {
        // Act
        SpaceObject spaceObject = Any.SpaceObject().WithCatalogNumber(catalogNumber).Generate();

        // Assert
        Assert.Equal(catalogNumber, spaceObject.CatalogNumber);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("\t\n")]
    public void A_space_object_refuses_a_blank_name(string blank) {
        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => Any.SpaceObject().WithName(blank).Generate());

        // Assert
        Assert.Equal("name", refusal.ParamName);
    }

    [Fact]
    public void A_space_object_refuses_a_missing_name() {
        // Act
        ArgumentNullException refusal = Assert.Throws<ArgumentNullException>(() => Any.SpaceObject().WithName((string)null!).Generate());

        // Assert
        Assert.Equal("name", refusal.ParamName);
    }

    [Fact]
    public void A_space_object_refuses_a_name_of_twenty_five_characters() {
        // Arrange
        string twentyFive = new string('A', 25);

        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => Any.SpaceObject().WithName(twentyFive).Generate());

        // Assert
        Assert.Equal("name", refusal.ParamName);
    }

    [Fact]
    public void A_space_object_accepts_a_name_of_twenty_four_characters() {
        // Arrange
        string twentyFour = new string('A', 24);

        // Act
        SpaceObject spaceObject = Any.SpaceObject().WithName(twentyFour).Generate();

        // Assert
        Assert.Equal(twentyFour, spaceObject.Name);
    }

    [Fact]
    public void A_space_object_refuses_an_undefined_kind() {
        // Act
        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(() => Any.SpaceObject().WithKind((ObjectKind)42).Generate());

        // Assert
        Assert.Equal("kind", refusal.ParamName);
    }

    [Theory]
    [InlineData(0.0)]
    [InlineData(-1.0)]
    [InlineData(100.001)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    public void A_space_object_refuses_a_hard_body_radius_outside_zero_to_one_hundred_metres(double radius) {
        // Act
        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(() => Any.SpaceObject().WithHardBodyRadiusMeters(radius).Generate());

        // Assert
        Assert.Equal("hardBodyRadiusMeters", refusal.ParamName);
    }

    [Theory]
    [InlineData(0.001)]
    [InlineData(100.0)]
    public void A_space_object_accepts_the_edges_of_the_radius_range(double radius) {
        // Act
        SpaceObject spaceObject = Any.SpaceObject().WithHardBodyRadiusMeters(radius).Generate();

        // Assert
        Assert.Equal(radius, spaceObject.HardBodyRadiusMeters);
    }

    [Theory]
    [InlineData(ObjectKind.RocketBody)]
    [InlineData(ObjectKind.Debris)]
    public void Only_a_payload_can_be_active(ObjectKind inertKind) {
        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => Any.SpaceObject().WithKind(inertKind).WithIsActive(true).Generate());

        // Assert
        Assert.Equal("isActive", refusal.ParamName);
    }

    [Fact]
    public void An_active_payload_is_accepted() {
        // Act
        SpaceObject satellite = Any.SpaceObject().Active().Generate();

        // Assert
        Assert.True(satellite.IsActive);
        Assert.Equal(ObjectKind.Payload, satellite.Kind);
    }

    [Fact]
    public void A_payload_may_be_retired() {
        // Act
        SpaceObject retired = Any.SpaceObject().WithKind(ObjectKind.Payload).WithIsActive(false).Generate();

        // Assert
        Assert.False(retired.IsActive);
    }

    [Theory]
    [InlineData(ObjectKind.RocketBody)]
    [InlineData(ObjectKind.Debris)]
    public void Rocket_bodies_and_debris_are_inert(ObjectKind inertKind) {
        // Act
        SpaceObject inert = Any.SpaceObject().WithKind(inertKind).WithIsActive(false).Generate();

        // Assert
        Assert.False(inert.IsActive);
        Assert.Equal(inertKind, inert.Kind);
    }

}
