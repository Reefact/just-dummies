// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate BusId --entry-point any --force` overwrites it.
// It needs C# 14: a static extension member is what reaches this spelling without touching the library.

using JustDummies;

namespace GridLoadFlow.Domain;

/// <summary>Hangs <c>Any.BusId()</c> off the library's own entry point.</summary>
public static class AnyBusIdEntry {

    extension(Any) {

        /// <summary>Starts an arbitrary <c>BusId</c>: constrain it through <c>With…</c>, then <c>Generate()</c>.</summary>
        public static AnyBusId BusId() {
            return new AnyBusId();
        }

    }

}
