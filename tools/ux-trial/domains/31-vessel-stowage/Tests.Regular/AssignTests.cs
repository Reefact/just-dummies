using JustDummies;

using VesselStowage.Domain;

using Xunit;

namespace VesselStowage.Tests.Regular;

public sealed class AssignTests {

    private static StowageViolation RefusalOf(Action assign) {
        return Assert.Throws<StowageRefusedException>(assign).Violation;
    }

    [Fact]
    public void A_box_assigned_to_a_free_bottom_slot_is_found_at_that_slot() {
        // Arrange
        Rotation      anyRotation = Stowage.AnyThreePortRotation();
        StowagePlan   plan        = new(Stowage.AnyRoomyVessel().Generate(), anyRotation);
        Container     anyBox      = Stowage.AnyGeneralCargo(anyRotation.Ports[0], anyRotation.Ports[2]).Generate();
        SlotPosition  slot        = new(Stowage.BayFor(anyBox.Size), 0, 2);

        // Act
        plan.Assign(anyBox, slot);

        // Assert
        Assert.Same(anyBox, plan.At(slot)?.Container);
        Assert.Equal(slot, plan.Find(anyBox.Id)?.Slot);
    }

    [Fact]
    public void A_slot_beyond_the_vessels_last_bay_is_outside_the_vessel() {
        // Arrange
        Rotation      anyRotation = Stowage.AnyThreePortRotation();
        VesselProfile anyVessel   = Stowage.AnyRoomyVessel().WithBayCount(20).Generate();
        StowagePlan   plan        = new(anyVessel, anyRotation);
        Container     anyBox      = Stowage.AnyGeneralCargo(anyRotation.Ports[0], anyRotation.Ports[1])
                                           .WithSize(ContainerSize.FortyFoot)
                                           .Generate();

        // Act
        StowageViolation violation = RefusalOf(() => plan.Assign(anyBox, new SlotPosition(22, 0, 2)));

        // Assert
        Assert.Equal(StowageViolation.SlotOutsideVessel, violation);
    }

    [Fact]
    public void A_forty_foot_box_does_not_fit_an_odd_bay() {
        // Arrange
        Rotation    anyRotation = Stowage.AnyThreePortRotation();
        StowagePlan plan        = new(Stowage.AnyRoomyVessel().Generate(), anyRotation);
        Container   anyBox      = Stowage.AnyGeneralCargo(anyRotation.Ports[0], anyRotation.Ports[1])
                                         .WithSize(ContainerSize.FortyFoot)
                                         .Generate();

        // Act
        StowageViolation violation = RefusalOf(() => plan.Assign(anyBox, new SlotPosition(3, 0, 2)));

        // Assert
        Assert.Equal(StowageViolation.BayDoesNotFitSize, violation);
    }

    [Fact]
    public void A_box_already_in_the_plan_cannot_be_stowed_a_second_time() {
        // Arrange
        Rotation    anyRotation = Stowage.AnyThreePortRotation();
        StowagePlan plan        = new(Stowage.AnyRoomyVessel().Generate(), anyRotation);
        Container   anyBox      = Stowage.AnyGeneralCargo(anyRotation.Ports[0], anyRotation.Ports[2]).Generate();
        int         bay         = Stowage.BayFor(anyBox.Size);
        plan.Assign(anyBox, new SlotPosition(bay, 0, 2));

        // Act
        StowageViolation violation = RefusalOf(() => plan.Assign(anyBox, new SlotPosition(bay, 1, 2)));

        // Assert
        Assert.Equal(StowageViolation.AlreadyStowed, violation);
    }

    [Fact]
    public void A_second_box_cannot_take_an_occupied_slot() {
        // Arrange
        Rotation      anyRotation = Stowage.AnyThreePortRotation();
        StowagePlan   plan        = new(Stowage.AnyRoomyVessel().Generate(), anyRotation);
        ContainerSize anySize     = Any.Enum<ContainerSize>().Generate();
        AnyContainer  boxes       = Stowage.AnyGeneralCargo(anyRotation.Ports[0], anyRotation.Ports[2]).WithSize(anySize);
        SlotPosition  slot        = new(Stowage.BayFor(anySize), 0, 2);
        plan.Assign(boxes.Generate(), slot);
        Container anyOtherBox = boxes.Generate();

        // Act
        StowageViolation violation = RefusalOf(() => plan.Assign(anyOtherBox, slot));

        // Assert
        Assert.Equal(StowageViolation.SlotOccupied, violation);
    }

    [Fact]
    public void A_box_discharged_at_a_port_called_before_its_loading_port_is_refused() {
        // Arrange
        Rotation    anyRotation = Stowage.AnyThreePortRotation();
        StowagePlan plan        = new(Stowage.AnyRoomyVessel().Generate(), anyRotation);
        Container   anyBox      = Stowage.AnyGeneralCargo(anyRotation.Ports[2], anyRotation.Ports[0]).Generate();

        // Act
        StowageViolation violation = RefusalOf(() => plan.Assign(anyBox, new SlotPosition(Stowage.BayFor(anyBox.Size), 0, 2)));

        // Assert
        Assert.Equal(StowageViolation.PortNotInRotation, violation);
    }

    [Fact]
    public void A_box_bound_for_a_port_outside_the_rotation_is_refused() {
        // Arrange
        Rotation    anyRotation  = Stowage.AnyThreePortRotation();
        StowagePlan plan         = new(Stowage.AnyRoomyVessel().Generate(), anyRotation);
        PortCode    anyElsewhere = Arbitrary.PortCode().WithValue(v => v.Except(anyRotation.Ports.Select(p => p.Value).ToArray())).Generate();
        Container   anyBox       = Stowage.AnyGeneralCargo(anyRotation.Ports[0], anyElsewhere).Generate();

        // Act
        StowageViolation violation = RefusalOf(() => plan.Assign(anyBox, new SlotPosition(Stowage.BayFor(anyBox.Size), 0, 2)));

        // Assert
        Assert.Equal(StowageViolation.PortNotInRotation, violation);
    }

    [Fact]
    public void Explosives_are_refused_in_the_hold() {
        // Arrange
        Rotation    anyRotation   = Stowage.AnyThreePortRotation();
        StowagePlan plan          = new(Stowage.AnyRoomyVessel().Generate(), anyRotation);
        Container   anyExplosives = Stowage.AnyGeneralCargo(anyRotation.Ports[0], anyRotation.Ports[1])
                                           .WithHazard(HazardClass.Explosives)
                                           .Generate();

        // Act
        StowageViolation violation = RefusalOf(() => plan.Assign(anyExplosives, new SlotPosition(Stowage.BayFor(anyExplosives.Size), 0, 2)));

        // Assert
        Assert.Equal(StowageViolation.ExplosivesUnderDeck, violation);
    }

    [Fact]
    public void Explosives_are_accepted_on_the_lowest_deck_tier() {
        // Arrange
        Rotation    anyRotation   = Stowage.AnyThreePortRotation();
        StowagePlan plan          = new(Stowage.AnyRoomyVessel().Generate(), anyRotation);
        Container   anyExplosives = Stowage.AnyGeneralCargo(anyRotation.Ports[0], anyRotation.Ports[1])
                                           .WithHazard(HazardClass.Explosives)
                                           .Generate();

        // Act
        StowageViolation violation = plan.Check(anyExplosives, new SlotPosition(Stowage.BayFor(anyExplosives.Size), 0, 82));

        // Assert
        Assert.Equal(StowageViolation.None, violation);
    }

    [Fact]
    public void A_box_cannot_float_above_an_empty_hold_tier() {
        // Arrange
        Rotation    anyRotation = Stowage.AnyThreePortRotation();
        StowagePlan plan        = new(Stowage.AnyRoomyVessel().Generate(), anyRotation);
        Container   anyBox      = Stowage.AnyGeneralCargo(anyRotation.Ports[0], anyRotation.Ports[2]).Generate();

        // Act
        StowageViolation violation = RefusalOf(() => plan.Assign(anyBox, new SlotPosition(Stowage.BayFor(anyBox.Size), 0, 4)));

        // Assert
        Assert.Equal(StowageViolation.NoSupportBelow, violation);
    }

    [Fact]
    public void A_box_heavier_than_the_one_below_is_refused() {
        // Arrange
        Rotation      anyRotation = Stowage.AnyThreePortRotation();
        StowagePlan   plan        = new(Stowage.AnyRoomyVessel().Generate(), anyRotation);
        ContainerSize anySize     = Any.Enum<ContainerSize>().Generate();
        AnyContainer  boxes       = Stowage.AnyGeneralCargo(anyRotation.Ports[0], anyRotation.Ports[2]).WithSize(anySize);
        int           bay         = Stowage.BayFor(anySize);
        plan.Assign(boxes.WithGrossMassTonnes(12m).Generate(), new SlotPosition(bay, 0, 2));
        Container anyHeavierBox = boxes.WithGrossMassTonnes(18m).Generate();

        // Act
        StowageViolation violation = RefusalOf(() => plan.Assign(anyHeavierBox, new SlotPosition(bay, 0, 4)));

        // Assert
        Assert.Equal(StowageViolation.HeavierThanContainerBelow, violation);
    }

    [Fact]
    public void A_stack_over_the_vessels_weight_limit_is_refused() {
        // Arrange
        Rotation      anyRotation = Stowage.AnyThreePortRotation();
        VesselProfile anyVessel   = Stowage.AnyRoomyVessel().WithMaxStackWeightTonnes(40m).Generate();
        StowagePlan   plan        = new(anyVessel, anyRotation);
        ContainerSize anySize     = Any.Enum<ContainerSize>().Generate();
        AnyContainer  boxes       = Stowage.AnyGeneralCargo(anyRotation.Ports[0], anyRotation.Ports[2]).WithSize(anySize);
        int           bay         = Stowage.BayFor(anySize);
        plan.Assign(boxes.WithGrossMassTonnes(25m).Generate(), new SlotPosition(bay, 0, 2));
        Container anyBoxOnTop = boxes.WithGrossMassTonnes(20m).Generate();

        // Act
        StowageViolation violation = RefusalOf(() => plan.Assign(anyBoxOnTop, new SlotPosition(bay, 0, 4)));

        // Assert
        Assert.Equal(StowageViolation.StackWeightExceeded, violation);
    }

    [Fact]
    public void A_box_discharged_later_cannot_sit_on_a_box_discharged_earlier() {
        // Arrange
        Rotation      anyRotation = Stowage.AnyThreePortRotation();
        StowagePlan   plan        = new(Stowage.AnyRoomyVessel().Generate(), anyRotation);
        ContainerSize anySize     = Any.Enum<ContainerSize>().Generate();
        int           bay         = Stowage.BayFor(anySize);
        Container anyBoxForSecondPort = Stowage.AnyGeneralCargo(anyRotation.Ports[0], anyRotation.Ports[1])
                                               .WithSize(anySize).WithGrossMassTonnes(20m).Generate();
        Container anyBoxForLastPort = Stowage.AnyGeneralCargo(anyRotation.Ports[0], anyRotation.Ports[2])
                                             .WithSize(anySize).WithGrossMassTonnes(10m).Generate();
        plan.Assign(anyBoxForSecondPort, new SlotPosition(bay, 0, 2));

        // Act
        StowageViolation violation = RefusalOf(() => plan.Assign(anyBoxForLastPort, new SlotPosition(bay, 0, 4)));

        // Assert
        Assert.Equal(StowageViolation.Overstow, violation);
    }

    [Fact]
    public void A_deck_box_discharged_after_a_hold_box_of_its_bay_blocks_the_hatch() {
        // Arrange
        Rotation      anyRotation = Stowage.AnyThreePortRotation();
        StowagePlan   plan        = new(Stowage.AnyRoomyVessel().Generate(), anyRotation);
        ContainerSize anySize     = Any.Enum<ContainerSize>().Generate();
        int           bay         = Stowage.BayFor(anySize);
        Container anyHoldBox = Stowage.AnyGeneralCargo(anyRotation.Ports[0], anyRotation.Ports[1]).WithSize(anySize).Generate();
        Container anyDeckBox = Stowage.AnyGeneralCargo(anyRotation.Ports[0], anyRotation.Ports[2]).WithSize(anySize).Generate();
        plan.Assign(anyHoldBox, new SlotPosition(bay, 0, 2));

        // Act
        StowageViolation violation = RefusalOf(() => plan.Assign(anyDeckBox, new SlotPosition(bay, 1, 82)));

        // Assert
        Assert.Equal(StowageViolation.HatchAccessBlocked, violation);
    }

    [Fact]
    public void Flammable_liquids_cannot_stand_beside_oxidizers() {
        // Arrange
        Rotation      anyRotation = Stowage.AnyThreePortRotation();
        StowagePlan   plan        = new(Stowage.AnyRoomyVessel().Generate(), anyRotation);
        ContainerSize anySize     = Any.Enum<ContainerSize>().Generate();
        int           bay         = Stowage.BayFor(anySize);
        AnyContainer  boxes       = Stowage.AnyGeneralCargo(anyRotation.Ports[0], anyRotation.Ports[2]).WithSize(anySize);
        plan.Assign(boxes.WithHazard(HazardClass.Oxidizers).Generate(), new SlotPosition(bay, 0, 2));
        Container anyFlammable = boxes.WithHazard(HazardClass.FlammableLiquids).Generate();

        // Act
        StowageViolation violation = RefusalOf(() => plan.Assign(anyFlammable, new SlotPosition(bay, 2, 2)));

        // Assert
        Assert.Equal(StowageViolation.SegregationConflict, violation);
    }

    [Fact]
    public void A_box_that_would_take_the_ship_past_its_maximum_displacement_is_refused() {
        // Arrange
        Rotation      anyRotation = Stowage.AnyThreePortRotation();
        VesselProfile anyVessel   = Stowage.AnyRoomyVessel().WithLightshipTonnes(20_000m).WithMaxDisplacementTonnes(20_040m).Generate();
        StowagePlan   plan        = new(anyVessel, anyRotation);
        ContainerSize anySize     = Any.Enum<ContainerSize>().Generate();
        int           bay         = Stowage.BayFor(anySize);
        AnyContainer  boxes       = Stowage.AnyGeneralCargo(anyRotation.Ports[0], anyRotation.Ports[2]).WithSize(anySize).WithGrossMassTonnes(25m);
        plan.Assign(boxes.Generate(), new SlotPosition(bay, 0, 2));

        // Act
        StowageViolation violation = RefusalOf(() => plan.Assign(boxes.Generate(), new SlotPosition(bay, 2, 2)));

        // Assert
        Assert.Equal(StowageViolation.DisplacementExceeded, violation);
    }

    [Fact]
    public void A_heavy_box_high_on_deck_raises_the_centre_of_gravity_past_the_limit() {
        // Arrange
        Rotation      anyRotation = Stowage.AnyThreePortRotation();
        VesselProfile anyVessel   = Stowage.AnyRoomyVessel()
                                           .WithHoldTiers(10)
                                           .WithLightshipTonnes(10_000m)
                                           .WithLightshipVcgMetres(10.0)
                                           .WithMaxVcgMetres(10.05)
                                           .Generate();
        StowagePlan   plan        = new(anyVessel, anyRotation);
        Container     anyBox      = Stowage.AnyGeneralCargo(anyRotation.Ports[0], anyRotation.Ports[2])
                                           .WithGrossMassTonnes(30m)
                                           .Generate();
        int           bay         = Stowage.BayFor(anyBox.Size);

        // Act
        StowageViolation onDeck  = plan.Check(anyBox, new SlotPosition(bay, 0, 82));
        StowageViolation inHold  = plan.Check(anyBox, new SlotPosition(bay, 0, 2));

        // Assert
        Assert.Equal(StowageViolation.VerticalStabilityExceeded, onDeck);
        Assert.Equal(StowageViolation.None, inHold);
    }

    [Fact]
    public void A_heavy_box_in_the_outermost_row_lists_the_ship_past_its_limit() {
        // Arrange
        Rotation      anyRotation = Stowage.AnyThreePortRotation();
        VesselProfile anyVessel   = Stowage.AnyRoomyVessel()
                                           .WithRowCount(21)
                                           .WithLightshipTonnes(10_000m)
                                           .WithMaxTcgMetres(0.05)
                                           .Generate();
        StowagePlan   plan        = new(anyVessel, anyRotation);
        Container     anyBox      = Stowage.AnyGeneralCargo(anyRotation.Ports[0], anyRotation.Ports[2])
                                           .WithGrossMassTonnes(30m)
                                           .Generate();
        int           bay         = Stowage.BayFor(anyBox.Size);

        // Act
        StowageViolation outboard   = plan.Check(anyBox, new SlotPosition(bay, 19, 2));
        StowageViolation centreline = plan.Check(anyBox, new SlotPosition(bay, 0, 2));

        // Assert
        Assert.Equal(StowageViolation.ListExceeded, outboard);
        Assert.Equal(StowageViolation.None, centreline);
    }

}
