using System.Text.RegularExpressions;

namespace BatchGenealogy.Domain;

/// <summary>An item master code such as <c>MAT-01234</c>: what a lot is made of.</summary>
public sealed partial class MaterialCode : IEquatable<MaterialCode>
{
    public MaterialCode(string value)
    {
        ArgumentNullException.ThrowIfNull(value);
        if (!Pattern().IsMatch(value))
        {
            throw new FormatException($"'{value}' is not a material code: expected MAT- followed by 5 digits.");
        }

        Value = value;
    }

    public string Value { get; }

    public bool Equals(MaterialCode? other) => other is not null && other.Value == Value;

    public override bool Equals(object? obj) => Equals(obj as MaterialCode);

    public override int GetHashCode() => StringComparer.Ordinal.GetHashCode(Value);

    public override string ToString() => Value;

    [GeneratedRegex("^MAT-[0-9]{5}$", RegexOptions.CultureInvariant)]
    private static partial Regex Pattern();
}
