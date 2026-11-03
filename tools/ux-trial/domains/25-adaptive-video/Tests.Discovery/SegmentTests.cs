using AdaptiveVideo.Domain;

namespace AdaptiveVideo.Tests.Discovery;

public sealed class SegmentTests {

    [Theory]
    [InlineData(0, 0.5)]
    [InlineData(7, 10.0)]
    [InlineData(1_000, 4.0)]
    public void A_segment_inside_the_ranges_keeps_its_index_and_duration(int index, double durationSeconds) {
        Segment segment = new Segment(index, durationSeconds);

        Assert.Equal(index, segment.Index);
        Assert.Equal(durationSeconds, segment.DurationSeconds);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(int.MinValue)]
    public void A_negative_index_is_refused(int index) {
        double anyDuration = Any.Double().Between(Segment.MinDurationSeconds, Segment.MaxDurationSeconds).Generate();

        ArgumentOutOfRangeException exception = Assert.Throws<ArgumentOutOfRangeException>(() => new Segment(index, anyDuration));

        Assert.Equal("index", exception.ParamName);
    }

    [Theory]
    [InlineData(0.49)]
    [InlineData(0)]
    [InlineData(-4)]
    [InlineData(10.01)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(double.NegativeInfinity)]
    public void A_duration_outside_the_range_is_refused(double durationSeconds) {
        int anyIndex = Any.Int32().Between(0, 100_000).Generate();

        ArgumentOutOfRangeException exception = Assert.Throws<ArgumentOutOfRangeException>(() => new Segment(anyIndex, durationSeconds));

        Assert.Equal("durationSeconds", exception.ParamName);
    }

    [Fact]
    public void A_segment_weighs_the_variant_bitrate_times_its_duration() {
        Segment        segment = Any.Segment().WithDurationSeconds(4).Generate();
        EncodingVariant variant = Fixtures.Variant(height: 720, kbps: 2_500);

        double kilobits = segment.SizeKilobits(variant);

        Assert.Equal(10_000.0, kilobits);
    }

    [Fact]
    public void Weighing_a_segment_without_a_variant_is_refused() {
        Segment anySegment = Any.Segment().Generate();

        Assert.Throws<ArgumentNullException>(() => anySegment.SizeKilobits(null!));
    }

    [Fact]
    public void The_next_segment_follows_the_index_and_lasts_as_long() {
        Segment segment = Any.Segment().WithIndex(41).WithDurationSeconds(6.5).Generate();

        Segment next = segment.Next();

        Assert.Equal(42, next.Index);
        Assert.Equal(6.5, next.DurationSeconds);
    }

    [Fact]
    public void The_next_segment_is_a_new_one_and_leaves_the_current_untouched() {
        Segment segment = Any.Segment().WithIndex(3).Generate();

        Segment next = segment.Next();

        Assert.NotSame(segment, next);
        Assert.Equal(3, segment.Index);
    }

}
