using AdaptiveVideo.Domain;
using AdaptiveVideo.Tests.Regular.Generators;

using JustDummies.Xunit;

using Xunit;

namespace AdaptiveVideo.Tests.Regular;

public sealed class BitrateLadderTests {

    [Fact, Reproducible]
    public void A_ladder_orders_its_variants_by_ascending_bitrate() {
        // Arrange
        EncodingVariant low  = Arbitrary.EncodingVariant().WithHeight(360).WithBitrate(new Bitrate(500)).Generate();
        EncodingVariant mid  = Arbitrary.EncodingVariant().WithHeight(720).WithBitrate(new Bitrate(1_500)).Generate();
        EncodingVariant high = Arbitrary.EncodingVariant().WithHeight(1080).WithBitrate(new Bitrate(3_000)).Generate();

        // Act
        BitrateLadder ladder = new([high, low, mid]);

        // Assert
        Assert.Equal([500, 1_500, 3_000], ladder.Variants.Select(v => v.Bitrate.Kbps));
    }

    [Fact, Reproducible]
    public void A_variant_with_a_higher_bitrate_but_a_lower_resolution_is_rejected() {
        // Arrange
        EncodingVariant sharpButCheap = Arbitrary.EncodingVariant().WithHeight(1080).WithBitrate(new Bitrate(1_000)).Generate();
        EncodingVariant blurryButDear = Arbitrary.EncodingVariant().WithHeight(480).WithBitrate(new Bitrate(2_000)).Generate();

        // Act
        Exception? error = Record.Exception(() => new BitrateLadder([sharpButCheap, blurryButDear]));

        // Assert
        Assert.IsType<ArgumentException>(error);
    }

    [Fact, Reproducible]
    public void Two_variants_with_the_same_bitrate_are_rejected() {
        // Arrange
        EncodingVariant first  = Arbitrary.EncodingVariant().WithHeight(480).WithBitrate(new Bitrate(1_200)).Generate();
        EncodingVariant second = Arbitrary.EncodingVariant().WithHeight(720).WithBitrate(new Bitrate(1_200)).Generate();

        // Act
        Exception? error = Record.Exception(() => new BitrateLadder([first, second]));

        // Assert
        Assert.IsType<ArgumentException>(error);
    }

    [Theory, Reproducible]
    [InlineData(2_000, 1_500)]
    [InlineData(3_000, 3_000)]
    [InlineData(100, 500)]
    public void The_highest_variant_within_a_budget_falls_back_to_the_lowest(double budgetKbps, int expectedKbps) {
        // Arrange
        BitrateLadder ladder = Arbitrary.BitrateLadder().WithBitrates(500, 1_500, 3_000).Generate();

        // Act
        EncodingVariant chosen = ladder.HighestWithin(budgetKbps);

        // Assert
        Assert.Equal(expectedKbps, chosen.Bitrate.Kbps);
    }

    [Fact, Reproducible]
    public void Stepping_up_from_the_top_rung_stays_on_it() {
        // Arrange
        BitrateLadder ladder = Arbitrary.BitrateLadder().Generate();

        // Act
        EncodingVariant stepped = ladder.StepUp(ladder.Highest);

        // Assert
        Assert.Same(ladder.Highest, stepped);
    }

    [Fact, Reproducible]
    public void A_variant_from_another_ladder_has_no_rung() {
        // Arrange
        BitrateLadder   ladder  = Arbitrary.BitrateLadder().WithBitrates(500, 1_500).Generate();
        EncodingVariant foreign = Arbitrary.EncodingVariant().WithHeight(2160).Generate();

        // Act
        Exception? error = Record.Exception(() => ladder.IndexOf(foreign));

        // Assert
        Assert.IsType<ArgumentException>(error);
    }

}
