using AdaptiveVideo.Domain;

using JustDummies;

using Xunit;

namespace AdaptiveVideo.Tests.Expert;

public sealed class BitrateLadderTests {

    // The ladder most tests read: three rungs, at 800, 2500 and 5000 kbps.
    private static BitrateLadder ThreeRungs() {
        return Any.BitrateLadder().WithBitratesKbps(800, 2_500, 5_000).Generate();
    }

    [Fact]
    public void A_ladder_is_sorted_by_ascending_bitrate_whatever_the_order_it_was_given() {
        // Arrange
        EncodingVariant low  = Variants.Of(height: 360, framesPerSecond: 30, kbps: 800);
        EncodingVariant mid  = Variants.Of(height: 720, framesPerSecond: 30, kbps: 2_500);
        EncodingVariant high = Variants.Of(height: 1080, framesPerSecond: 30, kbps: 5_000);

        // Act
        BitrateLadder fromHighToLow = new BitrateLadder(new[] { high, mid, low });
        BitrateLadder shuffled      = new BitrateLadder(new[] { mid, high, low });

        // Assert
        Assert.Equal(new[] { low, mid, high }, fromHighToLow.Variants);
        Assert.Equal(new[] { low, mid, high }, shuffled.Variants);
    }

    [Fact]
    public void The_lowest_and_the_highest_are_the_ends_of_the_ladder() {
        // Arrange
        BitrateLadder ladder = ThreeRungs();

        // Assert
        Assert.Equal(800, ladder.Lowest.Bitrate.Kbps);
        Assert.Equal(5_000, ladder.Highest.Bitrate.Kbps);
    }

    [Fact]
    public void A_ladder_of_one_variant_is_its_own_lowest_highest_and_neighbour() {
        // Arrange
        BitrateLadder ladder = Any.BitrateLadder().WithBitratesKbps(1_500).Generate();

        // Assert
        Assert.Same(ladder.Lowest, ladder.Highest);
        Assert.Same(ladder.Lowest, ladder.StepUp(ladder.Lowest));
        Assert.Same(ladder.Lowest, ladder.StepDown(ladder.Lowest));
    }

    [Fact]
    public void A_ladder_does_not_follow_the_collection_it_was_built_from() {
        // Arrange
        List<EncodingVariant> source = new List<EncodingVariant> {
            Variants.Of(height: 360, framesPerSecond: 30, kbps: 800),
            Variants.Of(height: 720, framesPerSecond: 30, kbps: 2_500)
        };
        BitrateLadder ladder = new BitrateLadder(source);

        // Act
        source.Clear();

        // Assert
        Assert.Equal(2, ladder.Variants.Count);
    }

    [Fact]
    public void A_ladder_needs_at_least_one_variant() {
        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => new BitrateLadder(Array.Empty<EncodingVariant>()));

        // Assert
        Assert.Equal("variants", refusal.ParamName);
    }

    [Fact]
    public void A_ladder_needs_a_collection_of_variants() {
        // Act
        ArgumentNullException refusal = Assert.Throws<ArgumentNullException>(() => new BitrateLadder(null!));

        // Assert
        Assert.Equal("variants", refusal.ParamName);
    }

    [Fact]
    public void A_ladder_cannot_hold_a_missing_variant() {
        // Arrange
        EncodingVariant anyVariant = Any.EncodingVariant().Generate();

        // Act + Assert
        Assert.Throws<ArgumentException>(() => new BitrateLadder(new EncodingVariant?[] { anyVariant, null }!));
    }

    [Fact]
    public void Two_variants_of_a_ladder_cannot_share_an_id() {
        // Arrange
        EncodingVariant first  = Variants.Of(height: 720, framesPerSecond: 30, kbps: 2_500);
        EncodingVariant second = Variants.Of(height: 720, framesPerSecond: 30, kbps: 3_000);

        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => new BitrateLadder(new[] { first, second }));

        // Assert
        Assert.Contains("unique", refusal.Message);
    }

    [Fact]
    public void Two_variants_of_a_ladder_cannot_share_a_bitrate() {
        // Arrange
        EncodingVariant first  = Variants.Of(height: 720, framesPerSecond: 30, kbps: 2_500);
        EncodingVariant second = Variants.Of(height: 1080, framesPerSecond: 30, kbps: 2_500);

        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => new BitrateLadder(new[] { first, second }));

        // Assert
        Assert.Contains("distinct", refusal.Message);
    }

    [Fact]
    public void A_higher_bitrate_cannot_come_with_a_lower_resolution() {
        // Arrange
        EncodingVariant sharpButCheap = Variants.Of(height: 1080, framesPerSecond: 30, kbps: 1_000);
        EncodingVariant blurryButRich = Variants.Of(height: 720, framesPerSecond: 30, kbps: 2_000);

        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => new BitrateLadder(new[] { sharpButCheap, blurryButRich }));

        // Assert
        Assert.Contains("720p30", refusal.Message);
        Assert.Contains("1080p30", refusal.Message);
    }

    [Fact]
    public void A_higher_bitrate_may_keep_the_resolution_and_raise_the_frame_rate() {
        // Arrange
        EncodingVariant thirty = Variants.Of(height: 720, framesPerSecond: 30, kbps: 2_000);
        EncodingVariant sixty  = Variants.Of(height: 720, framesPerSecond: 60, kbps: 3_000);

        // Act
        BitrateLadder ladder = new BitrateLadder(new[] { sixty, thirty });

        // Assert
        Assert.Equal(new[] { thirty, sixty }, ladder.Variants);
    }

    [Fact]
    public void A_variant_is_found_by_its_id() {
        // Arrange
        EncodingVariant wanted = Variants.Of(height: 720, framesPerSecond: 30, kbps: 2_500);
        BitrateLadder   ladder = new BitrateLadder(new[] { Variants.Of(height: 360, framesPerSecond: 30, kbps: 800), wanted });

        // Act
        EncodingVariant found = ladder.Find("720p30");

        // Assert
        Assert.Same(wanted, found);
    }

    [Theory]
    [InlineData("1080p30")]
    [InlineData("720P30")]
    [InlineData("")]
    public void Looking_for_an_id_the_ladder_does_not_have_is_a_key_error(string id) {
        // Arrange
        BitrateLadder ladder = Any.BitrateLadder().WithBitratesKbps(800).Generate();

        // Act
        KeyNotFoundException refusal = Assert.Throws<KeyNotFoundException>(() => ladder.Find(id));

        // Assert
        Assert.Contains(id, refusal.Message);
    }

    [Fact]
    public void Looking_for_no_id_is_a_null_error() {
        // Arrange
        BitrateLadder anyLadder = Any.BitrateLadder().Generate();

        // Act + Assert
        Assert.Throws<ArgumentNullException>(() => anyLadder.Find(null!));
    }

    [Theory]
    [InlineData(-1, 800)]
    [InlineData(0, 800)]
    [InlineData(799.999, 800)]
    [InlineData(800, 800)]
    [InlineData(2_499.999, 800)]
    [InlineData(2_500, 2_500)]
    [InlineData(4_999.999, 2_500)]
    [InlineData(5_000, 5_000)]
    [InlineData(1_000_000, 5_000)]
    public void The_highest_variant_within_a_budget_is_the_one_that_fits_it_or_else_the_lowest(double budgetKbps, int expectedKbps) {
        // Arrange
        BitrateLadder ladder = ThreeRungs();

        // Act
        EncodingVariant variant = ladder.HighestWithin(budgetKbps);

        // Assert
        Assert.Equal(expectedKbps, variant.Bitrate.Kbps);
    }

    [Theory]
    [InlineData(800, 2_500)]
    [InlineData(2_500, 5_000)]
    [InlineData(5_000, 5_000)]
    public void Stepping_up_climbs_one_rung_and_stays_on_the_top_one(int currentKbps, int expectedKbps) {
        // Arrange
        BitrateLadder ladder  = ThreeRungs();
        EncodingVariant current = ladder.Variants.Single(v => v.Bitrate.Kbps == currentKbps);

        // Act
        EncodingVariant stepped = ladder.StepUp(current);

        // Assert
        Assert.Equal(expectedKbps, stepped.Bitrate.Kbps);
    }

    [Theory]
    [InlineData(5_000, 2_500)]
    [InlineData(2_500, 800)]
    [InlineData(800, 800)]
    public void Stepping_down_descends_one_rung_and_stays_on_the_bottom_one(int currentKbps, int expectedKbps) {
        // Arrange
        BitrateLadder ladder  = ThreeRungs();
        EncodingVariant current = ladder.Variants.Single(v => v.Bitrate.Kbps == currentKbps);

        // Act
        EncodingVariant stepped = ladder.StepDown(current);

        // Assert
        Assert.Equal(expectedKbps, stepped.Bitrate.Kbps);
    }

    [Theory]
    [InlineData(0, 800)]
    [InlineData(1, 2_500)]
    [InlineData(2, 5_000)]
    public void The_index_of_a_variant_is_its_position_from_the_lowest(int expectedIndex, int kbps) {
        // Arrange
        BitrateLadder   ladder  = ThreeRungs();
        EncodingVariant variant = ladder.Variants.Single(v => v.Bitrate.Kbps == kbps);

        // Act + Assert
        Assert.Equal(expectedIndex, ladder.IndexOf(variant));
    }

    [Fact]
    public void A_variant_of_another_ladder_has_no_index_in_this_one() {
        // Arrange
        BitrateLadder ladder = new BitrateLadder(new[] {
            Variants.Of(height: 360, framesPerSecond: 30, kbps: 800),
            Variants.Of(height: 720, framesPerSecond: 30, kbps: 2_500)
        });
        EncodingVariant foreign = Variants.Of(height: 1080, framesPerSecond: 60, kbps: 6_000);

        // Act + Assert
        Assert.Throws<ArgumentException>(() => ladder.IndexOf(foreign));
        Assert.Throws<ArgumentException>(() => ladder.StepUp(foreign));
        Assert.Throws<ArgumentException>(() => ladder.StepDown(foreign));
    }

    [Fact]
    public void Asking_for_the_index_of_no_variant_is_a_null_error() {
        // Arrange
        BitrateLadder anyLadder = Any.BitrateLadder().Generate();

        // Act + Assert
        Assert.Throws<ArgumentNullException>(() => anyLadder.IndexOf(null!));
    }

}
