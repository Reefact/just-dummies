using AdaptiveVideo.Domain;
using AdaptiveVideo.Tests.Regular.Generators;

using JustDummies.Xunit;

using Xunit;

namespace AdaptiveVideo.Tests.Regular;

public sealed class PlaybackBufferTests {

    [Fact, Reproducible]
    public void Draining_more_than_the_buffer_holds_returns_the_stall_and_empties_it() {
        // Arrange
        PlaybackBuffer buffer = Arbitrary.PlaybackBuffer().WithLevelSeconds(3).Generate();

        // Act
        double stall = buffer.Drain(5);

        // Assert
        Assert.Equal(2.0, stall, precision: 10);
        Assert.True(buffer.IsEmpty);
    }

    [Fact, Reproducible]
    public void A_segment_that_does_not_fit_is_refused() {
        // Arrange
        PlaybackBuffer buffer  = new(levelSeconds: 18, capacitySeconds: 20);
        Segment        segment = Arbitrary.Segment().WithDurationSeconds(4).Generate();

        // Act
        Exception? error = Record.Exception(() => buffer.Append(segment));

        // Assert
        Assert.IsType<InvalidOperationException>(error);
    }

    [Fact, Reproducible]
    public void An_appended_segment_adds_its_duration_to_the_level() {
        // Arrange
        PlaybackBuffer buffer  = new(levelSeconds: 10, capacitySeconds: 30);
        Segment        segment = Arbitrary.Segment().WithDurationSeconds(4).Generate();

        // Act
        buffer.Append(segment);

        // Assert
        Assert.Equal(14.0, buffer.LevelSeconds, precision: 10);
    }

}
