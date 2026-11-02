// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate Room --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.
// A TODO(any) above a `_ = TODO_verify_…` line names a guard the scaffolder could not read; the recipe
// under it is its best generator for the type. Verify it honours that guard, or replace it, then delete
// those two lines. What the scaffolder reads, and what it cannot:
// https://github.com/Reefact/just-dummies/blob/main/doc/handwritten/for-users/packages/justdummies-cli.en.md#what-the-last-column-means

using System;

using JustDummies;

namespace Booking.Domain;

/// <summary>
///     A generator of arbitrary <see cref="Room" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyRoom : IAny<Room> {

    private IAny<RoomNumber> _number;
    private IAny<RoomType>   _type;
    private int?              _capacityPin;
    private IAny<Money>      _nightlyRate;

    private AnyRoomNumber     _numberRecipe;
    private AnyEnum<RoomType> _typeRecipe;
    private AnyInt32          _capacityRecipe;
    private AnyMoney          _nightlyRateRecipe;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyRoom()
        : this(number:      AnyValidNumber(),
               type:        AnyValidType(),
               capacity:    AnyValidCapacity(),
               nightlyRate: AnyValidNightlyRate()) { }

    private static AnyRoomNumber AnyValidNumber() {
        return new AnyRoomNumber();
    }

    private static AnyEnum<RoomType> AnyValidType() {
        return Any.Enum<RoomType>();
    }

    private static AnyInt32 AnyValidCapacity() {
        // Narrowed to the type's maximum in Generate(): the cap depends on the drawn type.
        return Any.Int32().Positive();
    }

    private static AnyMoney AnyValidNightlyRate() {
        // A room is never free, and a hotel's nightly rate stays far below the decimal range.
        return new AnyMoney().WithAmount(a => a.Between(0.01m, 100_000m));
    }

    /// <summary>Draws every parameter from the recipe given for it.</summary>
    private AnyRoom(AnyRoomNumber     number,
                    AnyEnum<RoomType> type,
                    AnyInt32          capacity,
                    AnyMoney          nightlyRate) {
        _number            = number;
        _numberRecipe      = number;
        _type              = type;
        _typeRecipe        = type;
        _capacityRecipe    = capacity;
        _nightlyRate       = nightlyRate;
        _nightlyRateRecipe = nightlyRate;
    }

    /// <summary>Copies <paramref name="from" />, for a <c>With…</c> method to change one parameter of before handing it out.</summary>
    private AnyRoom(AnyRoom from) {
        _number            = from._number;
        _numberRecipe      = from._numberRecipe;
        _type              = from._type;
        _typeRecipe        = from._typeRecipe;
        _capacityPin       = from._capacityPin;
        _capacityRecipe    = from._capacityRecipe;
        _nightlyRate       = from._nightlyRate;
        _nightlyRateRecipe = from._nightlyRateRecipe;
    }

    /// <summary>Pins <c>number</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyRoom WithNumber(RoomNumber value) {
        AnyRoom copy = new AnyRoom(this);

        copy._number = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>number</c> is drawn from — <c>AnyValidNumber()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyRoomNumber, AnyRoomNumber&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyRoom WithNumber(Func<AnyRoomNumber, AnyRoomNumber> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyRoom copy = new AnyRoom(this);

        copy._numberRecipe = configure(_numberRecipe);
        copy._number       = copy._numberRecipe;

        return copy;
    }

    /// <summary>Pins <c>type</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyRoom WithType(RoomType value) {
        AnyRoom copy = new AnyRoom(this);

        copy._type = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>type</c> is drawn from — <c>AnyValidType()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyEnum&lt;RoomType&gt;, AnyEnum&lt;RoomType&gt;&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyRoom WithType(Func<AnyEnum<RoomType>, AnyEnum<RoomType>> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyRoom copy = new AnyRoom(this);

        copy._typeRecipe = configure(_typeRecipe);
        copy._type       = copy._typeRecipe;

        return copy;
    }

    /// <summary>Pins <c>capacity</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyRoom WithCapacity(int value) {
        AnyRoom copy = new AnyRoom(this);

        copy._capacityPin = value;

        return copy;
    }

    /// <summary>Refines the recipe <c>capacity</c> is drawn from — <c>AnyValidCapacity()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyInt32, AnyInt32&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyRoom WithCapacity(Func<AnyInt32, AnyInt32> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyRoom copy = new AnyRoom(this);

        copy._capacityRecipe = configure(_capacityRecipe);
        copy._capacityPin    = null;

        return copy;
    }

    /// <summary>Pins <c>nightlyRate</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyRoom WithNightlyRate(Money value) {
        AnyRoom copy = new AnyRoom(this);

        copy._nightlyRate = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>nightlyRate</c> is drawn from — <c>AnyValidNightlyRate()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyMoney, AnyMoney&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyRoom WithNightlyRate(Func<AnyMoney, AnyMoney> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyRoom copy = new AnyRoom(this);

        copy._nightlyRateRecipe = configure(_nightlyRateRecipe);
        copy._nightlyRate       = copy._nightlyRateRecipe;

        return copy;
    }

    /// <summary>Produces one arbitrary <see cref="Room" />.</summary>
    public Room Generate() {
        RoomNumber number = _number.Generate();
        RoomType   type   = _type.Generate();
        int        capacity = _capacityPin ?? _capacityRecipe.LessThanOrEqualTo(MaximumCapacityOf(type)).Generate();

        return new Room(number, type, capacity, _nightlyRate.Generate());
    }

    private static int MaximumCapacityOf(RoomType type) {
        return type switch {
            RoomType.Single => 1,
            RoomType.Double => 2,
            RoomType.Twin   => 2,
            RoomType.Suite  => 4,
            _               => throw new ArgumentOutOfRangeException(nameof(type))
        };
    }

}
