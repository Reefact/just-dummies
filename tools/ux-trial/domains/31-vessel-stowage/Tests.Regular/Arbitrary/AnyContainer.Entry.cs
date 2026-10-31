// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate VesselStowage.Domain.Container --entry-point static:Arbitrary --force` overwrites it.
// The root is partial, so every other type's entry point lands in its own file beside this one.

namespace VesselStowage.Domain;

/// <summary>Reaches this project's scaffolded generators through one root.</summary>
public static partial class Arbitrary {

    /// <summary>Starts an arbitrary <c>Container</c>: constrain it through <c>With…</c>, then <c>Generate()</c>.</summary>
    public static AnyContainer Container() {
        return new AnyContainer();
    }

}
