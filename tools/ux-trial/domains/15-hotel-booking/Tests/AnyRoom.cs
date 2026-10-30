// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate Room --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.

using System;

using JustDummies;

namespace Booking.Domain;

/// <summary>
///     A generator of arbitrary <see cref="Room" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyRoom : IAny<Room> {

    private readonly IAny<RoomNumber> _number;
    private readonly IAny<RoomType>   _type;
    private readonly Func<RoomType, IAny<int>> _capacity;
    private readonly IAny<Money>      _nightlyRate;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyRoom()
        : this(number:      AnyValidNumber(),
               type:        AnyValidType(),
               capacity:    AnyValidCapacity,
               nightlyRate: AnyValidNightlyRate()) { }

    private static AnyRoomNumber AnyValidNumber() {
        return new AnyRoomNumber();
    }

    private static AnyEnum<RoomType> AnyValidType() {
        return Any.Enum<RoomType>();
    }

    // Hand-edited: how many a room sleeps depends on its type, drawn first.
    private static AnyInt32 AnyValidCapacity(RoomType type) {
        int maximum = type switch {
            RoomType.Single => 1,
            RoomType.Suite  => 4,
            _               => 2
        };

        return Any.Int32().Between(1, maximum);
    }

    private static AnyMoney AnyValidNightlyRate() {
        // A room is never free.
        return new AnyMoney().WithAmount(amount => amount.Positive());
    }

    private AnyRoom(IAny<RoomNumber> number,
                    IAny<RoomType>   type,
                    Func<RoomType, IAny<int>> capacity,
                    IAny<Money>      nightlyRate) {
        _number      = number;
        _type        = type;
        _capacity    = capacity;
        _nightlyRate = nightlyRate;
    }

    /// <summary>Pins <c>number</c> to a fixed value.</summary>
    public AnyRoom WithNumber(RoomNumber value) {
        return new AnyRoom(new FixedValue<RoomNumber>(value), _type, _capacity, _nightlyRate);
    }

    /// <summary>Draws <c>number</c> from <c>AnyValidNumber()</c>, refined by <paramref name="configure" />.</summary>
    public AnyRoom WithNumber(Func<AnyRoomNumber, AnyRoomNumber> configure) {
        return new AnyRoom(configure(AnyValidNumber()), _type, _capacity, _nightlyRate);
    }

    /// <summary>Pins <c>type</c> to a fixed value.</summary>
    public AnyRoom WithType(RoomType value) {
        return new AnyRoom(_number, new FixedValue<RoomType>(value), _capacity, _nightlyRate);
    }

    /// <summary>Draws <c>type</c> from <c>AnyValidType()</c>, refined by <paramref name="configure" />.</summary>
    public AnyRoom WithType(Func<AnyEnum<RoomType>, AnyEnum<RoomType>> configure) {
        return new AnyRoom(_number, configure(AnyValidType()), _capacity, _nightlyRate);
    }

    /// <summary>Pins <c>capacity</c> to a fixed value.</summary>
    public AnyRoom WithCapacity(int value) {
        return new AnyRoom(_number, _type, _ => new FixedValue<int>(value), _nightlyRate);
    }

    /// <summary>Draws <c>capacity</c> from <c>AnyValidCapacity()</c>, refined by <paramref name="configure" />.</summary>
    public AnyRoom WithCapacity(Func<AnyInt32, AnyInt32> configure) {
        return new AnyRoom(_number, _type, type => configure(AnyValidCapacity(type)), _nightlyRate);
    }

    /// <summary>Pins <c>nightlyRate</c> to a fixed value.</summary>
    public AnyRoom WithNightlyRate(Money value) {
        return new AnyRoom(_number, _type, _capacity, new FixedValue<Money>(value));
    }

    /// <summary>Draws <c>nightlyRate</c> from <c>AnyValidNightlyRate()</c>, refined by <paramref name="configure" />.</summary>
    public AnyRoom WithNightlyRate(Func<AnyMoney, AnyMoney> configure) {
        return new AnyRoom(_number, _type, _capacity, configure(AnyValidNightlyRate()));
    }

    /// <summary>Produces one arbitrary <see cref="Room" />.</summary>
    public Room Generate() {
        RoomType type = _type.Generate();

        return new Room(_number.Generate(),
                        type,
                        _capacity(type).Generate(),
                        _nightlyRate.Generate());
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
