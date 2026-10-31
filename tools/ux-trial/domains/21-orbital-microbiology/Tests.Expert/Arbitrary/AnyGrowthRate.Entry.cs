// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate GrowthRate --entry-point any --force` overwrites it.
// It needs C# 14: a static extension member is what reaches this spelling without touching the library.

using JustDummies;

namespace OrbitalMicrobiology.Tests.Expert.Arbitrary;

/// <summary>Hangs <c>Any.GrowthRate()</c> off the library's own entry point.</summary>
public static class AnyGrowthRateEntry {

    extension(Any) {

        /// <summary>Starts an arbitrary <c>GrowthRate</c>: constrain it through <c>With…</c>, then <c>Generate()</c>.</summary>
        public static AnyGrowthRate GrowthRate() {
            return new AnyGrowthRate();
        }

    }

}
