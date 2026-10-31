using JustDummies;

using RailwayInterlocking.Domain;

using Xunit;

namespace RailwayInterlocking.Tests.Expert.Tests;

public sealed class TrackSectionTests {

    [Fact]
    public void Clearance_time_is_the_time_to_run_the_section_length_at_line_speed() {
        // Arrange
        TrackSection section = Any.TrackSection().WithLengthMetres(500).WithLineSpeedKmh(90).Generate();

        // Act
        double clearance = section.ClearanceTimeSeconds;

        // Assert
        Assert.Equal(20, clearance, precision: 9);   // 500 m at 25 m/s
    }

    [Fact]
    public void A_track_circuit_shorter_than_ten_metres_is_refused() {
        // Arrange
        SectionId anyId        = Any.SectionId().Generate();
        double    anyLineSpeed = Any.Double().GreaterThan(0).LessThanOrEqualTo(300).Generate();

        // Act
        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(() => new TrackSection(anyId, 9.5, anyLineSpeed));

        // Assert
        Assert.Equal("lengthMetres", refusal.ParamName);
    }

    [Fact]
    public void A_new_track_section_is_clear_and_free() {
        // Arrange
        AnyTrackSection anySection = Any.TrackSection();

        // Act
        TrackSection section = anySection.Generate();

        // Assert
        Assert.False(section.IsOccupied);
        Assert.False(section.IsLocked);
    }

}
