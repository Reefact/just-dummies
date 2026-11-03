using JustDummies;

using RailwayInterlocking.Domain;

namespace RailwayInterlocking.Tests.Discovery;

public sealed class PointSettingTests {

    [Theory]
    [InlineData(PointPosition.Normal)]
    [InlineData(PointPosition.Reverse)]
    public void Keeps_the_point_name_and_the_required_position(PointPosition required) {
        // Act
        PointSetting setting = new PointSetting("P1", required);

        // Assert
        Assert.Equal("P1", setting.PointName);
        Assert.Equal(required, setting.Required);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("\t\n")]
    public void Refuses_a_blank_point_name(string blank) {
        // Arrange
        PointPosition anyPosition = Any.Enum<PointPosition>().Generate();

        // Act & Assert
        Assert.Throws<ArgumentException>(() => new PointSetting(blank, anyPosition));
    }

    [Fact]
    public void Refuses_a_null_point_name() {
        // Arrange
        PointPosition anyPosition = Any.Enum<PointPosition>().Generate();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new PointSetting(null!, anyPosition));
    }

    [Theory]
    [InlineData(2)]
    [InlineData(-1)]
    [InlineData(int.MaxValue)]
    public void Refuses_a_position_that_is_not_defined(int undefined) {
        // Arrange
        string anyName = Any.String().NotBlank().Generate();

        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => new PointSetting(anyName, (PointPosition)undefined));
    }

    [Theory]
    [InlineData("P1", PointPosition.Normal, "P1N")]
    [InlineData("P1", PointPosition.Reverse, "P1R")]
    public void Is_written_as_the_point_name_followed_by_N_or_R(string name, PointPosition required, string expected) {
        // Act & Assert
        Assert.Equal(expected, new PointSetting(name, required).ToString());
    }

}
