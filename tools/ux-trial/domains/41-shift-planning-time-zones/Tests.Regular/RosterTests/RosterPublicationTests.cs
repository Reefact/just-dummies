using ShiftPlanning.Domain.Tests.Regular.Support;

using Xunit;

namespace ShiftPlanning.Domain.Tests.Regular.RosterTests;

public sealed class RosterPublicationTests {

    [Fact]
    public void A_roster_with_every_shift_staffed_can_be_published() {
        // Arrange
        Scene scene = new Scene();
        scene.PlanAndAssign(scene.First, 0, "08:00", "16:00");
        scene.PlanAndAssign(scene.Second, 0, "08:00", "16:00");

        // Act
        scene.Roster.Publish();

        // Assert
        Assert.Equal(RosterStatus.Published, scene.Roster.Status);
    }

    [Fact]
    public void A_roster_without_a_shift_cannot_be_published() {
        // Arrange
        Scene scene = new Scene();

        // Act
        RosterRefusedException refusal = Assert.Throws<RosterRefusedException>(() => scene.Roster.Publish());

        // Assert
        Assert.Equal(RefusalReason.NothingToPublish, refusal.Reason);
        Assert.Equal(RosterStatus.Draft, scene.Roster.Status);
    }

    [Fact]
    public void A_roster_whose_only_shift_was_cancelled_has_nothing_to_publish() {
        // Arrange
        Scene scene = new Scene();
        scene.Roster.CancelShift(scene.Plan(0, "08:00", "16:00").Number);

        // Act & Assert
        Assert.Equal(RefusalReason.NothingToPublish, Assert.Throws<RosterRefusedException>(() => scene.Roster.Publish()).Reason);
    }

    [Fact]
    public void A_roster_with_a_shift_nobody_works_cannot_be_published() {
        // Arrange
        Scene scene = new Scene();
        scene.PlanAndAssign(scene.First, 0, "08:00", "16:00");
        Shift unstaffed = scene.Plan(1, "08:00", "16:00");

        // Act
        RosterRefusedException refusal = Assert.Throws<RosterRefusedException>(() => scene.Roster.Publish());

        // Assert
        Assert.Equal(RefusalReason.UnstaffedShift, refusal.Reason);
        Assert.Contains($"#{unstaffed.Number}", refusal.Message);
        Assert.Equal(RosterStatus.Draft, scene.Roster.Status);
    }

    [Fact]
    public void A_roster_cannot_be_published_twice() {
        // Arrange
        Scene scene = new Scene();
        scene.Publish();

        // Act
        RosterRefusedException refusal = Assert.Throws<RosterRefusedException>(() => scene.Roster.Publish());

        // Assert
        Assert.Equal(RefusalReason.RosterPublished, refusal.Reason);
    }

    [Fact]
    public void A_published_roster_keeps_its_assignments() {
        // Arrange
        Scene scene = new Scene();
        Shift shift = scene.PlanAndAssign(scene.Second, 0, "08:00", "16:00");

        // Act
        scene.Roster.Publish();

        // Assert
        Assert.Equal(scene.Second.Id, scene.Roster.FindShift(shift.Number)!.AssignedTo);
    }

}
