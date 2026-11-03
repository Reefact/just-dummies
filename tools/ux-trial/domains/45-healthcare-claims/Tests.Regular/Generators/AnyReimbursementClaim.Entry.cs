// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate ReimbursementClaim --entry-point any --force` overwrites it.
// It needs C# 14: a static extension member is what reaches this spelling without touching the library.

using JustDummies;

namespace HealthcareClaims.Tests.Regular.Generators;

/// <summary>Hangs <c>Any.ReimbursementClaim()</c> off the library's own entry point.</summary>
public static class AnyReimbursementClaimEntry {

    extension(Any) {

        /// <summary>Starts an arbitrary <c>ReimbursementClaim</c>: constrain it through <c>With…</c>, then <c>Generate()</c>.</summary>
        public static AnyReimbursementClaim ReimbursementClaim() {
            return new AnyReimbursementClaim();
        }

    }

}
