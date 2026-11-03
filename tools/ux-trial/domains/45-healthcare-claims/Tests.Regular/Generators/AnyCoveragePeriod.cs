using Insurance.Domain;

using JustDummies;

namespace HealthcareClaims.Tests.Regular.Generators;

/// <summary>
///     A generator of arbitrary <see cref="CoveragePeriod" /> values: a start, and a length of one to 365 days, which
///     cannot leave the period longer than a year or end before it starts.
///     Written by hand after <c>any generate CoveragePeriod</c>: the scaffold paired two bounds, which cannot honour the one-year limit.
/// </summary>
public sealed class AnyCoveragePeriod : IAny<CoveragePeriod> {

    private readonly IAny<DateOnly> _start;
    private readonly IAny<int>      _lengthInDays;

    public AnyCoveragePeriod()
        : this(Any.DateOnly().Between(new DateOnly(2015, 1, 1), new DateOnly(2029, 12, 31)),
               Any.Int32().Between(1, 365)) { }

    private AnyCoveragePeriod(IAny<DateOnly> start, IAny<int> lengthInDays) {
        _start        = start;
        _lengthInDays = lengthInDays;
    }

    public AnyCoveragePeriod StartingOn(DateOnly start) => new AnyCoveragePeriod(Any.Fixed(start), _lengthInDays);

    public AnyCoveragePeriod StartingBetween(DateOnly earliest, DateOnly latest) => new AnyCoveragePeriod(Any.DateOnly().Between(earliest, latest), _lengthInDays);

    public AnyCoveragePeriod LastingDays(int days) => new AnyCoveragePeriod(_start, Any.Fixed(days));

    public CoveragePeriod Generate() {
        DateOnly start = _start.Generate();

        return new CoveragePeriod(start, start.AddDays(_lengthInDays.Generate()));
    }

}
