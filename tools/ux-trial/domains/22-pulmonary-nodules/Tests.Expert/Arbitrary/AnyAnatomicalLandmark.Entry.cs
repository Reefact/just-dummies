// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate AnatomicalLandmark --entry-point any --force` overwrites it.
// It needs C# 14: a static extension member is what reaches this spelling without touching the library.

using JustDummies;

namespace PulmonaryNodules.Tests.Expert.Arbitrary;

/// <summary>Hangs <c>Any.AnatomicalLandmark()</c> off the library's own entry point.</summary>
public static class AnyAnatomicalLandmarkEntry {

    extension(Any) {

        /// <summary>Starts an arbitrary <c>AnatomicalLandmark</c>: constrain it through <c>With…</c>, then <c>Generate()</c>.</summary>
        public static AnyAnatomicalLandmark AnatomicalLandmark() {
            return new AnyAnatomicalLandmark();
        }

    }

}
