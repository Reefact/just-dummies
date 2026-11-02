// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate Reservation --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.
// A TODO(any) above a `_ = TODO_verify_…` line names a guard the scaffolder could not read; the recipe
// under it is its best generator for the type. Verify it honours that guard, or replace it, then delete
// those two lines. What the scaffolder reads, and what it cannot:
// https://github.com/Reefact/just-dummies/blob/main/doc/handwritten/for-users/packages/justdummies-cli.en.md#what-the-last-column-means

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

    private IAny<Guid>                     _id;
    private IAny<Room>                     _room;
    private IAny<StayPeriod>               _stay;
    private IReadOnlyList<GuestName>?      _guestsPin;
    private bool                           _guestsPinned;
    private IAny<CancellationPolicy>       _policy;

    private AnyGuid               _idRecipe;
    private AnyRoom               _roomRecipe;
    private AnyStayPeriod         _stayRecipe;
    private AnyList<GuestName>    _guestsRecipe;
    private AnyCancellationPolicy _policyRecipe;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyReservation()
        : this(id:     AnyValidId(),
               room:   AnyValidRoom(),
               stay:   AnyValidStay(),
               guests: AnyValidGuests(),
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

    private static AnyList<GuestName> AnyValidGuests() {
        // Narrowed to the drawn room's capacity in Generate().
        return Any.ListOf(new AnyGuestName());
    }

    private static AnyCancellationPolicy AnyValidPolicy() {
        return new AnyCancellationPolicy();
    }

    /// <summary>Draws every parameter from the recipe given for it.</summary>
    private AnyReservation(AnyGuid               id,
                           AnyRoom               room,
                           AnyStayPeriod         stay,
                           AnyList<GuestName>    guests,
                           AnyCancellationPolicy policy) {
        _id           = id;
        _idRecipe     = id;
        _room         = room;
        _roomRecipe   = room;
        _stay         = stay;
        _stayRecipe   = stay;
        _guestsRecipe = guests;
        _policy       = policy;
        _policyRecipe = policy;
    }

    /// <summary>Copies <paramref name="from" />, for a <c>With…</c> method to change one parameter of before handing it out.</summary>
    private AnyReservation(AnyReservation from) {
        _id           = from._id;
        _idRecipe     = from._idRecipe;
        _room         = from._room;
        _roomRecipe   = from._roomRecipe;
        _stay         = from._stay;
        _stayRecipe   = from._stayRecipe;
        _guestsPin    = from._guestsPin;
        _guestsPinned = from._guestsPinned;
        _guestsRecipe = from._guestsRecipe;
        _policy       = from._policy;
        _policyRecipe = from._policyRecipe;
    }

    /// <summary>Pins <c>id</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyReservation WithId(Guid value) {
        AnyReservation copy = new AnyReservation(this);

        copy._id = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>id</c> is drawn from — <c>AnyValidId()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyGuid, AnyGuid&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyReservation WithId(Func<AnyGuid, AnyGuid> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyReservation copy = new AnyReservation(this);

        copy._idRecipe = configure(_idRecipe);
        copy._id       = copy._idRecipe;

        return copy;
    }

    /// <summary>Pins <c>room</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyReservation WithRoom(Room value) {
        AnyReservation copy = new AnyReservation(this);

        copy._room = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>room</c> is drawn from — <c>AnyValidRoom()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyRoom, AnyRoom&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyReservation WithRoom(Func<AnyRoom, AnyRoom> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyReservation copy = new AnyReservation(this);

        copy._roomRecipe = configure(_roomRecipe);
        copy._room       = copy._roomRecipe;

        return copy;
    }

    /// <summary>Pins <c>stay</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyReservation WithStay(StayPeriod value) {
        AnyReservation copy = new AnyReservation(this);

        copy._stay = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>stay</c> is drawn from — <c>AnyValidStay()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyStayPeriod, AnyStayPeriod&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyReservation WithStay(Func<AnyStayPeriod, AnyStayPeriod> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyReservation copy = new AnyReservation(this);

        copy._stayRecipe = configure(_stayRecipe);
        copy._stay       = copy._stayRecipe;

        return copy;
    }

    /// <summary>Pins <c>guests</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyReservation WithGuests(IReadOnlyList<GuestName> value) {
        AnyReservation copy = new AnyReservation(this);

        copy._guestsPin    = value;
        copy._guestsPinned = true;

        return copy;
    }

    /// <summary>Refines the recipe <c>guests</c> is drawn from — <c>AnyValidGuests()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyList&lt;GuestName&gt;, AnyList&lt;GuestName&gt;&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyReservation WithGuests(Func<AnyList<GuestName>, AnyList<GuestName>> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyReservation copy = new AnyReservation(this);

        copy._guestsRecipe = configure(_guestsRecipe);
        copy._guestsPin    = null;
        copy._guestsPinned = false;

        return copy;
    }

    /// <summary>Pins <c>policy</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyReservation WithPolicy(CancellationPolicy value) {
        AnyReservation copy = new AnyReservation(this);

        copy._policy = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>policy</c> is drawn from — <c>AnyValidPolicy()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyCancellationPolicy, AnyCancellationPolicy&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyReservation WithPolicy(Func<AnyCancellationPolicy, AnyCancellationPolicy> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyReservation copy = new AnyReservation(this);

        copy._policyRecipe = configure(_policyRecipe);
        copy._policy       = copy._policyRecipe;

        return copy;
    }

    /// <summary>Produces one arbitrary <see cref="Reservation" />.</summary>
    public Reservation Generate() {
        Room room = _room.Generate();
        IReadOnlyList<GuestName> guests = _guestsPinned ? _guestsPin! : _guestsRecipe.WithCountBetween(1, room?.Capacity ?? 1).Generate();

        return new Reservation(_id.Generate(),
                               room!,
                               _stay.Generate(),
                               guests,
                               _policy.Generate());
    }

}
