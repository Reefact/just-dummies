// Scaffolded by `any` (JustDummies), then reshaped by hand: `Any.SequenceOf(new AnyEncodingVariant())`
// cannot honour the ladder's invariants, which live BETWEEN the rungs (unique ids, distinct bitrates,
// resolution never decreasing as bitrate rises). The ladder is therefore built rung by rung: draw the
// bitrates and the heights as two distinct sets of the same size, sort both, and pair them.
// Do NOT re-run `any generate BitrateLadder --force` on this file.

using System;
using System.Collections.Generic;
using System.Linq;

using JustDummies;

namespace AdaptiveVideo.Domain;

/// <summary>A generator of arbitrary, well-formed <see cref="BitrateLadder" /> values.</summary>
public sealed partial class AnyBitrateLadder : IAny<BitrateLadder> {

    private readonly IAny<int>                           _rungCount;
    private readonly IReadOnlyList<int>?                 _bitrates;
    private readonly IAny<IEnumerable<EncodingVariant>>? _variants;

    public AnyBitrateLadder()
        : this(rungCount: Any.Int32().Between(1, 8), bitrates: null, variants: null) { }

    private AnyBitrateLadder(IAny<int> rungCount, IReadOnlyList<int>? bitrates, IAny<IEnumerable<EncodingVariant>>? variants) {
        _rungCount = rungCount;
        _bitrates  = bitrates;
        _variants  = variants;
    }

    /// <summary>Pins the whole ladder.</summary>
    public AnyBitrateLadder WithVariants(IEnumerable<EncodingVariant> value) {
        return new AnyBitrateLadder(_rungCount, null, new FixedValue<IEnumerable<EncodingVariant>>(value));
    }

    /// <summary>Pins the bitrates the test is about; every other property of each rung stays arbitrary.</summary>
    public AnyBitrateLadder WithBitrates(params int[] kbps) {
        return new AnyBitrateLadder(new FixedValue<int>(kbps.Length), kbps, null);
    }

    /// <summary>Draws a ladder of exactly <paramref name="count" /> rungs.</summary>
    public AnyBitrateLadder WithRungCount(int count) {
        return new AnyBitrateLadder(new FixedValue<int>(count), null, null);
    }

    public BitrateLadder Generate() {
        if (_variants is not null) {
            return new BitrateLadder(_variants.Generate());
        }

        int count = _rungCount.Generate();
        IEnumerable<int> bitrates = (IEnumerable<int>?)_bitrates
                                    ?? Any.SetOf(Any.Int32().Between(Bitrate.MinKbps, Bitrate.MaxKbps)).WithCount(count).Generate();
        HashSet<int> heights = Any.SetOf(AnyEncodingVariant.AnyValidHeight()).WithCount(count).Generate();

        IEnumerable<EncodingVariant> rungs = bitrates.Order()
                                                     .Zip(heights.Order(),
                                                          (kbps, height) => new AnyEncodingVariant()
                                                                            .WithHeight(height)
                                                                            .WithBitrate(new Bitrate(kbps))
                                                                            .Generate());

        return new BitrateLadder(rungs.ToList());
    }

    private sealed class FixedValue<TValue> : IAny<TValue> {

        private readonly TValue _value;

        public FixedValue(TValue value) {
            _value = value;
        }

        public TValue Generate() {
            return _value;
        }

    }

}
