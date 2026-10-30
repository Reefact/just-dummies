// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate RoleAssignment --entry-point static:Arbitrary --force` overwrites it.
// The root is partial, so every other type's entry point lands in its own file beside this one.

namespace Identity.Domain;

/// <summary>Reaches this project's scaffolded generators through one root.</summary>
public static partial class Arbitrary {

    /// <summary>Starts an arbitrary <c>RoleAssignment</c>: constrain it through <c>With…</c>, then <c>Generate()</c>.</summary>
    public static AnyRoleAssignment RoleAssignment() {
        return new AnyRoleAssignment();
    }

}
