// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate Catalogue --entry-point any --force` overwrites it.
// It needs C# 14: a static extension member is what reaches this spelling without touching the library.

using JustDummies;

namespace InternationalCatalogue.Domain;

/// <summary>Hangs <c>Any.Catalogue()</c> off the library's own entry point.</summary>
public static class AnyCatalogueEntry {

    extension(Any) {

        /// <summary>Starts an arbitrary <c>Catalogue</c>: constrain it through <c>With…</c>, then <c>Generate()</c>.</summary>
        public static AnyCatalogue Catalogue() {
            return new AnyCatalogue();
        }

    }

}
