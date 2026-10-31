using System;
using System.Collections.Generic;

namespace RailwayInterlocking.Domain;

/// <summary>Two settings of the same point are the same setting as far as a route is concerned.</summary>
internal sealed class PointSettingByPointName : IEqualityComparer<PointSetting> {

    public static readonly PointSettingByPointName Instance = new();

    public bool Equals(PointSetting? x, PointSetting? y) {
        return string.Equals(x?.PointName, y?.PointName, StringComparison.Ordinal);
    }

    public int GetHashCode(PointSetting obj) {
        return StringComparer.Ordinal.GetHashCode(obj.PointName);
    }

}
