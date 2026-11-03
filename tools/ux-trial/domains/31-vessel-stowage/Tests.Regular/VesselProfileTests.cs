using JustDummies;

using Xunit;

namespace VesselStowage.Domain.Tests;

public sealed class VesselProfileTests {

    // The parameters tied to another one (lightship/maximum displacement, lightship VCG/maximum VCG) are the test's own
    // literals; everything else is a dummy from the generator.
    private static VesselProfile Make(decimal lightshipTonnes = 20_000m, decimal maxDisplacementTonnes = 80_000m, double lightshipVcgMetres = 10.0, double maxVcgMetres = 15.0, double maxTcgMetres = 1.0) {
        VesselProfile anyVessel = Any.VesselProfile().Generate();

        return new VesselProfile(anyVessel.Name, anyVessel.BayCount, anyVessel.RowCount, anyVessel.HoldTiers, anyVessel.DeckTiers, anyVessel.MaxStackWeightTonnes, lightshipTonnes, lightshipVcgMetres, maxDisplacementTonnes, maxVcgMetres, maxTcgMetres);
    }

    [Fact]
    public void A_vessel_keeps_everything_it_was_built_with() {
        // Arrange
        string anyName = Any.String().NotBlank().WithMaxLength(40).Generate();

        // Act
        VesselProfile vessel = new VesselProfile(anyName, 20, 15, 8, 6, 120.5m, 21_000m, 9.5, 95_000m, 14.2, 1.1);

        // Assert
        Assert.Equal(anyName, vessel.Name);
        Assert.Equal(20, vessel.BayCount);
        Assert.Equal(15, vessel.RowCount);
        Assert.Equal(8, vessel.HoldTiers);
        Assert.Equal(6, vessel.DeckTiers);
        Assert.Equal(120.5m, vessel.MaxStackWeightTonnes);
        Assert.Equal(21_000m, vessel.LightshipTonnes);
        Assert.Equal(9.5, vessel.LightshipVcgMetres);
        Assert.Equal(95_000m, vessel.MaxDisplacementTonnes);
        Assert.Equal(14.2, vessel.MaxVcgMetres);
        Assert.Equal(1.1, vessel.MaxTcgMetres);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("\t\n")]
    public void A_blank_name_is_refused(string name) {
        // Act & Assert
        Assert.Throws<ArgumentException>("name", () => Any.VesselProfile().WithName(name).Generate());
    }

    [Fact]
    public void A_null_name_is_refused() {
        // Act & Assert
        Assert.Throws<ArgumentNullException>("name", () => Any.VesselProfile().WithName((string)null!).Generate());
    }

    [Theory]
    [InlineData(1)]
    [InlineData(99)]
    public void The_limits_of_the_bay_count_are_accepted(int bayCount) {
        // Act
        VesselProfile vessel = Any.VesselProfile().WithBayCount(bayCount).Generate();

        // Assert
        Assert.Equal(bayCount, vessel.BayCount);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(100)]
    public void A_bay_count_outside_1_to_99_is_refused(int bayCount) {
        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>("bayCount", () => Any.VesselProfile().WithBayCount(bayCount).Generate());
    }

    [Theory]
    [InlineData(1)]
    [InlineData(21)]
    public void The_limits_of_the_row_count_are_accepted(int rowCount) {
        // Act
        VesselProfile vessel = Any.VesselProfile().WithRowCount(rowCount).Generate();

        // Assert
        Assert.Equal(rowCount, vessel.RowCount);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(22)]
    public void A_row_count_outside_1_to_21_is_refused(int rowCount) {
        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>("rowCount", () => Any.VesselProfile().WithRowCount(rowCount).Generate());
    }

    [Theory]
    [InlineData(1)]
    [InlineData(10)]
    public void The_limits_of_the_hold_tier_count_are_accepted(int holdTiers) {
        // Act
        VesselProfile vessel = Any.VesselProfile().WithHoldTiers(holdTiers).Generate();

        // Assert
        Assert.Equal(holdTiers, vessel.HoldTiers);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(11)]
    public void A_hold_tier_count_outside_1_to_10_is_refused(int holdTiers) {
        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>("holdTiers", () => Any.VesselProfile().WithHoldTiers(holdTiers).Generate());
    }

    [Theory]
    [InlineData(0)]
    [InlineData(9)]
    public void A_vessel_may_have_no_deck_tier_and_up_to_nine(int deckTiers) {
        // Act
        VesselProfile vessel = Any.VesselProfile().WithDeckTiers(deckTiers).Generate();

        // Assert
        Assert.Equal(deckTiers, vessel.DeckTiers);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(10)]
    public void A_deck_tier_count_outside_0_to_9_is_refused(int deckTiers) {
        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>("deckTiers", () => Any.VesselProfile().WithDeckTiers(deckTiers).Generate());
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public void A_stack_weight_limit_that_is_not_positive_is_refused(int tonnes) {
        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>("maxStackWeightTonnes", () => Any.VesselProfile().WithMaxStackWeightTonnes(tonnes).Generate());
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public void A_lightship_weight_that_is_not_positive_is_refused(int tonnes) {
        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>("lightshipTonnes", () => Make(lightshipTonnes: tonnes, maxDisplacementTonnes: 80_000m));
    }

    [Fact]
    public void A_maximum_displacement_equal_to_the_lightship_weight_is_refused() {
        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>("maxDisplacementTonnes", () => Make(lightshipTonnes: 20_000m, maxDisplacementTonnes: 20_000m));
    }

    [Fact]
    public void A_maximum_displacement_just_above_the_lightship_weight_is_accepted() {
        // Act
        VesselProfile vessel = Make(lightshipTonnes: 20_000m, maxDisplacementTonnes: 20_000.1m);

        // Assert
        Assert.Equal(20_000.1m, vessel.MaxDisplacementTonnes);
    }

    [Theory]
    [InlineData(0.0)]
    [InlineData(-3.0)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(double.NegativeInfinity)]
    public void A_lightship_VCG_that_is_not_a_positive_finite_height_is_refused(double vcg) {
        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>("lightshipVcgMetres", () => Make(lightshipVcgMetres: vcg));
    }

    [Theory]
    [InlineData(10.0)]
    [InlineData(9.0)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    public void A_maximum_VCG_that_is_not_a_finite_height_above_the_lightship_VCG_is_refused(double vcg) {
        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>("maxVcgMetres", () => Make(lightshipVcgMetres: 10.0, maxVcgMetres: vcg));
    }

    [Theory]
    [InlineData(0.0)]
    [InlineData(-1.0)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    public void A_maximum_TCG_that_is_not_a_positive_finite_distance_is_refused(double tcg) {
        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>("maxTcgMetres", () => Make(maxTcgMetres: tcg));
    }

    // Geometry -------------------------------------------------------------------------------------------------------

    [Theory]
    [InlineData(5, 3, 2, 1, 5, 2, 2, true)]    // last bay, last row, last hold level
    [InlineData(5, 3, 2, 1, 5, 2, 82, true)]   // last bay, last row, the one deck level
    [InlineData(5, 3, 2, 1, 6, 0, 2, false)]   // beyond the last bay
    [InlineData(5, 3, 2, 1, 1, 3, 2, false)]   // beyond the last row
    [InlineData(5, 3, 2, 1, 1, 0, 6, false)]   // beyond the hold tiers
    [InlineData(5, 3, 2, 1, 1, 0, 84, false)]  // beyond the deck tiers
    [InlineData(5, 3, 2, 0, 1, 0, 82, false)]  // a vessel without deck tiers has no deck slot
    public void A_vessel_contains_the_slots_inside_its_bays_rows_and_tiers(int bays, int rows, int holdTiers, int deckTiers, int bay, int row, int tier, bool contained) {
        // Arrange
        VesselProfile vessel = Any.VesselProfile().WithBayCount(bays).WithRowCount(rows).WithHoldTiers(holdTiers).WithDeckTiers(deckTiers).Generate();

        // Act
        bool contains = vessel.Contains(new SlotPosition(bay, row, tier));

        // Assert
        Assert.Equal(contained, contains);
    }

    [Fact]
    public void A_vessel_cannot_be_asked_about_a_null_slot() {
        // Arrange
        VesselProfile anyVessel = Any.VesselProfile().Generate();

        // Act & Assert
        Assert.Throws<ArgumentNullException>("slot", () => anyVessel.Contains(null!));
    }

    [Fact]
    public void A_vessel_lists_one_slot_per_bay_row_and_tier() {
        // Arrange
        VesselProfile vessel = Any.VesselProfile().WithBayCount(4).WithRowCount(3).WithHoldTiers(2).WithDeckTiers(1).Generate();

        // Act
        List<SlotPosition> slots = vessel.AllSlots().ToList();

        // Assert
        Assert.Equal(4 * 3 * (2 + 1), slots.Count);
        Assert.Equal(slots.Count, slots.Distinct().Count());
        Assert.All(slots, slot => Assert.True(vessel.Contains(slot)));
    }

    [Fact]
    public void A_vessel_lists_its_slots_bay_by_bay_row_by_row_with_the_hold_before_the_deck() {
        // Arrange
        VesselProfile vessel = Any.VesselProfile().WithBayCount(2).WithRowCount(2).WithHoldTiers(2).WithDeckTiers(2).Generate();

        // Act
        List<string> slots = vessel.AllSlots().Select(slot => slot.ToString()).Take(8).ToList();

        // Assert
        Assert.Equal(["010002", "010004", "010082", "010084", "010102", "010104", "010182", "010184"], slots);
    }

    [Fact]
    public void A_vessel_without_deck_tiers_lists_hold_slots_only() {
        // Arrange
        VesselProfile vessel = Any.VesselProfile().WithDeckTiers(0).Generate();

        // Act & Assert
        Assert.All(vessel.AllSlots(), slot => Assert.False(slot.IsOnDeck));
    }

}
