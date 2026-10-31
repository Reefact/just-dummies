// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate Lot --entry-point any --force` overwrites it.
// It needs C# 14: a static extension member is what reaches this spelling without touching the library.

using JustDummies;

namespace BatchGenealogy.Domain;

/// <summary>Hangs <c>Any.Lot()</c> off the library's own entry point.</summary>
public static class AnyLotEntry {

    extension(Any) {

        /// <summary>Starts an arbitrary <c>Lot</c>: constrain it through <c>With…</c>, then <c>Generate()</c>.</summary>
        public static AnyLot Lot() {
            return new AnyLot();
        }

    }

}
