using JustDummies;

using ShiftPlanning.Domain.Tests.Regular.Support;

using Xunit;

namespace ShiftPlanning.Domain.Tests.Regular.RosterTests;

public sealed class RosterRegistrationTests {

    [Fact]
    public void A_new_roster_is_a_draft_with_nothing_in_it() {
        // Arrange
        PlanningPeriod anyPeriod = new AnyPlanningPeriod().Generate();

        // Act
        Roster roster = new Roster(anyPeriod);

        // Assert
        Assert.Equal(RosterStatus.Draft, roster.Status);
        Assert.Same(anyPeriod, roster.Period);
        Assert.Empty(roster.Sites);
        Assert.Empty(roster.Employees);
        Assert.Empty(roster.Shifts);
        Assert.Empty(roster.Meetings);
    }

    [Fact]
    public void A_roster_needs_a_period() {
        Assert.Throws<ArgumentNullException>(() => new Roster(null!));
    }

    [Fact]
    public void Registered_sites_are_kept() {
        // Arrange
        Roster     roster = new Roster(new AnyPlanningPeriod().Generate());
        List<Site> anySites = Kit.OfficesIn(Kit.Paris, Kit.Tokyo, Kit.NewYork);

        // Act
        foreach (Site site in anySites) { roster.RegisterSite(site); }

        // Assert
        Assert.Equal(anySites.OrderBy(s => s.Id.Value), roster.Sites.OrderBy(s => s.Id.Value));
    }

    [Fact]
    public void A_site_registered_twice_is_refused() {
        // Arrange
        Site   anySite = new AnySite().Generate();
        Roster roster  = Kit.RosterWith(new AnyPlanningPeriod().Generate(), anySite);

        // Act
        RosterRefusedException refusal = Assert.Throws<RosterRefusedException>(() => roster.RegisterSite(anySite));

        // Assert
        Assert.Equal(RefusalReason.DuplicateSite, refusal.Reason);
        Assert.Single(roster.Sites);
    }

    [Fact]
    public void Another_site_with_the_same_id_is_refused() {
        // Arrange
        Site   anySite  = new AnySite().Generate();
        Site   homonym  = new AnySite().WithId(anySite.Id).Generate();
        Roster roster   = Kit.RosterWith(new AnyPlanningPeriod().Generate(), anySite);

        // Act & Assert
        Assert.Equal(RefusalReason.DuplicateSite, Assert.Throws<RosterRefusedException>(() => roster.RegisterSite(homonym)).Reason);
    }

    [Fact]
    public void An_employee_whose_home_site_is_not_registered_is_refused() {
        // Arrange
        Roster   roster       = new Roster(new AnyPlanningPeriod().Generate());
        Employee anyEmployee  = new AnyEmployee().Generate();

        // Act
        RosterRefusedException refusal = Assert.Throws<RosterRefusedException>(() => roster.RegisterEmployee(anyEmployee));

        // Assert
        Assert.Equal(RefusalReason.UnknownSite, refusal.Reason);
        Assert.Empty(roster.Employees);
    }

    [Fact]
    public void An_employee_of_a_registered_site_is_kept() {
        // Arrange
        Site     anySite     = new AnySite().Generate();
        Roster   roster      = Kit.RosterWith(new AnyPlanningPeriod().Generate(), anySite);
        Employee anyEmployee = new AnyEmployee().WithHomeSite(anySite).Generate();

        // Act
        roster.RegisterEmployee(anyEmployee);

        // Assert
        Assert.Same(anyEmployee, Assert.Single(roster.Employees));
    }

    [Fact]
    public void An_employee_registered_twice_is_refused() {
        // Arrange
        Site     anySite     = new AnySite().Generate();
        Roster   roster      = Kit.RosterWith(new AnyPlanningPeriod().Generate(), anySite);
        Employee anyEmployee = new AnyEmployee().WithHomeSite(anySite).Generate();
        roster.RegisterEmployee(anyEmployee);

        // Act
        RosterRefusedException refusal = Assert.Throws<RosterRefusedException>(() => roster.RegisterEmployee(anyEmployee));

        // Assert
        Assert.Equal(RefusalReason.DuplicateEmployee, refusal.Reason);
    }

    [Fact]
    public void Another_employee_with_the_same_number_is_refused() {
        // Arrange
        Site     anySite     = new AnySite().Generate();
        Roster   roster      = Kit.RosterWith(new AnyPlanningPeriod().Generate(), anySite);
        Employee anyEmployee = new AnyEmployee().WithHomeSite(anySite).Generate();
        Employee namesake    = new AnyEmployee().WithHomeSite(anySite).WithId(anyEmployee.Id).Generate();
        roster.RegisterEmployee(anyEmployee);

        // Act & Assert
        Assert.Equal(RefusalReason.DuplicateEmployee, Assert.Throws<RosterRefusedException>(() => roster.RegisterEmployee(namesake)).Reason);
    }

    [Fact]
    public void Registering_nothing_is_refused() {
        // Arrange
        Roster roster = new Roster(new AnyPlanningPeriod().Generate());

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => roster.RegisterSite(null!));
        Assert.Throws<ArgumentNullException>(() => roster.RegisterEmployee(null!));
    }

    [Fact]
    public void A_refusal_names_a_defined_reason() {
        Assert.Throws<ArgumentOutOfRangeException>(() => new RosterRefusedException(RefusalReason.None, "no reason"));
        Assert.Throws<ArgumentOutOfRangeException>(() => new RosterRefusedException((RefusalReason)999, "unknown reason"));
        Assert.IsAssignableFrom<InvalidOperationException>(new RosterRefusedException(RefusalReason.UnknownShift, "no such shift"));
    }

}
