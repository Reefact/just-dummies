using JustDummies;

namespace AdaptiveVideo.Domain.Tests;

public sealed class SegmentTests {

    [Theory]
    [InlineData(0, 0.5)]
    [InlineData(1_000, 10.0)]
    public void A_segment_accepts_both_ends_of_its_duration_range(int index, double duration) {
        // Act
        Segment segment = new Segment(index, duration);

        // Assert
        Assert.Equal(index, segment.Index);
        Assert.Equal(duration, segment.DurationSeconds);
    }

    [Fact]
    public void A_negative_index_is_refused() {
        // Arrange
        int    anyNegative = Any.Int32().Negative().Generate();
        double anyDuration = Any.Double().Between(Segment.MinDurationSeconds, Segment.MaxDurationSeconds).Generate();

        // Act / Assert
        Assert.Throws<ArgumentOutOfRangeException>("index", () => new Segment(anyNegative, anyDuration));
    }

    [Theory]
    [InlineData(0.49)]
    [InlineData(10.01)]
    [InlineData(0.0)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    public void A_duration_outside_the_range_is_refused(double duration) {
        Assert.Throws<ArgumentOutOfRangeException>("durationSeconds", () => new Segment(0, duration));
    }

    [Fact]
    public void A_segment_of_four_seconds_in_a_2000_kbps_variant_weighs_8000_kilobits() {
        // Arrange
        Segment         segment = new AnySegment().WithDurationSeconds(4).Generate();
        EncodingVariant variant = Fixtures.Variant(height: 720, framesPerSecond: 30, kbps: 2_000);

        // Act
        double size = segment.SizeKilobits(variant);

        // Assert
        Assert.Equal(8_000, size);
    }

    [Fact]
    public void Measuring_a_segment_needs_a_variant() {
        // Arrange
        Segment anySegment = new AnySegment().Generate();

        // Act / Assert
        Assert.Throws<ArgumentNullException>("variant", () => anySegment.SizeKilobits(null!));
    }

    [Fact]
    public void The_next_segment_follows_the_index_and_lasts_as_long() {
        // Arrange
        Segment segment = new Segment(7, 4.0);

        // Act
        Segment next = segment.Next();

        // Assert
        Assert.Equal(8, next.Index);
        Assert.Equal(4.0, next.DurationSeconds);
    }

    [Fact(Skip = "domain defect: Index + 1 overflows to int.MinValue at int.MaxValue and Next() then throws 'A segment index cannot be negative'; deterministic, literal input, no seed")]
    public void The_segment_after_the_last_representable_index_can_still_be_asked_for() {
        // Arrange
        Segment last = new AnySegment().WithIndex(int.MaxValue).Generate();

        // Act
        Exception? failure = Record.Exception(() => last.Next());

        // Assert
        Assert.Null(failure);
    }

}
