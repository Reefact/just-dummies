using System;

using JustDummies;

namespace FxSettlement.Domain.Tests.Expert;

/// <summary>Dates of the desk's calendar: no holiday, only weekends. Drawn between 2020 and 2035.</summary>
public static class Days {

    private static readonly DateOnly First = new DateOnly(2020, 1, 1);
    private static readonly DateOnly Last  = new DateOnly(2035, 12, 31);

    /// <summary>Any Monday-to-Friday date.</summary>
    public static IAny<DateOnly> Business() {
        return Any.DateOnly().Between(First, Last).As(date => NextBusinessDay(date));
    }

    /// <summary>Any Monday-to-Friday date from <paramref name="from" /> to <paramref name="to" />, both included.</summary>
    public static IAny<DateOnly> BusinessBetween(DateOnly from, DateOnly to) {
        return Any.DateOnly().Between(from, to).As(date => {
            DateOnly forward = NextBusinessDay(date);

            return forward <= to ? forward : PreviousBusinessDay(date);
        });
    }

    /// <summary>Any Saturday or Sunday.</summary>
    public static IAny<DateOnly> Weekend() {
        return Any.DateOnly().Between(First, Last).As(date => {
            DateOnly saturday = date.AddDays(((int)DayOfWeek.Saturday - (int)date.DayOfWeek + 7) % 7);

            return saturday.AddDays(date.DayNumber % 2);
        });
    }

    /// <summary>Any date falling on <paramref name="day" />.</summary>
    public static IAny<DateOnly> On(DayOfWeek day) {
        return Any.DateOnly().Between(First, Last).As(date => date.AddDays(((int)day - (int)date.DayOfWeek + 7) % 7));
    }

    private static DateOnly NextBusinessDay(DateOnly date) {
        return date.DayOfWeek switch {
            DayOfWeek.Saturday => date.AddDays(2),
            DayOfWeek.Sunday   => date.AddDays(1),
            _                  => date
        };
    }

    private static DateOnly PreviousBusinessDay(DateOnly date) {
        return date.DayOfWeek switch {
            DayOfWeek.Saturday => date.AddDays(-1),
            DayOfWeek.Sunday   => date.AddDays(-2),
            _                  => date
        };
    }

}
