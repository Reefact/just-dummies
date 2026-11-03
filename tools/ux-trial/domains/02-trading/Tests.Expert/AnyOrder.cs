// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate Order --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.
// A TODO(any) above a `_ = TODO_verify_…` line names a guard the scaffolder could not read; the recipe
// under it is its best generator for the type. Verify it honours that guard, or replace it, then delete
// those two lines. What the scaffolder reads, and what it cannot:
// https://github.com/Reefact/just-dummies/blob/main/doc/handwritten/for-users/packages/justdummies-cli.en.md#what-the-last-column-means

using System;

using JustDummies;

namespace Trading.Domain;

/// <summary>
///     A generator of arbitrary <see cref="Order" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyOrder : IAny<Order> {

    private IAny<Guid>        _id;
    private IAny<Ticker>      _ticker;
    private IAny<Side>        _side;
    private IAny<Quantity>    _quantity;
    private Price?            _pinnedLimit;
    private Price?            _pinnedStop;
    private bool              _stopPinned;
    private IAny<TickSize>    _tick;
    private IAny<TimeInForce> _timeInForce;
    private IAny<DateTime>    _placedAt;

    private AnyGuid              _idRecipe;
    private AnyTicker            _tickerRecipe;
    private AnyEnum<Side>        _sideRecipe;
    private AnyQuantity          _quantityRecipe;
    private AnyTickSize          _tickRecipe;
    private AnyEnum<TimeInForce> _timeInForceRecipe;
    private AnyDateTime          _placedAtRecipe;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyOrder()
        : this(id:          AnyValidId(),
               ticker:      AnyValidTicker(),
               side:        AnyValidSide(),
               quantity:    AnyValidQuantity(),
               tick:        AnyValidTick(),
               timeInForce: AnyValidTimeInForce(),
               placedAt:    AnyValidPlacedAt()) { }

    private static AnyGuid AnyValidId() {
        return Any.Guid().NonEmpty();
    }

    private static AnyTicker AnyValidTicker() {
        return new AnyTicker();
    }

    private static AnyEnum<Side> AnyValidSide() {
        return Any.Enum<Side>();
    }

    private static AnyQuantity AnyValidQuantity() {
        return new AnyQuantity();
    }

    private static AnyTickSize AnyValidTick() {
        return new AnyTickSize();
    }

    private static AnyEnum<TimeInForce> AnyValidTimeInForce() {
        return Any.Enum<TimeInForce>();
    }

    private static AnyDateTime AnyValidPlacedAt() {
        return Any.DateTime();
    }

    /// <summary>Draws every parameter from the recipe given for it.</summary>
    private AnyOrder(AnyGuid              id,
                     AnyTicker            ticker,
                     AnyEnum<Side>        side,
                     AnyQuantity          quantity,
                     AnyTickSize          tick,
                     AnyEnum<TimeInForce> timeInForce,
                     AnyDateTime          placedAt) {
        _id                = id;
        _idRecipe          = id;
        _ticker            = ticker;
        _tickerRecipe      = ticker;
        _side              = side;
        _sideRecipe        = side;
        _quantity          = quantity;
        _quantityRecipe    = quantity;
        _tick              = tick;
        _tickRecipe        = tick;
        _timeInForce       = timeInForce;
        _timeInForceRecipe = timeInForce;
        _placedAt          = placedAt;
        _placedAtRecipe    = placedAt;
    }

    /// <summary>Copies <paramref name="from" />, for a <c>With…</c> method to change one parameter of before handing it out.</summary>
    private AnyOrder(AnyOrder from) {
        _id                = from._id;
        _idRecipe          = from._idRecipe;
        _ticker            = from._ticker;
        _tickerRecipe      = from._tickerRecipe;
        _side              = from._side;
        _sideRecipe        = from._sideRecipe;
        _quantity          = from._quantity;
        _quantityRecipe    = from._quantityRecipe;
        _pinnedLimit       = from._pinnedLimit;
        _pinnedStop        = from._pinnedStop;
        _stopPinned        = from._stopPinned;
        _tick              = from._tick;
        _tickRecipe        = from._tickRecipe;
        _timeInForce       = from._timeInForce;
        _timeInForceRecipe = from._timeInForceRecipe;
        _placedAt          = from._placedAt;
        _placedAtRecipe    = from._placedAtRecipe;
    }

    /// <summary>Pins <c>id</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyOrder WithId(Guid value) {
        AnyOrder copy = new AnyOrder(this);

        copy._id = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>id</c> is drawn from — <c>AnyValidId()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyGuid, AnyGuid&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyOrder WithId(Func<AnyGuid, AnyGuid> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyOrder copy = new AnyOrder(this);

        copy._idRecipe = configure(_idRecipe);
        copy._id       = copy._idRecipe;

        return copy;
    }

    /// <summary>Pins <c>ticker</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyOrder WithTicker(Ticker value) {
        AnyOrder copy = new AnyOrder(this);

        copy._ticker = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>ticker</c> is drawn from — <c>AnyValidTicker()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyTicker, AnyTicker&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyOrder WithTicker(Func<AnyTicker, AnyTicker> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyOrder copy = new AnyOrder(this);

        copy._tickerRecipe = configure(_tickerRecipe);
        copy._ticker       = copy._tickerRecipe;

        return copy;
    }

    /// <summary>Pins <c>side</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyOrder WithSide(Side value) {
        AnyOrder copy = new AnyOrder(this);

        copy._side = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>side</c> is drawn from — <c>AnyValidSide()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyEnum&lt;Side&gt;, AnyEnum&lt;Side&gt;&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyOrder WithSide(Func<AnyEnum<Side>, AnyEnum<Side>> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyOrder copy = new AnyOrder(this);

        copy._sideRecipe = configure(_sideRecipe);
        copy._side       = copy._sideRecipe;

        return copy;
    }

    /// <summary>Pins <c>quantity</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyOrder WithQuantity(Quantity value) {
        AnyOrder copy = new AnyOrder(this);

        copy._quantity = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>quantity</c> is drawn from — <c>AnyValidQuantity()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyQuantity, AnyQuantity&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyOrder WithQuantity(Func<AnyQuantity, AnyQuantity> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyOrder copy = new AnyOrder(this);

        copy._quantityRecipe = configure(_quantityRecipe);
        copy._quantity       = copy._quantityRecipe;

        return copy;
    }

    /// <summary>Pins <c>limit</c> to <paramref name="value" />. Drawn otherwise: a whole number of ticks, at least two.</summary>
    public AnyOrder WithLimit(Price value) {
        AnyOrder copy = new AnyOrder(this);

        copy._pinnedLimit = value;

        return copy;
    }

    /// <summary>Pins <c>stop</c> to <paramref name="value" />, <c>null</c> included. Drawn otherwise: a whole number of ticks below the limit, or absent one draw in ten.</summary>
    public AnyOrder WithStop(Price? value) {
        AnyOrder copy = new AnyOrder(this);

        copy._pinnedStop = value;
        copy._stopPinned = true;

        return copy;
    }

    /// <summary>Pins <c>stop</c> to <c>null</c>.</summary>
    public AnyOrder WithoutStop() {
        return WithStop(null);
    }

    /// <summary>Pins <c>tick</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyOrder WithTick(TickSize value) {
        AnyOrder copy = new AnyOrder(this);

        copy._tick = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>tick</c> is drawn from — <c>AnyValidTick()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyTickSize, AnyTickSize&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyOrder WithTick(Func<AnyTickSize, AnyTickSize> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyOrder copy = new AnyOrder(this);

        copy._tickRecipe = configure(_tickRecipe);
        copy._tick       = copy._tickRecipe;

        return copy;
    }

    /// <summary>Pins <c>timeInForce</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyOrder WithTimeInForce(TimeInForce value) {
        AnyOrder copy = new AnyOrder(this);

        copy._timeInForce = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>timeInForce</c> is drawn from — <c>AnyValidTimeInForce()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyEnum&lt;TimeInForce&gt;, AnyEnum&lt;TimeInForce&gt;&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyOrder WithTimeInForce(Func<AnyEnum<TimeInForce>, AnyEnum<TimeInForce>> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyOrder copy = new AnyOrder(this);

        copy._timeInForceRecipe = configure(_timeInForceRecipe);
        copy._timeInForce       = copy._timeInForceRecipe;

        return copy;
    }

    /// <summary>Pins <c>placedAt</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyOrder WithPlacedAt(DateTime value) {
        AnyOrder copy = new AnyOrder(this);

        copy._placedAt = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>placedAt</c> is drawn from — <c>AnyValidPlacedAt()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyDateTime, AnyDateTime&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyOrder WithPlacedAt(Func<AnyDateTime, AnyDateTime> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyOrder copy = new AnyOrder(this);

        copy._placedAtRecipe = configure(_placedAtRecipe);
        copy._placedAt       = copy._placedAtRecipe;

        return copy;
    }

    /// <summary>Produces one arbitrary <see cref="Order" />.</summary>
    public Order Generate() {
        // The limit and the stop sit on the tick grid, and the stop is strictly below the limit: one rule across three
        // parameters, so they are drawn together. A tick, a limit or a stop the test pinned is kept as it is, and the
        // others are drawn to agree with it.
        Price? pinnedStop = _stopPinned ? _pinnedStop : null;

        TickSize tick;
        if (!ReferenceEquals(_tick, _tickRecipe)) {
            tick = _tick.Generate();
        } else if (_pinnedLimit is not null || pinnedStop is not null) {
            tick = FinestTickAdmitting(_pinnedLimit, pinnedStop);
        } else {
            tick = _tick.Generate();
        }

        Price  limit = _pinnedLimit ?? (pinnedStop is null ? DrawLimit(tick, 2) : DrawLimit(tick, TicksOf(pinnedStop, tick) + 1));
        Price? stop  = _stopPinned ? _pinnedStop : DrawStop(tick, limit);

        return new Order(_id.Generate(),
                         _ticker.Generate(),
                         _side.Generate(),
                         _quantity.Generate(),
                         limit,
                         stop,
                         tick,
                         _timeInForce.Generate(),
                         _placedAt.Generate());
    }

    private static long TicksOf(Price price, TickSize tick) {
        return (long)(price.Amount / tick.Increment);
    }

    /// <summary>A tick of the smallest decimal place a pinned limit or stop uses, which admits both.</summary>
    private static TickSize FinestTickAdmitting(Price? limit, Price? stop) {
        int scale = Math.Max(limit?.Amount.Scale ?? 0, stop?.Amount.Scale ?? 0);

        return new TickSize(1m / (decimal)Math.Pow(10, scale));
    }

    private static Price DrawLimit(TickSize tick, long fewestTicks) {
        long mostTicks = (long)decimal.Floor((Price.Ceiling - 0.0001m) / tick.Increment);

        return new Price(Any.Int64().Between(fewestTicks, mostTicks).Generate() * tick.Increment);
    }

    private static Price? DrawStop(TickSize tick, Price limit) {
        return Any.Int64().Between(1, TicksOf(limit, tick) - 1).As(ticks => new Price(ticks * tick.Increment)).OrNull().Generate();
    }

}
