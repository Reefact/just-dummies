using JustDummies;

using RailwayInterlocking.Domain;

namespace RailwayInterlocking.Tests.Discovery;

public sealed class TrackSectionTests {

    [Fact]
    public void A_new_section_is_free_and_unlocked() {
        // Arrange
        SectionId anyId = Any.SectionId().Generate();

        // Act
        TrackSection section = Any.TrackSection().WithId(anyId).Generate();

        // Assert
        Assert.Same(anyId, section.Id);
        Assert.False(section.IsOccupied);
        Assert.False(section.IsLocked);
        Assert.Null(section.LockedByRoute);
    }

    [Theory]
    [InlineData(10d)]
    [InlineData(5000d)]
    public void Accepts_the_limits_of_the_length_range(double length) {
        // Arrange
        SectionId anyId    = Any.SectionId().Generate();
        double    anySpeed = Any.Double().Positive().LessThanOrEqualTo(300d).Generate();

        // Act
        TrackSection section = new TrackSection(anyId, length, anySpeed);

        // Assert
        Assert.Equal(length, section.LengthMetres);
    }

    [Theory]
    [InlineData(9.99d)]
    [InlineData(5000.01d)]
    [InlineData(0d)]
    [InlineData(-100d)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(double.NegativeInfinity)]
    public void Refuses_a_length_outside_ten_to_five_thousand_metres(double length) {
        // Arrange
        SectionId anyId    = Any.SectionId().Generate();
        double    anySpeed = Any.Double().Positive().LessThanOrEqualTo(300d).Generate();

        // Act
        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(() => new TrackSection(anyId, length, anySpeed));

        // Assert
        Assert.Equal("lengthMetres", refusal.ParamName);
    }

    [Theory]
    [InlineData(300d)]
    [InlineData(0.001d)]
    public void Accepts_a_speed_up_to_three_hundred_and_above_zero(double speed) {
        // Arrange
        SectionId anyId     = Any.SectionId().Generate();
        double    anyLength = Any.Double().Between(10d, 5000d).Generate();

        // Act
        TrackSection section = new TrackSection(anyId, anyLength, speed);

        // Assert
        Assert.Equal(speed, section.LineSpeedKmh);
    }

    [Theory]
    [InlineData(0d)]
    [InlineData(-1d)]
    [InlineData(300.01d)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(double.NegativeInfinity)]
    public void Refuses_a_speed_outside_zero_exclusive_to_three_hundred(double speed) {
        // Arrange
        SectionId anyId     = Any.SectionId().Generate();
        double    anyLength = Any.Double().Between(10d, 5000d).Generate();

        // Act
        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(() => new TrackSection(anyId, anyLength, speed));

        // Assert
        Assert.Equal("lineSpeedKmh", refusal.ParamName);
    }

    [Fact]
    public void Refuses_a_null_id() {
        // Arrange
        double anyLength = Any.Double().Between(10d, 5000d).Generate();
        double anySpeed  = Any.Double().Positive().LessThanOrEqualTo(300d).Generate();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new TrackSection(null!, anyLength, anySpeed));
    }

    [Theory]
    [InlineData(1000d, 36d, 100d)]    // 36 km/h is 10 m/s
    [InlineData(360d, 36d, 36d)]
    [InlineData(500d, 180d, 10d)]     // 180 km/h is 50 m/s
    public void Clearance_time_is_the_length_over_the_speed_in_metres_per_second(double length, double speed, double expectedSeconds) {
        // Arrange
        TrackSection section = Any.TrackSection().WithLengthMetres(length).WithLineSpeedKmh(speed).Generate();

        // Act & Assert
        Assert.Equal(expectedSeconds, section.ClearanceTimeSeconds, 9);
    }

    [Fact]
    public void Clearance_time_is_longer_on_a_longer_section_at_the_same_speed() {
        // Arrange
        double anySpeed = Any.Double().Positive().LessThanOrEqualTo(300d).Generate();

        TrackSection shortSection = Any.TrackSection().WithLengthMetres(100d).WithLineSpeedKmh(anySpeed).Generate();
        TrackSection longSection  = Any.TrackSection().WithLengthMetres(200d).WithLineSpeedKmh(anySpeed).Generate();

        // Assert
        Assert.True(longSection.ClearanceTimeSeconds > shortSection.ClearanceTimeSeconds);
    }

}
