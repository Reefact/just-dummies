// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate Individual --entry-point any --force` overwrites it.
// It needs C# 14: a static extension member is what reaches this spelling without touching the library.

using JustDummies;

namespace CaptiveBreeding.Domain;

/// <summary>Hangs <c>Any.Individual()</c> off the library's own entry point.</summary>
public static class AnyIndividualEntry {

    extension(Any) {

        /// <summary>Starts an arbitrary <c>Individual</c>: constrain it through <c>With…</c>, then <c>Generate()</c>.</summary>
        public static AnyIndividual Individual() {
            return new AnyIndividual();
        }

    }

}
