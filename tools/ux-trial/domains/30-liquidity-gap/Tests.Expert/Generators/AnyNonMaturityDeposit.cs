// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate NonMaturityDeposit --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.

using System;

using JustDummies;
using LiquidityGap.Domain;

namespace LiquidityGap.Domain.Tests.Expert.Generators;

/// <summary>
///     A generator of arbitrary <see cref="NonMaturityDeposit" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyNonMaturityDeposit : IAny<NonMaturityDeposit> {

    private IAny<string>                _depositId;
    private IAny<decimal>               _balance;
    private IAny<Currency>              _currency;
    private IAny<BehaviouralAssumption> _assumption;

    private AnyString                _depositIdRecipe;
    private AnyDecimal               _balanceRecipe;
    private AnyCurrency              _currencyRecipe;
    private AnyBehaviouralAssumption _assumptionRecipe;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyNonMaturityDeposit()
        : this(depositId:  AnyValidDepositId(),
               balance:    AnyValidBalance(),
               currency:   AnyValidCurrency(),
               assumption: AnyValidAssumption()) { }

    private static AnyString AnyValidDepositId() {
        return Any.String().NotBlank();
    }

    private static AnyDecimal AnyValidBalance() {
        return Any.Decimal().Between(0.01m, 1_000_000_000m).WithScale(2);
    }

    private static AnyCurrency AnyValidCurrency() {
        return new AnyCurrency();
    }

    private static AnyBehaviouralAssumption AnyValidAssumption() {
        return new AnyBehaviouralAssumption();
    }

    /// <summary>Draws every parameter from the recipe given for it.</summary>
    private AnyNonMaturityDeposit(AnyString                depositId,
                                  AnyDecimal               balance,
                                  AnyCurrency              currency,
                                  AnyBehaviouralAssumption assumption) {
        _depositId        = depositId;
        _depositIdRecipe  = depositId;
        _balance          = balance;
        _balanceRecipe    = balance;
        _currency         = currency;
        _currencyRecipe   = currency;
        _assumption       = assumption;
        _assumptionRecipe = assumption;
    }

    /// <summary>Copies <paramref name="from" />, for a <c>With…</c> method to change one parameter of before handing it out.</summary>
    private AnyNonMaturityDeposit(AnyNonMaturityDeposit from) {
        _depositId        = from._depositId;
        _depositIdRecipe  = from._depositIdRecipe;
        _balance          = from._balance;
        _balanceRecipe    = from._balanceRecipe;
        _currency         = from._currency;
        _currencyRecipe   = from._currencyRecipe;
        _assumption       = from._assumption;
        _assumptionRecipe = from._assumptionRecipe;
    }

    /// <summary>Pins <c>depositId</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyNonMaturityDeposit WithDepositId(string value) {
        AnyNonMaturityDeposit copy = new AnyNonMaturityDeposit(this);

        copy._depositId = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>depositId</c> is drawn from — <c>AnyValidDepositId()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyString, AnyString&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyNonMaturityDeposit WithDepositId(Func<AnyString, AnyString> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyNonMaturityDeposit copy = new AnyNonMaturityDeposit(this);

        copy._depositIdRecipe = configure(_depositIdRecipe);
        copy._depositId       = copy._depositIdRecipe;

        return copy;
    }

    /// <summary>Pins <c>balance</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyNonMaturityDeposit WithBalance(decimal value) {
        AnyNonMaturityDeposit copy = new AnyNonMaturityDeposit(this);

        copy._balance = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>balance</c> is drawn from — <c>AnyValidBalance()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyDecimal, AnyDecimal&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyNonMaturityDeposit WithBalance(Func<AnyDecimal, AnyDecimal> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyNonMaturityDeposit copy = new AnyNonMaturityDeposit(this);

        copy._balanceRecipe = configure(_balanceRecipe);
        copy._balance       = copy._balanceRecipe;

        return copy;
    }

    /// <summary>Pins <c>currency</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyNonMaturityDeposit WithCurrency(Currency value) {
        AnyNonMaturityDeposit copy = new AnyNonMaturityDeposit(this);

        copy._currency = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>currency</c> is drawn from — <c>AnyValidCurrency()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyCurrency, AnyCurrency&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyNonMaturityDeposit WithCurrency(Func<AnyCurrency, AnyCurrency> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyNonMaturityDeposit copy = new AnyNonMaturityDeposit(this);

        copy._currencyRecipe = configure(_currencyRecipe);
        copy._currency       = copy._currencyRecipe;

        return copy;
    }

    /// <summary>Pins <c>assumption</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyNonMaturityDeposit WithAssumption(BehaviouralAssumption value) {
        AnyNonMaturityDeposit copy = new AnyNonMaturityDeposit(this);

        copy._assumption = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>assumption</c> is drawn from — <c>AnyValidAssumption()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyBehaviouralAssumption, AnyBehaviouralAssumption&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyNonMaturityDeposit WithAssumption(Func<AnyBehaviouralAssumption, AnyBehaviouralAssumption> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyNonMaturityDeposit copy = new AnyNonMaturityDeposit(this);

        copy._assumptionRecipe = configure(_assumptionRecipe);
        copy._assumption       = copy._assumptionRecipe;

        return copy;
    }

    /// <summary>Produces one arbitrary <see cref="NonMaturityDeposit" />.</summary>
    public NonMaturityDeposit Generate() {
        return new NonMaturityDeposit(_depositId.Generate(),
                                      _balance.Generate(),
                                      _currency.Generate(),
                                      _assumption.Generate());
    }

}
