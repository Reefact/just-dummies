using Laboratory.Domain;

namespace Laboratory.Tests;

/// <summary>
/// The anchors the scaffolded generators order their timestamps around. They are arbitrary: only their
/// order and spacing matter (collection, then reception within 72 hours, then reporting).
/// </summary>
public static class LaboratoryCalendar {

    public static readonly DateTimeOffset CollectionStart = new(2025, 3, 1, 0, 0, 0, TimeSpan.Zero);
    public static readonly DateTimeOffset CollectionEnd   = CollectionStart.AddDays(1);
    public static readonly DateTimeOffset ReceptionEnd    = CollectionStart.AddDays(3);
    public static readonly DateTimeOffset ReportingEnd    = CollectionStart.AddDays(30);

}

/// <summary>Two results are the same entry of a report when they measure the same analyte.</summary>
public sealed class LabResultByAnalyte : IEqualityComparer<LabResult> {

    public static readonly LabResultByAnalyte Instance = new();

    public bool Equals(LabResult? x, LabResult? y) => x?.Analyte.Equals(y?.Analyte) ?? y is null;

    public int GetHashCode(LabResult obj) => obj.Analyte.GetHashCode();

}
