using JustDummies;
using PulmonaryNodules.Domain;
using PulmonaryNodules.Tests.Expert.Generators;
using Xunit;

namespace PulmonaryNodules.Tests.Expert.Tests;

public sealed class AnatomicalLandmarkTests {

    [Fact]
    public void A_landmark_keeps_the_structure_it_marks_and_where_it_was_found() {
        // Arrange
        SpatialPoint position = new SpatialPoint(10.0, -20.0, 30.0);

        // Act
        AnatomicalLandmark landmark = new AnatomicalLandmark(LandmarkKind.CarinaBifurcation, position);

        // Assert
        Assert.Equal(LandmarkKind.CarinaBifurcation, landmark.Kind);
        Assert.Same(position, landmark.Position);
    }

    [Fact]
    public void Every_declared_kind_can_be_located() {
        // Arrange
        SpatialPoint anyPosition = Any.SpatialPoint().Generate();

        // Act
        AnatomicalLandmark[] landmarks = Enum.GetValues<LandmarkKind>().Select(kind => new AnatomicalLandmark(kind, anyPosition)).ToArray();

        // Assert
        Assert.Equal(Enum.GetValues<LandmarkKind>(), landmarks.Select(l => l.Kind));
    }

    [Fact]
    public void A_kind_that_the_enumeration_does_not_declare_is_refused() {
        // Arrange
        SpatialPoint anyPosition = Any.SpatialPoint().Generate();
        LandmarkKind undeclared = (LandmarkKind)99;

        // Act
        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(() => new AnatomicalLandmark(undeclared, anyPosition));

        // Assert
        Assert.Equal("kind", refusal.ParamName);
    }

    [Fact]
    public void A_landmark_without_a_position_is_refused() {
        // Arrange
        LandmarkKind anyKind = Any.Enum<LandmarkKind>().Generate();

        // Act
        ArgumentNullException refusal = Assert.Throws<ArgumentNullException>(() => new AnatomicalLandmark(anyKind, null!));

        // Assert
        Assert.Equal("position", refusal.ParamName);
    }

}
