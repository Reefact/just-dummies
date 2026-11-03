using JustDummies;

using RailwayInterlocking.Domain;

namespace RailwayInterlocking.Tests.Discovery;

public sealed class PointTests {

    [Theory]
    [InlineData(PointPosition.Normal)]
    [InlineData(PointPosition.Reverse)]
    public void A_new_point_lies_in_its_section_in_its_initial_position_and_is_free(PointPosition initial) {
        // Arrange
        SectionId anySection = Any.SectionId().Generate();

        // Act
        Point point = new Point("P1", anySection, initial);

        // Assert
        Assert.Equal("P1", point.Name);
        Assert.Same(anySection, point.Section);
        Assert.Equal(initial, point.Position);
        Assert.False(point.IsLocked);
        Assert.Null(point.LockedByRoute);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Refuses_a_blank_name(string blank) {
        // Arrange
        SectionId     anySection  = Any.SectionId().Generate();
        PointPosition anyPosition = Any.Enum<PointPosition>().Generate();

        // Act & Assert
        Assert.Throws<ArgumentException>(() => new Point(blank, anySection, anyPosition));
    }

    [Fact]
    public void Refuses_a_null_name() {
        // Arrange
        SectionId     anySection  = Any.SectionId().Generate();
        PointPosition anyPosition = Any.Enum<PointPosition>().Generate();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new Point(null!, anySection, anyPosition));
    }

    [Fact]
    public void Refuses_a_null_section() {
        // Arrange
        string        anyName     = Any.String().NotBlank().Generate();
        PointPosition anyPosition = Any.Enum<PointPosition>().Generate();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new Point(anyName, null!, anyPosition));
    }

    [Fact]
    public void Refuses_a_position_that_is_not_defined() {
        // Arrange
        string    anyName    = Any.String().NotBlank().Generate();
        SectionId anySection = Any.SectionId().Generate();

        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => new Point(anyName, anySection, (PointPosition)7));
    }

}
