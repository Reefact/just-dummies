// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate SpotTrade --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.
// A TODO(any) above a `_ = TODO_verify_…` line names a guard the scaffolder could not read; the recipe
// under it is its best generator for the type. Verify it honours that guard, or replace it, then delete
// those two lines. What the scaffolder reads, and what it cannot:
// https://github.com/Reefact/just-dummies/blob/main/doc/handwritten/for-users/packages/justdummies-cli.en.md#what-the-last-column-means

using System;

using JustDummies;

namespace FxSettlement.Domain;

/// <summary>
///     A generator of arbitrary <see cref="SpotTrade" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnySpotTrade : IAny<SpotTrade> {

    private IAny<TradeId>      _id;
    private IAny<Lei>          _counterparty;
    private IAny<TradeSide>    _side;
    private IAny<Money>        _dealtAmount;
    private IAny<ExchangeRate> _rate;
    private IAny<DateOnly>     _tradeDate;

    private AnyTradeId         _idRecipe;
    private AnyLei             _counterpartyRecipe;
    private AnyEnum<TradeSide> _sideRecipe;
    private AnyMoney           _dealtAmountRecipe;
    private AnyExchangeRate    _rateRecipe;
    private IAny<DateOnly>     _tradeDateRecipe;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnySpotTrade()
        : this(id:           AnyValidId(),
               counterparty: AnyValidCounterparty(),
               side:         AnyValidSide(),
               dealtAmount:  AnyValidDealtAmount(),
               rate:         AnyValidRate(),
               tradeDate:    AnyValidTradeDate()) { }

    private static AnyTradeId AnyValidId() {
        return new AnyTradeId();
    }

    private static AnyLei AnyValidCounterparty() {
        return new AnyLei();
    }

    private static AnyEnum<TradeSide> AnyValidSide() {
        return Any.Enum<TradeSide>();
    }

    private static AnyMoney AnyValidDealtAmount() {
        // Worth far more than one minor unit of the other leg, and positive; its currency is chosen in
        // Generate() among the two of the rate's pair, unless a test pinned the amount.
        return new AnyMoney().WithAmount(a => a.Between(1_000m, 1_000_000m));
    }

    private static AnyExchangeRate AnyValidRate() {
        return new AnyExchangeRate();
    }

    private static IAny<DateOnly> AnyValidTradeDate() {
        return BusinessDays.Any();
    }

    /// <summary>Draws every parameter from the recipe given for it.</summary>
    private AnySpotTrade(AnyTradeId         id,
                         AnyLei             counterparty,
                         AnyEnum<TradeSide> side,
                         AnyMoney           dealtAmount,
                         AnyExchangeRate    rate,
                         IAny<DateOnly>     tradeDate) {
        _id                 = id;
        _idRecipe           = id;
        _counterparty       = counterparty;
        _counterpartyRecipe = counterparty;
        _side               = side;
        _sideRecipe         = side;
        _dealtAmount        = dealtAmount;
        _dealtAmountRecipe  = dealtAmount;
        _rate               = rate;
        _rateRecipe         = rate;
        _tradeDate          = tradeDate;
        _tradeDateRecipe    = tradeDate;
    }

    /// <summary>Copies <paramref name="from" />, for a <c>With…</c> method to change one parameter of before handing it out.</summary>
    private AnySpotTrade(AnySpotTrade from) {
        _id                 = from._id;
        _idRecipe           = from._idRecipe;
        _counterparty       = from._counterparty;
        _counterpartyRecipe = from._counterpartyRecipe;
        _side               = from._side;
        _sideRecipe         = from._sideRecipe;
        _dealtAmount        = from._dealtAmount;
        _dealtAmountRecipe  = from._dealtAmountRecipe;
        _rate               = from._rate;
        _rateRecipe         = from._rateRecipe;
        _tradeDate          = from._tradeDate;
        _tradeDateRecipe    = from._tradeDateRecipe;
        _settlingOn         = from._settlingOn;
    }

    private DateOnly? _settlingOn;

    /// <summary>Makes the trade settle on <paramref name="valueDate" />, a business day: the trade date is the one that many spot days earlier for the pair drawn.</summary>
    public AnySpotTrade SettlingOn(DateOnly valueDate) {
        AnySpotTrade copy = new AnySpotTrade(this);

        copy._settlingOn = valueDate;

        return copy;
    }

    /// <summary>Pins <c>id</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnySpotTrade WithId(TradeId value) {
        AnySpotTrade copy = new AnySpotTrade(this);

        copy._id = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>id</c> is drawn from — <c>AnyValidId()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyTradeId, AnyTradeId&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnySpotTrade WithId(Func<AnyTradeId, AnyTradeId> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnySpotTrade copy = new AnySpotTrade(this);

        copy._idRecipe = configure(_idRecipe);
        copy._id       = copy._idRecipe;

        return copy;
    }

    /// <summary>Pins <c>counterparty</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnySpotTrade WithCounterparty(Lei value) {
        AnySpotTrade copy = new AnySpotTrade(this);

        copy._counterparty = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>counterparty</c> is drawn from — <c>AnyValidCounterparty()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyLei, AnyLei&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnySpotTrade WithCounterparty(Func<AnyLei, AnyLei> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnySpotTrade copy = new AnySpotTrade(this);

        copy._counterpartyRecipe = configure(_counterpartyRecipe);
        copy._counterparty       = copy._counterpartyRecipe;

        return copy;
    }

    /// <summary>Pins <c>side</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnySpotTrade WithSide(TradeSide value) {
        AnySpotTrade copy = new AnySpotTrade(this);

        copy._side = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>side</c> is drawn from — <c>AnyValidSide()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyEnum&lt;TradeSide&gt;, AnyEnum&lt;TradeSide&gt;&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnySpotTrade WithSide(Func<AnyEnum<TradeSide>, AnyEnum<TradeSide>> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnySpotTrade copy = new AnySpotTrade(this);

        copy._sideRecipe = configure(_sideRecipe);
        copy._side       = copy._sideRecipe;

        return copy;
    }

    /// <summary>Pins <c>dealtAmount</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnySpotTrade WithDealtAmount(Money value) {
        AnySpotTrade copy = new AnySpotTrade(this);

        copy._dealtAmount = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>dealtAmount</c> is drawn from — <c>AnyValidDealtAmount()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyMoney, AnyMoney&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnySpotTrade WithDealtAmount(Func<AnyMoney, AnyMoney> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnySpotTrade copy = new AnySpotTrade(this);

        copy._dealtAmountRecipe = configure(_dealtAmountRecipe);
        copy._dealtAmount       = copy._dealtAmountRecipe;

        return copy;
    }

    /// <summary>Pins <c>rate</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnySpotTrade WithRate(ExchangeRate value) {
        AnySpotTrade copy = new AnySpotTrade(this);

        copy._rate = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>rate</c> is drawn from — <c>AnyValidRate()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyExchangeRate, AnyExchangeRate&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnySpotTrade WithRate(Func<AnyExchangeRate, AnyExchangeRate> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnySpotTrade copy = new AnySpotTrade(this);

        copy._rateRecipe = configure(_rateRecipe);
        copy._rate       = copy._rateRecipe;

        return copy;
    }

    /// <summary>Pins <c>tradeDate</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnySpotTrade WithTradeDate(DateOnly value) {
        AnySpotTrade copy = new AnySpotTrade(this);

        copy._tradeDate = Any.Fixed(value);

        return copy;
    }

    /// <summary>Produces one arbitrary <see cref="SpotTrade" />.</summary>
    public SpotTrade Generate() {
        ExchangeRate rate = _rate.Generate();

        // The dealt currency is one of the two of the rate's pair, unless a test pinned the amount.
        Money dealtAmount = ReferenceEquals(_dealtAmount, _dealtAmountRecipe)
            ? _dealtAmountRecipe.WithCurrency(Any.Boolean().Generate() ? rate.Pair.Base : rate.Pair.Quote).Generate()
            : _dealtAmount.Generate();

        DateOnly tradeDate = _settlingOn is { } valueDate
            ? BusinessDays.TradeDateFor(valueDate, rate.Pair.SpotLagDays)
            : _tradeDate.Generate();

        return new SpotTrade(_id.Generate(),
                             _counterparty.Generate(),
                             _side.Generate(),
                             dealtAmount,
                             rate,
                             tradeDate);
    }

}
