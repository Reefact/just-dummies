using AdaptiveVideo.Domain;

using JustDummies;

using Xunit;

namespace AdaptiveVideo.Tests.Expert;

public sealed class BitrateLadderTests {

    [Fact]
    public void The_ladder_orders_its_rungs_by_bitrate_whatever_order_they_are_listed_in() {
        // Arrange: three well-formed rungs, handed over in an arbitrary order.
        IReadOnlyList<EncodingVariant> rungs = Any.BitrateLadder().WithBitrates(400, 1_000, 2_500).Generate().Variants;
        // A drawn permutation: three distinct indices out of three. (No shuffle generator exists.)
        List<int> anyOrder = Any.ListOf(Any.Int32().Between(0, 2)).WithCount(3).Distinct().Generate();

        // Act
        BitrateLadder ladder = new(anyOrder.Select(i => rungs[i]));

        // Assert
        Assert.Equal([400, 1_000, 2_500], ladder.Variants.Select(v => v.Bitrate.Kbps));
    }

    [Fact]
    public void Two_rungs_with_the_same_bitrate_are_refused() {
        // Arrange
        BitrateLadder   anyLadder = Any.BitrateLadder().WithRungCount(2).Generate();
        EncodingVariant low       = anyLadder.Lowest;
        EncodingVariant high      = anyLadder.Highest;
        EncodingVariant sameRate  = new(high.Id, high.Width, high.Height, low.Bitrate);

        // Act
        Exception? refusal = Record.Exception(() => new BitrateLadder([low, sameRate]));

        // Assert
        ArgumentException argument = Assert.IsType<ArgumentException>(refusal);
        Assert.Contains("distinct", argument.Message);
    }

    [Fact]
    public void A_higher_bitrate_cannot_carry_a_lower_resolution() {
        // Arrange: swap the bitrates of two well-formed rungs, so resolution falls as bitrate rises.
        BitrateLadder   anyLadder = Any.BitrateLadder().WithRungCount(2).Generate();
        EncodingVariant low       = anyLadder.Lowest;
        EncodingVariant high      = anyLadder.Highest;

        EncodingVariant smallButExpensive = new(low.Id, low.Width, low.Height, high.Bitrate);
        EncodingVariant largeButCheap     = new(high.Id, high.Width, high.Height, low.Bitrate);

        // Act
        Exception? refusal = Record.Exception(() => new BitrateLadder([smallButExpensive, largeButCheap]));

        // Assert
        ArgumentException argument = Assert.IsType<ArgumentException>(refusal);
        Assert.Contains("lower resolution", argument.Message);
    }

    [Theory]
    [InlineData(3_000, 2_500)]
    [InlineData(2_500, 2_500)]
    [InlineData(999, 400)]
    [InlineData(300, 400)]
    public void The_highest_rung_within_a_budget_falls_back_to_the_lowest(double budgetKbps, int expectedKbps) {
        // Arrange
        BitrateLadder ladder = Player.Ladder.Generate();

        // Act
        EncodingVariant chosen = ladder.HighestWithin(budgetKbps);

        // Assert
        Assert.Equal(expectedKbps, chosen.Bitrate.Kbps);
    }

    [Fact]
    public void Stepping_beyond_either_end_of_the_ladder_stays_on_it() {
        // Arrange
        BitrateLadder ladder = Player.Ladder.Generate();

        // Act
        EncodingVariant aboveTop    = ladder.StepUp(ladder.Highest);
        EncodingVariant belowBottom = ladder.StepDown(ladder.Lowest);

        // Assert
        Assert.Equal(5_000, aboveTop.Bitrate.Kbps);
        Assert.Equal(400, belowBottom.Bitrate.Kbps);
    }

    [Fact]
    public void A_variant_from_another_ladder_has_no_rung_on_this_one() {
        // Arrange
        BitrateLadder anyLadder = Any.BitrateLadder().Generate();
        // Ids are '<height>p<fps>': a height no rung uses guarantees a foreign id.
        int[]           usedHeights = anyLadder.Variants.Select(v => v.Height).ToArray();
        EncodingVariant anyForeign  = Any.EncodingVariant().WithHeight(h => h.Except(usedHeights)).Generate();

        // Act
        Exception? byIndex = Record.Exception(() => anyLadder.IndexOf(anyForeign));
        Exception? byId    = Record.Exception(() => anyLadder.Find(anyForeign.Id));

        // Assert
        Assert.IsType<ArgumentException>(byIndex);
        Assert.IsType<KeyNotFoundException>(byId);
    }

}
