// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate FeePolicy --entry-point any --force` overwrites it.
// It needs C# 14: a static extension member is what reaches this spelling without touching the library.

using JustDummies;

namespace Payments.Domain;

/// <summary>Hangs <c>Any.FeePolicy()</c> off the library's own entry point.</summary>
public static class AnyFeePolicyEntry {

    extension(Any) {

        /// <summary>Starts an arbitrary <c>FeePolicy</c>: constrain it through <c>With…</c>, then <c>Generate()</c>.</summary>
        public static AnyFeePolicy FeePolicy() {
            return new AnyFeePolicy();
        }

    }

}
