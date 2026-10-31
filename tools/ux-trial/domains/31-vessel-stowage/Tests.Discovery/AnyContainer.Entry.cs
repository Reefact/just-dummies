// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate VesselStowage.Domain.Container --entry-point any --force` overwrites it.
// It needs C# 14: a static extension member is what reaches this spelling without touching the library.

using JustDummies;

namespace VesselStowage.Domain;

/// <summary>Hangs <c>Any.Container()</c> off the library's own entry point.</summary>
public static class AnyContainerEntry {

    extension(Any) {

        /// <summary>Starts an arbitrary <c>Container</c>: constrain it through <c>With…</c>, then <c>Generate()</c>.</summary>
        public static AnyContainer Container() {
            return new AnyContainer();
        }

    }

}
