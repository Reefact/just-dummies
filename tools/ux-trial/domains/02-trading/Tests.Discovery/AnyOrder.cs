// Scaffolded by `any`, then rewritten by hand: limit, stop and tick are tied by the tick grid and by
// "stop strictly below limit", which the scaffold cannot express per parameter. Limit and stop are
// therefore derived from the tick inside Generate(), and pinned (not refined) when a test needs one.

using System;

using JustDummies;

namespace Trading.Domain;

public sealed partial class AnyOrder : IAny<Order> {

    private const decimal LargestPrice = 999_999.9999m;

    private IAny<Guid>        _id          = Any.Guid().NonEmpty();
    private IAny<Ticker>      _ticker      = new AnyTicker();
    private IAny<Side>        _side        = Any.Enum<Side>();
    private IAny<Quantity>    _quantity    = new AnyQuantity();
    private IAny<TickSize>    _tick        = new AnyTickSize();
    private IAny<TimeInForce> _timeInForce = Any.Enum<TimeInForce>();
    private IAny<DateTime>    _placedAt    = Any.DateTime();

    private Price? _limitPin;
    private bool   _stopPinned;
    private Price? _stopPin;

    public AnyOrder() { }

    private AnyOrder(AnyOrder from) {
        _id          = from._id;
        _ticker      = from._ticker;
        _side        = from._side;
        _quantity    = from._quantity;
        _tick        = from._tick;
        _timeInForce = from._timeInForce;
        _placedAt    = from._placedAt;
        _limitPin    = from._limitPin;
        _stopPinned  = from._stopPinned;
        _stopPin     = from._stopPin;
    }

    public AnyOrder WithId(Guid value) {
        AnyOrder copy = new AnyOrder(this);
        copy._id = Any.Fixed(value);
        return copy;
    }

    public AnyOrder WithTicker(Ticker value) {
        AnyOrder copy = new AnyOrder(this);
        copy._ticker = Any.Fixed(value);
        return copy;
    }

    public AnyOrder WithSide(Side value) {
        AnyOrder copy = new AnyOrder(this);
        copy._side = Any.Fixed(value);
        return copy;
    }

    public AnyOrder WithQuantity(Quantity value) {
        AnyOrder copy = new AnyOrder(this);
        copy._quantity = Any.Fixed(value);
        return copy;
    }

    public AnyOrder WithTick(TickSize value) {
        AnyOrder copy = new AnyOrder(this);
        copy._tick = Any.Fixed(value);
        return copy;
    }

    public AnyOrder WithTimeInForce(TimeInForce value) {
        AnyOrder copy = new AnyOrder(this);
        copy._timeInForce = Any.Fixed(value);
        return copy;
    }

    public AnyOrder WithPlacedAt(DateTime value) {
        AnyOrder copy = new AnyOrder(this);
        copy._placedAt = Any.Fixed(value);
        return copy;
    }

    public AnyOrder WithLimit(Price value) {
        AnyOrder copy = new AnyOrder(this);
        copy._limitPin = value;
        return copy;
    }

    public AnyOrder WithStop(Price value) {
        AnyOrder copy = new AnyOrder(this);
        copy._stopPinned = true;
        copy._stopPin    = value;
        return copy;
    }

    public AnyOrder WithoutStop() {
        AnyOrder copy = new AnyOrder(this);
        copy._stopPinned = true;
        copy._stopPin    = null;
        return copy;
    }

    public Order Generate() {
        TickSize tick       = _tick.Generate();
        decimal  increment  = tick.Increment;
        long     largestTick = (long)decimal.Floor(LargestPrice / increment);

        Price? stop       = _stopPin;
        bool   wantsStop  = _stopPinned ? _stopPin is not null : Any.Boolean().Generate();
        Price  limit;
        if (_limitPin is not null) {
            limit = _limitPin;
        } else {
            long lowest = stop is not null ? (long)(stop.Amount / increment) + 1 : (wantsStop ? 2 : 1);
            limit = new Price(Any.Int64().Between(lowest, largestTick).Generate() * increment);
        }
        if (!_stopPinned && wantsStop) {
            long limitTicks = (long)(limit.Amount / increment);
            stop = limitTicks >= 2 ? new Price(Any.Int64().Between(1, limitTicks - 1).Generate() * increment) : null;
        }

        return new Order(_id.Generate(), _ticker.Generate(), _side.Generate(), _quantity.Generate(), limit, stop, tick, _timeInForce.Generate(), _placedAt.Generate());
    }

}
