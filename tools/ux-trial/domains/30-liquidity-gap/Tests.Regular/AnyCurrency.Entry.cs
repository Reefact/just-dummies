// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate Currency --entry-point any --force` overwrites it.
// It needs C# 14: a static extension member is what reaches this spelling without touching the library.

using JustDummies;

namespace LiquidityGap.Domain;

/// <summary>Hangs <c>Any.Currency()</c> off the library's own entry point.</summary>
public static class AnyCurrencyEntry {

    extension(Any) {

        /// <summary>Starts an arbitrary <c>Currency</c>: constrain it through <c>With…</c>, then <c>Generate()</c>.</summary>
        public static AnyCurrency Currency() {
            return new AnyCurrency();
        }

    }

}
