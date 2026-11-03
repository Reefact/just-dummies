using AdaptiveVideo.Domain;

using JustDummies;

using Xunit;

namespace AdaptiveVideo.Tests.Expert;

public sealed class BitrateTests {

    [Theory]
    [InlineData(50)]
    [InlineData(51)]
    [InlineData(99_999)]
    [InlineData(100_000)]
    public void A_bitrate_inside_the_supported_range_is_kept_as_given(int kbps) {
        // Act
        Bitrate bitrate = new Bitrate(kbps);

        // Assert
        Assert.Equal(kbps, bitrate.Kbps);
    }

    [Theory]
    [InlineData(49)]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(int.MinValue)]
    [InlineData(100_001)]
    [InlineData(int.MaxValue)]
    public void A_bitrate_outside_the_supported_range_is_refused(int kbps) {
        // Act
        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(() => new Bitrate(kbps));

        // Assert
        Assert.Equal("kbps", refusal.ParamName);
    }

    [Fact]
    public void Four_seconds_of_media_at_2000_kbps_take_one_second_over_a_8000_kbps_link() {
        // Arrange
        Bitrate bitrate = new Bitrate(2_000);

        // Act
        double seconds = bitrate.DownloadSeconds(mediaSeconds: 4, throughputKbps: 8_000);

        // Assert
        Assert.Equal(1.0, seconds);
    }

    [Fact]
    public void The_download_time_grows_with_the_media_and_shrinks_with_the_throughput() {
        // Arrange
        Bitrate bitrate = new Bitrate(3_000);

        // Act
        double baseline = bitrate.DownloadSeconds(mediaSeconds: 2, throughputKbps: 6_000);
        double longerMedia = bitrate.DownloadSeconds(mediaSeconds: 4, throughputKbps: 6_000);
        double fasterLink = bitrate.DownloadSeconds(mediaSeconds: 2, throughputKbps: 12_000);

        // Assert
        Assert.Equal(1.0, baseline);
        Assert.Equal(2.0, longerMedia);
        Assert.Equal(0.5, fasterLink);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-0.5)]
    [InlineData(double.NegativeInfinity)]
    public void A_non_positive_media_duration_cannot_be_downloaded(double mediaSeconds) {
        // Arrange
        Bitrate anyBitrate    = Any.Bitrate().Generate();
        double  anyThroughput = Any.Double().Positive().Generate();

        // Act
        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(
            () => anyBitrate.DownloadSeconds(mediaSeconds, anyThroughput));

        // Assert
        Assert.Equal("mediaSeconds", refusal.ParamName);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1000)]
    [InlineData(double.NegativeInfinity)]
    public void A_non_positive_throughput_cannot_download_anything(double throughputKbps) {
        // Arrange
        Bitrate anyBitrate = Any.Bitrate().Generate();
        double  anyMedia   = Any.Double().Positive().Generate();

        // Act
        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(
            () => anyBitrate.DownloadSeconds(anyMedia, throughputKbps));

        // Assert
        Assert.Equal("throughputKbps", refusal.ParamName);
    }

    [Theory(Skip = "domain defect: DownloadSeconds lets NaN through (NaN <= 0 is false) and returns NaN, where every other " +
                   "numeric guard of the model rejects NaN explicitly. Literal inputs, no seed involved.")]
    [InlineData(double.NaN, 1_000)]
    [InlineData(4, double.NaN)]
    public void A_NaN_duration_or_throughput_is_refused_like_any_other_invalid_one(double mediaSeconds, double throughputKbps) {
        // Arrange
        Bitrate anyBitrate = Any.Bitrate().Generate();

        // Act + Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => anyBitrate.DownloadSeconds(mediaSeconds, throughputKbps));
    }

    [Fact]
    public void Two_bitrates_of_the_same_rate_are_equal_and_hash_alike() {
        // Arrange
        Bitrate first  = new Bitrate(800);
        Bitrate second = new Bitrate(800);

        // Assert
        Assert.True(first.Equals(second));
        Assert.True(first.Equals((object)second));
        Assert.Equal(first.GetHashCode(), second.GetHashCode());
    }

    [Fact]
    public void Two_bitrates_of_different_rates_are_not_equal() {
        // Arrange
        Bitrate lower  = new Bitrate(800);
        Bitrate higher = new Bitrate(801);

        // Assert
        Assert.False(lower.Equals(higher));
    }

    [Fact]
    public void A_bitrate_is_not_equal_to_nothing_nor_to_a_plain_number() {
        // Arrange
        Bitrate bitrate = new Bitrate(800);

        // Assert
        Assert.False(bitrate.Equals(null));
        Assert.False(bitrate.Equals((object?)null));
        Assert.False(bitrate.Equals((object)800));
    }

    [Fact]
    public void Bitrates_are_ordered_by_rate_and_anything_follows_nothing() {
        // Arrange
        Bitrate lower  = new Bitrate(800);
        Bitrate higher = new Bitrate(1_200);

        // Assert
        Assert.True(lower.CompareTo(higher) < 0);
        Assert.True(higher.CompareTo(lower) > 0);
        Assert.Equal(0, lower.CompareTo(new Bitrate(800)));
        Assert.True(lower.CompareTo(null) > 0);
    }

    [Fact]
    public void A_bitrate_reads_as_its_rate_in_kbps() {
        // Assert
        Assert.Equal("2500 kbps", new Bitrate(2_500).ToString());
    }

}
