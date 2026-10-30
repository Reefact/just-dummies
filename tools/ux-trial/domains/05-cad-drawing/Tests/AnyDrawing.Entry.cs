// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate Drawing --entry-point static:Arbitrary --force` overwrites it.
// The root is partial, so every other type's entry point lands in its own file beside this one.

namespace Drafting.Domain;

/// <summary>Reaches this project's scaffolded generators through one root.</summary>
public static partial class Arbitrary {

    /// <summary>Starts an arbitrary <c>Drawing</c>: constrain it through <c>With…</c>, then <c>Generate()</c>.</summary>
    public static AnyDrawing Drawing() {
        return new AnyDrawing();
    }

}
