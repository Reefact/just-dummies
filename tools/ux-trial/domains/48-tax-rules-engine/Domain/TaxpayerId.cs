namespace TaxRules.Domain;

/// <summary>A taxpayer number: nine digits, not starting with 0, the last a Luhn check digit.</summary>
public sealed class TaxpayerId : IEquatable<TaxpayerId>
{
    public TaxpayerId(string value)
    {
        ArgumentNullException.ThrowIfNull(value);
        if (value.Length != 9 || !value.All(char.IsAsciiDigit))
        {
            throw new FormatException($"A taxpayer number has exactly nine digits; got '{value}'.");
        }

        if (value[0] == '0')
        {
            throw new FormatException("A taxpayer number does not start with 0.");
        }

        if (!PassesLuhn(value))
        {
            throw new ArgumentException($"The check digit of taxpayer number '{value}' is wrong.", nameof(value));
        }

        Value = value;
    }

    public string Value { get; }

    /// <summary>The Luhn check digit for the first eight digits.</summary>
    public static int CheckDigitFor(string firstEight)
    {
        ArgumentNullException.ThrowIfNull(firstEight);
        if (firstEight.Length != 8 || !firstEight.All(char.IsAsciiDigit))
        {
            throw new ArgumentException("Eight digits are expected.", nameof(firstEight));
        }

        int sum = 0;
        for (int i = 0; i < 8; i++)
        {
            int digit = firstEight[7 - i] - '0';
            if (i % 2 == 0)
            {
                digit *= 2;
                if (digit > 9)
                {
                    digit -= 9;
                }
            }

            sum += digit;
        }

        return (10 - (sum % 10)) % 10;
    }

    private static bool PassesLuhn(string value) => CheckDigitFor(value[..8]) == value[8] - '0';

    public bool Equals(TaxpayerId? other) => other is not null && other.Value == Value;

    public override bool Equals(object? obj) => Equals(obj as TaxpayerId);

    public override int GetHashCode() => Value.GetHashCode(StringComparison.Ordinal);

    public override string ToString() => Value;
}
