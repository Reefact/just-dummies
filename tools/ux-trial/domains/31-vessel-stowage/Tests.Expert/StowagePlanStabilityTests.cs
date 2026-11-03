using JustDummies;
using VesselStowage.Domain.Tests.Generators;
using Xunit;

namespace VesselStowage.Domain.Tests;

/// <summary>
///     Displacement, vertical and transverse centres of gravity. The hand figures: a vessel of 1000 t lightship
///     with its centre of gravity 1.0 m above the keel, two hold tiers (so a box in the first hold tier has its
///     centre 2.795 m up and a box in the first deck tier 8.975 m up), a row 1 box 2.44 m to starboard.
/// </summary>
public sealed class StowagePlanStabilityTests {

    private static AnyVesselProfile Vessel() {
        return Any.VesselProfile().Roomy().WithLightshipTonnes(1_000m).WithLightshipVcgMetres(1.0).WithHoldTiers(2);
    }

    // --- displacement

    [Fact]
    public void The_displacement_may_reach_the_maximum_exactly() {
        // Arrange: 1000 t lightship + 15 t + 15 t = 1030 t
        Voyage      voyage = Voyage.Draw();
        StowagePlan plan   = voyage.EmptyPlanOf(Vessel().WithMaxDisplacementTonnes(1_030m));
        plan.Assign(voyage.Box().Weighing(15m).Generate(), new SlotPosition(1, 0, 2));
        Container container = voyage.Box().Weighing(15m).Generate();

        // Assert
        PlanAssert.Accepts(plan, container, new SlotPosition(1, 3, 2));
    }

    [Fact]
    public void A_box_taking_the_displacement_over_the_maximum_is_refused() {
        // Arrange: 1000 t lightship + 15 t + 15.01 t = 1030.01 t
        Voyage      voyage = Voyage.Draw();
        StowagePlan plan   = voyage.EmptyPlanOf(Vessel().WithMaxDisplacementTonnes(1_030m));
        plan.Assign(voyage.Box().Weighing(15m).Generate(), new SlotPosition(1, 0, 2));
        Container container = voyage.Box().Weighing(15.01m).Generate();

        // Assert
        PlanAssert.Refuses(plan, container, new SlotPosition(1, 3, 2), StowageViolation.DisplacementExceeded);
    }

    [Fact]
    public void Removing_a_box_makes_room_for_another_under_the_displacement_limit() {
        // Arrange
        Voyage      voyage    = Voyage.Draw();
        StowagePlan plan      = voyage.EmptyPlanOf(Vessel().WithMaxDisplacementTonnes(1_030m));
        Container   first     = voyage.Box().Weighing(20m).Generate();
        Container   second    = voyage.Box().Weighing(20m).Generate();
        plan.Assign(first, new SlotPosition(1, 0, 2));
        PlanAssert.Refuses(plan, second, new SlotPosition(1, 3, 2), StowageViolation.DisplacementExceeded);

        // Act
        plan.Remove(first.Id);

        // Assert
        PlanAssert.Accepts(plan, second, new SlotPosition(1, 3, 2));
    }

    // --- vertical centre of gravity: 20 t in the first hold tier gives 1.0352 m, in the first deck tier 1.1564 m

    [Fact]
    public void A_box_in_the_hold_keeps_the_centre_of_gravity_under_a_limit_a_box_on_deck_would_break() {
        // Arrange
        Voyage      voyage    = Voyage.Draw();
        StowagePlan plan      = voyage.EmptyPlanOf(Vessel().WithMaxVcgMetres(1.10));
        Container   container = voyage.Box().Weighing(20m).Generate();

        // Assert
        PlanAssert.Accepts(plan, container, new SlotPosition(1, 0, 2));
    }

    [Fact]
    public void A_box_that_lifts_the_centre_of_gravity_over_the_limit_is_refused() {
        // Arrange
        Voyage      voyage    = Voyage.Draw();
        StowagePlan plan      = voyage.EmptyPlanOf(Vessel().WithMaxVcgMetres(1.15));
        Container   container = voyage.Box().Weighing(20m).Generate();

        // Assert: 1.1564 m on deck against a limit of 1.15 m
        PlanAssert.Refuses(plan, container, new SlotPosition(1, 0, 82), StowageViolation.VerticalStabilityExceeded);
    }

    [Fact]
    public void The_same_box_on_deck_is_accepted_under_a_slightly_higher_limit() {
        // Arrange
        Voyage      voyage    = Voyage.Draw();
        StowagePlan plan      = voyage.EmptyPlanOf(Vessel().WithMaxVcgMetres(1.16));
        Container   container = voyage.Box().Weighing(20m).Generate();

        // Assert: 1.1564 m on deck against a limit of 1.16 m
        PlanAssert.Accepts(plan, container, new SlotPosition(1, 0, 82));
    }

    [Fact]
    public void The_centre_of_gravity_counts_every_box_already_planned() {
        // Arrange: a 20 t box on deck alone gives 1.1564 m, which a limit of 1.25 m allows;
        // a second one brings it to (1000 + 2 * 20 * 8.975) / 1040 = 1.3068 m
        Voyage      voyage = Voyage.Draw();
        StowagePlan plan   = voyage.EmptyPlanOf(Vessel().WithMaxVcgMetres(1.25));
        plan.Assign(voyage.Box().Weighing(20m).Generate(), new SlotPosition(1, 0, 82));
        Container second = voyage.Box().Weighing(20m).Generate();

        // Assert
        PlanAssert.Refuses(plan, second, new SlotPosition(1, 3, 82), StowageViolation.VerticalStabilityExceeded);
    }

    // --- transverse centre of gravity: 20 t on row 1 gives 0.0478 m to starboard

    private static AnyVesselProfile VesselWithATransverseLimitOf(double metres) {
        return Vessel().WithMaxTcgMetres(metres);
    }

    [Fact]
    public void A_box_on_the_centreline_never_lists_the_vessel() {
        // Arrange
        Voyage      voyage    = Voyage.Draw();
        StowagePlan plan      = voyage.EmptyPlanOf(VesselWithATransverseLimitOf(0.001));
        Container   container = voyage.Box().Weighing(30m).Generate();

        // Assert
        PlanAssert.Accepts(plan, container, new SlotPosition(1, 0, 2));
    }

    [Theory]
    [InlineData(5)] // row 5 is the third row to starboard: 7.32 m, giving 0.1435 m for 20 t
    [InlineData(6)] // row 6 is the third row to port: -7.32 m
    public void A_box_far_from_the_centreline_is_refused_on_either_side(int row) {
        // Arrange
        Voyage      voyage    = Voyage.Draw();
        StowagePlan plan      = voyage.EmptyPlanOf(VesselWithATransverseLimitOf(0.10));
        Container   container = voyage.Box().Weighing(20m).Generate();

        // Assert
        PlanAssert.Refuses(plan, container, new SlotPosition(1, row, 2), StowageViolation.ListExceeded);
    }

    [Theory]
    [InlineData(1)] // 0.0478 m to starboard
    [InlineData(2)] // 0.0478 m to port
    public void A_box_within_the_limit_on_either_side_is_accepted(int row) {
        // Arrange
        Voyage      voyage    = Voyage.Draw();
        StowagePlan plan      = voyage.EmptyPlanOf(VesselWithATransverseLimitOf(0.05));
        Container   container = voyage.Box().Weighing(20m).Generate();

        // Assert
        PlanAssert.Accepts(plan, container, new SlotPosition(1, row, 2));
    }

    [Fact]
    public void A_second_box_on_the_same_side_is_refused_where_one_on_the_other_side_is_accepted() {
        // Arrange: 20 t to starboard on row 1 is 0.0478 m; another 20 t on row 3 (4.88 m) would make 0.1408 m
        Voyage      voyage = Voyage.Draw();
        StowagePlan plan   = voyage.EmptyPlanOf(VesselWithATransverseLimitOf(0.05));
        plan.Assign(voyage.Box().Weighing(20m).Generate(), new SlotPosition(1, 1, 2));
        Container sameSide  = voyage.Box().Weighing(20m).Generate();
        Container otherSide = voyage.Box().Weighing(20m).Generate();

        // Assert: row 2 balances row 1 exactly
        PlanAssert.Refuses(plan, sameSide, new SlotPosition(1, 3, 2), StowageViolation.ListExceeded);
        PlanAssert.Accepts(plan, otherSide, new SlotPosition(1, 2, 2));
    }

    // --- the order in which the limits are checked

    [Fact]
    public void The_displacement_is_reported_before_the_vertical_centre_of_gravity() {
        // Arrange: a box on deck that breaks both the displacement limit (1010 t) and the VCG limit (1.10 m)
        Voyage      voyage    = Voyage.Draw();
        StowagePlan plan      = voyage.EmptyPlanOf(Vessel().WithMaxDisplacementTonnes(1_010m).WithMaxVcgMetres(1.10));
        Container   container = voyage.Box().Weighing(20m).Generate();

        // Assert
        PlanAssert.Refuses(plan, container, new SlotPosition(1, 0, 82), StowageViolation.DisplacementExceeded);
    }

    [Fact]
    public void The_vertical_centre_of_gravity_is_reported_before_the_list() {
        // Arrange: a box on deck on row 5 that breaks both the VCG limit (1.10 m) and the TCG limit (0.10 m)
        Voyage      voyage    = Voyage.Draw();
        StowagePlan plan      = voyage.EmptyPlanOf(Vessel().WithMaxVcgMetres(1.10).WithMaxTcgMetres(0.10));
        Container   container = voyage.Box().Weighing(20m).Generate();

        // Assert
        PlanAssert.Refuses(plan, container, new SlotPosition(1, 5, 82), StowageViolation.VerticalStabilityExceeded);
    }

    [Fact]
    public void Segregation_is_reported_before_the_stability_limits() {
        // Arrange
        Voyage      voyage = Voyage.Draw();
        StowagePlan plan   = voyage.EmptyPlanOf(Vessel().WithMaxDisplacementTonnes(1_010m));
        plan.Assign(voyage.Box().Carrying(HazardClass.FlammableLiquids).Weighing(5m).Generate(), new SlotPosition(1, 0, 2));
        Container container = voyage.Box().Carrying(HazardClass.Oxidizers).Weighing(20m).Generate();

        // Assert: next to the flammable box, and 25 t over a limit of 10 t
        PlanAssert.Refuses(plan, container, new SlotPosition(1, 1, 2), StowageViolation.SegregationConflict);
    }

}
