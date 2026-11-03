// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate Volume --entry-point any --force` overwrites it.
// It needs C# 14: a static extension member is what reaches this spelling without touching the library.

using JustDummies;

namespace Freight.Domain;

/// <summary>Hangs <c>Any.Volume()</c> off the library's own entry point.</summary>
public static class AnyVolumeEntry {

    extension(Any) {

        /// <summary>Starts an arbitrary <c>Volume</c>: constrain it through <c>With…</c>, then <c>Generate()</c>.</summary>
        public static AnyVolume Volume() {
            return new AnyVolume();
        }

    }

}
