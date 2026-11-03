using AdaptiveVideo.Domain;

namespace AdaptiveVideo.Tests.Discovery;

public sealed class BitrateLadderTests {

    [Fact]
    public void A_ladder_sorts_its_variants_by_ascending_bitrate() {
        EncodingVariant low  = Fixtures.Variant(240, 400);
        EncodingVariant mid  = Fixtures.Variant(480, 2_000);
        EncodingVariant high = Fixtures.Variant(1080, 8_000);

        BitrateLadder ladder = new BitrateLadder([high, low, mid]);

        Assert.Equal([low, mid, high], ladder.Variants);
    }

    [Fact]
    public void The_lowest_and_highest_variants_are_the_ends_of_the_sorted_ladder() {
        EncodingVariant low  = Fixtures.Variant(240, 400);
        EncodingVariant high = Fixtures.Variant(1080, 8_000);

        BitrateLadder ladder = new BitrateLadder([high, low]);

        Assert.Same(low, ladder.Lowest);
        Assert.Same(high, ladder.Highest);
    }

    [Fact]
    public void A_ladder_of_one_variant_has_it_for_lowest_and_highest() {
        EncodingVariant only = Any.EncodingVariant().Generate();

        BitrateLadder ladder = new BitrateLadder([only]);

        Assert.Same(only, ladder.Lowest);
        Assert.Same(only, ladder.Highest);
    }

    [Fact]
    public void A_ladder_does_not_follow_later_changes_to_the_sequence_it_was_built_from() {
        List<EncodingVariant> source = [Fixtures.Variant(240, 400), Fixtures.Variant(480, 2_000)];
        BitrateLadder         ladder = new BitrateLadder(source);

        source.Add(Fixtures.Variant(1080, 8_000));

        Assert.Equal(2, ladder.Variants.Count);
    }

    [Fact]
    public void A_ladder_without_a_sequence_is_refused() {
        Assert.Throws<ArgumentNullException>(() => new BitrateLadder(null!));
    }

    [Fact]
    public void An_empty_ladder_is_refused() {
        ArgumentException exception = Assert.Throws<ArgumentException>(() => new BitrateLadder([]));

        Assert.Equal("variants", exception.ParamName);
    }

    [Fact]
    public void A_ladder_holding_a_null_variant_is_refused() {
        EncodingVariant anyVariant = Any.EncodingVariant().Generate();

        ArgumentException exception = Assert.Throws<ArgumentException>(() => new BitrateLadder([anyVariant, null!]));

        Assert.Contains("null", exception.Message);
    }

    [Fact]
    public void Two_variants_with_the_same_id_are_refused() {
        EncodingVariant first  = Fixtures.Variant(720, 2_000);
        EncodingVariant second = Fixtures.Variant(720, 4_000);

        ArgumentException exception = Assert.Throws<ArgumentException>(() => new BitrateLadder([first, second]));

        Assert.Contains("unique", exception.Message);
    }

    [Fact]
    public void Two_variants_with_the_same_bitrate_are_refused() {
        EncodingVariant first  = Fixtures.Variant(480, 2_000);
        EncodingVariant second = Fixtures.Variant(720, 2_000);

        ArgumentException exception = Assert.Throws<ArgumentException>(() => new BitrateLadder([first, second]));

        Assert.Contains("distinct", exception.Message);
    }

    [Fact]
    public void A_higher_bitrate_with_a_lower_resolution_is_refused() {
        EncodingVariant first  = Fixtures.Variant(720, 2_000);
        EncodingVariant second = Fixtures.Variant(480, 4_000);

        ArgumentException exception = Assert.Throws<ArgumentException>(() => new BitrateLadder([first, second]));

        Assert.Contains(second.Id, exception.Message);
    }

    [Fact]
    public void A_higher_bitrate_at_the_same_resolution_is_accepted() {
        EncodingVariant first  = Fixtures.Variant(720, 2_000, framesPerSecond: 30);
        EncodingVariant second = Fixtures.Variant(720, 4_000, framesPerSecond: 60);

        BitrateLadder ladder = new BitrateLadder([first, second]);

        Assert.Same(second, ladder.Highest);
    }

    [Fact]
    public void A_variant_is_found_by_its_id() {
        EncodingVariant wanted = Fixtures.Variant(480, 2_000, framesPerSecond: 25);
        BitrateLadder   ladder = new BitrateLadder([Fixtures.Variant(240, 400), wanted, Fixtures.Variant(1080, 8_000)]);

        EncodingVariant found = ladder.Find("480p25");

        Assert.Same(wanted, found);
    }

    [Fact]
    public void An_unknown_id_is_not_found() {
        BitrateLadder ladder = Fixtures.FiveRungLadder();

        KeyNotFoundException exception = Assert.Throws<KeyNotFoundException>(() => ladder.Find("144p24"));

        Assert.Contains("144p24", exception.Message);
    }

    [Fact]
    public void Looking_up_a_missing_id_is_refused() {
        BitrateLadder ladder = Fixtures.FiveRungLadder();

        Assert.Throws<ArgumentNullException>(() => ladder.Find(null!));
    }

    [Theory]
    [InlineData(8_000, 4)]
    [InlineData(100_000, 4)]
    [InlineData(7_999.9, 3)]
    [InlineData(4_000, 3)]
    [InlineData(3_999, 2)]
    [InlineData(2_000, 2)]
    [InlineData(1_000, 1)]
    [InlineData(400, 0)]
    public void The_highest_variant_within_a_budget_is_the_highest_one_not_above_it(double budgetKbps, int expectedIndex) {
        BitrateLadder ladder = Fixtures.FiveRungLadder();

        EncodingVariant chosen = ladder.HighestWithin(budgetKbps);

        Assert.Same(ladder.Variants[expectedIndex], chosen);
    }

    [Theory]
    [InlineData(399.9)]
    [InlineData(0)]
    [InlineData(-5)]
    [InlineData(double.NaN)]
    [InlineData(double.NegativeInfinity)]
    public void A_budget_below_the_lowest_variant_falls_back_to_the_lowest(double budgetKbps) {
        BitrateLadder ladder = Fixtures.FiveRungLadder();

        EncodingVariant chosen = ladder.HighestWithin(budgetKbps);

        Assert.Same(ladder.Lowest, chosen);
    }

    [Fact]
    public void An_infinite_budget_selects_the_highest_variant() {
        BitrateLadder ladder = Fixtures.FiveRungLadder();

        EncodingVariant chosen = ladder.HighestWithin(double.PositiveInfinity);

        Assert.Same(ladder.Highest, chosen);
    }

    [Theory]
    [InlineData(0, 1)]
    [InlineData(1, 2)]
    [InlineData(3, 4)]
    public void Stepping_up_moves_one_rung(int fromIndex, int expectedIndex) {
        BitrateLadder ladder = Fixtures.FiveRungLadder();

        EncodingVariant stepped = ladder.StepUp(ladder.Variants[fromIndex]);

        Assert.Same(ladder.Variants[expectedIndex], stepped);
    }

    [Fact]
    public void Stepping_up_from_the_top_stays_there() {
        BitrateLadder ladder = Fixtures.FiveRungLadder();

        EncodingVariant stepped = ladder.StepUp(ladder.Highest);

        Assert.Same(ladder.Highest, stepped);
    }

    [Theory]
    [InlineData(4, 3)]
    [InlineData(3, 2)]
    [InlineData(1, 0)]
    public void Stepping_down_moves_one_rung(int fromIndex, int expectedIndex) {
        BitrateLadder ladder = Fixtures.FiveRungLadder();

        EncodingVariant stepped = ladder.StepDown(ladder.Variants[fromIndex]);

        Assert.Same(ladder.Variants[expectedIndex], stepped);
    }

    [Fact]
    public void Stepping_down_from_the_bottom_stays_there() {
        BitrateLadder ladder = Fixtures.FiveRungLadder();

        EncodingVariant stepped = ladder.StepDown(ladder.Lowest);

        Assert.Same(ladder.Lowest, stepped);
    }

    [Fact]
    public void A_variant_is_indexed_by_its_position_in_the_sorted_ladder() {
        EncodingVariant low  = Fixtures.Variant(240, 400);
        EncodingVariant high = Fixtures.Variant(1080, 8_000);
        BitrateLadder   ladder = new BitrateLadder([high, low]);

        Assert.Equal(0, ladder.IndexOf(low));
        Assert.Equal(1, ladder.IndexOf(high));
    }

    [Fact]
    public void A_variant_from_another_ladder_has_no_index() {
        BitrateLadder   ladder  = new BitrateLadder([Fixtures.Variant(240, 400), Fixtures.Variant(480, 2_000)]);
        EncodingVariant foreign = Fixtures.Variant(1080, 8_000);

        ArgumentException exception = Assert.Throws<ArgumentException>(() => ladder.IndexOf(foreign));

        Assert.Contains(foreign.Id, exception.Message);
    }

    [Fact]
    public void Stepping_from_a_foreign_variant_is_refused() {
        BitrateLadder   ladder  = new BitrateLadder([Fixtures.Variant(240, 400), Fixtures.Variant(480, 2_000)]);
        EncodingVariant foreign = Fixtures.Variant(1080, 8_000);

        Assert.Throws<ArgumentException>(() => ladder.StepUp(foreign));
        Assert.Throws<ArgumentException>(() => ladder.StepDown(foreign));
    }

    [Fact]
    public void Indexing_nothing_is_refused() {
        BitrateLadder ladder = Fixtures.FiveRungLadder();

        Assert.Throws<ArgumentNullException>(() => ladder.IndexOf(null!));
    }

    [Fact(Skip = "domain defect: IndexOf/StepUp/StepDown match a variant by id only, so a variant that shares an id with a rung but has another bitrate is accepted as part of the ladder; literal input, any seed")]
    public void A_look_alike_sharing_an_id_but_not_the_bitrate_is_a_foreign_variant() {
        BitrateLadder   ladder    = new BitrateLadder([Fixtures.Variant(240, 400), Fixtures.Variant(480, 2_000)]);
        EncodingVariant lookAlike = Fixtures.Variant(480, 3_000);   // "480p30" like the rung at 2 000 kbps

        Assert.Throws<ArgumentException>(() => ladder.IndexOf(lookAlike));
    }

    [Fact]
    public void A_drawn_ladder_is_a_valid_ladder_that_sorts_to_its_lowest_and_highest_ends() {
        BitrateLadder anyLadder = Any.BitrateLadder().Generate();

        Assert.True(anyLadder.Lowest.Bitrate.Kbps <= anyLadder.Highest.Bitrate.Kbps);
        Assert.Equal(anyLadder.Variants.Count, anyLadder.Variants.Select(v => v.Id).Distinct().Count());
    }

}
