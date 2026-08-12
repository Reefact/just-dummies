// Scaffolded by dum (JustDummies). This file is yours: read it, edit it, commit it.
// `dum generate Order --entry-point any --force` overwrites it.
// It needs C# 14: a static extension member is what reaches this spelling without touching the library.

using JustDummies;

namespace Shop.Domain;

/// <summary>Hangs <c>Any.Order()</c> off the library's own entry point.</summary>
public static class AnyOrderEntry {

    extension(Any) {

        /// <summary>Starts an arbitrary <c>Order</c>: constrain it through <c>With…</c>, then <c>Generate()</c>.</summary>
        public static AnyOrder Order() {
            return new AnyOrder();
        }

    }

}
