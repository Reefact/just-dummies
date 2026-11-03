// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate ClaimAssessment --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.
// A TODO(any) above a `_ = TODO_verify_…` line names a guard the scaffolder could not read; the recipe
// under it is its best generator for the type. Verify it honours that guard, or replace it, then delete
// those two lines. What the scaffolder reads, and what it cannot:
// https://github.com/Reefact/just-dummies/blob/main/doc/handwritten/for-users/packages/justdummies-cli.en.md#what-the-last-column-means

using System;

using JustDummies;

namespace HealthcareClaims.Domain;

/// <summary>
///     A generator of arbitrary <see cref="ClaimAssessment" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyClaimAssessment : IAny<ClaimAssessment> {

    private IAny<decimal> _coveredAmount;
    private IAny<decimal> _deductibleApplied;
    private IAny<decimal> _ceilingReduction;

    private AnyDecimal _coveredAmountRecipe;
    private AnyDecimal _deductibleAppliedRecipe;
    private AnyDecimal _ceilingReductionRecipe;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyClaimAssessment()
        : this(coveredAmount:     AnyValidCoveredAmount(),
               deductibleApplied: AnyValidDeductibleApplied(),
               ceilingReduction:  AnyValidCeilingReduction()) { }

    private static AnyDecimal AnyValidCoveredAmount() {
        return Any.Decimal().Between(0m, ClaimAssessment.MaximumAmount).WithScale(2);
    }

    private static AnyDecimal AnyValidDeductibleApplied() {
        return Any.Decimal().Between(0m, ClaimAssessment.MaximumAmount).WithScale(2);
    }

    private static AnyDecimal AnyValidCeilingReduction() {
        return Any.Decimal().Between(0m, ClaimAssessment.MaximumAmount).WithScale(2);
    }

    /// <summary>Draws every parameter from the recipe given for it.</summary>
    private AnyClaimAssessment(AnyDecimal coveredAmount,
                               AnyDecimal deductibleApplied,
                               AnyDecimal ceilingReduction) {
        _coveredAmount           = coveredAmount;
        _coveredAmountRecipe     = coveredAmount;
        _deductibleApplied       = deductibleApplied;
        _deductibleAppliedRecipe = deductibleApplied;
        _ceilingReduction        = ceilingReduction;
        _ceilingReductionRecipe  = ceilingReduction;
    }

    /// <summary>Copies <paramref name="from" />, for a <c>With…</c> method to change one parameter of before handing it out.</summary>
    private AnyClaimAssessment(AnyClaimAssessment from) {
        _coveredAmount           = from._coveredAmount;
        _coveredAmountRecipe     = from._coveredAmountRecipe;
        _deductibleApplied       = from._deductibleApplied;
        _deductibleAppliedRecipe = from._deductibleAppliedRecipe;
        _ceilingReduction        = from._ceilingReduction;
        _ceilingReductionRecipe  = from._ceilingReductionRecipe;
    }

    /// <summary>Pins <c>coveredAmount</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyClaimAssessment WithCoveredAmount(decimal value) {
        AnyClaimAssessment copy = new AnyClaimAssessment(this);

        copy._coveredAmount = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>coveredAmount</c> is drawn from — <c>AnyValidCoveredAmount()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyDecimal, AnyDecimal&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyClaimAssessment WithCoveredAmount(Func<AnyDecimal, AnyDecimal> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyClaimAssessment copy = new AnyClaimAssessment(this);

        copy._coveredAmountRecipe = configure(_coveredAmountRecipe);
        copy._coveredAmount       = copy._coveredAmountRecipe;

        return copy;
    }

    /// <summary>Pins <c>deductibleApplied</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyClaimAssessment WithDeductibleApplied(decimal value) {
        AnyClaimAssessment copy = new AnyClaimAssessment(this);

        copy._deductibleApplied = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>deductibleApplied</c> is drawn from — <c>AnyValidDeductibleApplied()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyDecimal, AnyDecimal&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyClaimAssessment WithDeductibleApplied(Func<AnyDecimal, AnyDecimal> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyClaimAssessment copy = new AnyClaimAssessment(this);

        copy._deductibleAppliedRecipe = configure(_deductibleAppliedRecipe);
        copy._deductibleApplied       = copy._deductibleAppliedRecipe;

        return copy;
    }

    /// <summary>Pins <c>ceilingReduction</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyClaimAssessment WithCeilingReduction(decimal value) {
        AnyClaimAssessment copy = new AnyClaimAssessment(this);

        copy._ceilingReduction = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>ceilingReduction</c> is drawn from — <c>AnyValidCeilingReduction()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyDecimal, AnyDecimal&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyClaimAssessment WithCeilingReduction(Func<AnyDecimal, AnyDecimal> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyClaimAssessment copy = new AnyClaimAssessment(this);

        copy._ceilingReductionRecipe = configure(_ceilingReductionRecipe);
        copy._ceilingReduction       = copy._ceilingReductionRecipe;

        return copy;
    }

    /// <summary>Produces one arbitrary <see cref="ClaimAssessment" />.</summary>
    public ClaimAssessment Generate() {
        decimal covered = _coveredAmount.Generate();

        // deductible + reduction <= covered: unless a test pinned one, draw each below what is left.
        decimal deductible = ReferenceEquals(_deductibleApplied, _deductibleAppliedRecipe)
            ? _deductibleAppliedRecipe.LessThanOrEqualTo(covered).Generate()
            : _deductibleApplied.Generate();
        decimal reduction = ReferenceEquals(_ceilingReduction, _ceilingReductionRecipe)
            ? _ceilingReductionRecipe.LessThanOrEqualTo(covered - deductible).Generate()
            : _ceilingReduction.Generate();

        return new ClaimAssessment(covered, deductible, reduction);
    }

}
