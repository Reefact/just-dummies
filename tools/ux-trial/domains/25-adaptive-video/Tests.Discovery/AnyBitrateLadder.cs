// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate BitrateLadder --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.
// A TODO(any) above a `_ = TODO_verify_…` line names a guard the scaffolder could not read; the recipe
// under it is its best generator for the type. Verify it honours that guard, or replace it, then delete
// those two lines. What the scaffolder reads, and what it cannot:
// https://github.com/Reefact/just-dummies/blob/main/doc/handwritten/for-users/packages/justdummies-cli.en.md#what-the-last-column-means

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

    private IAny<IEnumerable<EncodingVariant>> _variants;

    private IAny<IEnumerable<EncodingVariant>> _variantsRecipe;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyBitrateLadder()
        : this(variants: AnyValidVariants()) { }

    // A ladder is 1 to 6 variants. Heights are strictly increasing (so the ids differ and the resolution never falls),
    // bitrates strictly increasing (so they are distinct and pair with the heights in order).
    private static IAny<IEnumerable<EncodingVariant>> AnyValidVariants() {
        return Any.Int32().Between(1, 6).Then(count =>
            Any.Combine(
                Any.ListOf(Any.Int32().Between(100, 4320).Even()).WithCount(count).Ordered().Distinct(),
                Any.ListOf(Any.Int32().Between(Bitrate.MinKbps, Bitrate.MaxKbps)).WithCount(count).Ordered().Distinct(),
                Any.ListOf(Any.Int32().OneOf(24, 25, 30, 50, 60)).WithCount(count),
                Any.ListOf(Any.Int32().Between(0, 3000).Even()).WithCount(count),
                (heights, bitrates, framesPerSecond, widthOffsets) => {
                    List<EncodingVariant> variants = new List<EncodingVariant>();
                    for (int i = 0; i < count; i++) {
                        // Landscape and even: a width at least the height (and at least 128), at most 7680, from an even offset.
                        int width = Math.Min(7680, Math.Max(128, heights[i]) + widthOffsets[i]);
                        variants.Add(new EncodingVariant($"{heights[i]}p{framesPerSecond[i]}", width, heights[i], new Bitrate(bitrates[i])));
                    }

                    return (IEnumerable<EncodingVariant>)variants;
                }));
    }

    /// <summary>Draws every parameter from the recipe given for it.</summary>
    private AnyBitrateLadder(IAny<IEnumerable<EncodingVariant>> variants) {
        _variants       = variants;
        _variantsRecipe = variants;
    }

    /// <summary>Copies <paramref name="from" />, for a <c>With…</c> method to change one parameter of before handing it out.</summary>
    private AnyBitrateLadder(AnyBitrateLadder from) {
        _variants       = from._variants;
        _variantsRecipe = from._variantsRecipe;
    }

    /// <summary>Pins <c>variants</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyBitrateLadder WithVariants(IEnumerable<EncodingVariant> value) {
        AnyBitrateLadder copy = new AnyBitrateLadder(this);

        copy._variants = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>variants</c> is drawn from — <c>AnyValidVariants()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnySequence&lt;EncodingVariant&gt;, AnySequence&lt;EncodingVariant&gt;&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyBitrateLadder WithVariants(Func<IAny<IEnumerable<EncodingVariant>>, IAny<IEnumerable<EncodingVariant>>> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyBitrateLadder copy = new AnyBitrateLadder(this);

        copy._variantsRecipe = configure(_variantsRecipe);
        copy._variants       = copy._variantsRecipe;

        return copy;
    }

    /// <summary>Produces one arbitrary <see cref="BitrateLadder" />.</summary>
    public BitrateLadder Generate() {
        return new BitrateLadder(_variants.Generate());
    }

}
