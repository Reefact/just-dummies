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

    private IAny<WitnessSiglum>    _witness;
    private IAny<PassageReference> _from;
    private IAny<PassageReference> _to;
    private IAny<string>           _cause;

    private AnyWitnessSiglum    _witnessRecipe;
    private AnyPassageReference _fromRecipe;
    private AnyPassageReference _toRecipe;
    private AnyString           _causeRecipe;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyLacuna()
        : this(witness: AnyValidWitness(),
               from:    AnyValidFrom(),
               to:      AnyValidTo(),
               cause:   AnyValidCause()) { }

    private static AnyWitnessSiglum AnyValidWitness() {
        return new AnyWitnessSiglum();
    }

    private static AnyPassageReference AnyValidFrom() {
        return new AnyPassageReference();
    }

    private static AnyPassageReference AnyValidTo() {
        return new AnyPassageReference();
    }

    private static AnyString AnyValidCause() {
        return Any.String().NotBlank();
    }

    /// <summary>Draws every parameter from the recipe given for it.</summary>
    private AnyLacuna(AnyWitnessSiglum    witness,
                      AnyPassageReference from,
                      AnyPassageReference to,
                      AnyString           cause) {
        _witness       = witness;
        _witnessRecipe = witness;
        _from          = from;
        _fromRecipe    = from;
        _to            = to;
        _toRecipe      = to;
        _cause         = cause;
        _causeRecipe   = cause;
    }

    /// <summary>Copies <paramref name="from" />, for a <c>With…</c> method to change one parameter of before handing it out.</summary>
    private AnyLacuna(AnyLacuna from) {
        _witness       = from._witness;
        _witnessRecipe = from._witnessRecipe;
        _from          = from._from;
        _fromRecipe    = from._fromRecipe;
        _to            = from._to;
        _toRecipe      = from._toRecipe;
        _cause         = from._cause;
        _causeRecipe   = from._causeRecipe;
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

    /// <summary>Pins <c>from</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyLacuna WithFrom(PassageReference value) {
        AnyLacuna copy = new AnyLacuna(this);

        copy._from = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>from</c> is drawn from — <c>AnyValidFrom()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyPassageReference, AnyPassageReference&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyLacuna WithFrom(Func<AnyPassageReference, AnyPassageReference> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyLacuna copy = new AnyLacuna(this);

        copy._fromRecipe = configure(_fromRecipe);
        copy._from       = copy._fromRecipe;

        return copy;
    }

    /// <summary>Pins <c>to</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyLacuna WithTo(PassageReference value) {
        AnyLacuna copy = new AnyLacuna(this);

        copy._to = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>to</c> is drawn from — <c>AnyValidTo()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyPassageReference, AnyPassageReference&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyLacuna WithTo(Func<AnyPassageReference, AnyPassageReference> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyLacuna copy = new AnyLacuna(this);

        copy._toRecipe = configure(_toRecipe);
        copy._to       = copy._toRecipe;

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
        PassageReference first  = _from.Generate();
        PassageReference second = _to.Generate();

        // A lacuna spans from a passage to a later (or the same) one.
        PassageReference from = first.CompareTo(second) <= 0 ? first : second;
        PassageReference to   = first.CompareTo(second) <= 0 ? second : first;

        return new Lacuna(_witness.Generate(),
                          from,
                          to,
                          _cause.Generate());
    }

}
