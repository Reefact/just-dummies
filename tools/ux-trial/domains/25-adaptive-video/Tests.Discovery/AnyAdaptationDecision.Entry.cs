// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate AdaptationDecision --entry-point any --force` overwrites it.
// It needs C# 14: a static extension member is what reaches this spelling without touching the library.

using JustDummies;

namespace AdaptiveVideo.Domain;

/// <summary>Hangs <c>Any.AdaptationDecision()</c> off the library's own entry point.</summary>
public static class AnyAdaptationDecisionEntry {

    extension(Any) {

        /// <summary>Starts an arbitrary <c>AdaptationDecision</c>: constrain it through <c>With…</c>, then <c>Generate()</c>.</summary>
        public static AnyAdaptationDecision AdaptationDecision() {
            return new AnyAdaptationDecision();
        }

    }

}
