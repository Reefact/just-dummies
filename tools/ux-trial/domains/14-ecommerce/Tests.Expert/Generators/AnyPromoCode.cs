// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate PromoCode --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.
// A TODO(any) above a `_ = TODO_verify_…` line names a guard the scaffolder could not read; the recipe
// under it is its best generator for the type. Verify it honours that guard, or replace it, then delete
// those two lines. What the scaffolder reads, and what it cannot:
// https://github.com/Reefact/just-dummies/blob/main/doc/handwritten/for-users/packages/justdummies-cli.en.md#what-the-last-column-means

using System;

using JustDummies;

namespace Shop.Domain;

/// <summary>
///     A generator of arbitrary <see cref="PromoCode" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyPromoCode : IAny<PromoCode> {

    private IAny<string>             _code;
    private IAny<decimal>            _percentOff;
    private AnyOrderedPair<DateOnly> _validFromAndValidUntil;
    private IAny<decimal>            _minimumBasket;
    private IAny<int>                _maxUses;

    private AnyString   _codeRecipe;
    private AnyDecimal  _percentOffRecipe;
    private AnyDateOnly _validFromRecipe;
    private AnyDateOnly _validUntilRecipe;
    private AnyDecimal  _minimumBasketRecipe;
    private AnyInt32    _maxUsesRecipe;

    private DateOnly? _validFromPin;
    private DateOnly? _validUntilPin;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyPromoCode()
        : this(code:          AnyValidCode(),
               percentOff:    AnyValidPercentOff(),
               validFrom:     AnyValidValidFrom(),
               validUntil:    AnyValidValidUntil(),
               minimumBasket: AnyValidMinimumBasket(),
               maxUses:       AnyValidMaxUses()) { }

    private static AnyString AnyValidCode() {
        return Any.String().NotBlank().WithLengthBetween(4, 12);
    }

    private static AnyDecimal AnyValidPercentOff() {
        return Any.Decimal().Positive().LessThanOrEqualTo(50m);
    }

    private static AnyDateOnly AnyValidValidFrom() {
        return Any.DateOnly();
    }

    private static AnyDateOnly AnyValidValidUntil() {
        return Any.DateOnly();
    }

    private static AnyDecimal AnyValidMinimumBasket() {
        return Any.Decimal().GreaterThanOrEqualTo(0m);
    }

    private static AnyInt32 AnyValidMaxUses() {
        return Any.Int32().Positive();
    }

    /// <summary>Draws every parameter from the recipe given for it.</summary>
    private AnyPromoCode(AnyString   code,
                         AnyDecimal  percentOff,
                         AnyDateOnly validFrom,
                         AnyDateOnly validUntil,
                         AnyDecimal  minimumBasket,
                         AnyInt32    maxUses) {
        _code                   = code;
        _codeRecipe             = code;
        _percentOff             = percentOff;
        _percentOffRecipe       = percentOff;
        _validFromRecipe        = validFrom;
        _validUntilRecipe       = validUntil;
        _minimumBasket          = minimumBasket;
        _minimumBasketRecipe    = minimumBasket;
        _maxUses                = maxUses;
        _maxUsesRecipe          = maxUses;
        _validFromAndValidUntil = ValidFromAndValidUntil();
    }

    /// <summary>Copies <paramref name="from" />, for a <c>With…</c> method to change one parameter of before handing it out.</summary>
    private AnyPromoCode(AnyPromoCode from) {
        _code                   = from._code;
        _codeRecipe             = from._codeRecipe;
        _percentOff             = from._percentOff;
        _percentOffRecipe       = from._percentOffRecipe;
        _validFromAndValidUntil = from._validFromAndValidUntil;
        _validFromRecipe        = from._validFromRecipe;
        _validFromPin           = from._validFromPin;
        _validUntilRecipe       = from._validUntilRecipe;
        _validUntilPin          = from._validUntilPin;
        _minimumBasket          = from._minimumBasket;
        _minimumBasketRecipe    = from._minimumBasketRecipe;
        _maxUses                = from._maxUses;
        _maxUsesRecipe          = from._maxUsesRecipe;
    }

    /// <summary>Draws <c>validFrom</c> and <c>validUntil</c> together, <c>validFrom</c> never after <c>validUntil</c> as the constructor requires, each from its current recipe and pin.</summary>
    private AnyOrderedPair<DateOnly> ValidFromAndValidUntil() {
        AnyOrderedPair<DateOnly> pair = Any.OrderedPair(_validFromRecipe, _validUntilRecipe);

        if (_validFromPin is { } validFrom) { pair = pair.WithFirst(validFrom); }
        if (_validUntilPin is { } validUntil) { pair = pair.WithSecond(validUntil); }

        return pair;
    }

    /// <summary>Pins <c>code</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyPromoCode WithCode(string value) {
        AnyPromoCode copy = new AnyPromoCode(this);

        copy._code = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>code</c> is drawn from — <c>AnyValidCode()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyString, AnyString&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyPromoCode WithCode(Func<AnyString, AnyString> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyPromoCode copy = new AnyPromoCode(this);

        copy._codeRecipe = configure(_codeRecipe);
        copy._code       = copy._codeRecipe;

        return copy;
    }

    /// <summary>Pins <c>percentOff</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyPromoCode WithPercentOff(decimal value) {
        AnyPromoCode copy = new AnyPromoCode(this);

        copy._percentOff = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>percentOff</c> is drawn from — <c>AnyValidPercentOff()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyDecimal, AnyDecimal&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyPromoCode WithPercentOff(Func<AnyDecimal, AnyDecimal> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyPromoCode copy = new AnyPromoCode(this);

        copy._percentOffRecipe = configure(_percentOffRecipe);
        copy._percentOff       = copy._percentOffRecipe;

        return copy;
    }

    /// <summary>Pins <c>validFrom</c> to a fixed value, which bounds <c>validUntil</c>; its recipe is kept for a later refinement.</summary>
    public AnyPromoCode WithValidFrom(DateOnly value) {
        AnyPromoCode copy = new AnyPromoCode(this);

        copy._validFromPin           = value;
        copy._validFromAndValidUntil = copy.ValidFromAndValidUntil();

        return copy;
    }

    /// <summary>Refines the recipe <c>validFrom</c> is drawn from — <c>AnyValidValidFrom()</c>, then each earlier refinement — through <paramref name="configure" />, in order with <c>validUntil</c>.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyDateOnly, AnyDateOnly&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyPromoCode WithValidFrom(Func<AnyDateOnly, AnyDateOnly> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyPromoCode copy = new AnyPromoCode(this);

        copy._validFromRecipe        = configure(_validFromRecipe);
        copy._validFromPin           = null;
        copy._validFromAndValidUntil = copy.ValidFromAndValidUntil();

        return copy;
    }

    /// <summary>Pins <c>validUntil</c> to a fixed value, which bounds <c>validFrom</c>; its recipe is kept for a later refinement.</summary>
    public AnyPromoCode WithValidUntil(DateOnly value) {
        AnyPromoCode copy = new AnyPromoCode(this);

        copy._validUntilPin          = value;
        copy._validFromAndValidUntil = copy.ValidFromAndValidUntil();

        return copy;
    }

    /// <summary>Refines the recipe <c>validUntil</c> is drawn from — <c>AnyValidValidUntil()</c>, then each earlier refinement — through <paramref name="configure" />, in order with <c>validFrom</c>.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyDateOnly, AnyDateOnly&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyPromoCode WithValidUntil(Func<AnyDateOnly, AnyDateOnly> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyPromoCode copy = new AnyPromoCode(this);

        copy._validUntilRecipe       = configure(_validUntilRecipe);
        copy._validUntilPin          = null;
        copy._validFromAndValidUntil = copy.ValidFromAndValidUntil();

        return copy;
    }

    /// <summary>Pins <c>minimumBasket</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyPromoCode WithMinimumBasket(decimal value) {
        AnyPromoCode copy = new AnyPromoCode(this);

        copy._minimumBasket = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>minimumBasket</c> is drawn from — <c>AnyValidMinimumBasket()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyDecimal, AnyDecimal&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyPromoCode WithMinimumBasket(Func<AnyDecimal, AnyDecimal> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyPromoCode copy = new AnyPromoCode(this);

        copy._minimumBasketRecipe = configure(_minimumBasketRecipe);
        copy._minimumBasket       = copy._minimumBasketRecipe;

        return copy;
    }

    /// <summary>Pins <c>maxUses</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyPromoCode WithMaxUses(int value) {
        AnyPromoCode copy = new AnyPromoCode(this);

        copy._maxUses = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>maxUses</c> is drawn from — <c>AnyValidMaxUses()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyInt32, AnyInt32&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyPromoCode WithMaxUses(Func<AnyInt32, AnyInt32> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyPromoCode copy = new AnyPromoCode(this);

        copy._maxUsesRecipe = configure(_maxUsesRecipe);
        copy._maxUses       = copy._maxUsesRecipe;

        return copy;
    }

    /// <summary>Produces one arbitrary <see cref="PromoCode" />.</summary>
    public PromoCode Generate() {
        (DateOnly validFrom, DateOnly validUntil) = _validFromAndValidUntil.Generate();

        return new PromoCode(_code.Generate(),
                             _percentOff.Generate(),
                             validFrom,
                             validUntil,
                             _minimumBasket.Generate(),
                             _maxUses.Generate());
    }

}
