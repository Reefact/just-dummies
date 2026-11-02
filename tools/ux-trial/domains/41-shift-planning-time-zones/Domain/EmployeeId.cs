namespace ShiftPlanning.Domain;

/// <summary>A personnel number: <c>E</c> followed by six digits, not all zero (e.g. <c>E004217</c>).</summary>
public sealed class EmployeeId : IEquatable<EmployeeId>
{
    public EmployeeId(string value)
    {
        ArgumentNullException.ThrowIfNull(value);
        if (value.Length != 7 || value[0] != 'E')
        {
            throw new FormatException($"'{value}' is not a personnel number (E + 6 digits).");
        }

        for (int i = 1; i < value.Length; i++)
        {
            if (!char.IsAsciiDigit(value[i]))
            {
                throw new FormatException($"'{value}' is not a personnel number (E + 6 digits).");
            }
        }

        if (value == "E000000")
        {
            throw new ArgumentException("E000000 is not a personnel number.", nameof(value));
        }

        Value = value;
    }

    public string Value { get; }

    public bool Equals(EmployeeId? other) => other is not null && other.Value == Value;

    public override bool Equals(object? obj) => Equals(obj as EmployeeId);

    public override int GetHashCode() => Value.GetHashCode(StringComparison.Ordinal);

    public override string ToString() => Value;
}
