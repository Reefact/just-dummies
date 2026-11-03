// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate NormalizedVariant --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.
// A TODO(any) above a `_ = TODO_verify_…` line names a guard the scaffolder could not read; the recipe
// under it is its best generator for the type. Verify it honours that guard, or replace it, then delete
// those two lines. What the scaffolder reads, and what it cannot:
// https://github.com/Reefact/just-dummies/blob/main/doc/handwritten/for-users/packages/justdummies-cli.en.md#what-the-last-column-means

using System;

using DnaVariants.Domain;
using JustDummies;

namespace DnaVariants.Tests.Expert.Generators;

/// <summary>
///     A generator of arbitrary <see cref="NormalizedVariant" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyNormalizedVariant : IAny<NormalizedVariant> {

    private IAny<Variant>                 _variant;
    private IAny<NormalizationConvention> _convention;
    private IAny<VariantKind>?            _kind;

    private AnyVariant                       _variantRecipe;
    private AnyEnum<NormalizationConvention> _conventionRecipe;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyNormalizedVariant()
        : this(variant:    AnyValidVariant(),
               convention: AnyValidConvention()) { }

    private static AnyVariant AnyValidVariant() {
        return new AnyVariant();
    }

    private static AnyEnum<NormalizationConvention> AnyValidConvention() {
        return Any.Enum<NormalizationConvention>();
    }

    /// <summary>Draws every parameter from the recipe given for it.</summary>
    private AnyNormalizedVariant(AnyVariant                       variant,
                                 AnyEnum<NormalizationConvention> convention) {
        _variant          = variant;
        _variantRecipe    = variant;
        _convention       = convention;
        _conventionRecipe = convention;
    }

    /// <summary>Copies <paramref name="from" />, for a <c>With…</c> method to change one parameter of before handing it out.</summary>
    private AnyNormalizedVariant(AnyNormalizedVariant from) {
        _variant          = from._variant;
        _variantRecipe    = from._variantRecipe;
        _convention       = from._convention;
        _conventionRecipe = from._conventionRecipe;
        _kind             = from._kind;
    }

    /// <summary>Pins <c>variant</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyNormalizedVariant WithVariant(Variant value) {
        AnyNormalizedVariant copy = new AnyNormalizedVariant(this);

        copy._variant = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>variant</c> is drawn from — <c>AnyValidVariant()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyVariant, AnyVariant&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyNormalizedVariant WithVariant(Func<AnyVariant, AnyVariant> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyNormalizedVariant copy = new AnyNormalizedVariant(this);

        copy._variantRecipe = configure(_variantRecipe);
        copy._variant       = copy._variantRecipe;

        return copy;
    }

    /// <summary>Pins <c>convention</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyNormalizedVariant WithConvention(NormalizationConvention value) {
        AnyNormalizedVariant copy = new AnyNormalizedVariant(this);

        copy._convention = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>convention</c> is drawn from — <c>AnyValidConvention()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyEnum&lt;NormalizationConvention&gt;, AnyEnum&lt;NormalizationConvention&gt;&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyNormalizedVariant WithConvention(Func<AnyEnum<NormalizationConvention>, AnyEnum<NormalizationConvention>> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyNormalizedVariant copy = new AnyNormalizedVariant(this);

        copy._conventionRecipe = configure(_conventionRecipe);
        copy._convention       = copy._conventionRecipe;

        return copy;
    }

    /// <summary>Pins the kind the variant is classified as; left alone, it is the kind of the drawn variant itself (the kind is a consequence of the variant, not an independent draw).</summary>
    public AnyNormalizedVariant WithKind(VariantKind value) {
        AnyNormalizedVariant copy = new AnyNormalizedVariant(this);

        copy._kind = Any.Fixed(value);

        return copy;
    }

    /// <summary>Produces one arbitrary <see cref="NormalizedVariant" />.</summary>
    public NormalizedVariant Generate() {
        Variant variant = _variant.Generate();

        return new NormalizedVariant(variant,
                                     _convention.Generate(),
                                     _kind is null ? variant.Kind : _kind.Generate());
    }

}
