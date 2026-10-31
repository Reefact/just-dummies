using System.Diagnostics.CodeAnalysis;

namespace RailwayInterlocking.Domain;

/// <summary>A route may not set the same point twice: two settings are "the same" when they name the same point.</summary>
public sealed class PointNameComparer : IEqualityComparer<PointSetting> {

    public static readonly PointNameComparer Instance = new();

    public bool Equals(PointSetting? x, PointSetting? y) => string.Equals(x?.PointName, y?.PointName, StringComparison.Ordinal);

    public int GetHashCode([DisallowNull] PointSetting obj) => StringComparer.Ordinal.GetHashCode(obj.PointName);

}
