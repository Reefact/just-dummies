// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate Driver --entry-point any --force` overwrites it.
// It needs C# 14: a static extension member is what reaches this spelling without touching the library.

using JustDummies;

namespace Insurance.Domain;

/// <summary>Hangs <c>Any.Driver()</c> off the library's own entry point.</summary>
public static class AnyDriverEntry {

    extension(Any) {

        /// <summary>Starts an arbitrary <c>Driver</c>: constrain it through <c>With…</c>, then <c>Generate()</c>.</summary>
        public static AnyDriver Driver() {
            return new AnyDriver();
        }

    }

}
