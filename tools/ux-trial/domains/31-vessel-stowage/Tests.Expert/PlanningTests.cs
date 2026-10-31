using JustDummies;

using VesselStowage.Domain;

using Xunit;

namespace VesselStowage.Tests.Expert;

/// <summary>What a planner does with a plan beyond a single assignment: options, corrections, discharge.</summary>
public sealed class PlanningTests {

    [Fact]
    public void A_free_slot_becomes_usable_once_a_box_supports_it() {
        // Arrange
        // One twenty-foot bay, one row, two hold tiers and no deck: the whole ship is two slots.
        VesselProfile anyOneStackVessel = Any.VesselProfile().Roomy()
                                             .WithBayCount(1).WithRowCount(1).WithHoldTiers(2).WithDeckTiers(0)
                                             .Generate();
        Rotation     anyRotation = Any.Rotation().Generate();
        StowagePlan  plan        = new(anyOneStackVessel, anyRotation);
        AnyContainer anyBox      = Any.Container().SegregationNeutral().WithSize(ContainerSize.TwentyFoot)
                                      .Travelling(anyRotation.Ports[0], anyRotation.Ports[1]);
        Container    anyLaden    = anyBox.Laden().Generate();
        Container    anyLighter  = anyBox.StackableOn(anyLaden).Generate();
        SlotPosition floor       = new(1, 0, 2);
        SlotPosition secondTier  = new(1, 0, 4);
        IReadOnlyList<SlotPosition> beforeLoading = plan.UsableSlotsFor(anyLighter);
        plan.Assign(anyLaden, floor);

        // Act
        IReadOnlyList<SlotPosition> afterLoading = plan.UsableSlotsFor(anyLighter);

        // Assert
        Assert.Equal([floor], beforeLoading);
        Assert.Equal([secondTier], afterLoading);
    }

    [Fact]
    public void Checking_a_slot_leaves_the_plan_untouched() {
        // Arrange
        Rotation      anyRotation = Any.Rotation().Generate();
        StowagePlan   plan        = new(Any.VesselProfile().Roomy().Generate(), anyRotation);
        SlotPosition  anyFloor    = Any.SlotPosition().InRoomyVessel(ContainerSize.FortyFoot).AtFootOfHold().Generate();
        Container     anyBox      = Any.Container().SegregationNeutral().FitFor(anyFloor).Travelling(anyRotation.Ports[0], anyRotation.Ports[1]).Generate();

        // Act
        StowageViolation violation = plan.Check(anyBox, anyFloor);

        // Assert
        Assert.Equal(StowageViolation.None, violation);
        Assert.Empty(plan.Assignments);
        Assert.Null(plan.Find(anyBox.Id));
    }

    [Fact]
    public void A_box_comes_off_the_plan_only_once_nothing_stands_on_it() {
        // Arrange
        Rotation     anyRotation = Any.Rotation().Generate();
        StowagePlan  plan        = new(Any.VesselProfile().Roomy().Generate(), anyRotation);
        SlotPosition anyFloor    = Any.SlotPosition().InRoomyVessel(ContainerSize.TwentyFoot).AtFootOfHold().Generate();
        AnyContainer anyBox      = Any.Container().SegregationNeutral().Travelling(anyRotation.Ports[0], anyRotation.Ports[1]);
        Container    anyLower    = anyBox.FitFor(anyFloor).Laden().Generate();
        Container    anyUpper    = anyBox.StackableOn(anyLower).Generate();
        plan.Assign(anyLower, anyFloor);
        plan.Assign(anyUpper, anyFloor.Above());

        // Act
        StowageRefusedException refusal = Assert.Throws<StowageRefusedException>(() => plan.Remove(anyLower.Id));
        plan.Remove(anyUpper.Id);
        plan.Remove(anyLower.Id);

        // Assert
        Assert.Equal(StowageViolation.ContainerStackedAbove, refusal.Violation);
        Assert.Empty(plan.Assignments);
    }

    [Fact]
    public void Removing_a_box_that_is_not_planned_is_refused() {
        // Arrange
        StowagePlan plan  = new(Any.VesselProfile().Generate(), Any.Rotation().Generate());
        ContainerId anyId = Any.ContainerId().Generate();

        // Act
        StowageRefusedException refusal = Assert.Throws<StowageRefusedException>(() => plan.Remove(anyId));

        // Assert
        Assert.Equal(StowageViolation.NotStowed, refusal.Violation);
    }

    [Fact]
    public void The_crane_lifts_a_port_bay_by_bay_deck_before_hold_and_top_tier_first() {
        // Arrange
        Rotation    anyRotation = Any.Rotation().WithPorts(p => p.WithMinCount(3)).Generate();
        PortCode    second      = anyRotation.Ports[1];
        PortCode    third       = anyRotation.Ports[2];
        StowagePlan plan        = new(Any.VesselProfile().Roomy().Generate(), anyRotation);
        // Five boxes, five distinct numbers: the plan tells boxes apart by number.
        ContainerId[] anyIds = [.. Any.SetOf(Any.ContainerId()).WithCount(5).Generate()];
        // One weight for all: equal boxes may stand on each other, so weight plays no part in the order.
        AnyContainer  anyBox = Any.Container().SegregationNeutral().WithSize(ContainerSize.FortyFoot).WithGrossMassTonnes(10m);
        Container bay2HoldFloor = anyBox.WithId(anyIds[0]).Travelling(anyRotation.Ports[0], second).Generate();
        Container bay2HoldTier4 = anyBox.WithId(anyIds[1]).Travelling(anyRotation.Ports[0], second).Generate();
        Container bay2Deck      = anyBox.WithId(anyIds[2]).Travelling(anyRotation.Ports[0], second).Generate();
        Container bay6HoldFloor = anyBox.WithId(anyIds[3]).Travelling(anyRotation.Ports[0], second).Generate();
        Container bay4ForThird  = anyBox.WithId(anyIds[4]).Travelling(anyRotation.Ports[0], third).Generate();
        plan.Assign(bay6HoldFloor, new SlotPosition(6, 0, 2));
        plan.Assign(bay2HoldFloor, new SlotPosition(2, 0, 2));
        plan.Assign(bay2HoldTier4, new SlotPosition(2, 0, 4));
        plan.Assign(bay2Deck,      new SlotPosition(2, 0, 82));
        plan.Assign(bay4ForThird,  new SlotPosition(4, 0, 2));

        // Act
        IReadOnlyList<StowageAssignment> sequence = plan.DischargeSequence(second);

        // Assert
        Assert.Equal([bay2Deck, bay2HoldTier4, bay2HoldFloor, bay6HoldFloor], sequence.Select(a => a.Container));
    }

}
