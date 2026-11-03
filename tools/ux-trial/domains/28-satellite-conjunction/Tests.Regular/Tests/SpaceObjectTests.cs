using JustDummies;

using SatelliteConjunction.Domain;

using Xunit;

namespace SatelliteConjunction.Tests.Regular;

public sealed class SpaceObjectTests {

    [Fact]
    public void An_object_keeps_its_attributes() {
        // Arrange
        int    anyCatalogNumber = Any.Int32().Between(1, SpaceObject.MaximumCatalogNumber).Generate();
        string anyName          = Any.String().Alpha().WithLengthBetween(1, SpaceObject.MaximumNameLength).Generate();
        double anyRadius        = Any.Double().Between(0.01, SpaceObject.MaximumHardBodyRadiusMeters).Generate();

        // Act
        SpaceObject spaceObject = new(anyCatalogNumber, anyName, ObjectKind.Payload, anyRadius, isActive: true);

        // Assert
        Assert.Equal(anyCatalogNumber, spaceObject.CatalogNumber);
        Assert.Equal(anyName, spaceObject.Name);
        Assert.Equal(ObjectKind.Payload, spaceObject.Kind);
        Assert.Equal(anyRadius, spaceObject.HardBodyRadiusMeters);
        Assert.True(spaceObject.IsActive);
    }

    [Fact]
    public void An_arbitrary_object_is_always_accepted() {
        // Arrange / Act
        SpaceObject anyObject = Any.SpaceObject().Generate();

        // Assert
        Assert.True(!anyObject.IsActive || anyObject.Kind == ObjectKind.Payload);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(SpaceObject.MaximumCatalogNumber)]
    public void The_two_ends_of_the_catalogue_range_are_accepted(int catalogNumber) {
        // Arrange / Act
        SpaceObject spaceObject = Any.SpaceObject().WithCatalogNumber(catalogNumber).Generate();

        // Assert
        Assert.Equal(catalogNumber, spaceObject.CatalogNumber);
    }

    [Fact]
    public void A_catalogue_number_of_zero_or_less_is_refused() {
        // Arrange
        int anyNonPositive = Any.Int32().LessThanOrEqualTo(0).Generate();

        // Act
        Exception? refusal = Record.Exception(() => Any.SpaceObject().WithCatalogNumber(anyNonPositive).Generate());

        // Assert
        Assert.IsType<ArgumentOutOfRangeException>(refusal);
    }

    [Fact]
    public void A_catalogue_number_above_the_maximum_is_refused() {
        // Arrange
        int anyExcessive = Any.Int32().GreaterThan(SpaceObject.MaximumCatalogNumber).Generate();

        // Act
        Exception? refusal = Record.Exception(() => Any.SpaceObject().WithCatalogNumber(anyExcessive).Generate());

        // Assert
        Assert.IsType<ArgumentOutOfRangeException>(refusal);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("\t \n")]
    public void A_blank_name_is_refused(string name) {
        // Arrange / Act
        Exception? refusal = Record.Exception(() => Any.SpaceObject().WithName(name).Generate());

        // Assert
        Assert.IsType<ArgumentException>(refusal);
    }

    [Fact]
    public void A_missing_name_is_refused() {
        // Arrange / Act
        Exception? refusal = Record.Exception(() => Any.SpaceObject().WithName((string)null!).Generate());

        // Assert
        Assert.IsType<ArgumentNullException>(refusal);
    }

    [Fact]
    public void A_name_of_the_maximum_length_is_accepted() {
        // Arrange
        string anyLongestName = Any.String().Alpha().WithLength(SpaceObject.MaximumNameLength).Generate();

        // Act
        SpaceObject spaceObject = Any.SpaceObject().WithName(anyLongestName).Generate();

        // Assert
        Assert.Equal(anyLongestName, spaceObject.Name);
    }

    [Fact]
    public void A_name_longer_than_the_maximum_is_refused() {
        // Arrange
        string anyTooLongName = Any.String().Alpha().WithLength(SpaceObject.MaximumNameLength + 1).Generate();

        // Act
        Exception? refusal = Record.Exception(() => Any.SpaceObject().WithName(anyTooLongName).Generate());

        // Assert
        Assert.IsType<ArgumentException>(refusal);
    }

    [Fact]
    public void A_kind_that_is_not_defined_is_refused() {
        // Arrange
        ObjectKind anyUndefinedKind = (ObjectKind)Any.Int32().GreaterThan((int)ObjectKind.Debris).Generate();

        // Act
        Exception? refusal = Record.Exception(() => Any.SpaceObject().Inert().WithKind(anyUndefinedKind).Generate());

        // Assert
        Assert.IsType<ArgumentOutOfRangeException>(refusal);
    }

    [Fact]
    public void A_negative_kind_is_not_defined_either() {
        // Arrange
        ObjectKind anyNegativeKind = (ObjectKind)Any.Int32().Negative().Generate();

        // Act
        Exception? refusal = Record.Exception(() => Any.SpaceObject().Inert().WithKind(anyNegativeKind).Generate());

        // Assert
        Assert.IsType<ArgumentOutOfRangeException>(refusal);
    }

    [Fact]
    public void A_radius_of_zero_or_less_is_refused() {
        // Arrange
        double anyNonPositive = Any.Double().LessThanOrEqualTo(0).Generate();

        // Act
        Exception? refusal = Record.Exception(() => Any.SpaceObject().WithHardBodyRadiusMeters(anyNonPositive).Generate());

        // Assert
        Assert.IsType<ArgumentOutOfRangeException>(refusal);
    }

    [Fact]
    public void A_radius_above_one_hundred_metres_is_refused() {
        // Arrange
        double anyExcessive = Any.Double().GreaterThan(SpaceObject.MaximumHardBodyRadiusMeters).Generate();

        // Act
        Exception? refusal = Record.Exception(() => Any.SpaceObject().WithHardBodyRadiusMeters(anyExcessive).Generate());

        // Assert
        Assert.IsType<ArgumentOutOfRangeException>(refusal);
    }

    [Fact]
    public void A_radius_of_exactly_one_hundred_metres_is_accepted() {
        // Arrange / Act
        SpaceObject spaceObject = Any.SpaceObject().WithHardBodyRadiusMeters(100).Generate();

        // Assert
        Assert.Equal(100d, spaceObject.HardBodyRadiusMeters);
    }

    [Theory]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    public void A_radius_that_is_not_finite_is_refused(double radius) {
        // Arrange / Act
        Exception? refusal = Record.Exception(() => Any.SpaceObject().WithHardBodyRadiusMeters(radius).Generate());

        // Assert
        Assert.IsType<ArgumentOutOfRangeException>(refusal);
    }

    [Theory]
    [InlineData(ObjectKind.RocketBody)]
    [InlineData(ObjectKind.Debris)]
    public void Only_a_payload_can_be_active(ObjectKind kind) {
        // Arrange: built by hand, since the generator itself never draws an active non-payload
        int    anyCatalogNumber = Any.Int32().Between(1, SpaceObject.MaximumCatalogNumber).Generate();
        string anyName          = Any.String().Alpha().WithLengthBetween(1, SpaceObject.MaximumNameLength).Generate();

        // Act
        Exception? refusal = Record.Exception(() => new SpaceObject(anyCatalogNumber, anyName, kind, 1, isActive: true));

        // Assert
        Assert.IsType<ArgumentException>(refusal);
    }

    [Fact]
    public void A_payload_can_be_active() {
        // Arrange / Act
        SpaceObject spaceObject = Any.SpaceObject().Active().Generate();

        // Assert
        Assert.Equal(ObjectKind.Payload, spaceObject.Kind);
        Assert.True(spaceObject.IsActive);
    }

    [Fact]
    public void An_inert_object_can_be_of_any_kind() {
        // Arrange
        ObjectKind anyKind = Any.Enum<ObjectKind>().Generate();

        // Act
        SpaceObject spaceObject = Any.SpaceObject().Inert().WithKind(anyKind).Generate();

        // Assert
        Assert.Equal(anyKind, spaceObject.Kind);
        Assert.False(spaceObject.IsActive);
    }

    [Fact]
    public void An_object_is_written_with_its_catalogue_number_and_name() {
        // Arrange
        SpaceObject spaceObject = new(25544, "ISS (ZARYA)", ObjectKind.Payload, 50, isActive: true);

        // Act
        string text = spaceObject.ToString();

        // Assert
        Assert.Equal("25544 ISS (ZARYA)", text);
    }

}
