using JustDummies;
using VesselStowage.Domain.Tests.Generators;
using Xunit;

namespace VesselStowage.Domain.Tests;

public sealed class SlotPositionTests {

    [Theory]
    [InlineData(1, 0, 2)]
    [InlineData(99, 20, 20)]
    [InlineData(1, 0, 82)]
    [InlineData(99, 20, 98)]
    [InlineData(50, 10, 12)]
    public void A_slot_inside_the_ISO_numbering_is_accepted(int bay, int row, int tier) {
        // Act
        SlotPosition slot = new SlotPosition(bay, row, tier);

        // Assert
        Assert.Equal(bay, slot.Bay);
        Assert.Equal(row, slot.Row);
        Assert.Equal(tier, slot.Tier);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(100)]
    [InlineData(-1)]
    public void A_bay_outside_1_to_99_is_refused(int bay) {
        // Arrange
        int anyRow  = Any.Int32().Between(0, 20).Generate();
        int anyTier = Any.Int32().OneOf(2, 82).Generate();

        // Act
        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(() => new SlotPosition(bay, anyRow, anyTier));

        // Assert
        Assert.Equal("bay", refusal.ParamName);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(21)]
    public void A_row_outside_0_to_20_is_refused(int row) {
        // Arrange
        int anyBay  = Any.Int32().Between(1, 99).Generate();
        int anyTier = Any.Int32().OneOf(2, 82).Generate();

        // Act
        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(() => new SlotPosition(anyBay, row, anyTier));

        // Assert
        Assert.Equal("row", refusal.ParamName);
    }

    [Theory]
    [InlineData(0)]   // below the hold
    [InlineData(1)]   // odd
    [InlineData(3)]   // odd
    [InlineData(22)]  // just above the hold
    [InlineData(80)]  // between hold and deck
    [InlineData(81)]  // odd
    [InlineData(100)] // above the deck
    [InlineData(-2)]
    [InlineData(83)]  // odd, on deck
    public void A_tier_that_is_odd_or_outside_the_hold_and_the_deck_is_refused(int tier) {
        // Arrange
        int anyBay = Any.Int32().Between(1, 99).Generate();
        int anyRow = Any.Int32().Between(0, 20).Generate();

        // Act
        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(() => new SlotPosition(anyBay, anyRow, tier));

        // Assert
        Assert.Equal("tier", refusal.ParamName);
    }

    [Theory]
    [InlineData(2, false, 0)]
    [InlineData(4, false, 1)]
    [InlineData(20, false, 9)]
    [InlineData(82, true, 0)]
    [InlineData(84, true, 1)]
    [InlineData(98, true, 8)]
    public void The_tier_gives_the_section_and_the_level_within_it(int tier, bool onDeck, int level) {
        // Arrange
        SlotPosition slot = Any.SlotPosition().WithTier(tier).Generate();

        // Assert
        Assert.Equal(onDeck, slot.IsOnDeck);
        Assert.Equal(level, slot.Level);
        Assert.Equal(level == 0, slot.IsLowestOfSection);
    }

    [Theory]
    [InlineData(1, ContainerSize.TwentyFoot)]
    [InlineData(3, ContainerSize.TwentyFoot)]
    [InlineData(99, ContainerSize.TwentyFoot)]
    [InlineData(2, ContainerSize.FortyFoot)]
    [InlineData(4, ContainerSize.FortyFoot)]
    [InlineData(98, ContainerSize.FortyFoot)]
    public void Odd_bays_take_twenty_foot_boxes_and_even_bays_forty_foot_ones(int bay, ContainerSize accepted) {
        // Arrange
        SlotPosition slot = Any.SlotPosition().WithBay(bay).Generate();

        // Assert
        Assert.Equal(accepted, slot.AcceptedSize);
    }

    [Theory]
    [InlineData(0, 0.0, 0)]
    [InlineData(1, 2.44, 1)]    // first row to starboard
    [InlineData(2, -2.44, -1)]  // first row to port
    [InlineData(3, 4.88, 2)]
    [InlineData(4, -4.88, -2)]
    [InlineData(19, 24.4, 10)]
    [InlineData(20, -24.4, -10)]
    public void The_row_gives_the_offset_from_the_centreline_positive_to_starboard(int row, double offsetMetres, int index) {
        // Arrange
        SlotPosition slot = Any.SlotPosition().WithRow(row).Generate();

        // Assert
        Assert.Equal(offsetMetres, slot.TransverseOffsetMetres, 9);
        Assert.Equal(index, slot.TransverseIndex);
    }

    [Theory]
    [InlineData(2, 2.795)]   // 1.5 hold floor + half a tier
    [InlineData(4, 5.385)]   // one tier higher
    [InlineData(20, 26.105)] // tenth level of the hold
    public void A_hold_slot_centre_sits_above_the_hold_floor_whatever_the_hold_depth(int tier, double centreHeightMetres) {
        // Arrange
        SlotPosition slot             = Any.SlotPosition().WithTier(tier).Generate();
        int          anyHoldTiers     = Any.Int32().Between(1, 10).Generate();

        // Assert
        Assert.Equal(centreHeightMetres, slot.CentreHeightMetres(anyHoldTiers), 9);
    }

    [Theory]
    [InlineData(2, 82, 8.975)]   // 1.5 + 2 hold tiers + 1.0 hatch cover + half a tier
    [InlineData(2, 84, 11.565)]
    [InlineData(4, 82, 14.155)]
    [InlineData(10, 82, 29.695)]
    public void A_deck_slot_centre_sits_above_the_hold_and_the_hatch_cover(int holdTiers, int tier, double centreHeightMetres) {
        // Arrange
        SlotPosition slot = Any.SlotPosition().WithTier(tier).Generate();

        // Assert
        Assert.Equal(centreHeightMetres, slot.CentreHeightMetres(holdTiers), 9);
    }

    [Theory]
    [InlineData(2)]
    [InlineData(82)]
    public void Nothing_lies_below_the_lowest_slot_of_a_section(int tier) {
        // Arrange
        SlotPosition slot = Any.SlotPosition().WithTier(tier).Generate();

        // Assert
        Assert.Null(slot.Below());
    }

    [Theory]
    [InlineData(4, 2)]
    [InlineData(20, 18)]
    [InlineData(84, 82)]
    [InlineData(98, 96)]
    public void Below_is_the_slot_one_tier_down_in_the_same_bay_and_row(int tier, int tierBelow) {
        // Arrange
        int          anyBay = Any.Int32().Between(1, 99).Generate();
        int          anyRow = Any.Int32().Between(0, 20).Generate();
        SlotPosition slot   = new SlotPosition(anyBay, anyRow, tier);

        // Act
        SlotPosition? below = slot.Below();

        // Assert
        Assert.Equal(new SlotPosition(anyBay, anyRow, tierBelow), below);
    }

    [Theory]
    [InlineData(1, 4, 0, 4)]   // starboard row next to the centreline
    [InlineData(2, 4, 0, 4)]   // port row next to the centreline
    [InlineData(3, 4, 1, 4)]   // the first two starboard rows
    [InlineData(4, 4, 2, 4)]   // two port rows
    [InlineData(0, 6, 0, 4)]   // one tier up
    [InlineData(0, 84, 0, 86)] // one tier up on deck
    public void Slots_side_by_side_or_one_on_top_of_the_other_are_adjacent(int row, int tier, int otherRow, int otherTier) {
        // Arrange
        int          anyBay = Any.Int32().Between(1, 99).Generate();
        SlotPosition slot   = new SlotPosition(anyBay, row, tier);
        SlotPosition other  = new SlotPosition(anyBay, otherRow, otherTier);

        // Assert
        Assert.True(slot.IsAdjacentTo(other));
        Assert.True(other.IsAdjacentTo(slot));
    }

    [Theory]
    [InlineData(1, 4, 2, 4)]   // either side of the centreline, two rows apart
    [InlineData(1, 4, 5, 4)]   // two rows apart, same side
    [InlineData(0, 4, 0, 8)]   // two tiers apart
    [InlineData(1, 4, 0, 6)]   // diagonal
    [InlineData(0, 4, 0, 4)]   // the same slot
    [InlineData(0, 20, 0, 82)] // top of the hold and bottom of the deck: the hatch cover lies between
    public void Slots_that_do_not_touch_are_not_adjacent(int row, int tier, int otherRow, int otherTier) {
        // Arrange
        int          anyBay = Any.Int32().Between(1, 99).Generate();
        SlotPosition slot   = new SlotPosition(anyBay, row, tier);
        SlotPosition other  = new SlotPosition(anyBay, otherRow, otherTier);

        // Assert
        Assert.False(slot.IsAdjacentTo(other));
        Assert.False(other.IsAdjacentTo(slot));
    }

    [Fact]
    public void Slots_in_different_bays_are_never_adjacent() {
        // Arrange
        int anyBay      = Any.Int32().Between(1, 98).Generate();
        int anyNextBay  = anyBay + 1;

        // Assert
        Assert.False(new SlotPosition(anyBay, 0, 4).IsAdjacentTo(new SlotPosition(anyNextBay, 0, 4)));
    }

    [Fact]
    public void A_missing_slot_cannot_be_compared_for_adjacency() {
        // Arrange
        SlotPosition anySlot = Any.SlotPosition().Generate();

        // Assert
        Assert.Throws<ArgumentNullException>(() => anySlot.IsAdjacentTo(null!));
    }

    [Fact]
    public void Slots_with_the_same_coordinates_are_equal() {
        // Arrange
        SlotPosition anySlot = Any.SlotPosition().Generate();

        // Act
        SlotPosition sameSlot = new SlotPosition(anySlot.Bay, anySlot.Row, anySlot.Tier);

        // Assert
        Assert.Equal(anySlot, sameSlot);
        Assert.True(anySlot.Equals((object)sameSlot));
        Assert.Equal(anySlot.GetHashCode(), sameSlot.GetHashCode());
    }

    [Theory]
    [InlineData(2, 1, 4)]
    [InlineData(1, 2, 4)]
    [InlineData(1, 1, 6)]
    public void Slots_differing_by_one_coordinate_are_not_equal(int bay, int row, int tier) {
        // Assert
        Assert.NotEqual(new SlotPosition(1, 1, 4), new SlotPosition(bay, row, tier));
    }

    [Fact]
    public void A_slot_is_written_as_bay_row_tier_on_two_digits_each() {
        Assert.Equal("030082", new SlotPosition(3, 0, 82).ToString());
        Assert.Equal("991220", new SlotPosition(99, 12, 20).ToString());
        Assert.Equal("010102", new SlotPosition(1, 1, 2).ToString());
    }

}
