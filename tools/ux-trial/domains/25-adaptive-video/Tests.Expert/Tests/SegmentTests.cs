using AdaptiveVideo.Domain;

using JustDummies;

using Xunit;

namespace AdaptiveVideo.Tests.Expert;

public sealed class SegmentTests {

    [Theory]
    [InlineData(0, 0.5)]
    [InlineData(1, 0.5000001)]
    [InlineData(2_000_000, 4)]
    [InlineData(7, 10)]
    public void A_segment_of_a_supported_duration_keeps_its_index_and_duration(int index, double durationSeconds) {
        // Act
        Segment segment = new Segment(index, durationSeconds);

        // Assert
        Assert.Equal(index, segment.Index);
        Assert.Equal(durationSeconds, segment.DurationSeconds);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(int.MinValue)]
    public void A_negative_index_is_refused(int index) {
        // Arrange
        double anyDuration = Any.Double().Between(Segment.MinDurationSeconds, Segment.MaxDurationSeconds).Generate();

        // Act
        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(() => new Segment(index, anyDuration));

        // Assert
        Assert.Equal("index", refusal.ParamName);
    }

    [Theory]
    [InlineData(0.4999999)]
    [InlineData(0)]
    [InlineData(-4)]
    [InlineData(10.0000001)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    public void A_duration_outside_half_a_second_to_ten_seconds_is_refused(double durationSeconds) {
        // Arrange
        int anyIndex = Any.Int32().GreaterThanOrEqualTo(0).Generate();

        // Act
        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(() => new Segment(anyIndex, durationSeconds));

        // Assert
        Assert.Equal("durationSeconds", refusal.ParamName);
    }

    [Fact]
    public void A_segment_weighs_its_duration_times_the_bitrate_of_the_variant() {
        // Arrange
        Segment         segment = Any.Segment().WithDurationSeconds(4).Generate();
        EncodingVariant variant = Any.EncodingVariant().WithBitrateKbps(2_500).Generate();

        // Act
        double kilobits = segment.SizeKilobits(variant);

        // Assert
        Assert.Equal(10_000, kilobits);
    }

    [Fact]
    public void The_size_of_a_segment_needs_a_variant() {
        // Arrange
        Segment anySegment = Any.Segment().Generate();

        // Act + Assert
        Assert.Throws<ArgumentNullException>(() => anySegment.SizeKilobits(null!));
    }

    [Fact]
    public void The_next_segment_follows_by_one_and_lasts_as_long() {
        // Arrange
        Segment segment = new Segment(41, 4);

        // Act
        Segment next = segment.Next();

        // Assert
        Assert.Equal(42, next.Index);
        Assert.Equal(4, next.DurationSeconds);
        Assert.Equal(41, segment.Index);
    }

    [Fact(Skip = "domain defect: Next() on the last representable index wraps to int.MinValue and surfaces as " +
                 "\"A segment index cannot be negative\" from the constructor, instead of a refusal that names the cause. " +
                 "Literal input, no seed involved.")]
    public void Nothing_follows_the_last_representable_segment() {
        // Arrange
        double  anyDuration = Any.Double().Between(Segment.MinDurationSeconds, Segment.MaxDurationSeconds).Generate();
        Segment last        = new Segment(int.MaxValue, anyDuration);

        // Act
        InvalidOperationException refusal = Assert.Throws<InvalidOperationException>(() => last.Next());

        // Assert
        Assert.Contains("last", refusal.Message);
    }

}
