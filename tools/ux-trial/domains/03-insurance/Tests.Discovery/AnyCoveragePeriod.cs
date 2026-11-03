// Scaffolded by `any` (JustDummies), then rewritten by hand: the constructor ties `end` to `start`
// (after it, at most one year after), which the scaffolded ordered pair could not honour on its own.
// The pair is replaced by a start and a number of days after it, so the rule holds by construction.

using System;

using JustDummies;

namespace Insurance.Domain;

/// <summary>
///     A generator of arbitrary <see cref="CoveragePeriod" /> values: a start in 2000-2040, and an end
///     1 to 365 days after it (so never past <c>start.AddYears(1)</c>).
/// </summary>
public sealed partial class AnyCoveragePeriod : IAny<CoveragePeriod> {

    private readonly IAny<DateOnly> _start;
    private readonly IAny<int>      _daysAfterStart;

    /// <summary>Creates the generator with a default recipe for the start and the length.</summary>
    public AnyCoveragePeriod()
        : this(Any.DateOnly().Between(new DateOnly(2000, 1, 1), new DateOnly(2040, 12, 31)),
               Any.Int32().Between(1, 365)) { }

    private AnyCoveragePeriod(IAny<DateOnly> start, IAny<int> daysAfterStart) {
        _start          = start;
        _daysAfterStart = daysAfterStart;
    }

    /// <summary>Pins the first day of the period.</summary>
    public AnyCoveragePeriod WithStart(DateOnly start) {
        return new AnyCoveragePeriod(Any.Fixed(start), _daysAfterStart);
    }

    /// <summary>Pins <see cref="CoveragePeriod.Days" />, both ends included (2 to 366).</summary>
    public AnyCoveragePeriod WithDays(int days) {
        return new AnyCoveragePeriod(_start, Any.Fixed(days - 1));
    }

    /// <summary>Produces one arbitrary <see cref="CoveragePeriod" />.</summary>
    public CoveragePeriod Generate() {
        DateOnly start = _start.Generate();

        return new CoveragePeriod(start, start.AddDays(_daysAfterStart.Generate()));
    }

}
