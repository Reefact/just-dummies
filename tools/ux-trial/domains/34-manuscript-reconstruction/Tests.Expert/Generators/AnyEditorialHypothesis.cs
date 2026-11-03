// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate EditorialHypothesis --force` overwrites it. This type is partial, so members you add in a
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
///     A generator of arbitrary <see cref="EditorialHypothesis" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyEditorialHypothesis : IAny<EditorialHypothesis> {

    private IAny<Guid>                               _id;
    private IAny<PassageReference>                   _passage;
    private IAny<string>                             _proposedReading;
    private IAny<string>                             _editor;
    private IAny<IReadOnlyCollection<WitnessSiglum>> _supportingWitnesses;
    private IAny<bool>                               _isConjecture;
    private IAny<string>                             _rationale;

    private AnyGuid                _idRecipe;
    private AnyPassageReference    _passageRecipe;
    private AnyString              _proposedReadingRecipe;
    private AnyString              _editorRecipe;
    private AnyList<WitnessSiglum> _supportingWitnessesRecipe;
    private AnyBoolean             _isConjectureRecipe;
    private AnyString              _rationaleRecipe;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyEditorialHypothesis()
        : this(id:                  AnyValidId(),
               passage:             AnyValidPassage(),
               proposedReading:     AnyValidProposedReading(),
               editor:              AnyValidEditor(),
               supportingWitnesses: AnyValidSupportingWitnesses(),
               isConjecture:        AnyValidIsConjecture(),
               rationale:           AnyValidRationale()) { }

    private static AnyGuid AnyValidId() {
        return Any.Guid().NonEmpty();
    }

    private static AnyPassageReference AnyValidPassage() {
        return new AnyPassageReference();
    }

    private static AnyString AnyValidProposedReading() {
        return Any.String();
    }

    private static AnyString AnyValidEditor() {
        return Any.String().NotBlank();
    }

    private static AnyList<WitnessSiglum> AnyValidSupportingWitnesses() {
        return Any.ListOf(new AnyWitnessSiglum()).WithCountBetween(1, 6).Distinct();
    }

    private static AnyBoolean AnyValidIsConjecture() {
        return Any.Boolean();
    }

    private static AnyString AnyValidRationale() {
        return Any.String().NotBlank().WithMaxLength(EditorialHypothesis.MaxRationaleLength);
    }

    /// <summary>Draws every parameter from the recipe given for it.</summary>
    private AnyEditorialHypothesis(AnyGuid                id,
                                   AnyPassageReference    passage,
                                   AnyString              proposedReading,
                                   AnyString              editor,
                                   AnyList<WitnessSiglum> supportingWitnesses,
                                   AnyBoolean             isConjecture,
                                   AnyString              rationale) {
        _id                        = id;
        _idRecipe                  = id;
        _passage                   = passage;
        _passageRecipe             = passage;
        _proposedReading           = proposedReading;
        _proposedReadingRecipe     = proposedReading;
        _editor                    = editor;
        _editorRecipe              = editor;
        _supportingWitnesses       = supportingWitnesses;
        _supportingWitnessesRecipe = supportingWitnesses;
        _isConjecture              = isConjecture;
        _isConjectureRecipe        = isConjecture;
        _rationale                 = rationale;
        _rationaleRecipe           = rationale;
    }

    /// <summary>Copies <paramref name="from" />, for a <c>With…</c> method to change one parameter of before handing it out.</summary>
    private AnyEditorialHypothesis(AnyEditorialHypothesis from) {
        _id                        = from._id;
        _idRecipe                  = from._idRecipe;
        _passage                   = from._passage;
        _passageRecipe             = from._passageRecipe;
        _proposedReading           = from._proposedReading;
        _proposedReadingRecipe     = from._proposedReadingRecipe;
        _editor                    = from._editor;
        _editorRecipe              = from._editorRecipe;
        _supportingWitnesses       = from._supportingWitnesses;
        _supportingWitnessesRecipe = from._supportingWitnessesRecipe;
        _isConjecture              = from._isConjecture;
        _isConjectureRecipe        = from._isConjectureRecipe;
        _rationale                 = from._rationale;
        _rationaleRecipe           = from._rationaleRecipe;
    }

    /// <summary>Pins <c>id</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyEditorialHypothesis WithId(Guid value) {
        AnyEditorialHypothesis copy = new AnyEditorialHypothesis(this);

        copy._id = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>id</c> is drawn from — <c>AnyValidId()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyGuid, AnyGuid&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyEditorialHypothesis WithId(Func<AnyGuid, AnyGuid> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyEditorialHypothesis copy = new AnyEditorialHypothesis(this);

        copy._idRecipe = configure(_idRecipe);
        copy._id       = copy._idRecipe;

        return copy;
    }

    /// <summary>Pins <c>passage</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyEditorialHypothesis WithPassage(PassageReference value) {
        AnyEditorialHypothesis copy = new AnyEditorialHypothesis(this);

        copy._passage = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>passage</c> is drawn from — <c>AnyValidPassage()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyPassageReference, AnyPassageReference&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyEditorialHypothesis WithPassage(Func<AnyPassageReference, AnyPassageReference> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyEditorialHypothesis copy = new AnyEditorialHypothesis(this);

        copy._passageRecipe = configure(_passageRecipe);
        copy._passage       = copy._passageRecipe;

        return copy;
    }

    /// <summary>Pins <c>proposedReading</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyEditorialHypothesis WithProposedReading(string value) {
        AnyEditorialHypothesis copy = new AnyEditorialHypothesis(this);

        copy._proposedReading = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>proposedReading</c> is drawn from — <c>AnyValidProposedReading()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyString, AnyString&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyEditorialHypothesis WithProposedReading(Func<AnyString, AnyString> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyEditorialHypothesis copy = new AnyEditorialHypothesis(this);

        copy._proposedReadingRecipe = configure(_proposedReadingRecipe);
        copy._proposedReading       = copy._proposedReadingRecipe;

        return copy;
    }

    /// <summary>Pins <c>editor</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyEditorialHypothesis WithEditor(string value) {
        AnyEditorialHypothesis copy = new AnyEditorialHypothesis(this);

        copy._editor = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>editor</c> is drawn from — <c>AnyValidEditor()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyString, AnyString&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyEditorialHypothesis WithEditor(Func<AnyString, AnyString> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyEditorialHypothesis copy = new AnyEditorialHypothesis(this);

        copy._editorRecipe = configure(_editorRecipe);
        copy._editor       = copy._editorRecipe;

        return copy;
    }

    /// <summary>Pins <c>supportingWitnesses</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyEditorialHypothesis WithSupportingWitnesses(IReadOnlyCollection<WitnessSiglum> value) {
        AnyEditorialHypothesis copy = new AnyEditorialHypothesis(this);

        copy._supportingWitnesses = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>supportingWitnesses</c> is drawn from — <c>AnyValidSupportingWitnesses()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyList&lt;WitnessSiglum&gt;, AnyList&lt;WitnessSiglum&gt;&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyEditorialHypothesis WithSupportingWitnesses(Func<AnyList<WitnessSiglum>, AnyList<WitnessSiglum>> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyEditorialHypothesis copy = new AnyEditorialHypothesis(this);

        copy._supportingWitnessesRecipe = configure(_supportingWitnessesRecipe);
        copy._supportingWitnesses       = copy._supportingWitnessesRecipe;

        return copy;
    }

    /// <summary>Pins <c>isConjecture</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyEditorialHypothesis WithIsConjecture(bool value) {
        AnyEditorialHypothesis copy = new AnyEditorialHypothesis(this);

        copy._isConjecture = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>isConjecture</c> is drawn from — <c>AnyValidIsConjecture()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyBoolean, AnyBoolean&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyEditorialHypothesis WithIsConjecture(Func<AnyBoolean, AnyBoolean> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyEditorialHypothesis copy = new AnyEditorialHypothesis(this);

        copy._isConjectureRecipe = configure(_isConjectureRecipe);
        copy._isConjecture       = copy._isConjectureRecipe;

        return copy;
    }

    /// <summary>Pins <c>rationale</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyEditorialHypothesis WithRationale(string value) {
        AnyEditorialHypothesis copy = new AnyEditorialHypothesis(this);

        copy._rationale = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>rationale</c> is drawn from — <c>AnyValidRationale()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyString, AnyString&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyEditorialHypothesis WithRationale(Func<AnyString, AnyString> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyEditorialHypothesis copy = new AnyEditorialHypothesis(this);

        copy._rationaleRecipe = configure(_rationaleRecipe);
        copy._rationale       = copy._rationaleRecipe;

        return copy;
    }

    /// <summary>Produces one arbitrary <see cref="EditorialHypothesis" />.</summary>
    public EditorialHypothesis Generate() {
        bool conjecturePinned = !ReferenceEquals(_isConjecture, _isConjectureRecipe);
        bool witnessesPinned  = !ReferenceEquals(_supportingWitnesses, _supportingWitnessesRecipe);

        // A conjecture cites no witness and every other reading cites at least one: whichever the test pinned
        // decides the other, and with neither pinned the conjecture is drawn first.
        IReadOnlyCollection<WitnessSiglum> witnesses;
        bool                               isConjecture;
        if (witnessesPinned && !conjecturePinned) {
            witnesses    = _supportingWitnesses.Generate();
            isConjecture = witnesses.Count == 0;
        } else {
            isConjecture = _isConjecture.Generate();
            witnesses    = witnessesPinned ? _supportingWitnesses.Generate()
                         : isConjecture    ? Array.Empty<WitnessSiglum>()
                                           : _supportingWitnessesRecipe.WithMinCount(1).Generate();
        }

        return new EditorialHypothesis(_id.Generate(),
                                       _passage.Generate(),
                                       _proposedReading.Generate(),
                                       _editor.Generate(),
                                       witnesses,
                                       isConjecture,
                                       _rationale.Generate());
    }

}
