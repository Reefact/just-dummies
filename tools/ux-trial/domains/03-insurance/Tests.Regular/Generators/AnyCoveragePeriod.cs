// Scaffolded by `any`, then rewritten by hand: the constructor ties `end` to `start` by a one-year ceiling,
// which no per-parameter recipe can honour, so the period is drawn as a start and a length in days.

using System;

using JustDummies;

namespace Insurance.Domain;

/// <summary>A generator of arbitrary <see cref="CoveragePeriod" /> values: a start and a length of 1 to 365 days.</summary>
public sealed partial class AnyCoveragePeriod : IAny<CoveragePeriod> {

    private readonly IAny<DateOnly> _start;
    private readonly IAny<int>      _daysAfterStart;

    public AnyCoveragePeriod()
        : this(AnyValidStart(), AnyValidDaysAfterStart()) { }

    private AnyCoveragePeriod(IAny<DateOnly> start, IAny<int> daysAfterStart) {
        _start          = start;
        _daysAfterStart = daysAfterStart;
    }

    private static AnyDateOnly AnyValidStart() {
        return Any.DateOnly().Between(new DateOnly(2000, 1, 1), new DateOnly(2100, 12, 31));
    }

    // 365 days after the start is always within the year, leap year or not, and so is the same length from Next().
    private static AnyInt32 AnyValidDaysAfterStart() {
        return Any.Int32().Between(1, 365);
    }

    public AnyCoveragePeriod WithStart(DateOnly value) {
        return new AnyCoveragePeriod(Any.Fixed(value), _daysAfterStart);
    }

    public AnyCoveragePeriod WithDaysAfterStart(int value) {
        return new AnyCoveragePeriod(_start, Any.Fixed(value));
    }

    public CoveragePeriod Generate() {
        DateOnly start = _start.Generate();

        return new CoveragePeriod(start, start.AddDays(_daysAfterStart.Generate()));
    }

}
