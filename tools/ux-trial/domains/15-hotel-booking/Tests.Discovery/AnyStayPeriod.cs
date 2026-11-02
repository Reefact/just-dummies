using System;

using JustDummies;

namespace Booking.Domain;

/// <summary>
///     Hand-written after `any generate StayPeriod`: the scaffold paired checkIn and checkOut as an ordered
///     pair, which cannot honour the thirty-night ceiling. A stay is drawn as a check-in and a number of nights.
/// </summary>
public sealed partial class AnyStayPeriod : IAny<StayPeriod> {

    private readonly IAny<DateOnly> _checkIn;
    private readonly IAny<int>      _nights;

    public AnyStayPeriod()
        : this(Any.DateOnly().Between(new DateOnly(2000, 1, 1), new DateOnly(2100, 1, 1)),
               Any.Int32().Between(1, StayPeriod.MaxNights)) { }

    private AnyStayPeriod(IAny<DateOnly> checkIn, IAny<int> nights) {
        _checkIn = checkIn;
        _nights  = nights;
    }

    public AnyStayPeriod WithCheckIn(DateOnly value) {
        return new AnyStayPeriod(Any.Fixed(value), _nights);
    }

    public AnyStayPeriod WithNights(int value) {
        return new AnyStayPeriod(_checkIn, Any.Fixed(value));
    }

    public StayPeriod Generate() {
        DateOnly checkIn = _checkIn.Generate();

        return new StayPeriod(checkIn, checkIn.AddDays(_nights.Generate()));
    }

}
