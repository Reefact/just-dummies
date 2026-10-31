// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate TrackedNodule --entry-point any --force` overwrites it.
// It needs C# 14: a static extension member is what reaches this spelling without touching the library.

using JustDummies;

namespace PulmonaryNodules.Domain;

/// <summary>Hangs <c>Any.TrackedNodule()</c> off the library's own entry point.</summary>
public static class AnyTrackedNoduleEntry {

    extension(Any) {

        /// <summary>Starts an arbitrary <c>TrackedNodule</c>: constrain it through <c>With…</c>, then <c>Generate()</c>.</summary>
        public static AnyTrackedNodule TrackedNodule() {
            return new AnyTrackedNodule();
        }

    }

}
