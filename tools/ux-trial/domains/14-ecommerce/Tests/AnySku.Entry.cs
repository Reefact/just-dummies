// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate Sku --entry-point any --force` overwrites it.
// It needs C# 14: a static extension member is what reaches this spelling without touching the library.

using JustDummies;

namespace Shop.Tests;

/// <summary>Hangs <c>Any.Sku()</c> off the library's own entry point.</summary>
public static class AnySkuEntry {

    extension(Any) {

        /// <summary>Starts an arbitrary <c>Sku</c>: constrain it through <c>With…</c>, then <c>Generate()</c>.</summary>
        public static AnySku Sku() {
            return new AnySku();
        }

    }

}
