using System.Globalization;
using System.Text.RegularExpressions;

namespace PassiveComponents.Domain;

/// <summary>
/// A position on the schematic, IEEE 315 style: <c>C</c>, <c>L</c> or <c>R</c> then 1 to 9999 with no leading
/// zero (<c>C1</c>, <c>R12</c>, <c>L3</c>). The letter names the kind of part the position takes.
/// Ordered by letter, then numerically (<c>R2</c> before <c>R10</c>).
/// </summary>
public sealed partial class ReferenceDesignator : IEquatable<ReferenceDesignator>, IComparable<ReferenceDesignator>
{
    public ReferenceDesignator(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("A reference designator is not blank.", nameof(value));
        }

        Match match = Format().Match(value);
        if (!match.Success)
        {
            throw new FormatException($"A reference designator reads C, L or R then 1-9999 (e.g. R12); got '{value}'.");
        }

        Kind = match.Groups["prefix"].Value switch
        {
            "C" => ComponentKind.Capacitor,
            "L" => ComponentKind.Inductor,
            _ => ComponentKind.Resistor,
        };
        Number = int.Parse(match.Groups["number"].Value, CultureInfo.InvariantCulture);
        Value = value;
    }

    public string Value { get; }

    public ComponentKind Kind { get; }

    public int Number { get; }

    public int CompareTo(ReferenceDesignator? other)
    {
        if (other is null)
        {
            return 1;
        }

        int byKind = string.CompareOrdinal(Value[..1], other.Value[..1]);
        return byKind != 0 ? byKind : Number.CompareTo(other.Number);
    }

    public bool Equals(ReferenceDesignator? other) => other is not null && string.Equals(other.Value, Value, StringComparison.Ordinal);

    public override bool Equals(object? obj) => Equals(obj as ReferenceDesignator);

    public override int GetHashCode() => StringComparer.Ordinal.GetHashCode(Value);

    public override string ToString() => Value;

    [GeneratedRegex(@"^(?<prefix>[CLR])(?<number>[1-9]\d{0,3})$")]
    private static partial Regex Format();
}
