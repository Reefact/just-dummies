using JustDummies;
using VesselStowage.Domain.Tests.Generators;
using Xunit;

namespace VesselStowage.Domain.Tests;

public sealed class StowagePlanUsableSlotsTests {

    // A vessel of one bay, two rows (0 and 1), two hold tiers and no deck: four slots in all.
    private static AnyVesselProfile TinyVessel() {
        return Any.VesselProfile().Roomy().WithBayCount(1).WithRowCount(2).WithHoldTiers(2).WithDeckTiers(0);
    }

    private static string[] Written(IEnumerable<SlotPosition> slots) {
        return slots.Select(slot => slot.ToString()).ToArray();
    }

    [Fact]
    public void In_an_empty_plan_only_the_lowest_tier_is_usable() {
        // Arrange
        Voyage      voyage    = Voyage.Draw();
        StowagePlan plan      = voyage.EmptyPlanOf(TinyVessel());
        Container   container = voyage.Box().Generate();

        // Act
        IReadOnlyList<SlotPosition> usable = plan.UsableSlotsFor(container);

        // Assert
        Assert.Equal(new[] { "010002", "010102" }, Written(usable));
    }

    [Fact]
    public void A_box_that_does_not_fit_the_bay_has_no_usable_slot() {
        // Arrange: the only bay is odd, a forty-foot box needs an even one
        Voyage      voyage    = Voyage.Draw();
        StowagePlan plan      = voyage.EmptyPlanOf(TinyVessel());
        Container   container = voyage.Box().OfFortyFeet().Generate();

        // Assert
        Assert.Empty(plan.UsableSlotsFor(container));
    }

    [Fact]
    public void A_stowed_slot_is_not_usable_but_the_tier_above_it_is() {
        // Arrange
        Voyage      voyage = Voyage.Draw();
        StowagePlan plan   = voyage.EmptyPlanOf(TinyVessel());
        plan.Assign(voyage.Box().Weighing(20m).Generate(), new SlotPosition(1, 0, 2));
        Container lighter = voyage.Box().Weighing(10m).Generate();

        // Act
        IReadOnlyList<SlotPosition> usable = plan.UsableSlotsFor(lighter);

        // Assert
        Assert.Equal(new[] { "010004", "010102" }, Written(usable));
    }

    [Fact]
    public void A_free_slot_can_be_unusable_through_weight() {
        // Arrange: the 25 t box is heavier than the 20 t box it would stand on
        Voyage      voyage = Voyage.Draw();
        StowagePlan plan   = voyage.EmptyPlanOf(TinyVessel());
        plan.Assign(voyage.Box().Weighing(20m).Generate(), new SlotPosition(1, 0, 2));
        Container heavier = voyage.Box().Weighing(25m).Generate();

        // Act
        IReadOnlyList<SlotPosition> usable = plan.UsableSlotsFor(heavier);

        // Assert: the tier above (010004) is free but refused
        Assert.Equal(new[] { "010102" }, Written(usable));
    }

    [Fact]
    public void A_free_slot_can_be_unusable_through_its_neighbours() {
        // Arrange: oxidizers cannot stand beside flammable liquids
        Voyage      voyage = Voyage.Draw();
        StowagePlan plan   = voyage.EmptyPlanOf(TinyVessel());
        plan.Assign(voyage.Box().Carrying(HazardClass.FlammableLiquids).Weighing(20m).Generate(), new SlotPosition(1, 0, 2));
        Container oxidizers = voyage.Box().Carrying(HazardClass.Oxidizers).Weighing(10m).Generate();

        // Assert: neither the next row nor the tier above is usable
        Assert.Empty(plan.UsableSlotsFor(oxidizers));
    }

    [Fact]
    public void A_box_already_in_the_plan_has_no_usable_slot() {
        // Arrange
        Voyage      voyage    = Voyage.Draw();
        StowagePlan plan      = voyage.EmptyPlanOf(TinyVessel());
        Container   container = voyage.Box().Generate();
        plan.Assign(container, new SlotPosition(1, 0, 2));

        // Assert
        Assert.Empty(plan.UsableSlotsFor(container));
    }

    [Fact]
    public void A_missing_box_has_no_usable_slots_but_an_error() {
        Assert.Throws<ArgumentNullException>(() => Voyage.Draw().EmptyPlan().UsableSlotsFor(null!));
    }

    [Fact]
    public void A_slot_is_usable_exactly_when_Check_allows_it() {
        // Arrange: a vessel with every kind of obstacle somewhere: a stack, a deck box, a dangerous neighbour
        Voyage      voyage = Voyage.Draw();
        StowagePlan plan   = voyage.EmptyPlanOf(Any.VesselProfile().Roomy().WithBayCount(2).WithRowCount(3).WithHoldTiers(2).WithDeckTiers(2).WithMaxStackWeightTonnes(35m));
        plan.Assign(voyage.Box().Weighing(20m).Generate(), new SlotPosition(1, 0, 2));
        plan.Assign(voyage.Box().Weighing(10m).Generate(), new SlotPosition(1, 0, 4));
        plan.Assign(voyage.Box().Carrying(HazardClass.FlammableLiquids).Weighing(12m).Generate(), new SlotPosition(1, 1, 2));
        plan.Assign(voyage.Box().Weighing(8m).Generate(), new SlotPosition(1, 2, 82));
        Container candidate = voyage.Box().Carrying(HazardClass.Oxidizers).Weighing(11m).Generate();

        // Act
        IReadOnlyList<SlotPosition> usable = plan.UsableSlotsFor(candidate);

        // Assert
        foreach (SlotPosition slot in plan.Vessel.AllSlots()) {
            Assert.Equal(plan.Check(candidate, slot) == StowageViolation.None, usable.Contains(slot));
        }
        Assert.NotEmpty(usable);
        Assert.True(usable.Count < plan.Vessel.AllSlots().Count());
    }

}
