// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate Dividend --entry-point any --force` overwrites it.
// It needs C# 14: a static extension member is what reaches this spelling without touching the library.

using JustDummies;

namespace ParimutuelBetting.Domain;

/// <summary>Hangs <c>Any.Dividend()</c> off the library's own entry point.</summary>
public static class AnyDividendEntry {

    extension(Any) {

        /// <summary>Starts an arbitrary <c>Dividend</c>: constrain it through <c>With…</c>, then <c>Generate()</c>.</summary>
        public static AnyDividend Dividend() {
            return new AnyDividend();
        }

    }

}
