// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate Quantity --entry-point any --force` overwrites it.
// It needs C# 14: a static extension member is what reaches this spelling without touching the library.

using JustDummies;

namespace Shop.Tests;

/// <summary>Hangs <c>Any.Quantity()</c> off the library's own entry point.</summary>
public static class AnyQuantityEntry {

    extension(Any) {

        /// <summary>Starts an arbitrary <c>Quantity</c>: constrain it through <c>With…</c>, then <c>Generate()</c>.</summary>
        public static AnyQuantity Quantity() {
            return new AnyQuantity();
        }

    }

}
