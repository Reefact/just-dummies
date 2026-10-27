using Booking.Domain;
using JustDummies;
using Xunit;

namespace Booking.Tests;

/// <summary>Never run: each method is a mistake an analyzer could catch at build time.</summary>
internal static class AnalyzerProbes {

    // A. A drawn value flowing into the assertion: the test is a property with a sample size of one.
    public static void Drawn_value_in_assertion() {
        DateOnly   anyCheckIn = Any.DateOnly().Between(new DateOnly(2025, 1, 1), new DateOnly(2035, 12, 31)).Generate();
        StayPeriod stay       = new StayPeriod(anyCheckIn, anyCheckIn.AddDays(2));
        Assert.Equal(anyCheckIn, stay.CheckIn);
    }

    // B. A contradictory constant date interval: check-out strictly after and strictly before the same day.
    public static void Contradictory_constant_dates() {
        DateOnly impossible = Any.DateOnly().After(new DateOnly(2025, 6, 1)).Before(new DateOnly(2025, 6, 1)).Generate();
    }

    // C. Reversed constant date bounds.
    public static void Reversed_constant_dates() {
        DateOnly impossible = Any.DateOnly().Between(new DateOnly(2025, 6, 6), new DateOnly(2025, 6, 1)).Generate();
    }

    // D. An overnight arrival window, written the way TimeOnly.IsBetween reads it.
    public static void Overnight_time_window() {
        TimeOnly lateArrival = Any.TimeOnly().Between(new TimeOnly(22, 0), new TimeOnly(2, 0)).Generate();
    }

    // E. More distinct room types than the enum has.
    public static void Too_many_distinct_room_types() {
        HashSet<RoomType> impossible = Any.SetOf(Any.Enum<RoomType>()).WithCount(5).Generate();
    }

    // F. Distinct guest names, but GuestName has no value equality.
    public static void Distinct_without_value_equality() {
        List<GuestName> anyGuests = Any.ListOf(new AnyGuestName()).WithCount(3).Distinct().Generate();
    }

    // G. A combine operand drawn and thrown away.
    public static void Unused_combine_operand() {
        IAny<StayPeriod> anyStay = Any.Combine(Any.DateOnly().Between(new DateOnly(2025, 1, 1), new DateOnly(2035, 1, 1)),
                                               Any.Int32().Between(1, 30),
                                               (checkIn, nights) => new StayPeriod(checkIn, checkIn.AddDays(3)));
    }

    // H. A seed scope opened and discarded.
    public static void Discarded_seed_scope() {
        // Any.UseSeed(42);   // JD004 fired here as a build ERROR (see NOTES.md); commented out so the suite builds.
    }

    // I. A lookahead in a constant pattern.
    public static void Unsupported_constant_pattern() {
        string impossible = Any.StringMatching(@"^(?=.*\d)[A-Z0-9]{6}$").Generate();
    }

    // J. A decimal scale finer than the declared interval admits.
    public static void Scale_finer_than_interval() {
        decimal impossible = Any.Decimal().Between(0.001m, 0.009m).WithScale(2).Generate();
    }

    // K. Nights bounded in a way no integer satisfies.
    public static void Contradictory_constant_nights() {
        int impossible = Any.Int32().Between(1, 30).GreaterThan(30).Generate();
    }

}
