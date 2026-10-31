// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate Concentration --entry-point any --force` overwrites it.
// It needs C# 14: a static extension member is what reaches this spelling without touching the library.

using JustDummies;

namespace OrbitalMicrobiology.Domain;

/// <summary>Hangs <c>Any.Concentration()</c> off the library's own entry point.</summary>
public static class AnyConcentrationEntry {

    extension(Any) {

        /// <summary>Starts an arbitrary <c>Concentration</c>: constrain it through <c>With…</c>, then <c>Generate()</c>.</summary>
        public static AnyConcentration Concentration() {
            return new AnyConcentration();
        }

    }

}
