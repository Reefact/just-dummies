// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate Isbn10 --entry-point any --force` overwrites it.
// It needs C# 14: a static extension member is what reaches this spelling without touching the library.

using JustDummies;

namespace InternationalCatalogue.Domain;

/// <summary>Hangs <c>Any.Isbn10()</c> off the library's own entry point.</summary>
public static class AnyIsbn10Entry {

    extension(Any) {

        /// <summary>Starts an arbitrary <c>Isbn10</c>: constrain it through <c>With…</c>, then <c>Generate()</c>.</summary>
        public static AnyIsbn10 Isbn10() {
            return new AnyIsbn10();
        }

    }

}
