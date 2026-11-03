using ShiftPlanning.Domain.Tests.Expert.Support;

namespace ShiftPlanning.Domain.Tests.Expert.RosterBehaviour;

public sealed class RosterPublicationTests {

    [Fact]
    public void A_roster_whose_shifts_are_all_staffed_can_be_published() {
        // Arrange
        Scene scene = new Scene();
        Site site = scene.OpenSite();
        Employee employee = scene.Hire(site);
        Shift shift = scene.PlanAnyShiftAt(site);
        scene.Roster.Assign(shift.Number, employee.Id);

        // Act
        scene.Roster.Publish();

        // Assert
        Assert.Equal(RosterStatus.Published, scene.Roster.Status);
    }

    [Fact]
    public void A_roster_without_shifts_cannot_be_published() {
        // Arrange
        Scene scene = new Scene();
        scene.OpenSite();

        // Act
        Action publish = () => scene.Roster.Publish();

        // Assert
        Assert.Equal(RefusalReason.NothingToPublish, Refusal.Of(publish));
        Assert.Equal(RosterStatus.Draft, scene.Roster.Status);
    }

    [Fact]
    public void A_roster_with_one_unstaffed_shift_cannot_be_published() {
        // Arrange
        Scene scene = new Scene();
        Site site = scene.OpenSite();
        Employee employee = scene.Hire(site);
        Shift staffed = scene.PlanAnyShiftAt(site);
        scene.PlanAnyShiftAt(site);   // nobody takes it
        scene.Roster.Assign(staffed.Number, employee.Id);

        // Act
        Action publish = () => scene.Roster.Publish();

        // Assert
        Assert.Equal(RefusalReason.UnstaffedShift, Refusal.Of(publish));
        Assert.Equal(RosterStatus.Draft, scene.Roster.Status);
    }

    [Fact]
    public void A_roster_cannot_be_published_twice() {
        // Arrange
        Scene scene = new Scene();
        Site site = scene.OpenSite();
        Employee employee = scene.Hire(site);
        scene.PublishOneStaffedShift(employee, site);

        // Act
        Action publishAgain = () => scene.Roster.Publish();

        // Assert
        Assert.Equal(RefusalReason.RosterPublished, Refusal.Of(publishAgain));
    }

    [Fact]
    public void A_cancelled_unstaffed_shift_no_longer_blocks_publication() {
        // Arrange
        Scene scene = new Scene();
        Site site = scene.OpenSite();
        Employee employee = scene.Hire(site);
        Shift staffed = scene.PlanAnyShiftAt(site);
        Shift unstaffed = scene.PlanAnyShiftAt(site);
        scene.Roster.Assign(staffed.Number, employee.Id);
        scene.Roster.CancelShift(unstaffed.Number);

        // Act
        scene.Roster.Publish();

        // Assert
        Assert.Equal(RosterStatus.Published, scene.Roster.Status);
    }

    [Fact]
    public void Cancelling_the_only_shift_leaves_nothing_to_publish() {
        // Arrange
        Scene scene = new Scene();
        Site site = scene.OpenSite();
        Shift only = scene.PlanAnyShiftAt(site);
        scene.Roster.CancelShift(only.Number);

        // Act
        Action publish = () => scene.Roster.Publish();

        // Assert
        Assert.Equal(RefusalReason.NothingToPublish, Refusal.Of(publish));
    }

    [Fact]
    public void Sites_and_employees_can_still_be_registered_after_publication() {
        // Arrange
        Scene scene = new Scene();
        Site site = scene.OpenSite();
        Employee employee = scene.Hire(site);
        scene.PublishOneStaffedShift(employee, site);

        // Act
        Site lateSite = scene.OpenSite();
        Employee lateEmployee = scene.Hire(lateSite);

        // Assert
        Assert.Equal(2, scene.Roster.Sites.Count);
        Assert.Contains(lateEmployee, scene.Roster.Employees);
    }

}
