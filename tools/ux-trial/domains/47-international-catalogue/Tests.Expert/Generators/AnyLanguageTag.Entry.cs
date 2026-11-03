// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate LanguageTag --entry-point any --force` overwrites it.
// It needs C# 14: a static extension member is what reaches this spelling without touching the library.

using JustDummies;

namespace InternationalCatalogue.Domain;

/// <summary>Hangs <c>Any.LanguageTag()</c> off the library's own entry point.</summary>
public static class AnyLanguageTagEntry {

    extension(Any) {

        /// <summary>Starts an arbitrary <c>LanguageTag</c>: constrain it through <c>With…</c>, then <c>Generate()</c>.</summary>
        public static AnyLanguageTag LanguageTag() {
            return new AnyLanguageTag();
        }

    }

}
