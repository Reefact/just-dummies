// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate Ticker --entry-point any --force` overwrites it.
// It needs C# 14: a static extension member is what reaches this spelling without touching the library.

using JustDummies;

namespace Trading.Domain;

/// <summary>Hangs <c>Any.Ticker()</c> off the library's own entry point.</summary>
public static class AnyTickerEntry {

    extension(Any) {

        /// <summary>Starts an arbitrary <c>Ticker</c>: constrain it through <c>With…</c>, then <c>Generate()</c>.</summary>
        public static AnyTicker Ticker() {
            return new AnyTicker();
        }

    }

}
