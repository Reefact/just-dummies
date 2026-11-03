using JustDummies;
using VesselStowage.Domain.Tests.Generators;
using Xunit;

namespace VesselStowage.Domain.Tests;

public sealed class StowagePlanRemovalTests {

    [Fact]
    public void Removing_a_box_frees_its_slot_and_forgets_its_id() {
        // Arrange
        Voyage       voyage    = Voyage.Draw();
        StowagePlan  plan      = voyage.EmptyPlan();
        Container    container = voyage.Box().Generate();
        SlotPosition slot      = new SlotPosition(1, 0, 2);
        plan.Assign(container, slot);

        // Act
        plan.Remove(container.Id);

        // Assert
        Assert.Null(plan.At(slot));
        Assert.Null(plan.Find(container.Id));
        Assert.Empty(plan.Assignments);
    }

    [Fact]
    public void A_removed_box_can_be_stowed_again_elsewhere_and_its_slot_given_to_another() {
        // Arrange
        Voyage       voyage         = Voyage.Draw();
        StowagePlan  plan           = voyage.EmptyPlan();
        Container    container      = voyage.Box().Generate();
        Container    otherContainer = voyage.Box().Generate();
        plan.Assign(container, new SlotPosition(1, 0, 2));
        plan.Remove(container.Id);

        // Assert
        PlanAssert.Accepts(plan, otherContainer, new SlotPosition(1, 0, 2));
        PlanAssert.Accepts(plan, container, new SlotPosition(1, 1, 2));
    }

    [Fact]
    public void Removing_a_box_leaves_the_others_where_they_are() {
        // Arrange
        Voyage      voyage    = Voyage.Draw();
        StowagePlan plan      = voyage.EmptyPlan();
        Container   removed   = voyage.Box().Generate();
        Container   remaining = voyage.Box().Generate();
        plan.Assign(removed, new SlotPosition(1, 0, 2));
        plan.Assign(remaining, new SlotPosition(1, 1, 2));

        // Act
        plan.Remove(removed.Id);

        // Assert
        Assert.Same(remaining, plan.At(new SlotPosition(1, 1, 2))!.Container);
        Assert.Same(remaining, Assert.Single(plan.Assignments).Container);
    }

    [Fact]
    public void A_box_that_is_not_in_the_plan_cannot_be_removed() {
        // Arrange
        StowagePlan plan  = Voyage.Draw().EmptyPlan();
        ContainerId anyId = Any.ContainerId().Generate();

        // Act
        StowageRefusedException refusal = Assert.Throws<StowageRefusedException>(() => plan.Remove(anyId));

        // Assert
        Assert.Equal(StowageViolation.NotStowed, refusal.Violation);
        Assert.Contains(anyId.Value, refusal.Message);
    }

    [Fact]
    public void A_box_removed_twice_is_refused_the_second_time() {
        // Arrange
        Voyage      voyage    = Voyage.Draw();
        StowagePlan plan      = voyage.EmptyPlan();
        Container   container = voyage.Box().Generate();
        plan.Assign(container, new SlotPosition(1, 0, 2));
        plan.Remove(container.Id);

        // Assert
        Assert.Equal(StowageViolation.NotStowed, Assert.Throws<StowageRefusedException>(() => plan.Remove(container.Id)).Violation);
    }

    [Fact]
    public void A_missing_id_cannot_be_removed() {
        Assert.Throws<ArgumentNullException>(() => Voyage.Draw().EmptyPlan().Remove(null!));
    }

    [Fact]
    public void A_box_with_another_stacked_on_it_cannot_be_removed() {
        // Arrange
        Voyage      voyage = Voyage.Draw();
        StowagePlan plan   = voyage.EmptyPlan();
        Container   bottom = voyage.Box().Weighing(20m).Generate();
        Container   top    = voyage.Box().Weighing(10m).Generate();
        plan.Assign(bottom, new SlotPosition(1, 0, 2));
        plan.Assign(top, new SlotPosition(1, 0, 4));

        // Act
        StowageRefusedException refusal = Assert.Throws<StowageRefusedException>(() => plan.Remove(bottom.Id));

        // Assert
        Assert.Equal(StowageViolation.ContainerStackedAbove, refusal.Violation);
        Assert.Contains("010004", refusal.Message);
        Assert.Same(bottom, plan.Find(bottom.Id)!.Container);
        Assert.Equal(2, plan.Assignments.Count);
    }

    [Fact]
    public void A_box_can_be_removed_once_the_one_stacked_on_it_has_gone() {
        // Arrange
        Voyage      voyage = Voyage.Draw();
        StowagePlan plan   = voyage.EmptyPlan();
        Container   bottom = voyage.Box().Weighing(20m).Generate();
        Container   top    = voyage.Box().Weighing(10m).Generate();
        plan.Assign(bottom, new SlotPosition(1, 0, 2));
        plan.Assign(top, new SlotPosition(1, 0, 4));

        // Act
        plan.Remove(top.Id);
        plan.Remove(bottom.Id);

        // Assert
        Assert.Empty(plan.Assignments);
    }

    [Fact]
    public void A_box_on_deck_can_be_removed_whatever_is_in_the_hold_below() {
        // Arrange
        Voyage      voyage = Voyage.Draw();
        StowagePlan plan   = voyage.EmptyPlan();
        Container   inHold = voyage.Box().Generate();
        Container   onDeck = voyage.Box().Generate();
        plan.Assign(inHold, new SlotPosition(1, 0, 2));
        plan.Assign(onDeck, new SlotPosition(1, 0, 82));

        // Act
        plan.Remove(inHold.Id);

        // Assert
        Assert.Same(onDeck, Assert.Single(plan.Assignments).Container);
    }

    [Fact]
    public void The_top_box_of_a_full_hold_stack_can_be_removed() {
        // Arrange: ten tiers in the hold, the last one at tier 20
        Voyage      voyage = Voyage.Draw();
        StowagePlan plan   = voyage.EmptyPlanOf(Any.VesselProfile().Roomy().WithHoldTiers(10));
        Container   top    = null!;
        for (int level = 0; level < 10; level++) {
            top = voyage.Box().Weighing(20m - level).Generate();
            plan.Assign(top, new SlotPosition(1, 0, 2 + 2 * level));
        }

        // Act
        plan.Remove(top.Id);

        // Assert
        Assert.Equal(9, plan.Assignments.Count);
        Assert.Null(plan.At(new SlotPosition(1, 0, 20)));
    }

    [Fact]
    public void The_top_box_of_a_full_deck_stack_can_be_removed() {
        // Arrange: nine tiers on deck, the last one at tier 98
        Voyage      voyage = Voyage.Draw();
        StowagePlan plan   = voyage.EmptyPlanOf(Any.VesselProfile().Roomy().WithDeckTiers(9));
        Container   top    = null!;
        for (int level = 0; level < 9; level++) {
            top = voyage.Box().Weighing(20m - level).Generate();
            plan.Assign(top, new SlotPosition(1, 0, 82 + 2 * level));
        }

        // Act
        plan.Remove(top.Id);

        // Assert
        Assert.Equal(8, plan.Assignments.Count);
    }

    [Fact]
    public void A_box_in_the_top_hold_tier_is_not_blocked_by_the_first_deck_tier_above_the_hatch_cover() {
        // Arrange: a hold of two tiers, a box at the top of the hold and one on deck in the same row
        Voyage      voyage = Voyage.Draw();
        StowagePlan plan   = voyage.EmptyPlanOf(Any.VesselProfile().Roomy().WithHoldTiers(2));
        Container   bottom = voyage.Box().Weighing(20m).Generate();
        Container   top    = voyage.Box().Weighing(10m).Generate();
        plan.Assign(bottom, new SlotPosition(1, 0, 2));
        plan.Assign(top, new SlotPosition(1, 0, 4));
        plan.Assign(voyage.Box().Generate(), new SlotPosition(1, 0, 82));

        // Act
        plan.Remove(top.Id);

        // Assert
        Assert.Equal(2, plan.Assignments.Count);
    }

}
