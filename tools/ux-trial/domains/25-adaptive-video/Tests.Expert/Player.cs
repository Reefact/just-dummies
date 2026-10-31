using AdaptiveVideo.Domain;

using JustDummies;
using JustDummies.Xunit;

// Every test case of this suite draws under its own seed, reported when (and only when) it fails.
[assembly: Reproducible]

namespace AdaptiveVideo.Tests.Expert;

/// <summary>
///     Recipes for the adaptation feedback loop. Only what the decisions are about is pinned:
///     the rung bitrates, the thresholds and the share of the estimate the policy spends. Rung
///     resolutions and frame rates, buffer capacity, segment duration and latency stay arbitrary.
/// </summary>
internal static class Player {

    public const double PanicSeconds   = 5;
    public const double ComfortSeconds = 15;

    /// <summary>A ladder whose bitrates are 400, 1000, 2500 and 5000 kbps; every other trait of a rung is drawn.</summary>
    public static AnyBitrateLadder Ladder => Any.BitrateLadder().WithBitrates(400, 1_000, 2_500, 5_000);

    /// <summary>A policy that spends the whole estimate, panics below 5 s and climbs from 15 s.</summary>
    public static AnyAdaptationPolicy Policy => Any.AdaptationPolicy()
                                                   .WithSafetyFactor(1)
                                                   .WithPanicBufferSeconds(PanicSeconds)
                                                   .WithComfortBufferSeconds(ComfortSeconds);

    /// <summary>
    ///     An estimate the policy already trusts (three samples or more) and that follows the last sample
    ///     exactly (smoothing factor 1), so a test steers the budget through the downloads it completes.
    /// </summary>
    public static AnyBandwidthEstimate TrustedEstimate => Any.BandwidthEstimate()
                                                             .WithSampleCount(c => c.GreaterThanOrEqualTo(3))
                                                             .WithSmoothingFactor(1);

    /// <summary>A session on <see cref="Ladder" /> with <see cref="Policy" />, before its first decision.</summary>
    public static AnyPlaybackSession NewSession => Any.PlaybackSession()
                                                      .WithLadder(_ => Ladder)
                                                      .WithPolicy(_ => Policy);

    /// <summary>
    ///     A session past its startup decision, now playing the 1000 kbps rung, whose estimate stands at
    ///     <paramref name="estimateKbps" /> and whose buffer holds <paramref name="bufferSeconds" />.
    /// </summary>
    public static PlaybackSession PlayingSecondRung(double estimateKbps, double bufferSeconds) {
        PlaybackSession session = NewSession
                                  // Room to spare: whatever the first download costs, the buffer can still be drained to the level asked.
                                  .WithBuffer(b => b.WithLevelSeconds(30))
                                  // Enough for the startup decision to take its one rung up, and no more is asked of it.
                                  .WithBandwidth(_ => TrustedEstimate.WithThroughputKbps(t => t.GreaterThanOrEqualTo(1_000)))
                                  .Generate();
        Download(session, observedKbps: estimateKbps);
        DrainTo(session, bufferSeconds);

        return session;
    }

    /// <summary>Decides the next segment and completes its download at the given observed throughput.</summary>
    public static AdaptationDecision Download(PlaybackSession session, double observedKbps) {
        AdaptationDecision decision = session.DecideNext(Any.Latency().Generate());
        session.CompleteDownload(decision.Segment.SizeKilobits(decision.Chosen) / observedKbps);

        return decision;
    }

    /// <summary>Plays without downloading until the buffer holds <paramref name="seconds" />.</summary>
    public static void DrainTo(PlaybackSession session, double seconds) {
        session.Idle(session.Buffer.LevelSeconds - seconds);
    }

}
