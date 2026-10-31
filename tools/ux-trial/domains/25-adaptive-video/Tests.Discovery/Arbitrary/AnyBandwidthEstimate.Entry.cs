// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate BandwidthEstimate --entry-point any --force` overwrites it.
// It needs C# 14: a static extension member is what reaches this spelling without touching the library.

using JustDummies;

namespace AdaptiveVideo.Domain;

/// <summary>Hangs <c>Any.BandwidthEstimate()</c> off the library's own entry point.</summary>
public static class AnyBandwidthEstimateEntry {

    extension(Any) {

        /// <summary>Starts an arbitrary <c>BandwidthEstimate</c>: constrain it through <c>With…</c>, then <c>Generate()</c>.</summary>
        public static AnyBandwidthEstimate BandwidthEstimate() {
            return new AnyBandwidthEstimate();
        }

    }

}
