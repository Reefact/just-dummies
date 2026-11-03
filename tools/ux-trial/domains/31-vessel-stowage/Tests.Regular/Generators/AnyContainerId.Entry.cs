// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate ContainerId --entry-point any --force` overwrites it.
// It needs C# 14: a static extension member is what reaches this spelling without touching the library.

using JustDummies;

namespace VesselStowage.Domain;

/// <summary>Hangs <c>Any.ContainerId()</c> off the library's own entry point.</summary>
public static class AnyContainerIdEntry {

    extension(Any) {

        /// <summary>Starts an arbitrary <c>ContainerId</c>: constrain it through <c>With…</c>, then <c>Generate()</c>.</summary>
        public static AnyContainerId ContainerId() {
            return new AnyContainerId();
        }

    }

}
