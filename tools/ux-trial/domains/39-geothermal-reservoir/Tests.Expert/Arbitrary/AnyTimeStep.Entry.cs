// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate TimeStep --entry-point any --force` overwrites it.
// It needs C# 14: a static extension member is what reaches this spelling without touching the library.

using JustDummies;

namespace GeothermalReservoir.Tests.Expert.Arbitrary;

/// <summary>Hangs <c>Any.TimeStep()</c> off the library's own entry point.</summary>
public static class AnyTimeStepEntry {

    extension(Any) {

        /// <summary>Starts an arbitrary <c>TimeStep</c>: constrain it through <c>With…</c>, then <c>Generate()</c>.</summary>
        public static AnyTimeStep TimeStep() {
            return new AnyTimeStep();
        }

    }

}
