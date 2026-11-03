// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate TransferRecord --entry-point any --force` overwrites it.
// It needs C# 14: a static extension member is what reaches this spelling without touching the library.

using JustDummies;

namespace TokenLedger.Domain;

/// <summary>Hangs <c>Any.TransferRecord()</c> off the library's own entry point.</summary>
public static class AnyTransferRecordEntry {

    extension(Any) {

        /// <summary>Starts an arbitrary <c>TransferRecord</c>: constrain it through <c>With…</c>, then <c>Generate()</c>.</summary>
        public static AnyTransferRecord TransferRecord() {
            return new AnyTransferRecord();
        }

    }

}
