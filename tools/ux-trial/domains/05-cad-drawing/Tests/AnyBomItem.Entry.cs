// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate BomItem --entry-point any --force` overwrites it.
// It needs C# 14: a static extension member is what reaches this spelling without touching the library.

using JustDummies;

namespace Drafting.Domain;

/// <summary>Hangs <c>Any.BomItem()</c> off the library's own entry point.</summary>
public static class AnyBomItemEntry {

    extension(Any) {

        /// <summary>Starts an arbitrary <c>BomItem</c>: constrain it through <c>With…</c>, then <c>Generate()</c>.</summary>
        public static AnyBomItem BomItem() {
            return new AnyBomItem();
        }

    }

}
