// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate RouteLock --entry-point any --force` overwrites it.
// It needs C# 14: a static extension member is what reaches this spelling without touching the library.

using JustDummies;

namespace RailwayInterlocking.Domain;

/// <summary>Hangs <c>Any.RouteLock()</c> off the library's own entry point.</summary>
public static class AnyRouteLockEntry {

    extension(Any) {

        /// <summary>Starts an arbitrary <c>RouteLock</c>: constrain it through <c>With…</c>, then <c>Generate()</c>.</summary>
        public static AnyRouteLock RouteLock() {
            return new AnyRouteLock();
        }

    }

}
