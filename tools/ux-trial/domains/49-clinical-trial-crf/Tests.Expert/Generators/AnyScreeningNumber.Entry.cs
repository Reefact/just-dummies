// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate ScreeningNumber --entry-point any --force` overwrites it.
// It needs C# 14: a static extension member is what reaches this spelling without touching the library.

using JustDummies;

namespace ClinicalTrial.Domain;

/// <summary>Hangs <c>Any.ScreeningNumber()</c> off the library's own entry point.</summary>
public static class AnyScreeningNumberEntry {

    extension(Any) {

        /// <summary>Starts an arbitrary <c>ScreeningNumber</c>: constrain it through <c>With…</c>, then <c>Generate()</c>.</summary>
        public static AnyScreeningNumber ScreeningNumber() {
            return new AnyScreeningNumber();
        }

    }

}
