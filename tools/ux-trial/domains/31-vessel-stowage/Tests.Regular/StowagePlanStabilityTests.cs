using JustDummies;

using Xunit;

namespace VesselStowage.Domain.Tests;

/// <summary>Displacement, vertical centre of gravity and list: the limits of the vessel, and the figures the plan computes.</summary>
public sealed class StowagePlanStabilityTests {

    // Displacement --------------------------------------------------------------------------------------------------------

    [Fact]
    public void The_displacement_is_the_lightship_plus_the_gross_mass_of_every_box_stowed() {
        // Arrange
        PlanScenario scenario = new PlanScenario(PlanScenario.VesselOf(lightshipTonnes: 18_000m));
        scenario.Stow(scenario.Teu(grossTonnes: 12.5m), 1, 0, 2);
        scenario.Stow(scenario.Feu(grossTonnes: 20.25m), 2, 1, 2);
        scenario.Stow(scenario.Teu(grossTonnes: 7m), 1, 0, 4);

        // Act
        decimal displacement = scenario.Plan.DisplacementTonnes;

        // Assert
        Assert.Equal(18_039.75m, displacement);
    }

    [Fact]
    public void A_box_that_would_take_the_displacement_over_the_limit_is_refused() {
        // Arrange
        PlanScenario scenario = new PlanScenario(PlanScenario.VesselOf(lightshipTonnes: 10_000m, maxDisplacementTonnes: 10_050m));
        scenario.Stow(scenario.Teu(grossTonnes: 30m), 1, 0, 2);
        Container second = scenario.Teu(grossTonnes: 30m);

        // Act
        StowageRefusedException refusal = Assert.Throws<StowageRefusedException>(() => scenario.Stow(second, 1, 1, 2));

        // Assert
        Assert.Equal(StowageViolation.DisplacementExceeded, refusal.Violation);
        Assert.Equal(StowageViolation.DisplacementExceeded, scenario.Plan.Check(second, new SlotPosition(1, 1, 2)));
        Assert.Equal(10_030m, scenario.Plan.DisplacementTonnes);
    }

    [Fact]
    public void A_displacement_exactly_at_the_limit_is_accepted() {
        // Arrange
        PlanScenario scenario = new PlanScenario(PlanScenario.VesselOf(lightshipTonnes: 10_000m, maxDisplacementTonnes: 10_050m));
        scenario.Stow(scenario.Teu(grossTonnes: 30m), 1, 0, 2);

        // Act
        scenario.Stow(scenario.Teu(grossTonnes: 20m), 1, 1, 2);

        // Assert
        Assert.Equal(10_050m, scenario.Plan.DisplacementTonnes);
    }

    // Vertical centre of gravity ----------------------------------------------------------------------------------------

    [Fact]
    public void The_vertical_centre_of_gravity_is_the_mass_weighted_mean_height_of_the_lightship_and_the_boxes() {
        // Arrange: lightship 10 000 t at 10 m; a 30 t box at the lowest deck tier of a vessel with two hold tiers
        //          stands 1.5 + 2 x 2.59 + 1.0 + 2.59 / 2 = 8.975 m above the keel
        PlanScenario scenario = new PlanScenario(PlanScenario.VesselOf(holdTiers: 2, lightshipTonnes: 10_000m, lightshipVcgMetres: 10.0));
        scenario.Stow(scenario.Teu(grossTonnes: 30m), 1, 0, 82);

        // Act
        double vcg = scenario.Plan.VcgMetres;

        // Assert
        Assert.Equal((10_000 * 10.0 + 30 * 8.975) / 10_030, vcg, 9);
        Assert.Equal(9.996934, vcg, 6);
    }

    [Fact]
    public void A_box_that_would_raise_the_vertical_centre_of_gravity_over_the_limit_is_refused() {
        // Arrange: one 30 t box at the lowest deck tier brings the VCG to 9.99693 m, a second one above it to 10.00161 m
        PlanScenario scenario = new PlanScenario(PlanScenario.VesselOf(holdTiers: 2, lightshipTonnes: 10_000m, lightshipVcgMetres: 10.0, maxVcgMetres: 10.001));
        scenario.Stow(scenario.Teu(grossTonnes: 30m), 1, 0, 82);
        Container second = scenario.Teu(grossTonnes: 30m);

        // Act
        StowageRefusedException refusal = Assert.Throws<StowageRefusedException>(() => scenario.Stow(second, 1, 0, 84));

        // Assert
        Assert.Equal(StowageViolation.VerticalStabilityExceeded, refusal.Violation);
        Assert.Contains("10.00", refusal.Message);
    }

    [Fact]
    public void The_same_box_in_the_hold_keeps_the_vertical_centre_of_gravity_under_the_limit() {
        // Arrange
        PlanScenario scenario = new PlanScenario(PlanScenario.VesselOf(holdTiers: 2, lightshipTonnes: 10_000m, lightshipVcgMetres: 10.0, maxVcgMetres: 10.001));
        scenario.Stow(scenario.Teu(grossTonnes: 30m), 1, 0, 82);

        // Act
        StowageAssignment assignment = scenario.Stow(scenario.Teu(grossTonnes: 30m), 1, 1, 2);

        // Assert
        Assert.Equal(new SlotPosition(1, 1, 2), assignment.Slot);
        Assert.True(scenario.Plan.VcgMetres < 10.001);
    }

    // List --------------------------------------------------------------------------------------------------------------

    [Fact]
    public void The_transverse_centre_of_gravity_is_the_mass_weighted_mean_offset_over_the_whole_displacement() {
        // Arrange: lightship 10 000 t; a 30 t box in row 1 (2.44 m to starboard), a 10 t box in row 2 (2.44 m to port)
        PlanScenario scenario = new PlanScenario(PlanScenario.VesselOf(lightshipTonnes: 10_000m));
        scenario.Stow(scenario.Teu(grossTonnes: 30m), 1, 1, 2);
        scenario.Stow(scenario.Teu(grossTonnes: 10m), 1, 2, 2);

        // Act
        double tcg = scenario.Plan.TcgMetres;

        // Assert
        Assert.Equal((30 * 2.44 - 10 * 2.44) / 10_040, tcg, 9);
        Assert.True(tcg > 0, "more mass to starboard gives a positive TCG");
    }

    [Fact]
    public void Boxes_on_the_centreline_or_evenly_either_side_leave_the_transverse_centre_of_gravity_at_zero() {
        // Arrange
        PlanScenario scenario = new PlanScenario();
        scenario.Stow(scenario.Teu(grossTonnes: 18m), 1, 0, 2);
        scenario.Stow(scenario.Teu(grossTonnes: 21m), 1, 3, 2);
        scenario.Stow(scenario.Teu(grossTonnes: 21m), 1, 4, 2);

        // Act
        double tcg = scenario.Plan.TcgMetres;

        // Assert: row 3 is 2 x 2.44 m to starboard and row 4 is 2 x 2.44 m to port
        Assert.Equal(0.0, tcg, 9);
    }

    [Theory]
    [InlineData(1)]   // starboard
    [InlineData(2)]   // port
    public void A_box_that_would_list_the_vessel_over_the_limit_on_either_side_is_refused(int row) {
        // Arrange: 30 t at 2.44 m on 10 030 t is 0.0073 m, over a limit of 0.007 m
        PlanScenario scenario = new PlanScenario(PlanScenario.VesselOf(lightshipTonnes: 10_000m, maxTcgMetres: 0.007));
        Container box        = scenario.Teu(grossTonnes: 30m);

        // Act
        StowageRefusedException refusal = Assert.Throws<StowageRefusedException>(() => scenario.Stow(box, 1, row, 2));

        // Assert
        Assert.Equal(StowageViolation.ListExceeded, refusal.Violation);
    }

    [Fact]
    public void The_same_box_on_the_centreline_or_a_lighter_box_off_it_does_not_list_the_vessel_over_the_limit() {
        // Arrange
        PlanScenario scenario = new PlanScenario(PlanScenario.VesselOf(lightshipTonnes: 10_000m, maxTcgMetres: 0.007));

        // Act
        scenario.Stow(scenario.Teu(grossTonnes: 30m), 1, 0, 2);
        scenario.Stow(scenario.Teu(grossTonnes: 10m), 1, 1, 2);

        // Assert
        Assert.Equal(2, scenario.Plan.Assignments.Count);
    }

}
