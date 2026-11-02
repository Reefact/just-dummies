using Warehouse.Domain;

namespace ColdChain.Domain;

/// <summary>
/// The readings of one pallet's logger, in time order, and what they show against the product's label:
/// excursions, minutes spent outside, hard-limit breaches, and whether the record covers a period without a gap.
/// </summary>
public sealed class TemperatureHistory
{
    private readonly List<LoggerReading> _readings = new();

    public TemperatureHistory(DataLogger logger, TemperatureRange labelledRange)
    {
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(labelledRange);
        Logger = logger;
        LabelledRange = labelledRange;
    }

    public DataLogger Logger { get; }

    public TemperatureRange LabelledRange { get; }

    public IReadOnlyList<LoggerReading> Readings => _readings;

    public LoggerReading? Last => _readings.Count == 0 ? null : _readings[^1];

    public bool HasHardLimitBreach => _readings.Any(r => r.BreachesHardLimit);

    public IReadOnlyList<Excursion> Excursions => Detect();

    public int MinutesOutsideRange => Detect().Sum(e => e.DurationMinutes);

    public int LongestExcursionMinutes => Detect().Select(e => e.DurationMinutes).DefaultIfEmpty(0).Max();

    public void Record(LoggerReading reading)
    {
        ArgumentNullException.ThrowIfNull(reading);
        if (!reading.Logger.Equals(Logger.Id))
        {
            throw new ArgumentException($"Reading from {reading.Logger} does not belong to logger {Logger.Id}.", nameof(reading));
        }

        if (Last is not null && reading.TakenAt <= Last.TakenAt)
        {
            throw new InvalidOperationException($"Reading at {reading.TakenAt:u} does not follow the last one at {Last.TakenAt:u}.");
        }

        _readings.Add(reading);
    }

    /// <summary>
    /// True when the readings span <paramref name="from"/> to <paramref name="to"/> with no silence longer than
    /// <see cref="DataLogger.MaxSilence"/>, the edges included.
    /// </summary>
    public bool Covers(DateTimeOffset from, DateTimeOffset to)
    {
        if (to < from)
        {
            throw new ArgumentException("A period cannot end before it starts.", nameof(to));
        }

        if (_readings.Count == 0)
        {
            return false;
        }

        TimeSpan silence = Logger.MaxSilence;
        if (_readings[0].TakenAt - from > silence || to - _readings[^1].TakenAt > silence)
        {
            return false;
        }

        for (int i = 1; i < _readings.Count; i++)
        {
            if (_readings[i].TakenAt - _readings[i - 1].TakenAt > silence)
            {
                return false;
            }
        }

        return true;
    }

    private ExcursionKind? Classify(decimal celsius)
    {
        if (celsius < LabelledRange.MinCelsius)
        {
            return ExcursionKind.Cold;
        }

        return celsius > LabelledRange.MaxCelsius ? ExcursionKind.Warm : null;
    }

    private List<Excursion> Detect()
    {
        List<Excursion> excursions = new();
        LoggerReading? start = null;
        ExcursionKind kind = ExcursionKind.Cold;
        decimal extreme = 0m;

        foreach (LoggerReading reading in _readings)
        {
            ExcursionKind? current = Classify(reading.Celsius);
            if (start is not null)
            {
                if (current == kind)
                {
                    extreme = kind == ExcursionKind.Cold ? Math.Min(extreme, reading.Celsius) : Math.Max(extreme, reading.Celsius);
                    continue;
                }

                excursions.Add(new Excursion(kind, LabelledRange, start.TakenAt, reading.TakenAt, extreme, isOpen: false));
                start = null;
            }

            if (current is ExcursionKind found)
            {
                start = reading;
                kind = found;
                extreme = reading.Celsius;
            }
        }

        if (start is not null)
        {
            excursions.Add(new Excursion(kind, LabelledRange, start.TakenAt, _readings[^1].TakenAt, extreme, isOpen: true));
        }

        return excursions;
    }
}
