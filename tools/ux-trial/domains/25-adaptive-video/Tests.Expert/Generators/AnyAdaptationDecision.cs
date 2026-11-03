// Scaffolded by `any` (JustDummies), then rewritten by hand: the constructor ties the reason to the move
// between the two variants (a Hold keeps the bitrate, a SwitchUp raises it...), a rule that no
// per-parameter recipe can honour. The reason is drawn first, then both variants are drawn to agree with it.

using System;

using JustDummies;

namespace AdaptiveVideo.Domain;

/// <summary>A generator of arbitrary <see cref="AdaptationDecision" /> values, whose reason agrees with its move.</summary>
public sealed partial class AnyAdaptationDecision : IAny<AdaptationDecision> {

    private readonly IAny<Segment>          _segment;
    private readonly EncodingVariant?       _previous;
    private readonly EncodingVariant?       _chosen;
    private readonly IAny<AdaptationReason> _reason;
    private readonly IAny<double>           _predictedDownloadSeconds;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyAdaptationDecision()
        : this(new AnySegment(), null, null, Any.Enum<AdaptationReason>(), Any.Double().Positive()) { }

    private AnyAdaptationDecision(IAny<Segment> segment, EncodingVariant? previous, EncodingVariant? chosen,
                                  IAny<AdaptationReason> reason, IAny<double> predictedDownloadSeconds) {
        _segment                  = segment;
        _previous                 = previous;
        _chosen                   = chosen;
        _reason                   = reason;
        _predictedDownloadSeconds = predictedDownloadSeconds;
    }

    /// <summary>Pins the segment.</summary>
    public AnyAdaptationDecision WithSegment(Segment value) {
        return new AnyAdaptationDecision(Any.Fixed(value), _previous, _chosen, _reason, _predictedDownloadSeconds);
    }

    /// <summary>Pins the variant playback was on.</summary>
    public AnyAdaptationDecision WithPrevious(EncodingVariant value) {
        return new AnyAdaptationDecision(_segment, value, _chosen, _reason, _predictedDownloadSeconds);
    }

    /// <summary>Pins the variant chosen.</summary>
    public AnyAdaptationDecision WithChosen(EncodingVariant value) {
        return new AnyAdaptationDecision(_segment, _previous, value, _reason, _predictedDownloadSeconds);
    }

    /// <summary>Pins the reason; the two variants are drawn to agree with it unless they are pinned too.</summary>
    public AnyAdaptationDecision WithReason(AdaptationReason value) {
        return new AnyAdaptationDecision(_segment, _previous, _chosen, Any.Fixed(value), _predictedDownloadSeconds);
    }

    /// <summary>Pins the predicted download time.</summary>
    public AnyAdaptationDecision WithPredictedDownloadSeconds(double value) {
        return new AnyAdaptationDecision(_segment, _previous, _chosen, _reason, Any.Fixed(value));
    }

    /// <summary>Produces one arbitrary <see cref="AdaptationDecision" />.</summary>
    public AdaptationDecision Generate() {
        AdaptationReason reason = _reason.Generate();

        // A SwitchUp needs a bitrate above the previous one, a SwitchDown one below it.
        EncodingVariant previous = _previous
                                   ?? new AnyEncodingVariant()
                                       .WithBitrate(b => b.WithKbps(k => reason switch {
                                           AdaptationReason.SwitchUp   => k.LessThan(Bitrate.MaxKbps),
                                           AdaptationReason.SwitchDown => k.GreaterThan(Bitrate.MinKbps),
                                           _                           => k
                                       }))
                                       .Generate();
        int from = previous.Bitrate.Kbps;

        EncodingVariant chosen = _chosen
                                 ?? new AnyEncodingVariant()
                                     .WithBitrate(b => b.WithKbps(k => reason switch {
                                         AdaptationReason.Hold       => k.Between(from, from),
                                         AdaptationReason.SwitchUp   => k.GreaterThan(from),
                                         AdaptationReason.SwitchDown => k.LessThan(from),
                                         AdaptationReason.Panic      => k.LessThanOrEqualTo(from),
                                         _                           => k
                                     }))
                                     .Generate();

        return new AdaptationDecision(_segment.Generate(), previous, chosen, reason, _predictedDownloadSeconds.Generate());
    }

}
