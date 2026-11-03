// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate BinLocation --entry-point any --force` overwrites it.
// It needs C# 14: a static extension member is what reaches this spelling without touching the library.

using JustDummies;

namespace Warehouse.Domain.Tests.Generators;

/// <summary>Hangs <c>Any.BinLocation()</c> off the library's own entry point.</summary>
public static class AnyBinLocationEntry {

    extension(Any) {

        /// <summary>Starts an arbitrary <c>BinLocation</c>: constrain it through <c>With…</c>, then <c>Generate()</c>.</summary>
        public static AnyBinLocation BinLocation() {
            return new AnyBinLocation();
        }

    }

}
