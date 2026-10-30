// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate Leg --entry-point any --force` overwrites it.
// It needs C# 14: a static extension member is what reaches this spelling without touching the library.

using JustDummies;

namespace Repro.Domain;

/// <summary>Hangs <c>Any.Leg()</c> off the library's own entry point.</summary>
public static class AnyLegEntry {

    extension(Any) {

        /// <summary>Starts an arbitrary <c>Leg</c>: constrain it through <c>With…</c>, then <c>Generate()</c>.</summary>
        public static AnyLeg Leg() {
            return new AnyLeg();
        }

    }

}
