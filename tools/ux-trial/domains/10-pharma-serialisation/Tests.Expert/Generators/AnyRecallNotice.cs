// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate RecallNotice --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.
// A TODO(any) above a `_ = TODO_verify_…` line names a guard the scaffolder could not read; the recipe
// under it is its best generator for the type. Verify it honours that guard, or replace it, then delete
// those two lines. What the scaffolder reads, and what it cannot:
// https://github.com/Reefact/just-dummies/blob/main/doc/handwritten/for-users/packages/justdummies-cli.en.md#what-the-last-column-means

using System;

using JustDummies;

namespace Serialisation.Domain;

/// <summary>
///     A generator of arbitrary <see cref="RecallNotice" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyRecallNotice : IAny<RecallNotice> {

    private IAny<BatchNumber> _batch;
    private AnyOrderedPair<DateOnly> _issuedOnAndEffectiveFrom;
    private IAny<RecallClass> _recallClass;
    private IAny<string>      _reason;

    private AnyBatchNumber       _batchRecipe;
    private AnyDateOnly          _issuedOnRecipe;
    private AnyDateOnly          _effectiveFromRecipe;
    private AnyEnum<RecallClass> _recallClassRecipe;
    private AnyString            _reasonRecipe;

    private DateOnly? _issuedOnPin;
    private DateOnly? _effectiveFromPin;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyRecallNotice()
        : this(batch:         AnyValidBatch(),
               issuedOn:      AnyValidIssuedOn(),
               effectiveFrom: AnyValidEffectiveFrom(),
               recallClass:   AnyValidRecallClass(),
               reason:        AnyValidReason()) { }

    private static AnyBatchNumber AnyValidBatch() {
        return new AnyBatchNumber();
    }

    private static AnyDateOnly AnyValidIssuedOn() {
        return Any.DateOnly().Between(new DateOnly(2000, 1, 1), new DateOnly(2099, 12, 31));
    }

    private static AnyDateOnly AnyValidEffectiveFrom() {
        return Any.DateOnly().Between(new DateOnly(2000, 1, 1), new DateOnly(2099, 12, 31));
    }

    private static AnyEnum<RecallClass> AnyValidRecallClass() {
        return Any.Enum<RecallClass>();
    }

    private static AnyString AnyValidReason() {
        return Any.String().NonEmpty().WithMaxLength(500);
    }

    /// <summary>Draws every parameter from the recipe given for it.</summary>
    private AnyRecallNotice(AnyBatchNumber       batch,
                            AnyDateOnly          issuedOn,
                            AnyDateOnly          effectiveFrom,
                            AnyEnum<RecallClass> recallClass,
                            AnyString            reason) {
        _batch               = batch;
        _batchRecipe         = batch;
        _issuedOnRecipe      = issuedOn;
        _effectiveFromRecipe = effectiveFrom;
        _recallClass         = recallClass;
        _recallClassRecipe   = recallClass;
        _reason              = reason;
        _reasonRecipe        = reason;
        _issuedOnAndEffectiveFrom = IssuedOnAndEffectiveFrom();
    }

    /// <summary>Copies <paramref name="from" />, for a <c>With…</c> method to change one parameter of before handing it out.</summary>
    private AnyRecallNotice(AnyRecallNotice from) {
        _batch               = from._batch;
        _batchRecipe         = from._batchRecipe;
        _issuedOnAndEffectiveFrom = from._issuedOnAndEffectiveFrom;
        _issuedOnRecipe      = from._issuedOnRecipe;
        _issuedOnPin         = from._issuedOnPin;
        _effectiveFromRecipe = from._effectiveFromRecipe;
        _effectiveFromPin    = from._effectiveFromPin;
        _recallClass         = from._recallClass;
        _recallClassRecipe   = from._recallClassRecipe;
        _reason              = from._reason;
        _reasonRecipe        = from._reasonRecipe;
    }

    /// <summary>Draws <c>issuedOn</c> and <c>effectiveFrom</c> together, <c>effectiveFrom</c> never before <c>issuedOn</c> (the same day is accepted), each from its current recipe and pin.</summary>
    private AnyOrderedPair<DateOnly> IssuedOnAndEffectiveFrom() {
        AnyOrderedPair<DateOnly> pair = Any.OrderedPair(_issuedOnRecipe, _effectiveFromRecipe);

        if (_issuedOnPin is { } issuedOn) { pair = pair.WithFirst(issuedOn); }
        if (_effectiveFromPin is { } effectiveFrom) { pair = pair.WithSecond(effectiveFrom); }

        return pair;
    }

    /// <summary>Pins <c>batch</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyRecallNotice WithBatch(BatchNumber value) {
        AnyRecallNotice copy = new AnyRecallNotice(this);

        copy._batch = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>batch</c> is drawn from — <c>AnyValidBatch()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyBatchNumber, AnyBatchNumber&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyRecallNotice WithBatch(Func<AnyBatchNumber, AnyBatchNumber> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyRecallNotice copy = new AnyRecallNotice(this);

        copy._batchRecipe = configure(_batchRecipe);
        copy._batch       = copy._batchRecipe;

        return copy;
    }

    /// <summary>Pins <c>issuedOn</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyRecallNotice WithIssuedOn(DateOnly value) {
        AnyRecallNotice copy = new AnyRecallNotice(this);

        copy._issuedOnPin                = value;
        copy._issuedOnAndEffectiveFrom = copy.IssuedOnAndEffectiveFrom();

        return copy;
    }

    /// <summary>Refines the recipe <c>issuedOn</c> is drawn from — <c>AnyValidIssuedOn()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyDateOnly, AnyDateOnly&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyRecallNotice WithIssuedOn(Func<AnyDateOnly, AnyDateOnly> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyRecallNotice copy = new AnyRecallNotice(this);

        copy._issuedOnRecipe = configure(_issuedOnRecipe);
        copy._issuedOnPin    = null;
        copy._issuedOnAndEffectiveFrom = copy.IssuedOnAndEffectiveFrom();

        return copy;
    }

    /// <summary>Pins <c>effectiveFrom</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyRecallNotice WithEffectiveFrom(DateOnly value) {
        AnyRecallNotice copy = new AnyRecallNotice(this);

        copy._effectiveFromPin         = value;
        copy._issuedOnAndEffectiveFrom = copy.IssuedOnAndEffectiveFrom();

        return copy;
    }

    /// <summary>Refines the recipe <c>effectiveFrom</c> is drawn from — <c>AnyValidEffectiveFrom()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyDateOnly, AnyDateOnly&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyRecallNotice WithEffectiveFrom(Func<AnyDateOnly, AnyDateOnly> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyRecallNotice copy = new AnyRecallNotice(this);

        copy._effectiveFromRecipe = configure(_effectiveFromRecipe);
        copy._effectiveFromPin    = null;
        copy._issuedOnAndEffectiveFrom = copy.IssuedOnAndEffectiveFrom();

        return copy;
    }

    /// <summary>Pins <c>recallClass</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyRecallNotice WithRecallClass(RecallClass value) {
        AnyRecallNotice copy = new AnyRecallNotice(this);

        copy._recallClass = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>recallClass</c> is drawn from — <c>AnyValidRecallClass()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyEnum&lt;RecallClass&gt;, AnyEnum&lt;RecallClass&gt;&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyRecallNotice WithRecallClass(Func<AnyEnum<RecallClass>, AnyEnum<RecallClass>> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyRecallNotice copy = new AnyRecallNotice(this);

        copy._recallClassRecipe = configure(_recallClassRecipe);
        copy._recallClass       = copy._recallClassRecipe;

        return copy;
    }

    /// <summary>Pins <c>reason</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyRecallNotice WithReason(string value) {
        AnyRecallNotice copy = new AnyRecallNotice(this);

        copy._reason = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>reason</c> is drawn from — <c>AnyValidReason()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyString, AnyString&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyRecallNotice WithReason(Func<AnyString, AnyString> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyRecallNotice copy = new AnyRecallNotice(this);

        copy._reasonRecipe = configure(_reasonRecipe);
        copy._reason       = copy._reasonRecipe;

        return copy;
    }

    /// <summary>Produces one arbitrary <see cref="RecallNotice" />.</summary>
    public RecallNotice Generate() {
        (DateOnly issuedOn, DateOnly effectiveFrom) = _issuedOnAndEffectiveFrom.Generate();

        return new RecallNotice(_batch.Generate(),
                                issuedOn,
                                effectiveFrom,
                                _recallClass.Generate(),
                                _reason.Generate());
    }

}
