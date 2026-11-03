using System;

using JustDummies;

namespace ClinicalTrial.Domain;

/// <summary>
///     The era trial dates are drawn from. Study-day arithmetic moves a date by up to 1 125 days, so a draw
///     over the whole type (year 1 to 9999) would overflow at its ends; no trial runs there.
/// </summary>
public static class TrialCalendar {

    public static readonly DateOnly Earliest = new DateOnly(2010, 1, 1);
    public static readonly DateOnly Latest   = new DateOnly(2040, 12, 31);

    public static AnyDateOnly Date() {
        return Any.DateOnly().Between(Earliest, Latest);
    }

}
