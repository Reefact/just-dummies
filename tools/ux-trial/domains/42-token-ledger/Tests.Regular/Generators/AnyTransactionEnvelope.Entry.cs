// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate TransactionEnvelope --entry-point any --force` overwrites it.
// It needs C# 14: a static extension member is what reaches this spelling without touching the library.

using JustDummies;

namespace TokenLedger.Domain;

/// <summary>Hangs <c>Any.TransactionEnvelope()</c> off the library's own entry point.</summary>
public static class AnyTransactionEnvelopeEntry {

    extension(Any) {

        /// <summary>Starts an arbitrary <c>TransactionEnvelope</c>: constrain it through <c>With…</c>, then <c>Generate()</c>.</summary>
        public static AnyTransactionEnvelope TransactionEnvelope() {
            return new AnyTransactionEnvelope();
        }

    }

}
