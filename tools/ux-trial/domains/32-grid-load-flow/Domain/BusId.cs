using System.Text.RegularExpressions;

namespace GridLoadFlow.Domain;

/// <summary>Identifier of a bus: an upper-case letter followed by up to 15 upper-case letters, digits, '-' or '_'.</summary>
public sealed class BusId : IEquatable<BusId>
{
    private static readonly Regex Format = new("^[A-Z][A-Z0-9_-]{0,15}$", RegexOptions.CultureInvariant);

    public BusId(string value)
    {
        ArgumentNullException.ThrowIfNull(value);
        if (!Format.IsMatch(value))
        {
            throw new FormatException($"Bus id '{value}' must match {Format}.");
        }

        Value = value;
    }

    public string Value { get; }

    public bool Equals(BusId? other) => other is not null && Value == other.Value;

    public override bool Equals(object? obj) => obj is BusId other && Equals(other);

    public override int GetHashCode() => Value.GetHashCode(StringComparison.Ordinal);

    public override string ToString() => Value;
}
