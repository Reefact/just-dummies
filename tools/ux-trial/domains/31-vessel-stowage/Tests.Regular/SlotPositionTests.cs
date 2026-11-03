using JustDummies;

using Xunit;

namespace VesselStowage.Domain.Tests;

public sealed class SlotPositionTests {

    [Fact]
    public void A_slot_keeps_its_bay_row_and_tier() {
        // Act
        SlotPosition slot = new SlotPosition(12, 3, 84);

        // Assert
        Assert.Equal(12, slot.Bay);
        Assert.Equal(3, slot.Row);
        Assert.Equal(84, slot.Tier);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(99)]
    public void The_first_and_last_bays_are_accepted(int bay) {
        // Arrange
        int anyRow  = Any.Int32().Between(0, 20).Generate();
        int anyTier = Any.Int32().OneOf(2, 20, 82, 98).Generate();

        // Act
        SlotPosition slot = new SlotPosition(bay, anyRow, anyTier);

        // Assert
        Assert.Equal(bay, slot.Bay);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(100)]
    [InlineData(-1)]
    public void A_bay_outside_1_to_99_is_refused(int bay) {
        // Arrange
        int anyRow  = Any.Int32().Between(0, 20).Generate();
        int anyTier = Any.SlotPosition().Generate().Tier;

        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>("bay", () => new SlotPosition(bay, anyRow, anyTier));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(20)]
    public void The_centreline_and_the_last_port_row_are_accepted(int row) {
        // Arrange
        int anyBay  = Any.Int32().Between(1, 99).Generate();
        int anyTier = Any.SlotPosition().Generate().Tier;

        // Act
        SlotPosition slot = new SlotPosition(anyBay, row, anyTier);

        // Assert
        Assert.Equal(row, slot.Row);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(21)]
    public void A_row_outside_0_to_20_is_refused(int row) {
        // Arrange
        int anyBay  = Any.Int32().Between(1, 99).Generate();
        int anyTier = Any.SlotPosition().Generate().Tier;

        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>("row", () => new SlotPosition(anyBay, row, anyTier));
    }

    [Theory]
    [InlineData(2)]
    [InlineData(20)]
    [InlineData(82)]
    [InlineData(98)]
    public void The_first_and_last_tiers_of_the_hold_and_of_the_deck_are_accepted(int tier) {
        // Arrange
        int anyBay = Any.Int32().Between(1, 99).Generate();
        int anyRow = Any.Int32().Between(0, 20).Generate();

        // Act
        SlotPosition slot = new SlotPosition(anyBay, anyRow, tier);

        // Assert
        Assert.Equal(tier, slot.Tier);
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
    public void A_tier_that_is_odd_or_outside_the_hold_and_the_deck_ranges_is_refused(int tier) {
        // Arrange
        int anyBay = Any.Int32().Between(1, 99).Generate();
        int anyRow = Any.Int32().Between(0, 20).Generate();

        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>("tier", () => new SlotPosition(anyBay, anyRow, tier));
    }

    [Theory]
    [InlineData(2, false, 0)]
    [InlineData(4, false, 1)]
    [InlineData(20, false, 9)]
    [InlineData(82, true, 0)]
    [InlineData(84, true, 1)]
    [InlineData(98, true, 8)]
    public void A_tier_gives_the_section_and_the_level_within_it(int tier, bool isOnDeck, int level) {
        // Arrange
        SlotPosition slot = Any.SlotPosition().WithTier(tier).Generate();

        // Assert
        Assert.Equal(isOnDeck, slot.IsOnDeck);
        Assert.Equal(level, slot.Level);
        Assert.Equal(level == 0, slot.IsLowestOfSection);
    }

    [Theory]
    [InlineData(1, ContainerSize.TwentyFoot)]
    [InlineData(13, ContainerSize.TwentyFoot)]
    [InlineData(99, ContainerSize.TwentyFoot)]
    [InlineData(2, ContainerSize.FortyFoot)]
    [InlineData(14, ContainerSize.FortyFoot)]
    [InlineData(98, ContainerSize.FortyFoot)]
    public void An_odd_bay_takes_twenty_foot_boxes_and_an_even_bay_forty_foot_boxes(int bay, ContainerSize accepted) {
        // Arrange
        SlotPosition slot = Any.SlotPosition().WithBay(bay).Generate();

        // Assert
        Assert.Equal(accepted, slot.AcceptedSize);
    }

    [Theory]
    [InlineData(0, 0, 0.0)]
    [InlineData(1, 1, 2.44)]
    [InlineData(2, -1, -2.44)]
    [InlineData(3, 2, 4.88)]
    [InlineData(4, -2, -4.88)]
    [InlineData(19, 10, 24.4)]
    [InlineData(20, -10, -24.4)]
    public void Odd_rows_lie_to_starboard_and_even_rows_to_port_of_the_centreline(int row, int index, double offsetMetres) {
        // Arrange
        SlotPosition slot = Any.SlotPosition().WithRow(row).Generate();

        // Assert
        Assert.Equal(index, slot.TransverseIndex);
        Assert.Equal(offsetMetres, slot.TransverseOffsetMetres, 9);
    }

    [Theory]
    [InlineData(2, 3, 1.5 + 1.295)]
    [InlineData(4, 3, 1.5 + 2.59 + 1.295)]
    [InlineData(20, 10, 1.5 + 9 * 2.59 + 1.295)]
    public void The_centre_of_a_hold_slot_stands_a_tier_height_per_level_above_the_hold_floor(int tier, int holdTiers, double heightMetres) {
        // Arrange
        SlotPosition slot = Any.SlotPosition().WithTier(tier).Generate();

        // Act
        double height = slot.CentreHeightMetres(holdTiers);

        // Assert
        Assert.Equal(heightMetres, height, 9);
    }

    [Theory]
    [InlineData(82, 3, 1.5 + 3 * 2.59 + 1.0 + 1.295)]
    [InlineData(86, 3, 1.5 + 3 * 2.59 + 1.0 + 2 * 2.59 + 1.295)]
    [InlineData(82, 1, 1.5 + 2.59 + 1.0 + 1.295)]
    public void The_centre_of_a_deck_slot_stands_above_the_hold_tiers_and_the_hatch_cover(int tier, int holdTiers, double heightMetres) {
        // Arrange
        SlotPosition slot = Any.SlotPosition().WithTier(tier).Generate();

        // Act
        double height = slot.CentreHeightMetres(holdTiers);

        // Assert
        Assert.Equal(heightMetres, height, 9);
    }

    [Fact]
    public void The_slot_below_is_the_previous_tier_of_the_same_stack() {
        // Arrange
        int anyBay = Any.Int32().Between(1, 99).Generate();
        int anyRow = Any.Int32().Between(0, 20).Generate();
        SlotPosition slot = new SlotPosition(anyBay, anyRow, 6);

        // Act
        SlotPosition? below = slot.Below();

        // Assert
        Assert.Equal(new SlotPosition(anyBay, anyRow, 4), below);
    }

    [Theory]
    [InlineData(2)]
    [InlineData(82)]
    public void The_lowest_slot_of_the_hold_and_of_the_deck_has_nothing_below(int tier) {
        // Arrange
        SlotPosition slot = Any.SlotPosition().WithTier(tier).Generate();

        // Act
        SlotPosition? below = slot.Below();

        // Assert
        Assert.Null(below);
    }

    [Theory]
    [InlineData(5, 1, 4, 5, 2, 4, false)]  // starboard 1 and port 1 are on either side of the centreline: not neighbours
    [InlineData(5, 0, 4, 5, 1, 4, true)]   // centreline and first starboard row
    [InlineData(5, 0, 4, 5, 2, 4, true)]   // centreline and first port row
    [InlineData(5, 1, 4, 5, 3, 4, true)]   // two starboard rows
    [InlineData(5, 2, 4, 5, 4, 4, true)]   // two port rows
    [InlineData(5, 1, 4, 5, 4, 4, false)]  // starboard 1 and port 2 are three transverse steps apart
    [InlineData(5, 3, 4, 5, 3, 6, true)]   // one on top of the other
    [InlineData(5, 3, 4, 5, 3, 8, false)]  // two tiers apart
    [InlineData(5, 3, 4, 5, 4, 6, false)]  // diagonal
    [InlineData(5, 3, 4, 7, 3, 6, false)]  // another bay
    [InlineData(5, 3, 20, 5, 3, 82, false)] // top of the hold and bottom of the deck: another section
    [InlineData(5, 3, 4, 5, 3, 4, false)]  // the same slot
    public void Two_slots_are_adjacent_when_they_touch_side_by_side_or_stacked_in_one_bay_and_section(int bay1, int row1, int tier1, int bay2, int row2, int tier2, bool adjacent) {
        // Arrange
        SlotPosition slot      = new SlotPosition(bay1, row1, tier1);
        SlotPosition otherSlot = new SlotPosition(bay2, row2, tier2);

        // Act & Assert
        Assert.Equal(adjacent, slot.IsAdjacentTo(otherSlot));
        Assert.Equal(adjacent, otherSlot.IsAdjacentTo(slot));
    }

    [Fact]
    public void Adjacency_to_a_null_slot_is_refused() {
        // Arrange
        SlotPosition anySlot = Any.SlotPosition().Generate();

        // Act & Assert
        Assert.Throws<ArgumentNullException>("other", () => anySlot.IsAdjacentTo(null!));
    }

    [Fact]
    public void Two_slots_with_the_same_bay_row_and_tier_are_equal_and_share_a_hash_code() {
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
    [InlineData(3, 0, 2, 4, 0, 2)]
    [InlineData(3, 0, 2, 3, 1, 2)]
    [InlineData(3, 0, 2, 3, 0, 4)]
    public void Two_slots_differing_by_one_coordinate_are_not_equal(int bay1, int row1, int tier1, int bay2, int row2, int tier2) {
        // Arrange
        SlotPosition slot      = new SlotPosition(bay1, row1, tier1);
        SlotPosition otherSlot = new SlotPosition(bay2, row2, tier2);

        // Assert
        Assert.False(slot.Equals(otherSlot));
        Assert.False(slot.Equals(null));
    }

    [Theory]
    [InlineData(1, 0, 2, "010002")]
    [InlineData(12, 3, 84, "120384")]
    [InlineData(99, 20, 98, "992098")]
    public void A_slot_is_written_as_bay_row_and_tier_on_two_digits_each(int bay, int row, int tier, string text) {
        // Act
        SlotPosition slot = new SlotPosition(bay, row, tier);

        // Assert
        Assert.Equal(text, slot.ToString());
    }

}
