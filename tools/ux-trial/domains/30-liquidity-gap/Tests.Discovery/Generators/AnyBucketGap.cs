// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate BucketGap --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.

using System;

using JustDummies;

namespace LiquidityGap.Domain;

/// <summary>
///     A generator of arbitrary <see cref="BucketGap" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyBucketGap : IAny<BucketGap> {

    private IAny<TimeBucket> _bucket;
    private IAny<decimal>    _inflows;
    private IAny<decimal>    _outflows;
    private IAny<decimal>    _cumulativeGap;

    private AnyTimeBucket _bucketRecipe;
    private AnyDecimal    _inflowsRecipe;
    private AnyDecimal    _outflowsRecipe;
    private AnyDecimal    _cumulativeGapRecipe;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyBucketGap()
        : this(bucket:        AnyValidBucket(),
               inflows:       AnyValidInflows(),
               outflows:      AnyValidOutflows(),
               cumulativeGap: AnyValidCumulativeGap()) { }

    private static AnyTimeBucket AnyValidBucket() {
        return new AnyTimeBucket();
    }

    private static AnyDecimal AnyValidInflows() {
        return Any.Decimal().GreaterThanOrEqualTo(0m);
    }

    private static AnyDecimal AnyValidOutflows() {
        return Any.Decimal().GreaterThanOrEqualTo(0m);
    }

    private static AnyDecimal AnyValidCumulativeGap() {
        return Any.Decimal();
    }

    /// <summary>Draws every parameter from the recipe given for it.</summary>
    private AnyBucketGap(AnyTimeBucket bucket,
                         AnyDecimal    inflows,
                         AnyDecimal    outflows,
                         AnyDecimal    cumulativeGap) {
        _bucket              = bucket;
        _bucketRecipe        = bucket;
        _inflows             = inflows;
        _inflowsRecipe       = inflows;
        _outflows            = outflows;
        _outflowsRecipe      = outflows;
        _cumulativeGap       = cumulativeGap;
        _cumulativeGapRecipe = cumulativeGap;
    }

    /// <summary>Copies <paramref name="from" />, for a <c>With…</c> method to change one parameter of before handing it out.</summary>
    private AnyBucketGap(AnyBucketGap from) {
        _bucket              = from._bucket;
        _bucketRecipe        = from._bucketRecipe;
        _inflows             = from._inflows;
        _inflowsRecipe       = from._inflowsRecipe;
        _outflows            = from._outflows;
        _outflowsRecipe      = from._outflowsRecipe;
        _cumulativeGap       = from._cumulativeGap;
        _cumulativeGapRecipe = from._cumulativeGapRecipe;
    }

    /// <summary>Pins <c>bucket</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyBucketGap WithBucket(TimeBucket value) {
        AnyBucketGap copy = new AnyBucketGap(this);

        copy._bucket = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>bucket</c> is drawn from — <c>AnyValidBucket()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyTimeBucket, AnyTimeBucket&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyBucketGap WithBucket(Func<AnyTimeBucket, AnyTimeBucket> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyBucketGap copy = new AnyBucketGap(this);

        copy._bucketRecipe = configure(_bucketRecipe);
        copy._bucket       = copy._bucketRecipe;

        return copy;
    }

    /// <summary>Pins <c>inflows</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyBucketGap WithInflows(decimal value) {
        AnyBucketGap copy = new AnyBucketGap(this);

        copy._inflows = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>inflows</c> is drawn from — <c>AnyValidInflows()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyDecimal, AnyDecimal&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyBucketGap WithInflows(Func<AnyDecimal, AnyDecimal> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyBucketGap copy = new AnyBucketGap(this);

        copy._inflowsRecipe = configure(_inflowsRecipe);
        copy._inflows       = copy._inflowsRecipe;

        return copy;
    }

    /// <summary>Pins <c>outflows</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyBucketGap WithOutflows(decimal value) {
        AnyBucketGap copy = new AnyBucketGap(this);

        copy._outflows = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>outflows</c> is drawn from — <c>AnyValidOutflows()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyDecimal, AnyDecimal&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyBucketGap WithOutflows(Func<AnyDecimal, AnyDecimal> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyBucketGap copy = new AnyBucketGap(this);

        copy._outflowsRecipe = configure(_outflowsRecipe);
        copy._outflows       = copy._outflowsRecipe;

        return copy;
    }

    /// <summary>Pins <c>cumulativeGap</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyBucketGap WithCumulativeGap(decimal value) {
        AnyBucketGap copy = new AnyBucketGap(this);

        copy._cumulativeGap = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>cumulativeGap</c> is drawn from — <c>AnyValidCumulativeGap()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyDecimal, AnyDecimal&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyBucketGap WithCumulativeGap(Func<AnyDecimal, AnyDecimal> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyBucketGap copy = new AnyBucketGap(this);

        copy._cumulativeGapRecipe = configure(_cumulativeGapRecipe);
        copy._cumulativeGap       = copy._cumulativeGapRecipe;

        return copy;
    }

    /// <summary>Produces one arbitrary <see cref="BucketGap" />.</summary>
    public BucketGap Generate() {
        return new BucketGap(_bucket.Generate(),
                             _inflows.Generate(),
                             _outflows.Generate(),
                             _cumulativeGap.Generate());
    }

}
