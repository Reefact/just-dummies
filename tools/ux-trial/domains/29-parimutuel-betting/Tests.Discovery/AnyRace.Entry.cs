// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate Race --entry-point any --force` overwrites it.
// It needs C# 14: a static extension member is what reaches this spelling without touching the library.

using JustDummies;

namespace ParimutuelBetting.Domain;

/// <summary>Hangs <c>Any.Race()</c> off the library's own entry point.</summary>
public static class AnyRaceEntry {

    extension(Any) {

        /// <summary>Starts an arbitrary <c>Race</c>: constrain it through <c>With…</c>, then <c>Generate()</c>.</summary>
        public static AnyRace Race() {
            return new AnyRace();
        }

    }

}
