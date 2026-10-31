// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate PortCode --entry-point any --force` overwrites it.
// It needs C# 14: a static extension member is what reaches this spelling without touching the library.

using JustDummies;

namespace VesselStowage.Domain;

/// <summary>Hangs <c>Any.PortCode()</c> off the library's own entry point.</summary>
public static class AnyPortCodeEntry {

    extension(Any) {

        /// <summary>Starts an arbitrary <c>PortCode</c>: constrain it through <c>With…</c>, then <c>Generate()</c>.</summary>
        public static AnyPortCode PortCode() {
            return new AnyPortCode();
        }

    }

}
