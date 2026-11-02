// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate AircraftRegistration --entry-point any --force` overwrites it.
// It needs C# 14: a static extension member is what reaches this spelling without touching the library.

using JustDummies;

namespace Aviation.Domain;

/// <summary>Hangs <c>Any.AircraftRegistration()</c> off the library's own entry point.</summary>
public static class AnyAircraftRegistrationEntry {

    extension(Any) {

        /// <summary>Starts an arbitrary <c>AircraftRegistration</c>: constrain it through <c>With…</c>, then <c>Generate()</c>.</summary>
        public static AnyAircraftRegistration AircraftRegistration() {
            return new AnyAircraftRegistration();
        }

    }

}
