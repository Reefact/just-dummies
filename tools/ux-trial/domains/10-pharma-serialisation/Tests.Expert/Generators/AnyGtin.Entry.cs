// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate Gtin --entry-point any --force` overwrites it.
// It needs C# 14: a static extension member is what reaches this spelling without touching the library.

using JustDummies;

namespace Serialisation.Domain;

/// <summary>Hangs <c>Any.Gtin()</c> off the library's own entry point.</summary>
public static class AnyGtinEntry {

    extension(Any) {

        /// <summary>Starts an arbitrary <c>Gtin</c>: constrain it through <c>With…</c>, then <c>Generate()</c>.</summary>
        public static AnyGtin Gtin() {
            return new AnyGtin();
        }

    }

}
