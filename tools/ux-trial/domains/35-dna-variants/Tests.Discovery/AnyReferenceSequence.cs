// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate ReferenceSequence --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.

using System;

using JustDummies;

namespace DnaVariants.Domain;

/// <summary>
///     A generator of arbitrary <see cref="ReferenceSequence" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyReferenceSequence : IAny<ReferenceSequence> {

    private IAny<Accession>          _accession;
    private IAny<string>             _name;
    private IAny<NucleotideSequence> _sequence;

    private AnyAccession          _accessionRecipe;
    private AnyString             _nameRecipe;
    private AnyNucleotideSequence _sequenceRecipe;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyReferenceSequence()
        : this(accession: AnyValidAccession(),
               name:      AnyValidName(),
               sequence:  AnyValidSequence()) { }

    private static AnyAccession AnyValidAccession() {
        return new AnyAccession();
    }

    private static AnyString AnyValidName() {
        return Any.String().NotBlank();
    }

    private static AnyNucleotideSequence AnyValidSequence() {
        return new AnyNucleotideSequence();
    }

    /// <summary>Draws every parameter from the recipe given for it.</summary>
    private AnyReferenceSequence(AnyAccession          accession,
                                 AnyString             name,
                                 AnyNucleotideSequence sequence) {
        _accession       = accession;
        _accessionRecipe = accession;
        _name            = name;
        _nameRecipe      = name;
        _sequence        = sequence;
        _sequenceRecipe  = sequence;
    }

    /// <summary>Copies <paramref name="from" />, for a <c>With…</c> method to change one parameter of before handing it out.</summary>
    private AnyReferenceSequence(AnyReferenceSequence from) {
        _accession       = from._accession;
        _accessionRecipe = from._accessionRecipe;
        _name            = from._name;
        _nameRecipe      = from._nameRecipe;
        _sequence        = from._sequence;
        _sequenceRecipe  = from._sequenceRecipe;
    }

    /// <summary>Pins <c>accession</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyReferenceSequence WithAccession(Accession value) {
        AnyReferenceSequence copy = new AnyReferenceSequence(this);

        copy._accession = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>accession</c> is drawn from — <c>AnyValidAccession()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyAccession, AnyAccession&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyReferenceSequence WithAccession(Func<AnyAccession, AnyAccession> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyReferenceSequence copy = new AnyReferenceSequence(this);

        copy._accessionRecipe = configure(_accessionRecipe);
        copy._accession       = copy._accessionRecipe;

        return copy;
    }

    /// <summary>Pins <c>name</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyReferenceSequence WithName(string value) {
        AnyReferenceSequence copy = new AnyReferenceSequence(this);

        copy._name = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>name</c> is drawn from — <c>AnyValidName()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyString, AnyString&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyReferenceSequence WithName(Func<AnyString, AnyString> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyReferenceSequence copy = new AnyReferenceSequence(this);

        copy._nameRecipe = configure(_nameRecipe);
        copy._name       = copy._nameRecipe;

        return copy;
    }

    /// <summary>Pins <c>sequence</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyReferenceSequence WithSequence(NucleotideSequence value) {
        AnyReferenceSequence copy = new AnyReferenceSequence(this);

        copy._sequence = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>sequence</c> is drawn from — <c>AnyValidSequence()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyNucleotideSequence, AnyNucleotideSequence&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyReferenceSequence WithSequence(Func<AnyNucleotideSequence, AnyNucleotideSequence> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyReferenceSequence copy = new AnyReferenceSequence(this);

        copy._sequenceRecipe = configure(_sequenceRecipe);
        copy._sequence       = copy._sequenceRecipe;

        return copy;
    }

    /// <summary>Produces one arbitrary <see cref="ReferenceSequence" />.</summary>
    public ReferenceSequence Generate() {
        return new ReferenceSequence(_accession.Generate(),
                                     _name.Generate(),
                                     _sequence.Generate());
    }

}
