// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate Excursion --entry-point any --force` overwrites it.
// It needs C# 14: a static extension member is what reaches this spelling without touching the library.

using JustDummies;

namespace ColdChain.Tests.Expert.Generators;

/// <summary>Hangs <c>Any.Excursion()</c> off the library's own entry point.</summary>
public static class AnyExcursionEntry {

    extension(Any) {

        /// <summary>Starts an arbitrary <c>Excursion</c>: constrain it through <c>With…</c>, then <c>Generate()</c>.</summary>
        public static AnyExcursion Excursion() {
            return new AnyExcursion();
        }

    }

}
