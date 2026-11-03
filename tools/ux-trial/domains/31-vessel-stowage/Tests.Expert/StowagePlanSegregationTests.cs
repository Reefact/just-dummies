using JustDummies;
using VesselStowage.Domain.Tests.Generators;
using Xunit;

namespace VesselStowage.Domain.Tests;

public sealed class StowagePlanSegregationTests {

    private static StowagePlan PlanWith(Voyage voyage, HazardClass hazard, SlotPosition slot, decimal grossMassTonnes = 20m) {
        StowagePlan plan = voyage.EmptyPlan();
        plan.Assign(voyage.Box().Carrying(hazard).Weighing(grossMassTonnes).Generate(), slot);

        return plan;
    }

    [Fact]
    public void Flammable_goods_are_refused_beside_oxidizers() {
        // Arrange
        Voyage      voyage    = Voyage.Draw();
        StowagePlan plan      = PlanWith(voyage, HazardClass.FlammableLiquids, new SlotPosition(1, 0, 2));
        Container   container = voyage.Box().Carrying(HazardClass.Oxidizers).Generate();

        // Assert
        PlanAssert.Refuses(plan, container, new SlotPosition(1, 1, 2), StowageViolation.SegregationConflict);
    }

    [Fact]
    public void Oxidizers_are_refused_beside_flammable_goods_whichever_is_stowed_first() {
        // Arrange
        Voyage      voyage    = Voyage.Draw();
        StowagePlan plan      = PlanWith(voyage, HazardClass.Oxidizers, new SlotPosition(1, 0, 2));
        Container   container = voyage.Box().Carrying(HazardClass.FlammableSolids).Generate();

        // Assert
        PlanAssert.Refuses(plan, container, new SlotPosition(1, 2, 2), StowageViolation.SegregationConflict);
    }

    [Fact]
    public void Flammable_goods_are_refused_above_oxidizers() {
        // Arrange
        Voyage      voyage    = Voyage.Draw();
        StowagePlan plan      = PlanWith(voyage, HazardClass.Oxidizers, new SlotPosition(1, 0, 2), grossMassTonnes: 20m);
        Container   container = voyage.Box().Carrying(HazardClass.FlammableLiquids).Weighing(10m).Generate();

        // Assert
        PlanAssert.Refuses(plan, container, new SlotPosition(1, 0, 4), StowageViolation.SegregationConflict);
    }

    [Fact]
    public void Explosives_are_refused_beside_any_other_dangerous_goods_on_deck() {
        // Arrange
        Voyage      voyage    = Voyage.Draw();
        StowagePlan plan      = PlanWith(voyage, HazardClass.Toxic, new SlotPosition(1, 0, 82));
        Container   container = voyage.Box().Carrying(HazardClass.Explosives).Generate();

        // Assert
        PlanAssert.Refuses(plan, container, new SlotPosition(1, 1, 82), StowageViolation.SegregationConflict);
    }

    [Fact]
    public void Radioactive_goods_are_refused_beside_any_other_dangerous_goods() {
        // Arrange
        Voyage      voyage    = Voyage.Draw();
        StowagePlan plan      = PlanWith(voyage, HazardClass.Corrosives, new SlotPosition(1, 0, 2));
        Container   container = voyage.Box().Carrying(HazardClass.Radioactive).Generate();

        // Assert
        PlanAssert.Refuses(plan, container, new SlotPosition(1, 1, 2), StowageViolation.SegregationConflict);
    }

    [Fact]
    public void Explosives_and_radioactive_goods_never_touch_one_another_either() {
        // Arrange
        Voyage      voyage    = Voyage.Draw();
        StowagePlan plan      = PlanWith(voyage, HazardClass.Explosives, new SlotPosition(1, 0, 82));
        Container   container = voyage.Box().Carrying(HazardClass.Explosives).Generate();

        // Assert
        PlanAssert.Refuses(plan, container, new SlotPosition(1, 1, 82), StowageViolation.SegregationConflict);
    }

    [Fact]
    public void Compatible_dangerous_goods_may_stand_side_by_side() {
        // Arrange
        Voyage      voyage    = Voyage.Draw();
        StowagePlan plan      = PlanWith(voyage, HazardClass.FlammableLiquids, new SlotPosition(1, 0, 2));
        Container   container = voyage.Box().Carrying(HazardClass.Toxic).Generate();

        // Assert
        PlanAssert.Accepts(plan, container, new SlotPosition(1, 1, 2));
    }

    [Theory]
    [InlineData(HazardClass.Explosives)]
    [InlineData(HazardClass.Radioactive)]
    [InlineData(HazardClass.Oxidizers)]
    public void General_cargo_stands_next_to_anything(HazardClass neighbour) {
        // Arrange: explosives go on deck, the others anywhere; deck is fine for all three
        Voyage      voyage    = Voyage.Draw();
        StowagePlan plan      = PlanWith(voyage, neighbour, new SlotPosition(1, 0, 82));
        Container   container = voyage.Box().GeneralCargo().Generate();

        // Assert
        PlanAssert.Accepts(plan, container, new SlotPosition(1, 1, 82));
    }

    [Fact]
    public void Dangerous_goods_two_rows_apart_are_not_neighbours() {
        // Arrange: row 1 is starboard of the centreline, row 2 port of it: row 0 lies between
        Voyage      voyage    = Voyage.Draw();
        StowagePlan plan      = PlanWith(voyage, HazardClass.FlammableLiquids, new SlotPosition(1, 1, 2));
        Container   container = voyage.Box().Carrying(HazardClass.Oxidizers).Generate();

        // Assert
        PlanAssert.Accepts(plan, container, new SlotPosition(1, 2, 2));
    }

    [Fact]
    public void Dangerous_goods_in_the_same_row_two_tiers_apart_are_not_neighbours() {
        // Arrange
        Voyage      voyage = Voyage.Draw();
        StowagePlan plan   = voyage.EmptyPlan();
        plan.Assign(voyage.Box().Carrying(HazardClass.FlammableLiquids).Weighing(20m).Generate(), new SlotPosition(1, 0, 2));
        plan.Assign(voyage.Box().GeneralCargo().Weighing(15m).Generate(), new SlotPosition(1, 0, 4));
        Container container = voyage.Box().Carrying(HazardClass.Oxidizers).Weighing(10m).Generate();

        // Assert
        PlanAssert.Accepts(plan, container, new SlotPosition(1, 0, 6));
    }

    [Fact]
    public void Dangerous_goods_in_different_bays_are_not_neighbours() {
        // Arrange
        Voyage      voyage    = Voyage.Draw();
        StowagePlan plan      = PlanWith(voyage, HazardClass.FlammableLiquids, new SlotPosition(1, 0, 2));
        Container   container = voyage.Box().Carrying(HazardClass.Oxidizers).Generate();

        // Assert
        PlanAssert.Accepts(plan, container, new SlotPosition(3, 0, 2));
    }

    [Fact]
    public void Explosives_on_deck_are_not_neighbours_of_dangerous_goods_in_the_hold_below() {
        // Arrange: hold and deck are separate sections, whatever lies between
        Voyage      voyage    = Voyage.Draw();
        StowagePlan plan      = PlanWith(voyage, HazardClass.Toxic, new SlotPosition(1, 0, 2));
        Container   container = voyage.Box().Carrying(HazardClass.Explosives).Generate();

        // Assert
        PlanAssert.Accepts(plan, container, new SlotPosition(1, 0, 82));
    }

}
