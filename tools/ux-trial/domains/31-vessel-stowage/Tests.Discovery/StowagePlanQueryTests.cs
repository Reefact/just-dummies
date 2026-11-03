using VesselStowage.Domain;

namespace VesselStowage.Tests;

public sealed class StowagePlanRemoveTests {

    [Fact]
    public void A_box_taken_off_the_plan_is_no_longer_found() {
        // Arrange
        StowagePlan plan      = Scene.PlanOnRoomyVessel();
        Container   container = Scene.BoxOfMass(10m);
        SlotPosition slot     = new SlotPosition(1, 0, 2);
        plan.Assign(container, slot);

        // Act
        plan.Remove(container.Id);

        // Assert
        Assert.Null(plan.Find(container.Id));
        Assert.Null(plan.At(slot));
        Assert.Empty(plan.Assignments);
    }

    [Fact]
    public void A_box_that_was_taken_off_can_be_assigned_again() {
        StowagePlan plan      = Scene.PlanOnRoomyVessel();
        Container   container = Scene.BoxOfMass(10m);
        plan.Assign(container, new SlotPosition(1, 0, 2));
        plan.Remove(container.Id);

        plan.Assign(container, new SlotPosition(3, 1, 2));

        Assert.Equal(new SlotPosition(3, 1, 2), plan.Find(container.Id)!.Slot);
    }

    [Fact]
    public void A_box_that_is_not_stowed_cannot_be_removed() {
        StowagePlan plan = Scene.PlanOnRoomyVessel();

        StowageRefusedException refusal = Assert.Throws<StowageRefusedException>(() => plan.Remove(Any.ContainerId().Generate()));

        Assert.Equal(StowageViolation.NotStowed, refusal.Violation);
    }

    [Fact]
    public void A_box_with_another_box_on_it_cannot_be_removed() {
        StowagePlan plan  = Scene.PlanOnRoomyVessel();
        Container   lower = Scene.BoxOfMass(20m);
        plan.Assign(lower, new SlotPosition(1, 0, 2));
        plan.Assign(Scene.BoxOfMass(10m), new SlotPosition(1, 0, 4));

        StowageRefusedException refusal = Assert.Throws<StowageRefusedException>(() => plan.Remove(lower.Id));

        Assert.Equal(StowageViolation.ContainerStackedAbove, refusal.Violation);
        Assert.NotNull(plan.Find(lower.Id));
    }

    [Fact]
    public void The_top_box_of_a_stack_can_be_removed_and_then_the_one_under_it() {
        StowagePlan plan  = Scene.PlanOnRoomyVessel();
        Container   lower = Scene.BoxOfMass(20m);
        Container   upper = Scene.BoxOfMass(10m);
        plan.Assign(lower, new SlotPosition(1, 0, 2));
        plan.Assign(upper, new SlotPosition(1, 0, 4));

        plan.Remove(upper.Id);
        plan.Remove(lower.Id);

        Assert.Empty(plan.Assignments);
    }

    [Fact]
    public void A_deck_box_does_not_stand_on_the_top_hold_box() {
        // Arrange: the hold has exactly 3 tiers; fill the top one, then the first deck tier
        VesselProfile vessel = Scene.RoomyVessel().WithHoldTiers(3).Generate();
        StowagePlan   plan   = new StowagePlan(vessel, Scene.FourPortRotation());
        Container     topOfHold = Scene.BoxOfMass(5m);
        plan.Assign(Scene.BoxOfMass(20m), new SlotPosition(1, 0, 2));
        plan.Assign(Scene.BoxOfMass(10m), new SlotPosition(1, 0, 4));
        plan.Assign(topOfHold, new SlotPosition(1, 0, 6));
        plan.Assign(Scene.BoxOfMass(10m), new SlotPosition(1, 0, 82));

        plan.Remove(topOfHold.Id);

        Assert.Null(plan.Find(topOfHold.Id));
    }

    [Fact]
    public void The_top_hold_tier_is_removable_when_the_vessel_has_exactly_that_many_hold_tiers() {
        VesselProfile vessel = Scene.RoomyVessel().WithHoldTiers(3).Generate();
        StowagePlan   plan   = new StowagePlan(vessel, Scene.FourPortRotation());
        plan.Assign(Scene.BoxOfMass(20m), new SlotPosition(1, 0, 2));
        plan.Assign(Scene.BoxOfMass(10m), new SlotPosition(1, 0, 4));
        Container top = Scene.BoxOfMass(5m);
        plan.Assign(top, new SlotPosition(1, 0, 6));

        plan.Remove(top.Id);

        Assert.Equal(2, plan.Assignments.Count);
    }

    [Fact]
    public void The_top_deck_tier_is_removable_when_the_vessel_has_exactly_that_many_deck_tiers() {
        VesselProfile vessel = Scene.RoomyVessel().WithDeckTiers(2).Generate();
        StowagePlan   plan   = new StowagePlan(vessel, Scene.FourPortRotation());
        plan.Assign(Scene.BoxOfMass(20m), new SlotPosition(1, 0, 82));
        Container top = Scene.BoxOfMass(10m);
        plan.Assign(top, new SlotPosition(1, 0, 84));

        plan.Remove(top.Id);

        Assert.Single(plan.Assignments);
    }

    [Fact]
    public void Removing_null_is_refused() {
        Assert.Throws<ArgumentNullException>(() => Scene.PlanOnRoomyVessel().Remove(null!));
    }

}

public sealed class StowagePlanDischargeTests {

    [Fact]
    public void Boxes_are_lifted_bay_by_bay_deck_before_hold_and_top_tier_first() {
        // Arrange: every box is loaded at Rotterdam and discharged at Hamburg
        StowagePlan plan = Scene.PlanOnRoomyVessel();
        Container holdFloor1 = Scene.BoxOfMass(20m);
        Container holdSecond1 = Scene.BoxOfMass(10m);
        Container deck1 = Scene.BoxOfMass(10m);
        Container holdFloor3 = Scene.BoxOfMass(10m);
        plan.Assign(holdFloor3, new SlotPosition(3, 1, 2));
        plan.Assign(holdFloor1, new SlotPosition(1, 0, 2));
        plan.Assign(deck1, new SlotPosition(1, 0, 82));
        plan.Assign(holdSecond1, new SlotPosition(1, 0, 4));

        // Act
        IReadOnlyList<StowageAssignment> sequence = plan.DischargeSequence(Scene.Hamburg);

        // Assert
        Assert.Equal(new[] { deck1.Id, holdSecond1.Id, holdFloor1.Id, holdFloor3.Id }, sequence.Select(a => a.Container.Id).ToArray());
    }

    [Fact]
    public void Boxes_at_the_same_tier_of_a_bay_are_lifted_by_row() {
        StowagePlan plan = Scene.PlanOnRoomyVessel();
        Container row2 = Scene.BoxOfMass(10m);
        Container row0 = Scene.BoxOfMass(10m);
        Container row1 = Scene.BoxOfMass(10m);
        plan.Assign(row2, new SlotPosition(1, 2, 2));
        plan.Assign(row0, new SlotPosition(1, 0, 2));
        plan.Assign(row1, new SlotPosition(1, 1, 2));

        IReadOnlyList<StowageAssignment> sequence = plan.DischargeSequence(Scene.Hamburg);

        Assert.Equal(new[] { row0.Id, row1.Id, row2.Id }, sequence.Select(a => a.Container.Id).ToArray());
    }

    [Fact]
    public void A_deck_box_of_a_higher_bay_is_still_lifted_after_the_hold_box_of_a_lower_bay() {
        StowagePlan plan = Scene.PlanOnRoomyVessel();
        Container holdBay1 = Scene.BoxOfMass(10m);
        Container deckBay3 = Scene.BoxOfMass(10m);
        plan.Assign(deckBay3, new SlotPosition(3, 0, 82));
        plan.Assign(holdBay1, new SlotPosition(1, 0, 2));

        IReadOnlyList<StowageAssignment> sequence = plan.DischargeSequence(Scene.Hamburg);

        Assert.Equal(new[] { holdBay1.Id, deckBay3.Id }, sequence.Select(a => a.Container.Id).ToArray());
    }

    [Fact]
    public void Only_the_boxes_discharged_at_the_port_are_lifted() {
        StowagePlan plan = Scene.PlanOnRoomyVessel();
        Container forHamburg = Scene.Box(Scene.Rotterdam, Scene.Hamburg).Generate();
        Container forFelixstowe = Scene.Box(Scene.Rotterdam, Scene.Felixstowe).Generate();
        plan.Assign(forHamburg, new SlotPosition(1, 0, 2));
        plan.Assign(forFelixstowe, new SlotPosition(3, 0, 2));

        Assert.Equal(new[] { forHamburg.Id }, plan.DischargeSequence(Scene.Hamburg).Select(a => a.Container.Id).ToArray());
        Assert.Equal(new[] { forFelixstowe.Id }, plan.DischargeSequence(Scene.Felixstowe).Select(a => a.Container.Id).ToArray());
    }

    [Fact]
    public void A_port_where_nothing_is_discharged_gives_an_empty_sequence() {
        StowagePlan plan = Scene.PlanOnRoomyVessel();
        plan.Assign(Scene.BoxOfMass(10m), new SlotPosition(1, 0, 2));

        Assert.Empty(plan.DischargeSequence(Scene.LeHavre));
    }

    [Fact]
    public void A_port_outside_the_rotation_is_refused() {
        StowagePlan plan = Scene.PlanOnRoomyVessel();

        Assert.Throws<ArgumentException>(() => plan.DischargeSequence(new PortCode("BEANR")));
    }

    [Fact]
    public void A_null_port_is_refused() {
        Assert.Throws<ArgumentNullException>(() => Scene.PlanOnRoomyVessel().DischargeSequence(null!));
    }

}

public sealed class StowagePlanUsableSlotsTests {

    [Fact]
    public void On_an_empty_plan_a_twenty_foot_box_can_go_to_the_bottom_of_every_odd_bay_and_to_the_deck() {
        // Arrange: 5 bays (1, 3, 5 take 20 ft), 3 rows, 2 hold tiers, 1 deck tier
        VesselProfile vessel = Scene.RoomyVessel().WithBayCount(5).WithRowCount(3).WithHoldTiers(2).WithDeckTiers(1).Generate();
        StowagePlan   plan   = new StowagePlan(vessel, Scene.FourPortRotation());

        // Act
        IReadOnlyList<SlotPosition> usable = plan.UsableSlotsFor(Scene.BoxOfMass(10m));

        // Assert: per odd bay and row, the hold floor and the deck floor
        Assert.Equal(3 * 3 * 2, usable.Count);
        Assert.All(usable, slot => Assert.True(slot.IsLowestOfSection));
        Assert.All(usable, slot => Assert.Equal(1, slot.Bay % 2));
    }

    [Fact]
    public void A_forty_foot_box_is_offered_the_even_bays() {
        VesselProfile vessel = Scene.RoomyVessel().WithBayCount(4).WithRowCount(2).WithHoldTiers(1).WithDeckTiers(0).Generate();
        StowagePlan   plan   = new StowagePlan(vessel, Scene.FourPortRotation());
        Container     fortyFooter = Scene.Box(Scene.Rotterdam, Scene.Hamburg).WithSize(ContainerSize.FortyFoot).Generate();

        IReadOnlyList<SlotPosition> usable = plan.UsableSlotsFor(fortyFooter);

        Assert.Equal(4, usable.Count);
        Assert.All(usable, slot => Assert.Equal(0, slot.Bay % 2));
    }

    [Fact]
    public void A_slot_on_top_of_a_lighter_box_is_not_offered_to_a_heavier_one() {
        // Arrange: 1 bay, 1 row, 3 hold tiers, no deck; a 10 t box on the floor
        VesselProfile vessel = Scene.RoomyVessel().WithBayCount(1).WithRowCount(1).WithHoldTiers(3).WithDeckTiers(0).Generate();
        StowagePlan   plan   = new StowagePlan(vessel, Scene.FourPortRotation());
        plan.Assign(Scene.BoxOfMass(10m), new SlotPosition(1, 0, 2));

        // Assert
        Assert.Equal(new[] { new SlotPosition(1, 0, 4) }, plan.UsableSlotsFor(Scene.BoxOfMass(9m)).ToArray());
        Assert.Empty(plan.UsableSlotsFor(Scene.BoxOfMass(11m)));
    }

    [Fact]
    public void An_occupied_slot_is_not_offered() {
        VesselProfile vessel = Scene.RoomyVessel().WithBayCount(1).WithRowCount(1).WithHoldTiers(1).WithDeckTiers(0).Generate();
        StowagePlan   plan   = new StowagePlan(vessel, Scene.FourPortRotation());
        plan.Assign(Scene.BoxOfMass(10m), new SlotPosition(1, 0, 2));

        Assert.Empty(plan.UsableSlotsFor(Scene.BoxOfMass(10m)));
    }

    [Fact]
    public void A_box_already_in_the_plan_has_no_usable_slot() {
        VesselProfile vessel = Scene.RoomyVessel().WithBayCount(3).WithRowCount(3).Generate();
        StowagePlan   plan   = new StowagePlan(vessel, Scene.FourPortRotation());
        Container     container = Scene.BoxOfMass(10m);
        plan.Assign(container, new SlotPosition(1, 0, 2));

        Assert.Empty(plan.UsableSlotsFor(container));
    }

    [Fact]
    public void Explosives_are_offered_deck_slots_only() {
        VesselProfile vessel = Scene.RoomyVessel().WithBayCount(1).WithRowCount(2).WithHoldTiers(2).WithDeckTiers(2).Generate();
        StowagePlan   plan   = new StowagePlan(vessel, Scene.FourPortRotation());

        IReadOnlyList<SlotPosition> usable = plan.UsableSlotsFor(Scene.Dangerous(HazardClass.Explosives));

        Assert.Equal(2, usable.Count);
        Assert.All(usable, slot => Assert.True(slot.IsOnDeck));
    }

    [Fact]
    public void A_free_slot_beside_incompatible_goods_is_not_offered() {
        // Arrange: 1 bay, 3 rows (0, 1, 2), 1 hold tier; an oxidizer in the centre row
        VesselProfile vessel = Scene.RoomyVessel().WithBayCount(1).WithRowCount(3).WithHoldTiers(1).WithDeckTiers(0).Generate();
        StowagePlan   plan   = new StowagePlan(vessel, Scene.FourPortRotation());
        plan.Assign(Scene.Dangerous(HazardClass.Oxidizers), new SlotPosition(1, 0, 2));

        // Act: a flammable box is refused in rows 1 and 2, which touch the centre row; the plan has no other free slot
        IReadOnlyList<SlotPosition> usable = plan.UsableSlotsFor(Scene.Dangerous(HazardClass.FlammableSolids));

        // Assert
        Assert.Empty(usable);
    }

    [Fact]
    public void A_free_slot_can_be_unusable_through_the_stack_weight() {
        VesselProfile vessel = Scene.RoomyVessel().WithBayCount(1).WithRowCount(1).WithHoldTiers(3).WithDeckTiers(0).WithMaxStackWeightTonnes(25m).Generate();
        StowagePlan   plan   = new StowagePlan(vessel, Scene.FourPortRotation());
        plan.Assign(Scene.BoxOfMass(20m), new SlotPosition(1, 0, 2));

        Assert.Empty(plan.UsableSlotsFor(Scene.BoxOfMass(6m)));
        Assert.Single(plan.UsableSlotsFor(Scene.BoxOfMass(5m)));
    }

    [Fact]
    public void Every_slot_offered_is_one_Check_allows_and_Assign_accepts() {
        // Arrange
        VesselProfile vessel = Scene.RoomyVessel().WithBayCount(3).WithRowCount(3).WithHoldTiers(3).WithDeckTiers(2).Generate();
        StowagePlan   plan   = new StowagePlan(vessel, Scene.FourPortRotation());
        plan.Assign(Scene.BoxOfMass(20m), new SlotPosition(1, 0, 2));
        plan.Assign(Scene.Dangerous(HazardClass.Oxidizers), new SlotPosition(1, 1, 2));
        Container candidate = Scene.BoxOfMass(5m);

        // Act
        IReadOnlyList<SlotPosition> usable = plan.UsableSlotsFor(candidate);
        SlotPosition                firstUsable = usable.First();

        // Assert
        Assert.NotEmpty(usable);
        Assert.All(usable, slot => Assert.Equal(StowageViolation.None, plan.Check(candidate, slot)));
        Assert.Equal(StowageViolation.None, plan.Check(candidate, firstUsable));
        plan.Assign(candidate, firstUsable);
    }

    [Fact]
    public void A_null_container_is_refused() {
        Assert.Throws<ArgumentNullException>(() => Scene.PlanOnRoomyVessel().UsableSlotsFor(null!));
    }

}
