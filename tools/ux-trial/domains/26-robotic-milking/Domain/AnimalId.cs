using System.Text.RegularExpressions;

namespace RoboticMilking.Domain;

/// <summary>
/// An EU ear-tag identifier: a two-letter ISO 3166 country code followed by twelve digits, e.g. <c>FR123456789012</c>.
/// </summary>
public sealed partial class AnimalId : IEquatable<AnimalId>
{
    public AnimalId(string value)
    {
        ArgumentNullException.ThrowIfNull(value);
        if (!Format().IsMatch(value))
        {
            throw new ArgumentException(
                $"An animal id is two upper-case letters followed by twelve digits; got '{value}'.", nameof(value));
        }

        Value = value;
    }

    public string Value { get; }

    public string CountryCode => Value[..2];

    public bool Equals(AnimalId? other) => other is not null && Value == other.Value;

    public override bool Equals(object? obj) => Equals(obj as AnimalId);

    public override int GetHashCode() => Value.GetHashCode(StringComparison.Ordinal);

    public override string ToString() => Value;

    [GeneratedRegex("^[A-Z]{2}[0-9]{12}$")]
    private static partial Regex Format();
}
