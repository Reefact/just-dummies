namespace VesselStowage.Domain;

/// <summary>An ISO 6346 container number: owner code (3 letters), category (U, J or Z), 6-digit serial, check digit.</summary>
public sealed class ContainerId : IEquatable<ContainerId>
{
    public ContainerId(string value)
    {
        ArgumentNullException.ThrowIfNull(value);
        if (value.Length != 11)
        {
            throw new ArgumentException("A container number has exactly 11 characters.", nameof(value));
        }

        for (int i = 0; i < 3; i++)
        {
            if (value[i] < 'A' || value[i] > 'Z')
            {
                throw new ArgumentException("The owner code must be three upper-case letters.", nameof(value));
            }
        }

        if (value[3] != 'U' && value[3] != 'J' && value[3] != 'Z')
        {
            throw new ArgumentException("The equipment category must be U, J or Z.", nameof(value));
        }

        for (int i = 4; i < 11; i++)
        {
            if (!char.IsAsciiDigit(value[i]))
            {
                throw new ArgumentException("The serial number and check digit must be digits.", nameof(value));
            }
        }

        if (ComputeCheckDigit(value) != value[10] - '0')
        {
            throw new ArgumentException($"Check digit of '{value}' is wrong.", nameof(value));
        }

        Value = value;
    }

    public string Value { get; }

    public static int ComputeCheckDigit(string firstTen)
    {
        ArgumentNullException.ThrowIfNull(firstTen);
        int sum = 0;
        for (int i = 0; i < 10; i++)
        {
            char c = firstTen[i];
            int code = char.IsAsciiDigit(c) ? c - '0' : LetterValue(c);
            sum += code << i;
        }

        return sum % 11 % 10;
    }

    private static int LetterValue(char letter)
    {
        // A = 10, skipping multiples of 11 (11, 22, 33).
        int value = 10;
        for (char c = 'A'; c < letter; c++)
        {
            value++;
            if (value % 11 == 0)
            {
                value++;
            }
        }

        return value;
    }

    public bool Equals(ContainerId? other) => other is not null && other.Value == Value;

    public override bool Equals(object? obj) => Equals(obj as ContainerId);

    public override int GetHashCode() => Value.GetHashCode(StringComparison.Ordinal);

    public override string ToString() => Value;
}
