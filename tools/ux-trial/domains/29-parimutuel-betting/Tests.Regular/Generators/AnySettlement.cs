// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate Settlement --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.
// A TODO(any) above a `_ = TODO_verify_…` line names a guard the scaffolder could not read; the recipe
// under it is its best generator for the type. Verify it honours that guard, or replace it, then delete
// those two lines. What the scaffolder reads, and what it cannot:
// https://github.com/Reefact/just-dummies/blob/main/doc/handwritten/for-users/packages/justdummies-cli.en.md#what-the-last-column-means

using System;
using System.Collections.Generic;

using JustDummies;

namespace ParimutuelBetting.Domain;

/// <summary>
///     A generator of arbitrary <see cref="Settlement" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnySettlement : IAny<Settlement> {

    private IAny<decimal>                 _deductions;
    private IAny<IReadOnlyList<Dividend>> _dividends;
    private IAny<decimal>                 _carryover;

    private AnyDecimal        _grossPoolRecipe;
    private AnyDecimal        _refundedRecipe;
    private AnyDecimal        _deductionsRecipe;
    private AnyList<Dividend> _dividendsRecipe;
    private AnyDecimal        _carryoverRecipe;

    private decimal? _grossPoolPin;
    private decimal? _refundedPin;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnySettlement()
        : this(grossPool:  AnyValidGrossPool(),
               refunded:   AnyValidRefunded(),
               deductions: AnyValidDeductions(),
               dividends:  AnyValidDividends(),
               carryover:  AnyValidCarryover()) { }

    private static AnyDecimal AnyValidGrossPool() {
        return Any.Decimal().Between(0m, 1_000_000m).WithScale(2);
    }

    private static AnyDecimal AnyValidRefunded() {
        return Any.Decimal().Between(0m, 1_000_000m).WithScale(2);
    }

    private static AnyDecimal AnyValidDeductions() {
        return Any.Decimal().GreaterThanOrEqualTo(0m).WithScale(2);
    }

    private static AnyList<Dividend> AnyValidDividends() {
        return Any.ListOf(new AnyDividend()).WithMaxCount(3);
    }

    private static AnyDecimal AnyValidCarryover() {
        return Any.Decimal().GreaterThanOrEqualTo(0m).WithScale(2);
    }

    /// <summary>Draws every parameter from the recipe given for it.</summary>
    private AnySettlement(AnyDecimal        grossPool,
                          AnyDecimal        refunded,
                          AnyDecimal        deductions,
                          AnyList<Dividend> dividends,
                          AnyDecimal        carryover) {
        _grossPoolRecipe      = grossPool;
        _refundedRecipe       = refunded;
        _deductions           = deductions;
        _deductionsRecipe     = deductions;
        _dividends            = dividends;
        _dividendsRecipe      = dividends;
        _carryover            = carryover;
        _carryoverRecipe      = carryover;
    }

    /// <summary>Copies <paramref name="from" />, for a <c>With…</c> method to change one parameter of before handing it out.</summary>
    private AnySettlement(AnySettlement from) {
        _grossPoolRecipe      = from._grossPoolRecipe;
        _grossPoolPin         = from._grossPoolPin;
        _refundedRecipe       = from._refundedRecipe;
        _refundedPin          = from._refundedPin;
        _deductions           = from._deductions;
        _deductionsRecipe     = from._deductionsRecipe;
        _dividends            = from._dividends;
        _dividendsRecipe      = from._dividendsRecipe;
        _carryover            = from._carryover;
        _carryoverRecipe      = from._carryoverRecipe;
    }

    /// <summary>Draws <c>refunded</c> and <c>grossPool</c>, <c>refunded</c> never after <c>grossPool</c> as the constructor requires: together when neither is pinned, one bounded by the other's pin, and two pins as they are, for the constructor to judge.</summary>
    private (decimal Refunded, decimal GrossPool) RefundedAndGrossPool() {
        if (_refundedPin is { } refunded) { return (refunded, _grossPoolPin ?? _grossPoolRecipe.GreaterThanOrEqualTo(refunded).Generate()); }
        if (_grossPoolPin is { } grossPool) { return (_refundedRecipe.LessThanOrEqualTo(grossPool).Generate(), grossPool); }

        return Any.Pair(_refundedRecipe, _grossPoolRecipe).Having.Item1LessThanOrEqualToItem2().Generate();
    }

    /// <summary>Pins <c>grossPool</c> to a fixed value, which bounds <c>refunded</c>; its recipe is kept for a later refinement.</summary>
    public AnySettlement WithGrossPool(decimal value) {
        AnySettlement copy = new AnySettlement(this);

        copy._grossPoolPin         = value;

        return copy;
    }

    /// <summary>Refines the recipe <c>grossPool</c> is drawn from — <c>AnyValidGrossPool()</c>, then each earlier refinement — through <paramref name="configure" />, in order with <c>refunded</c>.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyDecimal, AnyDecimal&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnySettlement WithGrossPool(Func<AnyDecimal, AnyDecimal> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnySettlement copy = new AnySettlement(this);

        copy._grossPoolRecipe      = configure(_grossPoolRecipe);
        copy._grossPoolPin         = null;

        return copy;
    }

    /// <summary>Pins <c>refunded</c> to a fixed value, which bounds <c>grossPool</c>; its recipe is kept for a later refinement.</summary>
    public AnySettlement WithRefunded(decimal value) {
        AnySettlement copy = new AnySettlement(this);

        copy._refundedPin          = value;

        return copy;
    }

    /// <summary>Refines the recipe <c>refunded</c> is drawn from — <c>AnyValidRefunded()</c>, then each earlier refinement — through <paramref name="configure" />, in order with <c>grossPool</c>.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyDecimal, AnyDecimal&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnySettlement WithRefunded(Func<AnyDecimal, AnyDecimal> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnySettlement copy = new AnySettlement(this);

        copy._refundedRecipe       = configure(_refundedRecipe);
        copy._refundedPin          = null;

        return copy;
    }

    /// <summary>Pins <c>deductions</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnySettlement WithDeductions(decimal value) {
        AnySettlement copy = new AnySettlement(this);

        copy._deductions = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>deductions</c> is drawn from — <c>AnyValidDeductions()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyDecimal, AnyDecimal&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnySettlement WithDeductions(Func<AnyDecimal, AnyDecimal> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnySettlement copy = new AnySettlement(this);

        copy._deductionsRecipe = configure(_deductionsRecipe);
        copy._deductions       = copy._deductionsRecipe;

        return copy;
    }

    /// <summary>Pins <c>dividends</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnySettlement WithDividends(IReadOnlyList<Dividend> value) {
        AnySettlement copy = new AnySettlement(this);

        copy._dividends = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>dividends</c> is drawn from — <c>AnyValidDividends()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyList&lt;Dividend&gt;, AnyList&lt;Dividend&gt;&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnySettlement WithDividends(Func<AnyList<Dividend>, AnyList<Dividend>> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnySettlement copy = new AnySettlement(this);

        copy._dividendsRecipe = configure(_dividendsRecipe);
        copy._dividends       = copy._dividendsRecipe;

        return copy;
    }

    /// <summary>Pins <c>carryover</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnySettlement WithCarryover(decimal value) {
        AnySettlement copy = new AnySettlement(this);

        copy._carryover = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>carryover</c> is drawn from — <c>AnyValidCarryover()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyDecimal, AnyDecimal&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnySettlement WithCarryover(Func<AnyDecimal, AnyDecimal> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnySettlement copy = new AnySettlement(this);

        copy._carryoverRecipe = configure(_carryoverRecipe);
        copy._carryover       = copy._carryoverRecipe;

        return copy;
    }

    /// <summary>Produces one arbitrary <see cref="Settlement" />.</summary>
    public Settlement Generate() {
        (decimal refunded, decimal grossPool) = RefundedAndGrossPool();

        // Deductions fit in what refunds leave; carryover fits in the net pool and only exists without dividends.
        decimal deductions = Within(_deductions, _deductionsRecipe, grossPool - refunded).Generate();
        decimal netPool    = grossPool - refunded - deductions;

        IReadOnlyList<Dividend> dividends = _dividends.Generate();
        decimal                 carryover = dividends.Count > 0 ? 0m : Within(_carryover, _carryoverRecipe, netPool).Generate();

        return new Settlement(grossPool, refunded, deductions, dividends, carryover);
    }

    // A pinned parameter is taken as given; a drawn one is capped by what the others leave it.
    private static IAny<decimal> Within(IAny<decimal> current, AnyDecimal recipe, decimal ceiling) {
        return ReferenceEquals(current, recipe) ? recipe.LessThanOrEqualTo(ceiling) : current;
    }

}
