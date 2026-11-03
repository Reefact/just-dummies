// Scaffolded by `any` (JustDummies), then rewritten by hand: the constructor ties `end` to `start`
// by a one-year ceiling that `any` cannot express, so the generator draws a start and a length in days.

using System;

using JustDummies;

namespace Insurance.Domain;

/// <summary>
///     A generator of arbitrary <see cref="CoveragePeriod" /> values: a start, and a length of 2 to 366 days (1 to 365 days after the start)
///     (so that <c>end</c> is after <c>start</c> and never past one year).
/// </summary>
public sealed partial class AnyCoveragePeriod : IAny<CoveragePeriod> {

    private IAny<DateOnly> _start;
    private IAny<int>      _daysAfterStart;

    private AnyDateOnly _startRecipe;
    private AnyInt32    _daysAfterStartRecipe;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyCoveragePeriod()
        : this(start:          AnyValidStart(),
               daysAfterStart: AnyValidDaysAfterStart()) { }

    private static AnyDateOnly AnyValidStart() {
        // Far from DateOnly's limits, so that AddYears and Next() never leave the type.
        return Any.DateOnly().Between(new DateOnly(2000, 1, 1), new DateOnly(2100, 12, 31));
    }

    private static AnyInt32 AnyValidDaysAfterStart() {
        // 365 days after the start is never past start.AddYears(1), leap year or not.
        return Any.Int32().Between(1, 365);
    }

    private AnyCoveragePeriod(AnyDateOnly start, AnyInt32 daysAfterStart) {
        _start                = start;
        _startRecipe          = start;
        _daysAfterStart       = daysAfterStart;
        _daysAfterStartRecipe = daysAfterStart;
    }

    private AnyCoveragePeriod(AnyCoveragePeriod from) {
        _start                = from._start;
        _startRecipe          = from._startRecipe;
        _daysAfterStart       = from._daysAfterStart;
        _daysAfterStartRecipe = from._daysAfterStartRecipe;
    }

    public AnyCoveragePeriod WithStart(DateOnly value) {
        AnyCoveragePeriod copy = new AnyCoveragePeriod(this);

        copy._start = Any.Fixed(value);

        return copy;
    }

    public AnyCoveragePeriod WithStart(Func<AnyDateOnly, AnyDateOnly> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyCoveragePeriod copy = new AnyCoveragePeriod(this);

        copy._startRecipe = configure(_startRecipe);
        copy._start       = copy._startRecipe;

        return copy;
    }

    /// <summary>Pins the number of days between the start and the end (a period of N+1 days, both inclusive).</summary>
    public AnyCoveragePeriod WithDaysAfterStart(int value) {
        AnyCoveragePeriod copy = new AnyCoveragePeriod(this);

        copy._daysAfterStart = Any.Fixed(value);

        return copy;
    }

    public AnyCoveragePeriod WithDaysAfterStart(Func<AnyInt32, AnyInt32> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyCoveragePeriod copy = new AnyCoveragePeriod(this);

        copy._daysAfterStartRecipe = configure(_daysAfterStartRecipe);
        copy._daysAfterStart       = copy._daysAfterStartRecipe;

        return copy;
    }

    public CoveragePeriod Generate() {
        DateOnly start = _start.Generate();

        return new CoveragePeriod(start, start.AddDays(_daysAfterStart.Generate()));
    }

}
