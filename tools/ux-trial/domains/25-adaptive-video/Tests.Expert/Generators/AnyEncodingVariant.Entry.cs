// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate EncodingVariant --entry-point any --force` overwrites it.
// It needs C# 14: a static extension member is what reaches this spelling without touching the library.

using JustDummies;

namespace AdaptiveVideo.Domain;

/// <summary>Hangs <c>Any.EncodingVariant()</c> off the library's own entry point.</summary>
public static class AnyEncodingVariantEntry {

    extension(Any) {

        /// <summary>Starts an arbitrary <c>EncodingVariant</c>: constrain it through <c>With…</c>, then <c>Generate()</c>.</summary>
        public static AnyEncodingVariant EncodingVariant() {
            return new AnyEncodingVariant();
        }

    }

}
