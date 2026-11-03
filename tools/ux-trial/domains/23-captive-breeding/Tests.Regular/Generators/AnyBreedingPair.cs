// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate BreedingPair --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.
// A TODO(any) above a `_ = TODO_verify_…` line names a guard the scaffolder could not read; the recipe
// under it is its best generator for the type. Verify it honours that guard, or replace it, then delete
// those two lines. What the scaffolder reads, and what it cannot:
// https://github.com/Reefact/just-dummies/blob/main/doc/handwritten/for-users/packages/justdummies-cli.en.md#what-the-last-column-means

using System;

using JustDummies;

namespace CaptiveBreeding.Domain;

/// <summary>
///     A generator of arbitrary <see cref="BreedingPair" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyBreedingPair : IAny<BreedingPair> {

    private IAny<StudbookId>         _sireId;
    private IAny<StudbookId>         _damId;
    private IAny<KinshipCoefficient> _kinship;

    private AnyStudbookId         _sireIdRecipe;
    private AnyStudbookId         _damIdRecipe;
    private AnyKinshipCoefficient _kinshipRecipe;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyBreedingPair()
        : this(sireId:  AnyValidSireId(),
               damId:   AnyValidDamId(),
               kinship: AnyValidKinship()) { }

    private static AnyStudbookId AnyValidSireId() {
        // sire and dam are drawn from disjoint institution alphabets, so they can never be the same id.
        return new AnyStudbookId().WithInstitutionLetters("A-M");
    }

    private static AnyStudbookId AnyValidDamId() {
        return new AnyStudbookId().WithInstitutionLetters("N-Z");
    }

    private static AnyKinshipCoefficient AnyValidKinship() {
        return new AnyKinshipCoefficient().WithValue(v => v.LessThanOrEqualTo(0.5));
    }

    /// <summary>Draws every parameter from the recipe given for it.</summary>
    private AnyBreedingPair(AnyStudbookId         sireId,
                            AnyStudbookId         damId,
                            AnyKinshipCoefficient kinship) {
        _sireId        = sireId;
        _sireIdRecipe  = sireId;
        _damId         = damId;
        _damIdRecipe   = damId;
        _kinship       = kinship;
        _kinshipRecipe = kinship;
    }

    /// <summary>Copies <paramref name="from" />, for a <c>With…</c> method to change one parameter of before handing it out.</summary>
    private AnyBreedingPair(AnyBreedingPair from) {
        _sireId        = from._sireId;
        _sireIdRecipe  = from._sireIdRecipe;
        _damId         = from._damId;
        _damIdRecipe   = from._damIdRecipe;
        _kinship       = from._kinship;
        _kinshipRecipe = from._kinshipRecipe;
    }

    /// <summary>Pins <c>sireId</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyBreedingPair WithSireId(StudbookId value) {
        AnyBreedingPair copy = new AnyBreedingPair(this);

        copy._sireId = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>sireId</c> is drawn from — <c>AnyValidSireId()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyStudbookId, AnyStudbookId&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyBreedingPair WithSireId(Func<AnyStudbookId, AnyStudbookId> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyBreedingPair copy = new AnyBreedingPair(this);

        copy._sireIdRecipe = configure(_sireIdRecipe);
        copy._sireId       = copy._sireIdRecipe;

        return copy;
    }

    /// <summary>Pins <c>damId</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyBreedingPair WithDamId(StudbookId value) {
        AnyBreedingPair copy = new AnyBreedingPair(this);

        copy._damId = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>damId</c> is drawn from — <c>AnyValidDamId()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyStudbookId, AnyStudbookId&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyBreedingPair WithDamId(Func<AnyStudbookId, AnyStudbookId> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyBreedingPair copy = new AnyBreedingPair(this);

        copy._damIdRecipe = configure(_damIdRecipe);
        copy._damId       = copy._damIdRecipe;

        return copy;
    }

    /// <summary>Pins <c>kinship</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyBreedingPair WithKinship(KinshipCoefficient value) {
        AnyBreedingPair copy = new AnyBreedingPair(this);

        copy._kinship = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>kinship</c> is drawn from — <c>AnyValidKinship()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyKinshipCoefficient, AnyKinshipCoefficient&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyBreedingPair WithKinship(Func<AnyKinshipCoefficient, AnyKinshipCoefficient> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyBreedingPair copy = new AnyBreedingPair(this);

        copy._kinshipRecipe = configure(_kinshipRecipe);
        copy._kinship       = copy._kinshipRecipe;

        return copy;
    }

    /// <summary>Produces one arbitrary <see cref="BreedingPair" />.</summary>
    public BreedingPair Generate() {
        return new BreedingPair(_sireId.Generate(),
                                _damId.Generate(),
                                _kinship.Generate());
    }

}
