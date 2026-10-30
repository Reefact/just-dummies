// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate Reservation --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.

using System;
using System.Collections.Generic;

using JustDummies;

namespace Booking.Domain;

/// <summary>
///     A generator of arbitrary <see cref="Reservation" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyReservation : IAny<Reservation> {

    private readonly IAny<Guid>                     _id;
    private readonly IAny<Room>                     _room;
    private readonly IAny<StayPeriod>               _stay;
    private readonly Func<Room, IAny<IReadOnlyList<GuestName>>> _guests;
    private readonly IAny<CancellationPolicy>       _policy;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyReservation()
        : this(id:     AnyValidId(),
               room:   AnyValidRoom(),
               stay:   AnyValidStay(),
               guests: AnyValidGuests,
               policy: AnyValidPolicy()) { }

    private static AnyGuid AnyValidId() {
        return Any.Guid().NonEmpty();
    }

    private static AnyRoom AnyValidRoom() {
        return new AnyRoom();
    }

    private static AnyStayPeriod AnyValidStay() {
        return new AnyStayPeriod();
    }

    // Hand-edited: at least one guest, and no more than the room drawn first sleeps.
    private static AnyList<GuestName> AnyValidGuests(Room room) {
        return Any.ListOf(new AnyGuestName()).WithCountBetween(1, room.Capacity);
    }

    private static AnyCancellationPolicy AnyValidPolicy() {
        return new AnyCancellationPolicy();
    }

    private AnyReservation(IAny<Guid>                     id,
                           IAny<Room>                     room,
                           IAny<StayPeriod>               stay,
                           Func<Room, IAny<IReadOnlyList<GuestName>>> guests,
                           IAny<CancellationPolicy>       policy) {
        _id     = id;
        _room   = room;
        _stay   = stay;
        _guests = guests;
        _policy = policy;
    }

    /// <summary>Pins <c>id</c> to a fixed value.</summary>
    public AnyReservation WithId(Guid value) {
        return new AnyReservation(new FixedValue<Guid>(value), _room, _stay, _guests, _policy);
    }

    /// <summary>Draws <c>id</c> from <c>AnyValidId()</c>, refined by <paramref name="configure" />.</summary>
    public AnyReservation WithId(Func<AnyGuid, AnyGuid> configure) {
        return new AnyReservation(configure(AnyValidId()), _room, _stay, _guests, _policy);
    }

    /// <summary>Pins <c>room</c> to a fixed value.</summary>
    public AnyReservation WithRoom(Room value) {
        return new AnyReservation(_id, new FixedValue<Room>(value), _stay, _guests, _policy);
    }

    /// <summary>Draws <c>room</c> from <c>AnyValidRoom()</c>, refined by <paramref name="configure" />.</summary>
    public AnyReservation WithRoom(Func<AnyRoom, AnyRoom> configure) {
        return new AnyReservation(_id, configure(AnyValidRoom()), _stay, _guests, _policy);
    }

    /// <summary>Pins <c>stay</c> to a fixed value.</summary>
    public AnyReservation WithStay(StayPeriod value) {
        return new AnyReservation(_id, _room, new FixedValue<StayPeriod>(value), _guests, _policy);
    }

    /// <summary>Draws <c>stay</c> from <c>AnyValidStay()</c>, refined by <paramref name="configure" />.</summary>
    public AnyReservation WithStay(Func<AnyStayPeriod, AnyStayPeriod> configure) {
        return new AnyReservation(_id, _room, configure(AnyValidStay()), _guests, _policy);
    }

    /// <summary>Pins <c>guests</c> to a fixed value.</summary>
    public AnyReservation WithGuests(IReadOnlyList<GuestName> value) {
        return new AnyReservation(_id, _room, _stay, _ => new FixedValue<IReadOnlyList<GuestName>>(value), _policy);
    }

    /// <summary>Draws <c>guests</c> from <c>AnyValidGuests()</c>, refined by <paramref name="configure" />.</summary>
    public AnyReservation WithGuests(Func<AnyList<GuestName>, AnyList<GuestName>> configure) {
        return new AnyReservation(_id, _room, _stay, room => configure(AnyValidGuests(room)), _policy);
    }

    /// <summary>Pins <c>policy</c> to a fixed value.</summary>
    public AnyReservation WithPolicy(CancellationPolicy value) {
        return new AnyReservation(_id, _room, _stay, _guests, new FixedValue<CancellationPolicy>(value));
    }

    /// <summary>Draws <c>policy</c> from <c>AnyValidPolicy()</c>, refined by <paramref name="configure" />.</summary>
    public AnyReservation WithPolicy(Func<AnyCancellationPolicy, AnyCancellationPolicy> configure) {
        return new AnyReservation(_id, _room, _stay, _guests, configure(AnyValidPolicy()));
    }

    /// <summary>Produces one arbitrary <see cref="Reservation" />.</summary>
    public Reservation Generate() {
        Room room = _room.Generate();

        return new Reservation(_id.Generate(),
                               room,
                               _stay.Generate(),
                               _guests(room).Generate(),
                               _policy.Generate());
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
