using JustDummies;
using VesselStowage.Domain.Tests.Generators;
using Xunit;

namespace VesselStowage.Domain.Tests;

public sealed class StowagePlanAssignTests {

    // A vessel of 3 bays, 2 rows (0 and 1), 2 hold tiers and 1 deck tier: the geometry is what these tests are about.
    private static AnyVesselProfile ThreeBayVessel() {
        return Any.VesselProfile().Roomy().WithBayCount(3).WithRowCount(2).WithHoldTiers(2).WithDeckTiers(1);
    }

    [Fact]
    public void A_plan_knows_its_vessel_and_its_rotation() {
        // Arrange
        VesselProfile anyVessel   = Any.VesselProfile().Generate();
        Rotation      anyRotation = Any.Rotation().Generate();

        // Act
        StowagePlan plan = new StowagePlan(anyVessel, anyRotation);

        // Assert
        Assert.Same(anyVessel, plan.Vessel);
        Assert.Same(anyRotation, plan.Rotation);
        Assert.Empty(plan.Assignments);
    }

    [Fact]
    public void A_plan_needs_a_vessel_and_a_rotation() {
        // Arrange
        VesselProfile anyVessel   = Any.VesselProfile().Generate();
        Rotation      anyRotation = Any.Rotation().Generate();

        // Assert
        Assert.Throws<ArgumentNullException>(() => new StowagePlan(null!, anyRotation));
        Assert.Throws<ArgumentNullException>(() => new StowagePlan(anyVessel, null!));
    }

    [Fact]
    public void Assign_stows_the_box_in_the_slot_and_the_plan_finds_it_by_slot_and_by_id() {
        // Arrange
        Voyage       voyage    = Voyage.Draw();
        StowagePlan  plan      = voyage.EmptyPlan();
        Container    container = voyage.Box().Generate();
        SlotPosition slot      = new SlotPosition(1, 0, 2);

        // Act
        StowageAssignment assignment = plan.Assign(container, slot);

        // Assert
        Assert.Same(container, assignment.Container);
        Assert.Equal(slot, assignment.Slot);
        Assert.Same(assignment, plan.At(new SlotPosition(1, 0, 2)));
        Assert.Same(assignment, plan.Find(container.Id));
        Assert.Same(assignment, Assert.Single(plan.Assignments));
    }

    [Fact]
    public void A_forty_foot_box_is_stowed_in_an_even_bay() {
        // Arrange
        Voyage      voyage    = Voyage.Draw();
        StowagePlan plan      = voyage.EmptyPlan();
        Container   container = voyage.Box().OfFortyFeet().Generate();

        // Act
        StowageAssignment assignment = PlanAssert.Accepts(plan, container, new SlotPosition(2, 0, 2));

        // Assert
        Assert.Same(container, assignment.Container);
    }

    [Fact]
    public void Nothing_is_found_in_an_empty_plan() {
        // Arrange
        Voyage       voyage = Voyage.Draw();
        StowagePlan  plan   = voyage.EmptyPlan();
        ContainerId  anyId  = Any.ContainerId().Generate();

        // Assert
        Assert.Null(plan.At(new SlotPosition(1, 0, 2)));
        Assert.Null(plan.Find(anyId));
    }

    [Fact]
    public void Looking_for_nothing_is_refused() {
        // Arrange
        StowagePlan plan = Voyage.Draw().EmptyPlan();

        // Assert
        Assert.Throws<ArgumentNullException>(() => plan.At(null!));
        Assert.Throws<ArgumentNullException>(() => plan.Find(null!));
    }

    [Fact]
    public void Assigning_or_checking_nothing_is_refused() {
        // Arrange
        Voyage      voyage    = Voyage.Draw();
        StowagePlan plan      = voyage.EmptyPlan();
        Container   container = voyage.Box().Generate();
        SlotPosition slot     = new SlotPosition(1, 0, 2);

        // Assert
        Assert.Throws<ArgumentNullException>(() => plan.Assign(null!, slot));
        Assert.Throws<ArgumentNullException>(() => plan.Assign(container, null!));
        Assert.Throws<ArgumentNullException>(() => plan.Check(null!, slot));
        Assert.Throws<ArgumentNullException>(() => plan.Check(container, null!));
    }

    // --- the slot must exist, take the box's size, be free, and the box not be stowed yet

    [Theory]
    [InlineData(4, 0, 2)]  // a fourth bay on a vessel of three
    [InlineData(1, 2, 2)]  // a third row on a vessel of rows 0 and 1
    [InlineData(1, 0, 6)]  // a third tier in a hold of two
    [InlineData(1, 0, 84)] // a second tier on a deck of one
    public void A_slot_beyond_the_geometry_of_the_vessel_is_refused(int bay, int row, int tier) {
        // Arrange
        Voyage      voyage    = Voyage.Draw();
        StowagePlan plan      = voyage.EmptyPlanOf(ThreeBayVessel());
        Container   container = voyage.Box().Generate();

        // Assert
        PlanAssert.Refuses(plan, container, new SlotPosition(bay, row, tier), StowageViolation.SlotOutsideVessel);
    }

    [Fact]
    public void A_vessel_without_deck_tiers_has_no_slot_on_deck() {
        // Arrange
        Voyage      voyage    = Voyage.Draw();
        StowagePlan plan      = voyage.EmptyPlanOf(ThreeBayVessel().WithDeckTiers(0));
        Container   container = voyage.Box().Generate();

        // Assert
        PlanAssert.Refuses(plan, container, new SlotPosition(1, 0, 82), StowageViolation.SlotOutsideVessel);
    }

    [Fact]
    public void A_forty_foot_box_is_refused_in_an_odd_bay() {
        // Arrange
        Voyage      voyage    = Voyage.Draw();
        StowagePlan plan      = voyage.EmptyPlan();
        Container   container = voyage.Box().OfFortyFeet().Generate();

        // Assert
        PlanAssert.Refuses(plan, container, new SlotPosition(1, 0, 2), StowageViolation.BayDoesNotFitSize);
    }

    [Fact]
    public void A_twenty_foot_box_is_refused_in_an_even_bay() {
        // Arrange
        Voyage      voyage    = Voyage.Draw();
        StowagePlan plan      = voyage.EmptyPlan();
        Container   container = voyage.Box().OfTwentyFeet().Generate();

        // Assert
        PlanAssert.Refuses(plan, container, new SlotPosition(2, 0, 2), StowageViolation.BayDoesNotFitSize);
    }

    [Fact]
    public void A_box_already_in_the_plan_cannot_be_stowed_again_elsewhere() {
        // Arrange
        Voyage      voyage    = Voyage.Draw();
        StowagePlan plan      = voyage.EmptyPlan();
        Container   container = voyage.Box().Generate();
        plan.Assign(container, new SlotPosition(1, 0, 2));

        // Assert
        PlanAssert.Refuses(plan, container, new SlotPosition(1, 1, 2), StowageViolation.AlreadyStowed);
    }

    [Fact]
    public void A_stowed_box_is_never_stowed_twice_not_even_where_it_already_stands() {
        // Arrange
        Voyage      voyage    = Voyage.Draw();
        StowagePlan plan      = voyage.EmptyPlan();
        Container   container = voyage.Box().Generate();
        plan.Assign(container, new SlotPosition(1, 0, 2));

        // Assert
        PlanAssert.Refuses(plan, container, new SlotPosition(1, 0, 2), StowageViolation.AlreadyStowed);
    }

    [Fact]
    public void An_occupied_slot_is_refused() {
        // Arrange
        Voyage      voyage         = Voyage.Draw();
        StowagePlan plan           = voyage.EmptyPlan();
        Container   container      = voyage.Box().Generate();
        Container   otherContainer = voyage.Box().Generate();
        plan.Assign(container, new SlotPosition(1, 0, 2));

        // Assert
        PlanAssert.Refuses(plan, otherContainer, new SlotPosition(1, 0, 2), StowageViolation.SlotOccupied);
    }

    [Fact]
    public void A_slot_outside_the_vessel_is_reported_before_a_size_mismatch() {
        // Arrange
        Voyage      voyage    = Voyage.Draw();
        StowagePlan plan      = voyage.EmptyPlanOf(ThreeBayVessel());
        Container   container = voyage.Box().OfFortyFeet().Generate();

        // Assert: bay 5 is beyond the three bays, and odd
        PlanAssert.Refuses(plan, container, new SlotPosition(5, 0, 2), StowageViolation.SlotOutsideVessel);
    }

    [Fact]
    public void A_size_mismatch_is_reported_before_the_box_being_already_stowed() {
        // Arrange
        Voyage      voyage    = Voyage.Draw();
        StowagePlan plan      = voyage.EmptyPlan();
        Container   container = voyage.Box().OfTwentyFeet().Generate();
        plan.Assign(container, new SlotPosition(1, 0, 2));

        // Assert
        PlanAssert.Refuses(plan, container, new SlotPosition(2, 0, 2), StowageViolation.BayDoesNotFitSize);
    }

    // --- the box must load before it discharges, at ports the vessel calls at

    [Fact]
    public void A_box_loaded_at_a_port_the_vessel_does_not_call_at_is_refused() {
        // Arrange
        Voyage      voyage    = Voyage.Draw();
        StowagePlan plan      = voyage.EmptyPlan();
        Container   container = voyage.Box().Sailing(voyage.Elsewhere(), voyage.SecondCall).Generate();

        // Assert
        PlanAssert.Refuses(plan, container, new SlotPosition(1, 0, 2), StowageViolation.PortNotInRotation);
    }

    [Fact]
    public void A_box_discharged_at_a_port_the_vessel_does_not_call_at_is_refused() {
        // Arrange
        Voyage      voyage    = Voyage.Draw();
        StowagePlan plan      = voyage.EmptyPlan();
        Container   container = voyage.Box().Sailing(voyage.FirstCall, voyage.Elsewhere()).Generate();

        // Assert
        PlanAssert.Refuses(plan, container, new SlotPosition(1, 0, 2), StowageViolation.PortNotInRotation);
    }

    [Fact]
    public void A_box_discharged_at_a_port_called_before_the_one_it_is_loaded_at_is_refused() {
        // Arrange
        Voyage      voyage    = Voyage.Draw();
        StowagePlan plan      = voyage.EmptyPlan();
        Container   container = voyage.Box().Sailing(voyage.ThirdCall, voyage.SecondCall).Generate();

        // Assert
        PlanAssert.Refuses(plan, container, new SlotPosition(1, 0, 2), StowageViolation.PortNotInRotation);
    }

    [Fact]
    public void A_box_may_stay_aboard_over_several_calls() {
        // Arrange
        Voyage      voyage    = Voyage.Draw();
        StowagePlan plan      = voyage.EmptyPlan();
        Container   container = voyage.Box().Sailing(voyage.FirstCall, voyage.FourthCall).Generate();

        // Assert
        PlanAssert.Accepts(plan, container, new SlotPosition(1, 0, 2));
    }

    [Fact]
    public void A_box_may_be_loaded_at_any_call_but_the_last() {
        // Arrange
        Voyage      voyage    = Voyage.Draw();
        StowagePlan plan      = voyage.EmptyPlan();
        Container   container = voyage.Box().Sailing(voyage.ThirdCall, voyage.FourthCall).Generate();

        // Assert
        PlanAssert.Accepts(plan, container, new SlotPosition(1, 0, 2));
    }

    [Fact]
    public void The_route_is_checked_before_the_explosives_rule() {
        // Arrange
        Voyage      voyage    = Voyage.Draw();
        StowagePlan plan      = voyage.EmptyPlan();
        Container   container = voyage.Box().Carrying(HazardClass.Explosives).Sailing(voyage.ThirdCall, voyage.SecondCall).Generate();

        // Assert: in the hold, where explosives are refused too
        PlanAssert.Refuses(plan, container, new SlotPosition(1, 0, 2), StowageViolation.PortNotInRotation);
    }

    // --- explosives

    [Fact]
    public void Explosives_are_refused_in_the_hold() {
        // Arrange
        Voyage      voyage    = Voyage.Draw();
        StowagePlan plan      = voyage.EmptyPlan();
        Container   container = voyage.Box().Carrying(HazardClass.Explosives).Generate();

        // Assert
        PlanAssert.Refuses(plan, container, new SlotPosition(1, 0, 2), StowageViolation.ExplosivesUnderDeck);
    }

    [Fact]
    public void Explosives_are_accepted_on_deck() {
        // Arrange
        Voyage      voyage    = Voyage.Draw();
        StowagePlan plan      = voyage.EmptyPlan();
        Container   container = voyage.Box().Carrying(HazardClass.Explosives).Generate();

        // Assert
        PlanAssert.Accepts(plan, container, new SlotPosition(1, 0, 82));
    }

    [Fact]
    public void The_explosives_rule_is_checked_before_the_support_below() {
        // Arrange
        Voyage      voyage    = Voyage.Draw();
        StowagePlan plan      = voyage.EmptyPlan();
        Container   container = voyage.Box().Carrying(HazardClass.Explosives).Generate();

        // Assert: tier 4 has nothing below it, and is in the hold
        PlanAssert.Refuses(plan, container, new SlotPosition(1, 0, 4), StowageViolation.ExplosivesUnderDeck);
    }

    [Theory]
    [InlineData(HazardClass.Gases)]
    [InlineData(HazardClass.FlammableLiquids)]
    [InlineData(HazardClass.Radioactive)]
    [InlineData(HazardClass.Miscellaneous)]
    public void Other_dangerous_goods_may_go_in_the_hold(HazardClass hazard) {
        // Arrange
        Voyage      voyage    = Voyage.Draw();
        StowagePlan plan      = voyage.EmptyPlan();
        Container   container = voyage.Box().Carrying(hazard).Generate();

        // Assert
        PlanAssert.Accepts(plan, container, new SlotPosition(1, 0, 2));
    }

}
