// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate PromoCode --entry-point any --force` overwrites it.
// It needs C# 14: a static extension member is what reaches this spelling without touching the library.

using JustDummies;

namespace Shop.Tests;

/// <summary>Hangs <c>Any.PromoCode()</c> off the library's own entry point.</summary>
public static class AnyPromoCodeEntry {

    extension(Any) {

        /// <summary>Starts an arbitrary <c>PromoCode</c>: constrain it through <c>With…</c>, then <c>Generate()</c>.</summary>
        public static AnyPromoCode PromoCode() {
            return new AnyPromoCode();
        }

    }

}
