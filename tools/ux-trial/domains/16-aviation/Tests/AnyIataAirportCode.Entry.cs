// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate IataAirportCode --entry-point any --force` overwrites it.
// It needs C# 14: a static extension member is what reaches this spelling without touching the library.

using JustDummies;

namespace Aviation.Domain;

/// <summary>Hangs <c>Any.IataAirportCode()</c> off the library's own entry point.</summary>
public static class AnyIataAirportCodeEntry {

    extension(Any) {

        /// <summary>Starts an arbitrary <c>IataAirportCode</c>: constrain it through <c>With…</c>, then <c>Generate()</c>.</summary>
        public static AnyIataAirportCode IataAirportCode() {
            return new AnyIataAirportCode();
        }

    }

}
