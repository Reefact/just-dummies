using AdaptiveVideo.Domain;

using JustDummies;

using Xunit;

namespace AdaptiveVideo.Tests.Expert;

public sealed class AdaptationDecisionTests {

    [Fact]
    public void A_decision_exposes_what_it_was_built_from() {
        // Arrange
        Segment         segment  = new Segment(12, 4);
        EncodingVariant previous = Variants.Of(height: 480, framesPerSecond: 30, kbps: 1_000);
        EncodingVariant chosen   = Variants.Of(height: 720, framesPerSecond: 30, kbps: 2_500);

        // Act
        AdaptationDecision decision = new AdaptationDecision(segment, previous, chosen, AdaptationReason.SwitchUp, 1.25);

        // Assert
        Assert.Same(segment, decision.Segment);
        Assert.Same(previous, decision.Previous);
        Assert.Same(chosen, decision.Chosen);
        Assert.Equal(AdaptationReason.SwitchUp, decision.Reason);
        Assert.Equal(1.25, decision.PredictedDownloadSeconds);
    }

    [Theory]
    [InlineData(AdaptationReason.Hold, 1_000, 1_000)]
    [InlineData(AdaptationReason.SwitchUp, 1_000, 2_000)]
    [InlineData(AdaptationReason.SwitchDown, 2_000, 1_000)]
    [InlineData(AdaptationReason.Panic, 2_000, 1_000)]
    [InlineData(AdaptationReason.Panic, 1_000, 1_000)]
    [InlineData(AdaptationReason.Startup, 1_000, 2_000)]
    [InlineData(AdaptationReason.Startup, 1_000, 1_000)]
    [InlineData(AdaptationReason.Startup, 2_000, 1_000)]
    public void A_reason_that_agrees_with_the_move_is_accepted(AdaptationReason reason, int previousKbps, int chosenKbps) {
        // Arrange
        Segment         anySegment  = Any.Segment().Generate();
        EncodingVariant previous    = Any.EncodingVariant().WithBitrateKbps(previousKbps).Generate();
        EncodingVariant chosen      = Any.EncodingVariant().WithBitrateKbps(chosenKbps).Generate();
        double          anyDuration = Any.Double().Positive().Generate();

        // Act
        AdaptationDecision decision = new AdaptationDecision(anySegment, previous, chosen, reason, anyDuration);

        // Assert
        Assert.Equal(reason, decision.Reason);
    }

    [Theory]
    [InlineData(AdaptationReason.Hold, 1_000, 2_000)]
    [InlineData(AdaptationReason.Hold, 2_000, 1_000)]
    [InlineData(AdaptationReason.SwitchUp, 1_000, 1_000)]
    [InlineData(AdaptationReason.SwitchUp, 2_000, 1_000)]
    [InlineData(AdaptationReason.SwitchDown, 1_000, 1_000)]
    [InlineData(AdaptationReason.SwitchDown, 1_000, 2_000)]
    [InlineData(AdaptationReason.Panic, 1_000, 2_000)]
    public void A_reason_that_contradicts_the_move_is_refused(AdaptationReason reason, int previousKbps, int chosenKbps) {
        // Arrange
        Segment         anySegment  = Any.Segment().Generate();
        EncodingVariant previous    = Any.EncodingVariant().WithBitrateKbps(previousKbps).Generate();
        EncodingVariant chosen      = Any.EncodingVariant().WithBitrateKbps(chosenKbps).Generate();
        double          anyDuration = Any.Double().Positive().Generate();

        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(
            () => new AdaptationDecision(anySegment, previous, chosen, reason, anyDuration));

        // Assert
        Assert.Equal("reason", refusal.ParamName);
    }

    [Fact]
    public void The_refusal_of_a_contradicting_reason_names_the_reason_and_both_variants() {
        // Arrange
        Segment         anySegment = Any.Segment().Generate();
        EncodingVariant previous   = Variants.Of(height: 480, framesPerSecond: 30, kbps: 1_000);
        EncodingVariant chosen     = Variants.Of(height: 720, framesPerSecond: 30, kbps: 2_500);

        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(
            () => new AdaptationDecision(anySegment, previous, chosen, AdaptationReason.SwitchDown, 1));

        // Assert
        Assert.Contains("SwitchDown", refusal.Message);
        Assert.Contains("480p30", refusal.Message);
        Assert.Contains("720p30", refusal.Message);
    }

    [Fact]
    public void A_hold_may_change_the_variant_as_long_as_the_bitrate_stays() {
        // Arrange
        EncodingVariant previous = Variants.Of(height: 720, framesPerSecond: 30, kbps: 3_000);
        EncodingVariant chosen   = Variants.Of(height: 720, framesPerSecond: 60, kbps: 3_000);

        // Act
        AdaptationDecision decision = Any.AdaptationDecision().WithPrevious(previous).WithChosen(chosen).WithReason(AdaptationReason.Hold).Generate();

        // Assert
        Assert.NotEqual(decision.Previous.Id, decision.Chosen.Id);
        Assert.False(decision.IsSwitch);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-0.001)]
    [InlineData(double.NegativeInfinity)]
    [InlineData(double.NaN)]
    public void A_prediction_that_is_not_a_positive_time_is_refused(double predictedDownloadSeconds) {
        // Arrange
        AdaptationDecision anyDecision = Any.AdaptationDecision().Generate();

        // Act
        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(
            () => new AdaptationDecision(anyDecision.Segment, anyDecision.Previous, anyDecision.Chosen, anyDecision.Reason, predictedDownloadSeconds));

        // Assert
        Assert.Equal("predictedDownloadSeconds", refusal.ParamName);
    }

    [Fact]
    public void An_unknown_reason_is_refused() {
        // Arrange
        AdaptationDecision anyDecision = Any.AdaptationDecision().Generate();

        // Act
        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(
            () => new AdaptationDecision(anyDecision.Segment, anyDecision.Previous, anyDecision.Chosen, (AdaptationReason)42, anyDecision.PredictedDownloadSeconds));

        // Assert
        Assert.Equal("reason", refusal.ParamName);
    }

    [Fact]
    public void A_decision_needs_a_segment_and_both_variants() {
        // Arrange
        AdaptationDecision anyDecision = Any.AdaptationDecision().WithReason(AdaptationReason.Startup).Generate();

        // Act
        ArgumentNullException withoutSegment  = Assert.Throws<ArgumentNullException>(() => new AdaptationDecision(null!, anyDecision.Previous, anyDecision.Chosen, anyDecision.Reason, anyDecision.PredictedDownloadSeconds));
        ArgumentNullException withoutPrevious = Assert.Throws<ArgumentNullException>(() => new AdaptationDecision(anyDecision.Segment, null!, anyDecision.Chosen, anyDecision.Reason, anyDecision.PredictedDownloadSeconds));
        ArgumentNullException withoutChosen   = Assert.Throws<ArgumentNullException>(() => new AdaptationDecision(anyDecision.Segment, anyDecision.Previous, null!, anyDecision.Reason, anyDecision.PredictedDownloadSeconds));

        // Assert
        Assert.Equal("segment", withoutSegment.ParamName);
        Assert.Equal("previous", withoutPrevious.ParamName);
        Assert.Equal("chosen", withoutChosen.ParamName);
    }

    [Theory]
    [InlineData(AdaptationReason.SwitchUp)]
    [InlineData(AdaptationReason.SwitchDown)]
    public void A_switch_up_or_down_is_a_switch(AdaptationReason reason) {
        // Arrange
        AdaptationDecision decision = Any.AdaptationDecision().WithReason(reason).Generate();

        // Act + Assert
        Assert.True(decision.IsSwitch);
    }

    [Fact]
    public void A_hold_is_not_a_switch() {
        // Arrange
        AdaptationDecision decision = Any.AdaptationDecision().WithReason(AdaptationReason.Hold).Generate();

        // Act + Assert
        Assert.False(decision.IsSwitch);
    }

}
