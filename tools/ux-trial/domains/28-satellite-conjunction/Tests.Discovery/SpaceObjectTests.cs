using System;

using JustDummies;

using SatelliteConjunction.Domain;

using Xunit;

namespace SatelliteConjunction.Tests.Discovery;

public sealed class SpaceObjectTests {

    [Theory]
    [InlineData(ObjectKind.RocketBody)]
    [InlineData(ObjectKind.Debris)]
    public void An_inert_kind_of_object_cannot_be_active(ObjectKind inertKind) {
        // Arrange
        SpaceObject anyObject = Any.SpaceObject().WithKind(inertKind).Generate();

        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(
            () => new SpaceObject(anyObject.CatalogNumber, anyObject.Name, inertKind, anyObject.HardBodyRadiusMeters, isActive: true));

        // Assert
        Assert.Equal("isActive", refusal.ParamName);
    }

    [Fact]
    public void An_active_payload_is_accepted() {
        // Arrange
        SpaceObject anyObject = Any.SpaceObject().Generate();

        // Act
        SpaceObject satellite = new(anyObject.CatalogNumber, anyObject.Name, ObjectKind.Payload, anyObject.HardBodyRadiusMeters, isActive: true);

        // Assert
        Assert.True(satellite.IsActive);
    }

    [Fact]
    public void A_name_longer_than_the_catalogue_allows_is_refused() {
        // Arrange
        SpaceObject anyObject   = Any.SpaceObject().Generate();
        string      anyLongName = Any.String().Alpha().WithLengthBetween(SpaceObject.MaximumNameLength + 1, 80).Generate();

        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(
            () => new SpaceObject(anyObject.CatalogNumber, anyLongName, anyObject.Kind, anyObject.HardBodyRadiusMeters, anyObject.IsActive));

        // Assert
        Assert.Equal("name", refusal.ParamName);
    }

}
