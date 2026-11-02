using System.Text.RegularExpressions;

namespace ShiftPlanning.Domain;

/// <summary>
/// A site's time zone, named by its IANA id (e.g. <c>Europe/Paris</c>) and resolved from the system's
/// time-zone database. Converts local wall-clock times to instants and back, and says whether a local
/// time is skipped (spring-forward gap) or ambiguous (fall-back overlap).
/// </summary>
public sealed partial class SiteZone : IEquatable<SiteZone>
{
    public SiteZone(string ianaId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(ianaId);
        if (!IanaShape().IsMatch(ianaId))
        {
            throw new FormatException($"'{ianaId}' is not an IANA time-zone id (Area/Location).");
        }

        if (!TimeZoneInfo.TryFindSystemTimeZoneById(ianaId, out TimeZoneInfo? info))
        {
            throw new ArgumentException($"Unknown time zone '{ianaId}'.", nameof(ianaId));
        }

        Id = ianaId;
        Info = info;
    }

    public string Id { get; }

    public TimeZoneInfo Info { get; }

    public LocalTimeKind Classify(DateOnly date, TimeOnly time)
    {
        DateTime local = date.ToDateTime(time, DateTimeKind.Unspecified);
        if (Info.IsInvalidTime(local))
        {
            return LocalTimeKind.Skipped;
        }

        return Info.IsAmbiguousTime(local) ? LocalTimeKind.Ambiguous : LocalTimeKind.Unique;
    }

    /// <summary>
    /// The instant at which the wall clock of this zone shows <paramref name="time"/> on <paramref name="date"/>.
    /// A skipped time is refused; an ambiguous one needs <see cref="OffsetChoice.Earlier"/> or <see cref="OffsetChoice.Later"/>.
    /// </summary>
    public DateTimeOffset ToUtc(DateOnly date, TimeOnly time, OffsetChoice choice)
    {
        if (!Enum.IsDefined(choice))
        {
            throw new ArgumentOutOfRangeException(nameof(choice), choice, "Unknown offset choice.");
        }

        DateTime local = date.ToDateTime(time, DateTimeKind.Unspecified);
        switch (Classify(date, time))
        {
            case LocalTimeKind.Skipped:
                throw new ArgumentException($"{local:yyyy-MM-dd HH:mm} does not exist in {Id} (daylight-saving gap).", nameof(time));
            case LocalTimeKind.Ambiguous:
                if (choice == OffsetChoice.Unspecified)
                {
                    throw new ArgumentException($"{local:yyyy-MM-dd HH:mm} occurs twice in {Id}; choose the earlier or later offset.", nameof(choice));
                }

                TimeSpan[] offsets = Info.GetAmbiguousTimeOffsets(local);
                // Clocks go back: the offset in force before the transition is the larger one.
                TimeSpan offset = choice == OffsetChoice.Earlier ? offsets.Max() : offsets.Min();
                return new DateTimeOffset(local, offset).ToUniversalTime();
            default:
                return new DateTimeOffset(TimeZoneInfo.ConvertTimeToUtc(local, Info), TimeSpan.Zero);
        }
    }

    /// <summary>The instant as this zone's wall clock shows it, with the offset in force then.</summary>
    public DateTimeOffset ToLocal(DateTimeOffset instant) => TimeZoneInfo.ConvertTime(instant, Info);

    public TimeSpan OffsetAt(DateTimeOffset instant) => Info.GetUtcOffset(instant);

    [GeneratedRegex(@"^(UTC|[A-Z][A-Za-z_]+(/[A-Za-z0-9_+\-]+){1,2})$")]
    private static partial Regex IanaShape();

    public bool Equals(SiteZone? other) => other is not null && other.Id == Id;

    public override bool Equals(object? obj) => Equals(obj as SiteZone);

    public override int GetHashCode() => Id.GetHashCode(StringComparison.Ordinal);

    public override string ToString() => Id;
}
