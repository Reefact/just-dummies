// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate MicrobialPopulation --entry-point any --force` overwrites it.
// It needs C# 14: a static extension member is what reaches this spelling without touching the library.

using JustDummies;

namespace OrbitalMicrobiology.Tests.Expert;

/// <summary>Hangs <c>Any.MicrobialPopulation()</c> off the library's own entry point.</summary>
public static class AnyMicrobialPopulationEntry {

    extension(Any) {

        /// <summary>Starts an arbitrary <c>MicrobialPopulation</c>: constrain it through <c>With…</c>, then <c>Generate()</c>.</summary>
        public static AnyMicrobialPopulation MicrobialPopulation() {
            return new AnyMicrobialPopulation();
        }

    }

}
