// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate Variant --force` overwrites it. This type is partial, so members you add in a
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
///     A generator of arbitrary <see cref="Variant" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyVariant : IAny<Variant> {

    private IAny<Accession> _accession;
    private IAny<int>       _position;
    private IAny<Allele>    _reference;
    private IAny<Allele>    _alternate;

    private AnyAccession _accessionRecipe;
    private AnyInt32     _positionRecipe;
    private AnyAllele    _referenceRecipe;
    private AnyAllele    _alternateRecipe;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyVariant()
        : this(accession: AnyValidAccession(),
               position:  AnyValidPosition(),
               reference: AnyValidReference(),
               alternate: AnyValidAlternate()) { }

    private static AnyAccession AnyValidAccession() {
        return new AnyAccession();
    }

    private static AnyInt32 AnyValidPosition() {
        return Any.Int32().Positive();
    }

    private static AnyAllele AnyValidReference() {
        return new AnyAllele();
    }

    private static AnyAllele AnyValidAlternate() {
        return new AnyAllele();
    }

    /// <summary>Draws every parameter from the recipe given for it.</summary>
    private AnyVariant(AnyAccession accession,
                       AnyInt32     position,
                       AnyAllele    reference,
                       AnyAllele    alternate) {
        _accession       = accession;
        _accessionRecipe = accession;
        _position        = position;
        _positionRecipe  = position;
        _reference       = reference;
        _referenceRecipe = reference;
        _alternate       = alternate;
        _alternateRecipe = alternate;
    }

    /// <summary>Copies <paramref name="from" />, for a <c>With…</c> method to change one parameter of before handing it out.</summary>
    private AnyVariant(AnyVariant from) {
        _accession       = from._accession;
        _accessionRecipe = from._accessionRecipe;
        _position        = from._position;
        _positionRecipe  = from._positionRecipe;
        _reference       = from._reference;
        _referenceRecipe = from._referenceRecipe;
        _alternate       = from._alternate;
        _alternateRecipe = from._alternateRecipe;
    }

    /// <summary>Pins <c>accession</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyVariant WithAccession(Accession value) {
        AnyVariant copy = new AnyVariant(this);

        copy._accession = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>accession</c> is drawn from — <c>AnyValidAccession()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyAccession, AnyAccession&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyVariant WithAccession(Func<AnyAccession, AnyAccession> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyVariant copy = new AnyVariant(this);

        copy._accessionRecipe = configure(_accessionRecipe);
        copy._accession       = copy._accessionRecipe;

        return copy;
    }

    /// <summary>Pins <c>position</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyVariant WithPosition(int value) {
        AnyVariant copy = new AnyVariant(this);

        copy._position = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>position</c> is drawn from — <c>AnyValidPosition()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyInt32, AnyInt32&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyVariant WithPosition(Func<AnyInt32, AnyInt32> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyVariant copy = new AnyVariant(this);

        copy._positionRecipe = configure(_positionRecipe);
        copy._position       = copy._positionRecipe;

        return copy;
    }

    /// <summary>Pins <c>reference</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyVariant WithReference(Allele value) {
        AnyVariant copy = new AnyVariant(this);

        copy._reference = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>reference</c> is drawn from — <c>AnyValidReference()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyAllele, AnyAllele&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyVariant WithReference(Func<AnyAllele, AnyAllele> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyVariant copy = new AnyVariant(this);

        copy._referenceRecipe = configure(_referenceRecipe);
        copy._reference       = copy._referenceRecipe;

        return copy;
    }

    /// <summary>Pins <c>alternate</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyVariant WithAlternate(Allele value) {
        AnyVariant copy = new AnyVariant(this);

        copy._alternate = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>alternate</c> is drawn from — <c>AnyValidAlternate()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyAllele, AnyAllele&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyVariant WithAlternate(Func<AnyAllele, AnyAllele> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyVariant copy = new AnyVariant(this);

        copy._alternateRecipe = configure(_alternateRecipe);
        copy._alternate       = copy._alternateRecipe;

        return copy;
    }

    /// <summary>Produces one arbitrary <see cref="Variant" />.</summary>
    public Variant Generate() {
        Allele reference = _reference.Generate();

        return new Variant(_accession.Generate(),
                           _position.Generate(),
                           reference,
                           _alternate.DifferentFrom(reference).Generate());
    }

}
