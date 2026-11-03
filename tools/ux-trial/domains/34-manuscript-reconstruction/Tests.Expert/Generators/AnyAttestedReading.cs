// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate AttestedReading --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.
// A TODO(any) above a `_ = TODO_verify_…` line names a guard the scaffolder could not read; the recipe
// under it is its best generator for the type. Verify it honours that guard, or replace it, then delete
// those two lines. What the scaffolder reads, and what it cannot:
// https://github.com/Reefact/just-dummies/blob/main/doc/handwritten/for-users/packages/justdummies-cli.en.md#what-the-last-column-means

using System;
using System.Collections.Generic;

using JustDummies;

namespace ManuscriptReconstruction.Domain;

/// <summary>
///     A generator of arbitrary <see cref="AttestedReading" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyAttestedReading : IAny<AttestedReading> {

    private IAny<PassageReference>                   _passage;
    private IAny<string>                             _text;
    private IAny<IReadOnlyCollection<WitnessSiglum>> _witnesses;

    private AnyPassageReference    _passageRecipe;
    private AnyString              _textRecipe;
    private AnyList<WitnessSiglum> _witnessesRecipe;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyAttestedReading()
        : this(passage:   AnyValidPassage(),
               text:      AnyValidText(),
               witnesses: AnyValidWitnesses()) { }

    private static AnyPassageReference AnyValidPassage() {
        return new AnyPassageReference();
    }

    private static AnyString AnyValidText() {
        return Any.String();
    }

    private static AnyList<WitnessSiglum> AnyValidWitnesses() {
        return Any.ListOf(new AnyWitnessSiglum()).WithCountBetween(1, 6).Distinct();
    }

    /// <summary>Draws every parameter from the recipe given for it.</summary>
    private AnyAttestedReading(AnyPassageReference    passage,
                               AnyString              text,
                               AnyList<WitnessSiglum> witnesses) {
        _passage         = passage;
        _passageRecipe   = passage;
        _text            = text;
        _textRecipe      = text;
        _witnesses       = witnesses;
        _witnessesRecipe = witnesses;
    }

    /// <summary>Copies <paramref name="from" />, for a <c>With…</c> method to change one parameter of before handing it out.</summary>
    private AnyAttestedReading(AnyAttestedReading from) {
        _passage         = from._passage;
        _passageRecipe   = from._passageRecipe;
        _text            = from._text;
        _textRecipe      = from._textRecipe;
        _witnesses       = from._witnesses;
        _witnessesRecipe = from._witnessesRecipe;
    }

    /// <summary>Pins <c>passage</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyAttestedReading WithPassage(PassageReference value) {
        AnyAttestedReading copy = new AnyAttestedReading(this);

        copy._passage = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>passage</c> is drawn from — <c>AnyValidPassage()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyPassageReference, AnyPassageReference&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyAttestedReading WithPassage(Func<AnyPassageReference, AnyPassageReference> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyAttestedReading copy = new AnyAttestedReading(this);

        copy._passageRecipe = configure(_passageRecipe);
        copy._passage       = copy._passageRecipe;

        return copy;
    }

    /// <summary>Pins <c>text</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyAttestedReading WithText(string value) {
        AnyAttestedReading copy = new AnyAttestedReading(this);

        copy._text = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>text</c> is drawn from — <c>AnyValidText()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyString, AnyString&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyAttestedReading WithText(Func<AnyString, AnyString> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyAttestedReading copy = new AnyAttestedReading(this);

        copy._textRecipe = configure(_textRecipe);
        copy._text       = copy._textRecipe;

        return copy;
    }

    /// <summary>Pins <c>witnesses</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyAttestedReading WithWitnesses(IReadOnlyCollection<WitnessSiglum> value) {
        AnyAttestedReading copy = new AnyAttestedReading(this);

        copy._witnesses = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>witnesses</c> is drawn from — <c>AnyValidWitnesses()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyList&lt;WitnessSiglum&gt;, AnyList&lt;WitnessSiglum&gt;&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyAttestedReading WithWitnesses(Func<AnyList<WitnessSiglum>, AnyList<WitnessSiglum>> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyAttestedReading copy = new AnyAttestedReading(this);

        copy._witnessesRecipe = configure(_witnessesRecipe);
        copy._witnesses       = copy._witnessesRecipe;

        return copy;
    }

    /// <summary>Produces one arbitrary <see cref="AttestedReading" />.</summary>
    public AttestedReading Generate() {
        return new AttestedReading(_passage.Generate(),
                                   _text.Generate(),
                                   _witnesses.Generate());
    }

}
