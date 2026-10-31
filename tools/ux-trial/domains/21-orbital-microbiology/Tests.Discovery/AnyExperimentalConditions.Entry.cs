// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate ExperimentalConditions --entry-point any --force` overwrites it.
// It needs C# 14: a static extension member is what reaches this spelling without touching the library.

using JustDummies;

namespace OrbitalMicrobiology.Domain;

/// <summary>Hangs <c>Any.ExperimentalConditions()</c> off the library's own entry point.</summary>
public static class AnyExperimentalConditionsEntry {

    extension(Any) {

        /// <summary>Starts an arbitrary <c>ExperimentalConditions</c>: constrain it through <c>With…</c>, then <c>Generate()</c>.</summary>
        public static AnyExperimentalConditions ExperimentalConditions() {
            return new AnyExperimentalConditions();
        }

    }

}
