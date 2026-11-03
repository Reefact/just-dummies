// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate GeoPoint --entry-point any --force` overwrites it.
// It needs C# 14: a static extension member is what reaches this spelling without touching the library.

using JustDummies;

namespace CatastropheLosses.Domain;

/// <summary>Hangs <c>Any.GeoPoint()</c> off the library's own entry point.</summary>
public static class AnyGeoPointEntry {

    extension(Any) {

        /// <summary>Starts an arbitrary <c>GeoPoint</c>: constrain it through <c>With…</c>, then <c>Generate()</c>.</summary>
        public static AnyGeoPoint GeoPoint() {
            return new AnyGeoPoint();
        }

    }

}
