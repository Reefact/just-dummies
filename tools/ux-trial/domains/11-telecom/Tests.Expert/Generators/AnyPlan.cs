// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate Plan --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.
// A TODO(any) above a `_ = TODO_verify_…` line names a guard the scaffolder could not read; the recipe
// under it is its best generator for the type. Verify it honours that guard, or replace it, then delete
// those two lines. What the scaffolder reads, and what it cannot:
// https://github.com/Reefact/just-dummies/blob/main/doc/handwritten/for-users/packages/justdummies-cli.en.md#what-the-last-column-means

using System;

using JustDummies;
using Telecom.Domain;

namespace Telecom.Tests.Expert.Generators;

/// <summary>
///     A generator of arbitrary <see cref="Plan" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyPlan : IAny<Plan> {

    private IAny<string>   _name;
    private IAny<PlanTier> _tier;
    private IAny<decimal>  _monthlyFee;
    private IAny<int>      _dataAllowanceGb;
    private IAny<decimal>  _overagePerGb;

    private AnyString         _nameRecipe;
    private AnyEnum<PlanTier> _tierRecipe;
    private AnyDecimal        _monthlyFeeRecipe;
    private AnyInt32          _dataAllowanceGbRecipe;
    private AnyDecimal        _overagePerGbRecipe;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyPlan()
        : this(name:            AnyValidName(),
               tier:            AnyValidTier(),
               monthlyFee:      AnyValidMonthlyFee(),
               dataAllowanceGb: AnyValidDataAllowanceGb(),
               overagePerGb:    AnyValidOveragePerGb()) { }

    private static AnyString AnyValidName() {
        return Any.String().NotBlank().WithMaxLength(40);
    }

    private static AnyEnum<PlanTier> AnyValidTier() {
        return Any.Enum<PlanTier>();
    }

    private static AnyDecimal AnyValidMonthlyFee() {
        return Any.Decimal().GreaterThanOrEqualTo(0m).WithScale(2);
    }

    private static AnyInt32 AnyValidDataAllowanceGb() {
        return Any.Int32().Positive();
    }

    private static AnyDecimal AnyValidOveragePerGb() {
        return Any.Decimal().GreaterThanOrEqualTo(0m);
    }

    /// <summary>Draws every parameter from the recipe given for it.</summary>
    private AnyPlan(AnyString         name,
                    AnyEnum<PlanTier> tier,
                    AnyDecimal        monthlyFee,
                    AnyInt32          dataAllowanceGb,
                    AnyDecimal        overagePerGb) {
        _name                  = name;
        _nameRecipe            = name;
        _tier                  = tier;
        _tierRecipe            = tier;
        _monthlyFee            = monthlyFee;
        _monthlyFeeRecipe      = monthlyFee;
        _dataAllowanceGb       = dataAllowanceGb;
        _dataAllowanceGbRecipe = dataAllowanceGb;
        _overagePerGb          = overagePerGb;
        _overagePerGbRecipe    = overagePerGb;
    }

    /// <summary>Copies <paramref name="from" />, for a <c>With…</c> method to change one parameter of before handing it out.</summary>
    private AnyPlan(AnyPlan from) {
        _name                  = from._name;
        _nameRecipe            = from._nameRecipe;
        _tier                  = from._tier;
        _tierRecipe            = from._tierRecipe;
        _monthlyFee            = from._monthlyFee;
        _monthlyFeeRecipe      = from._monthlyFeeRecipe;
        _dataAllowanceGb       = from._dataAllowanceGb;
        _dataAllowanceGbRecipe = from._dataAllowanceGbRecipe;
        _overagePerGb          = from._overagePerGb;
        _overagePerGbRecipe    = from._overagePerGbRecipe;
    }

    /// <summary>Pins <c>name</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyPlan WithName(string value) {
        AnyPlan copy = new AnyPlan(this);

        copy._name = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>name</c> is drawn from — <c>AnyValidName()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyString, AnyString&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyPlan WithName(Func<AnyString, AnyString> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyPlan copy = new AnyPlan(this);

        copy._nameRecipe = configure(_nameRecipe);
        copy._name       = copy._nameRecipe;

        return copy;
    }

    /// <summary>Pins <c>tier</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyPlan WithTier(PlanTier value) {
        AnyPlan copy = new AnyPlan(this);

        copy._tier = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>tier</c> is drawn from — <c>AnyValidTier()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyEnum&lt;PlanTier&gt;, AnyEnum&lt;PlanTier&gt;&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyPlan WithTier(Func<AnyEnum<PlanTier>, AnyEnum<PlanTier>> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyPlan copy = new AnyPlan(this);

        copy._tierRecipe = configure(_tierRecipe);
        copy._tier       = copy._tierRecipe;

        return copy;
    }

    /// <summary>Pins <c>monthlyFee</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyPlan WithMonthlyFee(decimal value) {
        AnyPlan copy = new AnyPlan(this);

        copy._monthlyFee = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>monthlyFee</c> is drawn from — <c>AnyValidMonthlyFee()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyDecimal, AnyDecimal&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyPlan WithMonthlyFee(Func<AnyDecimal, AnyDecimal> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyPlan copy = new AnyPlan(this);

        copy._monthlyFeeRecipe = configure(_monthlyFeeRecipe);
        copy._monthlyFee       = copy._monthlyFeeRecipe;

        return copy;
    }

    /// <summary>Pins <c>dataAllowanceGb</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyPlan WithDataAllowanceGb(int value) {
        AnyPlan copy = new AnyPlan(this);

        copy._dataAllowanceGb = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>dataAllowanceGb</c> is drawn from — <c>AnyValidDataAllowanceGb()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyInt32, AnyInt32&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyPlan WithDataAllowanceGb(Func<AnyInt32, AnyInt32> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyPlan copy = new AnyPlan(this);

        copy._dataAllowanceGbRecipe = configure(_dataAllowanceGbRecipe);
        copy._dataAllowanceGb       = copy._dataAllowanceGbRecipe;

        return copy;
    }

    /// <summary>Pins <c>overagePerGb</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyPlan WithOveragePerGb(decimal value) {
        AnyPlan copy = new AnyPlan(this);

        copy._overagePerGb = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>overagePerGb</c> is drawn from — <c>AnyValidOveragePerGb()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyDecimal, AnyDecimal&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyPlan WithOveragePerGb(Func<AnyDecimal, AnyDecimal> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyPlan copy = new AnyPlan(this);

        copy._overagePerGbRecipe = configure(_overagePerGbRecipe);
        copy._overagePerGb       = copy._overagePerGbRecipe;

        return copy;
    }

    /// <summary>Produces one arbitrary <see cref="Plan" />.</summary>
    public Plan Generate() {
        return new Plan(_name.Generate(),
                        _tier.Generate(),
                        _monthlyFee.Generate(),
                        _dataAllowanceGb.Generate(),
                        _overagePerGb.Generate());
    }

}
