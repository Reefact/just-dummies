// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate VariantNormalizer --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.

using System;

using JustDummies;

namespace DnaVariants.Domain;

/// <summary>
///     A generator of arbitrary <see cref="VariantNormalizer" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyVariantNormalizer : IAny<VariantNormalizer> {

    private IAny<ReferenceSequence>       _reference;
    private IAny<NormalizationConvention> _convention;

    private AnyReferenceSequence             _referenceRecipe;
    private AnyEnum<NormalizationConvention> _conventionRecipe;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyVariantNormalizer()
        : this(reference:  AnyValidReference(),
               convention: AnyValidConvention()) { }

    private static AnyReferenceSequence AnyValidReference() {
        return new AnyReferenceSequence();
    }

    private static AnyEnum<NormalizationConvention> AnyValidConvention() {
        return Any.Enum<NormalizationConvention>();
    }

    /// <summary>Draws every parameter from the recipe given for it.</summary>
    private AnyVariantNormalizer(AnyReferenceSequence             reference,
                                 AnyEnum<NormalizationConvention> convention) {
        _reference        = reference;
        _referenceRecipe  = reference;
        _convention       = convention;
        _conventionRecipe = convention;
    }

    /// <summary>Copies <paramref name="from" />, for a <c>With…</c> method to change one parameter of before handing it out.</summary>
    private AnyVariantNormalizer(AnyVariantNormalizer from) {
        _reference        = from._reference;
        _referenceRecipe  = from._referenceRecipe;
        _convention       = from._convention;
        _conventionRecipe = from._conventionRecipe;
    }

    /// <summary>Pins <c>reference</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyVariantNormalizer WithReference(ReferenceSequence value) {
        AnyVariantNormalizer copy = new AnyVariantNormalizer(this);

        copy._reference = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>reference</c> is drawn from — <c>AnyValidReference()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyReferenceSequence, AnyReferenceSequence&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyVariantNormalizer WithReference(Func<AnyReferenceSequence, AnyReferenceSequence> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyVariantNormalizer copy = new AnyVariantNormalizer(this);

        copy._referenceRecipe = configure(_referenceRecipe);
        copy._reference       = copy._referenceRecipe;

        return copy;
    }

    /// <summary>Pins <c>convention</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyVariantNormalizer WithConvention(NormalizationConvention value) {
        AnyVariantNormalizer copy = new AnyVariantNormalizer(this);

        copy._convention = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>convention</c> is drawn from — <c>AnyValidConvention()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyEnum&lt;NormalizationConvention&gt;, AnyEnum&lt;NormalizationConvention&gt;&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyVariantNormalizer WithConvention(Func<AnyEnum<NormalizationConvention>, AnyEnum<NormalizationConvention>> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyVariantNormalizer copy = new AnyVariantNormalizer(this);

        copy._conventionRecipe = configure(_conventionRecipe);
        copy._convention       = copy._conventionRecipe;

        return copy;
    }

    /// <summary>Produces one arbitrary <see cref="VariantNormalizer" />.</summary>
    public VariantNormalizer Generate() {
        return new VariantNormalizer(_reference.Generate(),
                                     _convention.Generate());
    }

}
