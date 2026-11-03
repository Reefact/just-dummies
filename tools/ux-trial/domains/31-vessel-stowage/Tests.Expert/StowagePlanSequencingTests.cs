using JustDummies;
using VesselStowage.Domain.Tests.Generators;
using Xunit;

namespace VesselStowage.Domain.Tests;

/// <summary>Overstow and hatch access: which box may sit over which, given the order of the calls.</summary>
public sealed class StowagePlanSequencingTests {

    // --- overstow: a box above must load no earlier and discharge no later than the box below

    // The lower box weighs 20 t and the upper one 10 t in every test: the upper must not be heavier,
    // and weight is not what these tests are about.
    private static (StowagePlan Plan, Container Upper) StackOf(Voyage voyage, (int From, int To) lower, (int From, int To) upper) {
        StowagePlan plan = voyage.EmptyPlan();
        plan.Assign(voyage.Box().Sailing(voyage.Rotation.Ports[lower.From], voyage.Rotation.Ports[lower.To]).Weighing(20m).Generate(), new SlotPosition(1, 0, 2));
        Container upperBox = voyage.Box().Sailing(voyage.Rotation.Ports[upper.From], voyage.Rotation.Ports[upper.To]).Weighing(10m).Generate();

        return (plan, upperBox);
    }

    [Theory]
    [InlineData(0, 1, 0, 1)] // same route
    [InlineData(0, 2, 1, 2)] // loaded later, discharged together
    [InlineData(0, 2, 0, 1)] // loaded together, discharged earlier
    [InlineData(0, 3, 1, 2)] // loaded later and discharged earlier: nested inside
    public void A_box_loaded_no_earlier_and_discharged_no_later_may_sit_on_top(int lowerFrom, int lowerTo, int upperFrom, int upperTo) {
        // Arrange
        Voyage voyage = Voyage.Draw();
        (StowagePlan plan, Container upper) = StackOf(voyage, (lowerFrom, lowerTo), (upperFrom, upperTo));

        // Assert
        PlanAssert.Accepts(plan, upper, new SlotPosition(1, 0, 4));
    }

    [Theory]
    [InlineData(0, 2, 0, 3)] // would be discharged after the box below
    [InlineData(1, 2, 0, 2)] // would be loaded before the box below
    [InlineData(1, 3, 0, 3)] // loaded earlier, discharged together
    [InlineData(0, 2, 1, 3)] // loaded later, but discharged later
    [InlineData(1, 2, 0, 3)] // wraps the box below on both sides
    public void A_box_that_would_block_the_one_below_is_refused(int lowerFrom, int lowerTo, int upperFrom, int upperTo) {
        // Arrange
        Voyage voyage = Voyage.Draw();
        (StowagePlan plan, Container upper) = StackOf(voyage, (lowerFrom, lowerTo), (upperFrom, upperTo));

        // Assert
        PlanAssert.Refuses(plan, upper, new SlotPosition(1, 0, 4), StowageViolation.Overstow);
    }

    [Fact]
    public void A_box_loaded_at_the_port_where_the_box_below_leaves_is_still_overstowing_it_if_it_leaves_later() {
        // Arrange: the box below leaves at the second call, the new one is loaded there and stays on to the third
        Voyage      voyage = Voyage.Draw();
        StowagePlan plan   = voyage.EmptyPlan();
        plan.Assign(voyage.Box().Sailing(voyage.FirstCall, voyage.SecondCall).Weighing(20m).Generate(), new SlotPosition(1, 0, 2));
        Container top = voyage.Box().Sailing(voyage.SecondCall, voyage.ThirdCall).Weighing(10m).Generate();

        // Assert
        PlanAssert.Refuses(plan, top, new SlotPosition(1, 0, 4), StowageViolation.Overstow);
    }

    [Fact]
    public void Boxes_in_other_stacks_are_not_overstowed() {
        // Arrange: a box in the next row leaves at the second call, the new one stays until the fourth
        Voyage      voyage = Voyage.Draw();
        StowagePlan plan   = voyage.EmptyPlan();
        plan.Assign(voyage.Box().Sailing(voyage.FirstCall, voyage.SecondCall).Generate(), new SlotPosition(1, 1, 2));
        plan.Assign(voyage.Box().Sailing(voyage.FirstCall, voyage.SecondCall).Generate(), new SlotPosition(3, 0, 2));
        Container container = voyage.Box().Sailing(voyage.SecondCall, voyage.FourthCall).Generate();

        // Assert
        PlanAssert.Accepts(plan, container, new SlotPosition(1, 0, 2));
    }

    // --- hatch access: the boxes on deck in a bay must load no earlier and discharge no later than the boxes in its hold

    private static StowagePlan PlanWithBoxIn(Voyage voyage, SlotPosition slot, int from, int to) {
        StowagePlan plan = voyage.EmptyPlan();
        plan.Assign(voyage.Box().Sailing(voyage.Rotation.Ports[from], voyage.Rotation.Ports[to]).Generate(), slot);

        return plan;
    }

    [Theory]
    [InlineData(0, 2, 0, 2)] // same route as the hold box
    [InlineData(0, 2, 1, 2)] // loaded later
    [InlineData(0, 2, 0, 1)] // discharged earlier
    [InlineData(0, 3, 1, 2)] // nested inside
    public void A_deck_box_that_leaves_the_hold_box_reachable_may_be_stowed_over_it(int holdFrom, int holdTo, int deckFrom, int deckTo) {
        // Arrange
        Voyage      voyage = Voyage.Draw();
        StowagePlan plan   = PlanWithBoxIn(voyage, new SlotPosition(1, 3, 2), holdFrom, holdTo);
        Container   deck   = voyage.Box().Sailing(voyage.Rotation.Ports[deckFrom], voyage.Rotation.Ports[deckTo]).Generate();

        // Assert: the hold box is in another row, the hatch cover covers the whole bay
        PlanAssert.Accepts(plan, deck, new SlotPosition(1, 0, 82));
    }

    [Theory]
    [InlineData(0, 2, 0, 3)] // the deck box would still be aboard when the hold box must come out
    [InlineData(1, 2, 0, 2)] // the deck box would be loaded before the hatch cover can be removed for the hold box
    [InlineData(1, 3, 0, 3)]
    public void A_deck_box_that_would_block_the_hold_of_its_bay_is_refused(int holdFrom, int holdTo, int deckFrom, int deckTo) {
        // Arrange
        Voyage      voyage = Voyage.Draw();
        StowagePlan plan   = PlanWithBoxIn(voyage, new SlotPosition(1, 3, 2), holdFrom, holdTo);
        Container   deck   = voyage.Box().Sailing(voyage.Rotation.Ports[deckFrom], voyage.Rotation.Ports[deckTo]).Generate();

        // Assert
        PlanAssert.Refuses(plan, deck, new SlotPosition(1, 0, 82), StowageViolation.HatchAccessBlocked);
    }

    [Theory]
    [InlineData(0, 2, 0, 2)] // same route
    [InlineData(1, 2, 0, 2)] // the deck box is loaded after the hold box
    [InlineData(0, 1, 0, 2)] // the deck box leaves before the hold box
    [InlineData(1, 2, 0, 3)] // nested inside
    public void A_hold_box_the_deck_boxes_of_its_bay_do_not_block_may_be_stowed(int deckFrom, int deckTo, int holdFrom, int holdTo) {
        // Arrange: here the deck box is stowed first, and the hold box is the newcomer
        Voyage      voyage = Voyage.Draw();
        StowagePlan plan   = PlanWithBoxIn(voyage, new SlotPosition(1, 0, 82), deckFrom, deckTo);
        Container   hold   = voyage.Box().Sailing(voyage.Rotation.Ports[holdFrom], voyage.Rotation.Ports[holdTo]).Generate();

        // Assert
        PlanAssert.Accepts(plan, hold, new SlotPosition(1, 3, 2));
    }

    [Theory]
    [InlineData(0, 3, 0, 2)] // the deck box leaves after the hold box
    [InlineData(0, 2, 1, 2)] // the deck box is loaded before the hold box
    [InlineData(0, 3, 1, 2)] // both
    public void A_hold_box_that_the_deck_boxes_of_its_bay_would_block_is_refused(int deckFrom, int deckTo, int holdFrom, int holdTo) {
        // Arrange
        Voyage      voyage = Voyage.Draw();
        StowagePlan plan   = PlanWithBoxIn(voyage, new SlotPosition(1, 0, 82), deckFrom, deckTo);
        Container   hold   = voyage.Box().Sailing(voyage.Rotation.Ports[holdFrom], voyage.Rotation.Ports[holdTo]).Generate();

        // Assert
        PlanAssert.Refuses(plan, hold, new SlotPosition(1, 3, 2), StowageViolation.HatchAccessBlocked);
    }

    [Fact]
    public void The_hatch_of_another_bay_is_not_concerned() {
        // Arrange: a hold box that leaves at the second call, a deck box in the next bay that stays until the fourth
        Voyage      voyage = Voyage.Draw();
        StowagePlan plan   = PlanWithBoxIn(voyage, new SlotPosition(1, 0, 2), 0, 1);
        Container   deck   = voyage.Box().Sailing(voyage.FirstCall, voyage.FourthCall).Generate();

        // Assert
        PlanAssert.Accepts(plan, deck, new SlotPosition(3, 0, 82));
    }

    [Fact]
    public void Deck_boxes_do_not_conflict_with_each_other_whatever_their_route() {
        // Arrange
        Voyage      voyage = Voyage.Draw();
        StowagePlan plan   = PlanWithBoxIn(voyage, new SlotPosition(1, 0, 82), 0, 1);
        Container   deck   = voyage.Box().Sailing(voyage.SecondCall, voyage.FourthCall).Generate();

        // Assert
        PlanAssert.Accepts(plan, deck, new SlotPosition(1, 3, 82));
    }

    [Fact]
    public void Hold_boxes_do_not_conflict_with_each_other_across_rows_whatever_their_route() {
        // Arrange
        Voyage      voyage = Voyage.Draw();
        StowagePlan plan   = PlanWithBoxIn(voyage, new SlotPosition(1, 0, 2), 0, 1);
        Container   hold   = voyage.Box().Sailing(voyage.SecondCall, voyage.FourthCall).Generate();

        // Assert
        PlanAssert.Accepts(plan, hold, new SlotPosition(1, 3, 2));
    }

    [Fact]
    public void Overstow_is_reported_before_hatch_access() {
        // Arrange: an upper hold box that blocks the one below it, with a deck box that blocks it too
        Voyage      voyage = Voyage.Draw();
        StowagePlan plan   = voyage.EmptyPlan();
        plan.Assign(voyage.Box().Sailing(voyage.FirstCall, voyage.SecondCall).Weighing(20m).Generate(), new SlotPosition(1, 0, 2));
        plan.Assign(voyage.Box().Sailing(voyage.FirstCall, voyage.SecondCall).Generate(), new SlotPosition(1, 3, 82));
        Container container = voyage.Box().Sailing(voyage.FirstCall, voyage.ThirdCall).Weighing(10m).Generate();

        // Assert
        PlanAssert.Refuses(plan, container, new SlotPosition(1, 0, 4), StowageViolation.Overstow);
    }

}
