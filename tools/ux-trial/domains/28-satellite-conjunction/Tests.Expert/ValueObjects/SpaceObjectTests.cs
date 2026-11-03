using JustDummies;

using Xunit;

namespace SatelliteConjunction.Domain.Tests.Expert.ValueObjects;

public sealed class SpaceObjectTests {

    [Fact]
    public void An_object_exposes_what_it_was_built_from() {
        // Act
        SpaceObject station = new(25544, "ISS (ZARYA)", ObjectKind.Payload, 54.5, isActive: true);

        // Assert
        Assert.Equal(25544, station.CatalogNumber);
        Assert.Equal("ISS (ZARYA)", station.Name);
        Assert.Equal(ObjectKind.Payload, station.Kind);
        Assert.Equal(54.5, station.HardBodyRadiusMeters);
        Assert.True(station.IsActive);
    }

    [Fact]
    public void An_object_is_written_as_its_catalogue_number_then_its_name() {
        // Arrange
        SpaceObject station = Any.SpaceObject().WithCatalogNumber(25544).WithName("ISS (ZARYA)").Generate();

        // Act & Assert
        Assert.Equal("25544 ISS (ZARYA)", station.ToString());
    }

    [Theory]
    [InlineData(1)]
    [InlineData(999_999_999)]
    public void The_first_and_the_last_catalogue_numbers_are_accepted(int number) {
        // Arrange
        AnySpaceObject anyObject = Any.SpaceObject();

        // Act
        SpaceObject spaceObject = anyObject.WithCatalogNumber(number).Generate();

        // Assert
        Assert.Equal(number, spaceObject.CatalogNumber);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(1_000_000_000)]
    [InlineData(int.MinValue)]
    [InlineData(int.MaxValue)]
    public void A_catalogue_number_outside_one_to_999_999_999_is_refused(int number) {
        // Arrange
        string        anyName   = Any.String().Alpha().WithLengthBetween(1, SpaceObject.MaximumNameLength).Generate();
        ObjectKind    anyKind   = Any.Enum<ObjectKind>().Generate();
        double        anyRadius = Any.Double().Between(0.1, SpaceObject.MaximumHardBodyRadiusMeters).Generate();

        // Act
        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(() => new SpaceObject(number, anyName, anyKind, anyRadius, isActive: false));

        // Assert
        Assert.Equal("catalogNumber", refusal.ParamName);
    }

    [Fact]
    public void A_name_of_exactly_24_characters_is_accepted() {
        // Arrange
        string name = new('N', 24);

        // Act
        SpaceObject spaceObject = Any.SpaceObject().WithName(name).Generate();

        // Assert
        Assert.Equal(name, spaceObject.Name);
    }

    [Fact]
    public void A_name_of_25_characters_is_refused() {
        // Arrange
        string tooLong = new('N', 25);

        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => Any.SpaceObject().WithName(tooLong).Generate());

        // Assert
        Assert.Equal("name", refusal.ParamName);
    }

    [Fact]
    public void A_name_made_of_a_single_character_is_accepted() {
        // Act
        SpaceObject spaceObject = Any.SpaceObject().WithName("X").Generate();

        // Assert
        Assert.Equal("X", spaceObject.Name);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("   ")]
    [InlineData("\t")]
    [InlineData(" \r\n ")]
    public void A_blank_name_is_refused(string blank) {
        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => Any.SpaceObject().WithName(blank).Generate());

        // Assert
        Assert.Equal("name", refusal.ParamName);
    }

    [Fact]
    public void A_missing_name_is_refused() {
        // Arrange
        int        anyNumber = Any.Int32().Between(1, SpaceObject.MaximumCatalogNumber).Generate();
        ObjectKind anyKind   = Any.Enum<ObjectKind>().Generate();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new SpaceObject(anyNumber, null!, anyKind, 1, isActive: false));
    }

    [Fact]
    public void A_name_surrounded_by_blanks_is_kept_as_it_was_given() {
        // Act
        SpaceObject spaceObject = Any.SpaceObject().WithName("  STARLINK-1007  ").Generate();

        // Assert
        Assert.Equal("  STARLINK-1007  ", spaceObject.Name);
    }

    [Fact]
    public void A_kind_that_is_not_a_declared_member_is_refused() {
        // Arrange
        int        anyNumber = Any.Int32().Between(1, SpaceObject.MaximumCatalogNumber).Generate();
        ObjectKind undefined = (ObjectKind)42;

        // Act
        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(() => new SpaceObject(anyNumber, "Mystery", undefined, 1, isActive: false));

        // Assert
        Assert.Equal("kind", refusal.ParamName);
    }

    [Fact]
    public void The_largest_hard_body_radius_is_100_metres() {
        // Act
        SpaceObject spaceObject = Any.SpaceObject().WithHardBodyRadiusMeters(100).Generate();

        // Assert
        Assert.Equal(100, spaceObject.HardBodyRadiusMeters);
    }

    [Fact]
    public void A_radius_just_above_100_metres_is_refused() {
        // Act
        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(() => Any.SpaceObject().WithHardBodyRadiusMeters(Math.BitIncrement(100)).Generate());

        // Assert
        Assert.Equal("hardBodyRadiusMeters", refusal.ParamName);
    }

    [Fact]
    public void A_radius_of_zero_is_refused() {
        // Act
        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(() => Any.SpaceObject().WithHardBodyRadiusMeters(0).Generate());

        // Assert
        Assert.Equal("hardBodyRadiusMeters", refusal.ParamName);
    }

    [Fact]
    public void The_smallest_positive_radius_is_accepted() {
        // Act
        SpaceObject spaceObject = Any.SpaceObject().WithHardBodyRadiusMeters(double.Epsilon).Generate();

        // Assert
        Assert.Equal(double.Epsilon, spaceObject.HardBodyRadiusMeters);
    }

    [Fact]
    public void A_negative_radius_is_refused() {
        // Arrange
        double anyNegativeRadius = Any.Double().Negative().Generate();

        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => Any.SpaceObject().WithHardBodyRadiusMeters(anyNegativeRadius).Generate());
    }

    [Theory]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(double.NegativeInfinity)]
    public void A_radius_that_is_not_finite_is_refused(double nonFinite) {
        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => Any.SpaceObject().WithHardBodyRadiusMeters(nonFinite).Generate());
    }

    [Fact]
    public void An_active_payload_is_accepted() {
        // Act
        SpaceObject spaceObject = Any.SpaceObject().WithKind(ObjectKind.Payload).Active().Generate();

        // Assert
        Assert.True(spaceObject.IsActive);
        Assert.Equal(ObjectKind.Payload, spaceObject.Kind);
    }

    [Fact]
    public void A_retired_payload_is_accepted() {
        // Act
        SpaceObject spaceObject = Any.SpaceObject().WithKind(ObjectKind.Payload).Inactive().Generate();

        // Assert
        Assert.False(spaceObject.IsActive);
    }

    [Theory]
    [InlineData(ObjectKind.RocketBody)]
    [InlineData(ObjectKind.Debris)]
    public void A_rocket_body_or_a_debris_cannot_be_active(ObjectKind inertKind) {
        // Arrange
        int anyNumber = Any.Int32().Between(1, SpaceObject.MaximumCatalogNumber).Generate();

        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => new SpaceObject(anyNumber, "Inert", inertKind, 1, isActive: true));

        // Assert
        Assert.Equal("isActive", refusal.ParamName);
    }

    [Theory]
    [InlineData(ObjectKind.RocketBody)]
    [InlineData(ObjectKind.Debris)]
    public void A_rocket_body_or_a_debris_is_accepted_when_inactive(ObjectKind inertKind) {
        // Act
        SpaceObject spaceObject = Any.SpaceObject().WithKind(inertKind).Generate();

        // Assert
        Assert.False(spaceObject.IsActive);
        Assert.Equal(inertKind, spaceObject.Kind);
    }

}
