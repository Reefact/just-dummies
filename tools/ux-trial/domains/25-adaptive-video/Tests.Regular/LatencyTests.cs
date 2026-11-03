using JustDummies;

namespace AdaptiveVideo.Domain.Tests;

public sealed class LatencyTests {

    [Theory]
    [InlineData(0)]
    [InlineData(5_000)]
    public void A_latency_accepts_both_ends_of_its_range(int milliseconds) {
        Assert.Equal(milliseconds, new Latency(milliseconds).RoundTripMilliseconds);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(5_001)]
    public void A_latency_outside_the_range_is_refused(int milliseconds) {
        Assert.Throws<ArgumentOutOfRangeException>("roundTripMilliseconds", () => new Latency(milliseconds));
    }

    [Fact]
    public void A_latency_of_250_milliseconds_lasts_a_quarter_of_a_second() {
        // Arrange
        Latency latency = new AnyLatency().WithRoundTripMilliseconds(250).Generate();

        // Assert
        Assert.Equal(0.25, latency.Seconds);
    }

    [Fact]
    public void A_latency_below_zero_is_refused_whatever_it_is() {
        // Arrange
        int anyNegative = Any.Int32().Negative().Generate();

        // Act / Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => new Latency(anyNegative));
    }

    [Fact]
    public void A_latency_above_five_seconds_is_refused_whatever_it_is() {
        // Arrange
        int anyTooLong = Any.Int32().GreaterThan(Latency.MaxMilliseconds).Generate();

        // Act / Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => new Latency(anyTooLong));
    }

}
