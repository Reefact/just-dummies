# Adaptive Video Playback Control — model

Namespace `AdaptiveVideo.Domain`. Choose the variant of each next segment so playback stays smooth.

## Value objects
- `Bitrate(int kbps)` — 50..100 000 kbps. `DownloadSeconds(mediaSeconds, throughputKbps)` = kbps x seconds / throughput (both > 0).
- `EncodingVariant(string id, int width, int height, Bitrate bitrate)` — id matches `^\d{3,4}p(24|25|30|50|60)$` (FormatException);
  width 128..7680, height 96..4320, both even, height <= width, and the id's height equals `height`. Exposes `FramesPerSecond`.
- `Segment(int index, double durationSeconds)` — index >= 0, duration 0.5..10 s. `SizeKilobits(variant)`, `Next()`.
- `Latency(int roundTripMilliseconds)` — 0..5 000 ms.
- `BandwidthEstimate(double throughputKbps, int sampleCount, double smoothingFactor)` — throughput in (0, 1 000 000],
  samples >= 1, smoothing in (0, 1]. `WithSample(kilobits, seconds)` returns a new EWMA estimate; `IsWarmingUp` below 3 samples.
- `AdaptationPolicy(double safetyFactor, double panicBufferSeconds, double comfortBufferSeconds)` — safety 0.1..1,
  panic >= 0, comfort > panic.
- `AdaptationDecision(Segment, EncodingVariant previous, EncodingVariant chosen, AdaptationReason, double predictedDownloadSeconds)` —
  prediction > 0; the reason must agree with the move (Hold = same bitrate, SwitchUp = higher, SwitchDown = lower,
  Panic = not higher; Startup = any). `AdaptationReason`: Startup, Hold, SwitchUp, SwitchDown, Panic.

## Entities / aggregates
- `BitrateLadder(IEnumerable<EncodingVariant>)` — non-empty, no null, unique ids, distinct bitrates; sorted by bitrate and
  resolution never decreases as bitrate rises. `Lowest`, `Highest`, `Find(id)` (KeyNotFoundException), `HighestWithin(budget)`,
  `StepUp`/`StepDown` (clamped), `IndexOf` (ArgumentException for a foreign variant).
- `PlaybackBuffer(double levelSeconds, double capacitySeconds)` — capacity 4..300 s, 0 <= level <= capacity.
  `Drain(seconds)` plays and returns the stall time when it runs dry; `Append(segment)` throws InvalidOperationException if it does not fit.
- `PlaybackSession(BitrateLadder, PlaybackBuffer, AdaptationPolicy, BandwidthEstimate, double segmentDurationSeconds)` —
  comfort threshold <= buffer capacity, segment <= capacity; starts on the ladder's lowest variant at segment 0.

## Behaviour (the feedback loop)
- `DecideNext(Latency)`: budget = throughput x safety factor. First decision or warming-up estimate: highest variant within
  budget but at most one rung up (Startup on the first). Buffer below panic: drop to `Lowest` (Panic). Otherwise: drop at once to
  the sustainable variant if lower, climb one rung only when the buffer is at or above comfort, else hold.
  Prediction = latency + download time. Refuses (InvalidOperationException) while a download is pending or the buffer is full.
- `CompleteDownload(elapsedSeconds)` (0..600 s): drains the buffer for the elapsed time (counting a stall if it ran dry),
  appends the segment, feeds the observed throughput to the estimate, adopts the chosen variant, advances the segment.
  Refuses when nothing is pending. A higher variant takes longer to download, drains more buffer, and raises later stall risk.
- `Idle(seconds)`: plays without downloading; stalls are counted. `StallCount`, `TotalStallSeconds`, `History` expose the outcome.
