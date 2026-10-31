// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate BusId --entry-point static:Arbitrary --force` overwrites it.
// The root is partial, so every other type's entry point lands in its own file beside this one.

namespace GridLoadFlow.Domain;

/// <summary>Reaches this project's scaffolded generators through one root.</summary>
public static partial class Arbitrary {

    /// <summary>Starts an arbitrary <c>BusId</c>: constrain it through <c>With…</c>, then <c>Generate()</c>.</summary>
    public static AnyBusId BusId() {
        return new AnyBusId();
    }

}
