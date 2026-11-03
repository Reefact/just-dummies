using System;

using JustDummies;

namespace Insurance.Domain;

/// <summary>An arbitrary <see cref="CoveragePeriod" />: a start, and a length of 1 to 365 days — so it ends after it starts and never runs past a year.</summary>
public sealed partial class AnyCoveragePeriod : IAny<CoveragePeriod> {

    private IAny<DateOnly> _start;
    private IAny<int>      _days;

    public AnyCoveragePeriod() {
        _start = Any.DateOnly().Between(new DateOnly(2026, 1, 1), new DateOnly(2035, 12, 31));
        _days  = Any.Int32().Between(1, 365);
    }

    private AnyCoveragePeriod(AnyCoveragePeriod from) {
        _start = from._start;
        _days  = from._days;
    }

    public AnyCoveragePeriod WithStart(DateOnly start) {
        AnyCoveragePeriod copy = new AnyCoveragePeriod(this);

        copy._start = Any.Fixed(start);

        return copy;
    }

    public AnyCoveragePeriod WithDays(int days) {
        AnyCoveragePeriod copy = new AnyCoveragePeriod(this);

        copy._days = Any.Fixed(days);

        return copy;
    }

    public CoveragePeriod Generate() {
        DateOnly start = _start.Generate();

        return new CoveragePeriod(start, start.AddDays(_days.Generate()));
    }

}
