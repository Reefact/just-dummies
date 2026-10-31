using JustDummies;

using VesselStowage.Domain;

using Xunit;

namespace VesselStowage.Tests.Expert;

/// <summary>
///     Stability limits: the lightship and the limit are what each test is about, so they are literals;
///     the geometry and every other limit come from a roomy vessel.
/// </summary>
public sealed class StabilityTests {

    private static readonly AnyRotation AnyRotation = Any.Rotation();

    private static (StowagePlan Plan, AnyContainer AnyBoxOnTheVoyage) Voyage(VesselProfile vessel) {
        Rotation anyRotation = AnyRotation.Generate();

        return (new StowagePlan(vessel, anyRotation),
                Any.Container().SegregationNeutral().Travelling(anyRotation.Ports[0], anyRotation.Ports[^1]));
    }

    [Fact]
    public void Displacement_is_the_lightship_plus_the_gross_mass_of_every_box() {
        // Arrange
        VesselProfile anyVessel = Any.VesselProfile().Roomy().WithLightshipTonnes(10_000m).Generate();
        (StowagePlan plan, AnyContainer anyBox) = Voyage(anyVessel);
        AnySlotPosition anyFloor  = Any.SlotPosition().InRoomyVessel(ContainerSize.FortyFoot).AtFootOfHold();
        SlotPosition    anySlot   = anyFloor.Generate();
        plan.Assign(anyBox.FitFor(anySlot).WithGrossMassTonnes(20.5m).Generate(), anySlot);

        // Act
        plan.Assign(anyBox.FitFor(anySlot).WithGrossMassTonnes(12.25m).Generate(), anySlot.Above());

        // Assert
        Assert.Equal(10_032.75m, plan.DisplacementTonnes);
    }

    [Fact]
    public void A_box_that_would_sink_the_ship_below_its_marks_is_refused() {
        // Arrange
        VesselProfile anyVessel = Any.VesselProfile().Roomy().WithLightshipTonnes(10_000m).WithMaxDisplacementTonnes(10_020m).Generate();
        (StowagePlan plan, AnyContainer anyBox) = Voyage(anyVessel);
        SlotPosition anyFloor = Any.SlotPosition().InRoomyVessel(ContainerSize.FortyFoot).AtFootOfHold().Generate();

        // Act
        StowageViolation violation = plan.Check(anyBox.FitFor(anyFloor).WithGrossMassTonnes(25m).Generate(), anyFloor);

        // Assert
        Assert.Equal(StowageViolation.DisplacementExceeded, violation);
    }

    [Fact]
    public void A_heavy_box_high_on_deck_raises_the_centre_of_gravity_beyond_its_limit_where_the_hold_would_not() {
        // Arrange
        // 2 000 t lightship at 8.00 m, limit 8.05 m, four hold tiers: a 30 t box on deck lifts the VCG to
        // about 8.09 m, the same box on the floor of the hold lowers it to about 7.92 m.
        VesselProfile anyVessel = Any.VesselProfile().Roomy()
                                     .WithLightshipTonnes(2_000m).WithLightshipVcgMetres(8.0).WithMaxVcgMetres(8.05)
                                     .WithHoldTiers(4)
                                     .Generate();
        (StowagePlan plan, AnyContainer anyBox) = Voyage(anyVessel);
        SlotPosition anyCentrelineFloor = Any.SlotPosition().InRoomyVessel(ContainerSize.FortyFoot).AtFootOfHold().WithRow(0).Generate();
        SlotPosition sameStackOnDeck    = new(anyCentrelineFloor.Bay, 0, 82);
        Container    anyThirtyTonneBox  = anyBox.FitFor(anyCentrelineFloor).WithGrossMassTonnes(30m).Generate();

        // Act
        StowageViolation onDeck = plan.Check(anyThirtyTonneBox, sameStackOnDeck);
        StowageViolation inHold = plan.Check(anyThirtyTonneBox, anyCentrelineFloor);

        // Assert
        Assert.Equal(StowageViolation.VerticalStabilityExceeded, onDeck);
        Assert.Equal(StowageViolation.None, inHold);
    }

    [Fact]
    public void A_heavy_box_on_an_outboard_row_lists_the_ship_where_the_centreline_would_not() {
        // Arrange
        // 30 t on row 19 (24.4 m to starboard) of a 2 000 t ship: TCG about 0.36 m, over a 0.2 m limit.
        VesselProfile anyVessel = Any.VesselProfile().Roomy()
                                     .WithLightshipTonnes(2_000m).WithMaxTcgMetres(0.2)
                                     .WithRowCount(20)
                                     .Generate();
        (StowagePlan plan, AnyContainer anyBox) = Voyage(anyVessel);
        SlotPosition anyCentrelineFloor = Any.SlotPosition().InRoomyVessel(ContainerSize.TwentyFoot).AtFootOfHold().WithRow(0).Generate();
        SlotPosition outboardStarboard  = new(anyCentrelineFloor.Bay, 19, anyCentrelineFloor.Tier);
        Container    anyThirtyTonneBox  = anyBox.FitFor(anyCentrelineFloor).WithGrossMassTonnes(30m).Generate();

        // Act
        StowageViolation outboard   = plan.Check(anyThirtyTonneBox, outboardStarboard);
        StowageViolation centreline = plan.Check(anyThirtyTonneBox, anyCentrelineFloor);

        // Assert
        Assert.Equal(StowageViolation.ListExceeded, outboard);
        Assert.Equal(StowageViolation.None, centreline);
    }

}
