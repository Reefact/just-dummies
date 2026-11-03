// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate Witness --entry-point any --force` overwrites it.
// It needs C# 14: a static extension member is what reaches this spelling without touching the library.

using JustDummies;

namespace ManuscriptReconstruction.Domain;

/// <summary>Hangs <c>Any.Witness()</c> off the library's own entry point.</summary>
public static class AnyWitnessEntry {

    extension(Any) {

        /// <summary>Starts an arbitrary <c>Witness</c>: constrain it through <c>With…</c>, then <c>Generate()</c>.</summary>
        public static AnyWitness Witness() {
            return new AnyWitness();
        }

    }

}
