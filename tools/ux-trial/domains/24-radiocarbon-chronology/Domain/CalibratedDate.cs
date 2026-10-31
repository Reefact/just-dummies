namespace RadiocarbonChronology.Domain;

/// <summary>
/// A calendar date as a discrete probability distribution, one probability per calendar year from <see cref="FirstYear"/>.
/// It can be multimodal: its highest density region may be several disjoint intervals.
/// </summary>
public sealed class CalibratedDate
{
    public const double SumTolerance = 1e-6;

    private readonly double[] _probabilities;

    public CalibratedDate(int firstYear, IReadOnlyList<double> probabilities)
    {
        ArgumentNullException.ThrowIfNull(probabilities);
        if (probabilities.Count == 0)
        {
            throw new ArgumentException("A calibrated date needs at least one year of probability.", nameof(probabilities));
        }

        if (firstYear < CalendarInterval.EarliestYear || (long)firstYear + probabilities.Count - 1 > CalendarInterval.LatestYear)
        {
            throw new ArgumentOutOfRangeException(nameof(firstYear), firstYear, $"The distribution must lie between {CalendarInterval.EarliestYear} and {CalendarInterval.LatestYear}.");
        }

        double sum = 0;
        for (int i = 0; i < probabilities.Count; i++)
        {
            double p = probabilities[i];
            if (double.IsNaN(p) || p < 0 || p > 1)
            {
                throw new ArgumentOutOfRangeException(nameof(probabilities), p, $"The probability of year {firstYear + i} must lie between 0 and 1.");
            }

            sum += p;
        }

        if (Math.Abs(sum - 1) > SumTolerance)
        {
            throw new ArgumentException($"The probabilities must sum to 1, not {sum}.", nameof(probabilities));
        }

        FirstYear = firstYear;
        _probabilities = probabilities.ToArray();
    }

    public int FirstYear { get; }

    public int LastYear => FirstYear + _probabilities.Length - 1;

    public IReadOnlyList<double> Probabilities => _probabilities;

    public double ProbabilityOf(int year) => year < FirstYear || year > LastYear ? 0 : _probabilities[year - FirstYear];

    public double Mean
    {
        get
        {
            double mean = 0;
            for (int i = 0; i < _probabilities.Length; i++)
            {
                mean += (FirstYear + i) * _probabilities[i];
            }

            return mean;
        }
    }

    /// <summary>The smallest set of years holding at least <paramref name="level"/> of the probability, as disjoint intervals in calendar order.</summary>
    public IReadOnlyList<CalendarInterval> HighestDensityRegion(double level)
    {
        if (double.IsNaN(level) || level <= 0 || level >= 1)
        {
            throw new ArgumentOutOfRangeException(nameof(level), level, "A credible level must lie strictly between 0 and 1.");
        }

        int[] order = Enumerable.Range(0, _probabilities.Length).OrderByDescending(i => _probabilities[i]).ToArray();
        bool[] selected = new bool[_probabilities.Length];
        double accumulated = 0;
        foreach (int index in order)
        {
            if (accumulated >= level || _probabilities[index] <= 0)
            {
                break;
            }

            selected[index] = true;
            accumulated += _probabilities[index];
        }

        List<CalendarInterval> intervals = new();
        int start = -1;
        for (int i = 0; i <= selected.Length; i++)
        {
            bool inside = i < selected.Length && selected[i];
            if (inside && start < 0)
            {
                start = i;
            }
            else if (!inside && start >= 0)
            {
                intervals.Add(new CalendarInterval(FirstYear + start, FirstYear + i - 1));
                start = -1;
            }
        }

        return intervals;
    }

    /// <summary>Keeps only the years within the given bounds and renormalises; refuses when no probability remains.</summary>
    public CalibratedDate Truncate(int notBefore, int notAfter)
    {
        if (notBefore > notAfter)
        {
            throw new ArgumentException($"The bound {notBefore} comes after {notAfter}.", nameof(notAfter));
        }

        double[] kept = new double[_probabilities.Length];
        double total = 0;
        for (int i = 0; i < kept.Length; i++)
        {
            int year = FirstYear + i;
            if (year >= notBefore && year <= notAfter)
            {
                kept[i] = _probabilities[i];
                total += kept[i];
            }
        }

        if (total <= 0)
        {
            throw new InvalidOperationException($"No probability remains between {notBefore} and {notAfter}.");
        }

        for (int i = 0; i < kept.Length; i++)
        {
            kept[i] /= total;
        }

        return new CalibratedDate(FirstYear, kept);
    }

    /// <summary>An equally weighted mixture of dates: the span of a phase dated by several samples.</summary>
    public static CalibratedDate Combine(IReadOnlyList<CalibratedDate> dates)
    {
        ArgumentNullException.ThrowIfNull(dates);
        if (dates.Count == 0)
        {
            throw new ArgumentException("At least one date is needed to combine.", nameof(dates));
        }

        int first = dates.Min(d => d.FirstYear);
        int last = dates.Max(d => d.LastYear);
        double[] mixture = new double[last - first + 1];
        foreach (CalibratedDate date in dates)
        {
            for (int i = 0; i < date._probabilities.Length; i++)
            {
                mixture[date.FirstYear - first + i] += date._probabilities[i] / dates.Count;
            }
        }

        return new CalibratedDate(first, mixture);
    }
}
