// Written by hand, beside the scaffolded AnyBitrateLadder.cs.

using AdaptiveVideo.Domain;
using JustDummies;

namespace AdaptiveVideo.Tests.Regular.Generators;

public sealed partial class AnyBitrateLadder {

    /// <summary>A ladder offering exactly these bitrates; everything else about its variants is arbitrary.</summary>
    public AnyBitrateLadder WithBitrates(params int[] kbps) {
        return new AnyBitrateLadder(new LadderVariants(kbps));
    }

    private sealed class LadderVariants : IAny<IEnumerable<EncodingVariant>> {

        private readonly int[]? _bitratesKbps;

        public LadderVariants(int[]? bitratesKbps) {
            _bitratesKbps = bitratesKbps;
        }

        public IEnumerable<EncodingVariant> Generate() {
            int[] kbps = _bitratesKbps
                      ?? Any.ArrayOf(Any.Int32().Between(Bitrate.MinKbps, Bitrate.MaxKbps))
                            .WithCountBetween(1, 6)
                            .Distinct()
                            .Generate();
            // Distinct even heights, paired in order with the bitrates: a higher bitrate never gets a lower resolution.
            int[] heights = Any.ArrayOf(Any.Int32().Between(50, 2160))
                               .WithCount(kbps.Length)
                               .Distinct()
                               .Generate()
                               .Select(half => half * 2)
                               .Order()
                               .ToArray();
            int[] ascendingKbps = kbps.Order().ToArray();

            return ascendingKbps.Select((rate, i) => new AnyEncodingVariant()
                                                         .WithHeight(heights[i])
                                                         .WithBitrate(new Bitrate(rate))
                                                         .Generate())
                                .ToList();
        }

    }

}
