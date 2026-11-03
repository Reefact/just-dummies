// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate VariantNormalizer --entry-point any --force` overwrites it.
// It needs C# 14: a static extension member is what reaches this spelling without touching the library.

using JustDummies;

namespace DnaVariants.Tests.Expert.Generators;

/// <summary>Hangs <c>Any.VariantNormalizer()</c> off the library's own entry point.</summary>
public static class AnyVariantNormalizerEntry {

    extension(Any) {

        /// <summary>Starts an arbitrary <c>VariantNormalizer</c>: constrain it through <c>With…</c>, then <c>Generate()</c>.</summary>
        public static AnyVariantNormalizer VariantNormalizer() {
            return new AnyVariantNormalizer();
        }

    }

}
