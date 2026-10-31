// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate NoduleObservation --entry-point any --force` overwrites it.
// It needs C# 14: a static extension member is what reaches this spelling without touching the library.

using JustDummies;

namespace PulmonaryNodules.Tests.Expert.Arbitrary;

/// <summary>Hangs <c>Any.NoduleObservation()</c> off the library's own entry point.</summary>
public static class AnyNoduleObservationEntry {

    extension(Any) {

        /// <summary>Starts an arbitrary <c>NoduleObservation</c>: constrain it through <c>With…</c>, then <c>Generate()</c>.</summary>
        public static AnyNoduleObservation NoduleObservation() {
            return new AnyNoduleObservation();
        }

    }

}
