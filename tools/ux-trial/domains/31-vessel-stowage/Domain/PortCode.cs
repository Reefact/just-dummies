namespace VesselStowage.Domain;

/// <summary>A UN/LOCODE: two-letter country code followed by three letters or digits 2-9 (e.g. NLRTM).</summary>
public sealed class PortCode : IEquatable<PortCode>
{
    public PortCode(string value)
    {
        ArgumentNullException.ThrowIfNull(value);
        if (value.Length != 5)
        {
            throw new ArgumentException("A UN/LOCODE has exactly 5 characters.", nameof(value));
        }

        if (!IsUpperLetter(value[0]) || !IsUpperLetter(value[1]))
        {
            throw new ArgumentException("A UN/LOCODE starts with a two-letter country code.", nameof(value));
        }

        for (int i = 2; i < 5; i++)
        {
            char c = value[i];
            if (!IsUpperLetter(c) && (c < '2' || c > '9'))
            {
                throw new ArgumentException("The location part uses letters A-Z or digits 2-9.", nameof(value));
            }
        }

        Value = value;
    }

    public string Value { get; }

    private static bool IsUpperLetter(char c) => c >= 'A' && c <= 'Z';

    public bool Equals(PortCode? other) => other is not null && other.Value == Value;

    public override bool Equals(object? obj) => Equals(obj as PortCode);

    public override int GetHashCode() => Value.GetHashCode(StringComparison.Ordinal);

    public override string ToString() => Value;
}
