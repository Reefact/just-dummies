using JustDummies;

namespace AdaptiveVideo.Domain.Tests;

public sealed class BitrateLadderTests {

    private static EncodingVariant V240  => Fixtures.Variant(240, 30, 400);
    private static EncodingVariant V480  => Fixtures.Variant(480, 30, 1_200);
    private static EncodingVariant V720  => Fixtures.Variant(720, 30, 3_000);
    private static EncodingVariant V1080 => Fixtures.Variant(1080, 30, 6_000);

    [Fact]
    public void A_ladder_lists_its_variants_by_ascending_bitrate_whatever_the_order_given() {
        // Act
        BitrateLadder ladder = new BitrateLadder([V720, V240, V1080, V480]);

        // Assert
        Assert.Equal(["240p30", "480p30", "720p30", "1080p30"], ladder.Variants.Select(v => v.Id));
    }

    [Fact]
    public void The_lowest_and_the_highest_are_the_ends_of_the_ladder() {
        // Arrange
        BitrateLadder ladder = new BitrateLadder([V720, V240, V1080, V480]);

        // Assert
        Assert.Equal("240p30", ladder.Lowest.Id);
        Assert.Equal("1080p30", ladder.Highest.Id);
    }

    [Fact]
    public void A_ladder_of_one_variant_is_both_its_lowest_and_its_highest() {
        // Arrange
        EncodingVariant only = V720;

        // Act
        BitrateLadder ladder = new BitrateLadder([only]);

        // Assert
        Assert.Same(only, ladder.Lowest);
        Assert.Same(only, ladder.Highest);
    }

    [Fact]
    public void Two_renditions_of_the_same_height_may_share_a_ladder_at_different_bitrates() {
        // Act
        BitrateLadder ladder = new BitrateLadder([Fixtures.Variant(720, 30, 3_000), Fixtures.Variant(720, 60, 4_500)]);

        // Assert
        Assert.Equal(2, ladder.Variants.Count);
    }

    [Fact]
    public void A_ladder_is_not_affected_by_later_changes_to_the_list_it_was_built_from() {
        // Arrange
        List<EncodingVariant> source = [V240, V480];
        BitrateLadder         ladder = new BitrateLadder(source);

        // Act
        source.Add(V720);

        // Assert
        Assert.Equal(2, ladder.Variants.Count);
    }

    [Fact]
    public void A_missing_sequence_is_refused() {
        Assert.Throws<ArgumentNullException>("variants", () => new BitrateLadder(null!));
    }

    [Fact]
    public void An_empty_ladder_is_refused() {
        Assert.Throws<ArgumentException>("variants", () => new BitrateLadder([]));
    }

    [Fact]
    public void A_null_variant_is_refused() {
        Assert.Throws<ArgumentException>("variants", () => new BitrateLadder([V240, null!]));
    }

    [Fact]
    public void Two_variants_with_the_same_id_are_refused() {
        // Arrange: same id, different bitrates.
        EncodingVariant first  = Fixtures.Variant(720, 30, 3_000);
        EncodingVariant second = Fixtures.Variant(720, 30, 3_500);

        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => new BitrateLadder([first, second]));

        // Assert
        Assert.Contains("unique", refusal.Message);
    }

    [Fact]
    public void Two_variants_with_the_same_bitrate_are_refused() {
        // Arrange
        EncodingVariant first  = Fixtures.Variant(720, 30, 3_000);
        EncodingVariant second = Fixtures.Variant(1080, 30, 3_000);

        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => new BitrateLadder([first, second]));

        // Assert
        Assert.Contains("distinct", refusal.Message);
    }

    [Fact]
    public void A_higher_bitrate_at_a_lower_resolution_is_refused() {
        // Arrange
        EncodingVariant sharp = Fixtures.Variant(1080, 30, 2_000);
        EncodingVariant rich  = Fixtures.Variant(720, 30, 5_000);

        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => new BitrateLadder([sharp, rich]));

        // Assert
        Assert.Contains("720p30", refusal.Message);
    }

    [Fact]
    public void A_variant_is_found_by_its_id() {
        // Arrange
        BitrateLadder ladder = new BitrateLadder([V240, V480, V720]);

        // Assert
        Assert.Equal(1_200, ladder.Find("480p30").Bitrate.Kbps);
    }

    [Fact]
    public void Finding_an_id_the_ladder_does_not_hold_is_refused() {
        // Arrange
        BitrateLadder ladder = new BitrateLadder([V240, V480]);

        // Act
        KeyNotFoundException refusal = Assert.Throws<KeyNotFoundException>(() => ladder.Find("720p30"));

        // Assert
        Assert.Contains("720p30", refusal.Message);
    }

    [Fact]
    public void Finding_without_an_id_is_refused() {
        // Arrange
        BitrateLadder ladder = new AnyBitrateLadder().Generate();

        // Act / Assert
        Assert.Throws<ArgumentNullException>("id", () => ladder.Find(null!));
    }

    [Theory]
    [InlineData(100.0, "240p30")]
    [InlineData(399.9, "240p30")]
    [InlineData(400.0, "240p30")]
    [InlineData(1_199.9, "240p30")]
    [InlineData(1_200.0, "480p30")]
    [InlineData(2_999.0, "480p30")]
    [InlineData(3_000.0, "720p30")]
    [InlineData(5_999.9, "720p30")]
    [InlineData(6_000.0, "1080p30")]
    [InlineData(900_000.0, "1080p30")]
    public void The_highest_variant_within_a_budget_is_the_richest_that_costs_no_more(double budget, string expected) {
        // Arrange
        BitrateLadder ladder = new BitrateLadder([V240, V480, V720, V1080]);

        // Assert
        Assert.Equal(expected, ladder.HighestWithin(budget).Id);
    }

    [Fact]
    public void A_budget_below_the_whole_ladder_still_gets_the_lowest_variant() {
        // Arrange
        BitrateLadder ladder = new BitrateLadder([V480, V720]);

        // Assert
        Assert.Equal("480p30", ladder.HighestWithin(10).Id);
    }

    [Fact]
    public void Stepping_up_moves_one_rung_and_stops_at_the_top() {
        // Arrange
        BitrateLadder ladder = new BitrateLadder([V240, V480, V720]);

        // Assert
        Assert.Equal("480p30", ladder.StepUp(ladder.Find("240p30")).Id);
        Assert.Equal("720p30", ladder.StepUp(ladder.Find("480p30")).Id);
        Assert.Equal("720p30", ladder.StepUp(ladder.Find("720p30")).Id);
    }

    [Fact]
    public void Stepping_down_moves_one_rung_and_stops_at_the_bottom() {
        // Arrange
        BitrateLadder ladder = new BitrateLadder([V240, V480, V720]);

        // Assert
        Assert.Equal("480p30", ladder.StepDown(ladder.Find("720p30")).Id);
        Assert.Equal("240p30", ladder.StepDown(ladder.Find("480p30")).Id);
        Assert.Equal("240p30", ladder.StepDown(ladder.Find("240p30")).Id);
    }

    [Fact]
    public void A_rung_is_known_by_its_position_whichever_instance_designates_it() {
        // Arrange
        BitrateLadder ladder = new BitrateLadder([V240, V480, V720]);

        // Assert
        Assert.Equal(0, ladder.IndexOf(V240));
        Assert.Equal(2, ladder.IndexOf(V720));
    }

    [Fact]
    public void A_variant_from_another_ladder_has_no_position() {
        // Arrange
        BitrateLadder   ladder  = new BitrateLadder([V240, V480]);
        EncodingVariant foreign = Fixtures.Variant(1080, 60, 9_000);

        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => ladder.IndexOf(foreign));

        // Assert
        Assert.Contains("1080p60", refusal.Message);
    }

    [Fact]
    public void Stepping_from_a_foreign_variant_is_refused() {
        // Arrange
        BitrateLadder   ladder  = new BitrateLadder([V240, V480]);
        EncodingVariant foreign = Fixtures.Variant(1080, 60, 9_000);

        // Assert
        Assert.Throws<ArgumentException>(() => ladder.StepUp(foreign));
        Assert.Throws<ArgumentException>(() => ladder.StepDown(foreign));
    }

    [Fact]
    public void The_position_of_nothing_is_refused() {
        // Arrange
        BitrateLadder ladder = new AnyBitrateLadder().Generate();

        // Act / Assert
        Assert.Throws<ArgumentNullException>("variant", () => ladder.IndexOf(null!));
    }

    [Fact]
    public void A_drawn_ladder_is_one_the_constructor_accepts_and_has_the_rungs_asked_for() {
        // Arrange
        AnyBitrateLadder anyLadder = new AnyBitrateLadder().WithRungs(5);

        // Act / Assert: the generator is part of the test code, so it is checked over many draws.
        for (int i = 0; i < 200; i++) {
            Assert.Equal(5, anyLadder.Generate().Variants.Count);
        }
    }

}
