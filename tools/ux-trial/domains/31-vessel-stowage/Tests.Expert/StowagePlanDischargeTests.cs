using JustDummies;
using VesselStowage.Domain.Tests.Generators;
using Xunit;

namespace VesselStowage.Domain.Tests;

public sealed class StowagePlanDischargeTests {

    [Fact]
    public void The_crane_order_is_by_bay_then_deck_before_hold_then_top_tier_first_then_row() {
        // Arrange: boxes are stowed in an order unrelated to the expected one
        Voyage      voyage = Voyage.Draw();
        StowagePlan plan   = voyage.EmptyPlan();
        PortCode    port   = voyage.SecondCall;

        void Stow(int bay, int row, int tier, decimal grossMassTonnes) {
            plan.Assign(voyage.Box().Sailing(voyage.FirstCall, port).Weighing(grossMassTonnes).Generate(), new SlotPosition(bay, row, tier));
        }

        Stow(3, 1, 82, 20m);
        Stow(1, 1, 2, 20m);
        Stow(3, 0, 2, 20m);
        Stow(1, 0, 82, 20m);
        Stow(1, 1, 4, 10m);
        Stow(1, 0, 2, 20m);
        Stow(3, 0, 82, 20m);
        Stow(1, 0, 4, 10m);
        Stow(1, 1, 82, 20m);
        Stow(1, 1, 84, 10m);

        // Act
        IReadOnlyList<StowageAssignment> sequence = plan.DischargeSequence(port);

        // Assert
        Assert.Equal(new[] {
            "010184", // bay 1, deck, top tier first
            "010082", "010182", // bay 1, deck, lowest tier, by row
            "010004", "010104", // bay 1, hold, top tier, by row
            "010002", "010102", // bay 1, hold, lowest tier, by row
            "030082", "030182", // bay 3, deck, by row
            "030002", // bay 3, hold
        }, sequence.Select(assignment => assignment.Slot.ToString()).ToArray());
    }

    [Fact]
    public void Only_the_boxes_discharged_at_the_port_are_listed() {
        // Arrange
        Voyage      voyage    = Voyage.Draw();
        StowagePlan plan      = voyage.EmptyPlan();
        Container   leaving   = voyage.Box().Sailing(voyage.FirstCall, voyage.SecondCall).Generate();
        Container   staying   = voyage.Box().Sailing(voyage.FirstCall, voyage.ThirdCall).Generate();
        Container   boarding  = voyage.Box().Sailing(voyage.SecondCall, voyage.ThirdCall).Generate();
        plan.Assign(leaving, new SlotPosition(1, 0, 2));
        plan.Assign(staying, new SlotPosition(1, 1, 2));
        plan.Assign(boarding, new SlotPosition(1, 2, 2));

        // Act
        IReadOnlyList<StowageAssignment> sequence = plan.DischargeSequence(voyage.SecondCall);

        // Assert
        Assert.Same(leaving, Assert.Single(sequence).Container);
    }

    [Fact]
    public void Nothing_is_discharged_where_no_box_is_bound() {
        // Arrange: the first port is only ever a port of loading
        Voyage      voyage = Voyage.Draw();
        StowagePlan plan   = voyage.EmptyPlan();
        plan.Assign(voyage.Box().Generate(), new SlotPosition(1, 0, 2));

        // Assert
        Assert.Empty(plan.DischargeSequence(voyage.FirstCall));
    }

    [Fact]
    public void An_empty_plan_discharges_nothing_anywhere() {
        // Arrange
        Voyage      voyage = Voyage.Draw();
        StowagePlan plan   = voyage.EmptyPlan();

        // Assert
        Assert.Empty(plan.DischargeSequence(voyage.SecondCall));
    }

    [Fact]
    public void A_port_the_vessel_does_not_call_at_has_no_discharge_sequence() {
        // Arrange
        Voyage      voyage = Voyage.Draw();
        StowagePlan plan   = voyage.EmptyPlan();
        PortCode    port   = voyage.Elsewhere();

        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => plan.DischargeSequence(port));

        // Assert
        Assert.Contains(port.Value, refusal.Message);
    }

    [Fact]
    public void A_missing_port_has_no_discharge_sequence() {
        Assert.Throws<ArgumentNullException>(() => Voyage.Draw().EmptyPlan().DischargeSequence(null!));
    }

}
