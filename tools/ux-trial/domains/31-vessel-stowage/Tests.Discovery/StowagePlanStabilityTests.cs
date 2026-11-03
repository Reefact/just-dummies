using VesselStowage.Domain;

namespace VesselStowage.Tests;

public sealed class StowagePlanStabilityTests {

    /// <summary>Lightship 10 000 t at 10 m, three hold tiers, two deck tiers; the other limits are given by the test.</summary>
    private static StowagePlan PlanWith(decimal maxDisplacement = 100_000m, double maxVcg = 30.0, double maxTcg = 5.0) {
        VesselProfile vessel = Scene.RoomyVessel()
                                    .WithHoldTiers(3)
                                    .WithDeckTiers(2)
                                    .WithLightshipTonnes(10_000m)
                                    .WithMaxDisplacementTonnes(maxDisplacement)
                                    .WithLightshipVcgMetres(10.0)
                                    .WithMaxVcgMetres(maxVcg)
                                    .WithMaxTcgMetres(maxTcg)
                                    .Generate();

        return new StowagePlan(vessel, Scene.FourPortRotation());
    }

    [Fact]
    public void An_empty_plan_displaces_the_lightship_weight_with_its_centre_of_gravity() {
        StowagePlan plan = PlanWith();

        Assert.Equal(10_000m, plan.DisplacementTonnes);
        Assert.Equal(10.0, plan.VcgMetres, 9);
        Assert.Equal(0.0, plan.TcgMetres, 9);
    }

    [Fact]
    public void The_displacement_adds_the_gross_mass_of_every_box() {
        StowagePlan plan = PlanWith();
        plan.Assign(Scene.BoxOfMass(20m), new SlotPosition(1, 0, 2));
        plan.Assign(Scene.BoxOfMass(10.5m), new SlotPosition(1, 1, 2));

        Assert.Equal(10_030.5m, plan.DisplacementTonnes);
    }

    [Fact]
    public void The_vertical_centre_of_gravity_weights_each_box_by_its_height() {
        // Arrange: 20 t in a hold floor slot (centre 2.795 m), 10 t at hold tier 4 (5.385 m), 5 t on the first deck tier (11.565 m)
        StowagePlan plan = PlanWith();
        plan.Assign(Scene.BoxOfMass(20m), new SlotPosition(1, 0, 2));
        plan.Assign(Scene.BoxOfMass(10m), new SlotPosition(1, 0, 4));
        plan.Assign(Scene.BoxOfMass(5m), new SlotPosition(1, 0, 82));

        // Act
        double expected = (10_000 * 10.0 + 20 * 2.795 + 10 * 5.385 + 5 * 11.565) / 10_035.0;

        // Assert
        Assert.Equal(expected, plan.VcgMetres, 9);
    }

    [Fact]
    public void The_transverse_centre_of_gravity_weights_each_box_by_its_offset() {
        // Arrange: 20 t one row to starboard (+2.44 m), 10 t three rows to starboard (+4.88 m), 15 t one row to port (-2.44 m)
        StowagePlan plan = PlanWith();
        plan.Assign(Scene.BoxOfMass(20m), new SlotPosition(1, 1, 2));
        plan.Assign(Scene.BoxOfMass(10m), new SlotPosition(1, 3, 2));
        plan.Assign(Scene.BoxOfMass(15m), new SlotPosition(1, 2, 2));

        double expected = (20 * 2.44 + 10 * 4.88 - 15 * 2.44) / 10_045.0;

        Assert.Equal(expected, plan.TcgMetres, 9);
    }

    [Fact]
    public void Boxes_balanced_either_side_of_the_centreline_leave_the_centre_of_gravity_on_it() {
        StowagePlan plan = PlanWith();
        plan.Assign(Scene.BoxOfMass(12m), new SlotPosition(1, 1, 2));
        plan.Assign(Scene.BoxOfMass(12m), new SlotPosition(1, 2, 2));

        Assert.Equal(0.0, plan.TcgMetres, 9);
    }

    [Fact]
    public void The_centre_of_gravity_of_a_box_depends_on_the_hold_tier_count_for_a_deck_box() {
        // the same deck slot is higher on a vessel with more hold tiers
        VesselProfile fourTiers = Scene.RoomyVessel().WithHoldTiers(4).WithLightshipTonnes(10_000m).WithLightshipVcgMetres(10.0).WithMaxVcgMetres(40.0).Generate();
        StowagePlan   plan      = new StowagePlan(fourTiers, Scene.FourPortRotation());
        plan.Assign(Scene.BoxOfMass(10m), new SlotPosition(1, 0, 82));

        double deckBoxHeight = 1.5 + 4 * 2.59 + 1.0 + 1.295;
        Assert.Equal((10_000 * 10.0 + 10 * deckBoxHeight) / 10_010.0, plan.VcgMetres, 9);
    }

    [Fact]
    public void A_box_that_would_take_the_displacement_past_the_maximum_is_refused() {
        StowagePlan plan = PlanWith(maxDisplacement: 10_030m);
        plan.Assign(Scene.BoxOfMass(20m), new SlotPosition(1, 0, 2));

        StowageRefusedException refusal = Assert.Throws<StowageRefusedException>(() => plan.Assign(Scene.BoxOfMass(10.01m), new SlotPosition(1, 1, 2)));

        Assert.Equal(StowageViolation.DisplacementExceeded, refusal.Violation);
    }

    [Fact]
    public void A_displacement_that_reaches_the_maximum_exactly_is_accepted() {
        StowagePlan plan = PlanWith(maxDisplacement: 10_030m);
        plan.Assign(Scene.BoxOfMass(20m), new SlotPosition(1, 0, 2));

        plan.Assign(Scene.BoxOfMass(10m), new SlotPosition(1, 1, 2));

        Assert.Equal(10_030m, plan.DisplacementTonnes);
    }

    [Fact]
    public void A_box_that_would_raise_the_centre_of_gravity_past_the_maximum_is_refused() {
        // Arrange: lightship 10 000 t at 10 m; a single 30 t box at 11.565 m on deck raises the VCG to about 10.0037 m
        StowagePlan plan = PlanWith(maxVcg: 10.003);

        StowageRefusedException refusal = Assert.Throws<StowageRefusedException>(() => plan.Assign(Scene.BoxOfMass(30m), new SlotPosition(1, 0, 82)));

        Assert.Equal(StowageViolation.VerticalStabilityExceeded, refusal.Violation);
    }

    [Fact]
    public void A_low_box_that_lowers_the_centre_of_gravity_is_accepted_whatever_the_ceiling() {
        // a hold floor box (2.795 m) is below the lightship centre of gravity (10 m)
        StowagePlan plan = PlanWith(maxVcg: 10.003);

        plan.Assign(Scene.BoxOfMass(30m), new SlotPosition(1, 0, 2));

        Assert.True(plan.VcgMetres < 10.0);
    }

    [Fact]
    public void A_box_that_would_list_the_vessel_past_the_maximum_is_refused_on_either_side() {
        // Arrange: lightship 10 000 t; 30 t at 4.88 m moves the TCG by 0.0146 m
        StowagePlan starboardPlan = PlanWith(maxTcg: 0.01);
        StowagePlan portPlan      = PlanWith(maxTcg: 0.01);

        // Assert
        StowageRefusedException starboard = Assert.Throws<StowageRefusedException>(() => starboardPlan.Assign(Scene.BoxOfMass(30m), new SlotPosition(1, 3, 2)));
        StowageRefusedException port      = Assert.Throws<StowageRefusedException>(() => portPlan.Assign(Scene.BoxOfMass(30m), new SlotPosition(1, 4, 2)));
        Assert.Equal(StowageViolation.ListExceeded, starboard.Violation);
        Assert.Equal(StowageViolation.ListExceeded, port.Violation);
    }

    [Fact]
    public void A_box_on_the_centreline_never_lists_the_vessel() {
        StowagePlan plan = PlanWith(maxTcg: 0.0001);

        plan.Assign(Scene.BoxOfMass(30m), new SlotPosition(1, 0, 2));

        Assert.Equal(0.0, plan.TcgMetres, 9);
    }

    [Fact]
    public void A_second_box_on_the_other_side_can_cure_a_list_that_the_first_alone_would_cause() {
        // Arrange: each 30 t box alone moves the TCG by 0.0073 m, past the 0.005 m maximum; the pair would balance
        StowagePlan plan = PlanWith(maxTcg: 0.005);

        // Act / Assert: the first one is refused whichever side it goes to, even though the pair would balance
        Assert.Equal(StowageViolation.ListExceeded, plan.Check(Scene.BoxOfMass(30m), new SlotPosition(1, 1, 2)));
        Assert.Equal(StowageViolation.ListExceeded, plan.Check(Scene.BoxOfMass(30m), new SlotPosition(1, 2, 2)));
    }

    [Fact]
    public void The_displacement_is_checked_before_the_vertical_stability_and_the_list() {
        // a heavy box on deck and far off-centre, in a vessel that cannot take its mass at all
        StowagePlan plan = PlanWith(maxDisplacement: 10_010m, maxVcg: 10.0001, maxTcg: 0.0001);

        Assert.Equal(StowageViolation.DisplacementExceeded, plan.Check(Scene.BoxOfMass(30m), new SlotPosition(1, 3, 82)));
    }

    [Fact]
    public void The_vertical_stability_is_checked_before_the_list() {
        StowagePlan plan = PlanWith(maxVcg: 10.0001, maxTcg: 0.0001);

        Assert.Equal(StowageViolation.VerticalStabilityExceeded, plan.Check(Scene.BoxOfMass(30m), new SlotPosition(1, 3, 82)));
    }

    [Fact]
    public void The_stability_is_computed_with_every_planned_box_aboard_not_port_by_port() {
        // a box loaded at Rotterdam and discharged at Hamburg still counts for a box loaded at Felixstowe
        StowagePlan plan = PlanWith(maxDisplacement: 10_040m);
        plan.Assign(Scene.Box(Scene.Rotterdam, Scene.Hamburg).WithGrossMassTonnes(20m).Generate(), new SlotPosition(1, 0, 2));

        Container later = Scene.Box(Scene.Felixstowe, Scene.LeHavre).WithGrossMassTonnes(20.01m).Generate();

        Assert.Equal(StowageViolation.DisplacementExceeded, plan.Check(later, new SlotPosition(3, 0, 2)));
    }

    [Fact]
    public void Removing_a_box_restores_the_displacement_and_the_centres_of_gravity() {
        StowagePlan plan  = PlanWith();
        Container   first = Scene.BoxOfMass(20m);
        plan.Assign(first, new SlotPosition(1, 1, 2));
        plan.Assign(Scene.BoxOfMass(10m), new SlotPosition(1, 2, 2));
        plan.Remove(first.Id);

        Assert.Equal(10_010m, plan.DisplacementTonnes);
        Assert.Equal((10_000 * 10.0 + 10 * 2.795) / 10_010.0, plan.VcgMetres, 9);
        Assert.Equal(10 * -2.44 / 10_010.0, plan.TcgMetres, 9);
    }

    [Fact]
    public void A_generated_vessel_with_a_generated_box_can_always_take_a_first_box_on_the_hold_floor() {
        // The default limits of a generated vessel leave room for a single arbitrary general-cargo box: weight, stability, list
        VesselProfile anyVessel = Any.VesselProfile().Generate();
        StowagePlan   plan      = new StowagePlan(anyVessel, Scene.FourPortRotation());
        Container     anyBox    = Scene.Box(Scene.Rotterdam, Scene.Hamburg).Generate();
        SlotPosition  firstSlot = new SlotPosition(1, 0, 2);

        StowageViolation violation = plan.Check(anyBox, firstSlot);

        Assert.Equal(StowageViolation.None, violation);
    }

}
