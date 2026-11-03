// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate Address --entry-point any --force` overwrites it.
// It needs C# 14: a static extension member is what reaches this spelling without touching the library.

using JustDummies;

namespace TokenLedger.Domain;

/// <summary>Hangs <c>Any.Address()</c> off the library's own entry point.</summary>
public static class AnyAddressEntry {

    extension(Any) {

        /// <summary>Starts an arbitrary <c>Address</c>: constrain it through <c>With…</c>, then <c>Generate()</c>.</summary>
        public static AnyAddress Address() {
            return new AnyAddress();
        }

    }

}
