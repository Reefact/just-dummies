// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate RateSheet --entry-point any --force` overwrites it.
// It needs C# 14: a static extension member is what reaches this spelling without touching the library.

using JustDummies;

namespace FxSettlement.Domain.Tests.Regular;

/// <summary>Hangs <c>Any.RateSheet()</c> off the library's own entry point.</summary>
public static class AnyRateSheetEntry {

    extension(Any) {

        /// <summary>Starts an arbitrary <c>RateSheet</c>: constrain it through <c>With…</c>, then <c>Generate()</c>.</summary>
        public static AnyRateSheet RateSheet() {
            return new AnyRateSheet();
        }

    }

}
