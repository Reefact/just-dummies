// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate SolverSettings --entry-point any --force` overwrites it.
// It needs C# 14: a static extension member is what reaches this spelling without touching the library.

using JustDummies;

namespace GridLoadFlow.Domain;

/// <summary>Hangs <c>Any.SolverSettings()</c> off the library's own entry point.</summary>
public static class AnySolverSettingsEntry {

    extension(Any) {

        /// <summary>Starts an arbitrary <c>SolverSettings</c>: constrain it through <c>With…</c>, then <c>Generate()</c>.</summary>
        public static AnySolverSettings SolverSettings() {
            return new AnySolverSettings();
        }

    }

}
