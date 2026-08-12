// Scaffolded by dum (JustDummies). This file is yours: read it, edit it, commit it.
// `dum generate Order --entry-point static:Dummies --force` overwrites it.
// The root is partial, so every other type's entry point lands in its own file beside this one.

namespace Shop.Domain;

/// <summary>Reaches this project's scaffolded generators through one root.</summary>
public static partial class Dummies {

    /// <summary>Starts an arbitrary <c>Order</c>: constrain it through <c>With…</c>, then <c>Generate()</c>.</summary>
    public static AnyOrder Order() {
        return new AnyOrder();
    }

}
