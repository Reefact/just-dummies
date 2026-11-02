// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate Route --entry-point any --force` overwrites it.
// It needs C# 14: a static extension member is what reaches this spelling without touching the library.

using JustDummies;

namespace Aviation.Domain;

/// <summary>Hangs <c>Any.Route()</c> off the library's own entry point.</summary>
public static class AnyRouteEntry {

    extension(Any) {

        /// <summary>Starts an arbitrary <c>Route</c>: constrain it through <c>With…</c>, then <c>Generate()</c>.</summary>
        public static AnyRoute Route() {
            return new AnyRoute();
        }

    }

}
