using Identity.Domain;
using JustDummies;
using JustDummies.Xunit;
using Xunit;

namespace Identity.Tests;

[Reproducible]
public sealed class RoleTests {

    private static readonly DateOnly GrantedOn = new DateOnly(2025, 3, 1);

    [Fact]
    public void An_open_ended_grant_is_still_active_years_later() {
        // Arrange
        RoleAssignment anyGrant = Arbitrary.RoleAssignment().WithValidFrom(GrantedOn).WithValidUntil((DateOnly?)null).Generate();

        // Act
        bool active = anyGrant.IsActiveOn(new DateOnly(2040, 1, 1));

        // Assert
        Assert.True(active);
    }

    [Fact]
    public void A_grant_is_not_active_the_day_before_it_starts() {
        // Arrange
        RoleAssignment anyGrant = Arbitrary.RoleAssignment().WithValidFrom(GrantedOn).Generate();

        // Act
        bool active = anyGrant.IsActiveOn(GrantedOn.AddDays(-1));

        // Assert
        Assert.False(active);
    }

    [Fact]
    public void A_grant_cannot_end_before_it_starts() {
        // Arrange
        Guid anySubject = Any.Guid().NonEmpty().Generate();
        Role anyRole    = Any.Enum<Role>().Generate();

        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => new RoleAssignment(anySubject, anyRole, GrantedOn, GrantedOn.AddDays(-1)));

        // Assert
        Assert.Equal("validUntil", refusal.ParamName);
    }

    [Theory]
    [InlineData(Role.Reader)]
    [InlineData(Role.Contributor)]
    [InlineData(Role.Administrator)]
    public void Only_the_owner_may_grant_roles(Role role) {
        // Act
        Permission permissions = RolePermissions.Of(role);

        // Assert
        Assert.False(permissions.HasFlag(Permission.Grant));
    }

    [Fact]
    public void The_owner_holds_every_permission() {
        // Act
        Permission permissions = RolePermissions.Of(Role.Owner);

        // Assert
        Assert.Equal(Permission.Read | Permission.Write | Permission.Delete | Permission.Grant, permissions);
    }

}
