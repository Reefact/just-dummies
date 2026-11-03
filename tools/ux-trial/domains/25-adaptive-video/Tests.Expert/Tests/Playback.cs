using AdaptiveVideo.Domain;

using JustDummies;

using Xunit;

namespace AdaptiveVideo.Tests.Expert;

/// <summary>Steering a session through its feedback loop, so that a test can start from the state it is about.</summary>
internal static class Playback {

    internal static readonly Latency NoLatency = new Latency(0);

    // The ladder the decision tests read: four rungs.
    internal static readonly int[] RungsKbps = { 500, 1_500, 3_000, 6_000 };

    /// <summary>
    ///     A session on a four-rung ladder, whose segments last four seconds, whose estimate follows the last sample
    ///     alone (smoothing factor 1) and sits at <paramref name="throughputKbps" />.
    /// </summary>
    internal static AnyPlaybackSession Recipe(double throughputKbps,
                                              int    sampleCount     = 3,
                                              double safetyFactor    = 1.0,
                                              double panicSeconds    = 5,
                                              double comfortSeconds  = 20,
                                              double levelSeconds    = 100,
                                              double capacitySeconds = 300) {
        return Any.PlaybackSession()
                  .WithLadder(ladder => ladder.WithBitratesKbps(RungsKbps))
                  .WithSegmentDurationSeconds(4)
                  .WithBandwidth(estimate => estimate.WithThroughputKbps(throughputKbps).WithSampleCount(sampleCount).WithSmoothingFactor(1.0))
                  .WithPolicy(policy => policy.WithSafetyFactor(safetyFactor).WithPanicBufferSeconds(panicSeconds).WithComfortBufferSeconds(comfortSeconds))
                  .WithBuffer(buffer => buffer.WithCapacitySeconds(capacitySeconds).WithLevelSeconds(levelSeconds));
    }

    /// <summary>Requests the next segment and has it delivered in <paramref name="elapsedSeconds" />.</summary>
    internal static AdaptationDecision Download(PlaybackSession session, double elapsedSeconds) {
        AdaptationDecision decision = session.DecideNext(NoLatency);
        session.CompleteDownload(elapsedSeconds);

        return decision;
    }

    /// <summary>Lets playback run, without downloading, until the buffer holds exactly <paramref name="levelSeconds" />.</summary>
    internal static void DrainTo(PlaybackSession session, double levelSeconds) {
        session.Idle(session.Buffer.LevelSeconds - levelSeconds);

        Assert.Equal(levelSeconds, session.Buffer.LevelSeconds);
    }

    /// <summary>Downloads on a fast network, half a second per segment, until the session plays the rung at <paramref name="kbps" />.</summary>
    internal static void ClimbTo(PlaybackSession session, int kbps) {
        for (int attempt = 0; attempt < 10 && session.CurrentVariant.Bitrate.Kbps != kbps; attempt++) {
            Download(session, 0.5);
        }

        Assert.Equal(kbps, session.CurrentVariant.Bitrate.Kbps);
    }

}
