// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate TaxBracket --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.
// A TODO(any) above a `_ = TODO_verify_…` line names a guard the scaffolder could not read; the recipe
// under it is its best generator for the type. Verify it honours that guard, or replace it, then delete
// those two lines. What the scaffolder reads, and what it cannot:
// https://github.com/Reefact/just-dummies/blob/main/doc/handwritten/for-users/packages/justdummies-cli.en.md#what-the-last-column-means

using System;

using JustDummies;

namespace TaxRules.Domain;

/// <summary>
///     A generator of arbitrary <see cref="TaxBracket" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyTaxBracket : IAny<TaxBracket> {

    private IAny<Money>  _lower;
    private IAny<Money?> _upper;
    private IAny<Rate>   _rate;

    private AnyMoney _lowerRecipe;
    private AnyMoney _upperRecipe;
    private AnyRate  _rateRecipe;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyTaxBracket()
        : this(lower: AnyValidLower(),
               upper: AnyValidUpper(),
               rate:  AnyValidRate()) { }

    private static AnyMoney AnyValidLower() {
        // Kept well below Money.MaxAmount so that a strictly larger upper bound always exists.
        return new AnyMoney().WithAmount(a => a.LessThanOrEqualTo(100_000_000m));
    }

    private static AnyMoney AnyValidUpper() {
        // Redrawn above the lower bound in Generate() when it falls at or below it.
        return new AnyMoney().WithAmount(a => a.LessThanOrEqualTo(100_000_000m));
    }

    private static AnyRate AnyValidRate() {
        return new AnyRate();
    }

    /// <summary>Draws every parameter from the recipe given for it.</summary>
    private AnyTaxBracket(AnyMoney lower,
                          AnyMoney upper,
                          AnyRate  rate) {
        _lower       = lower;
        _lowerRecipe = lower;
        _upper       = upper.OrNull();
        _upperRecipe = upper;
        _rate        = rate;
        _rateRecipe  = rate;
    }

    /// <summary>Copies <paramref name="from" />, for a <c>With…</c> method to change one parameter of before handing it out.</summary>
    private AnyTaxBracket(AnyTaxBracket from) {
        _lower       = from._lower;
        _lowerRecipe = from._lowerRecipe;
        _upper       = from._upper;
        _upperRecipe = from._upperRecipe;
        _rate        = from._rate;
        _rateRecipe  = from._rateRecipe;
    }

    /// <summary>Pins <c>lower</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyTaxBracket WithLower(Money value) {
        AnyTaxBracket copy = new AnyTaxBracket(this);

        copy._lower = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>lower</c> is drawn from — <c>AnyValidLower()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyMoney, AnyMoney&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyTaxBracket WithLower(Func<AnyMoney, AnyMoney> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyTaxBracket copy = new AnyTaxBracket(this);

        copy._lowerRecipe = configure(_lowerRecipe);
        copy._lower       = copy._lowerRecipe;

        return copy;
    }

    /// <summary>Pins <c>upper</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyTaxBracket WithUpper(Money? value) {
        AnyTaxBracket copy = new AnyTaxBracket(this);

        copy._upper = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>upper</c> is drawn from — <c>AnyValidUpper()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyMoney, AnyMoney&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyTaxBracket WithUpper(Func<AnyMoney, AnyMoney> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyTaxBracket copy = new AnyTaxBracket(this);

        copy._upperRecipe = configure(_upperRecipe);
        copy._upper       = copy._upperRecipe;

        return copy;
    }

    /// <summary>Pins <c>upper</c> to <c>null</c>; its recipe is kept for a later refinement.</summary>
    public AnyTaxBracket WithoutUpper() {
        AnyTaxBracket copy = new AnyTaxBracket(this);

        copy._upper = Any.Fixed<Money?>(null);

        return copy;
    }

    /// <summary>Pins <c>rate</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyTaxBracket WithRate(Rate value) {
        AnyTaxBracket copy = new AnyTaxBracket(this);

        copy._rate = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>rate</c> is drawn from — <c>AnyValidRate()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyRate, AnyRate&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyTaxBracket WithRate(Func<AnyRate, AnyRate> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyTaxBracket copy = new AnyTaxBracket(this);

        copy._rateRecipe = configure(_rateRecipe);
        copy._rate       = copy._rateRecipe;

        return copy;
    }

    /// <summary>Produces one arbitrary <see cref="TaxBracket" />.</summary>
    public TaxBracket Generate() {
        // upper > lower is a rule between two parameters: draw the lower bound, then a strictly larger upper one.
        Money  lower = _lower.Generate();
        Money? upper = _upper.Generate();

        if (upper is not null && upper.CompareTo(lower) <= 0) {
            upper = Above(lower);
        }

        return new TaxBracket(lower, upper, _rate.Generate());
    }

    private static Money Above(Money lower) {
        return new Money(lower.Amount + Any.Decimal().Between(0.01m, 1_000m).WithScale(2).Generate());
    }

}
