using JustDummies;

using Xunit;

namespace VesselStowage.Domain.Tests;

public sealed class StowagePlanQueryTests {

    private static List<string> Slots(IEnumerable<StowageAssignment> assignments) {
        return assignments.Select(assignment => assignment.Slot.ToString()).ToList();
    }

    // DischargeSequence --------------------------------------------------------------------------------------------------

    [Fact]
    public void The_discharge_sequence_lists_the_boxes_for_the_port_by_bay_then_deck_before_hold_then_top_tier_first() {
        // Arrange: every box below is discharged at the third port of the rotation, except the one in 010102
        PlanScenario scenario = new PlanScenario();
        scenario.Stow(scenario.Teu(0, 2, 20m), 3, 2, 2);
        scenario.Stow(scenario.Teu(0, 2, 20m), 1, 0, 2);
        scenario.Stow(scenario.Teu(0, 2, 20m), 3, 1, 2);
        scenario.Stow(scenario.Teu(0, 2, 10m), 1, 0, 4);
        scenario.Stow(scenario.Teu(0, 2, 10m), 1, 0, 82);
        scenario.Stow(scenario.Teu(0, 3, 10m), 1, 1, 2);

        // Act
        IReadOnlyList<StowageAssignment> sequence = scenario.Plan.DischargeSequence(scenario.Port(2));

        // Assert
        Assert.Equal(["010082", "010004", "010002", "030102", "030202"], Slots(sequence));
    }

    [Fact]
    public void The_discharge_sequence_of_a_port_where_nothing_is_discharged_is_empty() {
        // Arrange
        PlanScenario scenario = new PlanScenario();
        scenario.Stow(scenario.Teu(0, 3), 1, 0, 2);

        // Act
        IReadOnlyList<StowageAssignment> sequence = scenario.Plan.DischargeSequence(scenario.Port(1));

        // Assert
        Assert.Empty(sequence);
    }

    [Fact]
    public void The_discharge_sequence_of_a_port_the_rotation_does_not_call_is_refused() {
        // Arrange
        PlanScenario scenario = new PlanScenario();

        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>("port", () => scenario.Plan.DischargeSequence(scenario.ForeignPort()));

        // Assert
        Assert.Contains("not in the rotation", refusal.Message);
    }

    [Fact]
    public void The_discharge_sequence_of_a_null_port_is_refused() {
        // Arrange
        PlanScenario scenario = new PlanScenario();

        // Act & Assert
        Assert.Throws<ArgumentNullException>("port", () => scenario.Plan.DischargeSequence(null!));
    }

    [Fact]
    public void The_discharge_sequence_does_not_change_the_plan() {
        // Arrange
        PlanScenario scenario = new PlanScenario();
        scenario.Stow(scenario.Teu(0, 2), 1, 0, 2);
        scenario.Stow(scenario.Teu(0, 2), 3, 0, 2);

        // Act
        scenario.Plan.DischargeSequence(scenario.Port(2));

        // Assert
        Assert.Equal(2, scenario.Plan.Assignments.Count);
    }

    // UsableSlotsFor -----------------------------------------------------------------------------------------------------

    // Two bays, two rows, two hold tiers and one deck tier: twelve slots.
    private static PlanScenario SmallVessel() {
        return new PlanScenario(PlanScenario.VesselOf(bays: 2, rows: 2, holdTiers: 2, deckTiers: 1));
    }

    private static List<string> Usable(PlanScenario scenario, Container container) {
        return scenario.Plan.UsableSlotsFor(container).Select(slot => slot.ToString()).OrderBy(text => text, StringComparer.Ordinal).ToList();
    }

    [Fact]
    public void On_an_empty_plan_a_twenty_foot_box_can_go_in_the_lowest_slots_of_the_odd_bays() {
        // Arrange
        PlanScenario scenario = SmallVessel();

        // Act
        List<string> usable = Usable(scenario, scenario.Teu());

        // Assert: bay 1 only (bay 2 is for forty-foot boxes), and no slot above a free one
        Assert.Equal(["010002", "010082", "010102", "010182"], usable);
    }

    [Fact]
    public void On_an_empty_plan_a_forty_foot_box_can_go_in_the_lowest_slots_of_the_even_bays() {
        // Arrange
        PlanScenario scenario = SmallVessel();

        // Act
        List<string> usable = Usable(scenario, scenario.Feu());

        // Assert
        Assert.Equal(["020002", "020082", "020102", "020182"], usable);
    }

    [Fact]
    public void Explosives_can_only_go_on_deck() {
        // Arrange
        PlanScenario scenario = SmallVessel();

        // Act
        List<string> usable = Usable(scenario, scenario.DangerousBox(ContainerSize.TwentyFoot, HazardClass.Explosives));

        // Assert
        Assert.Equal(["010082", "010182"], usable);
    }

    [Fact]
    public void A_stowed_box_takes_its_slot_away_and_opens_the_one_above_it() {
        // Arrange
        PlanScenario scenario = SmallVessel();
        scenario.Stow(scenario.Teu(grossTonnes: 20m), 1, 0, 2);

        // Act
        List<string> usable = Usable(scenario, scenario.Teu(grossTonnes: 10m));

        // Assert
        Assert.Equal(["010004", "010082", "010102", "010182"], usable);
    }

    [Fact]
    public void A_free_slot_above_a_lighter_box_is_not_usable_by_a_heavier_one() {
        // Arrange
        PlanScenario scenario = SmallVessel();
        scenario.Stow(scenario.Teu(grossTonnes: 20m), 1, 0, 2);

        // Act
        List<string> usable = Usable(scenario, scenario.Teu(grossTonnes: 25m));

        // Assert
        Assert.Equal(["010082", "010102", "010182"], usable);
    }

    [Fact]
    public void A_free_slot_next_to_incompatible_cargo_is_not_usable() {
        // Arrange
        PlanScenario scenario = SmallVessel();
        scenario.Stow(scenario.DangerousBox(ContainerSize.TwentyFoot, HazardClass.Explosives, grossTonnes: 20m), 1, 0, 82);

        // Act
        List<string> usable = Usable(scenario, scenario.DangerousBox(ContainerSize.TwentyFoot, HazardClass.Toxic, grossTonnes: 10m));

        // Assert: 010182 is next to the explosives; 010002 and 010102 are in the hold, another section
        Assert.Equal(["010002", "010102"], usable);
    }

    [Fact]
    public void A_box_already_in_the_plan_has_no_usable_slot() {
        // Arrange
        PlanScenario scenario = SmallVessel();
        Container anyBox      = scenario.Teu();
        scenario.Stow(anyBox, 1, 0, 2);

        // Act
        IReadOnlyList<SlotPosition> usable = scenario.Plan.UsableSlotsFor(anyBox);

        // Assert
        Assert.Empty(usable);
    }

    [Fact]
    public void The_usable_slots_are_the_ones_Check_allows() {
        // Arrange
        PlanScenario scenario = SmallVessel();
        scenario.Stow(scenario.Teu(0, 2, 20m), 1, 0, 2);
        scenario.Stow(scenario.Teu(1, 2, 10m), 1, 0, 4);
        scenario.Stow(scenario.Teu(1, 2, 10m), 1, 1, 82);
        Container candidate = scenario.Teu(1, 3, 5m);

        // Act
        List<SlotPosition> usable  = scenario.Plan.UsableSlotsFor(candidate).ToList();
        List<SlotPosition> allowed = scenario.Vessel.AllSlots().Where(slot => scenario.Plan.Check(candidate, slot) == StowageViolation.None).ToList();

        // Assert
        Assert.NotEmpty(usable);
        Assert.Equal(allowed, usable);
    }

    [Fact]
    public void A_null_container_has_no_usable_slots() {
        // Arrange
        PlanScenario scenario = SmallVessel();

        // Act & Assert
        Assert.Throws<ArgumentNullException>("container", () => scenario.Plan.UsableSlotsFor(null!));
    }

    // A plan filled the way a planner does: always in a slot the plan itself offers -----------------------------------

    [Fact]
    public void A_plan_filled_only_through_the_usable_slots_respects_every_stowage_rule_it_can_be_checked_against() {
        // Arrange
        PlanScenario scenario = new PlanScenario(Any.VesselProfile().WithBayCount(4).WithRowCount(3).WithHoldTiers(3).WithDeckTiers(2));
        VesselProfile vessel  = scenario.Vessel;

        // Act
        for (int attempt = 0; attempt < 60; attempt++) {
            Container box = scenario.AnyBox();
            List<SlotPosition> usable = scenario.Plan.UsableSlotsFor(box).ToList();
            if (usable.Count == 0) {
                continue;
            }

            scenario.Plan.Assign(box, Any.ElementOf(usable).Generate());
        }

        // Assert
        List<StowageAssignment> stowed = scenario.Plan.Assignments.ToList();

        Assert.Equal(stowed.Count, stowed.Select(a => a.Container.Id).Distinct().Count());
        Assert.Equal(stowed.Count, stowed.Select(a => a.Slot).Distinct().Count());
        Assert.All(stowed, assignment => {
            Assert.True(vessel.Contains(assignment.Slot));
            Assert.Equal(assignment.Slot.AcceptedSize, assignment.Container.Size);
            Assert.True(assignment.Container.Hazard != HazardClass.Explosives || assignment.Slot.IsOnDeck);

            SlotPosition? below = assignment.Slot.Below();
            if (below is not null) {
                StowageAssignment? support = scenario.Plan.At(below);
                Assert.NotNull(support);
                Assert.True(assignment.Container.GrossMassTonnes <= support!.Container.GrossMassTonnes);
                Assert.True(scenario.Rotation.IndexOf(assignment.Container.PortOfLoading) >= scenario.Rotation.IndexOf(support.Container.PortOfLoading));
                Assert.True(scenario.Rotation.IndexOf(assignment.Container.PortOfDischarge) <= scenario.Rotation.IndexOf(support.Container.PortOfDischarge));
            }
        });
        foreach (IGrouping<(int Bay, int Row, bool OnDeck), StowageAssignment> stack in stowed.GroupBy(a => (a.Slot.Bay, a.Slot.Row, a.Slot.IsOnDeck))) {
            Assert.True(stack.Sum(a => a.Container.GrossMassTonnes) <= vessel.MaxStackWeightTonnes);
        }

        foreach (StowageAssignment first in stowed) {
            foreach (StowageAssignment second in stowed.Where(other => other.Slot.IsAdjacentTo(first.Slot))) {
                Assert.True(first.Container.CanNeighbour(second.Container));
            }
        }

        Assert.True(scenario.Plan.DisplacementTonnes <= vessel.MaxDisplacementTonnes);
        Assert.True(scenario.Plan.VcgMetres <= vessel.MaxVcgMetres);
        Assert.True(Math.Abs(scenario.Plan.TcgMetres) <= vessel.MaxTcgMetres);
    }

}
