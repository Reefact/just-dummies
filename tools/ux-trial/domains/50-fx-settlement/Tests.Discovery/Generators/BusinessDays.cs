using System;

using JustDummies;

namespace FxSettlement.Domain;

/// <summary>Business days (Monday to Friday, no holiday calendar) as the desk reads them.</summary>
public static class BusinessDays {

    /// <summary>Any business day between 2024 and 2030.</summary>
    public static IAny<DateOnly> Any() {
        return OnOrAfter(JustDummies.Any.DateOnly().Between(new DateOnly(2024, 1, 1), new DateOnly(2030, 12, 31)));
    }

    /// <summary>Any business day within a month after <paramref name="from" />, itself included if it is one.</summary>
    public static IAny<DateOnly> OnOrAfter(DateOnly from) {
        return OnOrAfter(JustDummies.Any.DateOnly().Between(from, from.AddDays(30)));
    }

    public static IAny<DateOnly> OnOrAfter(IAny<DateOnly> dates) {
        return dates.As(Next);
    }

    /// <summary>The date itself when it is a business day, otherwise the following Monday.</summary>
    public static DateOnly Next(DateOnly date) {
        while (CurrencyPair.IsWeekend(date)) {
            date = date.AddDays(1);
        }

        return date;
    }

    /// <summary>The trade date that spot-settles on <paramref name="valueDate" />, <paramref name="lagDays" /> business days earlier.</summary>
    public static DateOnly TradeDateFor(DateOnly valueDate, int lagDays) {
        DateOnly date      = valueDate;
        int      remaining = lagDays;
        while (remaining > 0) {
            date = date.AddDays(-1);
            if (!CurrencyPair.IsWeekend(date)) { remaining--; }
        }

        return date;
    }

}
