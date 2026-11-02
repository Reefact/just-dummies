using Identity.Domain;

using JustDummies;

using Xunit;

namespace Identity.Tests.Regular;

public sealed class RoleAssignmentTests {

    private static AnyDateOnly AnyOrdinaryDay() => Any.DateOnly().Between(new DateOnly(2000, 1, 1), new DateOnly(2100, 12, 31));

    [Fact]
    public void An_assignment_keeps_what_it_was_granted_with() {
        // Arrange
        Guid     subjectId = Guid.Parse("11111111-2222-3333-4444-555555555555");
        DateOnly from      = new DateOnly(2025, 3, 1);
        DateOnly until     = new DateOnly(2025, 3, 31);

        // Act
        RoleAssignment assignment = new RoleAssignment(subjectId, Role.Contributor, from, until);

        // Assert
        Assert.Equal(subjectId, assignment.SubjectId);
        Assert.Equal(Role.Contributor, assignment.Role);
        Assert.Equal(from, assignment.ValidFrom);
        Assert.Equal(until, assignment.ValidUntil);
    }

    [Fact]
    public void An_open_ended_assignment_has_no_end_date() {
        // Arrange
        RoleAssignment anyOpenEnded = Any.RoleAssignment().WithoutValidUntil().Generate();

        // Assert
        Assert.Null(anyOpenEnded.ValidUntil);
    }

    [Fact]
    public void An_empty_subject_is_refused() {
        // Arrange
        Role     anyRole = Any.Enum<Role>().Generate();
        DateOnly anyDay  = AnyOrdinaryDay().Generate();

        // Act
        ArgumentException thrown = Assert.Throws<ArgumentException>(() => new RoleAssignment(Guid.Empty, anyRole, anyDay, null));

        // Assert
        Assert.Equal("subjectId", thrown.ParamName);
    }

    [Fact]
    public void An_undefined_role_is_refused() {
        // Arrange
        Guid     anySubjectId     = Any.Guid().NonEmpty().Generate();
        Role     anyUndefinedRole = (Role)Any.Int32().GreaterThan(3).Generate();
        DateOnly anyDay           = AnyOrdinaryDay().Generate();

        // Act
        ArgumentOutOfRangeException thrown = Assert.Throws<ArgumentOutOfRangeException>(
            () => new RoleAssignment(anySubjectId, anyUndefinedRole, anyDay, null));

        // Assert
        Assert.Equal("role", thrown.ParamName);
    }

    [Fact]
    public void An_end_date_before_the_start_date_is_refused() {
        // Arrange
        Guid     anySubjectId = Any.Guid().NonEmpty().Generate();
        Role     anyRole      = Any.Enum<Role>().Generate();
        DateOnly anyStart     = AnyOrdinaryDay().Generate();

        // Act
        ArgumentException thrown = Assert.Throws<ArgumentException>(
            () => new RoleAssignment(anySubjectId, anyRole, anyStart, anyStart.AddDays(-1)));

        // Assert
        Assert.Equal("validUntil", thrown.ParamName);
    }

    [Fact]
    public void A_one_day_assignment_is_allowed() {
        // Arrange
        Guid     anySubjectId = Any.Guid().NonEmpty().Generate();
        Role     anyRole      = Any.Enum<Role>().Generate();
        DateOnly anyDay       = AnyOrdinaryDay().Generate();

        // Act
        RoleAssignment assignment = new RoleAssignment(anySubjectId, anyRole, anyDay, anyDay);

        // Assert
        Assert.True(assignment.IsActiveOn(anyDay));
        Assert.False(assignment.IsActiveOn(anyDay.AddDays(1)));
        Assert.False(assignment.IsActiveOn(anyDay.AddDays(-1)));
    }

    [Fact]
    public void A_bounded_assignment_is_active_from_its_first_day_to_its_last_day_inclusive() {
        // Arrange
        DateOnly       anyStart   = AnyOrdinaryDay().Generate();
        DateOnly       end        = anyStart.AddDays(10);
        RoleAssignment assignment = Any.RoleAssignment().WithValidFrom(anyStart).WithValidUntil(end).Generate();

        // Act + Assert
        Assert.False(assignment.IsActiveOn(anyStart.AddDays(-1)));
        Assert.True(assignment.IsActiveOn(anyStart));
        Assert.True(assignment.IsActiveOn(anyStart.AddDays(5)));
        Assert.True(assignment.IsActiveOn(end));
        Assert.False(assignment.IsActiveOn(end.AddDays(1)));
    }

    [Fact]
    public void An_open_ended_assignment_stays_active_forever_after_its_start() {
        // Arrange
        DateOnly       anyStart   = AnyOrdinaryDay().Generate();
        RoleAssignment assignment = Any.RoleAssignment().WithValidFrom(anyStart).WithoutValidUntil().Generate();

        // Act + Assert
        Assert.False(assignment.IsActiveOn(anyStart.AddDays(-1)));
        Assert.True(assignment.IsActiveOn(anyStart));
        Assert.True(assignment.IsActiveOn(DateOnly.MaxValue));
    }

    [Fact]
    public void Any_generated_assignment_is_active_on_its_own_first_day() {
        // Arrange
        RoleAssignment anyAssignment = Any.RoleAssignment().Generate();

        // Act
        bool active = anyAssignment.IsActiveOn(anyAssignment.ValidFrom);

        // Assert
        Assert.True(active);
    }

}
