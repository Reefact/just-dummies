// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate BillOfMaterials --entry-point any --force` overwrites it.
// It needs C# 14: a static extension member is what reaches this spelling without touching the library.

using JustDummies;

namespace PassiveComponents.Domain;

/// <summary>Hangs <c>Any.BillOfMaterials()</c> off the library's own entry point.</summary>
public static class AnyBillOfMaterialsEntry {

    extension(Any) {

        /// <summary>Starts an arbitrary <c>BillOfMaterials</c>: constrain it through <c>With…</c>, then <c>Generate()</c>.</summary>
        public static AnyBillOfMaterials BillOfMaterials() {
            return new AnyBillOfMaterials();
        }

    }

}
