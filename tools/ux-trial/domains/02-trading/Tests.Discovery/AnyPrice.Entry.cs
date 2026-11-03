// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate Price --entry-point any --force` overwrites it.
// It needs C# 14: a static extension member is what reaches this spelling without touching the library.

using JustDummies;

namespace Trading.Domain;

/// <summary>Hangs <c>Any.Price()</c> off the library's own entry point.</summary>
public static class AnyPriceEntry {

    extension(Any) {

        /// <summary>Starts an arbitrary <c>Price</c>: constrain it through <c>With…</c>, then <c>Generate()</c>.</summary>
        public static AnyPrice Price() {
            return new AnyPrice();
        }

    }

}
