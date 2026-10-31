// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate AttestedReading --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.

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

    private readonly IAny<PassageReference>                   _passage;
    private readonly IAny<string>                             _text;
    private readonly IAny<IReadOnlyCollection<WitnessSiglum>> _witnesses;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyAttestedReading()
        : this(passage:   AnyValidPassage(),
               text:      AnyValidText(),
               witnesses: AnyValidWitnesses()) { }

    private static AnyPassageReference AnyValidPassage() {
        return new AnyPassageReference();
    }

    private static AnyString AnyValidText() {
        return Any.String().NonEmpty();
    }

    private static AnyList<WitnessSiglum> AnyValidWitnesses() {
        return Any.ListOf(new AnyWitnessSiglum()).WithCountBetween(1, 5).Distinct();
    }

    private AnyAttestedReading(IAny<PassageReference>                   passage,
                               IAny<string>                             text,
                               IAny<IReadOnlyCollection<WitnessSiglum>> witnesses) {
        _passage   = passage;
        _text      = text;
        _witnesses = witnesses;
    }

    /// <summary>Pins <c>passage</c> to a fixed value.</summary>
    public AnyAttestedReading WithPassage(PassageReference value) {
        return new AnyAttestedReading(new FixedValue<PassageReference>(value), _text, _witnesses);
    }

    /// <summary>Draws <c>passage</c> from <c>AnyValidPassage()</c>, refined by <paramref name="configure" />.</summary>
    public AnyAttestedReading WithPassage(Func<AnyPassageReference, AnyPassageReference> configure) {
        return new AnyAttestedReading(configure(AnyValidPassage()), _text, _witnesses);
    }

    /// <summary>Pins <c>text</c> to a fixed value.</summary>
    public AnyAttestedReading WithText(string value) {
        return new AnyAttestedReading(_passage, new FixedValue<string>(value), _witnesses);
    }

    /// <summary>Draws <c>text</c> from <c>AnyValidText()</c>, refined by <paramref name="configure" />.</summary>
    public AnyAttestedReading WithText(Func<AnyString, AnyString> configure) {
        return new AnyAttestedReading(_passage, configure(AnyValidText()), _witnesses);
    }

    /// <summary>Pins <c>witnesses</c> to a fixed value.</summary>
    public AnyAttestedReading WithWitnesses(IReadOnlyCollection<WitnessSiglum> value) {
        return new AnyAttestedReading(_passage, _text, new FixedValue<IReadOnlyCollection<WitnessSiglum>>(value));
    }

    /// <summary>Draws <c>witnesses</c> from <c>AnyValidWitnesses()</c>, refined by <paramref name="configure" />.</summary>
    public AnyAttestedReading WithWitnesses(Func<AnyList<WitnessSiglum>, AnyList<WitnessSiglum>> configure) {
        return new AnyAttestedReading(_passage, _text, configure(AnyValidWitnesses()));
    }

    /// <summary>Produces one arbitrary <see cref="AttestedReading" />.</summary>
    public AttestedReading Generate() {
        return new AttestedReading(_passage.Generate(),
                                   _text.Generate(),
                                   _witnesses.Generate());
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
