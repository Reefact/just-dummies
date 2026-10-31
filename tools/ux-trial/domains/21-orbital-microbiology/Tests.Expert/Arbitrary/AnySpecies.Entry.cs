// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate Species --entry-point any --force` overwrites it.
// It needs C# 14: a static extension member is what reaches this spelling without touching the library.

using JustDummies;

namespace OrbitalMicrobiology.Tests.Expert.Arbitrary;

/// <summary>Hangs <c>Any.Species()</c> off the library's own entry point.</summary>
public static class AnySpeciesEntry {

    extension(Any) {

        /// <summary>Starts an arbitrary <c>Species</c>: constrain it through <c>With…</c>, then <c>Generate()</c>.</summary>
        public static AnySpecies Species() {
            return new AnySpecies();
        }

    }

}
