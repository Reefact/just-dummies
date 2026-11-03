using JustDummies;
using VesselStowage.Domain.Tests.Generators;
using Xunit;

namespace VesselStowage.Domain.Tests;

/// <summary>
///     The displacement and the centres of gravity of the plan. The vessel is the one of the stability tests:
///     1000 t lightship, centre of gravity 1.0 m above the keel, two hold tiers. Heights of box centres:
///     2.795 m in the first hold tier, 8.975 m in the first deck tier.
/// </summary>
public sealed class StowagePlanFiguresTests {

    private const int Precision = 9;

    private static AnyVesselProfile Vessel() {
        return Any.VesselProfile().Roomy().WithLightshipTonnes(1_000m).WithLightshipVcgMetres(1.0).WithHoldTiers(2);
    }

    [Fact]
    public void An_empty_plan_has_the_displacement_and_the_centre_of_gravity_of_the_bare_vessel() {
        // Arrange
        Voyage        voyage = Voyage.Draw();
        VesselProfile vessel = Any.VesselProfile().Roomy().Generate();
        StowagePlan   plan   = new StowagePlan(vessel, voyage.Rotation);

        // Assert
        Assert.Equal(vessel.LightshipTonnes, plan.DisplacementTonnes);
        Assert.Equal(vessel.LightshipVcgMetres, plan.VcgMetres, Precision);
        Assert.Equal(0.0, plan.TcgMetres);
    }

    [Fact]
    public void One_box_on_the_centreline_adds_its_gross_mass_and_lifts_the_centre_of_gravity_to_its_height() {
        // Arrange
        Voyage      voyage = Voyage.Draw();
        StowagePlan plan   = voyage.EmptyPlanOf(Vessel());

        // Act
        plan.Assign(voyage.Box().Weighing(20m).Generate(), new SlotPosition(1, 0, 2));

        // Assert: (1000 * 1.0 + 20 * 2.795) / 1020
        Assert.Equal(1_020m, plan.DisplacementTonnes);
        Assert.Equal(1.0351960784313727, plan.VcgMetres, Precision);
        Assert.Equal(0.0, plan.TcgMetres, Precision);
    }

    [Fact]
    public void The_figures_weigh_every_box_by_its_gross_mass_and_its_position() {
        // Arrange
        Voyage      voyage = Voyage.Draw();
        StowagePlan plan   = voyage.EmptyPlanOf(Vessel());

        // Act: 20 t in the hold on the centreline, 10 t on deck on row 1 (2.44 m to starboard)
        plan.Assign(voyage.Box().Weighing(20m).Generate(), new SlotPosition(1, 0, 2));
        plan.Assign(voyage.Box().Weighing(10m).Generate(), new SlotPosition(3, 1, 82));

        // Assert
        Assert.Equal(1_030m, plan.DisplacementTonnes);
        Assert.Equal((1_000 * 1.0 + 20 * 2.795 + 10 * 8.975) / 1_030, plan.VcgMetres, Precision);
        Assert.Equal(10 * 2.44 / 1_030, plan.TcgMetres, Precision);
    }

    [Fact]
    public void A_box_to_port_pulls_the_transverse_centre_of_gravity_the_other_way() {
        // Arrange
        Voyage      voyage = Voyage.Draw();
        StowagePlan plan   = voyage.EmptyPlanOf(Vessel());

        // Act: 10 t on row 1 (+2.44 m) and 15 t on row 2 (-2.44 m)
        plan.Assign(voyage.Box().Weighing(10m).Generate(), new SlotPosition(1, 1, 2));
        plan.Assign(voyage.Box().Weighing(15m).Generate(), new SlotPosition(1, 2, 2));

        // Assert
        Assert.Equal(-5 * 2.44 / 1_025, plan.TcgMetres, Precision);
    }

    [Fact]
    public void The_hold_depth_of_the_vessel_sets_the_height_of_the_deck_boxes() {
        // Arrange: four hold tiers put the first deck tier at 14.155 m
        Voyage      voyage = Voyage.Draw();
        StowagePlan plan   = voyage.EmptyPlanOf(Vessel().WithHoldTiers(4));

        // Act
        plan.Assign(voyage.Box().Weighing(20m).Generate(), new SlotPosition(1, 0, 82));

        // Assert
        Assert.Equal((1_000 * 1.0 + 20 * 14.155) / 1_020, plan.VcgMetres, Precision);
    }

    [Fact]
    public void Removing_a_box_takes_it_out_of_the_figures() {
        // Arrange
        Voyage      voyage    = Voyage.Draw();
        StowagePlan plan      = voyage.EmptyPlanOf(Vessel());
        Container   container = voyage.Box().Weighing(20m).Generate();
        plan.Assign(container, new SlotPosition(1, 1, 82));

        // Act
        plan.Remove(container.Id);

        // Assert
        Assert.Equal(1_000m, plan.DisplacementTonnes);
        Assert.Equal(1.0, plan.VcgMetres, Precision);
        Assert.Equal(0.0, plan.TcgMetres, Precision);
    }

    [Fact]
    public void A_refused_box_does_not_change_the_figures() {
        // Arrange
        Voyage      voyage = Voyage.Draw();
        StowagePlan plan   = voyage.EmptyPlanOf(Vessel().WithMaxDisplacementTonnes(1_010m));
        Container   tooMuch = voyage.Box().Weighing(20m).Generate();

        // Act
        Assert.Throws<StowageRefusedException>(() => plan.Assign(tooMuch, new SlotPosition(1, 1, 82)));

        // Assert
        Assert.Equal(1_000m, plan.DisplacementTonnes);
        Assert.Equal(1.0, plan.VcgMetres, Precision);
        Assert.Equal(0.0, plan.TcgMetres, Precision);
    }

}
