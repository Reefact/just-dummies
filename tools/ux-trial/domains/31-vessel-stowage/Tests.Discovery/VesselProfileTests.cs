using VesselStowage.Domain;

namespace VesselStowage.Tests;

public sealed class VesselProfileTests {

    /// <summary>A valid profile by its constructor, with one argument replaced by the test.</summary>
    private static VesselProfile Build(string name = "MV Dummy",
                                       int bays = 10, int rows = 5, int holdTiers = 4, int deckTiers = 3,
                                       decimal stackLimit = 100m, decimal lightship = 10_000m, double lightshipVcg = 10.0,
                                       decimal maxDisplacement = 50_000m, double maxVcg = 20.0, double maxTcg = 2.0) {
        return new VesselProfile(name, bays, rows, holdTiers, deckTiers, stackLimit, lightship, lightshipVcg, maxDisplacement, maxVcg, maxTcg);
    }

    [Fact]
    public void A_profile_keeps_what_it_was_built_with() {
        VesselProfile vessel = Build("MV Example", 12, 6, 5, 4, 90m, 12_000m, 9.5, 60_000m, 18.5, 1.5);

        Assert.Equal("MV Example", vessel.Name);
        Assert.Equal(12, vessel.BayCount);
        Assert.Equal(6, vessel.RowCount);
        Assert.Equal(5, vessel.HoldTiers);
        Assert.Equal(4, vessel.DeckTiers);
        Assert.Equal(90m, vessel.MaxStackWeightTonnes);
        Assert.Equal(12_000m, vessel.LightshipTonnes);
        Assert.Equal(9.5, vessel.LightshipVcgMetres);
        Assert.Equal(60_000m, vessel.MaxDisplacementTonnes);
        Assert.Equal(18.5, vessel.MaxVcgMetres);
        Assert.Equal(1.5, vessel.MaxTcgMetres);
    }

    [Fact]
    public void A_generated_profile_is_always_accepted_by_its_constructor() {
        List<VesselProfile> anyProfiles = Any.ListOf(new AnyVesselProfile()).WithCount(200).Generate();

        Assert.Equal(200, anyProfiles.Count);
    }

    [Fact]
    public void A_generated_profile_displaces_more_fully_loaded_than_empty() {
        VesselProfile anyVessel = Any.VesselProfile().Generate();

        Assert.True(anyVessel.MaxDisplacementTonnes > anyVessel.LightshipTonnes);
        Assert.True(anyVessel.MaxVcgMetres > anyVessel.LightshipVcgMetres);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void A_blank_name_is_refused(string? name) {
        Assert.ThrowsAny<ArgumentException>(() => Build(name: name!));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(100)]
    public void A_bay_count_outside_1_to_99_is_refused(int bays) {
        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(() => Build(bays: bays));

        Assert.Equal("bayCount", refusal.ParamName);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(22)]
    public void A_row_count_outside_1_to_21_is_refused(int rows) {
        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(() => Build(rows: rows));

        Assert.Equal("rowCount", refusal.ParamName);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(11)]
    public void A_hold_tier_count_outside_1_to_10_is_refused(int holdTiers) {
        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(() => Build(holdTiers: holdTiers));

        Assert.Equal("holdTiers", refusal.ParamName);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(10)]
    public void A_deck_tier_count_outside_0_to_9_is_refused(int deckTiers) {
        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(() => Build(deckTiers: deckTiers));

        Assert.Equal("deckTiers", refusal.ParamName);
    }

    [Theory]
    [InlineData(1, 1, 0)]
    [InlineData(99, 21, 9)]
    public void The_extreme_geometries_are_accepted(int bays, int rows, int deckTiers) {
        VesselProfile vessel = Build(bays: bays, rows: rows, holdTiers: 10, deckTiers: deckTiers);

        Assert.Equal(bays, vessel.BayCount);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public void A_stack_weight_limit_that_is_not_positive_is_refused(decimal limit) {
        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(() => Build(stackLimit: limit));

        Assert.Equal("maxStackWeightTonnes", refusal.ParamName);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void A_lightship_weight_that_is_not_positive_is_refused(decimal lightship) {
        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(() => Build(lightship: lightship, maxDisplacement: 1000m));

        Assert.Equal("lightshipTonnes", refusal.ParamName);
    }

    [Theory]
    [InlineData(10_000)]
    [InlineData(9_999)]
    public void A_maximum_displacement_that_does_not_exceed_the_lightship_weight_is_refused(decimal maxDisplacement) {
        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(() => Build(lightship: 10_000m, maxDisplacement: maxDisplacement));

        Assert.Equal("maxDisplacementTonnes", refusal.ParamName);
    }

    [Fact]
    public void A_maximum_displacement_just_above_the_lightship_weight_is_accepted() {
        Assert.Equal(10_000.01m, Build(lightship: 10_000m, maxDisplacement: 10_000.01m).MaxDisplacementTonnes);
    }

    [Theory]
    [InlineData(0.0)]
    [InlineData(-1.0)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(double.NegativeInfinity)]
    public void A_lightship_VCG_that_is_not_a_positive_finite_height_is_refused(double vcg) {
        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(() => Build(lightshipVcg: vcg, maxVcg: 20.0));

        Assert.Equal("lightshipVcgMetres", refusal.ParamName);
    }

    [Theory]
    [InlineData(10.0)]
    [InlineData(9.0)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    public void A_maximum_VCG_that_is_not_a_finite_height_above_the_lightship_VCG_is_refused(double maxVcg) {
        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(() => Build(lightshipVcg: 10.0, maxVcg: maxVcg));

        Assert.Equal("maxVcgMetres", refusal.ParamName);
    }

    [Theory]
    [InlineData(0.0)]
    [InlineData(-0.5)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    public void A_maximum_TCG_that_is_not_a_positive_finite_distance_is_refused(double maxTcg) {
        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(() => Build(maxTcg: maxTcg));

        Assert.Equal("maxTcgMetres", refusal.ParamName);
    }

    [Fact]
    public void A_slot_inside_the_geometry_is_contained() {
        VesselProfile vessel = Build(bays: 10, rows: 5, holdTiers: 4, deckTiers: 3);

        Assert.True(vessel.Contains(new SlotPosition(10, 4, 8)));
        Assert.True(vessel.Contains(new SlotPosition(1, 0, 2)));
        Assert.True(vessel.Contains(new SlotPosition(10, 4, 86)));
    }

    [Fact]
    public void A_slot_beyond_the_last_bay_row_or_tier_is_not_contained() {
        VesselProfile vessel = Build(bays: 10, rows: 5, holdTiers: 4, deckTiers: 3);

        Assert.False(vessel.Contains(new SlotPosition(11, 0, 2)));
        Assert.False(vessel.Contains(new SlotPosition(1, 5, 2)));
        Assert.False(vessel.Contains(new SlotPosition(1, 0, 10)));
        Assert.False(vessel.Contains(new SlotPosition(1, 0, 88)));
    }

    [Fact]
    public void A_vessel_without_deck_tiers_has_no_deck_slot() {
        VesselProfile vessel = Build(deckTiers: 0);

        Assert.False(vessel.Contains(new SlotPosition(1, 0, 82)));
    }

    [Fact]
    public void Containment_of_null_is_refused() {
        Assert.Throws<ArgumentNullException>(() => Build().Contains(null!));
    }

    [Fact]
    public void Every_slot_is_listed_once_and_contained() {
        VesselProfile vessel = Build(bays: 3, rows: 2, holdTiers: 2, deckTiers: 1);

        List<SlotPosition> slots = vessel.AllSlots().ToList();

        Assert.Equal(3 * 2 * (2 + 1), slots.Count);
        Assert.Equal(slots.Count, slots.Distinct().Count());
        Assert.All(slots, slot => Assert.True(vessel.Contains(slot)));
    }

    [Fact]
    public void The_slot_count_is_bays_times_rows_times_all_tiers() {
        VesselProfile anyVessel = Any.VesselProfile().Generate();

        int count = anyVessel.AllSlots().Count();

        Assert.Equal(anyVessel.BayCount * anyVessel.RowCount * (anyVessel.HoldTiers + anyVessel.DeckTiers), count);
    }

}

public sealed class StowageAssignmentTests {

    [Fact]
    public void A_twenty_foot_box_goes_in_an_odd_bay() {
        Container twentyFooter = Any.Container().WithSize(ContainerSize.TwentyFoot).Generate();
        SlotPosition oddBaySlot = new SlotPosition(3, 1, 2);

        StowageAssignment assignment = new StowageAssignment(twentyFooter, oddBaySlot);

        Assert.Same(twentyFooter, assignment.Container);
        Assert.Same(oddBaySlot, assignment.Slot);
    }

    [Fact]
    public void A_forty_foot_box_goes_in_an_even_bay() {
        Container fortyFooter = Any.Container().WithSize(ContainerSize.FortyFoot).Generate();

        Assert.NotNull(new StowageAssignment(fortyFooter, new SlotPosition(4, 1, 2)));
    }

    [Fact]
    public void A_twenty_foot_box_in_an_even_bay_is_refused() {
        Container twentyFooter = Any.Container().WithSize(ContainerSize.TwentyFoot).Generate();

        ArgumentException refusal = Assert.Throws<ArgumentException>(() => new StowageAssignment(twentyFooter, new SlotPosition(4, 1, 2)));

        Assert.Equal("slot", refusal.ParamName);
        Assert.Contains("Bay 4", refusal.Message);
    }

    [Fact]
    public void A_forty_foot_box_in_an_odd_bay_is_refused() {
        Container fortyFooter = Any.Container().WithSize(ContainerSize.FortyFoot).Generate();

        Assert.Throws<ArgumentException>(() => new StowageAssignment(fortyFooter, new SlotPosition(3, 1, 2)));
    }

    [Fact]
    public void Null_arguments_are_refused() {
        Container anyContainer = Any.Container().Generate();
        SlotPosition anySlot   = Any.SlotPosition().Generate();

        Assert.Throws<ArgumentNullException>(() => new StowageAssignment(null!, anySlot));
        Assert.Throws<ArgumentNullException>(() => new StowageAssignment(anyContainer, null!));
    }

    [Fact]
    public void A_generated_assignment_always_matches_the_size_to_the_bay() {
        List<StowageAssignment> anyAssignments = Any.ListOf(new AnyStowageAssignment()).WithCount(200).Generate();

        Assert.Equal(200, anyAssignments.Count);
    }

    [Fact]
    public void A_generated_assignment_around_a_pinned_container_follows_its_size() {
        Container fortyFooter = Any.Container().WithSize(ContainerSize.FortyFoot).Generate();

        StowageAssignment anyAssignment = Any.StowageAssignment().WithContainer(fortyFooter).Generate();

        Assert.Equal(0, anyAssignment.Slot.Bay % 2);
    }

    [Fact]
    public void A_generated_assignment_around_a_pinned_slot_follows_its_bay() {
        SlotPosition oddBaySlot = new SlotPosition(7, 2, 4);

        StowageAssignment anyAssignment = Any.StowageAssignment().WithSlot(oddBaySlot).Generate();

        Assert.Equal(ContainerSize.TwentyFoot, anyAssignment.Container.Size);
    }

}
