using JustDummies.Xunit;

using RailwayInterlocking.Domain;

using Xunit;

namespace RailwayInterlocking.Tests.Regular;

public sealed class TrackTests {

    [Fact, Reproducible]
    public void Clearance_time_is_the_length_run_at_line_speed() {
        // Arrange
        TrackSection section = Arbitrary.TrackSection().WithLengthMetres(500).WithLineSpeedKmh(90).Generate();

        // Act
        double clearance = section.ClearanceTimeSeconds;

        // Assert
        Assert.Equal(20, clearance, precision: 9);
    }

    [Fact, Reproducible]
    public void A_section_shorter_than_ten_metres_is_refused() {
        // Arrange
        AnyTrackSection tooShort = Arbitrary.TrackSection().WithLengthMetres(9.5);

        // Act
        Exception? refusal = Record.Exception(() => tooShort.Generate());

        // Assert
        Assert.IsType<ArgumentOutOfRangeException>(refusal);
    }

    [Fact]
    public void Section_ids_follow_the_track_circuit_naming_scheme() {
        // Arrange
        string lowerCase = "t101";

        // Act
        Exception? refusal = Record.Exception(() => new SectionId(lowerCase));

        // Assert
        Assert.IsType<ArgumentException>(refusal);
    }

    [Fact, Reproducible]
    public void Two_section_ids_with_the_same_value_are_the_same_section() {
        // Arrange
        SectionId anyId = Arbitrary.SectionId().Generate();

        // Act
        bool same = anyId.Equals(new SectionId(anyId.Value));

        // Assert
        Assert.True(same);
    }

}
