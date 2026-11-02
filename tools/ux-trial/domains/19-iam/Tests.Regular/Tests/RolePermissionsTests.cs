using Identity.Domain;

using JustDummies;

using Xunit;

namespace Identity.Tests.Regular;

public sealed class RolePermissionsTests {

    [Theory]
    [InlineData(Role.Reader, Permission.Read)]
    [InlineData(Role.Contributor, Permission.Read | Permission.Write)]
    [InlineData(Role.Administrator, Permission.Read | Permission.Write | Permission.Delete)]
    [InlineData(Role.Owner, Permission.Read | Permission.Write | Permission.Delete | Permission.Grant)]
    public void Each_role_grants_its_permissions(Role role, Permission expected) {
        // Act
        Permission permissions = RolePermissions.Of(role);

        // Assert
        Assert.Equal(expected, permissions);
    }

    [Fact]
    public void Only_the_owner_may_grant() {
        // Arrange
        Role[] others = [Role.Reader, Role.Contributor, Role.Administrator];

        // Act + Assert
        Assert.True(RolePermissions.Of(Role.Owner).HasFlag(Permission.Grant));
        Assert.All(others, other => Assert.False(RolePermissions.Of(other).HasFlag(Permission.Grant)));
    }

    [Fact]
    public void An_undefined_role_is_refused() {
        // Arrange
        Role anyUndefinedRole = (Role)Any.Int32().GreaterThan(3).Generate();

        // Act
        ArgumentOutOfRangeException thrown = Assert.Throws<ArgumentOutOfRangeException>(() => RolePermissions.Of(anyUndefinedRole));

        // Assert
        Assert.Equal("role", thrown.ParamName);
    }

    [Fact]
    public void A_negative_role_value_is_refused() {
        // Arrange
        Role anyNegativeRole = (Role)Any.Int32().Negative().Generate();

        // Act + Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => RolePermissions.Of(anyNegativeRole));
    }

}
