// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate BitrateLadder --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.
// A TODO(any) above a `_ = TODO_verify_…` line names a guard the scaffolder could not read; the recipe
// under it is its best generator for the type. Verify it honours that guard, or replace it, then delete
// those two lines. What the scaffolder reads, and what it cannot:
// https://github.com/Reefact/just-dummies/blob/main/doc/handwritten/for-users/packages/justdummies-cli.en.md#what-the-last-column-means

using System;
using System.Collections.Generic;
using System.Linq;

using JustDummies;

namespace AdaptiveVideo.Domain;

/// <summary>
///     A generator of arbitrary <see cref="BitrateLadder" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyBitrateLadder : IAny<BitrateLadder> {

    private IAny<IEnumerable<EncodingVariant>>? _variants;
    private AnyInt32                             _rungs = Any.Int32().Between(2, 6);

    private AnySequence<EncodingVariant> _variantsRecipe;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyBitrateLadder()
        : this(variants: AnyValidVariants()) { }

    private static AnySequence<EncodingVariant> AnyValidVariants() {
        // Never used to draw: a ladder is built in Generate(), because its variants depend on one another
        // (distinct bitrates, ascending heights, unique ids). Kept as the shape of the scaffold.
        return Any.SequenceOf(new AnyEncodingVariant());
    }

    /// <summary>Draws every parameter from the recipe given for it.</summary>
    private AnyBitrateLadder(AnySequence<EncodingVariant> variants) {
        _variantsRecipe = variants;
    }

    /// <summary>Copies <paramref name="from" />, for a <c>With…</c> method to change one parameter of before handing it out.</summary>
    private AnyBitrateLadder(AnyBitrateLadder from) {
        _variants       = from._variants;
        _variantsRecipe = from._variantsRecipe;
        _rungs          = from._rungs;
    }

    /// <summary>Pins <c>variants</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyBitrateLadder WithVariants(IEnumerable<EncodingVariant> value) {
        AnyBitrateLadder copy = new AnyBitrateLadder(this);

        copy._variants = Any.Fixed(value);

        return copy;
    }

    /// <summary>The number of rungs of the drawn ladder.</summary>
    public AnyBitrateLadder WithRungs(int count) {
        AnyBitrateLadder copy = new AnyBitrateLadder(this);

        copy._rungs = Any.Int32().Between(count, count);

        return copy;
    }

    /// <summary>Produces one arbitrary <see cref="BitrateLadder" />.</summary>
    public BitrateLadder Generate() {
        if (_variants is not null) {
            return new BitrateLadder(_variants.Generate());
        }

        int count = _rungs.Generate();

        // Distinct bitrates and strictly ascending heights: the ids are then unique whatever the frame rates.
        List<int> bitrates = Any.SetOf(Any.Int32().Between(Bitrate.MinKbps, Bitrate.MaxKbps)).WithCount(count).Generate().OrderBy(k => k).ToList();
        List<int> heights  = Any.SetOf(Any.Int32().Between(48, 2160)).WithCount(count).Generate().OrderBy(h => h).ToList();

        List<EncodingVariant> variants = new List<EncodingVariant>();
        for (int i = 0; i < count; i++) {
            variants.Add(new AnyEncodingVariant().WithHeight(heights[i] * 2)
                                                 .WithBitrate(new Bitrate(bitrates[i]))
                                                 .Generate());
        }

        return new BitrateLadder(variants);
    }

}
