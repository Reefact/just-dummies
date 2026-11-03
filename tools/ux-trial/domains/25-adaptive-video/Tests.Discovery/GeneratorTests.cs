using AdaptiveVideo.Domain;

namespace AdaptiveVideo.Tests.Discovery;

/// <summary>The generators are ours (scaffolded, then completed by hand): they must only ever draw what the constructors accept.</summary>
public sealed class GeneratorTests {

    private const int Draws = 300;

    [Fact]
    public void Drawn_bitrates_are_all_accepted() {
        List<Bitrate> anyBitrates = Any.ListOf(Any.Bitrate()).WithCount(Draws).Generate();

        Assert.Equal(Draws, anyBitrates.Count);
    }

    [Fact]
    public void Drawn_encoding_variants_are_all_accepted() {
        List<EncodingVariant> anyVariants = Any.ListOf(Any.EncodingVariant()).WithCount(Draws).Generate();

        Assert.Equal(Draws, anyVariants.Count);
    }

    [Fact]
    public void Drawn_segments_are_all_accepted() {
        List<Segment> anySegments = Any.ListOf(Any.Segment()).WithCount(Draws).Generate();

        Assert.Equal(Draws, anySegments.Count);
    }

    [Fact]
    public void Drawn_latencies_are_all_accepted() {
        List<Latency> anyLatencies = Any.ListOf(Any.Latency()).WithCount(Draws).Generate();

        Assert.Equal(Draws, anyLatencies.Count);
    }

    [Fact]
    public void Drawn_bandwidth_estimates_are_all_accepted() {
        List<BandwidthEstimate> anyEstimates = Any.ListOf(Any.BandwidthEstimate()).WithCount(Draws).Generate();

        Assert.Equal(Draws, anyEstimates.Count);
    }

    [Fact]
    public void Drawn_policies_are_all_accepted() {
        List<AdaptationPolicy> anyPolicies = Any.ListOf(Any.AdaptationPolicy()).WithCount(Draws).Generate();

        Assert.Equal(Draws, anyPolicies.Count);
    }

    [Fact]
    public void Drawn_buffers_are_all_accepted() {
        List<PlaybackBuffer> anyBuffers = Any.ListOf(Any.PlaybackBuffer()).WithCount(Draws).Generate();

        Assert.Equal(Draws, anyBuffers.Count);
    }

    [Fact]
    public void Drawn_ladders_are_all_accepted() {
        List<BitrateLadder> anyLadders = Any.ListOf(Any.BitrateLadder()).WithCount(Draws).Generate();

        Assert.Equal(Draws, anyLadders.Count);
    }

    [Fact]
    public void Drawn_sessions_are_all_accepted() {
        List<PlaybackSession> anySessions = Any.ListOf(Any.PlaybackSession()).WithCount(Draws).Generate();

        Assert.Equal(Draws, anySessions.Count);
    }

    [Fact]
    public void Drawn_decisions_are_all_accepted() {
        List<AdaptationDecision> anyDecisions = Any.ListOf(Any.AdaptationDecision()).WithCount(Draws).Generate();

        Assert.Equal(Draws, anyDecisions.Count);
    }

    [Fact]
    public void A_drawn_session_with_room_in_its_buffer_can_take_its_first_decision() {
        // The buffer's capacity is at least 60 s and a segment at most 10 s: a level of at most 50 s always leaves room.
        PlaybackSession anySession = Any.PlaybackSession().WithBuffer(b => b.WithLevelSeconds(l => l.LessThanOrEqualTo(50d))).Generate();
        Latency         anyLatency = Any.Latency().Generate();

        AdaptationDecision decision = anySession.DecideNext(anyLatency);

        Assert.Same(anySession.PendingDecision, decision);
    }

}
