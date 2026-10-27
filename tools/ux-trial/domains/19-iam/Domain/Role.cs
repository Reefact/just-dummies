namespace Identity.Domain;

public enum Role {
    Reader,
    Contributor,
    Administrator,
    Owner,
}

[Flags]
public enum Permission {
    None   = 0,
    Read   = 1,
    Write  = 2,
    Delete = 4,
    Grant  = 8,
}

public static class RolePermissions {

    public static Permission Of(Role role) => role switch {
        Role.Reader        => Permission.Read,
        Role.Contributor   => Permission.Read | Permission.Write,
        Role.Administrator => Permission.Read | Permission.Write | Permission.Delete,
        Role.Owner         => Permission.Read | Permission.Write | Permission.Delete | Permission.Grant,
        _                  => throw new ArgumentOutOfRangeException(nameof(role), role, "Unknown role."),
    };

}
