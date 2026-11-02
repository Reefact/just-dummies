using Identity.Domain;

using JustDummies;

using Xunit;

namespace Identity.Tests.Discovery;

public sealed class RoleAssignmentTests {

    private static readonly Guid Subject = Guid.Parse("6f9619ff-8b86-d011-b42d-00c04fc964ff");

    [Fact]
    public void An_assignment_exposes_what_it_was_built_from() {
        // Act
        RoleAssignment assignment = new RoleAssignment(Subject, Role.Contributor, new DateOnly(2026, 3, 1), new DateOnly(2026, 3, 31));

        // Assert
        Assert.Equal(Subject, assignment.SubjectId);
        Assert.Equal(Role.Contributor, assignment.Role);
        Assert.Equal(new DateOnly(2026, 3, 1), assignment.ValidFrom);
        Assert.Equal(new DateOnly(2026, 3, 31), assignment.ValidUntil);
    }

    [Fact]
    public void An_open_ended_assignment_has_no_end_date() {
        // Act
        RoleAssignment assignment = new RoleAssignment(Subject, Role.Reader, new DateOnly(2026, 3, 1), null);

        // Assert
        Assert.Null(assignment.ValidUntil);
    }

    [Fact]
    public void An_assignment_to_the_empty_subject_is_refused() {
        // Arrange
        Role     anyRole      = Any.Enum<Role>().Generate();
        DateOnly anyStartDate = Any.DateOnly().Generate();

        // Act
        ArgumentException exception = Assert.Throws<ArgumentException>(() => new RoleAssignment(Guid.Empty, anyRole, anyStartDate, null));

        // Assert
        Assert.Equal("subjectId", exception.ParamName);
    }

    [Fact]
    public void An_assignment_of_a_role_that_is_not_declared_is_refused() {
        // Arrange
        Guid     anySubject   = Any.Guid().NonEmpty().Generate();
        Role     anyUndefined = (Role)Any.Int32().Except(0, 1, 2, 3).Generate();
        DateOnly anyStartDate = Any.DateOnly().Generate();

        // Act
        ArgumentOutOfRangeException exception = Assert.Throws<ArgumentOutOfRangeException>(() => new RoleAssignment(anySubject, anyUndefined, anyStartDate, null));

        // Assert
        Assert.Equal("role", exception.ParamName);
    }

    [Fact]
    public void An_assignment_ending_before_it_starts_is_refused() {
        // Arrange
        Guid anySubject = Any.Guid().NonEmpty().Generate();
        Role anyRole    = Any.Enum<Role>().Generate();
        (DateOnly earlier, DateOnly later) = Any.OrderedPair(Any.DateOnly(), Any.DateOnly(), strict: true).Generate();

        // Act
        ArgumentException exception = Assert.Throws<ArgumentException>(() => new RoleAssignment(anySubject, anyRole, later, earlier));

        // Assert
        Assert.Equal("validUntil", exception.ParamName);
    }

    [Fact]
    public void An_assignment_lasting_a_single_day_is_allowed() {
        // Arrange
        Guid     anySubject = Any.Guid().NonEmpty().Generate();
        Role     anyRole    = Any.Enum<Role>().Generate();
        DateOnly anyDay     = Any.DateOnly().Generate();

        // Act
        RoleAssignment assignment = new RoleAssignment(anySubject, anyRole, anyDay, anyDay);

        // Assert
        Assert.True(assignment.IsActiveOn(anyDay));
    }

    [Theory]
    [InlineData("2026-02-28", false)]                          // the day before
    [InlineData("2026-03-01", true)]                           // first day, inclusive
    [InlineData("2026-03-15", true)]
    [InlineData("2026-03-31", true)]                           // last day, inclusive
    [InlineData("2026-04-01", false)]                          // the day after
    public void A_bounded_assignment_is_active_from_its_first_to_its_last_day_inclusive(string day, bool expected) {
        // Arrange
        RoleAssignment assignment = new RoleAssignment(Subject, Role.Reader, new DateOnly(2026, 3, 1), new DateOnly(2026, 3, 31));

        // Act
        bool active = assignment.IsActiveOn(DateOnly.Parse(day));

        // Assert
        Assert.Equal(expected, active);
    }

    [Theory]
    [InlineData("2026-02-28", false)]
    [InlineData("2026-03-01", true)]
    [InlineData("2099-12-31", true)]
    [InlineData("9999-12-31", true)]
    public void An_open_ended_assignment_is_active_from_its_first_day_on_for_ever(string day, bool expected) {
        // Arrange
        RoleAssignment assignment = new RoleAssignment(Subject, Role.Reader, new DateOnly(2026, 3, 1), null);

        // Act
        bool active = assignment.IsActiveOn(DateOnly.Parse(day));

        // Assert
        Assert.Equal(expected, active);
    }

}
