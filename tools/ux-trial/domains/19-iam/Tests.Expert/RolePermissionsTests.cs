using Xunit;

namespace Identity.Domain.Tests.Expert;

public sealed class RolePermissionsTests {

    [Theory]
    [InlineData(Role.Reader,        Permission.Read)]
    [InlineData(Role.Contributor,   Permission.Read | Permission.Write)]
    [InlineData(Role.Administrator, Permission.Read | Permission.Write | Permission.Delete)]
    [InlineData(Role.Owner,         Permission.Read | Permission.Write | Permission.Delete | Permission.Grant)]
    public void A_role_grants_exactly_its_permissions(Role role, Permission expected) {
        // Act
        Permission granted = RolePermissions.Of(role);

        // Assert
        Assert.Equal(expected, granted);
    }

    [Theory]
    [InlineData(Role.Reader,        Permission.Write)]
    [InlineData(Role.Reader,        Permission.Grant)]
    [InlineData(Role.Contributor,   Permission.Delete)]
    [InlineData(Role.Administrator, Permission.Grant)]
    public void A_role_does_not_grant_what_the_next_one_adds(Role role, Permission missing) {
        // Act + Assert
        Assert.False(RolePermissions.Of(role).HasFlag(missing));
    }

    [Fact]
    public void A_role_that_is_not_declared_has_no_permissions_to_give() {
        // Act + Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => RolePermissions.Of((Role)42));
        Assert.Throws<ArgumentOutOfRangeException>(() => RolePermissions.Of((Role)(-1)));
    }

}
