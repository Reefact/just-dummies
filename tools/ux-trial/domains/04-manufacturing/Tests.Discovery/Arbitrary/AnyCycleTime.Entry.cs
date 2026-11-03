// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate CycleTime --entry-point any --force` overwrites it.
// It needs C# 14: a static extension member is what reaches this spelling without touching the library.

using JustDummies;

namespace Manufacturing.Tests.Discovery.Arbitrary;

/// <summary>Hangs <c>Any.CycleTime()</c> off the library's own entry point.</summary>
public static class AnyCycleTimeEntry {

    extension(Any) {

        /// <summary>Starts an arbitrary <c>CycleTime</c>: constrain it through <c>With…</c>, then <c>Generate()</c>.</summary>
        public static AnyCycleTime CycleTime() {
            return new AnyCycleTime();
        }

    }

}
