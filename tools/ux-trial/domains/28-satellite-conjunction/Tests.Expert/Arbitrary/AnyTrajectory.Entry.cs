// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate Trajectory --entry-point any --force` overwrites it.
// It needs C# 14: a static extension member is what reaches this spelling without touching the library.

using JustDummies;

namespace SatelliteConjunction.Tests.Expert.Arbitrary;

/// <summary>Hangs <c>Any.Trajectory()</c> off the library's own entry point.</summary>
public static class AnyTrajectoryEntry {

    extension(Any) {

        /// <summary>Starts an arbitrary <c>Trajectory</c>: constrain it through <c>With…</c>, then <c>Generate()</c>.</summary>
        public static AnyTrajectory Trajectory() {
            return new AnyTrajectory();
        }

    }

}
