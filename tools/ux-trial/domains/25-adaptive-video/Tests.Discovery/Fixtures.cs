using AdaptiveVideo.Domain;

namespace AdaptiveVideo.Tests.Discovery;

/// <summary>Shared builders. Literals here are the ones the tests are about; everything else is drawn.</summary>
internal static class Fixtures {

    /// <summary>A variant of the given height and bitrate. Its width is arbitrary (and valid for that height).</summary>
    public static EncodingVariant Variant(int height, int kbps, int framesPerSecond = 30) {
        return Any.EncodingVariant()
                  .WithHeight(height)
                  .WithFramesPerSecond(framesPerSecond)
                  .WithBitrate(new Bitrate(kbps))
                  .Generate();
    }

    /// <summary>The ladder most scenarios use: 240p 400, 360p 1000, 480p 2000, 720p 4000, 1080p 8000 kbps.</summary>
    public static BitrateLadder FiveRungLadder() {
        return new BitrateLadder([Variant(240, 400), Variant(360, 1_000), Variant(480, 2_000), Variant(720, 4_000), Variant(1080, 8_000)]);
    }

    /// <summary>
    ///     A session on <see cref="FiveRungLadder" /> with 4 s segments, a 100 s buffer holding 70 s, and an estimate that
    ///     replaces itself by each observation (smoothing 1).
    /// </summary>
    public static PlaybackSession NewSession(double throughputKbps, double safetyFactor, int sampleCount = 5, double panicSeconds = 8,
                                             double comfortSeconds = 30, double levelSeconds = 70) {
        return Any.PlaybackSession()
                  .WithLadder(FiveRungLadder())
                  .WithBuffer(new PlaybackBuffer(levelSeconds, 100))
                  .WithPolicy(new AdaptationPolicy(safetyFactor, panicSeconds, comfortSeconds))
                  .WithBandwidth(new BandwidthEstimate(throughputKbps, sampleCount, 1))
                  .WithSegmentDurationSeconds(4)
                  .Generate();
    }

    /// <summary>
    ///     Runs <paramref name="cycles" /> decide/complete rounds, each download taking exactly what the segment needs at
    ///     <paramref name="throughputKbps" />, so the estimate stays where it started.
    /// </summary>
    public static PlaybackSession Climbed(int cycles, double throughputKbps = 16_000, double safetyFactor = 0.7, double panicSeconds = 8,
                                          double comfortSeconds = 30, int sampleCount = 5) {
        PlaybackSession session = NewSession(throughputKbps, safetyFactor, sampleCount, panicSeconds, comfortSeconds);
        for (int i = 0; i < cycles; i++) {
            AdaptationDecision decision = session.DecideNext(Any.Latency().Generate());
            session.CompleteDownload(decision.Segment.SizeKilobits(decision.Chosen) / throughputKbps);
        }

        return session;
    }

    /// <summary>Plays without downloading until the buffer holds exactly <paramref name="levelSeconds" />.</summary>
    public static void DrainTo(PlaybackSession session, double levelSeconds) {
        session.Idle(session.Buffer.LevelSeconds - levelSeconds);
    }

}
