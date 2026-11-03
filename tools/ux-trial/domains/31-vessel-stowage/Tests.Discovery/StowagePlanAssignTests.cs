using VesselStowage.Domain;

namespace VesselStowage.Tests;

public sealed class StowagePlanAssignTests {

    // Slots used below (bay, row, tier). Bays 1 and 3 take 20-foot boxes, bay 2 and 4 take 40-foot boxes.
    private static SlotPosition HoldFloor      => new SlotPosition(1, 0, 2);
    private static SlotPosition HoldSecondTier => new SlotPosition(1, 0, 4);
    private static SlotPosition DeckFloor      => new SlotPosition(1, 0, 82);

    private static void AssertRefused(StowageViolation expected, StowagePlan plan, Container container, SlotPosition slot) {
        StowageRefusedException refusal = Assert.Throws<StowageRefusedException>(() => plan.Assign(container, slot));

        Assert.Equal(expected, refusal.Violation);
    }

    // ----- the happy path

    [Fact]
    public void A_box_assigned_to_a_free_slot_is_in_the_plan() {
        // Arrange
        StowagePlan plan      = Scene.PlanOnRoomyVessel();
        Container   container = Scene.Box(Scene.Rotterdam, Scene.Hamburg).Generate();

        // Act
        StowageAssignment assignment = plan.Assign(container, HoldFloor);

        // Assert
        Assert.Same(container, assignment.Container);
        Assert.Equal(HoldFloor, assignment.Slot);
        Assert.Same(assignment, plan.At(HoldFloor));
        Assert.Same(assignment, plan.Find(container.Id));
        Assert.Single(plan.Assignments);
    }

    [Fact]
    public void A_new_plan_is_empty() {
        StowagePlan plan = Scene.PlanOnRoomyVessel();

        Assert.Empty(plan.Assignments);
        Assert.Null(plan.At(HoldFloor));
        Assert.Null(plan.Find(Any.ContainerId().Generate()));
    }

    [Fact]
    public void A_plan_keeps_its_vessel_and_rotation() {
        VesselProfile anyVessel = Any.VesselProfile().Generate();
        Rotation      anyRotation = Any.Rotation().Generate();

        StowagePlan plan = new StowagePlan(anyVessel, anyRotation);

        Assert.Same(anyVessel, plan.Vessel);
        Assert.Same(anyRotation, plan.Rotation);
    }

    [Fact]
    public void A_plan_needs_a_vessel_and_a_rotation() {
        Assert.Throws<ArgumentNullException>(() => new StowagePlan(null!, Scene.FourPortRotation()));
        Assert.Throws<ArgumentNullException>(() => new StowagePlan(Scene.RoomyVessel().Generate(), null!));
    }

    [Fact]
    public void A_forty_foot_box_is_assigned_to_an_even_bay() {
        StowagePlan plan = Scene.PlanOnRoomyVessel();
        Container   fortyFooter = Scene.Box(Scene.Rotterdam, Scene.Hamburg).WithSize(ContainerSize.FortyFoot).Generate();

        StowageAssignment assignment = plan.Assign(fortyFooter, new SlotPosition(2, 0, 2));

        Assert.Equal(2, assignment.Slot.Bay);
    }

    [Fact]
    public void A_box_can_stand_on_a_lighter_or_equal_box_below_it_only_in_the_same_stack() {
        // Arrange: 20 t on the floor, 15 t above, 15 t above again
        StowagePlan plan = Scene.PlanOnRoomyVessel();
        plan.Assign(Scene.BoxOfMass(20m), HoldFloor);
        plan.Assign(Scene.BoxOfMass(15m), HoldSecondTier);

        // Act
        plan.Assign(Scene.BoxOfMass(15m), new SlotPosition(1, 0, 6));

        // Assert
        Assert.Equal(3, plan.Assignments.Count);
    }

    [Fact]
    public void A_deck_box_needs_nothing_below_it_not_even_in_the_hold() {
        StowagePlan plan = Scene.PlanOnRoomyVessel();

        plan.Assign(Scene.BoxOfMass(10m), DeckFloor);

        Assert.NotNull(plan.At(DeckFloor));
    }

    [Fact]
    public void Explosives_are_accepted_on_deck() {
        StowagePlan plan = Scene.PlanOnRoomyVessel();

        plan.Assign(Scene.Dangerous(HazardClass.Explosives), DeckFloor);

        Assert.NotNull(plan.At(DeckFloor));
    }

    [Fact]
    public void The_slot_of_a_removed_box_can_be_used_again() {
        StowagePlan plan = Scene.PlanOnRoomyVessel();
        Container   first = Scene.BoxOfMass(10m);
        plan.Assign(first, HoldFloor);
        plan.Remove(first.Id);

        plan.Assign(Scene.BoxOfMass(10m), HoldFloor);

        Assert.Single(plan.Assignments);
    }

    [Fact]
    public void Null_arguments_are_refused() {
        StowagePlan plan = Scene.PlanOnRoomyVessel();

        Assert.Throws<ArgumentNullException>(() => plan.Assign(null!, HoldFloor));
        Assert.Throws<ArgumentNullException>(() => plan.Assign(Scene.BoxOfMass(10m), null!));
        Assert.Throws<ArgumentNullException>(() => plan.Check(null!, HoldFloor));
        Assert.Throws<ArgumentNullException>(() => plan.Check(Scene.BoxOfMass(10m), null!));
        Assert.Throws<ArgumentNullException>(() => plan.At(null!));
        Assert.Throws<ArgumentNullException>(() => plan.Find(null!));
    }

    // ----- the refusals, one per violation

    [Fact]
    public void A_slot_in_a_bay_the_vessel_does_not_have_is_outside_the_vessel() {
        VesselProfile vessel = Scene.RoomyVessel().WithBayCount(4).Generate();
        StowagePlan   plan   = new StowagePlan(vessel, Scene.FourPortRotation());

        AssertRefused(StowageViolation.SlotOutsideVessel, plan, Scene.BoxOfMass(10m), new SlotPosition(5, 0, 2));
    }

    [Fact]
    public void A_slot_in_a_row_the_vessel_does_not_have_is_outside_the_vessel() {
        VesselProfile vessel = Scene.RoomyVessel().WithRowCount(4).Generate();
        StowagePlan   plan   = new StowagePlan(vessel, Scene.FourPortRotation());

        AssertRefused(StowageViolation.SlotOutsideVessel, plan, Scene.BoxOfMass(10m), new SlotPosition(1, 4, 2));
        plan.Assign(Scene.BoxOfMass(10m), new SlotPosition(1, 3, 2));
    }

    [Fact]
    public void A_slot_in_a_hold_tier_the_vessel_does_not_have_is_outside_the_vessel() {
        VesselProfile vessel = Scene.RoomyVessel().WithHoldTiers(3).Generate();
        StowagePlan   plan   = new StowagePlan(vessel, Scene.FourPortRotation());

        AssertRefused(StowageViolation.SlotOutsideVessel, plan, Scene.BoxOfMass(10m), new SlotPosition(1, 0, 8));
    }

    [Fact]
    public void A_deck_slot_on_a_vessel_without_deck_tiers_is_outside_the_vessel() {
        VesselProfile vessel = Scene.RoomyVessel().WithDeckTiers(0).Generate();
        StowagePlan   plan   = new StowagePlan(vessel, Scene.FourPortRotation());

        AssertRefused(StowageViolation.SlotOutsideVessel, plan, Scene.BoxOfMass(10m), DeckFloor);
    }

    [Fact]
    public void A_deck_slot_above_the_last_deck_tier_is_outside_the_vessel() {
        VesselProfile vessel = Scene.RoomyVessel().WithDeckTiers(2).Generate();
        StowagePlan   plan   = new StowagePlan(vessel, Scene.FourPortRotation());

        AssertRefused(StowageViolation.SlotOutsideVessel, plan, Scene.BoxOfMass(10m), new SlotPosition(1, 0, 86));
    }

    [Fact]
    public void A_forty_foot_box_in_an_odd_bay_does_not_fit() {
        StowagePlan plan = Scene.PlanOnRoomyVessel();
        Container   fortyFooter = Scene.Box(Scene.Rotterdam, Scene.Hamburg).WithSize(ContainerSize.FortyFoot).Generate();

        AssertRefused(StowageViolation.BayDoesNotFitSize, plan, fortyFooter, HoldFloor);
    }

    [Fact]
    public void A_twenty_foot_box_in_an_even_bay_does_not_fit() {
        StowagePlan plan = Scene.PlanOnRoomyVessel();

        AssertRefused(StowageViolation.BayDoesNotFitSize, plan, Scene.BoxOfMass(10m), new SlotPosition(2, 0, 2));
    }

    [Fact]
    public void A_box_already_stowed_cannot_be_stowed_again_elsewhere() {
        StowagePlan plan = Scene.PlanOnRoomyVessel();
        Container   container = Scene.BoxOfMass(10m);
        plan.Assign(container, HoldFloor);

        AssertRefused(StowageViolation.AlreadyStowed, plan, container, new SlotPosition(1, 1, 2));
    }

    [Fact]
    public void An_occupied_slot_refuses_a_second_box() {
        StowagePlan plan = Scene.PlanOnRoomyVessel();
        plan.Assign(Scene.BoxOfMass(10m), HoldFloor);

        AssertRefused(StowageViolation.SlotOccupied, plan, Scene.BoxOfMass(10m), HoldFloor);
    }

    [Fact]
    public void A_box_stowed_and_aimed_at_its_own_slot_is_already_stowed_rather_than_occupying_it() {
        // the container is checked before the slot
        StowagePlan plan = Scene.PlanOnRoomyVessel();
        Container   container = Scene.BoxOfMass(10m);
        plan.Assign(container, HoldFloor);

        AssertRefused(StowageViolation.AlreadyStowed, plan, container, HoldFloor);
    }

    [Fact]
    public void A_box_whose_loading_port_is_not_in_the_rotation_is_refused() {
        StowagePlan plan = Scene.PlanOnRoomyVessel();
        PortCode    unknownPort = new PortCode("BEANR");
        Container   container = Scene.Box(unknownPort, Scene.Hamburg).Generate();

        AssertRefused(StowageViolation.PortNotInRotation, plan, container, HoldFloor);
    }

    [Fact]
    public void A_box_whose_discharge_port_is_not_in_the_rotation_is_refused() {
        StowagePlan plan = Scene.PlanOnRoomyVessel();
        Container   container = Scene.Box(Scene.Rotterdam, new PortCode("BEANR")).Generate();

        AssertRefused(StowageViolation.PortNotInRotation, plan, container, HoldFloor);
    }

    [Fact]
    public void A_box_discharged_before_it_is_loaded_is_refused() {
        StowagePlan plan = Scene.PlanOnRoomyVessel();
        Container   container = Scene.Box(Scene.Hamburg, Scene.Rotterdam).Generate();

        AssertRefused(StowageViolation.PortNotInRotation, plan, container, HoldFloor);
    }

    [Fact]
    public void Explosives_in_the_hold_are_refused() {
        StowagePlan plan = Scene.PlanOnRoomyVessel();

        AssertRefused(StowageViolation.ExplosivesUnderDeck, plan, Scene.Dangerous(HazardClass.Explosives), HoldFloor);
    }

    [Fact]
    public void Other_dangerous_goods_are_accepted_in_the_hold() {
        StowagePlan plan = Scene.PlanOnRoomyVessel();
        Container   toxic = Scene.Dangerous(HazardClass.Toxic);

        plan.Assign(toxic, HoldFloor);

        Assert.Same(toxic, plan.At(HoldFloor)!.Container);
    }

    [Fact]
    public void A_hold_box_above_an_empty_slot_has_no_support() {
        StowagePlan plan = Scene.PlanOnRoomyVessel();

        AssertRefused(StowageViolation.NoSupportBelow, plan, Scene.BoxOfMass(10m), HoldSecondTier);
    }

    [Fact]
    public void A_deck_box_above_an_empty_deck_slot_has_no_support() {
        StowagePlan plan = Scene.PlanOnRoomyVessel();

        AssertRefused(StowageViolation.NoSupportBelow, plan, Scene.BoxOfMass(10m), new SlotPosition(1, 0, 84));
    }

    [Fact]
    public void The_first_deck_tier_does_not_lean_on_the_top_hold_tier() {
        // a full hold stack under the deck slot makes no difference to what the deck slot needs
        StowagePlan plan = Scene.PlanOnRoomyVessel();
        plan.Assign(Scene.BoxOfMass(10m), HoldFloor);

        plan.Assign(Scene.BoxOfMass(10m), DeckFloor);

        Assert.Equal(2, plan.Assignments.Count);
    }

    [Fact]
    public void A_box_heavier_than_the_one_it_stands_on_is_refused() {
        StowagePlan plan = Scene.PlanOnRoomyVessel();
        plan.Assign(Scene.BoxOfMass(10m), HoldFloor);

        AssertRefused(StowageViolation.HeavierThanContainerBelow, plan, Scene.BoxOfMass(10.01m), HoldSecondTier);
    }

    [Fact]
    public void A_box_of_exactly_the_mass_of_the_one_below_is_accepted() {
        StowagePlan plan = Scene.PlanOnRoomyVessel();
        plan.Assign(Scene.BoxOfMass(10m), HoldFloor);

        plan.Assign(Scene.BoxOfMass(10m), HoldSecondTier);

        Assert.Equal(2, plan.Assignments.Count);
    }

    [Fact]
    public void A_stack_that_would_pass_the_weight_limit_is_refused() {
        // Arrange: a limit of 25 t, 20 t already in the stack
        VesselProfile vessel = Scene.RoomyVessel().WithMaxStackWeightTonnes(25m).Generate();
        StowagePlan   plan   = new StowagePlan(vessel, Scene.FourPortRotation());
        plan.Assign(Scene.BoxOfMass(20m), HoldFloor);

        // Assert
        AssertRefused(StowageViolation.StackWeightExceeded, plan, Scene.BoxOfMass(5.01m), HoldSecondTier);
    }

    [Fact]
    public void A_stack_that_reaches_the_weight_limit_exactly_is_accepted() {
        VesselProfile vessel = Scene.RoomyVessel().WithMaxStackWeightTonnes(25m).Generate();
        StowagePlan   plan   = new StowagePlan(vessel, Scene.FourPortRotation());
        plan.Assign(Scene.BoxOfMass(20m), HoldFloor);

        plan.Assign(Scene.BoxOfMass(5m), HoldSecondTier);

        Assert.Equal(2, plan.Assignments.Count);
    }

    [Fact]
    public void A_single_box_heavier_than_the_stack_limit_is_refused() {
        VesselProfile vessel = Scene.RoomyVessel().WithMaxStackWeightTonnes(10m).Generate();
        StowagePlan   plan   = new StowagePlan(vessel, Scene.FourPortRotation());

        AssertRefused(StowageViolation.StackWeightExceeded, plan, Scene.BoxOfMass(10.5m), HoldFloor);
    }

    [Fact]
    public void The_weight_limit_applies_to_each_stack_separately() {
        VesselProfile vessel = Scene.RoomyVessel().WithMaxStackWeightTonnes(25m).Generate();
        StowagePlan   plan   = new StowagePlan(vessel, Scene.FourPortRotation());
        plan.Assign(Scene.BoxOfMass(20m), HoldFloor);

        // another row, another bay, and the deck of the same bay and row are other stacks
        plan.Assign(Scene.BoxOfMass(20m), new SlotPosition(1, 1, 2));
        plan.Assign(Scene.BoxOfMass(20m), new SlotPosition(3, 0, 2));
        plan.Assign(Scene.BoxOfMass(20m), DeckFloor);

        Assert.Equal(4, plan.Assignments.Count);
    }

    [Fact]
    public void A_box_loaded_after_the_one_below_it_is_discharged_is_refused_as_overstow() {
        // Arrange: the lower box stays aboard until Hamburg, the upper one until Felixstowe: the upper one blocks
        StowagePlan plan = Scene.PlanOnRoomyVessel();
        plan.Assign(Scene.Box(Scene.Rotterdam, Scene.Hamburg).WithGrossMassTonnes(20m).Generate(), HoldFloor);
        Container upper = Scene.Box(Scene.Rotterdam, Scene.Felixstowe).WithGrossMassTonnes(10m).Generate();

        // Assert
        AssertRefused(StowageViolation.Overstow, plan, upper, HoldSecondTier);
    }

    [Fact]
    public void A_box_loaded_before_the_one_below_it_is_refused_as_overstow() {
        // Arrange: the lower box is loaded at Hamburg, the upper one is already aboard at Rotterdam
        StowagePlan plan = Scene.PlanOnRoomyVessel();
        plan.Assign(Scene.Box(Scene.Hamburg, Scene.Felixstowe).WithGrossMassTonnes(20m).Generate(), HoldFloor);
        Container upper = Scene.Box(Scene.Rotterdam, Scene.Hamburg).WithGrossMassTonnes(10m).Generate();

        AssertRefused(StowageViolation.Overstow, plan, upper, HoldSecondTier);
    }

    [Fact]
    public void A_box_loaded_at_the_same_port_and_discharged_at_the_same_port_can_stand_on_the_one_below() {
        StowagePlan plan = Scene.PlanOnRoomyVessel();
        plan.Assign(Scene.Box(Scene.Hamburg, Scene.Felixstowe).WithGrossMassTonnes(20m).Generate(), HoldFloor);

        plan.Assign(Scene.Box(Scene.Hamburg, Scene.Felixstowe).WithGrossMassTonnes(10m).Generate(), HoldSecondTier);

        Assert.Equal(2, plan.Assignments.Count);
    }

    [Fact]
    public void A_box_loaded_later_and_discharged_earlier_can_stand_on_the_one_below() {
        StowagePlan plan = Scene.PlanOnRoomyVessel();
        plan.Assign(Scene.Box(Scene.Rotterdam, Scene.LeHavre).WithGrossMassTonnes(20m).Generate(), HoldFloor);

        plan.Assign(Scene.Box(Scene.Hamburg, Scene.Felixstowe).WithGrossMassTonnes(10m).Generate(), HoldSecondTier);

        Assert.Equal(2, plan.Assignments.Count);
    }

    [Fact]
    public void A_deck_box_discharged_after_a_hold_box_of_its_bay_blocks_the_hatch() {
        // Arrange: hold box leaves at Hamburg, deck box stays until Felixstowe
        StowagePlan plan = Scene.PlanOnRoomyVessel();
        plan.Assign(Scene.Box(Scene.Rotterdam, Scene.Hamburg).Generate(), HoldFloor);
        Container deck = Scene.Box(Scene.Rotterdam, Scene.Felixstowe).Generate();

        AssertRefused(StowageViolation.HatchAccessBlocked, plan, deck, DeckFloor);
    }

    [Fact]
    public void A_hold_box_discharged_before_a_deck_box_of_its_bay_is_refused_for_the_hatch_too() {
        // the deck box is already there: the hold box added afterwards would be under the hatch cover for too long
        StowagePlan plan = Scene.PlanOnRoomyVessel();
        plan.Assign(Scene.Box(Scene.Rotterdam, Scene.Felixstowe).Generate(), DeckFloor);
        Container hold = Scene.Box(Scene.Rotterdam, Scene.Hamburg).Generate();

        AssertRefused(StowageViolation.HatchAccessBlocked, plan, hold, HoldFloor);
    }

    [Fact]
    public void A_deck_box_loaded_before_a_hold_box_of_its_bay_blocks_the_hatch() {
        // the deck box is loaded at Rotterdam, the hold box only at Hamburg: the hatch would have to be opened
        StowagePlan plan = Scene.PlanOnRoomyVessel();
        plan.Assign(Scene.Box(Scene.Hamburg, Scene.Felixstowe).Generate(), HoldFloor);
        Container deck = Scene.Box(Scene.Rotterdam, Scene.Felixstowe).Generate();

        AssertRefused(StowageViolation.HatchAccessBlocked, plan, deck, DeckFloor);
    }

    [Fact]
    public void The_hatch_of_one_bay_says_nothing_about_another_bay() {
        StowagePlan plan = Scene.PlanOnRoomyVessel();
        plan.Assign(Scene.Box(Scene.Rotterdam, Scene.Hamburg).Generate(), HoldFloor);

        plan.Assign(Scene.Box(Scene.Rotterdam, Scene.Felixstowe).Generate(), new SlotPosition(3, 0, 82));

        Assert.Equal(2, plan.Assignments.Count);
    }

    [Fact]
    public void The_hatch_rule_covers_every_row_of_the_bay() {
        StowagePlan plan = Scene.PlanOnRoomyVessel();
        plan.Assign(Scene.Box(Scene.Rotterdam, Scene.Hamburg).Generate(), new SlotPosition(1, 3, 2));

        AssertRefused(StowageViolation.HatchAccessBlocked, plan, Scene.Box(Scene.Rotterdam, Scene.Felixstowe).Generate(), DeckFloor);
    }

    [Fact]
    public void An_explosives_box_cannot_stand_beside_other_dangerous_goods() {
        StowagePlan plan = Scene.PlanOnRoomyVessel();
        plan.Assign(Scene.Dangerous(HazardClass.Toxic), DeckFloor);

        AssertRefused(StowageViolation.SegregationConflict, plan, Scene.Dangerous(HazardClass.Explosives), new SlotPosition(1, 1, 82));
    }

    [Fact]
    public void A_flammable_box_cannot_stand_beside_an_oxidizer() {
        StowagePlan plan = Scene.PlanOnRoomyVessel();
        plan.Assign(Scene.Dangerous(HazardClass.Oxidizers), HoldFloor);

        AssertRefused(StowageViolation.SegregationConflict, plan, Scene.Dangerous(HazardClass.FlammableLiquids), new SlotPosition(1, 1, 2));
    }

    [Fact]
    public void A_flammable_box_cannot_stand_on_top_of_an_oxidizer() {
        StowagePlan plan = Scene.PlanOnRoomyVessel();
        plan.Assign(Scene.Box(Scene.Rotterdam, Scene.Hamburg).WithHazard(HazardClass.Oxidizers).WithIsReefer(false).WithGrossMassTonnes(20m).Generate(), HoldFloor);

        AssertRefused(StowageViolation.SegregationConflict, plan,
                      Scene.Box(Scene.Rotterdam, Scene.Hamburg).WithHazard(HazardClass.FlammableSolids).WithIsReefer(false).WithGrossMassTonnes(10m).Generate(),
                      HoldSecondTier);
    }

    [Fact]
    public void Incompatible_goods_two_rows_apart_are_not_neighbours() {
        StowagePlan plan = Scene.PlanOnRoomyVessel();
        plan.Assign(Scene.Dangerous(HazardClass.Oxidizers), HoldFloor);

        plan.Assign(Scene.Dangerous(HazardClass.FlammableLiquids), new SlotPosition(1, 3, 2));

        Assert.Equal(2, plan.Assignments.Count);
    }

    [Fact]
    public void Incompatible_goods_on_either_side_of_the_centreline_are_not_neighbours() {
        StowagePlan plan = Scene.PlanOnRoomyVessel();
        plan.Assign(Scene.Dangerous(HazardClass.Oxidizers), new SlotPosition(1, 1, 2));

        plan.Assign(Scene.Dangerous(HazardClass.FlammableLiquids), new SlotPosition(1, 2, 2));

        Assert.Equal(2, plan.Assignments.Count);
    }

    [Fact]
    public void Incompatible_goods_in_the_hold_and_on_deck_are_not_neighbours() {
        StowagePlan plan = Scene.PlanOnRoomyVessel();
        plan.Assign(Scene.Dangerous(HazardClass.Oxidizers), HoldFloor);

        plan.Assign(Scene.Dangerous(HazardClass.FlammableLiquids), DeckFloor);

        Assert.Equal(2, plan.Assignments.Count);
    }

    [Fact]
    public void Incompatible_goods_in_two_bays_are_not_neighbours() {
        StowagePlan plan = Scene.PlanOnRoomyVessel();
        plan.Assign(Scene.Dangerous(HazardClass.Oxidizers), HoldFloor);

        plan.Assign(Scene.Dangerous(HazardClass.FlammableLiquids), new SlotPosition(3, 0, 2));

        Assert.Equal(2, plan.Assignments.Count);
    }

    [Fact]
    public void Dangerous_goods_beside_general_cargo_are_accepted() {
        StowagePlan plan = Scene.PlanOnRoomyVessel();
        plan.Assign(Scene.BoxOfMass(10m), DeckFloor);

        plan.Assign(Scene.Dangerous(HazardClass.Explosives), new SlotPosition(1, 1, 82));

        Assert.Equal(2, plan.Assignments.Count);
    }

    // ----- the order the checks are made in

    [Fact]
    public void The_vessel_geometry_is_checked_before_the_bay_size() {
        VesselProfile vessel = Scene.RoomyVessel().WithBayCount(4).Generate();
        StowagePlan   plan   = new StowagePlan(vessel, Scene.FourPortRotation());
        // a 20 ft box in bay 6 (even, 40 ft) which the vessel does not have
        AssertRefused(StowageViolation.SlotOutsideVessel, plan, Scene.BoxOfMass(10m), new SlotPosition(6, 0, 2));
    }

    [Fact]
    public void The_bay_size_is_checked_before_the_box_being_already_stowed() {
        StowagePlan plan = Scene.PlanOnRoomyVessel();
        Container   fortyFooter = Scene.Box(Scene.Rotterdam, Scene.Hamburg).WithSize(ContainerSize.FortyFoot).Generate();
        plan.Assign(fortyFooter, new SlotPosition(2, 0, 2));

        AssertRefused(StowageViolation.BayDoesNotFitSize, plan, fortyFooter, HoldFloor);
    }

    [Fact]
    public void An_occupied_slot_is_reported_before_the_ports_are_looked_at() {
        StowagePlan plan = Scene.PlanOnRoomyVessel();
        plan.Assign(Scene.BoxOfMass(10m), HoldFloor);

        AssertRefused(StowageViolation.SlotOccupied, plan, Scene.Box(Scene.LeHavre, Scene.Rotterdam).Generate(), HoldFloor);
    }

    [Fact]
    public void The_ports_are_checked_before_explosives_under_deck() {
        StowagePlan plan = Scene.PlanOnRoomyVessel();
        Container   container = Scene.Box(Scene.LeHavre, Scene.Rotterdam).WithHazard(HazardClass.Explosives).WithIsReefer(false).Generate();

        AssertRefused(StowageViolation.PortNotInRotation, plan, container, HoldFloor);
    }

    [Fact]
    public void Explosives_under_deck_are_reported_before_the_missing_support() {
        StowagePlan plan = Scene.PlanOnRoomyVessel();

        AssertRefused(StowageViolation.ExplosivesUnderDeck, plan, Scene.Dangerous(HazardClass.Explosives), HoldSecondTier);
    }

    [Fact]
    public void The_missing_support_is_reported_before_the_weight_limit() {
        VesselProfile vessel = Scene.RoomyVessel().WithMaxStackWeightTonnes(10m).Generate();
        StowagePlan   plan   = new StowagePlan(vessel, Scene.FourPortRotation());

        AssertRefused(StowageViolation.NoSupportBelow, plan, Scene.BoxOfMass(20m), HoldSecondTier);
    }

    [Fact]
    public void Heavier_than_the_box_below_is_reported_before_the_stack_weight() {
        VesselProfile vessel = Scene.RoomyVessel().WithMaxStackWeightTonnes(25m).Generate();
        StowagePlan   plan   = new StowagePlan(vessel, Scene.FourPortRotation());
        plan.Assign(Scene.BoxOfMass(10m), HoldFloor);

        AssertRefused(StowageViolation.HeavierThanContainerBelow, plan, Scene.BoxOfMass(20m), HoldSecondTier);
    }

    [Fact]
    public void The_stack_weight_is_reported_before_overstow() {
        VesselProfile vessel = Scene.RoomyVessel().WithMaxStackWeightTonnes(25m).Generate();
        StowagePlan   plan   = new StowagePlan(vessel, Scene.FourPortRotation());
        plan.Assign(Scene.Box(Scene.Rotterdam, Scene.Hamburg).WithGrossMassTonnes(20m).Generate(), HoldFloor);
        Container overstowing = Scene.Box(Scene.Rotterdam, Scene.Felixstowe).WithGrossMassTonnes(10m).Generate();

        AssertRefused(StowageViolation.StackWeightExceeded, plan, overstowing, HoldSecondTier);
    }

    [Fact]
    public void Overstow_is_reported_before_the_hatch() {
        // the deck box also blocks the hatch of the hold box, but it first overstows the deck box below it
        StowagePlan plan = Scene.PlanOnRoomyVessel();
        plan.Assign(Scene.Box(Scene.Rotterdam, Scene.Hamburg).WithGrossMassTonnes(20m).Generate(), HoldFloor);
        plan.Assign(Scene.Box(Scene.Rotterdam, Scene.Hamburg).WithGrossMassTonnes(20m).Generate(), DeckFloor);
        Container upperDeck = Scene.Box(Scene.Rotterdam, Scene.Felixstowe).WithGrossMassTonnes(10m).Generate();

        AssertRefused(StowageViolation.Overstow, plan, upperDeck, new SlotPosition(1, 0, 84));
    }

    [Fact]
    public void The_hatch_is_reported_before_segregation() {
        StowagePlan plan = Scene.PlanOnRoomyVessel();
        plan.Assign(Scene.Box(Scene.Rotterdam, Scene.Hamburg).WithHazard(HazardClass.Oxidizers).WithIsReefer(false).Generate(), new SlotPosition(1, 1, 2));
        // flammable next to the oxidizer would conflict, but it is first refused for the hatch
        Container flammable = Scene.Box(Scene.Rotterdam, Scene.Felixstowe).WithHazard(HazardClass.FlammableLiquids).WithIsReefer(false).Generate();

        AssertRefused(StowageViolation.HatchAccessBlocked, plan, flammable, new SlotPosition(1, 1, 82));
    }

    // ----- Check

    [Fact]
    public void Check_returns_none_for_a_box_that_can_be_assigned() {
        StowagePlan plan = Scene.PlanOnRoomyVessel();

        Assert.Equal(StowageViolation.None, plan.Check(Scene.BoxOfMass(10m), HoldFloor));
    }

    [Fact]
    public void Check_does_not_change_the_plan() {
        StowagePlan plan = Scene.PlanOnRoomyVessel();
        Container   container = Scene.BoxOfMass(10m);

        plan.Check(container, HoldFloor);

        Assert.Empty(plan.Assignments);
        Assert.Null(plan.Find(container.Id));
    }

    [Fact]
    public void Check_names_the_violation_that_Assign_would_refuse_with() {
        StowagePlan plan = Scene.PlanOnRoomyVessel();
        plan.Assign(Scene.BoxOfMass(10m), HoldFloor);
        Container heavier = Scene.BoxOfMass(12m);

        StowageViolation checkedViolation = plan.Check(heavier, HoldSecondTier);
        StowageRefusedException refusal = Assert.Throws<StowageRefusedException>(() => plan.Assign(heavier, HoldSecondTier));

        Assert.Equal(StowageViolation.HeavierThanContainerBelow, checkedViolation);
        Assert.Equal(checkedViolation, refusal.Violation);
    }

    [Fact]
    public void The_refusal_message_names_the_box_the_slot_and_the_reason() {
        StowagePlan plan = Scene.PlanOnRoomyVessel();
        Container   container = Scene.BoxOfMass(10m);
        plan.Assign(Scene.BoxOfMass(10m), HoldFloor);

        StowageRefusedException refusal = Assert.Throws<StowageRefusedException>(() => plan.Assign(container, HoldFloor));

        Assert.Contains(container.Id.Value, refusal.Message);
        Assert.Contains("010002", refusal.Message);
        Assert.Contains("occupied", refusal.Message);
    }

    [Fact]
    public void A_refused_assignment_leaves_the_plan_unchanged() {
        StowagePlan plan = Scene.PlanOnRoomyVessel();
        plan.Assign(Scene.BoxOfMass(10m), HoldFloor);
        Container refused = Scene.BoxOfMass(10m);

        Assert.Throws<StowageRefusedException>(() => plan.Assign(refused, HoldFloor));

        Assert.Single(plan.Assignments);
        Assert.Null(plan.Find(refused.Id));
    }

}
