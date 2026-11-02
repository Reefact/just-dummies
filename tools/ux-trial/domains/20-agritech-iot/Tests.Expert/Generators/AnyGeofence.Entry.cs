// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate Geofence --entry-point any --force` overwrites it.
// It needs C# 14: a static extension member is what reaches this spelling without touching the library.

using JustDummies;

namespace AgriTech.Domain.Tests.Expert.Generators;

/// <summary>Hangs <c>Any.Geofence()</c> off the library's own entry point.</summary>
public static class AnyGeofenceEntry {

    extension(Any) {

        /// <summary>Starts an arbitrary <c>Geofence</c>: constrain it through <c>With…</c>, then <c>Generate()</c>.</summary>
        public static AnyGeofence Geofence() {
            return new AnyGeofence();
        }

    }

}
