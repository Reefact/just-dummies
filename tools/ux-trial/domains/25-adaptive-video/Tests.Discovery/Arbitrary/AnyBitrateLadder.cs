// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate BitrateLadder --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.

using System;
using System.Collections.Generic;

using JustDummies;

namespace AdaptiveVideo.Domain;

/// <summary>
///     A generator of arbitrary <see cref="BitrateLadder" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyBitrateLadder : IAny<BitrateLadder> {

    private readonly IAny<IEnumerable<EncodingVariant>> _variants;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyBitrateLadder()
        : this(variants: AnyValidVariants()) { }

    private static IAny<IEnumerable<EncodingVariant>> AnyValidVariants() {
        // A ladder rests on rules between its variants (distinct ids and bitrates, a resolution that never
        // drops as the bitrate rises), so the rungs are drawn as a whole: distinct bitrates and distinct
        // heights, each sorted, then paired.
        return Any.Combine(
            Any.ListOf(Any.Int32().Between(Bitrate.MinKbps, Bitrate.MaxKbps)).WithCountBetween(1, 6).Distinct(),
            Any.ListOf(Any.Int32().OneOf(StandardHeights)).WithCount(6).Distinct(),
            Any.Int32().OneOf(24, 25, 30, 50, 60),
            (bitrates, heights, fps) => Rungs(bitrates, heights, fps));
    }

    private static readonly int[] StandardHeights = [144, 240, 360, 480, 720, 1080, 1440, 2160];

    private static IEnumerable<EncodingVariant> Rungs(List<int> bitrates, List<int> heights, int fps) {
        List<int> sortedBitrates = bitrates.OrderBy(b => b).ToList();
        List<int> sortedHeights  = heights.OrderBy(h => h).ToList();
        List<EncodingVariant> rungs = [];
        for (int i = 0; i < sortedBitrates.Count; i++) {
            int height = sortedHeights[i];
            int width  = (int)Math.Round(height * 16 / 9.0 / 2) * 2;
            rungs.Add(new EncodingVariant($"{height}p{fps}", width, height, new Bitrate(sortedBitrates[i])));
        }

        return rungs;
    }

    private AnyBitrateLadder(IAny<IEnumerable<EncodingVariant>> variants) {
        _variants = variants;
    }

    /// <summary>Pins <c>variants</c> to a fixed value.</summary>
    public AnyBitrateLadder WithVariants(IEnumerable<EncodingVariant> value) {
        return new AnyBitrateLadder(new FixedValue<IEnumerable<EncodingVariant>>(value));
    }


    /// <summary>Produces one arbitrary <see cref="BitrateLadder" />.</summary>
    public BitrateLadder Generate() {
        return new BitrateLadder(_variants.Generate());
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
