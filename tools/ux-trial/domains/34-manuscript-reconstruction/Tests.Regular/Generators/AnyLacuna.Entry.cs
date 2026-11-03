// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate Lacuna --entry-point any --force` overwrites it.
// It needs C# 14: a static extension member is what reaches this spelling without touching the library.

using JustDummies;

namespace ManuscriptReconstruction.Domain;

/// <summary>Hangs <c>Any.Lacuna()</c> off the library's own entry point.</summary>
public static class AnyLacunaEntry {

    extension(Any) {

        /// <summary>Starts an arbitrary <c>Lacuna</c>: constrain it through <c>With…</c>, then <c>Generate()</c>.</summary>
        public static AnyLacuna Lacuna() {
            return new AnyLacuna();
        }

    }

}
