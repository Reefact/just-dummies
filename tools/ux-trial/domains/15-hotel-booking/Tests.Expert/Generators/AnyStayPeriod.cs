// Started from the file `any` scaffolded, then rewritten by hand: the constructor ties its two dates
// through a 30-night cap that no per-parameter recipe can express, so the stay is drawn as a
// check-in date and a number of nights, which makes the check-out follow by construction.

using System;

using JustDummies;

namespace Booking.Domain;

public sealed partial class AnyStayPeriod : IAny<StayPeriod> {

    private IAny<DateOnly> _checkIn;
    private IAny<int>      _nights;

    private AnyDateOnly _checkInRecipe;
    private AnyInt32    _nightsRecipe;

    public AnyStayPeriod()
        : this(checkIn: AnyValidCheckIn(),
               nights:  AnyValidNights()) { }

    private static AnyDateOnly AnyValidCheckIn() {
        return Any.DateOnly().Between(new DateOnly(2000, 1, 1), new DateOnly(2100, 1, 1));
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
        int      nights  = _nights.Generate();

        return new StayPeriod(checkIn, checkIn.AddDays(nights));
    }

}
