// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate Lacuna --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.
// A TODO(any) above a `_ = TODO_verify_…` line names a guard the scaffolder could not read; the recipe
// under it is its best generator for the type. Verify it honours that guard, or replace it, then delete
// those two lines. What the scaffolder reads, and what it cannot:
// https://github.com/Reefact/just-dummies/blob/main/doc/handwritten/for-users/packages/justdummies-cli.en.md#what-the-last-column-means

using System;

using JustDummies;

namespace ManuscriptReconstruction.Domain;

/// <summary>
///     A generator of arbitrary <see cref="Lacuna" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyLacuna : IAny<Lacuna> {

    // A passage is ordered book, then chapter, then line: its position in that order is an int, and a lacuna
    // runs between two positions. Drawing them as an ordered pair is what keeps `from` at or before `to`.
    private const int PassageCount = PassageReference.MaxBook * PassageReference.MaxChapter * PassageReference.MaxLine;

    private IAny<WitnessSiglum> _witness;
    private IAny<string>        _cause;

    private AnyWitnessSiglum _witnessRecipe;
    private AnyString        _causeRecipe;

    private PassageReference? _fromPin;
    private PassageReference? _toPin;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyLacuna()
        : this(witness: AnyValidWitness(),
               cause:   AnyValidCause()) { }

    private static AnyWitnessSiglum AnyValidWitness() {
        return new AnyWitnessSiglum();
    }

    private static AnyString AnyValidCause() {
        return Any.String().NotBlank();
    }

    /// <summary>Draws every parameter from the recipe given for it.</summary>
    private AnyLacuna(AnyWitnessSiglum witness,
                      AnyString        cause) {
        _witness       = witness;
        _witnessRecipe = witness;
        _cause         = cause;
        _causeRecipe   = cause;
    }

    /// <summary>Copies <paramref name="from" />, for a <c>With…</c> method to change one parameter of before handing it out.</summary>
    private AnyLacuna(AnyLacuna from) {
        _witness       = from._witness;
        _witnessRecipe = from._witnessRecipe;
        _cause         = from._cause;
        _causeRecipe   = from._causeRecipe;
        _fromPin       = from._fromPin;
        _toPin         = from._toPin;
    }

    /// <summary>Pins <c>witness</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyLacuna WithWitness(WitnessSiglum value) {
        AnyLacuna copy = new AnyLacuna(this);

        copy._witness = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>witness</c> is drawn from — <c>AnyValidWitness()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyWitnessSiglum, AnyWitnessSiglum&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyLacuna WithWitness(Func<AnyWitnessSiglum, AnyWitnessSiglum> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyLacuna copy = new AnyLacuna(this);

        copy._witnessRecipe = configure(_witnessRecipe);
        copy._witness       = copy._witnessRecipe;

        return copy;
    }

    /// <summary>Pins where the gap starts; where it ends is drawn at or after it unless <see cref="WithTo" /> pins it too.</summary>
    public AnyLacuna WithFrom(PassageReference value) {
        AnyLacuna copy = new AnyLacuna(this);

        copy._fromPin = value;

        return copy;
    }

    /// <summary>Pins where the gap ends; where it starts is drawn at or before it unless <see cref="WithFrom" /> pins it too.</summary>
    public AnyLacuna WithTo(PassageReference value) {
        AnyLacuna copy = new AnyLacuna(this);

        copy._toPin = value;

        return copy;
    }

    /// <summary>Pins <c>cause</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyLacuna WithCause(string value) {
        AnyLacuna copy = new AnyLacuna(this);

        copy._cause = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>cause</c> is drawn from — <c>AnyValidCause()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyString, AnyString&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyLacuna WithCause(Func<AnyString, AnyString> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyLacuna copy = new AnyLacuna(this);

        copy._causeRecipe = configure(_causeRecipe);
        copy._cause       = copy._causeRecipe;

        return copy;
    }

    /// <summary>Produces one arbitrary <see cref="Lacuna" />.</summary>
    public Lacuna Generate() {
        AnyOrderedPair<int> span = Any.OrderedPair(Any.Int32().Between(0, PassageCount - 1), Any.Int32().Between(0, PassageCount - 1));
        if (_fromPin is not null) { span = span.WithFirst(PositionOf(_fromPin)); }
        if (_toPin is not null) { span = span.WithSecond(PositionOf(_toPin)); }

        (int from, int to) = span.Generate();

        return new Lacuna(_witness.Generate(),
                          _fromPin ?? PassageAt(from),
                          _toPin ?? PassageAt(to),
                          _cause.Generate());
    }

    private static int PositionOf(PassageReference passage) {
        return ((passage.Book - 1) * PassageReference.MaxChapter + (passage.Chapter - 1)) * PassageReference.MaxLine + (passage.Line - 1);
    }

    private static PassageReference PassageAt(int position) {
        int line    = position % PassageReference.MaxLine;
        int chapter = position / PassageReference.MaxLine % PassageReference.MaxChapter;
        int book    = position / PassageReference.MaxLine / PassageReference.MaxChapter;

        return new PassageReference(book + 1, chapter + 1, line + 1);
    }

}
