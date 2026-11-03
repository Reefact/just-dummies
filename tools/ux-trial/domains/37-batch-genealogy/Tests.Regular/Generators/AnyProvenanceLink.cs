// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate ProvenanceLink --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.
// A TODO(any) above a `_ = TODO_verify_…` line names a guard the scaffolder could not read; the recipe
// under it is its best generator for the type. Verify it honours that guard, or replace it, then delete
// those two lines. What the scaffolder reads, and what it cannot:
// https://github.com/Reefact/just-dummies/blob/main/doc/handwritten/for-users/packages/justdummies-cli.en.md#what-the-last-column-means

using System;

using JustDummies;

namespace BatchGenealogy.Domain;

/// <summary>
///     A generator of arbitrary <see cref="ProvenanceLink" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyProvenanceLink : IAny<ProvenanceLink> {

    private IAny<LotNumber> _source;
    private IAny<LotNumber> _target;
    private IAny<string>    _transformationId;
    private IAny<decimal>   _share;

    private AnyLotNumber _sourceRecipe;
    private AnyLotNumber _targetRecipe;
    private AnyString    _transformationIdRecipe;
    private AnyDecimal   _shareRecipe;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyProvenanceLink()
        : this(source:           AnyValidSource(),
               target:           AnyValidTarget(),
               transformationId: AnyValidTransformationId(),
               share:            AnyValidShare()) { }

    private static AnyLotNumber AnyValidSource() {
        return new AnyLotNumber();
    }

    private static AnyLotNumber AnyValidTarget() {
        return new AnyLotNumber();
    }

    private static AnyString AnyValidTransformationId() {
        return Any.String().NotBlank();
    }

    private static AnyDecimal AnyValidShare() {
        return Any.Decimal().Positive().LessThanOrEqualTo(1m);
    }

    /// <summary>Draws every parameter from the recipe given for it.</summary>
    private AnyProvenanceLink(AnyLotNumber source,
                              AnyLotNumber target,
                              AnyString    transformationId,
                              AnyDecimal   share) {
        _source                 = source;
        _sourceRecipe           = source;
        _target                 = target;
        _targetRecipe           = target;
        _transformationId       = transformationId;
        _transformationIdRecipe = transformationId;
        _share                  = share;
        _shareRecipe            = share;
    }

    /// <summary>Copies <paramref name="from" />, for a <c>With…</c> method to change one parameter of before handing it out.</summary>
    private AnyProvenanceLink(AnyProvenanceLink from) {
        _source                 = from._source;
        _sourceRecipe           = from._sourceRecipe;
        _target                 = from._target;
        _targetRecipe           = from._targetRecipe;
        _transformationId       = from._transformationId;
        _transformationIdRecipe = from._transformationIdRecipe;
        _share                  = from._share;
        _shareRecipe            = from._shareRecipe;
    }

    /// <summary>Pins <c>source</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyProvenanceLink WithSource(LotNumber value) {
        AnyProvenanceLink copy = new AnyProvenanceLink(this);

        copy._source = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>source</c> is drawn from — <c>AnyValidSource()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyLotNumber, AnyLotNumber&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyProvenanceLink WithSource(Func<AnyLotNumber, AnyLotNumber> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyProvenanceLink copy = new AnyProvenanceLink(this);

        copy._sourceRecipe = configure(_sourceRecipe);
        copy._source       = copy._sourceRecipe;

        return copy;
    }

    /// <summary>Pins <c>target</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyProvenanceLink WithTarget(LotNumber value) {
        AnyProvenanceLink copy = new AnyProvenanceLink(this);

        copy._target = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>target</c> is drawn from — <c>AnyValidTarget()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyLotNumber, AnyLotNumber&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyProvenanceLink WithTarget(Func<AnyLotNumber, AnyLotNumber> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyProvenanceLink copy = new AnyProvenanceLink(this);

        copy._targetRecipe = configure(_targetRecipe);
        copy._target       = copy._targetRecipe;

        return copy;
    }

    /// <summary>Pins <c>transformationId</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyProvenanceLink WithTransformationId(string value) {
        AnyProvenanceLink copy = new AnyProvenanceLink(this);

        copy._transformationId = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>transformationId</c> is drawn from — <c>AnyValidTransformationId()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyString, AnyString&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyProvenanceLink WithTransformationId(Func<AnyString, AnyString> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyProvenanceLink copy = new AnyProvenanceLink(this);

        copy._transformationIdRecipe = configure(_transformationIdRecipe);
        copy._transformationId       = copy._transformationIdRecipe;

        return copy;
    }

    /// <summary>Pins <c>share</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyProvenanceLink WithShare(decimal value) {
        AnyProvenanceLink copy = new AnyProvenanceLink(this);

        copy._share = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>share</c> is drawn from — <c>AnyValidShare()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyDecimal, AnyDecimal&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyProvenanceLink WithShare(Func<AnyDecimal, AnyDecimal> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyProvenanceLink copy = new AnyProvenanceLink(this);

        copy._shareRecipe = configure(_shareRecipe);
        copy._share       = copy._shareRecipe;

        return copy;
    }

    /// <summary>Produces one arbitrary <see cref="ProvenanceLink" />.</summary>
    public ProvenanceLink Generate() {
        LotNumber source = _source.Generate();
        LotNumber target = _target.DifferentFrom(source).Generate();

        return new ProvenanceLink(source,
                                  target,
                                  _transformationId.Generate(),
                                  _share.Generate());
    }

}
