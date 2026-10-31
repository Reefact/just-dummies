using JustDummies;

using VesselStowage.Domain;

using Xunit;

namespace VesselStowage.Tests.Expert;

/// <summary>
///     Assignments on a roomy vessel over a rotation of at least three calls. Each test names the
///     calls it needs (first, second, third) because the handling sequence is what stowage is about;
///     which ports they are, and which ship, is arbitrary.
/// </summary>
public sealed class StowagePlanTests {

    private static readonly AnyRotation AnyRotationOfThreeCallsOrMore = Any.Rotation().WithPorts(p => p.WithMinCount(3));

    private static readonly IAny<StowagePlan> AnyEmptyPlanOnARoomyVessel =
        Any.Combine(Any.VesselProfile().Roomy(), AnyRotationOfThreeCallsOrMore, (vessel, rotation) => new StowagePlan(vessel, rotation));

    private static readonly AnyContainer AnyNeutralCargo = Any.Container().SegregationNeutral();

    private static PortCode Call(StowagePlan plan, int index) => plan.Rotation.Ports[index];

    [Fact]
    public void A_box_on_the_floor_of_the_hold_is_stowed_and_found_again() {
        // Arrange
        StowagePlan  anyPlan = AnyEmptyPlanOnARoomyVessel.Generate();
        SlotPosition anySlot = Any.SlotPosition().InRoomyVessel(ContainerSize.FortyFoot).AtFootOfHold().Generate();
        Container    anyBox  = AnyNeutralCargo.FitFor(anySlot).Travelling(Call(anyPlan, 0), Call(anyPlan, 1)).Generate();

        // Act
        anyPlan.Assign(anyBox, anySlot);

        // Assert
        Assert.Same(anyBox, anyPlan.At(anySlot)?.Container);
        Assert.Equal(anySlot, anyPlan.Find(anyBox.Id)?.Slot);
    }

    [Fact]
    public void A_slot_beyond_the_vessels_bays_is_refused() {
        // Arrange
        VesselProfile anyFiveBayVessel = Any.VesselProfile().Roomy().WithBayCount(5).Generate();
        Rotation      anyRotation      = AnyRotationOfThreeCallsOrMore.Generate();
        StowagePlan   plan             = new(anyFiveBayVessel, anyRotation);
        SlotPosition  anySlotInBay7    = Any.SlotPosition().WithBay(7).AtFootOfHold().WithRow(0).Generate();
        Container     anyBox           = AnyNeutralCargo.FitFor(anySlotInBay7).Travelling(anyRotation.Ports[0], anyRotation.Ports[1]).Generate();

        // Act
        StowageViolation violation = plan.Check(anyBox, anySlotInBay7);

        // Assert
        Assert.Equal(StowageViolation.SlotOutsideVessel, violation);
    }

    [Fact]
    public void A_forty_foot_box_does_not_go_into_a_twenty_foot_bay() {
        // Arrange
        StowagePlan  anyPlan            = AnyEmptyPlanOnARoomyVessel.Generate();
        SlotPosition anyTwentyFootSlot  = Any.SlotPosition().InRoomyVessel(ContainerSize.TwentyFoot).AtFootOfHold().Generate();
        Container    anyFortyFootBox    = AnyNeutralCargo.WithSize(ContainerSize.FortyFoot).Travelling(Call(anyPlan, 0), Call(anyPlan, 1)).Generate();

        // Act
        StowageRefusedException refusal = Assert.Throws<StowageRefusedException>(() => anyPlan.Assign(anyFortyFootBox, anyTwentyFootSlot));

        // Assert
        Assert.Equal(StowageViolation.BayDoesNotFitSize, refusal.Violation);
    }

    [Fact]
    public void A_box_cannot_float_above_an_empty_tier() {
        // Arrange
        StowagePlan  anyPlan       = AnyEmptyPlanOnARoomyVessel.Generate();
        SlotPosition anySecondTier = Any.SlotPosition().InRoomyVessel(ContainerSize.TwentyFoot).AtFootOfHold().Generate().Above();
        Container    anyBox        = AnyNeutralCargo.FitFor(anySecondTier).Travelling(Call(anyPlan, 0), Call(anyPlan, 1)).Generate();

        // Act
        StowageViolation violation = anyPlan.Check(anyBox, anySecondTier);

        // Assert
        Assert.Equal(StowageViolation.NoSupportBelow, violation);
    }

    [Fact]
    public void The_foot_of_the_deck_needs_no_box_below_it() {
        // Arrange
        StowagePlan  anyPlan       = AnyEmptyPlanOnARoomyVessel.Generate();
        SlotPosition anyFootOfDeck = Any.SlotPosition().InRoomyVessel(ContainerSize.FortyFoot).AtFootOfDeck().Generate();
        Container    anyBox        = AnyNeutralCargo.FitFor(anyFootOfDeck).Travelling(Call(anyPlan, 0), Call(anyPlan, 1)).Generate();

        // Act
        StowageViolation violation = anyPlan.Check(anyBox, anyFootOfDeck);

        // Assert
        Assert.Equal(StowageViolation.None, violation);
    }

    [Fact]
    public void A_box_heavier_than_the_one_below_is_refused() {
        // Arrange
        StowagePlan  anyPlan  = AnyEmptyPlanOnARoomyVessel.Generate();
        SlotPosition anyFloor = Any.SlotPosition().InRoomyVessel(ContainerSize.FortyFoot).AtFootOfHold().Generate();
        AnyContainer anyBoxForTheSameLeg = AnyNeutralCargo.FitFor(anyFloor).Travelling(Call(anyPlan, 0), Call(anyPlan, 1));
        anyPlan.Assign(anyBoxForTheSameLeg.WithGrossMassTonnes(12m).Generate(), anyFloor);
        Container anyHeavierBox = anyBoxForTheSameLeg.WithGrossMassTonnes(18m).Generate();

        // Act
        StowageViolation violation = anyPlan.Check(anyHeavierBox, anyFloor.Above());

        // Assert
        Assert.Equal(StowageViolation.HeavierThanContainerBelow, violation);
    }

    [Fact]
    public void A_stack_cannot_outweigh_the_tank_top_limit() {
        // Arrange
        VesselProfile anyVesselRatedForFortyTonnesAStack = Any.VesselProfile().Roomy().WithMaxStackWeightTonnes(40m).Generate();
        Rotation      anyRotation = AnyRotationOfThreeCallsOrMore.Generate();
        StowagePlan   plan        = new(anyVesselRatedForFortyTonnesAStack, anyRotation);
        SlotPosition  anyFloor    = Any.SlotPosition().InRoomyVessel(ContainerSize.FortyFoot).AtFootOfHold().Generate();
        AnyContainer  anyBox      = AnyNeutralCargo.FitFor(anyFloor).Travelling(anyRotation.Ports[0], anyRotation.Ports[1]);
        plan.Assign(anyBox.WithGrossMassTonnes(25m).Generate(), anyFloor);

        // Act
        StowageViolation violation = plan.Check(anyBox.WithGrossMassTonnes(20m).Generate(), anyFloor.Above());

        // Assert
        Assert.Equal(StowageViolation.StackWeightExceeded, violation);
    }

    [Fact]
    public void A_box_for_a_later_port_cannot_overstow_one_discharged_earlier() {
        // Arrange
        StowagePlan  anyPlan       = AnyEmptyPlanOnARoomyVessel.Generate();
        SlotPosition anyFloor      = Any.SlotPosition().InRoomyVessel(ContainerSize.TwentyFoot).AtFootOfHold().Generate();
        Container    anyForSecond  = AnyNeutralCargo.FitFor(anyFloor).Laden().Travelling(Call(anyPlan, 0), Call(anyPlan, 1)).Generate();
        anyPlan.Assign(anyForSecond, anyFloor);
        Container anyForThird = AnyNeutralCargo.StackableOn(anyForSecond).Travelling(Call(anyPlan, 0), Call(anyPlan, 2)).Generate();

        // Act
        StowageRefusedException refusal = Assert.Throws<StowageRefusedException>(() => anyPlan.Assign(anyForThird, anyFloor.Above()));

        // Assert
        Assert.Equal(StowageViolation.Overstow, refusal.Violation);
    }

    [Fact]
    public void A_box_discharged_earlier_may_stand_on_one_discharged_later() {
        // Arrange
        StowagePlan  anyPlan      = AnyEmptyPlanOnARoomyVessel.Generate();
        SlotPosition anyFloor     = Any.SlotPosition().InRoomyVessel(ContainerSize.TwentyFoot).AtFootOfHold().Generate();
        Container    anyForThird  = AnyNeutralCargo.FitFor(anyFloor).Laden().Travelling(Call(anyPlan, 0), Call(anyPlan, 2)).Generate();
        anyPlan.Assign(anyForThird, anyFloor);
        Container anyForSecond = AnyNeutralCargo.StackableOn(anyForThird).Travelling(Call(anyPlan, 0), Call(anyPlan, 1)).Generate();

        // Act
        StowageViolation violation = anyPlan.Check(anyForSecond, anyFloor.Above());

        // Assert
        Assert.Equal(StowageViolation.None, violation);
    }

    [Fact]
    public void A_deck_box_staying_aboard_longer_blocks_the_hatch_over_the_hold() {
        // Arrange
        StowagePlan     anyPlan     = AnyEmptyPlanOnARoomyVessel.Generate();
        AnySlotPosition anyBaySlot  = Any.SlotPosition().InRoomyVessel(ContainerSize.FortyFoot);
        SlotPosition    anyHoldSlot = anyBaySlot.AtFootOfHold().Generate();
        SlotPosition    anyDeckSlot = anyBaySlot.WithBay(anyHoldSlot.Bay).AtFootOfDeck().Generate();   // any row of the same bay
        anyPlan.Assign(AnyNeutralCargo.FitFor(anyHoldSlot).Travelling(Call(anyPlan, 0), Call(anyPlan, 1)).Generate(), anyHoldSlot);
        Container anyForThird = AnyNeutralCargo.FitFor(anyDeckSlot).Travelling(Call(anyPlan, 0), Call(anyPlan, 2)).Generate();

        // Act
        StowageViolation violation = anyPlan.Check(anyForThird, anyDeckSlot);

        // Assert
        Assert.Equal(StowageViolation.HatchAccessBlocked, violation);
    }

    [Fact]
    public void Explosives_are_stowed_on_deck_only() {
        // Arrange
        StowagePlan  anyPlan          = AnyEmptyPlanOnARoomyVessel.Generate();
        SlotPosition anyFloorOfHold   = Any.SlotPosition().InRoomyVessel(ContainerSize.TwentyFoot).AtFootOfHold().Generate();
        SlotPosition sameStackOnDeck  = new(anyFloorOfHold.Bay, anyFloorOfHold.Row, 82);
        Container    anyExplosives    = Any.Container().FitFor(anyFloorOfHold).WithHazard(HazardClass.Explosives).Travelling(Call(anyPlan, 0), Call(anyPlan, 1)).Generate();

        // Act
        StowageViolation underDeck = anyPlan.Check(anyExplosives, anyFloorOfHold);
        StowageViolation onDeck    = anyPlan.Check(anyExplosives, sameStackOnDeck);

        // Assert
        Assert.Equal(StowageViolation.ExplosivesUnderDeck, underDeck);
        Assert.Equal(StowageViolation.None, onDeck);
    }

    [Fact]
    public void An_oxidizer_cannot_be_stowed_beside_flammable_liquids() {
        // Arrange
        StowagePlan  anyPlan       = AnyEmptyPlanOnARoomyVessel.Generate();
        SlotPosition anyCentreline = Any.SlotPosition().InRoomyVessel(ContainerSize.FortyFoot).AtFootOfDeck().WithRow(0).Generate();
        SlotPosition firstStarboard = new(anyCentreline.Bay, 1, anyCentreline.Tier);
        AnyContainer anyBoxOnTheLeg = Any.Container().FitFor(anyCentreline).Travelling(Call(anyPlan, 0), Call(anyPlan, 1));
        anyPlan.Assign(anyBoxOnTheLeg.WithHazard(HazardClass.FlammableLiquids).Generate(), anyCentreline);

        // Act
        StowageViolation violation = anyPlan.Check(anyBoxOnTheLeg.WithHazard(HazardClass.Oxidizers).Generate(), firstStarboard);

        // Assert
        Assert.Equal(StowageViolation.SegregationConflict, violation);
    }

    [Fact]
    public void A_box_must_be_loaded_before_the_port_where_it_is_discharged() {
        // Arrange
        StowagePlan  anyPlan       = AnyEmptyPlanOnARoomyVessel.Generate();
        SlotPosition anyFloor      = Any.SlotPosition().InRoomyVessel(ContainerSize.FortyFoot).AtFootOfHold().Generate();
        Container    anyBackwards  = AnyNeutralCargo.FitFor(anyFloor).Travelling(Call(anyPlan, 2), Call(anyPlan, 0)).Generate();

        // Act
        StowageViolation violation = anyPlan.Check(anyBackwards, anyFloor);

        // Assert
        Assert.Equal(StowageViolation.PortNotInRotation, violation);
    }

    [Fact]
    public void A_box_for_a_port_the_vessel_does_not_call_is_refused() {
        // Arrange
        StowagePlan  anyPlan     = AnyEmptyPlanOnARoomyVessel.Generate();
        string[]     calledPorts = anyPlan.Rotation.Ports.Select(p => p.Value).ToArray();
        PortCode     anyUncalled = Any.PortCode().WithValue(v => v.Except(calledPorts)).Generate();
        SlotPosition anyFloor    = Any.SlotPosition().InRoomyVessel(ContainerSize.FortyFoot).AtFootOfHold().Generate();
        Container    anyBox      = AnyNeutralCargo.FitFor(anyFloor).Travelling(Call(anyPlan, 0), anyUncalled).Generate();

        // Act
        StowageViolation violation = anyPlan.Check(anyBox, anyFloor);

        // Assert
        Assert.Equal(StowageViolation.PortNotInRotation, violation);
    }

    [Fact]
    public void A_slot_holds_one_box_and_a_box_takes_one_slot() {
        // Arrange
        StowagePlan  anyPlan     = AnyEmptyPlanOnARoomyVessel.Generate();
        AnySlotPosition anyFloors = Any.SlotPosition().InRoomyVessel(ContainerSize.FortyFoot).AtFootOfHold();
        SlotPosition anyFloor    = anyFloors.Generate();
        SlotPosition anyOtherFloor = anyFloors.WithRow(r => r.LessThan(RoomyVessel.MinRows).DifferentFrom(anyFloor.Row)).Generate();
        AnyContainer anyBoxOnTheLeg = AnyNeutralCargo.FitFor(anyFloor).Travelling(Call(anyPlan, 0), Call(anyPlan, 1));
        Container    anyStowed   = anyBoxOnTheLeg.Generate();
        anyPlan.Assign(anyStowed, anyFloor);

        // Act
        StowageViolation sameSlot = anyPlan.Check(anyBoxOnTheLeg.Generate(), anyFloor);
        StowageViolation sameBox  = anyPlan.Check(anyStowed, anyOtherFloor);

        // Assert
        Assert.Equal(StowageViolation.SlotOccupied, sameSlot);
        Assert.Equal(StowageViolation.AlreadyStowed, sameBox);
    }

}
