namespace RailwayInterlocking.Domain;

/// <summary>A position a route requires of one set of points.</summary>
public sealed class PointSetting
{
    public PointSetting(string pointName, PointPosition required)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(pointName);
        if (!Enum.IsDefined(required))
        {
            throw new ArgumentOutOfRangeException(nameof(required), required, "Unknown point position.");
        }

        PointName = pointName;
        Required = required;
    }

    public string PointName { get; }

    public PointPosition Required { get; }

    public override string ToString() => $"{PointName}{(Required == PointPosition.Normal ? "N" : "R")}";
}
