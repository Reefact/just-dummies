// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate OrderLine --entry-point any --force` overwrites it.
// It needs C# 14: a static extension member is what reaches this spelling without touching the library.

using JustDummies;

namespace Shop.Tests;

/// <summary>Hangs <c>Any.OrderLine()</c> off the library's own entry point.</summary>
public static class AnyOrderLineEntry {

    extension(Any) {

        /// <summary>Starts an arbitrary <c>OrderLine</c>: constrain it through <c>With…</c>, then <c>Generate()</c>.</summary>
        public static AnyOrderLine OrderLine() {
            return new AnyOrderLine();
        }

    }

}
