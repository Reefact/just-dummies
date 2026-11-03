using AdaptiveVideo.Domain;

namespace AdaptiveVideo.Tests.Discovery;

public sealed class LatencyTests {

    [Theory]
    [InlineData(0)]
    [InlineData(5_000)]
    [InlineData(120)]
    public void A_latency_inside_the_range_keeps_its_milliseconds(int milliseconds) {
        Latency latency = new Latency(milliseconds);

        Assert.Equal(milliseconds, latency.RoundTripMilliseconds);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(5_001)]
    [InlineData(int.MinValue)]
    [InlineData(int.MaxValue)]
    public void A_latency_outside_the_range_is_refused(int milliseconds) {
        ArgumentOutOfRangeException exception = Assert.Throws<ArgumentOutOfRangeException>(() => new Latency(milliseconds));

        Assert.Equal("roundTripMilliseconds", exception.ParamName);
    }

    [Theory]
    [InlineData(0, 0.0)]
    [InlineData(250, 0.25)]
    [InlineData(1_500, 1.5)]
    [InlineData(5_000, 5.0)]
    public void A_latency_expresses_its_milliseconds_in_seconds(int milliseconds, double expectedSeconds) {
        Latency latency = new Latency(milliseconds);

        Assert.Equal(expectedSeconds, latency.Seconds);
    }

}
