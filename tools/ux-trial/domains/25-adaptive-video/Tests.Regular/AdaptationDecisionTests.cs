using JustDummies;

namespace AdaptiveVideo.Domain.Tests;

public sealed class AdaptationDecisionTests {

    private static EncodingVariant Slow    => Fixtures.Variant(height: 480, framesPerSecond: 30, kbps: 1_000);
    private static EncodingVariant SlowToo => Fixtures.Variant(height: 480, framesPerSecond: 60, kbps: 1_000);
    private static EncodingVariant Fast    => Fixtures.Variant(height: 720, framesPerSecond: 30, kbps: 3_000);

    private static AdaptationDecision Decide(EncodingVariant previous, EncodingVariant chosen, AdaptationReason reason) {
        Segment anySegment   = new AnySegment().Generate();
        double  anyPredicted = Any.Double().Positive().Generate();

        return new AdaptationDecision(anySegment, previous, chosen, reason, anyPredicted);
    }

    [Fact]
    public void A_decision_exposes_what_it_was_built_with() {
        // Arrange
        Segment         segment  = new Segment(3, 4.0);
        EncodingVariant previous = Slow;
        EncodingVariant chosen   = Fast;

        // Act
        AdaptationDecision decision = new AdaptationDecision(segment, previous, chosen, AdaptationReason.SwitchUp, 1.5);

        // Assert
        Assert.Same(segment, decision.Segment);
        Assert.Same(previous, decision.Previous);
        Assert.Same(chosen, decision.Chosen);
        Assert.Equal(AdaptationReason.SwitchUp, decision.Reason);
        Assert.Equal(1.5, decision.PredictedDownloadSeconds);
    }

    [Fact]
    public void Holding_means_the_same_bitrate_even_in_another_rendition() {
        // Act
        AdaptationDecision decision = Decide(Slow, SlowToo, AdaptationReason.Hold);

        // Assert
        Assert.False(decision.IsSwitch);
    }

    [Fact]
    public void Switching_up_means_a_higher_bitrate() {
        // Act
        AdaptationDecision decision = Decide(Slow, Fast, AdaptationReason.SwitchUp);

        // Assert
        Assert.True(decision.IsSwitch);
    }

    [Fact]
    public void Switching_down_means_a_lower_bitrate() {
        // Act
        AdaptationDecision decision = Decide(Fast, Slow, AdaptationReason.SwitchDown);

        // Assert
        Assert.True(decision.IsSwitch);
    }

    [Fact]
    public void Panicking_is_accepted_on_a_drop_and_on_a_variant_that_does_not_move() {
        // Act
        AdaptationDecision drop = Decide(Fast, Slow, AdaptationReason.Panic);
        AdaptationDecision stay = Decide(Slow, Slow, AdaptationReason.Panic);

        // Assert
        Assert.True(drop.IsSwitch);
        Assert.False(stay.IsSwitch);
    }

    [Theory]
    [InlineData(AdaptationReason.Hold, 1, 3)]
    [InlineData(AdaptationReason.Hold, 3, 1)]
    [InlineData(AdaptationReason.SwitchUp, 1, 1)]
    [InlineData(AdaptationReason.SwitchUp, 3, 1)]
    [InlineData(AdaptationReason.SwitchDown, 1, 1)]
    [InlineData(AdaptationReason.SwitchDown, 1, 3)]
    [InlineData(AdaptationReason.Panic, 1, 3)]
    public void A_reason_that_contradicts_the_move_is_refused(AdaptationReason reason, int previousMbps, int chosenMbps) {
        // Arrange
        EncodingVariant previous = Fixtures.Variant(720, 30, previousMbps * 1_000);
        EncodingVariant chosen   = Fixtures.Variant(720, 60, chosenMbps * 1_000);

        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => Decide(previous, chosen, reason));

        // Assert
        Assert.Equal("reason", refusal.ParamName);
    }

    [Theory]
    [InlineData(1, 3)]
    [InlineData(3, 1)]
    [InlineData(2, 2)]
    public void A_startup_decision_accepts_any_move(int previousMbps, int chosenMbps) {
        // Arrange
        EncodingVariant previous = Fixtures.Variant(720, 30, previousMbps * 1_000);
        EncodingVariant chosen   = Fixtures.Variant(720, 60, chosenMbps * 1_000);

        // Act
        AdaptationDecision decision = Decide(previous, chosen, AdaptationReason.Startup);

        // Assert
        Assert.Equal(AdaptationReason.Startup, decision.Reason);
    }

    [Fact]
    public void A_reason_the_enumeration_does_not_declare_is_refused() {
        // Arrange
        AdaptationReason undeclared = (AdaptationReason)99;

        // Act / Assert
        Assert.Throws<ArgumentOutOfRangeException>("reason", () => Decide(Slow, Fast, undeclared));
    }

    [Theory]
    [InlineData(0.0)]
    [InlineData(-0.5)]
    [InlineData(double.NaN)]
    public void A_predicted_download_time_that_is_not_positive_is_refused(double predicted) {
        // Arrange
        Segment anySegment = new AnySegment().Generate();

        // Act / Assert
        Assert.Throws<ArgumentOutOfRangeException>("predictedDownloadSeconds",
                                                   () => new AdaptationDecision(anySegment, Slow, Fast, AdaptationReason.SwitchUp, predicted));
    }

    [Fact]
    public void A_decision_needs_a_segment_and_both_variants() {
        // Arrange
        Segment anySegment = new AnySegment().Generate();

        // Assert
        Assert.Throws<ArgumentNullException>("segment", () => new AdaptationDecision(null!, Slow, Fast, AdaptationReason.Startup, 1));
        Assert.Throws<ArgumentNullException>("previous", () => new AdaptationDecision(anySegment, null!, Fast, AdaptationReason.Startup, 1));
        Assert.Throws<ArgumentNullException>("chosen", () => new AdaptationDecision(anySegment, Slow, null!, AdaptationReason.Startup, 1));
    }

    [Fact]
    public void A_drawn_decision_is_one_the_constructor_accepts() {
        // Arrange
        AnyAdaptationDecision anyDecision = new AnyAdaptationDecision();

        // Act / Assert: the generator is part of the test code, so it is checked over many draws.
        for (int i = 0; i < 300; i++) {
            Exception? failure = Record.Exception(() => anyDecision.Generate());

            Assert.Null(failure);
        }
    }

}
