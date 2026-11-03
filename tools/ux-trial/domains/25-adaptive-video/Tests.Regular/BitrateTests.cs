using JustDummies;

namespace AdaptiveVideo.Domain.Tests;

public sealed class BitrateTests {

    [Theory]
    [InlineData(50)]
    [InlineData(100_000)]
    public void A_bitrate_accepts_both_ends_of_its_range(int kbps) {
        // Act
        Bitrate bitrate = new Bitrate(kbps);

        // Assert
        Assert.Equal(kbps, bitrate.Kbps);
    }

    [Theory]
    [InlineData(49)]
    [InlineData(100_001)]
    [InlineData(0)]
    [InlineData(-4_000)]
    public void A_bitrate_outside_the_range_is_refused(int kbps) {
        Assert.Throws<ArgumentOutOfRangeException>("kbps", () => new Bitrate(kbps));
    }

    [Fact]
    public void A_bitrate_below_the_minimum_is_refused_whatever_it_is() {
        // Arrange
        int anyTooLow = Any.Int32().LessThan(Bitrate.MinKbps).Generate();

        // Act / Assert
        Assert.Throws<ArgumentOutOfRangeException>("kbps", () => new Bitrate(anyTooLow));
    }

    [Fact]
    public void A_bitrate_above_the_maximum_is_refused_whatever_it_is() {
        // Arrange
        int anyTooHigh = Any.Int32().GreaterThan(Bitrate.MaxKbps).Generate();

        // Act / Assert
        Assert.Throws<ArgumentOutOfRangeException>("kbps", () => new Bitrate(anyTooHigh));
    }

    [Fact]
    public void Downloading_two_seconds_of_4000_kbps_media_over_8000_kbps_takes_one_second() {
        // Arrange
        Bitrate bitrate = new Bitrate(4_000);

        // Act
        double seconds = bitrate.DownloadSeconds(mediaSeconds: 2, throughputKbps: 8_000);

        // Assert
        Assert.Equal(1.0, seconds);
    }

    [Fact]
    public void Downloading_slower_than_real_time_takes_longer_than_the_media_lasts() {
        // Arrange
        Bitrate bitrate = new Bitrate(3_000);

        // Act
        double seconds = bitrate.DownloadSeconds(mediaSeconds: 4, throughputKbps: 1_500);

        // Assert
        Assert.Equal(8.0, seconds);
    }

    [Fact]
    public void A_media_duration_that_is_not_positive_is_refused() {
        // Arrange
        Bitrate bitrate         = new AnyBitrate().Generate();
        double  anyNotPositive  = Any.Double().LessThanOrEqualTo(0).Generate();
        double  anyThroughput   = Any.Double().Positive().Generate();

        // Act / Assert
        Assert.Throws<ArgumentOutOfRangeException>("mediaSeconds", () => bitrate.DownloadSeconds(anyNotPositive, anyThroughput));
    }

    [Fact]
    public void A_throughput_that_is_not_positive_is_refused() {
        // Arrange
        Bitrate bitrate        = new AnyBitrate().Generate();
        double  anyDuration    = Any.Double().Between(Segment.MinDurationSeconds, Segment.MaxDurationSeconds).Generate();
        double  anyNotPositive = Any.Double().LessThanOrEqualTo(0).Generate();

        // Act / Assert
        Assert.Throws<ArgumentOutOfRangeException>("throughputKbps", () => bitrate.DownloadSeconds(anyDuration, anyNotPositive));
    }

    [Fact]
    public void Two_bitrates_of_the_same_rate_are_equal_and_share_a_hash_code() {
        // Arrange
        int     anyKbps = Any.Int32().Between(Bitrate.MinKbps, Bitrate.MaxKbps).Generate();
        Bitrate first   = new Bitrate(anyKbps);
        Bitrate second  = new Bitrate(anyKbps);

        // Assert
        Assert.True(first.Equals(second));
        Assert.True(first.Equals((object)second));
        Assert.Equal(first.GetHashCode(), second.GetHashCode());
    }

    [Fact]
    public void A_bitrate_is_not_equal_to_a_different_rate_nor_to_null() {
        // Arrange
        Bitrate bitrate = new Bitrate(1_000);

        // Assert
        Assert.False(bitrate.Equals(new Bitrate(1_001)));
        Assert.False(bitrate.Equals(null));
        Assert.False(bitrate.Equals((object?)"1000"));
    }

    [Fact]
    public void Bitrates_are_ordered_by_their_rate_and_anything_follows_null() {
        // Arrange
        Bitrate low  = new Bitrate(500);
        Bitrate high = new Bitrate(2_000);

        // Assert
        Assert.True(low.CompareTo(high) < 0);
        Assert.True(high.CompareTo(low) > 0);
        Assert.Equal(0, low.CompareTo(new Bitrate(500)));
        Assert.True(low.CompareTo(null) > 0);
    }

    [Fact]
    public void A_bitrate_prints_its_rate_in_kilobits_per_second() {
        Assert.Equal("2500 kbps", new Bitrate(2_500).ToString());
    }

}
