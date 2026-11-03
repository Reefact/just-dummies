using JustDummies;
using VesselStowage.Domain.Tests.Generators;
using Xunit;

namespace VesselStowage.Domain.Tests;

public sealed class VesselProfileTests {

    // A profile built by hand, for the tests that refuse one. Every parameter starts from a valid, drawn profile;
    // a test overrides the one or two figures its refusal is about.
    private static VesselProfile Rebuild(string? name = null,
                                         int? bayCount = null,
                                         int? rowCount = null,
                                         int? holdTiers = null,
                                         int? deckTiers = null,
                                         decimal? maxStackWeightTonnes = null,
                                         decimal? lightshipTonnes = null,
                                         double? lightshipVcgMetres = null,
                                         decimal? maxDisplacementTonnes = null,
                                         double? maxVcgMetres = null,
                                         double? maxTcgMetres = null) {
        VesselProfile valid = Any.VesselProfile().Roomy().Generate();

        return new VesselProfile(name ?? valid.Name,
                                 bayCount ?? valid.BayCount,
                                 rowCount ?? valid.RowCount,
                                 holdTiers ?? valid.HoldTiers,
                                 deckTiers ?? valid.DeckTiers,
                                 maxStackWeightTonnes ?? valid.MaxStackWeightTonnes,
                                 lightshipTonnes ?? valid.LightshipTonnes,
                                 lightshipVcgMetres ?? valid.LightshipVcgMetres,
                                 maxDisplacementTonnes ?? valid.MaxDisplacementTonnes,
                                 maxVcgMetres ?? valid.MaxVcgMetres,
                                 maxTcgMetres ?? valid.MaxTcgMetres);
    }

    [Fact]
    public void A_vessel_keeps_what_its_loading_manual_says() {
        // Act
        VesselProfile vessel = new VesselProfile("MV Dummy", 24, 17, 8, 6, 120m, 18_500m, 11.2, 95_000m, 14.8, 1.5);

        // Assert
        Assert.Equal("MV Dummy", vessel.Name);
        Assert.Equal(24, vessel.BayCount);
        Assert.Equal(17, vessel.RowCount);
        Assert.Equal(8, vessel.HoldTiers);
        Assert.Equal(6, vessel.DeckTiers);
        Assert.Equal(120m, vessel.MaxStackWeightTonnes);
        Assert.Equal(18_500m, vessel.LightshipTonnes);
        Assert.Equal(11.2, vessel.LightshipVcgMetres);
        Assert.Equal(95_000m, vessel.MaxDisplacementTonnes);
        Assert.Equal(14.8, vessel.MaxVcgMetres);
        Assert.Equal(1.5, vessel.MaxTcgMetres);
    }

    [Fact]
    public void The_smallest_and_the_largest_vessel_geometries_are_accepted() {
        // Act
        VesselProfile smallest = Any.VesselProfile().WithBayCount(1).WithRowCount(1).WithHoldTiers(1).WithDeckTiers(0).Generate();
        VesselProfile largest  = Any.VesselProfile().WithBayCount(99).WithRowCount(21).WithHoldTiers(10).WithDeckTiers(9).Generate();

        // Assert
        Assert.Equal((1, 1, 1, 0), (smallest.BayCount, smallest.RowCount, smallest.HoldTiers, smallest.DeckTiers));
        Assert.Equal((99, 21, 10, 9), (largest.BayCount, largest.RowCount, largest.HoldTiers, largest.DeckTiers));
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("\t")]
    public void A_vessel_without_a_name_is_refused(string name) {
        Assert.Throws<ArgumentException>(() => Rebuild(name: name));
    }

    [Fact]
    public void A_missing_name_is_refused() {
        // Arrange
        VesselProfile anyVessel = Any.VesselProfile().Generate();

        // Assert
        Assert.Throws<ArgumentNullException>(() => new VesselProfile(null!,
                                                                     anyVessel.BayCount,
                                                                     anyVessel.RowCount,
                                                                     anyVessel.HoldTiers,
                                                                     anyVessel.DeckTiers,
                                                                     anyVessel.MaxStackWeightTonnes,
                                                                     anyVessel.LightshipTonnes,
                                                                     anyVessel.LightshipVcgMetres,
                                                                     anyVessel.MaxDisplacementTonnes,
                                                                     anyVessel.MaxVcgMetres,
                                                                     anyVessel.MaxTcgMetres));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(100)]
    [InlineData(-1)]
    public void A_bay_count_outside_1_to_99_is_refused(int bayCount) {
        Assert.Equal("bayCount", Assert.Throws<ArgumentOutOfRangeException>(() => Rebuild(bayCount: bayCount)).ParamName);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(22)]
    public void A_row_count_outside_1_to_21_is_refused(int rowCount) {
        Assert.Equal("rowCount", Assert.Throws<ArgumentOutOfRangeException>(() => Rebuild(rowCount: rowCount)).ParamName);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(11)]
    public void A_hold_depth_outside_1_to_10_tiers_is_refused(int holdTiers) {
        Assert.Equal("holdTiers", Assert.Throws<ArgumentOutOfRangeException>(() => Rebuild(holdTiers: holdTiers)).ParamName);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(10)]
    public void A_deck_height_outside_0_to_9_tiers_is_refused(int deckTiers) {
        Assert.Equal("deckTiers", Assert.Throws<ArgumentOutOfRangeException>(() => Rebuild(deckTiers: deckTiers)).ParamName);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void A_stack_weight_limit_that_is_not_positive_is_refused(int maxStackWeightTonnes) {
        Assert.Equal("maxStackWeightTonnes", Assert.Throws<ArgumentOutOfRangeException>(() => Rebuild(maxStackWeightTonnes: maxStackWeightTonnes)).ParamName);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void A_lightship_weight_that_is_not_positive_is_refused(int lightshipTonnes) {
        Assert.Equal("lightshipTonnes", Assert.Throws<ArgumentOutOfRangeException>(() => Rebuild(lightshipTonnes: lightshipTonnes)).ParamName);
    }

    [Fact]
    public void The_maximum_displacement_must_exceed_the_lightship_weight() {
        // Arrange
        decimal anyLightship = Any.Decimal().Between(1_000m, 50_000m).Generate();

        // Assert
        Assert.Equal("maxDisplacementTonnes", Assert.Throws<ArgumentOutOfRangeException>(() => Rebuild(lightshipTonnes: anyLightship, maxDisplacementTonnes: anyLightship)).ParamName);
        Assert.Equal("maxDisplacementTonnes", Assert.Throws<ArgumentOutOfRangeException>(() => Rebuild(lightshipTonnes: anyLightship, maxDisplacementTonnes: anyLightship - 1m)).ParamName);
    }

    [Fact]
    public void A_maximum_displacement_a_hair_above_the_lightship_weight_is_accepted() {
        // Arrange
        decimal anyLightship = Any.Decimal().Between(1_000m, 50_000m).WithScale(2).Generate();

        // Act
        VesselProfile vessel = Rebuild(lightshipTonnes: anyLightship, maxDisplacementTonnes: anyLightship + 0.01m);

        // Assert
        Assert.Equal(anyLightship + 0.01m, vessel.MaxDisplacementTonnes);
    }

    [Theory]
    [InlineData(0.0)]
    [InlineData(-3.0)]
    [InlineData(double.NaN)]
    [InlineData(double.NegativeInfinity)]
    [InlineData(double.PositiveInfinity)]
    public void A_lightship_VCG_that_is_not_a_positive_height_is_refused(double lightshipVcgMetres) {
        Assert.Equal("lightshipVcgMetres", Assert.Throws<ArgumentOutOfRangeException>(() => Rebuild(lightshipVcgMetres: lightshipVcgMetres)).ParamName);
    }

    [Theory]
    [InlineData(12.0)]  // equal to the lightship VCG
    [InlineData(11.9)]  // below it
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    public void The_maximum_VCG_must_be_a_finite_height_above_the_lightship_VCG(double maxVcgMetres) {
        Assert.Equal("maxVcgMetres", Assert.Throws<ArgumentOutOfRangeException>(() => Rebuild(lightshipVcgMetres: 12.0, maxVcgMetres: maxVcgMetres)).ParamName);
    }

    [Theory]
    [InlineData(0.0)]
    [InlineData(-0.5)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    public void A_maximum_TCG_that_is_not_a_finite_positive_distance_is_refused(double maxTcgMetres) {
        Assert.Equal("maxTcgMetres", Assert.Throws<ArgumentOutOfRangeException>(() => Rebuild(maxTcgMetres: maxTcgMetres)).ParamName);
    }

    // --- geometry: a 2-bay, 3-row vessel with 2 hold tiers and 1 deck tier, so every limit is a literal

    private static VesselProfile SmallVessel() {
        return Any.VesselProfile().WithBayCount(2).WithRowCount(3).WithHoldTiers(2).WithDeckTiers(1).Generate();
    }

    [Theory]
    [InlineData(1, 0, 2)]
    [InlineData(2, 2, 4)]   // last bay, last row, top of the hold
    [InlineData(1, 1, 82)]  // the only deck tier
    [InlineData(2, 2, 82)]
    public void A_slot_within_the_bays_rows_and_tiers_of_the_vessel_is_inside_it(int bay, int row, int tier) {
        Assert.True(SmallVessel().Contains(new SlotPosition(bay, row, tier)));
    }

    [Theory]
    [InlineData(3, 0, 2)]   // third bay of two
    [InlineData(1, 3, 2)]   // fourth row of three (rows run from 0)
    [InlineData(1, 0, 6)]   // third hold tier of two
    [InlineData(1, 0, 84)]  // second deck tier of one
    public void A_slot_beyond_the_bays_rows_or_tiers_of_the_vessel_is_outside_it(int bay, int row, int tier) {
        Assert.False(SmallVessel().Contains(new SlotPosition(bay, row, tier)));
    }

    [Fact]
    public void A_vessel_without_deck_tiers_has_no_slot_on_deck() {
        // Arrange
        VesselProfile deckless = Any.VesselProfile().WithBayCount(2).WithRowCount(3).WithHoldTiers(2).WithDeckTiers(0).Generate();

        // Assert
        Assert.False(deckless.Contains(new SlotPosition(1, 0, 82)));
        Assert.DoesNotContain(deckless.AllSlots(), slot => slot.IsOnDeck);
    }

    [Fact]
    public void Contains_refuses_a_missing_slot() {
        Assert.Throws<ArgumentNullException>(() => SmallVessel().Contains(null!));
    }

    [Fact]
    public void AllSlots_lists_each_slot_of_the_vessel_once_bay_by_bay_row_by_row_from_the_bottom_up() {
        // Arrange
        VesselProfile vessel = Any.VesselProfile().WithBayCount(2).WithRowCount(2).WithHoldTiers(2).WithDeckTiers(1).Generate();

        // Act
        List<string> slots = vessel.AllSlots().Select(slot => slot.ToString()).ToList();

        // Assert
        Assert.Equal(new[] {
            "010002", "010004", "010082", "010102", "010104", "010182",
            "020002", "020004", "020082", "020102", "020104", "020182",
        }, slots);
    }

    [Fact]
    public void Every_slot_listed_is_inside_the_vessel_and_there_are_as_many_as_bays_times_rows_times_tiers() {
        // Arrange
        VesselProfile vessel = Any.VesselProfile().Generate();

        // Act
        List<SlotPosition> slots = vessel.AllSlots().ToList();

        // Assert
        Assert.All(slots, slot => Assert.True(vessel.Contains(slot)));
        Assert.Equal(vessel.BayCount * vessel.RowCount * (vessel.HoldTiers + vessel.DeckTiers), slots.Count);
        Assert.Equal(slots.Count, slots.Distinct().Count());
    }

}
