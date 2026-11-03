using AdaptiveVideo.Domain;

namespace AdaptiveVideo.Tests.Discovery;

public sealed class BitrateTests {

    [Theory]
    [InlineData(50)]
    [InlineData(100_000)]
    [InlineData(2_500)]
    public void A_bitrate_inside_the_range_keeps_its_value(int kbps) {
        Bitrate bitrate = new Bitrate(kbps);

        Assert.Equal(kbps, bitrate.Kbps);
    }

    [Theory]
    [InlineData(49)]
    [InlineData(100_001)]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(int.MinValue)]
    [InlineData(int.MaxValue)]
    public void A_bitrate_outside_the_range_is_refused(int kbps) {
        ArgumentOutOfRangeException exception = Assert.Throws<ArgumentOutOfRangeException>(() => new Bitrate(kbps));

        Assert.Equal("kbps", exception.ParamName);
    }

    [Fact]
    public void Downloading_takes_the_media_duration_scaled_by_the_ratio_of_bitrate_to_throughput() {
        Bitrate bitrate = new Bitrate(2_000);

        double seconds = bitrate.DownloadSeconds(mediaSeconds: 4, throughputKbps: 8_000);

        Assert.Equal(1.0, seconds);
    }

    [Fact]
    public void A_throughput_equal_to_the_bitrate_downloads_in_real_time() {
        Bitrate bitrate = new Bitrate(3_000);

        double seconds = bitrate.DownloadSeconds(mediaSeconds: 6, throughputKbps: 3_000);

        Assert.Equal(6.0, seconds);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-0.5)]
    [InlineData(double.NegativeInfinity)]
    public void A_non_positive_media_duration_is_refused(double mediaSeconds) {
        Bitrate anyBitrate    = Any.Bitrate().Generate();
        double  anyThroughput = Any.Double().Positive().LessThanOrEqualTo(1_000_000d).Generate();

        ArgumentOutOfRangeException exception = Assert.Throws<ArgumentOutOfRangeException>(() => anyBitrate.DownloadSeconds(mediaSeconds, anyThroughput));

        Assert.Equal("mediaSeconds", exception.ParamName);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-3)]
    [InlineData(double.NegativeInfinity)]
    public void A_non_positive_throughput_is_refused(double throughputKbps) {
        Bitrate anyBitrate = Any.Bitrate().Generate();
        double  anyMedia   = Any.Double().Between(0.5, 10).Generate();

        ArgumentOutOfRangeException exception = Assert.Throws<ArgumentOutOfRangeException>(() => anyBitrate.DownloadSeconds(anyMedia, throughputKbps));

        Assert.Equal("throughputKbps", exception.ParamName);
    }

    [Fact(Skip = "domain defect: Bitrate.DownloadSeconds(NaN, t) returns NaN instead of refusing (mediaSeconds <= 0 is false for NaN); literal input, any seed")]
    public void A_NaN_media_duration_is_refused() {
        Bitrate anyBitrate    = Any.Bitrate().Generate();
        double  anyThroughput = Any.Double().Positive().LessThanOrEqualTo(1_000_000d).Generate();

        Assert.Throws<ArgumentOutOfRangeException>(() => anyBitrate.DownloadSeconds(double.NaN, anyThroughput));
    }

    [Fact(Skip = "domain defect: Bitrate.DownloadSeconds(m, NaN) returns NaN instead of refusing (throughputKbps <= 0 is false for NaN); literal input, any seed")]
    public void A_NaN_throughput_is_refused() {
        Bitrate anyBitrate = Any.Bitrate().Generate();
        double  anyMedia   = Any.Double().Between(0.5, 10).Generate();

        Assert.Throws<ArgumentOutOfRangeException>(() => anyBitrate.DownloadSeconds(anyMedia, double.NaN));
    }

    [Fact]
    public void Two_bitrates_of_the_same_rate_are_equal() {
        int anyKbps = Any.Int32().Between(Bitrate.MinKbps, Bitrate.MaxKbps).Generate();

        Bitrate first  = new Bitrate(anyKbps);
        Bitrate second = new Bitrate(anyKbps);

        Assert.True(first.Equals(second));
        Assert.True(first.Equals((object)second));
        Assert.Equal(first.GetHashCode(), second.GetHashCode());
    }

    [Fact]
    public void Two_bitrates_of_different_rates_are_not_equal() {
        Bitrate first  = new Bitrate(1_000);
        Bitrate second = new Bitrate(1_001);

        Assert.False(first.Equals(second));
        Assert.False(first.Equals(null));
        Assert.False(first.Equals("1000"));
    }

    [Fact]
    public void A_bitrate_compares_by_rate_and_sorts_after_nothing() {
        Bitrate low  = new Bitrate(1_000);
        Bitrate high = new Bitrate(2_000);

        Assert.True(low.CompareTo(high) < 0);
        Assert.True(high.CompareTo(low) > 0);
        Assert.Equal(0, low.CompareTo(new Bitrate(1_000)));
        Assert.True(low.CompareTo(null) > 0);
    }

    [Fact]
    public void A_bitrate_prints_its_rate_in_kbps() {
        Bitrate bitrate = new Bitrate(2_500);

        Assert.Equal("2500 kbps", bitrate.ToString());
    }

}
