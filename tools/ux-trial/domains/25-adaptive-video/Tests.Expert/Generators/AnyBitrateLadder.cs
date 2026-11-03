// Scaffolded by `any` (JustDummies), then rewritten by hand: a ladder is not "a sequence of variants".
// Its rules tie the variants together (distinct ids, distinct bitrates, resolution never decreasing as the
// bitrate rises), so the generator draws the bitrates and the shapes apart and zips them in order.

using System;
using System.Collections.Generic;
using System.Linq;

using JustDummies;

namespace AdaptiveVideo.Domain;

/// <summary>
///     A generator of arbitrary <see cref="BitrateLadder" /> values: one to six variants, whose ids, bitrates and
///     resolutions satisfy the ladder's constructor by construction.
/// </summary>
public sealed partial class AnyBitrateLadder : IAny<BitrateLadder> {

    private IAny<IReadOnlyList<int>> _bitratesKbps;

    /// <summary>Creates the generator: between one and six variants at distinct bitrates.</summary>
    public AnyBitrateLadder() {
        _bitratesKbps = Any.SetOf(Any.Int32().Between(Bitrate.MinKbps, Bitrate.MaxKbps))
                           .WithCountBetween(1, 6)
                           .As(set => (IReadOnlyList<int>)set.OrderBy(kbps => kbps).ToList());
    }

    private AnyBitrateLadder(AnyBitrateLadder from) {
        _bitratesKbps = from._bitratesKbps;
    }

    /// <summary>Pins the bitrates of the variants, in any order; everything else about the variants stays arbitrary.</summary>
    public AnyBitrateLadder WithBitratesKbps(params int[] bitratesKbps) {
        AnyBitrateLadder copy = new AnyBitrateLadder(this);

        copy._bitratesKbps = Any.Fixed((IReadOnlyList<int>)bitratesKbps.OrderBy(kbps => kbps).ToList());

        return copy;
    }

    /// <summary>Produces one arbitrary <see cref="BitrateLadder" />.</summary>
    public BitrateLadder Generate() {
        IReadOnlyList<int> bitrates = _bitratesKbps.Generate();

        // One (height, frame rate) per variant, all different so the ids are, and sorted by height so the
        // resolution never decreases as the bitrate rises.
        List<(int Height, int FramesPerSecond)> shapes =
            Any.SetOf(Any.PairOf(Any.Int32().Between(96, 4320).Even(), Any.Int32().OneOf(24, 25, 30, 50, 60)))
               .WithCount(bitrates.Count)
               .Generate()
               .OrderBy(shape => shape.Item1)
               .ToList();

        List<EncodingVariant> variants = new();
        for (int i = 0; i < bitrates.Count; i++) {
            variants.Add(new AnyEncodingVariant().WithHeight(shapes[i].Height)
                                                 .WithFramesPerSecond(shapes[i].FramesPerSecond)
                                                 .WithBitrateKbps(bitrates[i])
                                                 .Generate());
        }

        return new BitrateLadder(variants);
    }

}
