// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate BoundaryCondition --entry-point any --force` overwrites it.
// It needs C# 14: a static extension member is what reaches this spelling without touching the library.

using JustDummies;

namespace GeothermalReservoir.Tests.Expert.Arbitrary;

/// <summary>Hangs <c>Any.BoundaryCondition()</c> off the library's own entry point.</summary>
public static class AnyBoundaryConditionEntry {

    extension(Any) {

        /// <summary>Starts an arbitrary <c>BoundaryCondition</c>: constrain it through <c>With…</c>, then <c>Generate()</c>.</summary>
        public static AnyBoundaryCondition BoundaryCondition() {
            return new AnyBoundaryCondition();
        }

    }

}
