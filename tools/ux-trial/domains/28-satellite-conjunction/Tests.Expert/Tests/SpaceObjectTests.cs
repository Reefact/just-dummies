using JustDummies;
using SatelliteConjunction.Domain;

namespace SatelliteConjunction.Tests.Expert.Tests;

public sealed class SpaceObjectTests {

    private static readonly AnyInt32  AnyCatalogNumber = Any.Int32().Between(1, SpaceObject.MaximumCatalogNumber);
    private static readonly AnyString AnyName          = Any.String().NotBlank().WithMaxLength(SpaceObject.MaximumNameLength);
    private static readonly AnyDouble AnyRadius        = Any.Double().GreaterThan(0).LessThanOrEqualTo(SpaceObject.MaximumHardBodyRadiusMeters);

    [Fact]
    public void A_rocket_body_or_a_piece_of_debris_cannot_be_active() {
        // Arrange
        int        anyCatalogNumber = AnyCatalogNumber.Generate();
        string     anyName          = AnyName.Generate();
        ObjectKind anyInertKind     = Any.Enum<ObjectKind>().DifferentFrom(ObjectKind.Payload).Generate();
        double     anyRadius        = AnyRadius.Generate();

        // Act
        Action creating = () => new SpaceObject(anyCatalogNumber, anyName, anyInertKind, anyRadius, isActive: true);

        // Assert
        Assert.Throws<ArgumentException>("isActive", creating);
    }

    [Fact]
    public void A_name_longer_than_24_characters_is_refused() {
        // Arrange
        int        anyCatalogNumber     = AnyCatalogNumber.Generate();
        ObjectKind anyKind              = Any.Enum<ObjectKind>().Generate();
        double     anyRadius            = AnyRadius.Generate();
        string     twentyFiveCharacters = new('X', 25);

        // Act
        Action creating = () => new SpaceObject(anyCatalogNumber, twentyFiveCharacters, anyKind, anyRadius, isActive: false);

        // Assert
        Assert.Throws<ArgumentException>("name", creating);
    }

}
