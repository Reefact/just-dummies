namespace RadiocarbonChronology.Domain;

/// <summary>A calibration curve (IntCal-like): calendar years in strictly increasing order, interpolated linearly year by year.</summary>
public sealed class CalibrationCurve
{
    private readonly CalibrationPoint[] _points;

    public CalibrationCurve(string name, IReadOnlyList<CalibrationPoint> points)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("A calibration curve needs a name.", nameof(name));
        }

        ArgumentNullException.ThrowIfNull(points);
        if (points.Count < 2)
        {
            throw new ArgumentException("A calibration curve needs at least two points.", nameof(points));
        }

        for (int i = 0; i < points.Count; i++)
        {
            if (points[i] is null)
            {
                throw new ArgumentException($"Calibration point {i} is null.", nameof(points));
            }

            if (i > 0 && points[i].CalendarYear <= points[i - 1].CalendarYear)
            {
                throw new ArgumentException($"Calibration points must be in strictly increasing calendar order; point {i} ({points[i].CalendarYear}) does not follow {points[i - 1].CalendarYear}.", nameof(points));
            }
        }

        Name = name;
        _points = points.ToArray();
    }

    public string Name { get; }

    public IReadOnlyList<CalibrationPoint> Points => _points;

    public CalendarInterval Coverage => new(_points[0].CalendarYear, _points[^1].CalendarYear);

    /// <summary>Calibrates a measured age into a year-by-year calendar probability distribution over the curve's coverage.</summary>
    public CalibratedDate Calibrate(RadiocarbonAge age)
    {
        ArgumentNullException.ThrowIfNull(age);
        int first = _points[0].CalendarYear;
        int last = _points[^1].CalendarYear;
        double[] likelihood = new double[last - first + 1];
        double total = 0;
        int segment = 0;
        for (int year = first; year <= last; year++)
        {
            while (segment < _points.Length - 2 && year > _points[segment + 1].CalendarYear)
            {
                segment++;
            }

            CalibrationPoint left = _points[segment];
            CalibrationPoint right = _points[segment + 1];
            double t = (double)(year - left.CalendarYear) / (right.CalendarYear - left.CalendarYear);
            double mu = left.RadiocarbonYearsBp + (t * (right.RadiocarbonYearsBp - left.RadiocarbonYearsBp));
            double curveSigma = left.Uncertainty + (t * (right.Uncertainty - left.Uncertainty));
            double variance = ((double)age.Uncertainty * age.Uncertainty) + (curveSigma * curveSigma);
            double deviation = age.YearsBp - mu;
            double value = Math.Exp(-(deviation * deviation) / (2 * variance)) / Math.Sqrt(variance);
            likelihood[year - first] = value;
            total += value;
        }

        if (total <= 0 || double.IsNaN(total))
        {
            throw new ArgumentOutOfRangeException(nameof(age), age, $"{age} falls outside the range the curve '{Name}' can calibrate.");
        }

        for (int i = 0; i < likelihood.Length; i++)
        {
            likelihood[i] /= total;
        }

        return new CalibratedDate(first, likelihood);
    }
}
