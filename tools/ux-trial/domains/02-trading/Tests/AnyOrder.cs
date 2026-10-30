// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate Order --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.

using System;

using JustDummies;

namespace Trading.Domain;

/// <summary>
///     A generator of arbitrary <see cref="Order" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyOrder : IAny<Order> {

    private readonly IAny<Guid>        _id;
    private readonly IAny<Ticker>      _ticker;
    private readonly IAny<Side>        _side;
    private readonly IAny<Quantity>    _quantity;
    private readonly IAny<Price>       _limit;
    private readonly IAny<Price?>      _stop;
    private readonly IAny<TickSize>    _tick;
    private readonly IAny<TimeInForce> _timeInForce;
    private readonly IAny<DateTime>    _placedAt;

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

    private static IAny<Price> AnyValidLimit() {
        // Two decimal places: on the grid of every tick AnyValidTick() draws. AnyPrice's base is
        // WithScale(4), and a refinement to WithScale(2) conflicts with it, so the base is restated.
        return Any.Decimal().Positive().LessThan(Price.Ceiling).WithScale(2).As(amount => new Price(amount));
    }

    private static IAny<Price?> AnyValidStop() {
        // Order: a stop is strictly below the limit, a relation between two parameters that no
        // per-parameter base can state. A plain limit order is the arbitrary case; a test about
        // stops pins both prices.
        return new NoStop();
    }

    private static AnyTickSize AnyValidTick() {
        // Order: limit and stop are multiples of the tick. Every tick here divides 0.01, so any
        // two-decimal price sits on the grid whichever tick is drawn.
        return new AnyTickSize().WithIncrement(increment => increment.OneOf(0.0001m, 0.0005m, 0.001m, 0.005m, 0.01m));
    }

    private static AnyEnum<TimeInForce> AnyValidTimeInForce() {
        return Any.Enum<TimeInForce>();
    }

    private static AnyDateTime AnyValidPlacedAt() {
        // Order: placedAt is UTC.
        return Any.DateTime().Between(new DateTime(2000, 1, 1, 0, 0, 0, DateTimeKind.Utc), new DateTime(2100, 1, 1, 0, 0, 0, DateTimeKind.Utc));
    }

    private AnyOrder(IAny<Guid>        id,
                     IAny<Ticker>      ticker,
                     IAny<Side>        side,
                     IAny<Quantity>    quantity,
                     IAny<Price>       limit,
                     IAny<Price?>      stop,
                     IAny<TickSize>    tick,
                     IAny<TimeInForce> timeInForce,
                     IAny<DateTime>    placedAt) {
        _id          = id;
        _ticker      = ticker;
        _side        = side;
        _quantity    = quantity;
        _limit       = limit;
        _stop        = stop;
        _tick        = tick;
        _timeInForce = timeInForce;
        _placedAt    = placedAt;
    }

    /// <summary>Pins <c>id</c> to a fixed value.</summary>
    public AnyOrder WithId(Guid value) {
        return new AnyOrder(new FixedValue<Guid>(value), _ticker, _side, _quantity, _limit, _stop, _tick, _timeInForce, _placedAt);
    }

    /// <summary>Draws <c>id</c> from <c>AnyValidId()</c>, refined by <paramref name="configure" />.</summary>
    public AnyOrder WithId(Func<AnyGuid, AnyGuid> configure) {
        return new AnyOrder(configure(AnyValidId()), _ticker, _side, _quantity, _limit, _stop, _tick, _timeInForce, _placedAt);
    }

    /// <summary>Pins <c>ticker</c> to a fixed value.</summary>
    public AnyOrder WithTicker(Ticker value) {
        return new AnyOrder(_id, new FixedValue<Ticker>(value), _side, _quantity, _limit, _stop, _tick, _timeInForce, _placedAt);
    }

    /// <summary>Draws <c>ticker</c> from <c>AnyValidTicker()</c>, refined by <paramref name="configure" />.</summary>
    public AnyOrder WithTicker(Func<AnyTicker, AnyTicker> configure) {
        return new AnyOrder(_id, configure(AnyValidTicker()), _side, _quantity, _limit, _stop, _tick, _timeInForce, _placedAt);
    }

    /// <summary>Pins <c>side</c> to a fixed value.</summary>
    public AnyOrder WithSide(Side value) {
        return new AnyOrder(_id, _ticker, new FixedValue<Side>(value), _quantity, _limit, _stop, _tick, _timeInForce, _placedAt);
    }

    /// <summary>Draws <c>side</c> from <c>AnyValidSide()</c>, refined by <paramref name="configure" />.</summary>
    public AnyOrder WithSide(Func<AnyEnum<Side>, AnyEnum<Side>> configure) {
        return new AnyOrder(_id, _ticker, configure(AnyValidSide()), _quantity, _limit, _stop, _tick, _timeInForce, _placedAt);
    }

    /// <summary>Pins <c>quantity</c> to a fixed value.</summary>
    public AnyOrder WithQuantity(Quantity value) {
        return new AnyOrder(_id, _ticker, _side, new FixedValue<Quantity>(value), _limit, _stop, _tick, _timeInForce, _placedAt);
    }

    /// <summary>Draws <c>quantity</c> from <c>AnyValidQuantity()</c>, refined by <paramref name="configure" />.</summary>
    public AnyOrder WithQuantity(Func<AnyQuantity, AnyQuantity> configure) {
        return new AnyOrder(_id, _ticker, _side, configure(AnyValidQuantity()), _limit, _stop, _tick, _timeInForce, _placedAt);
    }

    /// <summary>Pins <c>limit</c> to a fixed value.</summary>
    public AnyOrder WithLimit(Price value) {
        return new AnyOrder(_id, _ticker, _side, _quantity, new FixedValue<Price>(value), _stop, _tick, _timeInForce, _placedAt);
    }

    /// <summary>Pins <c>stop</c> to a fixed value: a stop-limit order. The stop must sit below the limit.</summary>
    public AnyOrder WithStop(Price value) {
        return new AnyOrder(_id, _ticker, _side, _quantity, _limit, new FixedValue<Price?>(value), _tick, _timeInForce, _placedAt);
    }

    /// <summary>Pins <c>tick</c> to a fixed value.</summary>
    public AnyOrder WithTick(TickSize value) {
        return new AnyOrder(_id, _ticker, _side, _quantity, _limit, _stop, new FixedValue<TickSize>(value), _timeInForce, _placedAt);
    }

    /// <summary>Draws <c>tick</c> from <c>AnyValidTick()</c>, refined by <paramref name="configure" />.</summary>
    public AnyOrder WithTick(Func<AnyTickSize, AnyTickSize> configure) {
        return new AnyOrder(_id, _ticker, _side, _quantity, _limit, _stop, configure(AnyValidTick()), _timeInForce, _placedAt);
    }

    /// <summary>Pins <c>timeInForce</c> to a fixed value.</summary>
    public AnyOrder WithTimeInForce(TimeInForce value) {
        return new AnyOrder(_id, _ticker, _side, _quantity, _limit, _stop, _tick, new FixedValue<TimeInForce>(value), _placedAt);
    }

    /// <summary>Draws <c>timeInForce</c> from <c>AnyValidTimeInForce()</c>, refined by <paramref name="configure" />.</summary>
    public AnyOrder WithTimeInForce(Func<AnyEnum<TimeInForce>, AnyEnum<TimeInForce>> configure) {
        return new AnyOrder(_id, _ticker, _side, _quantity, _limit, _stop, _tick, configure(AnyValidTimeInForce()), _placedAt);
    }

    /// <summary>Pins <c>placedAt</c> to a fixed value.</summary>
    public AnyOrder WithPlacedAt(DateTime value) {
        return new AnyOrder(_id, _ticker, _side, _quantity, _limit, _stop, _tick, _timeInForce, new FixedValue<DateTime>(value));
    }

    /// <summary>Draws <c>placedAt</c> from <c>AnyValidPlacedAt()</c>, refined by <paramref name="configure" />.</summary>
    public AnyOrder WithPlacedAt(Func<AnyDateTime, AnyDateTime> configure) {
        return new AnyOrder(_id, _ticker, _side, _quantity, _limit, _stop, _tick, _timeInForce, configure(AnyValidPlacedAt()));
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

    private sealed class FixedValue<TValue> : IAny<TValue> {

        private readonly TValue _value;

        public FixedValue(TValue value) {
            _value = value;
        }

        public TValue Generate() {
            return _value;
        }

    }

}
