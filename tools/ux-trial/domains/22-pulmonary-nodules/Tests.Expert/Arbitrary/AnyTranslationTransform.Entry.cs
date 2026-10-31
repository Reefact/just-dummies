// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate TranslationTransform --entry-point any --force` overwrites it.
// It needs C# 14: a static extension member is what reaches this spelling without touching the library.

using JustDummies;

namespace PulmonaryNodules.Tests.Expert.Arbitrary;

/// <summary>Hangs <c>Any.TranslationTransform()</c> off the library's own entry point.</summary>
public static class AnyTranslationTransformEntry {

    extension(Any) {

        /// <summary>Starts an arbitrary <c>TranslationTransform</c>: constrain it through <c>With…</c>, then <c>Generate()</c>.</summary>
        public static AnyTranslationTransform TranslationTransform() {
            return new AnyTranslationTransform();
        }

    }

}
