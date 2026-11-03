// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate Freight.Domain.Shipment --entry-point any --force` overwrites it.
// It needs C# 14: a static extension member is what reaches this spelling without touching the library.

using JustDummies;

namespace ColdChain.Tests.Expert.Generators;

/// <summary>Hangs <c>Any.Shipment()</c> off the library's own entry point.</summary>
public static class AnyShipmentEntry {

    extension(Any) {

        /// <summary>Starts an arbitrary <c>Shipment</c>: constrain it through <c>With…</c>, then <c>Generate()</c>.</summary>
        public static AnyShipment Shipment() {
            return new AnyShipment();
        }

    }

}
