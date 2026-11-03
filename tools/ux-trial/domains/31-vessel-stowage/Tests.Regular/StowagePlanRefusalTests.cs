using JustDummies;

using Xunit;

namespace VesselStowage.Domain.Tests;

/// <summary>The refusals of <see cref="StowagePlan.Assign" />, one violation at a time, then the order they are checked in.</summary>
public sealed class StowagePlanRefusalTests {

    // A refusal is the same through Check and through Assign, and Assign leaves the plan as it was.
    private static void AssertRefused(PlanScenario scenario, StowageViolation expected, Container container, int bay, int row, int tier) {
        SlotPosition slot  = new SlotPosition(bay, row, tier);
        int assignedBefore = scenario.Plan.Assignments.Count;

        StowageViolation checkedViolation = scenario.Plan.Check(container, slot);
        StowageRefusedException refusal   = Assert.Throws<StowageRefusedException>(() => scenario.Plan.Assign(container, slot));

        Assert.Equal(expected, checkedViolation);
        Assert.Equal(expected, refusal.Violation);
        Assert.Equal(assignedBefore, scenario.Plan.Assignments.Count);
    }

    // The slot is outside the vessel -----------------------------------------------------------------------------------

    [Theory]
    [InlineData(5, 0, 2)]    // beyond the last bay
    [InlineData(3, 3, 2)]    // beyond the last row
    [InlineData(3, 0, 6)]    // beyond the last hold tier
    [InlineData(3, 0, 84)]   // beyond the last deck tier
    public void A_slot_outside_the_vessels_geometry_is_refused(int bay, int row, int tier) {
        // Arrange
        PlanScenario scenario = new PlanScenario(PlanScenario.VesselOf(bays: 4, rows: 3, holdTiers: 2, deckTiers: 1));
        Container anyBox      = scenario.Teu();

        // Act & Assert
        AssertRefused(scenario, StowageViolation.SlotOutsideVessel, anyBox, bay, row, tier);
    }

    [Fact]
    public void A_vessel_without_deck_tiers_has_no_deck_slot() {
        // Arrange
        PlanScenario scenario = new PlanScenario(PlanScenario.VesselOf(deckTiers: 0));
        Container anyBox      = scenario.Teu();

        // Act & Assert
        AssertRefused(scenario, StowageViolation.SlotOutsideVessel, anyBox, 1, 0, 82);
    }

    // The bay does not take the size -----------------------------------------------------------------------------------

    [Fact]
    public void A_twenty_foot_box_in_an_even_bay_is_refused() {
        // Arrange
        PlanScenario scenario = new PlanScenario();

        // Act & Assert
        AssertRefused(scenario, StowageViolation.BayDoesNotFitSize, scenario.Teu(), 2, 0, 2);
    }

    [Fact]
    public void A_forty_foot_box_in_an_odd_bay_is_refused() {
        // Arrange
        PlanScenario scenario = new PlanScenario();

        // Act & Assert
        AssertRefused(scenario, StowageViolation.BayDoesNotFitSize, scenario.Feu(), 1, 0, 2);
    }

    // The container is already stowed / the slot is taken --------------------------------------------------------------

    [Fact]
    public void A_container_cannot_be_stowed_twice() {
        // Arrange
        PlanScenario scenario = new PlanScenario();
        Container anyBox      = scenario.Teu();
        scenario.Stow(anyBox, 1, 0, 2);

        // Act & Assert
        AssertRefused(scenario, StowageViolation.AlreadyStowed, anyBox, 1, 1, 2);
    }

    [Fact]
    public void A_slot_holds_a_single_box() {
        // Arrange
        PlanScenario scenario = new PlanScenario();
        scenario.Stow(scenario.Teu(), 1, 0, 2);

        // Act & Assert
        AssertRefused(scenario, StowageViolation.SlotOccupied, scenario.Teu(), 1, 0, 2);
    }

    // The ports ---------------------------------------------------------------------------------------------------------

    [Fact]
    public void A_box_loaded_at_a_port_the_rotation_does_not_call_is_refused() {
        // Arrange
        PlanScenario scenario = new PlanScenario();
        Container box         = Any.Container().WithSize(ContainerSize.TwentyFoot).WithHazard(HazardClass.None)
                                   .WithPortOfLoading(scenario.ForeignPort()).WithPortOfDischarge(scenario.Port(2)).Generate();

        // Act & Assert
        AssertRefused(scenario, StowageViolation.PortNotInRotation, box, 1, 0, 2);
    }

    [Fact]
    public void A_box_discharged_at_a_port_the_rotation_does_not_call_is_refused() {
        // Arrange
        PlanScenario scenario = new PlanScenario();
        Container box         = Any.Container().WithSize(ContainerSize.TwentyFoot).WithHazard(HazardClass.None)
                                   .WithPortOfLoading(scenario.Port(1)).WithPortOfDischarge(scenario.ForeignPort()).Generate();

        // Act & Assert
        AssertRefused(scenario, StowageViolation.PortNotInRotation, box, 1, 0, 2);
    }

    [Theory]
    [InlineData(2, 1)]
    [InlineData(3, 0)]
    public void A_box_that_would_be_discharged_before_it_is_loaded_is_refused(int loadsAt, int dischargesAt) {
        // Arrange
        PlanScenario scenario = new PlanScenario();

        // Act & Assert
        AssertRefused(scenario, StowageViolation.PortNotInRotation, scenario.Teu(loadsAt, dischargesAt), 1, 0, 2);
    }

    [Theory]
    [InlineData(0, 1)]
    [InlineData(1, 3)]
    [InlineData(2, 3)]
    public void A_box_loaded_before_it_is_discharged_is_accepted_between_any_two_ports(int loadsAt, int dischargesAt) {
        // Arrange
        PlanScenario scenario = new PlanScenario();

        // Act
        StowageAssignment assignment = scenario.Stow(scenario.Teu(loadsAt, dischargesAt), 1, 0, 2);

        // Assert
        Assert.Equal(scenario.Port(loadsAt), assignment.Container.PortOfLoading);
        Assert.Equal(scenario.Port(dischargesAt), assignment.Container.PortOfDischarge);
    }

    // Explosives --------------------------------------------------------------------------------------------------------

    [Fact]
    public void Explosives_under_deck_are_refused() {
        // Arrange
        PlanScenario scenario = new PlanScenario();
        Container explosives  = scenario.DangerousBox(ContainerSize.TwentyFoot, HazardClass.Explosives);

        // Act & Assert
        AssertRefused(scenario, StowageViolation.ExplosivesUnderDeck, explosives, 1, 0, 2);
    }

    [Fact]
    public void Explosives_on_deck_are_accepted() {
        // Arrange
        PlanScenario scenario = new PlanScenario();
        Container explosives  = scenario.DangerousBox(ContainerSize.TwentyFoot, HazardClass.Explosives);

        // Act
        StowageAssignment assignment = scenario.Stow(explosives, 1, 0, 82);

        // Assert
        Assert.True(assignment.Slot.IsOnDeck);
    }

    [Theory]
    [InlineData(HazardClass.Gases)]
    [InlineData(HazardClass.Radioactive)]
    [InlineData(HazardClass.Corrosives)]
    public void The_other_dangerous_goods_are_accepted_under_deck(HazardClass hazard) {
        // Arrange
        PlanScenario scenario = new PlanScenario();

        // Act
        StowageAssignment assignment = scenario.Stow(scenario.DangerousBox(ContainerSize.TwentyFoot, hazard), 1, 0, 2);

        // Assert
        Assert.False(assignment.Slot.IsOnDeck);
    }

    // Support -----------------------------------------------------------------------------------------------------------

    [Theory]
    [InlineData(1, 0, 4)]    // hold, nothing at tier 2
    [InlineData(1, 0, 84)]   // deck, nothing at tier 82
    public void A_box_needs_a_box_in_the_tier_below(int bay, int row, int tier) {
        // Arrange
        PlanScenario scenario = new PlanScenario();

        // Act & Assert
        AssertRefused(scenario, StowageViolation.NoSupportBelow, scenario.Teu(), bay, row, tier);
    }

    [Fact]
    public void A_box_cannot_float_above_an_empty_tier() {
        // Arrange
        PlanScenario scenario = new PlanScenario();
        scenario.Stow(scenario.Teu(grossTonnes: 20m), 1, 0, 2);

        // Act & Assert
        AssertRefused(scenario, StowageViolation.NoSupportBelow, scenario.Teu(grossTonnes: 10m), 1, 0, 6);
    }

    [Fact]
    public void The_support_must_stand_in_the_same_row_not_next_to_it() {
        // Arrange
        PlanScenario scenario = new PlanScenario();
        scenario.Stow(scenario.Teu(grossTonnes: 20m), 1, 1, 2);

        // Act & Assert
        AssertRefused(scenario, StowageViolation.NoSupportBelow, scenario.Teu(grossTonnes: 10m), 1, 0, 4);
    }

    [Fact]
    public void The_lowest_deck_tier_does_not_need_the_hold_below_it() {
        // Arrange
        PlanScenario scenario = new PlanScenario();

        // Act
        StowageViolation violation = scenario.Plan.Check(scenario.Teu(), new SlotPosition(1, 0, 82));

        // Assert
        Assert.Equal(StowageViolation.None, violation);
    }

    // Weight on weight --------------------------------------------------------------------------------------------------

    [Theory]
    [InlineData(20.01)]
    [InlineData(30.0)]
    public void A_box_heavier_than_the_one_it_stands_on_is_refused(double upperGross) {
        // Arrange
        PlanScenario scenario = new PlanScenario();
        scenario.Stow(scenario.Teu(grossTonnes: 20m), 1, 0, 2);

        // Act & Assert
        AssertRefused(scenario, StowageViolation.HeavierThanContainerBelow, scenario.Teu(grossTonnes: (decimal)upperGross), 1, 0, 4);
    }

    [Theory]
    [InlineData(20.0)]
    [InlineData(19.99)]
    [InlineData(5.0)]
    public void A_box_as_heavy_as_or_lighter_than_the_one_it_stands_on_is_accepted(double upperGross) {
        // Arrange
        PlanScenario scenario = new PlanScenario();
        scenario.Stow(scenario.Teu(grossTonnes: 20m), 1, 0, 2);

        // Act
        StowageAssignment assignment = scenario.Stow(scenario.Teu(grossTonnes: (decimal)upperGross), 1, 0, 4);

        // Assert
        Assert.Equal((decimal)upperGross, assignment.Container.GrossMassTonnes);
    }

    [Fact]
    public void A_stack_over_the_weight_limit_of_the_vessel_is_refused() {
        // Arrange
        PlanScenario scenario = new PlanScenario(PlanScenario.VesselOf(holdTiers: 3, maxStackWeightTonnes: 50m));
        scenario.Stow(scenario.Teu(grossTonnes: 20m), 1, 0, 2);
        scenario.Stow(scenario.Teu(grossTonnes: 20m), 1, 0, 4);

        // Act & Assert
        AssertRefused(scenario, StowageViolation.StackWeightExceeded, scenario.Teu(grossTonnes: 10.01m), 1, 0, 6);
    }

    [Fact]
    public void A_stack_exactly_at_the_weight_limit_of_the_vessel_is_accepted() {
        // Arrange
        PlanScenario scenario = new PlanScenario(PlanScenario.VesselOf(holdTiers: 3, maxStackWeightTonnes: 50m));
        scenario.Stow(scenario.Teu(grossTonnes: 20m), 1, 0, 2);
        scenario.Stow(scenario.Teu(grossTonnes: 20m), 1, 0, 4);

        // Act
        StowageAssignment assignment = scenario.Stow(scenario.Teu(grossTonnes: 10m), 1, 0, 6);

        // Assert
        Assert.Equal(50m, 20m + 20m + assignment.Container.GrossMassTonnes);
    }

    [Fact]
    public void The_weight_limit_applies_to_each_stack_on_its_own() {
        // Arrange
        PlanScenario scenario = new PlanScenario(PlanScenario.VesselOf(holdTiers: 3, maxStackWeightTonnes: 50m));
        scenario.Stow(scenario.Teu(grossTonnes: 20m), 1, 0, 2);
        scenario.Stow(scenario.Teu(grossTonnes: 20m), 1, 0, 4);

        // Act: another row, another bay, and the deck above the same row are each a stack of their own
        StowageAssignment nextRow  = scenario.Stow(scenario.Teu(grossTonnes: 30m), 1, 2, 2);
        StowageAssignment nextBay  = scenario.Stow(scenario.Teu(grossTonnes: 30m), 3, 0, 2);
        StowageAssignment onDeck   = scenario.Stow(scenario.Teu(grossTonnes: 30m), 1, 0, 82);

        // Assert
        Assert.Equal(5, scenario.Plan.Assignments.Count);
        Assert.NotNull(nextRow);
        Assert.NotNull(nextBay);
        Assert.NotNull(onDeck);
    }

    [Fact]
    public void A_single_box_heavier_than_the_stack_limit_is_refused_even_on_an_empty_slot() {
        // Arrange
        PlanScenario scenario = new PlanScenario(PlanScenario.VesselOf(maxStackWeightTonnes: 25m));

        // Act & Assert
        AssertRefused(scenario, StowageViolation.StackWeightExceeded, scenario.Teu(grossTonnes: 25.01m), 1, 0, 2);
    }

    // Sequence ----------------------------------------------------------------------------------------------------------

    // The lower box loads at port 1 and is discharged at port 2: nothing above it may load earlier or discharge later.
    [Theory]
    [InlineData(0, 2)]   // loaded earlier than the box under it
    [InlineData(1, 3)]   // discharged later than the box under it
    [InlineData(0, 3)]   // both
    public void A_box_that_would_have_to_be_lifted_after_the_box_under_it_is_an_overstow(int upperLoadsAt, int upperDischargesAt) {
        // Arrange
        PlanScenario scenario = new PlanScenario();
        scenario.Stow(scenario.Teu(loadsAt: 1, dischargesAt: 2, grossTonnes: 20m), 1, 0, 2);

        // Act & Assert
        AssertRefused(scenario, StowageViolation.Overstow, scenario.Teu(upperLoadsAt, upperDischargesAt, grossTonnes: 10m), 1, 0, 4);
    }

    [Theory]
    [InlineData(0, 3)]
    [InlineData(1, 2)]
    [InlineData(0, 1)]
    [InlineData(2, 3)]
    public void A_box_loaded_no_earlier_and_discharged_no_later_than_the_box_under_it_is_accepted(int upperLoadsAt, int upperDischargesAt) {
        // Arrange
        PlanScenario scenario = new PlanScenario();
        scenario.Stow(scenario.Teu(loadsAt: 0, dischargesAt: 3, grossTonnes: 20m), 1, 0, 2);

        // Act
        StowageAssignment assignment = scenario.Stow(scenario.Teu(upperLoadsAt, upperDischargesAt, grossTonnes: 10m), 1, 0, 4);

        // Assert
        Assert.Equal(new SlotPosition(1, 0, 4), assignment.Slot);
    }

    [Fact]
    public void The_sequence_of_neighbouring_stacks_does_not_matter() {
        // Arrange
        PlanScenario scenario = new PlanScenario();
        scenario.Stow(scenario.Teu(loadsAt: 1, dischargesAt: 2, grossTonnes: 20m), 1, 0, 2);

        // Act
        StowageAssignment sideBySide = scenario.Stow(scenario.Teu(loadsAt: 0, dischargesAt: 3, grossTonnes: 20m), 1, 2, 2);

        // Assert
        Assert.Equal(new SlotPosition(1, 2, 2), sideBySide.Slot);
    }

    // Hatch cover: the deck boxes of a bay must be lifted before (and loaded after) the hold boxes of the same bay.

    [Theory]
    [InlineData(0, 2)]   // the deck box is loaded earlier than the hold box
    [InlineData(1, 3)]   // the deck box is discharged later than the hold box
    public void A_deck_box_that_would_block_the_hatch_over_a_hold_box_of_its_bay_is_refused(int deckLoadsAt, int deckDischargesAt) {
        // Arrange
        PlanScenario scenario = new PlanScenario();
        scenario.Stow(scenario.Teu(loadsAt: 1, dischargesAt: 2), 1, 0, 2);

        // Act & Assert
        AssertRefused(scenario, StowageViolation.HatchAccessBlocked, scenario.Teu(deckLoadsAt, deckDischargesAt), 1, 2, 82);
    }

    [Theory]
    [InlineData(2, 3)]   // the hold box is loaded after the deck box
    [InlineData(0, 1)]   // the hold box is discharged before the deck box
    public void A_hold_box_that_would_sit_under_a_deck_box_of_its_bay_it_cannot_be_reached_from_is_refused(int holdLoadsAt, int holdDischargesAt) {
        // Arrange: the deck box is stowed first this time
        PlanScenario scenario = new PlanScenario();
        scenario.Stow(scenario.Teu(loadsAt: 1, dischargesAt: 2), 1, 2, 82);

        // Act & Assert: the hold box would have to be reached through the hatch cover of that deck box
        AssertRefused(scenario, StowageViolation.HatchAccessBlocked, scenario.Teu(holdLoadsAt, holdDischargesAt), 1, 0, 2);
    }

    [Theory]
    [InlineData(0, 3, 1, 2)]   // hold box, then a deck box inside its voyage
    [InlineData(0, 3, 0, 3)]
    [InlineData(1, 3, 1, 2)]
    public void A_deck_box_loaded_no_earlier_and_discharged_no_later_than_the_hold_boxes_of_its_bay_is_accepted(int holdLoadsAt, int holdDischargesAt, int deckLoadsAt, int deckDischargesAt) {
        // Arrange
        PlanScenario scenario = new PlanScenario();
        scenario.Stow(scenario.Teu(holdLoadsAt, holdDischargesAt), 1, 0, 2);

        // Act
        StowageAssignment assignment = scenario.Stow(scenario.Teu(deckLoadsAt, deckDischargesAt), 1, 2, 82);

        // Assert
        Assert.True(assignment.Slot.IsOnDeck);
    }

    [Fact]
    public void The_hatch_cover_of_another_bay_is_not_concerned() {
        // Arrange
        PlanScenario scenario = new PlanScenario();
        scenario.Stow(scenario.Teu(loadsAt: 1, dischargesAt: 2), 1, 0, 2);

        // Act: the deck box of bay 3 would block bay 1's hatch cover if it were stowed in bay 1
        StowageAssignment otherBay = scenario.Stow(scenario.Teu(loadsAt: 0, dischargesAt: 3), 3, 0, 82);

        // Assert
        Assert.Equal(3, otherBay.Slot.Bay);
    }

    // Dangerous goods ----------------------------------------------------------------------------------------------------

    [Theory]
    [InlineData(1, 1, 82)]   // side by side, starboard
    [InlineData(1, 2, 82)]   // side by side, port
    [InlineData(1, 0, 84)]   // on top of it
    public void Explosives_do_not_touch_other_dangerous_goods_of_their_bay_and_section(int bay, int row, int tier) {
        // Arrange
        PlanScenario scenario = new PlanScenario();
        scenario.Stow(scenario.DangerousBox(ContainerSize.TwentyFoot, HazardClass.Explosives, grossTonnes: 20m), 1, 0, 82);

        // Act & Assert
        AssertRefused(scenario, StowageViolation.SegregationConflict, scenario.DangerousBox(ContainerSize.TwentyFoot, HazardClass.Gases, grossTonnes: 10m), bay, row, tier);
    }

    [Theory]
    [InlineData(1, 3, 82)]   // two rows away
    [InlineData(1, 1, 2)]    // the hold of the same bay is another section
    [InlineData(3, 0, 82)]   // another bay
    public void Explosives_may_have_dangerous_goods_beyond_their_immediate_neighbours(int bay, int row, int tier) {
        // Arrange
        PlanScenario scenario = new PlanScenario();
        scenario.Stow(scenario.DangerousBox(ContainerSize.TwentyFoot, HazardClass.Explosives, grossTonnes: 20m), 1, 0, 82);

        // Act
        StowageAssignment assignment = scenario.Stow(scenario.DangerousBox(ContainerSize.TwentyFoot, HazardClass.Gases, grossTonnes: 10m), bay, row, tier);

        // Assert
        Assert.Equal(new SlotPosition(bay, row, tier), assignment.Slot);
    }

    [Fact]
    public void A_box_diagonal_to_explosives_is_not_a_neighbour() {
        // Arrange
        PlanScenario scenario = new PlanScenario();
        scenario.Stow(scenario.DangerousBox(ContainerSize.TwentyFoot, HazardClass.Explosives, grossTonnes: 20m), 1, 0, 82);
        scenario.Stow(scenario.Teu(grossTonnes: 20m), 1, 1, 82);

        // Act
        StowageAssignment diagonal = scenario.Stow(scenario.DangerousBox(ContainerSize.TwentyFoot, HazardClass.Gases, grossTonnes: 10m), 1, 1, 84);

        // Assert
        Assert.Equal(new SlotPosition(1, 1, 84), diagonal.Slot);
    }

    [Fact]
    public void General_cargo_may_stand_next_to_explosives() {
        // Arrange
        PlanScenario scenario = new PlanScenario();
        scenario.Stow(scenario.DangerousBox(ContainerSize.TwentyFoot, HazardClass.Explosives, grossTonnes: 20m), 1, 0, 82);

        // Act
        StowageAssignment neighbour = scenario.Stow(scenario.Teu(grossTonnes: 10m), 1, 1, 82);

        // Assert
        Assert.Equal(new SlotPosition(1, 1, 82), neighbour.Slot);
    }

    [Theory]
    [InlineData(HazardClass.FlammableLiquids, HazardClass.Oxidizers)]
    [InlineData(HazardClass.FlammableSolids, HazardClass.Oxidizers)]
    [InlineData(HazardClass.Oxidizers, HazardClass.FlammableLiquids)]
    [InlineData(HazardClass.Oxidizers, HazardClass.FlammableSolids)]
    public void Flammables_and_oxidizers_are_kept_apart_whichever_is_stowed_first(HazardClass first, HazardClass second) {
        // Arrange
        PlanScenario scenario = new PlanScenario();
        scenario.Stow(scenario.DangerousBox(ContainerSize.TwentyFoot, first, grossTonnes: 20m), 1, 0, 2);

        // Act & Assert
        AssertRefused(scenario, StowageViolation.SegregationConflict, scenario.DangerousBox(ContainerSize.TwentyFoot, second, grossTonnes: 10m), 1, 1, 2);
    }

    [Fact]
    public void The_first_starboard_row_and_the_first_port_row_are_not_neighbours() {
        // Arrange
        PlanScenario scenario = new PlanScenario();
        scenario.Stow(scenario.DangerousBox(ContainerSize.TwentyFoot, HazardClass.FlammableLiquids, grossTonnes: 20m), 1, 1, 2);

        // Act
        StowageAssignment otherSide = scenario.Stow(scenario.DangerousBox(ContainerSize.TwentyFoot, HazardClass.Oxidizers, grossTonnes: 20m), 1, 2, 2);

        // Assert
        Assert.Equal(new SlotPosition(1, 2, 2), otherSide.Slot);
    }

    [Fact]
    public void Two_flammables_and_a_flammable_with_other_dangerous_goods_may_touch() {
        // Arrange
        PlanScenario scenario = new PlanScenario();
        scenario.Stow(scenario.DangerousBox(ContainerSize.TwentyFoot, HazardClass.FlammableLiquids, grossTonnes: 20m), 1, 0, 2);
        scenario.Stow(scenario.DangerousBox(ContainerSize.TwentyFoot, HazardClass.FlammableSolids, grossTonnes: 20m), 1, 1, 2);

        // Act
        StowageAssignment third = scenario.Stow(scenario.DangerousBox(ContainerSize.TwentyFoot, HazardClass.Toxic, grossTonnes: 20m), 1, 2, 2);

        // Assert
        Assert.Equal(3, scenario.Plan.Assignments.Count);
        Assert.Equal(new SlotPosition(1, 2, 2), third.Slot);
    }

    // The order the rules are checked in ------------------------------------------------------------------------------

    [Fact]
    public void A_slot_outside_the_vessel_is_reported_before_a_bay_that_does_not_take_the_size() {
        // Arrange
        PlanScenario scenario = new PlanScenario(PlanScenario.VesselOf(bays: 4));

        // Act & Assert: bay 5 is odd and past the last bay, and the box is a forty-foot one
        AssertRefused(scenario, StowageViolation.SlotOutsideVessel, scenario.Feu(), 5, 0, 2);
    }

    [Fact]
    public void A_bay_that_does_not_take_the_size_is_reported_before_a_container_already_stowed() {
        // Arrange
        PlanScenario scenario = new PlanScenario();
        Container anyBox      = scenario.Teu();
        scenario.Stow(anyBox, 1, 0, 2);

        // Act & Assert
        AssertRefused(scenario, StowageViolation.BayDoesNotFitSize, anyBox, 2, 0, 2);
    }

    [Fact]
    public void A_container_already_stowed_is_reported_before_an_occupied_slot() {
        // Arrange
        PlanScenario scenario = new PlanScenario();
        Container anyBox      = scenario.Teu();
        scenario.Stow(anyBox, 1, 0, 2);

        // Act & Assert: the slot asked for is the one the box itself occupies
        AssertRefused(scenario, StowageViolation.AlreadyStowed, anyBox, 1, 0, 2);
    }

    [Fact]
    public void An_occupied_slot_is_reported_before_ports_out_of_order() {
        // Arrange
        PlanScenario scenario = new PlanScenario();
        scenario.Stow(scenario.Teu(), 1, 0, 2);

        // Act & Assert
        AssertRefused(scenario, StowageViolation.SlotOccupied, scenario.Teu(loadsAt: 2, dischargesAt: 1), 1, 0, 2);
    }

    [Fact]
    public void Ports_out_of_order_are_reported_before_explosives_under_deck() {
        // Arrange
        PlanScenario scenario = new PlanScenario();
        Container explosives  = scenario.DangerousBox(ContainerSize.TwentyFoot, HazardClass.Explosives, loadsAt: 2, dischargesAt: 1);

        // Act & Assert
        AssertRefused(scenario, StowageViolation.PortNotInRotation, explosives, 1, 0, 2);
    }

    [Fact]
    public void Explosives_under_deck_are_reported_before_a_missing_support() {
        // Arrange
        PlanScenario scenario = new PlanScenario();
        Container explosives  = scenario.DangerousBox(ContainerSize.TwentyFoot, HazardClass.Explosives);

        // Act & Assert: tier 4 with nothing at tier 2
        AssertRefused(scenario, StowageViolation.ExplosivesUnderDeck, explosives, 1, 0, 4);
    }

    [Fact]
    public void A_box_too_heavy_for_the_one_below_is_reported_before_a_stack_over_the_limit() {
        // Arrange
        PlanScenario scenario = new PlanScenario(PlanScenario.VesselOf(maxStackWeightTonnes: 50m));
        scenario.Stow(scenario.Teu(grossTonnes: 25m), 1, 0, 2);

        // Act & Assert: 30 t on 25 t is heavier than the box below, and the stack would weigh 55 t over a 50 t limit
        AssertRefused(scenario, StowageViolation.HeavierThanContainerBelow, scenario.Teu(grossTonnes: 30m), 1, 0, 4);
    }

    [Fact]
    public void A_stack_over_the_limit_is_reported_before_an_overstow() {
        // Arrange
        PlanScenario scenario = new PlanScenario(PlanScenario.VesselOf(maxStackWeightTonnes: 50m));
        scenario.Stow(scenario.Teu(loadsAt: 1, dischargesAt: 2, grossTonnes: 30m), 1, 0, 2);

        // Act & Assert: 30 + 25 is over 50 and the box above is discharged later than the one under it
        AssertRefused(scenario, StowageViolation.StackWeightExceeded, scenario.Teu(loadsAt: 1, dischargesAt: 3, grossTonnes: 25m), 1, 0, 4);
    }

    [Fact]
    public void An_overstow_is_reported_before_a_blocked_hatch() {
        // Arrange
        PlanScenario scenario = new PlanScenario();
        scenario.Stow(scenario.Teu(loadsAt: 1, dischargesAt: 2, grossTonnes: 20m), 1, 0, 2);
        scenario.Stow(scenario.Teu(loadsAt: 1, dischargesAt: 2, grossTonnes: 20m), 1, 2, 82);

        // Act & Assert: the box above is discharged later than the one under it and than the deck box
        AssertRefused(scenario, StowageViolation.Overstow, scenario.Teu(loadsAt: 1, dischargesAt: 3, grossTonnes: 10m), 1, 0, 4);
    }

    [Fact]
    public void A_blocked_hatch_is_reported_before_a_segregation_conflict() {
        // Arrange
        PlanScenario scenario = new PlanScenario();
        scenario.Stow(scenario.DangerousBox(ContainerSize.TwentyFoot, HazardClass.Gases, loadsAt: 1, dischargesAt: 2), 1, 0, 2);
        scenario.Stow(scenario.DangerousBox(ContainerSize.TwentyFoot, HazardClass.Explosives, loadsAt: 1, dischargesAt: 2), 1, 3, 82);

        // Act & Assert: a deck box next to the explosives (rows 1 and 3 touch), loaded before the hold box it must be lifted after
        AssertRefused(scenario, StowageViolation.HatchAccessBlocked, scenario.DangerousBox(ContainerSize.TwentyFoot, HazardClass.Toxic, loadsAt: 0, dischargesAt: 2), 1, 1, 82);
    }

    [Fact]
    public void A_segregation_conflict_is_reported_before_a_displacement_over_the_limit() {
        // Arrange
        PlanScenario scenario = new PlanScenario(PlanScenario.VesselOf(lightshipTonnes: 10_000m, maxDisplacementTonnes: 10_025m));
        scenario.Stow(scenario.DangerousBox(ContainerSize.TwentyFoot, HazardClass.Explosives, grossTonnes: 20m), 1, 0, 82);

        // Act & Assert: 10 020 t is aboard; another 20 t is over the limit and next to explosives
        AssertRefused(scenario, StowageViolation.SegregationConflict, scenario.DangerousBox(ContainerSize.TwentyFoot, HazardClass.Gases, grossTonnes: 20m), 1, 1, 82);
    }

}
