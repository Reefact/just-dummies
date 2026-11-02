using Warehouse.Domain;

namespace ColdChain.Domain;

/// <summary>
/// A continuous stretch spent outside the labelled range, from the first reading outside it to the next reading
/// back inside (or to the last reading, when the record ends still outside: an open excursion).
/// </summary>
public sealed class Excursion : IEquatable<Excursion>
{
    public Excursion(ExcursionKind kind, TemperatureRange labelledRange, DateTimeOffset startedAt, DateTimeOffset endedAt, decimal extremeCelsius, bool isOpen)
    {
        if (!Enum.IsDefined(kind))
        {
            throw new ArgumentOutOfRangeException(nameof(kind), kind, "Unknown excursion kind.");
        }

        ArgumentNullException.ThrowIfNull(labelledRange);
        if (endedAt < startedAt)
        {
            throw new ArgumentException("An excursion cannot end before it starts.", nameof(endedAt));
        }

        if (kind == ExcursionKind.Cold && extremeCelsius >= labelledRange.MinCelsius)
        {
            throw new ArgumentException($"A cold excursion goes below {labelledRange.MinCelsius} °C.", nameof(extremeCelsius));
        }

        if (kind == ExcursionKind.Warm && extremeCelsius <= labelledRange.MaxCelsius)
        {
            throw new ArgumentException($"A warm excursion goes above {labelledRange.MaxCelsius} °C.", nameof(extremeCelsius));
        }

        Kind = kind;
        LabelledRange = labelledRange;
        StartedAt = startedAt;
        EndedAt = endedAt;
        ExtremeCelsius = extremeCelsius;
        IsOpen = isOpen;
    }

    public ExcursionKind Kind { get; }

    public TemperatureRange LabelledRange { get; }

    public DateTimeOffset StartedAt { get; }

    public DateTimeOffset EndedAt { get; }

    /// <summary>The coldest reading of a cold excursion, the warmest of a warm one.</summary>
    public decimal ExtremeCelsius { get; }

    /// <summary>The record ended before the temperature came back within the label.</summary>
    public bool IsOpen { get; }

    public TimeSpan Duration => EndedAt - StartedAt;

    /// <summary>Whole minutes, rounded up: a started minute is charged to the stability budget.</summary>
    public int DurationMinutes => (int)Math.Ceiling(Duration.TotalMinutes);

    public bool BreachesHardLimit => VaccineProduct.BreachesHardLimit(ExtremeCelsius);

    public bool Equals(Excursion? other) =>
        other is not null
        && other.Kind == Kind
        && other.LabelledRange.MinCelsius == LabelledRange.MinCelsius
        && other.LabelledRange.MaxCelsius == LabelledRange.MaxCelsius
        && other.StartedAt == StartedAt
        && other.EndedAt == EndedAt
        && other.ExtremeCelsius == ExtremeCelsius
        && other.IsOpen == IsOpen;

    public override bool Equals(object? obj) => Equals(obj as Excursion);

    public override int GetHashCode() => HashCode.Combine(Kind, StartedAt, EndedAt, ExtremeCelsius, IsOpen);

    public override string ToString() => $"{Kind} excursion {StartedAt:u} -> {EndedAt:u}, extreme {ExtremeCelsius} °C{(IsOpen ? " (open)" : string.Empty)}";
}
