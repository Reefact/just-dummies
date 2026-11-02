// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate GiftCard --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.
// A TODO(any) above a `_ = TODO_verify_…` line names a guard the scaffolder could not read; the recipe
// under it is its best generator for the type. Verify it honours that guard, or replace it, then delete
// those two lines. What the scaffolder reads, and what it cannot:
// https://github.com/Reefact/just-dummies/blob/main/doc/handwritten/for-users/packages/justdummies-cli.en.md#what-the-last-column-means

using System;

using JustDummies;

namespace Shop.Domain;

/// <summary>
///     A generator of arbitrary <see cref="GiftCard" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyGiftCard : IAny<GiftCard> {

    private IAny<string>  _number;
    private IAny<decimal> _balance;
    private IAny<int>     _pin;

    private AnyString  _numberRecipe;
    private AnyDecimal _balanceRecipe;
    private AnyInt32   _pinRecipe;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyGiftCard()
        : this(number:  AnyValidNumber(),
               balance: AnyValidBalance(),
               pin:     AnyValidPin()) { }

    private static AnyString AnyValidNumber() {
        return Any.String().Numeric().WithLength(16);
    }

    private static AnyDecimal AnyValidBalance() {
        return Any.Decimal().Between(5m, 500m);
    }

    private static AnyInt32 AnyValidPin() {
        return Any.Int32().Between(0, 9999);
    }

    /// <summary>Draws every parameter from the recipe given for it.</summary>
    private AnyGiftCard(AnyString  number,
                        AnyDecimal balance,
                        AnyInt32   pin) {
        _number        = number;
        _numberRecipe  = number;
        _balance       = balance;
        _balanceRecipe = balance;
        _pin           = pin;
        _pinRecipe     = pin;
    }

    /// <summary>Copies <paramref name="from" />, for a <c>With…</c> method to change one parameter of before handing it out.</summary>
    private AnyGiftCard(AnyGiftCard from) {
        _number        = from._number;
        _numberRecipe  = from._numberRecipe;
        _balance       = from._balance;
        _balanceRecipe = from._balanceRecipe;
        _pin           = from._pin;
        _pinRecipe     = from._pinRecipe;
    }

    /// <summary>Pins <c>number</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyGiftCard WithNumber(string value) {
        AnyGiftCard copy = new AnyGiftCard(this);

        copy._number = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>number</c> is drawn from — <c>AnyValidNumber()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyString, AnyString&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyGiftCard WithNumber(Func<AnyString, AnyString> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyGiftCard copy = new AnyGiftCard(this);

        copy._numberRecipe = configure(_numberRecipe);
        copy._number       = copy._numberRecipe;

        return copy;
    }

    /// <summary>Pins <c>balance</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyGiftCard WithBalance(decimal value) {
        AnyGiftCard copy = new AnyGiftCard(this);

        copy._balance = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>balance</c> is drawn from — <c>AnyValidBalance()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyDecimal, AnyDecimal&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyGiftCard WithBalance(Func<AnyDecimal, AnyDecimal> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyGiftCard copy = new AnyGiftCard(this);

        copy._balanceRecipe = configure(_balanceRecipe);
        copy._balance       = copy._balanceRecipe;

        return copy;
    }

    /// <summary>Pins <c>pin</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyGiftCard WithPin(int value) {
        AnyGiftCard copy = new AnyGiftCard(this);

        copy._pin = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>pin</c> is drawn from — <c>AnyValidPin()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyInt32, AnyInt32&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyGiftCard WithPin(Func<AnyInt32, AnyInt32> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyGiftCard copy = new AnyGiftCard(this);

        copy._pinRecipe = configure(_pinRecipe);
        copy._pin       = copy._pinRecipe;

        return copy;
    }

    /// <summary>Produces one arbitrary <see cref="GiftCard" />.</summary>
    public GiftCard Generate() {
        return new GiftCard(_number.Generate(),
                            _balance.Generate(),
                            _pin.Generate());
    }

}
