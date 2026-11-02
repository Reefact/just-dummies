using Identity.Domain.Tests.Expert.Generators;

using JustDummies;

using Xunit;

namespace Identity.Domain.Tests.Expert;

public sealed class RoleAssignmentTests {

    private static readonly DateOnly FirstDay = new DateOnly(2025, 3, 1);
    private static readonly DateOnly LastDay  = new DateOnly(2025, 3, 31);

    [Fact]
    public void An_assignment_keeps_the_subject_the_role_and_the_dates_it_was_given() {
        // Arrange
        Guid subjectId = Guid.Parse("0d6c1f0e-8f0b-4d52-9b0e-6f0e7f6a5c11");

        // Act
        RoleAssignment assignment = new RoleAssignment(subjectId, Role.Contributor, FirstDay, LastDay);

        // Assert
        Assert.Equal(subjectId, assignment.SubjectId);
        Assert.Equal(Role.Contributor, assignment.Role);
        Assert.Equal(FirstDay, assignment.ValidFrom);
        Assert.Equal(LastDay, assignment.ValidUntil);
    }

    [Fact]
    public void An_assignment_can_have_no_end() {
        // Arrange
        Guid anySubjectId = Any.Guid().NonEmpty().Generate();
        Role anyRole      = Any.Enum<Role>().Generate();

        // Act
        RoleAssignment assignment = new RoleAssignment(anySubjectId, anyRole, FirstDay, null);

        // Assert
        Assert.Null(assignment.ValidUntil);
    }

    [Fact]
    public void An_assignment_can_start_and_end_on_the_same_day() {
        // Arrange
        Guid anySubjectId = Any.Guid().NonEmpty().Generate();
        Role anyRole      = Any.Enum<Role>().Generate();

        // Act
        RoleAssignment assignment = new RoleAssignment(anySubjectId, anyRole, FirstDay, FirstDay);

        // Assert
        Assert.Equal(FirstDay, assignment.ValidUntil);
    }

    [Fact]
    public void A_role_cannot_be_granted_to_nobody() {
        // Act + Assert
        Assert.Throws<ArgumentException>(() => Any.RoleAssignment().WithSubjectId(Guid.Empty).Generate());
    }

    [Fact]
    public void A_role_that_is_not_declared_cannot_be_granted() {
        // Act + Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => Any.RoleAssignment().WithRole((Role)42).Generate());
    }

    [Fact]
    public void A_grant_cannot_end_before_it_starts() {
        // Act + Assert
        Assert.Throws<ArgumentException>(() => Any.RoleAssignment().WithValidFrom(FirstDay).WithValidUntil(FirstDay.AddDays(-1)).Generate());
    }

    [Fact]
    public void The_subject_is_checked_before_the_role() {
        // Act
        Exception? refusal = Record.Exception(() => new RoleAssignment(Guid.Empty, (Role)42, FirstDay, null));

        // Assert
        Assert.IsType<ArgumentException>(refusal);
    }

    [Fact]
    public void A_bounded_grant_is_active_from_its_first_day_to_its_last_day_both_included() {
        // Arrange
        RoleAssignment anyAssignment = Any.RoleAssignment().WithValidFrom(FirstDay).WithValidUntil(LastDay).Generate();

        // Act + Assert
        Assert.False(anyAssignment.IsActiveOn(FirstDay.AddDays(-1)));
        Assert.True(anyAssignment.IsActiveOn(FirstDay));
        Assert.True(anyAssignment.IsActiveOn(new DateOnly(2025, 3, 15)));
        Assert.True(anyAssignment.IsActiveOn(LastDay));
        Assert.False(anyAssignment.IsActiveOn(LastDay.AddDays(1)));
    }

    [Fact]
    public void A_grant_without_an_end_stays_active_however_late_the_day() {
        // Arrange
        RoleAssignment anyOpenEndedAssignment = Any.RoleAssignment().WithValidFrom(FirstDay).WithoutValidUntil().Generate();

        // Act + Assert
        Assert.False(anyOpenEndedAssignment.IsActiveOn(FirstDay.AddDays(-1)));
        Assert.True(anyOpenEndedAssignment.IsActiveOn(FirstDay));
        Assert.True(anyOpenEndedAssignment.IsActiveOn(DateOnly.MaxValue));
    }

    [Fact]
    public void A_one_day_grant_is_active_on_that_day_only() {
        // Arrange
        RoleAssignment anyOneDayAssignment = Any.RoleAssignment().WithValidFrom(FirstDay).WithValidUntil(FirstDay).Generate();

        // Act + Assert
        Assert.False(anyOneDayAssignment.IsActiveOn(FirstDay.AddDays(-1)));
        Assert.True(anyOneDayAssignment.IsActiveOn(FirstDay));
        Assert.False(anyOneDayAssignment.IsActiveOn(FirstDay.AddDays(1)));
    }

}
