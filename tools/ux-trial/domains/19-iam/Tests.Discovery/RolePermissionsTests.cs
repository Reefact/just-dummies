using Identity.Domain;

using JustDummies;

using Xunit;

namespace Identity.Tests.Discovery;

public sealed class RolePermissionsTests {

    [Theory]
    [InlineData(Role.Reader, Permission.Read)]
    [InlineData(Role.Contributor, Permission.Read | Permission.Write)]
    [InlineData(Role.Administrator, Permission.Read | Permission.Write | Permission.Delete)]
    [InlineData(Role.Owner, Permission.Read | Permission.Write | Permission.Delete | Permission.Grant)]
    public void Each_role_grants_its_permissions(Role role, Permission expected) {
        // Act
        Permission granted = RolePermissions.Of(role);

        // Assert
        Assert.Equal(expected, granted);
    }

    [Fact]
    public void Only_the_owner_may_grant() {
        // Assert
        Assert.False(RolePermissions.Of(Role.Reader).HasFlag(Permission.Grant));
        Assert.False(RolePermissions.Of(Role.Contributor).HasFlag(Permission.Grant));
        Assert.False(RolePermissions.Of(Role.Administrator).HasFlag(Permission.Grant));
        Assert.True(RolePermissions.Of(Role.Owner).HasFlag(Permission.Grant));
    }

    [Fact]
    public void No_role_grants_nothing() {
        // Arrange
        Role[] roles = Enum.GetValues<Role>();

        // Assert
        Assert.All(roles, role => Assert.NotEqual(Permission.None, RolePermissions.Of(role)));
    }

    [Fact]
    public void A_value_that_is_not_a_declared_role_is_refused() {
        // Arrange
        Role anyUndeclared = (Role)Any.Int32().Except(0, 1, 2, 3).Generate();

        // Act
        ArgumentOutOfRangeException exception = Assert.Throws<ArgumentOutOfRangeException>(() => RolePermissions.Of(anyUndeclared));

        // Assert
        Assert.Equal("role", exception.ParamName);
    }

}
