using JustDummies;

using Xunit;

namespace VesselStowage.Domain.Tests;

public sealed class StowagePlanTests {

    [Fact]
    public void A_plan_knows_its_vessel_and_its_rotation_and_starts_empty() {
        // Arrange
        VesselProfile anyVessel = Any.VesselProfile().Generate();
        Rotation anyRotation    = Any.Rotation().Generate();

        // Act
        StowagePlan plan = new StowagePlan(anyVessel, anyRotation);

        // Assert
        Assert.Same(anyVessel, plan.Vessel);
        Assert.Same(anyRotation, plan.Rotation);
        Assert.Empty(plan.Assignments);
    }

    [Fact]
    public void A_plan_needs_a_vessel_and_a_rotation() {
        // Arrange
        VesselProfile anyVessel = Any.VesselProfile().Generate();
        Rotation anyRotation    = Any.Rotation().Generate();

        // Act & Assert
        Assert.Throws<ArgumentNullException>("vessel", () => new StowagePlan(null!, anyRotation));
        Assert.Throws<ArgumentNullException>("rotation", () => new StowagePlan(anyVessel, null!));
    }

    [Fact]
    public void An_empty_plan_displaces_the_lightship_and_has_the_lightship_centre_of_gravity() {
        // Arrange
        PlanScenario scenario = new PlanScenario(PlanScenario.VesselOf(lightshipTonnes: 18_500m, lightshipVcgMetres: 11.25));

        // Assert
        Assert.Equal(18_500m, scenario.Plan.DisplacementTonnes);
        Assert.Equal(11.25, scenario.Plan.VcgMetres, 9);
        Assert.Equal(0.0, scenario.Plan.TcgMetres, 9);
    }

    // Assign ---------------------------------------------------------------------------------------------------------

    [Fact]
    public void A_box_is_stowed_in_a_free_bottom_slot_of_a_bay_that_takes_its_size() {
        // Arrange
        PlanScenario scenario = new PlanScenario();
        Container anyBox      = scenario.Teu();

        // Act
        StowageAssignment assignment = scenario.Plan.Assign(anyBox, new SlotPosition(1, 0, 2));

        // Assert
        Assert.Same(anyBox, assignment.Container);
        Assert.Equal(new SlotPosition(1, 0, 2), assignment.Slot);
        Assert.Same(assignment, scenario.Plan.At(new SlotPosition(1, 0, 2)));
        Assert.Same(assignment, scenario.Plan.Find(anyBox.Id));
        Assert.Single(scenario.Plan.Assignments);
    }

    [Fact]
    public void A_forty_foot_box_is_stowed_in_an_even_bay() {
        // Arrange
        PlanScenario scenario = new PlanScenario();
        Container anyBox      = scenario.Feu();

        // Act
        StowageAssignment assignment = scenario.Stow(anyBox, 2, 0, 2);

        // Assert
        Assert.Equal(new SlotPosition(2, 0, 2), assignment.Slot);
    }

    [Fact]
    public void The_lowest_slot_of_the_deck_needs_nothing_in_the_hold_below_it() {
        // Arrange
        PlanScenario scenario = new PlanScenario();

        // Act
        StowageAssignment assignment = scenario.Stow(scenario.Teu(), 1, 0, 82);

        // Assert
        Assert.True(assignment.Slot.IsOnDeck);
    }

    [Fact]
    public void A_box_is_stowed_on_top_of_one_that_is_at_least_as_heavy() {
        // Arrange
        PlanScenario scenario = new PlanScenario();
        scenario.Stow(scenario.Teu(grossTonnes: 20m), 1, 0, 2);
        scenario.Stow(scenario.Teu(grossTonnes: 20m), 1, 0, 4);

        // Act
        StowageAssignment top = scenario.Stow(scenario.Teu(grossTonnes: 12m), 1, 0, 6);

        // Assert
        Assert.Equal(new SlotPosition(1, 0, 6), top.Slot);
        Assert.Equal(3, scenario.Plan.Assignments.Count);
    }

    [Fact]
    public void The_refusal_names_the_container_the_slot_and_the_reason() {
        // Arrange
        PlanScenario scenario = new PlanScenario();
        Container anyBox      = scenario.Teu();

        // Act
        StowageRefusedException refusal = Assert.Throws<StowageRefusedException>(() => scenario.Stow(anyBox, 1, 0, 4));

        // Assert
        Assert.Equal(StowageViolation.NoSupportBelow, refusal.Violation);
        Assert.Contains(anyBox.Id.Value, refusal.Message);
        Assert.Contains("010004", refusal.Message);
        Assert.Contains("010002", refusal.Message);
    }

    [Fact]
    public void A_refused_assignment_leaves_the_plan_as_it_was() {
        // Arrange
        PlanScenario scenario = new PlanScenario();
        Container stowed      = scenario.Teu();
        Container refused     = scenario.Teu();
        scenario.Stow(stowed, 1, 0, 2);
        decimal displacementBefore = scenario.Plan.DisplacementTonnes;

        // Act
        Assert.Throws<StowageRefusedException>(() => scenario.Stow(refused, 1, 0, 2));

        // Assert
        Assert.Single(scenario.Plan.Assignments);
        Assert.Null(scenario.Plan.Find(refused.Id));
        Assert.Same(stowed, scenario.Plan.At(new SlotPosition(1, 0, 2))!.Container);
        Assert.Equal(displacementBefore, scenario.Plan.DisplacementTonnes);
    }

    [Fact]
    public void Assigning_a_missing_container_or_slot_is_refused() {
        // Arrange
        PlanScenario scenario = new PlanScenario();

        // Act & Assert
        Assert.Throws<ArgumentNullException>("container", () => scenario.Plan.Assign(null!, new SlotPosition(1, 0, 2)));
        Assert.Throws<ArgumentNullException>("slot", () => scenario.Plan.Assign(scenario.Teu(), null!));
    }

    // At / Find ------------------------------------------------------------------------------------------------------

    [Fact]
    public void Nothing_is_found_in_a_free_slot_or_for_a_container_that_is_not_stowed() {
        // Arrange
        PlanScenario scenario = new PlanScenario();
        scenario.Stow(scenario.Teu(), 1, 0, 2);

        // Act & Assert
        Assert.Null(scenario.Plan.At(new SlotPosition(1, 1, 2)));
        Assert.Null(scenario.Plan.Find(scenario.Teu().Id));
    }

    [Fact]
    public void Asking_for_a_null_slot_or_a_null_id_is_refused() {
        // Arrange
        PlanScenario scenario = new PlanScenario();

        // Act & Assert
        Assert.Throws<ArgumentNullException>("slot", () => scenario.Plan.At(null!));
        Assert.Throws<ArgumentNullException>("id", () => scenario.Plan.Find(null!));
    }

    // Check ----------------------------------------------------------------------------------------------------------

    [Fact]
    public void Checking_an_allowed_placement_says_none_and_stows_nothing() {
        // Arrange
        PlanScenario scenario = new PlanScenario();
        Container anyBox      = scenario.Teu();

        // Act
        StowageViolation violation = scenario.Plan.Check(anyBox, new SlotPosition(1, 0, 2));

        // Assert
        Assert.Equal(StowageViolation.None, violation);
        Assert.Empty(scenario.Plan.Assignments);
        Assert.Null(scenario.Plan.Find(anyBox.Id));
    }

    [Fact]
    public void Checking_a_refused_placement_names_the_violation_and_stows_nothing() {
        // Arrange
        PlanScenario scenario = new PlanScenario();
        Container anyBox      = scenario.Teu();

        // Act
        StowageViolation violation = scenario.Plan.Check(anyBox, new SlotPosition(2, 0, 2));

        // Assert
        Assert.Equal(StowageViolation.BayDoesNotFitSize, violation);
        Assert.Empty(scenario.Plan.Assignments);
    }

    [Fact]
    public void Checking_with_a_null_container_or_slot_is_refused() {
        // Arrange
        PlanScenario scenario = new PlanScenario();

        // Act & Assert
        Assert.Throws<ArgumentNullException>("container", () => scenario.Plan.Check(null!, new SlotPosition(1, 0, 2)));
        Assert.Throws<ArgumentNullException>("slot", () => scenario.Plan.Check(scenario.Teu(), null!));
    }

    // Remove ---------------------------------------------------------------------------------------------------------

    [Fact]
    public void A_removed_box_frees_its_slot_and_can_be_stowed_again() {
        // Arrange
        PlanScenario scenario = new PlanScenario();
        Container anyBox      = scenario.Teu();
        scenario.Stow(anyBox, 1, 0, 2);

        // Act
        scenario.Plan.Remove(anyBox.Id);

        // Assert
        Assert.Empty(scenario.Plan.Assignments);
        Assert.Null(scenario.Plan.At(new SlotPosition(1, 0, 2)));
        Assert.Null(scenario.Plan.Find(anyBox.Id));
        Assert.Equal(StowageViolation.None, scenario.Plan.Check(anyBox, new SlotPosition(1, 3, 2)));
    }

    [Fact]
    public void Removing_a_box_takes_its_mass_off_the_displacement() {
        // Arrange
        PlanScenario scenario = new PlanScenario(PlanScenario.VesselOf(lightshipTonnes: 20_000m));
        Container box         = scenario.Teu(grossTonnes: 12.5m);
        scenario.Stow(box, 1, 0, 2);

        // Act
        scenario.Plan.Remove(box.Id);

        // Assert
        Assert.Equal(20_000m, scenario.Plan.DisplacementTonnes);
    }

    [Fact]
    public void A_box_with_another_box_stacked_on_it_cannot_be_removed() {
        // Arrange
        PlanScenario scenario = new PlanScenario();
        Container bottom      = scenario.Teu(grossTonnes: 20m);
        scenario.Stow(bottom, 1, 0, 2);
        scenario.Stow(scenario.Teu(grossTonnes: 10m), 1, 0, 4);

        // Act
        StowageRefusedException refusal = Assert.Throws<StowageRefusedException>(() => scenario.Plan.Remove(bottom.Id));

        // Assert
        Assert.Equal(StowageViolation.ContainerStackedAbove, refusal.Violation);
        Assert.Contains("010004", refusal.Message);
        Assert.Same(bottom, scenario.Plan.Find(bottom.Id)!.Container);
    }

    [Fact]
    public void A_stack_is_unloaded_from_the_top() {
        // Arrange
        PlanScenario scenario = new PlanScenario();
        Container bottom      = scenario.Teu(grossTonnes: 20m);
        Container top         = scenario.Teu(grossTonnes: 10m);
        scenario.Stow(bottom, 1, 0, 2);
        scenario.Stow(top, 1, 0, 4);

        // Act
        scenario.Plan.Remove(top.Id);
        scenario.Plan.Remove(bottom.Id);

        // Assert
        Assert.Empty(scenario.Plan.Assignments);
    }

    [Fact]
    public void The_box_at_the_top_of_the_hold_can_be_removed_whatever_stands_on_the_deck() {
        // Arrange
        PlanScenario scenario = new PlanScenario(PlanScenario.VesselOf(holdTiers: 1, deckTiers: 1));
        Container holdBox     = scenario.Teu();
        Container deckBox     = scenario.Teu();
        scenario.Stow(holdBox, 1, 0, 2);
        scenario.Stow(deckBox, 1, 0, 82);

        // Act
        scenario.Plan.Remove(holdBox.Id);

        // Assert
        Assert.Same(deckBox, scenario.Plan.At(new SlotPosition(1, 0, 82))!.Container);
        Assert.Single(scenario.Plan.Assignments);
    }

    [Fact]
    public void A_box_that_is_not_in_the_plan_cannot_be_removed() {
        // Arrange
        PlanScenario scenario = new PlanScenario();
        Container notStowed   = scenario.Teu();

        // Act
        StowageRefusedException refusal = Assert.Throws<StowageRefusedException>(() => scenario.Plan.Remove(notStowed.Id));

        // Assert
        Assert.Equal(StowageViolation.NotStowed, refusal.Violation);
        Assert.Contains(notStowed.Id.Value, refusal.Message);
    }

    [Fact]
    public void Removing_a_null_id_is_refused() {
        // Arrange
        PlanScenario scenario = new PlanScenario();

        // Act & Assert
        Assert.Throws<ArgumentNullException>("id", () => scenario.Plan.Remove(null!));
    }

}
