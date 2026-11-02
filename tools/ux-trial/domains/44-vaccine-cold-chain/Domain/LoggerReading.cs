using AgriTech.Domain;

namespace ColdChain.Domain;

/// <summary>One temperature sample from a data logger, time-stamped in UTC, at the logger's 0.1 °C resolution.</summary>
public sealed class LoggerReading : IEquatable<LoggerReading>
{
    public const decimal MinCelsius = -40m;
    public const decimal MaxCelsius = 85m;

    public LoggerReading(SensorId logger, DateTimeOffset takenAt, decimal celsius, BatteryVoltage battery)
    {
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(battery);
        if (takenAt.Offset != TimeSpan.Zero)
        {
            throw new ArgumentException("Logger readings are time-stamped in UTC.", nameof(takenAt));
        }

        if (celsius < MinCelsius || celsius > MaxCelsius)
        {
            throw new ArgumentOutOfRangeException(nameof(celsius), celsius, $"A logger measures between {MinCelsius} and {MaxCelsius} °C.");
        }

        if (decimal.Round(celsius, 1) != celsius)
        {
            throw new ArgumentException("A logger reads to 0.1 °C.", nameof(celsius));
        }

        Logger = logger;
        TakenAt = takenAt;
        Celsius = celsius;
        Battery = battery;
    }

    public SensorId Logger { get; }

    public DateTimeOffset TakenAt { get; }

    public decimal Celsius { get; }

    public BatteryVoltage Battery { get; }

    public bool BreachesHardLimit => VaccineProduct.BreachesHardLimit(Celsius);

    public bool Equals(LoggerReading? other) =>
        other is not null
        && other.Logger.Equals(Logger)
        && other.TakenAt == TakenAt
        && other.Celsius == Celsius
        && other.Battery.Volts == Battery.Volts;

    public override bool Equals(object? obj) => Equals(obj as LoggerReading);

    public override int GetHashCode() => HashCode.Combine(Logger, TakenAt, Celsius, Battery.Volts);

    public override string ToString() => $"{Logger} {TakenAt:u} {Celsius} °C";
}
