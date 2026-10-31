namespace AdaptiveVideo.Domain;

public enum AdaptationReason
{
    Startup,
    Hold,
    SwitchUp,
    SwitchDown,
    Panic,
}

/// <summary>The variant chosen for one segment, and why.</summary>
public sealed class AdaptationDecision
{
    public AdaptationDecision(Segment segment, EncodingVariant previous, EncodingVariant chosen, AdaptationReason reason,
                              double predictedDownloadSeconds)
    {
        ArgumentNullException.ThrowIfNull(segment);
        ArgumentNullException.ThrowIfNull(previous);
        ArgumentNullException.ThrowIfNull(chosen);
        if (!Enum.IsDefined(reason))
        {
            throw new ArgumentOutOfRangeException(nameof(reason), reason, "Unknown adaptation reason.");
        }

        if (double.IsNaN(predictedDownloadSeconds) || predictedDownloadSeconds <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(predictedDownloadSeconds), predictedDownloadSeconds,
                "The predicted download time must be positive.");
        }

        int direction = chosen.Bitrate.CompareTo(previous.Bitrate);
        bool consistent = reason switch
        {
            AdaptationReason.Hold => direction == 0,
            AdaptationReason.SwitchUp => direction > 0,
            AdaptationReason.SwitchDown => direction < 0,
            AdaptationReason.Panic => direction <= 0,
            _ => true,
        };
        if (!consistent)
        {
            throw new ArgumentException($"Reason {reason} contradicts a move from {previous.Id} to {chosen.Id}.", nameof(reason));
        }

        Segment = segment;
        Previous = previous;
        Chosen = chosen;
        Reason = reason;
        PredictedDownloadSeconds = predictedDownloadSeconds;
    }

    public Segment Segment { get; }

    public EncodingVariant Previous { get; }

    public EncodingVariant Chosen { get; }

    public AdaptationReason Reason { get; }

    public double PredictedDownloadSeconds { get; }

    public bool IsSwitch => Chosen.Bitrate.Kbps != Previous.Bitrate.Kbps;
}
