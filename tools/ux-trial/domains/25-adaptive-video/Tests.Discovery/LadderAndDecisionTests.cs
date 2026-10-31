using AdaptiveVideo.Domain;

using JustDummies;
using JustDummies.Xunit;

using Xunit;

namespace AdaptiveVideo.Tests.Discovery;

public sealed class LadderAndDecisionTests {

    // A rung whose height and bitrate the test chose; its width and frame rate are drawn.
    private static EncodingVariant AnyRung(int height, int kbps) {
        return Any.EncodingVariant().WithHeight(height).WithBitrate(new Bitrate(kbps)).Generate();
    }

    [Fact, Reproducible]
    public void A_ladder_orders_its_variants_by_bitrate_whatever_the_input_order() {
        // Arrange
        EncodingVariant anyRungAt800  = AnyRung(360, 800);
        EncodingVariant anyRungAt3000 = AnyRung(720, 3000);
        EncodingVariant anyRungAt300  = AnyRung(240, 300);

        // Act
        BitrateLadder ladder = new([anyRungAt800, anyRungAt3000, anyRungAt300]);

        // Assert
        Assert.Equal([300, 800, 3000], ladder.Variants.Select(v => v.Bitrate.Kbps));
    }

    [Fact, Reproducible]
    public void A_ladder_refuses_two_variants_at_the_same_bitrate() {
        // Arrange
        EncodingVariant anyRungAt1500 = AnyRung(480, 1500);
        EncodingVariant anyOtherAt1500 = AnyRung(720, 1500);

        // Act
        Exception? error = Record.Exception(() => new BitrateLadder([anyRungAt1500, anyOtherAt1500]));

        // Assert
        Assert.IsType<ArgumentException>(error);
    }

    [Fact, Reproducible]
    public void A_ladder_refuses_a_higher_bitrate_at_a_lower_resolution() {
        // Arrange
        EncodingVariant anyTallRungAt1000 = AnyRung(1080, 1000);
        EncodingVariant anyShortRungAt2000 = AnyRung(720, 2000);

        // Act
        Exception? error = Record.Exception(() => new BitrateLadder([anyTallRungAt1000, anyShortRungAt2000]));

        // Assert
        Assert.IsType<ArgumentException>(error);
    }

    [Fact, Reproducible]
    public void The_highest_variant_within_a_budget_is_the_last_one_that_fits() {
        // Arrange
        BitrateLadder ladder = new([AnyRung(240, 400), AnyRung(480, 1200), AnyRung(720, 2400)]);

        // Act
        EncodingVariant chosen = ladder.HighestWithin(2000);

        // Assert
        Assert.Equal(1200, chosen.Bitrate.Kbps);
    }

    [Fact, Reproducible]
    public void A_budget_below_every_variant_falls_back_to_the_lowest() {
        // Arrange
        BitrateLadder ladder = new([AnyRung(240, 400), AnyRung(480, 1200)]);

        // Act
        EncodingVariant chosen = ladder.HighestWithin(100);

        // Assert
        Assert.Equal(400, chosen.Bitrate.Kbps);
    }

    [Fact, Reproducible]
    public void Stepping_up_from_the_top_rung_stays_on_it() {
        // Arrange
        BitrateLadder ladder = new([AnyRung(240, 400), AnyRung(480, 1200)]);

        // Act
        EncodingVariant stepped = ladder.StepUp(ladder.Highest);

        // Assert
        Assert.Equal(1200, stepped.Bitrate.Kbps);
    }

    [Fact, Reproducible]
    public void A_variant_foreign_to_the_ladder_has_no_index() {
        // Arrange
        BitrateLadder   ladder        = new([AnyRung(240, 400), AnyRung(480, 1200)]);
        EncodingVariant anyForeignRung = AnyRung(1080, 6000);

        // Act
        Exception? error = Record.Exception(() => ladder.IndexOf(anyForeignRung));

        // Assert
        Assert.IsType<ArgumentException>(error);
    }

    [Fact, Reproducible]
    public void A_hold_decision_that_changes_bitrate_is_refused() {
        // Arrange
        Segment         anySegment    = Any.Segment().Generate();
        EncodingVariant anyRungAt800  = AnyRung(360, 800);
        EncodingVariant anyRungAt1600 = AnyRung(720, 1600);
        double          anyPrediction = Any.Double().Between(0.1, 30).Generate();

        // Act
        Exception? error = Record.Exception(
            () => new AdaptationDecision(anySegment, anyRungAt800, anyRungAt1600, AdaptationReason.Hold, anyPrediction));

        // Assert
        Assert.IsType<ArgumentException>(error);
    }

    [Fact, Reproducible]
    public void A_panic_that_stays_on_the_same_variant_is_not_a_switch() {
        // Arrange
        Segment         anySegment    = Any.Segment().Generate();
        EncodingVariant anyVariant    = Any.EncodingVariant().Generate();
        double          anyPrediction = Any.Double().Between(0.1, 30).Generate();

        // Act
        AdaptationDecision decision = new(anySegment, anyVariant, anyVariant, AdaptationReason.Panic, anyPrediction);

        // Assert
        Assert.False(decision.IsSwitch);
    }

}
