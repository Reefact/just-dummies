using System;

using JustDummies;

namespace FxSettlement.Domain.Tests.Regular;

/// <summary>Business days (Monday to Friday, no holiday calendar) as the desk reads them.</summary>
public static class BusinessDays {

    private static readonly DateOnly FirstMonday = new DateOnly(2020, 1, 6);

    /// <summary>A business day somewhere between 2020 and 2039.</summary>
    public static IAny<DateOnly> Any() {
        return JustDummies.Any.Combine(JustDummies.Any.Int32().Between(0, 1_000),
                                       JustDummies.Any.Int32().Between(0, 4),
                                       (weeks, dayOfWeek) => FirstMonday.AddDays(7 * weeks + dayOfWeek));
    }

    public static bool IsWeekend(DateOnly date) {
        return date.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday;
    }

    public static DateOnly Plus(DateOnly businessDay, int days) {
        DateOnly date = businessDay;
        while (days > 0) {
            date = date.AddDays(1);
            if (!IsWeekend(date)) { days--; }
        }

        return date;
    }

    public static DateOnly Minus(DateOnly businessDay, int days) {
        DateOnly date = businessDay;
        while (days > 0) {
            date = date.AddDays(-1);
            if (!IsWeekend(date)) { days--; }
        }

        return date;
    }

}
