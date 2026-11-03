using JustDummies;
using VesselStowage.Domain.Tests.Generators;
using Xunit;

namespace VesselStowage.Domain.Tests;

public sealed class StowagePlanStackingTests {

    // --- a box needs one under it, except at the bottom of the hold or of the deck

    [Fact]
    public void The_lowest_slot_of_the_hold_and_of_the_deck_need_no_support() {
        // Arrange
        Voyage      voyage    = Voyage.Draw();
        StowagePlan plan      = voyage.EmptyPlan();
        Container   inHold    = voyage.Box().Generate();
        Container   onDeck    = voyage.Box().Generate();

        // Assert: the deck needs nothing under it, not even a box in the hold
        PlanAssert.Accepts(plan, inHold, new SlotPosition(1, 0, 2));
        PlanAssert.Accepts(plan, onDeck, new SlotPosition(3, 0, 82));
    }

    [Theory]
    [InlineData(1, 0, 4)]
    [InlineData(1, 0, 8)]
    [InlineData(1, 0, 84)]
    public void A_box_cannot_float_above_an_empty_slot(int bay, int row, int tier) {
        // Arrange
        Voyage      voyage    = Voyage.Draw();
        StowagePlan plan      = voyage.EmptyPlan();
        Container   container = voyage.Box().Generate();

        // Assert
        PlanAssert.Refuses(plan, container, new SlotPosition(bay, row, tier), StowageViolation.NoSupportBelow);
    }

    [Fact]
    public void A_box_in_the_next_row_or_the_next_bay_does_not_support_a_box_above_it() {
        // Arrange
        Voyage      voyage         = Voyage.Draw();
        StowagePlan plan           = voyage.EmptyPlan();
        Container   neighbourInRow = voyage.Box().Generate();
        Container   neighbourInBay = voyage.Box().Generate();
        Container   container      = voyage.Box().Generate();
        plan.Assign(neighbourInRow, new SlotPosition(1, 1, 2));
        plan.Assign(neighbourInBay, new SlotPosition(3, 0, 2));

        // Assert
        PlanAssert.Refuses(plan, container, new SlotPosition(1, 0, 4), StowageViolation.NoSupportBelow);
    }

    [Fact]
    public void A_box_on_deck_is_not_supported_by_the_hold_below_the_hatch_cover() {
        // Arrange
        Voyage      voyage    = Voyage.Draw();
        StowagePlan plan      = voyage.EmptyPlan();
        Container   container = voyage.Box().Generate();

        // Assert: the second deck tier needs a box in the first deck tier
        PlanAssert.Refuses(plan, container, new SlotPosition(1, 0, 84), StowageViolation.NoSupportBelow);
    }

    [Fact]
    public void A_box_is_stowed_on_top_of_another_in_the_same_stack() {
        // Arrange
        Voyage      voyage = Voyage.Draw();
        StowagePlan plan   = voyage.EmptyPlan();
        Container   bottom = voyage.Box().Weighing(20m).Generate();
        Container   top    = voyage.Box().Weighing(10m).Generate();
        plan.Assign(bottom, new SlotPosition(1, 0, 2));

        // Assert
        PlanAssert.Accepts(plan, top, new SlotPosition(1, 0, 4));
    }

    // --- a box is never heavier than the one it stands on

    [Fact]
    public void A_box_heavier_than_the_one_below_is_refused() {
        // Arrange
        Voyage      voyage = Voyage.Draw();
        StowagePlan plan   = voyage.EmptyPlan();
        Container   bottom = voyage.Box().Weighing(20m).Generate();
        Container   top    = voyage.Box().Weighing(20.01m).Generate();
        plan.Assign(bottom, new SlotPosition(1, 0, 2));

        // Assert
        PlanAssert.Refuses(plan, top, new SlotPosition(1, 0, 4), StowageViolation.HeavierThanContainerBelow);
    }

    [Fact]
    public void A_box_as_heavy_as_the_one_below_is_accepted() {
        // Arrange
        Voyage      voyage = Voyage.Draw();
        StowagePlan plan   = voyage.EmptyPlan();
        Container   bottom = voyage.Box().Weighing(20m).Generate();
        Container   top    = voyage.Box().Weighing(20m).Generate();
        plan.Assign(bottom, new SlotPosition(1, 0, 2));

        // Assert
        PlanAssert.Accepts(plan, top, new SlotPosition(1, 0, 4));
    }

    [Fact]
    public void The_weight_comparison_is_made_on_the_gross_mass_not_on_the_cargo() {
        // Arrange: the lower box is heavier gross (20 t) but carries less than the upper one (5 t of cargo against 12 t)
        Voyage      voyage = Voyage.Draw();
        StowagePlan plan   = voyage.EmptyPlan();
        Container   bottom = voyage.Box().OfTwentyFeet().WithTareTonnes(3.2m).Weighing(20m).Generate();
        Container   top    = voyage.Box().OfTwentyFeet().WithTareTonnes(1.8m).Weighing(19m).Generate();
        plan.Assign(bottom, new SlotPosition(1, 0, 2));

        // Assert
        PlanAssert.Accepts(plan, top, new SlotPosition(1, 0, 4));
    }

    [Fact]
    public void Only_the_box_directly_below_is_compared_for_weight() {
        // Arrange: 20 t, 10 t, then 15 t is heavier than the 10 t below it, though lighter than the 20 t at the bottom
        Voyage      voyage = Voyage.Draw();
        StowagePlan plan   = voyage.EmptyPlan();
        plan.Assign(voyage.Box().Weighing(20m).Generate(), new SlotPosition(1, 0, 2));
        plan.Assign(voyage.Box().Weighing(10m).Generate(), new SlotPosition(1, 0, 4));
        Container third = voyage.Box().Weighing(15m).Generate();

        // Assert
        PlanAssert.Refuses(plan, third, new SlotPosition(1, 0, 6), StowageViolation.HeavierThanContainerBelow);
    }

    // --- the weight of a stack

    private static AnyVesselProfile VesselWithAStackLimitOf(decimal tonnes) {
        return Any.VesselProfile().Roomy().WithMaxStackWeightTonnes(tonnes);
    }

    [Fact]
    public void A_stack_may_weigh_exactly_the_limit_of_the_vessel() {
        // Arrange: 30 t + 20 t = 50 t
        Voyage      voyage = Voyage.Draw();
        StowagePlan plan   = voyage.EmptyPlanOf(VesselWithAStackLimitOf(50m));
        plan.Assign(voyage.Box().Weighing(30m).Generate(), new SlotPosition(1, 0, 2));
        Container top = voyage.Box().Weighing(20m).Generate();

        // Assert
        PlanAssert.Accepts(plan, top, new SlotPosition(1, 0, 4));
    }

    [Fact]
    public void A_stack_over_the_limit_of_the_vessel_is_refused() {
        // Arrange: 30 t + 20.01 t = 50.01 t
        Voyage      voyage = Voyage.Draw();
        StowagePlan plan   = voyage.EmptyPlanOf(VesselWithAStackLimitOf(50m));
        plan.Assign(voyage.Box().Weighing(30m).Generate(), new SlotPosition(1, 0, 2));
        Container top = voyage.Box().Weighing(20.01m).Generate();

        // Assert
        PlanAssert.Refuses(plan, top, new SlotPosition(1, 0, 4), StowageViolation.StackWeightExceeded);
    }

    [Fact]
    public void A_single_box_heavier_than_the_stack_limit_is_refused() {
        // Arrange
        Voyage      voyage    = Voyage.Draw();
        StowagePlan plan      = voyage.EmptyPlanOf(VesselWithAStackLimitOf(25m));
        Container   container = voyage.Box().Weighing(25.01m).Generate();

        // Assert
        PlanAssert.Refuses(plan, container, new SlotPosition(1, 0, 2), StowageViolation.StackWeightExceeded);
    }

    [Fact]
    public void The_whole_stack_counts_not_only_the_box_below() {
        // Arrange: 15 + 15 + 15 = 45 t over a limit of 40 t, though no pair of boxes exceeds it
        Voyage      voyage = Voyage.Draw();
        StowagePlan plan   = voyage.EmptyPlanOf(VesselWithAStackLimitOf(40m));
        plan.Assign(voyage.Box().Weighing(15m).Generate(), new SlotPosition(1, 0, 2));
        plan.Assign(voyage.Box().Weighing(15m).Generate(), new SlotPosition(1, 0, 4));
        Container third = voyage.Box().Weighing(15m).Generate();

        // Assert
        PlanAssert.Refuses(plan, third, new SlotPosition(1, 0, 6), StowageViolation.StackWeightExceeded);
    }

    [Fact]
    public void Boxes_in_other_rows_and_other_bays_do_not_weigh_on_the_stack() {
        // Arrange: a limit of 30 t, with a 30 t box in the next row and another in the next bay
        Voyage      voyage = Voyage.Draw();
        StowagePlan plan   = voyage.EmptyPlanOf(VesselWithAStackLimitOf(30m));
        plan.Assign(voyage.Box().Weighing(30m).Generate(), new SlotPosition(1, 1, 2));
        plan.Assign(voyage.Box().Weighing(30m).Generate(), new SlotPosition(3, 0, 2));
        Container container = voyage.Box().Weighing(30m).Generate();

        // Assert
        PlanAssert.Accepts(plan, container, new SlotPosition(1, 0, 2));
    }

    [Fact]
    public void The_stack_on_deck_is_weighed_apart_from_the_stack_in_the_hold_below_it() {
        // Arrange: a 30 t box in the hold, a limit of 30 t, and another 30 t box in the same bay and row on deck
        Voyage      voyage = Voyage.Draw();
        StowagePlan plan   = voyage.EmptyPlanOf(VesselWithAStackLimitOf(30m));
        plan.Assign(voyage.Box().Weighing(30m).Generate(), new SlotPosition(1, 0, 2));
        Container onDeck = voyage.Box().Weighing(30m).Generate();

        // Assert
        PlanAssert.Accepts(plan, onDeck, new SlotPosition(1, 0, 82));
    }

    [Fact]
    public void The_weight_of_a_box_is_checked_before_the_weight_of_the_stack() {
        // Arrange: a lone 30 t box heavier than the one below, in a stack that would also be too heavy
        Voyage      voyage = Voyage.Draw();
        StowagePlan plan   = voyage.EmptyPlanOf(VesselWithAStackLimitOf(40m));
        plan.Assign(voyage.Box().Weighing(20m).Generate(), new SlotPosition(1, 0, 2));
        Container top = voyage.Box().Weighing(21m).Generate();

        // Assert: 41 t over 40 t, and 21 t over 20 t
        PlanAssert.Refuses(plan, top, new SlotPosition(1, 0, 4), StowageViolation.HeavierThanContainerBelow);
    }

    // --- a full stack, up to the top tier of the hold and of the deck

    [Fact]
    public void A_stack_fills_the_hold_up_to_its_top_tier() {
        // Arrange: ten tiers in the hold, each box a little lighter than the one below
        Voyage      voyage = Voyage.Draw();
        StowagePlan plan   = voyage.EmptyPlanOf(Any.VesselProfile().Roomy().WithHoldTiers(10));

        // Act
        for (int level = 0; level < 10; level++) {
            plan.Assign(voyage.Box().Weighing(20m - level).Generate(), new SlotPosition(1, 0, 2 + 2 * level));
        }

        // Assert
        Assert.Equal(10, plan.Assignments.Count);
        Assert.NotNull(plan.At(new SlotPosition(1, 0, 20)));
    }

    [Fact]
    public void A_stack_fills_the_deck_up_to_its_top_tier() {
        // Arrange
        Voyage      voyage = Voyage.Draw();
        StowagePlan plan   = voyage.EmptyPlanOf(Any.VesselProfile().Roomy().WithDeckTiers(9));

        // Act
        for (int level = 0; level < 9; level++) {
            plan.Assign(voyage.Box().Weighing(20m - level).Generate(), new SlotPosition(1, 0, 82 + 2 * level));
        }

        // Assert
        Assert.Equal(9, plan.Assignments.Count);
        Assert.NotNull(plan.At(new SlotPosition(1, 0, 98)));
    }

}
