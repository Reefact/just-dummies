using VesselStowage.Domain;

namespace VesselStowage.Tests;

public sealed class SlotPositionTests {

    [Fact]
    public void A_generated_slot_is_accepted_by_the_constructor() {
        SlotPosition anySlot = Any.SlotPosition().Generate();

        Assert.NotNull(new SlotPosition(anySlot.Bay, anySlot.Row, anySlot.Tier));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(100)]
    [InlineData(-1)]
    public void A_bay_outside_1_to_99_is_refused(int bay) {
        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(() => new SlotPosition(bay, 0, 2));

        Assert.Equal("bay", refusal.ParamName);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(21)]
    public void A_row_outside_0_to_20_is_refused(int row) {
        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(() => new SlotPosition(1, row, 2));

        Assert.Equal("row", refusal.ParamName);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(3)]
    [InlineData(22)]
    [InlineData(80)]
    [InlineData(81)]
    [InlineData(100)]
    [InlineData(-2)]
    public void A_tier_that_is_odd_or_between_the_hold_and_the_deck_ranges_is_refused(int tier) {
        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(() => new SlotPosition(1, 0, tier));

        Assert.Equal("tier", refusal.ParamName);
    }

    [Theory]
    [InlineData(1, 0, 2)]
    [InlineData(99, 20, 98)]
    [InlineData(50, 10, 20)]
    [InlineData(50, 10, 82)]
    public void The_extreme_valid_positions_are_accepted(int bay, int row, int tier) {
        SlotPosition slot = new SlotPosition(bay, row, tier);

        Assert.Equal((bay, row, tier), (slot.Bay, slot.Row, slot.Tier));
    }

    [Theory]
    [InlineData(2, false, 0)]
    [InlineData(4, false, 1)]
    [InlineData(20, false, 9)]
    [InlineData(82, true, 0)]
    [InlineData(84, true, 1)]
    [InlineData(98, true, 8)]
    public void The_tier_gives_the_section_and_the_zero_based_level(int tier, bool onDeck, int level) {
        SlotPosition slot = Any.SlotPosition().WithTier(tier).Generate();

        Assert.Equal(onDeck, slot.IsOnDeck);
        Assert.Equal(level, slot.Level);
        Assert.Equal(level == 0, slot.IsLowestOfSection);
    }

    [Fact]
    public void An_odd_bay_takes_twenty_foot_boxes() {
        SlotPosition anyOddBaySlot = Any.SlotPosition().WithBay(b => b.Odd()).Generate();

        Assert.Equal(ContainerSize.TwentyFoot, anyOddBaySlot.AcceptedSize);
    }

    [Fact]
    public void An_even_bay_takes_forty_foot_boxes() {
        SlotPosition anyEvenBaySlot = Any.SlotPosition().WithBay(b => b.Even()).Generate();

        Assert.Equal(ContainerSize.FortyFoot, anyEvenBaySlot.AcceptedSize);
    }

    [Theory]
    [InlineData(0, 0.0, 0)]
    [InlineData(1, 2.44, 1)]
    [InlineData(2, -2.44, -1)]
    [InlineData(3, 4.88, 2)]
    [InlineData(4, -4.88, -2)]
    [InlineData(19, 24.4, 10)]
    [InlineData(20, -24.4, -10)]
    public void Odd_rows_are_to_starboard_and_even_rows_to_port(int row, double offsetMetres, int index) {
        SlotPosition slot = Any.SlotPosition().WithRow(row).Generate();

        Assert.Equal(offsetMetres, slot.TransverseOffsetMetres, 9);
        Assert.Equal(index, slot.TransverseIndex);
    }

    [Theory]
    [InlineData(2, 3, 2.795)]
    [InlineData(4, 3, 5.385)]
    [InlineData(20, 3, 1.5 + 9 * 2.59 + 1.295)]
    [InlineData(82, 3, 1.5 + 3 * 2.59 + 1.0 + 1.295)]
    [InlineData(84, 3, 1.5 + 3 * 2.59 + 1.0 + 2.59 + 1.295)]
    [InlineData(82, 5, 1.5 + 5 * 2.59 + 1.0 + 1.295)]
    public void The_centre_height_adds_the_floor_the_tiers_below_and_the_hatch_cover_for_a_deck_box(int tier, int holdTiers, double heightMetres) {
        SlotPosition slot = Any.SlotPosition().WithTier(tier).Generate();

        Assert.Equal(heightMetres, slot.CentreHeightMetres(holdTiers), 9);
    }

    [Fact]
    public void The_slot_below_a_hold_box_is_the_same_stack_one_tier_down() {
        SlotPosition slot = new SlotPosition(5, 3, 6);

        SlotPosition? below = slot.Below();

        Assert.Equal(new SlotPosition(5, 3, 4), below);
    }

    [Fact]
    public void The_slot_below_a_deck_box_is_the_same_stack_one_tier_down() {
        Assert.Equal(new SlotPosition(5, 3, 84), new SlotPosition(5, 3, 86).Below());
    }

    [Theory]
    [InlineData(2)]
    [InlineData(82)]
    public void Nothing_lies_below_the_lowest_slot_of_a_section(int tier) {
        SlotPosition anyLowestSlot = Any.SlotPosition().WithTier(tier).Generate();

        Assert.Null(anyLowestSlot.Below());
    }

    [Fact]
    public void Slots_one_row_apart_in_the_same_bay_and_section_are_adjacent() {
        SlotPosition slot = new SlotPosition(5, 1, 4);

        Assert.True(slot.IsAdjacentTo(new SlotPosition(5, 3, 4)));
        Assert.True(slot.IsAdjacentTo(new SlotPosition(5, 0, 4)));
    }

    [Fact]
    public void The_two_rows_either_side_of_the_centreline_are_not_adjacent_to_each_other() {
        // row 1 is one to starboard, row 2 one to port: the centreline lies between them
        Assert.False(new SlotPosition(5, 1, 4).IsAdjacentTo(new SlotPosition(5, 2, 4)));
        Assert.True(new SlotPosition(5, 0, 4).IsAdjacentTo(new SlotPosition(5, 2, 4)));
    }

    [Fact]
    public void Slots_one_tier_apart_in_the_same_stack_are_adjacent() {
        SlotPosition slot = new SlotPosition(5, 1, 4);

        Assert.True(slot.IsAdjacentTo(new SlotPosition(5, 1, 6)));
        Assert.True(slot.IsAdjacentTo(new SlotPosition(5, 1, 2)));
    }

    [Fact]
    public void A_diagonal_a_gap_or_the_same_slot_is_not_adjacent() {
        SlotPosition slot = new SlotPosition(5, 1, 4);

        Assert.False(slot.IsAdjacentTo(new SlotPosition(5, 3, 6)));
        Assert.False(slot.IsAdjacentTo(new SlotPosition(5, 5, 4)));
        Assert.False(slot.IsAdjacentTo(new SlotPosition(5, 1, 8)));
        Assert.False(slot.IsAdjacentTo(new SlotPosition(5, 1, 4)));
    }

    [Fact]
    public void Slots_of_two_bays_are_never_adjacent() {
        Assert.False(new SlotPosition(5, 1, 4).IsAdjacentTo(new SlotPosition(7, 1, 4)));
    }

    [Fact]
    public void The_top_hold_tier_and_the_first_deck_tier_are_not_adjacent() {
        // the hatch cover is between them, and they belong to different sections
        Assert.False(new SlotPosition(5, 1, 20).IsAdjacentTo(new SlotPosition(5, 1, 82)));
    }

    [Fact]
    public void Adjacency_to_null_is_refused() {
        Assert.Throws<ArgumentNullException>(() => new SlotPosition(5, 1, 4).IsAdjacentTo(null!));
    }

    [Fact]
    public void Slots_with_the_same_coordinates_are_equal_and_hash_alike() {
        SlotPosition first  = new SlotPosition(5, 1, 4);
        SlotPosition second = new SlotPosition(5, 1, 4);

        Assert.Equal(first, second);
        Assert.Equal(first.GetHashCode(), second.GetHashCode());
        Assert.NotEqual(first, new SlotPosition(5, 1, 6));
    }

    [Fact]
    public void A_slot_prints_as_bay_row_and_tier_on_two_digits_each() {
        Assert.Equal("050304", new SlotPosition(5, 3, 4).ToString());
        Assert.Equal("990098", new SlotPosition(99, 0, 98).ToString());
    }

}
