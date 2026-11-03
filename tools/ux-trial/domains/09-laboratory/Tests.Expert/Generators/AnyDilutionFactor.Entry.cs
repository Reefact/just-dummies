// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate DilutionFactor --entry-point any --force` overwrites it.
// It needs C# 14: a static extension member is what reaches this spelling without touching the library.

using JustDummies;

namespace Laboratory.Domain;

/// <summary>Hangs <c>Any.DilutionFactor()</c> off the library's own entry point.</summary>
public static class AnyDilutionFactorEntry {

    extension(Any) {

        /// <summary>Starts an arbitrary <c>DilutionFactor</c>: constrain it through <c>With…</c>, then <c>Generate()</c>.</summary>
        public static AnyDilutionFactor DilutionFactor() {
            return new AnyDilutionFactor();
        }

    }

}
