// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate FeePolicy --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.
// A TODO(any) above a `_ = TODO_verify_…` line names a guard the scaffolder could not read; the recipe
// under it is its best generator for the type. Verify it honours that guard, or replace it, then delete
// those two lines. What the scaffolder reads, and what it cannot:
// https://github.com/Reefact/just-dummies/blob/main/doc/handwritten/for-users/packages/justdummies-cli.en.md#what-the-last-column-means

using System;

using JustDummies;

namespace Payments.Domain;

/// <summary>
///     A generator of arbitrary <see cref="FeePolicy" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyFeePolicy : IAny<FeePolicy> {

    private IAny<decimal> _instantFlatFee;
    private IAny<decimal> _instantRate;
    private IAny<decimal> _freeStandardThreshold;
    private IAny<decimal> _standardRate;

    private AnyDecimal _instantFlatFeeRecipe;
    private AnyDecimal _instantRateRecipe;
    private AnyDecimal _freeStandardThresholdRecipe;
    private AnyDecimal _standardRateRecipe;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyFeePolicy()
        : this(instantFlatFee:        AnyValidInstantFlatFee(),
               instantRate:           AnyValidInstantRate(),
               freeStandardThreshold: AnyValidFreeStandardThreshold(),
               standardRate:          AnyValidStandardRate()) { }

    private static AnyDecimal AnyValidInstantFlatFee() {
        return Any.Decimal().GreaterThanOrEqualTo(0m);
    }

    private static AnyDecimal AnyValidInstantRate() {
        return Any.Decimal().Between(0m, 0.05m);
    }

    private static AnyDecimal AnyValidFreeStandardThreshold() {
        return Any.Decimal().GreaterThanOrEqualTo(0m);
    }

    private static AnyDecimal AnyValidStandardRate() {
        return Any.Decimal().Between(0m, 0.05m);
    }

    /// <summary>Draws every parameter from the recipe given for it.</summary>
    private AnyFeePolicy(AnyDecimal instantFlatFee,
                         AnyDecimal instantRate,
                         AnyDecimal freeStandardThreshold,
                         AnyDecimal standardRate) {
        _instantFlatFee              = instantFlatFee;
        _instantFlatFeeRecipe        = instantFlatFee;
        _instantRate                 = instantRate;
        _instantRateRecipe           = instantRate;
        _freeStandardThreshold       = freeStandardThreshold;
        _freeStandardThresholdRecipe = freeStandardThreshold;
        _standardRate                = standardRate;
        _standardRateRecipe          = standardRate;
    }

    /// <summary>Copies <paramref name="from" />, for a <c>With…</c> method to change one parameter of before handing it out.</summary>
    private AnyFeePolicy(AnyFeePolicy from) {
        _instantFlatFee              = from._instantFlatFee;
        _instantFlatFeeRecipe        = from._instantFlatFeeRecipe;
        _instantRate                 = from._instantRate;
        _instantRateRecipe           = from._instantRateRecipe;
        _freeStandardThreshold       = from._freeStandardThreshold;
        _freeStandardThresholdRecipe = from._freeStandardThresholdRecipe;
        _standardRate                = from._standardRate;
        _standardRateRecipe          = from._standardRateRecipe;
    }

    /// <summary>Pins <c>instantFlatFee</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyFeePolicy WithInstantFlatFee(decimal value) {
        AnyFeePolicy copy = new AnyFeePolicy(this);

        copy._instantFlatFee = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>instantFlatFee</c> is drawn from — <c>AnyValidInstantFlatFee()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyDecimal, AnyDecimal&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyFeePolicy WithInstantFlatFee(Func<AnyDecimal, AnyDecimal> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyFeePolicy copy = new AnyFeePolicy(this);

        copy._instantFlatFeeRecipe = configure(_instantFlatFeeRecipe);
        copy._instantFlatFee       = copy._instantFlatFeeRecipe;

        return copy;
    }

    /// <summary>Pins <c>instantRate</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyFeePolicy WithInstantRate(decimal value) {
        AnyFeePolicy copy = new AnyFeePolicy(this);

        copy._instantRate = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>instantRate</c> is drawn from — <c>AnyValidInstantRate()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyDecimal, AnyDecimal&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyFeePolicy WithInstantRate(Func<AnyDecimal, AnyDecimal> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyFeePolicy copy = new AnyFeePolicy(this);

        copy._instantRateRecipe = configure(_instantRateRecipe);
        copy._instantRate       = copy._instantRateRecipe;

        return copy;
    }

    /// <summary>Pins <c>freeStandardThreshold</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyFeePolicy WithFreeStandardThreshold(decimal value) {
        AnyFeePolicy copy = new AnyFeePolicy(this);

        copy._freeStandardThreshold = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>freeStandardThreshold</c> is drawn from — <c>AnyValidFreeStandardThreshold()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyDecimal, AnyDecimal&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyFeePolicy WithFreeStandardThreshold(Func<AnyDecimal, AnyDecimal> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyFeePolicy copy = new AnyFeePolicy(this);

        copy._freeStandardThresholdRecipe = configure(_freeStandardThresholdRecipe);
        copy._freeStandardThreshold       = copy._freeStandardThresholdRecipe;

        return copy;
    }

    /// <summary>Pins <c>standardRate</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyFeePolicy WithStandardRate(decimal value) {
        AnyFeePolicy copy = new AnyFeePolicy(this);

        copy._standardRate = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>standardRate</c> is drawn from — <c>AnyValidStandardRate()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyDecimal, AnyDecimal&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyFeePolicy WithStandardRate(Func<AnyDecimal, AnyDecimal> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyFeePolicy copy = new AnyFeePolicy(this);

        copy._standardRateRecipe = configure(_standardRateRecipe);
        copy._standardRate       = copy._standardRateRecipe;

        return copy;
    }

    /// <summary>Produces one arbitrary <see cref="FeePolicy" />.</summary>
    public FeePolicy Generate() {
        return new FeePolicy(_instantFlatFee.Generate(),
                             _instantRate.Generate(),
                             _freeStandardThreshold.Generate(),
                             _standardRate.Generate());
    }

}
