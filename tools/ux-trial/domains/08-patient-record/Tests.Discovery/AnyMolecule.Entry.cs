// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate Molecule --entry-point any --force` overwrites it.
// It needs C# 14: a static extension member is what reaches this spelling without touching the library.

using JustDummies;

namespace Clinical.Domain;

/// <summary>Hangs <c>Any.Molecule()</c> off the library's own entry point.</summary>
public static class AnyMoleculeEntry {

    extension(Any) {

        /// <summary>Starts an arbitrary <c>Molecule</c>: constrain it through <c>With…</c>, then <c>Generate()</c>.</summary>
        public static AnyMolecule Molecule() {
            return new AnyMolecule();
        }

    }

}
