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
    private IAny<Price>       _limit;
    private IAny<Price?>      _stop;
    private IAny<TickSize>    _tick;
    private IAny<TimeInForce> _timeInForce;
    private IAny<DateTime>    _placedAt;

    private AnyGuid              _idRecipe;
    private AnyTicker            _tickerRecipe;
    private AnyEnum<Side>        _sideRecipe;
    private AnyQuantity          _quantityRecipe;
    private AnyPrice             _limitRecipe;
    private AnyPrice             _stopRecipe;
    private AnyTickSize          _tickRecipe;
    private AnyEnum<TimeInForce> _timeInForceRecipe;
    private AnyDateTime          _placedAtRecipe;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyOrder()
        : this(id:          AnyValidId(),
               ticker:      AnyValidTicker(),
               side:        AnyValidSide(),
               quantity:    AnyValidQuantity(),
               limit:       AnyValidLimit(),
               stop:        AnyValidStop(),
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

    private static AnyPrice AnyValidLimit() {
        // The limit is on every tick grid AnyValidTick() draws (whole units), and above every stop AnyValidStop() draws.
        return new AnyPrice().WithAmount(amount => amount.Between(1_001m, 999_999m).MultipleOf(1m));
    }

    private static AnyPrice AnyValidStop() {
        // On every tick grid AnyValidTick() draws, and strictly below every limit AnyValidLimit() draws.
        return new AnyPrice().WithAmount(amount => amount.Between(1m, 1_000m).MultipleOf(1m));
    }

    private static AnyTickSize AnyValidTick() {
        // Only ticks that divide one unit, so that a whole-unit limit and stop are always on the grid.
        return new AnyTickSize().WithIncrement(increment => increment.OneOf(0.0001m, 0.001m, 0.01m, 0.05m, 0.1m, 0.25m, 0.5m, 1m));
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
                     AnyPrice             limit,
                     AnyPrice             stop,
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
        _limit             = limit;
        _limitRecipe       = limit;
        _stop              = stop.OrNull();
        _stopRecipe        = stop;
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
        _limit             = from._limit;
        _limitRecipe       = from._limitRecipe;
        _stop              = from._stop;
        _stopRecipe        = from._stopRecipe;
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

    /// <summary>Pins <c>limit</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyOrder WithLimit(Price value) {
        AnyOrder copy = new AnyOrder(this);

        copy._limit = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>limit</c> is drawn from — <c>AnyValidLimit()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyPrice, AnyPrice&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyOrder WithLimit(Func<AnyPrice, AnyPrice> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyOrder copy = new AnyOrder(this);

        copy._limitRecipe = configure(_limitRecipe);
        copy._limit       = copy._limitRecipe;

        return copy;
    }

    /// <summary>Pins <c>stop</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyOrder WithStop(Price? value) {
        AnyOrder copy = new AnyOrder(this);

        copy._stop = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>stop</c> is drawn from — <c>AnyValidStop()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyPrice, AnyPrice&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyOrder WithStop(Func<AnyPrice, AnyPrice> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyOrder copy = new AnyOrder(this);

        copy._stopRecipe = configure(_stopRecipe);
        copy._stop       = copy._stopRecipe;

        return copy;
    }

    /// <summary>Pins <c>stop</c> to <c>null</c>; its recipe is kept for a later refinement.</summary>
    public AnyOrder WithoutStop() {
        AnyOrder copy = new AnyOrder(this);

        copy._stop = Any.Fixed<Price?>(null);

        return copy;
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
        return new Order(_id.Generate(),
                         _ticker.Generate(),
                         _side.Generate(),
                         _quantity.Generate(),
                         _limit.Generate(),
                         _stop.Generate(),
                         _tick.Generate(),
                         _timeInForce.Generate(),
                         _placedAt.Generate());
    }

}
