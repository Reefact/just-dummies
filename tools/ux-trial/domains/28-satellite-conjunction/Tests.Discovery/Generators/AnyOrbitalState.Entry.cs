// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate OrbitalState --entry-point any --force` overwrites it.
// It needs C# 14: a static extension member is what reaches this spelling without touching the library.

using JustDummies;

namespace SatelliteConjunction.Domain;

/// <summary>Hangs <c>Any.OrbitalState()</c> off the library's own entry point.</summary>
public static class AnyOrbitalStateEntry {

    extension(Any) {

        /// <summary>Starts an arbitrary <c>OrbitalState</c>: constrain it through <c>With…</c>, then <c>Generate()</c>.</summary>
        public static AnyOrbitalState OrbitalState() {
            return new AnyOrbitalState();
        }

    }

}
