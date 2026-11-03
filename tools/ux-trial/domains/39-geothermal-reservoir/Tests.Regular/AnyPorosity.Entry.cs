// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate Porosity --entry-point any --force` overwrites it.
// It needs C# 14: a static extension member is what reaches this spelling without touching the library.

using JustDummies;

namespace GeothermalReservoir.Domain;

/// <summary>Hangs <c>Any.Porosity()</c> off the library's own entry point.</summary>
public static class AnyPorosityEntry {

    extension(Any) {

        /// <summary>Starts an arbitrary <c>Porosity</c>: constrain it through <c>With…</c>, then <c>Generate()</c>.</summary>
        public static AnyPorosity Porosity() {
            return new AnyPorosity();
        }

    }

}
