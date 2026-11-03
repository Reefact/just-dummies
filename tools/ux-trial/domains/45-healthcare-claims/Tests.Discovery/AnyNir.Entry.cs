// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate Nir --entry-point any --force` overwrites it.
// It needs C# 14: a static extension member is what reaches this spelling without touching the library.

using JustDummies;

namespace Clinical.Domain;

/// <summary>Hangs <c>Any.Nir()</c> off the library's own entry point.</summary>
public static class AnyNirEntry {

    extension(Any) {

        /// <summary>Starts an arbitrary <c>Nir</c>: constrain it through <c>With…</c>, then <c>Generate()</c>.</summary>
        public static AnyNir Nir() {
            return new AnyNir();
        }

    }

}
