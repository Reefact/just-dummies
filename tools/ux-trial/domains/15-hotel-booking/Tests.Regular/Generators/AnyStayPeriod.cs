// Scaffolded by `any`, then rewritten by hand: StayPeriod is a start plus a duration of 1 to MaxNights,
// which the scaffolded pair could not express (it left the 30-night guard to verify).
using System;

using JustDummies;

namespace Booking.Domain;

/// <summary>A generator of arbitrary <see cref="StayPeriod" /> values: a check-in and a number of nights.</summary>
public sealed partial class AnyStayPeriod : IAny<StayPeriod> {

    private IAny<DateOnly> _checkIn;
    private IAny<int>      _nights;

    private AnyDateOnly _checkInRecipe;
    private AnyInt32    _nightsRecipe;

    public AnyStayPeriod()
        : this(checkIn: AnyValidCheckIn(),
               nights:  AnyValidNights()) { }

    private static AnyDateOnly AnyValidCheckIn() {
        return Any.DateOnly().Between(new DateOnly(2020, 1, 1), new DateOnly(2040, 12, 31));
    }

    private static AnyInt32 AnyValidNights() {
        return Any.Int32().Between(1, StayPeriod.MaxNights);
    }

    private AnyStayPeriod(AnyDateOnly checkIn, AnyInt32 nights) {
        _checkIn       = checkIn;
        _checkInRecipe = checkIn;
        _nights        = nights;
        _nightsRecipe  = nights;
    }

    private AnyStayPeriod(AnyStayPeriod from) {
        _checkIn       = from._checkIn;
        _checkInRecipe = from._checkInRecipe;
        _nights        = from._nights;
        _nightsRecipe  = from._nightsRecipe;
    }

    public AnyStayPeriod WithCheckIn(DateOnly value) {
        AnyStayPeriod copy = new AnyStayPeriod(this);

        copy._checkIn = Any.Fixed(value);

        return copy;
    }

    public AnyStayPeriod WithCheckIn(Func<AnyDateOnly, AnyDateOnly> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyStayPeriod copy = new AnyStayPeriod(this);

        copy._checkInRecipe = configure(_checkInRecipe);
        copy._checkIn       = copy._checkInRecipe;

        return copy;
    }

    public AnyStayPeriod WithNights(int value) {
        AnyStayPeriod copy = new AnyStayPeriod(this);

        copy._nights = Any.Fixed(value);

        return copy;
    }

    public AnyStayPeriod WithNights(Func<AnyInt32, AnyInt32> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyStayPeriod copy = new AnyStayPeriod(this);

        copy._nightsRecipe = configure(_nightsRecipe);
        copy._nights       = copy._nightsRecipe;

        return copy;
    }

    public StayPeriod Generate() {
        DateOnly checkIn = _checkIn.Generate();

        return new StayPeriod(checkIn, checkIn.AddDays(_nights.Generate()));
    }

}
