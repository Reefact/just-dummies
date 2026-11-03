using AdaptiveVideo.Domain;

namespace AdaptiveVideo.Tests.Discovery;

public sealed class AdaptationDecisionTests {

    [Fact]
    public void A_decision_exposes_what_it_was_given() {
        EncodingVariant low  = Fixtures.Variant(360, 1_000);
        EncodingVariant high = Fixtures.Variant(720, 4_000);

        Segment anySegment = Any.Segment().Generate();

        AdaptationDecision decision = new AdaptationDecision(anySegment, low, high, AdaptationReason.SwitchUp, 1.5);

        Assert.Same(anySegment, decision.Segment);
        Assert.Same(low, decision.Previous);
        Assert.Same(high, decision.Chosen);
        Assert.Equal(AdaptationReason.SwitchUp, decision.Reason);
        Assert.Equal(1.5, decision.PredictedDownloadSeconds);
    }

    [Theory]
    [InlineData(AdaptationReason.Startup, true)]
    [InlineData(AdaptationReason.Startup, false)]
    [InlineData(AdaptationReason.SwitchUp, true)]
    [InlineData(AdaptationReason.SwitchDown, false)]
    [InlineData(AdaptationReason.Panic, false)]
    public void A_reason_that_agrees_with_the_move_is_accepted(AdaptationReason reason, bool movesUp) {
        EncodingVariant low           = Fixtures.Variant(360, 1_000);
        EncodingVariant high          = Fixtures.Variant(720, 4_000);
        Segment         anySegment    = Any.Segment().Generate();
        double          anyPrediction = Any.Double().Positive().LessThanOrEqualTo(600).Generate();

        AdaptationDecision decision = new AdaptationDecision(anySegment, movesUp ? low : high, movesUp ? high : low, reason, anyPrediction);

        Assert.Equal(reason, decision.Reason);
    }

    [Theory]
    [InlineData(AdaptationReason.Startup)]
    [InlineData(AdaptationReason.Hold)]
    [InlineData(AdaptationReason.Panic)]
    public void A_move_to_the_same_bitrate_accepts_startup_hold_and_panic(AdaptationReason reason) {
        EncodingVariant low  = Fixtures.Variant(360, 1_000);

        Segment anySegment    = Any.Segment().Generate();
        double  anyPrediction = Any.Double().Positive().LessThanOrEqualTo(600).Generate();

        AdaptationDecision decision = new AdaptationDecision(anySegment, low, low, reason, anyPrediction);

        Assert.False(decision.IsSwitch);
    }

    [Theory]
    [InlineData(AdaptationReason.Hold)]
    [InlineData(AdaptationReason.SwitchDown)]
    [InlineData(AdaptationReason.Panic)]
    public void A_climb_is_refused_for_a_reason_that_is_not_a_climb(AdaptationReason reason) {
        EncodingVariant low  = Fixtures.Variant(360, 1_000);
        EncodingVariant high = Fixtures.Variant(720, 4_000);

        Segment anySegment    = Any.Segment().Generate();
        double  anyPrediction = Any.Double().Positive().LessThanOrEqualTo(600).Generate();

        ArgumentException exception = Assert.Throws<ArgumentException>(() => new AdaptationDecision(anySegment, low, high, reason, anyPrediction));

        Assert.Equal("reason", exception.ParamName);
        Assert.Contains(reason.ToString(), exception.Message);
    }

    [Theory]
    [InlineData(AdaptationReason.Hold)]
    [InlineData(AdaptationReason.SwitchUp)]
    public void A_drop_is_refused_for_a_reason_that_is_not_a_drop(AdaptationReason reason) {
        EncodingVariant low  = Fixtures.Variant(360, 1_000);
        EncodingVariant high = Fixtures.Variant(720, 4_000);

        Segment anySegment    = Any.Segment().Generate();
        double  anyPrediction = Any.Double().Positive().LessThanOrEqualTo(600).Generate();

        Assert.Throws<ArgumentException>(() => new AdaptationDecision(anySegment, high, low, reason, anyPrediction));
    }

    [Theory]
    [InlineData(AdaptationReason.SwitchUp)]
    [InlineData(AdaptationReason.SwitchDown)]
    public void No_move_is_refused_for_a_switch_reason(AdaptationReason reason) {
        EncodingVariant low  = Fixtures.Variant(360, 1_000);

        Segment anySegment    = Any.Segment().Generate();
        double  anyPrediction = Any.Double().Positive().LessThanOrEqualTo(600).Generate();

        Assert.Throws<ArgumentException>(() => new AdaptationDecision(anySegment, low, low, reason, anyPrediction));
    }

    [Theory]
    [InlineData((AdaptationReason)5)]
    [InlineData((AdaptationReason)(-1))]
    [InlineData((AdaptationReason)99)]
    public void An_undefined_reason_is_refused(AdaptationReason reason) {
        EncodingVariant low  = Fixtures.Variant(360, 1_000);
        EncodingVariant high = Fixtures.Variant(720, 4_000);

        Segment anySegment    = Any.Segment().Generate();
        double  anyPrediction = Any.Double().Positive().LessThanOrEqualTo(600).Generate();

        ArgumentOutOfRangeException exception = Assert.Throws<ArgumentOutOfRangeException>(() => new AdaptationDecision(anySegment, low, high, reason, anyPrediction));

        Assert.Equal("reason", exception.ParamName);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-0.001)]
    [InlineData(-10)]
    [InlineData(double.NaN)]
    [InlineData(double.NegativeInfinity)]
    public void A_non_positive_prediction_is_refused(double predictedSeconds) {
        EncodingVariant low  = Fixtures.Variant(360, 1_000);
        EncodingVariant high = Fixtures.Variant(720, 4_000);

        Segment anySegment = Any.Segment().Generate();

        ArgumentOutOfRangeException exception = Assert.Throws<ArgumentOutOfRangeException>(() => new AdaptationDecision(anySegment, low, high, AdaptationReason.SwitchUp, predictedSeconds));

        Assert.Equal("predictedDownloadSeconds", exception.ParamName);
    }

    [Fact]
    public void A_decision_without_a_segment_is_refused() {
        EncodingVariant low  = Fixtures.Variant(360, 1_000);
        EncodingVariant high = Fixtures.Variant(720, 4_000);

        double anyPrediction = Any.Double().Positive().LessThanOrEqualTo(600).Generate();

        Assert.Throws<ArgumentNullException>(() => new AdaptationDecision(null!, low, high, AdaptationReason.SwitchUp, anyPrediction));
    }

    [Fact]
    public void A_decision_without_a_previous_variant_is_refused() {
        EncodingVariant high = Fixtures.Variant(720, 4_000);

        Segment anySegment    = Any.Segment().Generate();
        double  anyPrediction = Any.Double().Positive().LessThanOrEqualTo(600).Generate();

        Assert.Throws<ArgumentNullException>(() => new AdaptationDecision(anySegment, null!, high, AdaptationReason.SwitchUp, anyPrediction));
    }

    [Fact]
    public void A_decision_without_a_chosen_variant_is_refused() {
        EncodingVariant low  = Fixtures.Variant(360, 1_000);

        Segment anySegment    = Any.Segment().Generate();
        double  anyPrediction = Any.Double().Positive().LessThanOrEqualTo(600).Generate();

        Assert.Throws<ArgumentNullException>(() => new AdaptationDecision(anySegment, low, null!, AdaptationReason.SwitchUp, anyPrediction));
    }

    [Fact]
    public void A_decision_is_a_switch_when_the_bitrate_differs() {
        EncodingVariant low  = Fixtures.Variant(360, 1_000);
        EncodingVariant high = Fixtures.Variant(720, 4_000);

        AdaptationDecision climb = Any.AdaptationDecision().WithPrevious(low).WithChosen(high).Generate();

        Assert.True(climb.IsSwitch);
    }

    [Fact]
    public void A_decision_that_keeps_the_bitrate_is_not_a_switch() {
        EncodingVariant low  = Fixtures.Variant(360, 1_000);

        AdaptationDecision hold = Any.AdaptationDecision().WithPrevious(low).WithChosen(low).Generate();

        Assert.False(hold.IsSwitch);
    }

    [Fact]
    public void A_variant_with_another_id_but_the_same_bitrate_is_not_counted_as_a_switch() {
        EncodingVariant low  = Fixtures.Variant(360, 1_000);

        EncodingVariant sameRateOtherPicture = Fixtures.Variant(480, 1_000);

        AdaptationDecision decision = Any.AdaptationDecision().WithPrevious(low).WithChosen(sameRateOtherPicture).WithReason(AdaptationReason.Hold).Generate();

        Assert.False(decision.IsSwitch);
    }

}
