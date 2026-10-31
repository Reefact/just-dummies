// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate EditorialHypothesis --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.

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

    private readonly IAny<Guid>                               _id;
    private readonly IAny<PassageReference>                   _passage;
    private readonly IAny<string>                             _proposedReading;
    private readonly IAny<string>                             _editor;
    private readonly IAny<IReadOnlyCollection<WitnessSiglum>> _supportingWitnesses;
    private readonly IAny<bool>                               _isConjecture;
    private readonly IAny<string>                             _rationale;

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
        return Any.String().NonEmpty();
    }

    private static AnyString AnyValidEditor() {
        return Any.String().NotBlank();
    }

    private static AnyList<WitnessSiglum> AnyValidSupportingWitnesses() {
        // Not a conjecture by default, so at least one distinct witness.
        return Any.ListOf(new AnyWitnessSiglum()).WithCountBetween(1, 3).Distinct();
    }

    private static AnyBoolean AnyValidIsConjecture() {
        // A conjecture cites no witness: ask for one with WithIsConjecture(true) and an empty witness list.
        return Any.Boolean().False();
    }

    private static AnyString AnyValidRationale() {
        return Any.String().NotBlank().WithMaxLength(EditorialHypothesis.MaxRationaleLength);
    }

    private AnyEditorialHypothesis(IAny<Guid>                               id,
                                   IAny<PassageReference>                   passage,
                                   IAny<string>                             proposedReading,
                                   IAny<string>                             editor,
                                   IAny<IReadOnlyCollection<WitnessSiglum>> supportingWitnesses,
                                   IAny<bool>                               isConjecture,
                                   IAny<string>                             rationale) {
        _id                  = id;
        _passage             = passage;
        _proposedReading     = proposedReading;
        _editor              = editor;
        _supportingWitnesses = supportingWitnesses;
        _isConjecture        = isConjecture;
        _rationale           = rationale;
    }

    /// <summary>Pins <c>id</c> to a fixed value.</summary>
    public AnyEditorialHypothesis WithId(Guid value) {
        return new AnyEditorialHypothesis(new FixedValue<Guid>(value), _passage, _proposedReading, _editor, _supportingWitnesses, _isConjecture, _rationale);
    }

    /// <summary>Draws <c>id</c> from <c>AnyValidId()</c>, refined by <paramref name="configure" />.</summary>
    public AnyEditorialHypothesis WithId(Func<AnyGuid, AnyGuid> configure) {
        return new AnyEditorialHypothesis(configure(AnyValidId()), _passage, _proposedReading, _editor, _supportingWitnesses, _isConjecture, _rationale);
    }

    /// <summary>Pins <c>passage</c> to a fixed value.</summary>
    public AnyEditorialHypothesis WithPassage(PassageReference value) {
        return new AnyEditorialHypothesis(_id, new FixedValue<PassageReference>(value), _proposedReading, _editor, _supportingWitnesses, _isConjecture, _rationale);
    }

    /// <summary>Draws <c>passage</c> from <c>AnyValidPassage()</c>, refined by <paramref name="configure" />.</summary>
    public AnyEditorialHypothesis WithPassage(Func<AnyPassageReference, AnyPassageReference> configure) {
        return new AnyEditorialHypothesis(_id, configure(AnyValidPassage()), _proposedReading, _editor, _supportingWitnesses, _isConjecture, _rationale);
    }

    /// <summary>Pins <c>proposedReading</c> to a fixed value.</summary>
    public AnyEditorialHypothesis WithProposedReading(string value) {
        return new AnyEditorialHypothesis(_id, _passage, new FixedValue<string>(value), _editor, _supportingWitnesses, _isConjecture, _rationale);
    }

    /// <summary>Draws <c>proposedReading</c> from <c>AnyValidProposedReading()</c>, refined by <paramref name="configure" />.</summary>
    public AnyEditorialHypothesis WithProposedReading(Func<AnyString, AnyString> configure) {
        return new AnyEditorialHypothesis(_id, _passage, configure(AnyValidProposedReading()), _editor, _supportingWitnesses, _isConjecture, _rationale);
    }

    /// <summary>Pins <c>editor</c> to a fixed value.</summary>
    public AnyEditorialHypothesis WithEditor(string value) {
        return new AnyEditorialHypothesis(_id, _passage, _proposedReading, new FixedValue<string>(value), _supportingWitnesses, _isConjecture, _rationale);
    }

    /// <summary>Draws <c>editor</c> from <c>AnyValidEditor()</c>, refined by <paramref name="configure" />.</summary>
    public AnyEditorialHypothesis WithEditor(Func<AnyString, AnyString> configure) {
        return new AnyEditorialHypothesis(_id, _passage, _proposedReading, configure(AnyValidEditor()), _supportingWitnesses, _isConjecture, _rationale);
    }

    /// <summary>Pins <c>supportingWitnesses</c> to a fixed value.</summary>
    public AnyEditorialHypothesis WithSupportingWitnesses(IReadOnlyCollection<WitnessSiglum> value) {
        return new AnyEditorialHypothesis(_id, _passage, _proposedReading, _editor, new FixedValue<IReadOnlyCollection<WitnessSiglum>>(value), _isConjecture, _rationale);
    }

    /// <summary>Draws <c>supportingWitnesses</c> from <c>AnyValidSupportingWitnesses()</c>, refined by <paramref name="configure" />.</summary>
    public AnyEditorialHypothesis WithSupportingWitnesses(Func<AnyList<WitnessSiglum>, AnyList<WitnessSiglum>> configure) {
        return new AnyEditorialHypothesis(_id, _passage, _proposedReading, _editor, configure(AnyValidSupportingWitnesses()), _isConjecture, _rationale);
    }

    /// <summary>Pins <c>isConjecture</c> to a fixed value.</summary>
    public AnyEditorialHypothesis WithIsConjecture(bool value) {
        return new AnyEditorialHypothesis(_id, _passage, _proposedReading, _editor, _supportingWitnesses, new FixedValue<bool>(value), _rationale);
    }

    /// <summary>Draws <c>isConjecture</c> from <c>AnyValidIsConjecture()</c>, refined by <paramref name="configure" />.</summary>
    public AnyEditorialHypothesis WithIsConjecture(Func<AnyBoolean, AnyBoolean> configure) {
        return new AnyEditorialHypothesis(_id, _passage, _proposedReading, _editor, _supportingWitnesses, configure(AnyValidIsConjecture()), _rationale);
    }

    /// <summary>Pins <c>rationale</c> to a fixed value.</summary>
    public AnyEditorialHypothesis WithRationale(string value) {
        return new AnyEditorialHypothesis(_id, _passage, _proposedReading, _editor, _supportingWitnesses, _isConjecture, new FixedValue<string>(value));
    }

    /// <summary>Draws <c>rationale</c> from <c>AnyValidRationale()</c>, refined by <paramref name="configure" />.</summary>
    public AnyEditorialHypothesis WithRationale(Func<AnyString, AnyString> configure) {
        return new AnyEditorialHypothesis(_id, _passage, _proposedReading, _editor, _supportingWitnesses, _isConjecture, configure(AnyValidRationale()));
    }

    /// <summary>Produces one arbitrary <see cref="EditorialHypothesis" />.</summary>
    public EditorialHypothesis Generate() {
        return new EditorialHypothesis(_id.Generate(),
                                       _passage.Generate(),
                                       _proposedReading.Generate(),
                                       _editor.Generate(),
                                       _supportingWitnesses.Generate(),
                                       _isConjecture.Generate(),
                                       _rationale.Generate());
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
