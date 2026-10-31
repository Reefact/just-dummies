using System.Text.RegularExpressions;

namespace DnaVariants.Domain;

/// <summary>A versioned reference sequence accession such as <c>NC_000001.11</c>.</summary>
public sealed partial class Accession : IEquatable<Accession>
{
    public Accession(string value)
    {
        ArgumentNullException.ThrowIfNull(value);
        if (!Format().IsMatch(value))
        {
            throw new ArgumentException(
                $"'{value}' is not a versioned accession (two upper-case letters, '_', 6 to 9 digits, '.', a version of 1 to 3 digits).",
                nameof(value));
        }

        Value = value;
    }

    public string Value { get; }

    public string Prefix => Value[..2];

    public int Version => int.Parse(Value[(Value.IndexOf('.') + 1)..], System.Globalization.CultureInfo.InvariantCulture);

    public bool Equals(Accession? other) => other is not null && Value == other.Value;

    public override bool Equals(object? obj) => Equals(obj as Accession);

    public override int GetHashCode() => Value.GetHashCode(StringComparison.Ordinal);

    public override string ToString() => Value;

    [GeneratedRegex(@"^[A-Z]{2}_[0-9]{6,9}\.[0-9]{1,3}$")]
    private static partial Regex Format();
}
