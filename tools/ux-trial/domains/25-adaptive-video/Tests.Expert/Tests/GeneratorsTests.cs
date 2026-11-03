using AdaptiveVideo.Domain;

using JustDummies;

using Xunit;

namespace AdaptiveVideo.Tests.Expert;

// The generators written by hand carry rules no constructor-by-constructor scaffold could: these tests keep them honest.
public sealed class GeneratorsTests {

    private const int Draws = 300;

    [Fact]
    public void Every_generator_draws_values_its_own_constructor_accepts() {
        // Act
        for (int draw = 0; draw < Draws; draw++) {
            Any.Bitrate().Generate();
            Any.EncodingVariant().Generate();
            Any.Segment().Generate();
            Any.Latency().Generate();
            Any.BandwidthEstimate().Generate();
            Any.AdaptationPolicy().Generate();
            Any.BitrateLadder().Generate();
            Any.PlaybackBuffer().Generate();
            Any.PlaybackSession().Generate();
        }
    }

    [Theory]
    [InlineData(AdaptationReason.Startup)]
    [InlineData(AdaptationReason.Hold)]
    [InlineData(AdaptationReason.SwitchUp)]
    [InlineData(AdaptationReason.SwitchDown)]
    [InlineData(AdaptationReason.Panic)]
    public void A_decision_is_drawn_for_any_reason_it_can_be_given(AdaptationReason reason) {
        // Act
        for (int draw = 0; draw < Draws; draw++) {
            AdaptationDecision decision = Any.AdaptationDecision().WithReason(reason).Generate();

            Assert.Equal(reason, decision.Reason);
        }
    }

    [Fact]
    public void A_ladder_is_drawn_with_the_bitrates_a_test_asked_for_in_ascending_order() {
        // Act
        BitrateLadder ladder = Any.BitrateLadder().WithBitratesKbps(6_000, 500, 3_000).Generate();

        // Assert
        Assert.Equal(new[] { 500, 3_000, 6_000 }, ladder.Variants.Select(v => v.Bitrate.Kbps));
    }

    [Fact]
    public void A_session_draws_its_thresholds_and_segment_within_the_buffer_it_drew() {
        // Act
        for (int draw = 0; draw < Draws; draw++) {
            PlaybackSession session = Any.PlaybackSession().Generate();

            Assert.True(session.Policy.ComfortBufferSeconds <= session.Buffer.CapacitySeconds);
            Assert.True(session.NextSegment.DurationSeconds <= session.Buffer.CapacitySeconds);
        }
    }

    [Fact]
    public void A_seed_replays_the_hand_written_generators_value_for_value() {
        // Arrange
        int seed = Any.Int32().Generate();

        // Act
        string first  = DescribeWhatIsDrawnUnder(seed);
        string second = DescribeWhatIsDrawnUnder(seed);

        // Assert
        Assert.Equal(first, second);
    }

    private static string DescribeWhatIsDrawnUnder(int seed) {
        string description = string.Empty;

        Any.Reproducibly(seed, () => {
            BitrateLadder   ladder   = Any.BitrateLadder().Generate();
            PlaybackSession session  = Any.PlaybackSession().Generate();
            AdaptationDecision decision = Any.AdaptationDecision().Generate();

            description = string.Join(",", ladder.Variants.Select(v => $"{v.Id}/{v.Width}/{v.Bitrate.Kbps}"))
                          + $"|{session.Policy.PanicBufferSeconds}/{session.Policy.ComfortBufferSeconds}/{session.Buffer.LevelSeconds}/{session.Buffer.CapacitySeconds}/{session.NextSegment.DurationSeconds}/{session.Ladder.Variants.Count}"
                          + $"|{decision.Reason}/{decision.Previous.Id}/{decision.Chosen.Id}/{decision.PredictedDownloadSeconds}";
        });

        return description;
    }

}
