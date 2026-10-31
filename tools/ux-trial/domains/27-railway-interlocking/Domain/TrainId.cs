using System.Text.RegularExpressions;

namespace RailwayInterlocking.Domain;

/// <summary>A train reporting number (headcode), e.g. "1A23": digit, capital letter, two digits.</summary>
public sealed class TrainId : IEquatable<TrainId>
{
    private static readonly Regex Format = new("^[0-9][A-Z][0-9]{2}$", RegexOptions.CultureInvariant);

    public TrainId(string headcode)
    {
        ArgumentNullException.ThrowIfNull(headcode);
        if (!Format.IsMatch(headcode))
        {
            throw new ArgumentException($"Headcode '{headcode}' must be a digit, a capital letter and two digits.", nameof(headcode));
        }

        Headcode = headcode;
    }

    public string Headcode { get; }

    public bool Equals(TrainId? other) => other is not null && Headcode == other.Headcode;

    public override bool Equals(object? obj) => Equals(obj as TrainId);

    public override int GetHashCode() => Headcode.GetHashCode(StringComparison.Ordinal);

    public override string ToString() => Headcode;
}
