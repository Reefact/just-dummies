// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate CadastralReference --entry-point any --force` overwrites it.
// It needs C# 14: a static extension member is what reaches this spelling without touching the library.

using JustDummies;

namespace RealEstate.Domain;

/// <summary>Hangs <c>Any.CadastralReference()</c> off the library's own entry point.</summary>
public static class AnyCadastralReferenceEntry {

    extension(Any) {

        /// <summary>Starts an arbitrary <c>CadastralReference</c>: constrain it through <c>With…</c>, then <c>Generate()</c>.</summary>
        public static AnyCadastralReference CadastralReference() {
            return new AnyCadastralReference();
        }

    }

}
