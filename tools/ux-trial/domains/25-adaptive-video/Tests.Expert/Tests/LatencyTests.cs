using AdaptiveVideo.Domain;

using Xunit;

namespace AdaptiveVideo.Tests.Expert;

public sealed class LatencyTests {

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(4_999)]
    [InlineData(5_000)]
    public void A_round_trip_of_up_to_five_seconds_is_kept_as_given(int milliseconds) {
        // Act
        Latency latency = new Latency(milliseconds);

        // Assert
        Assert.Equal(milliseconds, latency.RoundTripMilliseconds);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(int.MinValue)]
    [InlineData(5_001)]
    [InlineData(int.MaxValue)]
    public void A_negative_round_trip_or_one_beyond_five_seconds_is_refused(int milliseconds) {
        // Act
        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(() => new Latency(milliseconds));

        // Assert
        Assert.Equal("roundTripMilliseconds", refusal.ParamName);
    }

    [Theory]
    [InlineData(0, 0.0)]
    [InlineData(250, 0.25)]
    [InlineData(1_500, 1.5)]
    [InlineData(5_000, 5.0)]
    public void A_latency_reads_in_seconds(int milliseconds, double expectedSeconds) {
        // Act
        Latency latency = new Latency(milliseconds);

        // Assert
        Assert.Equal(expectedSeconds, latency.Seconds);
    }

}
