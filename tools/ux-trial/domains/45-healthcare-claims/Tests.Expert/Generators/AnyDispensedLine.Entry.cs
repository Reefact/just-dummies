// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate DispensedLine --entry-point any --force` overwrites it.
// It needs C# 14: a static extension member is what reaches this spelling without touching the library.

using JustDummies;

namespace HealthcareClaims.Domain;

/// <summary>Hangs <c>Any.DispensedLine()</c> off the library's own entry point.</summary>
public static class AnyDispensedLineEntry {

    extension(Any) {

        /// <summary>Starts an arbitrary <c>DispensedLine</c>: constrain it through <c>With…</c>, then <c>Generate()</c>.</summary>
        public static AnyDispensedLine DispensedLine() {
            return new AnyDispensedLine();
        }

    }

}
