using JustDummies;
using JustDummies.Xunit;

using RailwayInterlocking.Domain;

using Xunit;

namespace RailwayInterlocking.Tests.Discovery.Tests;

[Reproducible]
public sealed class ValueObjectTests {

    [Fact]
    public void Two_section_ids_with_the_same_text_are_equal() {
        // Arrange
        SectionId anyId = Any.SectionId().Generate();

        // Act
        SectionId sameText = new SectionId(anyId.Value);

        // Assert
        Assert.Equal(anyId, sameText);
    }

    [Theory]
    [InlineData("t101")]
    [InlineData("ABC1")]
    [InlineData("T12345")]
    public void A_malformed_section_id_is_refused(string malformed) {
        // Arrange
        // nothing: the malformed text is the case

        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => new SectionId(malformed));

        // Assert
        Assert.Equal("value", refusal.ParamName);
    }

    [Theory]
    [InlineData("A123")]
    [InlineData("1a23")]
    public void A_malformed_headcode_is_refused(string malformed) {
        // Arrange
        // nothing: the malformed text is the case

        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => new TrainId(malformed));

        // Assert
        Assert.Equal("headcode", refusal.ParamName);
    }

    [Fact]
    public void Clearance_time_is_the_length_covered_at_line_speed() {
        // Arrange
        TrackSection section = Any.TrackSection().WithLengthMetres(500).WithLineSpeedKmh(90).Generate();

        // Act
        double clearance = section.ClearanceTimeSeconds;

        // Assert
        Assert.Equal(20, clearance, 6);
    }

    [Fact]
    public void A_section_shorter_than_ten_metres_is_refused() {
        // Arrange
        AnyTrackSection anyTooShort = Any.TrackSection().WithLengthMetres(9.5);

        // Act
        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(() => anyTooShort.Generate());

        // Assert
        Assert.Equal("lengthMetres", refusal.ParamName);
    }

    [Fact]
    public void A_fresh_section_is_clear_and_unlocked() {
        // Arrange
        // nothing beyond the dummy itself

        // Act
        TrackSection anySection = Any.TrackSection().Generate();

        // Assert
        Assert.False(anySection.IsOccupied);
        Assert.False(anySection.IsLocked);
    }

}
