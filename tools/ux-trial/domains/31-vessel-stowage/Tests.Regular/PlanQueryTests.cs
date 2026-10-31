using JustDummies;

using VesselStowage.Domain;

using Xunit;

namespace VesselStowage.Tests.Regular;

public sealed class PlanQueryTests {

    [Fact]
    public void Checking_a_slot_does_not_place_the_box() {
        // Arrange
        Rotation    anyRotation = Stowage.AnyThreePortRotation();
        StowagePlan plan        = new(Stowage.AnyRoomyVessel().Generate(), anyRotation);
        Container   anyBox      = Stowage.AnyGeneralCargo(anyRotation.Ports[0], anyRotation.Ports[1]).Generate();

        // Act
        plan.Check(anyBox, new SlotPosition(Stowage.BayFor(anyBox.Size), 0, 2));

        // Assert
        Assert.Empty(plan.Assignments);
    }

    [Fact]
    public void A_slot_becomes_usable_once_the_slot_below_it_is_filled() {
        // Arrange
        Rotation      anyRotation  = Stowage.AnyThreePortRotation();
        VesselProfile oneStackShip = Stowage.AnyRoomyVessel().WithBayCount(1).WithRowCount(1).WithHoldTiers(2).WithDeckTiers(0).Generate();
        StowagePlan   plan         = new(oneStackShip, anyRotation);
        AnyContainer  boxes        = Stowage.AnyGeneralCargo(anyRotation.Ports[0], anyRotation.Ports[2])
                                            .WithSize(ContainerSize.TwentyFoot)
                                            .WithGrossMassTonnes(15m);
        plan.Assign(boxes.Generate(), new SlotPosition(1, 0, 2));
        Container anyNextBox = boxes.Generate();

        // Act
        IReadOnlyList<SlotPosition> usable = plan.UsableSlotsFor(anyNextBox);

        // Assert
        Assert.Equal([new SlotPosition(1, 0, 4)], usable);
    }

    [Fact]
    public void A_box_is_discharged_deck_first_and_top_tier_first() {
        // Arrange
        Rotation      anyRotation = Stowage.AnyThreePortRotation();
        StowagePlan   plan        = new(Stowage.AnyRoomyVessel().Generate(), anyRotation);
        ContainerSize anySize     = Any.Enum<ContainerSize>().Generate();
        int           bay         = Stowage.BayFor(anySize);
        AnyContainer  forSecondPort = Stowage.AnyGeneralCargo(anyRotation.Ports[0], anyRotation.Ports[1]).WithSize(anySize);
        plan.Assign(forSecondPort.WithGrossMassTonnes(20m).Generate(), new SlotPosition(bay, 0, 2));
        plan.Assign(forSecondPort.WithGrossMassTonnes(20m).Generate(), new SlotPosition(bay, 0, 82));
        plan.Assign(forSecondPort.WithGrossMassTonnes(10m).Generate(), new SlotPosition(bay, 0, 84));
        plan.Assign(Stowage.AnyGeneralCargo(anyRotation.Ports[0], anyRotation.Ports[2]).WithSize(anySize).Generate(),
                    new SlotPosition(bay + 2, 0, 2));

        // Act
        IReadOnlyList<StowageAssignment> sequence = plan.DischargeSequence(anyRotation.Ports[1]);

        // Assert
        Assert.Equal([new SlotPosition(bay, 0, 84), new SlotPosition(bay, 0, 82), new SlotPosition(bay, 0, 2)],
                     sequence.Select(a => a.Slot));
    }

    [Fact]
    public void The_displacement_is_the_lightship_plus_every_planned_gross_mass() {
        // Arrange
        Rotation      anyRotation = Stowage.AnyThreePortRotation();
        StowagePlan   plan        = new(Stowage.AnyRoomyVessel().WithLightshipTonnes(20_000m).Generate(), anyRotation);
        ContainerSize anySize     = Any.Enum<ContainerSize>().Generate();
        int           bay         = Stowage.BayFor(anySize);
        AnyContainer  boxes       = Stowage.AnyGeneralCargo(anyRotation.Ports[0], anyRotation.Ports[2]).WithSize(anySize);
        plan.Assign(boxes.WithGrossMassTonnes(12.5m).Generate(), new SlotPosition(bay, 0, 2));
        plan.Assign(boxes.WithGrossMassTonnes(7.5m).Generate(), new SlotPosition(bay, 2, 2));

        // Act
        decimal displacement = plan.DisplacementTonnes;

        // Assert
        Assert.Equal(20_020m, displacement);
    }

    [Fact]
    public void A_box_with_another_stacked_on_it_cannot_be_removed() {
        // Arrange
        Rotation      anyRotation = Stowage.AnyThreePortRotation();
        StowagePlan   plan        = new(Stowage.AnyRoomyVessel().Generate(), anyRotation);
        ContainerSize anySize     = Any.Enum<ContainerSize>().Generate();
        int           bay         = Stowage.BayFor(anySize);
        AnyContainer  boxes       = Stowage.AnyGeneralCargo(anyRotation.Ports[0], anyRotation.Ports[2]).WithSize(anySize).WithGrossMassTonnes(15m);
        Container     anyBottom   = boxes.Generate();
        plan.Assign(anyBottom, new SlotPosition(bay, 0, 2));
        plan.Assign(boxes.Generate(), new SlotPosition(bay, 0, 4));

        // Act
        StowageRefusedException refusal = Assert.Throws<StowageRefusedException>(() => plan.Remove(anyBottom.Id));

        // Assert
        Assert.Equal(StowageViolation.ContainerStackedAbove, refusal.Violation);
    }

    [Fact]
    public void Removing_a_box_frees_its_slot() {
        // Arrange
        Rotation     anyRotation = Stowage.AnyThreePortRotation();
        StowagePlan  plan        = new(Stowage.AnyRoomyVessel().Generate(), anyRotation);
        Container    anyBox      = Stowage.AnyGeneralCargo(anyRotation.Ports[0], anyRotation.Ports[1]).Generate();
        SlotPosition slot        = new(Stowage.BayFor(anyBox.Size), 0, 2);
        plan.Assign(anyBox, slot);

        // Act
        plan.Remove(anyBox.Id);

        // Assert
        Assert.Null(plan.At(slot));
        Assert.Null(plan.Find(anyBox.Id));
    }

    [Fact]
    public void Removing_a_box_that_is_not_in_the_plan_is_refused() {
        // Arrange
        StowagePlan plan  = new(Stowage.AnyRoomyVessel().Generate(), Stowage.AnyThreePortRotation());
        ContainerId anyId = Arbitrary.ContainerId().Generate();

        // Act
        StowageRefusedException refusal = Assert.Throws<StowageRefusedException>(() => plan.Remove(anyId));

        // Assert
        Assert.Equal(StowageViolation.NotStowed, refusal.Violation);
    }

}
