namespace PulmonaryNodules.Tests.Discovery;

public sealed class AnatomicalLandmarkTests {

    [Fact]
    public void A_landmark_keeps_its_kind_and_its_position() {
        // Arrange
        SpatialPoint anyPosition = Any.SpatialPoint().Generate();

        // Act
        AnatomicalLandmark landmark = new AnatomicalLandmark(LandmarkKind.AorticArchApex, anyPosition);

        // Assert
        Assert.Equal(LandmarkKind.AorticArchApex, landmark.Kind);
        Assert.Same(anyPosition, landmark.Position);
    }

    [Fact]
    public void Every_declared_kind_can_locate_a_landmark() {
        // Arrange
        LandmarkKind   anyKind     = Any.Enum<LandmarkKind>().Generate();
        SpatialPoint   anyPosition = Any.SpatialPoint().Generate();

        // Act
        Exception? exception = Record.Exception(() => new AnatomicalLandmark(anyKind, anyPosition));

        // Assert
        Assert.Null(exception);
    }

    [Fact]
    public void A_kind_the_enumeration_does_not_declare_is_refused() {
        // Arrange
        SpatialPoint anyPosition = Any.SpatialPoint().Generate();

        // Act
        ArgumentOutOfRangeException exception = Assert.Throws<ArgumentOutOfRangeException>(() => new AnatomicalLandmark((LandmarkKind)99, anyPosition));

        // Assert
        Assert.Equal("kind", exception.ParamName);
    }

    [Fact]
    public void A_landmark_without_position_is_refused() {
        // Arrange
        LandmarkKind anyKind = Any.Enum<LandmarkKind>().Generate();

        // Act
        ArgumentNullException exception = Assert.Throws<ArgumentNullException>(() => new AnatomicalLandmark(anyKind, null!));

        // Assert
        Assert.Equal("position", exception.ParamName);
    }

    [Fact]
    public void A_landmark_is_described_by_its_kind_and_position() {
        // Arrange
        AnatomicalLandmark landmark = new AnatomicalLandmark(LandmarkKind.SternalAngle, new SpatialPoint(1, 2, 3));

        // Act
        string text = landmark.ToString();

        // Assert
        Assert.Contains("SternalAngle", text);
        Assert.Contains("(1, 2, 3) mm", text);
    }

}
