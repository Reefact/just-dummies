using JustDummies;

using VesselStowage.Domain;

using Xunit;

namespace VesselStowage.Tests.Discovery.Tests;

public sealed class StowagePlanTests {

    // Bay 02 takes 40-foot boxes. Every test below works in bays 02 and 04, rows 0 to 2,
    // hold tiers 02-06 and deck tiers 82-86.
    private const int Bay = 2;

    /// <summary>
    ///     A vessel large enough for the slots these tests use, with stability and stack limits far
    ///     above anything a few boxes can reach: tests not about those limits must not hit them.
    /// </summary>
    private static AnyVesselProfile AnyRoomyVessel() {
        return Any.VesselProfile()
                  .WithBayCount(b => b.GreaterThanOrEqualTo(4))
                  .WithRowCount(r => r.GreaterThanOrEqualTo(3))
                  .WithHoldTiers(t => t.GreaterThanOrEqualTo(3))
                  .WithDeckTiers(t => t.GreaterThanOrEqualTo(3))
                  .WithMaxStackWeightTonnes(1_000m)
                  .WithMaxDisplacementTonnes(1_000_000m)
                  .WithMaxVcgMetres(100.0)
                  .WithMaxTcgMetres(10.0);
    }

    /// <summary>A rotation calling exactly three ports, so a test can name the first, second and third call.</summary>
    private static Rotation AnyThreePortRotation() {
        return Any.Rotation().WithPorts(p => p.WithCount(3)).Generate();
    }

    private static AnyContainer AnyGeneralCargo(PortCode loading, PortCode discharge) {
        return Any.Container()
                  .WithSize(ContainerSize.FortyFoot)
                  .WithHazard(HazardClass.None)
                  .WithPortOfLoading(loading)
                  .WithPortOfDischarge(discharge);
    }

    [Fact]
    public void A_general_cargo_box_is_stowed_on_the_lowest_deck_tier() {
        // Arrange
        Rotation      anyRotation = AnyThreePortRotation();
        StowagePlan   plan        = new StowagePlan(AnyRoomyVessel().Generate(), anyRotation);
        Container     anyBox      = AnyGeneralCargo(anyRotation.Ports[0], anyRotation.Ports[1]).Generate();
        SlotPosition  slot        = new SlotPosition(Bay, 0, 82);

        // Act
        plan.Assign(anyBox, slot);

        // Assert
        Assert.Same(anyBox, plan.At(slot)?.Container);
    }

    [Fact]
    public void Checking_a_slot_does_not_stow_the_box() {
        // Arrange
        Rotation     anyRotation = AnyThreePortRotation();
        StowagePlan  plan        = new StowagePlan(AnyRoomyVessel().Generate(), anyRotation);
        Container    anyBox      = AnyGeneralCargo(anyRotation.Ports[0], anyRotation.Ports[2]).Generate();

        // Act
        StowageViolation violation = plan.Check(anyBox, new SlotPosition(Bay, 1, 2));

        // Assert
        Assert.Equal(StowageViolation.None, violation);
        Assert.Empty(plan.Assignments);
    }

    [Fact]
    public void A_box_needs_another_one_under_it_above_the_lowest_tier() {
        // Arrange
        Rotation    anyRotation = AnyThreePortRotation();
        StowagePlan plan        = new StowagePlan(AnyRoomyVessel().Generate(), anyRotation);
        Container   anyBox      = AnyGeneralCargo(anyRotation.Ports[0], anyRotation.Ports[1]).Generate();

        // Act
        StowageRefusedException refusal = Assert.Throws<StowageRefusedException>(() => plan.Assign(anyBox, new SlotPosition(Bay, 0, 84)));

        // Assert
        Assert.Equal(StowageViolation.NoSupportBelow, refusal.Violation);
    }

    [Fact]
    public void A_box_cannot_stand_on_a_lighter_one() {
        // Arrange
        Rotation    anyRotation = AnyThreePortRotation();
        StowagePlan plan        = new StowagePlan(AnyRoomyVessel().Generate(), anyRotation);
        plan.Assign(AnyGeneralCargo(anyRotation.Ports[0], anyRotation.Ports[1]).WithGrossMassTonnes(12m).Generate(), new SlotPosition(Bay, 0, 82));
        Container anyHeavierBox = AnyGeneralCargo(anyRotation.Ports[0], anyRotation.Ports[1]).WithGrossMassTonnes(18m).Generate();

        // Act
        StowageViolation violation = plan.Check(anyHeavierBox, new SlotPosition(Bay, 0, 84));

        // Assert
        Assert.Equal(StowageViolation.HeavierThanContainerBelow, violation);
    }

    [Fact]
    public void A_stack_cannot_exceed_the_vessel_stack_weight_limit() {
        // Arrange
        Rotation    anyRotation = AnyThreePortRotation();
        VesselProfile anyVessel = AnyRoomyVessel().WithMaxStackWeightTonnes(25m).Generate();
        StowagePlan plan        = new StowagePlan(anyVessel, anyRotation);
        plan.Assign(AnyGeneralCargo(anyRotation.Ports[0], anyRotation.Ports[1]).WithGrossMassTonnes(20m).Generate(), new SlotPosition(Bay, 0, 82));
        Container anySecondBox = AnyGeneralCargo(anyRotation.Ports[0], anyRotation.Ports[1]).WithGrossMassTonnes(10m).Generate();

        // Act
        StowageViolation violation = plan.Check(anySecondBox, new SlotPosition(Bay, 0, 84));

        // Assert
        Assert.Equal(StowageViolation.StackWeightExceeded, violation);
    }

    [Fact]
    public void A_box_discharged_later_cannot_overstow_one_discharged_earlier() {
        // Arrange
        Rotation    anyRotation = AnyThreePortRotation();
        StowagePlan plan        = new StowagePlan(AnyRoomyVessel().Generate(), anyRotation);
        plan.Assign(AnyGeneralCargo(anyRotation.Ports[0], anyRotation.Ports[1]).WithGrossMassTonnes(20m).Generate(), new SlotPosition(Bay, 0, 82));
        Container anyForThirdPort = AnyGeneralCargo(anyRotation.Ports[0], anyRotation.Ports[2]).WithGrossMassTonnes(15m).Generate();

        // Act
        StowageViolation violation = plan.Check(anyForThirdPort, new SlotPosition(Bay, 0, 84));

        // Assert
        Assert.Equal(StowageViolation.Overstow, violation);
    }

    [Fact]
    public void A_deck_box_cannot_block_the_hatch_over_a_hold_box_discharged_earlier() {
        // Arrange
        Rotation    anyRotation = AnyThreePortRotation();
        StowagePlan plan        = new StowagePlan(AnyRoomyVessel().Generate(), anyRotation);
        plan.Assign(AnyGeneralCargo(anyRotation.Ports[0], anyRotation.Ports[1]).Generate(), new SlotPosition(Bay, 0, 2));
        Container anyDeckBoxForThirdPort = AnyGeneralCargo(anyRotation.Ports[0], anyRotation.Ports[2]).Generate();

        // Act
        StowageViolation violation = plan.Check(anyDeckBoxForThirdPort, new SlotPosition(Bay, 2, 82));

        // Assert
        Assert.Equal(StowageViolation.HatchAccessBlocked, violation);
    }

    [Fact]
    public void A_box_whose_ports_are_called_in_the_wrong_order_is_refused() {
        // Arrange
        Rotation    anyRotation = AnyThreePortRotation();
        StowagePlan plan        = new StowagePlan(AnyRoomyVessel().Generate(), anyRotation);
        Container   anyBackwards = AnyGeneralCargo(anyRotation.Ports[2], anyRotation.Ports[0]).Generate();

        // Act
        StowageViolation violation = plan.Check(anyBackwards, new SlotPosition(Bay, 0, 82));

        // Assert
        Assert.Equal(StowageViolation.PortNotInRotation, violation);
    }

    [Fact]
    public void Explosives_are_stowed_on_deck_only() {
        // Arrange
        Rotation    anyRotation  = AnyThreePortRotation();
        StowagePlan plan         = new StowagePlan(AnyRoomyVessel().Generate(), anyRotation);
        Container   anyExplosive = AnyGeneralCargo(anyRotation.Ports[0], anyRotation.Ports[1]).WithHazard(HazardClass.Explosives).Generate();

        // Act
        StowageViolation inHold = plan.Check(anyExplosive, new SlotPosition(Bay, 0, 2));
        StowageViolation onDeck = plan.Check(anyExplosive, new SlotPosition(Bay, 0, 82));

        // Assert
        Assert.Equal(StowageViolation.ExplosivesUnderDeck, inHold);
        Assert.Equal(StowageViolation.None, onDeck);
    }

    [Fact]
    public void Flammables_and_oxidizers_are_not_stowed_side_by_side() {
        // Arrange
        Rotation    anyRotation = AnyThreePortRotation();
        StowagePlan plan        = new StowagePlan(AnyRoomyVessel().Generate(), anyRotation);
        plan.Assign(AnyGeneralCargo(anyRotation.Ports[0], anyRotation.Ports[1]).WithHazard(HazardClass.FlammableLiquids).Generate(), new SlotPosition(Bay, 0, 82));
        Container anyOxidizer = AnyGeneralCargo(anyRotation.Ports[0], anyRotation.Ports[1]).WithHazard(HazardClass.Oxidizers).Generate();

        // Act
        StowageViolation besideIt   = plan.Check(anyOxidizer, new SlotPosition(Bay, 1, 82));
        StowageViolation nextBayOver = plan.Check(anyOxidizer, new SlotPosition(Bay + 2, 0, 82));

        // Assert
        Assert.Equal(StowageViolation.SegregationConflict, besideIt);
        Assert.Equal(StowageViolation.None, nextBayOver);
    }

    [Fact]
    public void A_heavy_box_far_off_the_centreline_can_list_the_ship_beyond_its_limit() {
        // Arrange
        Rotation      anyRotation = AnyThreePortRotation();
        VesselProfile anyVessel   = AnyRoomyVessel().WithLightshipTonnes(2_000m).WithMaxTcgMetres(0.01).Generate();
        StowagePlan   plan        = new StowagePlan(anyVessel, anyRotation);
        Container     anyHeavyBox = AnyGeneralCargo(anyRotation.Ports[0], anyRotation.Ports[1]).WithGrossMassTonnes(30m).Generate();

        // Act
        StowageViolation offCentre = plan.Check(anyHeavyBox, new SlotPosition(Bay, 1, 2));
        StowageViolation centred   = plan.Check(anyHeavyBox, new SlotPosition(Bay, 0, 2));

        // Assert
        Assert.Equal(StowageViolation.ListExceeded, offCentre);
        Assert.Equal(StowageViolation.None, centred);
    }

    [Fact]
    public void The_displacement_adds_every_planned_box_to_the_lightship() {
        // Arrange
        Rotation      anyRotation = AnyThreePortRotation();
        VesselProfile anyVessel   = AnyRoomyVessel().WithLightshipTonnes(2_000m).Generate();
        StowagePlan   plan        = new StowagePlan(anyVessel, anyRotation);
        plan.Assign(AnyGeneralCargo(anyRotation.Ports[0], anyRotation.Ports[1]).WithGrossMassTonnes(20m).Generate(), new SlotPosition(Bay, 0, 82));
        plan.Assign(AnyGeneralCargo(anyRotation.Ports[0], anyRotation.Ports[1]).WithGrossMassTonnes(15m).Generate(), new SlotPosition(Bay, 0, 84));

        // Act
        decimal displacement = plan.DisplacementTonnes;

        // Assert
        Assert.Equal(2_035m, displacement);
    }

    [Fact]
    public void A_box_with_another_stacked_on_it_cannot_be_removed() {
        // Arrange
        Rotation    anyRotation = AnyThreePortRotation();
        StowagePlan plan        = new StowagePlan(AnyRoomyVessel().Generate(), anyRotation);
        Container   anyLower    = AnyGeneralCargo(anyRotation.Ports[0], anyRotation.Ports[1]).WithGrossMassTonnes(20m).Generate();
        plan.Assign(anyLower, new SlotPosition(Bay, 0, 82));
        plan.Assign(AnyGeneralCargo(anyRotation.Ports[0], anyRotation.Ports[1]).WithGrossMassTonnes(15m).Generate(), new SlotPosition(Bay, 0, 84));

        // Act
        StowageRefusedException refusal = Assert.Throws<StowageRefusedException>(() => plan.Remove(anyLower.Id));

        // Assert
        Assert.Equal(StowageViolation.ContainerStackedAbove, refusal.Violation);
    }

    [Fact]
    public void Removing_the_top_box_frees_its_slot() {
        // Arrange
        Rotation     anyRotation = AnyThreePortRotation();
        StowagePlan  plan        = new StowagePlan(AnyRoomyVessel().Generate(), anyRotation);
        Container    anyBox      = AnyGeneralCargo(anyRotation.Ports[0], anyRotation.Ports[1]).Generate();
        SlotPosition slot        = new SlotPosition(Bay, 0, 82);
        plan.Assign(anyBox, slot);

        // Act
        plan.Remove(anyBox.Id);

        // Assert
        Assert.Null(plan.At(slot));
        Assert.Null(plan.Find(anyBox.Id));
    }

    [Fact]
    public void The_slot_over_a_lighter_box_is_no_longer_usable_for_a_heavier_one() {
        // Arrange
        Rotation    anyRotation = AnyThreePortRotation();
        StowagePlan plan        = new StowagePlan(AnyRoomyVessel().Generate(), anyRotation);
        plan.Assign(AnyGeneralCargo(anyRotation.Ports[0], anyRotation.Ports[1]).WithGrossMassTonnes(10m).Generate(), new SlotPosition(Bay, 0, 82));
        Container anyHeavierBox = AnyGeneralCargo(anyRotation.Ports[0], anyRotation.Ports[1]).WithGrossMassTonnes(25m).Generate();

        // Act
        IReadOnlyList<SlotPosition> usable = plan.UsableSlotsFor(anyHeavierBox);

        // Assert
        Assert.DoesNotContain(new SlotPosition(Bay, 0, 84), usable);
        Assert.Contains(new SlotPosition(Bay, 1, 82), usable);
    }

    [Fact]
    public void The_discharge_sequence_lifts_deck_boxes_top_first_then_the_hold() {
        // Arrange
        Rotation    anyRotation = AnyThreePortRotation();
        StowagePlan plan        = new StowagePlan(AnyRoomyVessel().Generate(), anyRotation);
        PortCode    first       = anyRotation.Ports[0];
        PortCode    second      = anyRotation.Ports[1];
        Container   anyInHold   = AnyGeneralCargo(first, second).Generate();
        Container   anyOnDeck   = AnyGeneralCargo(first, second).WithGrossMassTonnes(20m).Generate();
        Container   anyOnTop    = AnyGeneralCargo(first, second).WithGrossMassTonnes(15m).Generate();
        Container   anyElsewhere = AnyGeneralCargo(first, anyRotation.Ports[2]).Generate();
        plan.Assign(anyInHold, new SlotPosition(Bay, 0, 2));
        plan.Assign(anyOnDeck, new SlotPosition(Bay, 0, 82));
        plan.Assign(anyOnTop, new SlotPosition(Bay, 0, 84));
        plan.Assign(anyElsewhere, new SlotPosition(Bay + 2, 0, 82));

        // Act
        IReadOnlyList<StowageAssignment> sequence = plan.DischargeSequence(second);

        // Assert
        Assert.Equal([anyOnTop.Id, anyOnDeck.Id, anyInHold.Id], sequence.Select(a => a.Container.Id));
    }

}
