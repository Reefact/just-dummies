using ShiftPlanning.Domain.Tests.Expert.Support;

namespace ShiftPlanning.Domain.Tests.Expert.RosterBehaviour;

public sealed class RosterRegistrationTests {

    [Fact]
    public void A_new_roster_is_a_draft_with_nothing_in_it() {
        // Arrange
        PlanningPeriod anyPeriod = Any.PlanningPeriod().Generate();

        // Act
        Roster roster = new Roster(anyPeriod);

        // Assert
        Assert.Same(anyPeriod, roster.Period);
        Assert.Equal(RosterStatus.Draft, roster.Status);
        Assert.Empty(roster.Sites);
        Assert.Empty(roster.Employees);
        Assert.Empty(roster.Shifts);
        Assert.Empty(roster.Meetings);
    }

    [Fact]
    public void A_roster_needs_a_period() {
        // Act
        Action construct = () => new Roster(null!);

        // Assert
        Assert.Throws<ArgumentNullException>("period", construct);
    }

    // ---- Sites -------------------------------------------------------------------------------------------------

    [Fact]
    public void A_registered_site_is_listed() {
        // Arrange
        Roster roster = new Roster(QuietPeriod.Period);
        Site anySite = Any.Site().Generate();

        // Act
        roster.RegisterSite(anySite);

        // Assert
        Assert.Same(anySite, Assert.Single(roster.Sites));
    }

    [Fact]
    public void A_site_whose_id_is_already_registered_is_refused_even_if_it_is_another_site() {
        // Arrange
        Roster roster = new Roster(QuietPeriod.Period);
        Site registered = Any.Site().Generate();
        Site sameIdElsewhere = Any.Site().WithId(registered.Id).Generate();
        roster.RegisterSite(registered);

        // Act
        Action register = () => roster.RegisterSite(sameIdElsewhere);

        // Assert
        Assert.Equal(RefusalReason.DuplicateSite, Refusal.Of(register));
        Assert.Same(registered, Assert.Single(roster.Sites));
    }

    [Fact]
    public void A_missing_site_is_refused() {
        // Arrange
        Roster roster = new Roster(QuietPeriod.Period);

        // Act
        Action register = () => roster.RegisterSite(null!);

        // Assert
        Assert.Throws<ArgumentNullException>("site", register);
    }

    // ---- Employees ---------------------------------------------------------------------------------------------

    [Fact]
    public void A_registered_employee_is_listed() {
        // Arrange
        Scene scene = new Scene();
        Site home = scene.OpenSite();
        Employee anyEmployee = Any.Employee().WithHomeSite(home).Generate();

        // Act
        scene.Roster.RegisterEmployee(anyEmployee);

        // Assert
        Assert.Same(anyEmployee, Assert.Single(scene.Roster.Employees));
    }

    [Fact]
    public void An_employee_whose_home_site_is_not_registered_is_refused() {
        // Arrange
        Roster roster = new Roster(QuietPeriod.Period);
        Employee employeeOfAnUnregisteredSite = Any.Employee().Generate();

        // Act
        Action register = () => roster.RegisterEmployee(employeeOfAnUnregisteredSite);

        // Assert
        Assert.Equal(RefusalReason.UnknownSite, Refusal.Of(register));
        Assert.Empty(roster.Employees);
    }

    [Fact]
    public void An_employee_whose_id_is_already_registered_is_refused() {
        // Arrange
        Scene scene = new Scene();
        Site home = scene.OpenSite();
        Employee registered = scene.Hire(home);
        Employee sameIdAgain = Any.Employee().WithId(registered.Id).WithHomeSite(home).Generate();

        // Act
        Action register = () => scene.Roster.RegisterEmployee(sameIdAgain);

        // Assert
        Assert.Equal(RefusalReason.DuplicateEmployee, Refusal.Of(register));
        Assert.Same(registered, Assert.Single(scene.Roster.Employees));
    }

    [Fact]
    public void An_unregistered_home_site_is_reported_before_a_duplicate_id() {
        // Arrange
        Scene scene = new Scene();
        Site home = scene.OpenSite();
        Employee registered = scene.Hire(home);
        Site unregisteredSite = Any.Site().WithId(Any.SiteId().DifferentFrom(home.Id).Generate()).Generate();
        Employee sameIdOfAnUnregisteredSite = Any.Employee().WithId(registered.Id).WithHomeSite(unregisteredSite).Generate();

        // Act
        Action register = () => scene.Roster.RegisterEmployee(sameIdOfAnUnregisteredSite);

        // Assert
        Assert.Equal(RefusalReason.UnknownSite, Refusal.Of(register));
    }

    [Fact]
    public void A_missing_employee_is_refused() {
        // Arrange
        Roster roster = new Roster(QuietPeriod.Period);

        // Act
        Action register = () => roster.RegisterEmployee(null!);

        // Assert
        Assert.Throws<ArgumentNullException>("employee", register);
    }

}
