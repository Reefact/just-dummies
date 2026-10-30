namespace Telecom.Domain;

/// <summary>A 15-digit handset identity whose last digit is a Luhn check digit.</summary>
public sealed class Imei {

    public Imei(string value) {
        Validate(value);
        Value = value;
    }

    public string Value { get; }

    /// <summary>The Type Allocation Code: the first eight digits, which identify the handset model.</summary>
    public string TypeAllocationCode => Value.Substring(0, 8);

    public static int LuhnCheckDigit(string body) {
        int sum = 0;
        for (int index = 0; index < body.Length; index++) {
            int digit = body[body.Length - 1 - index] - '0';
            if (index % 2 == 0) {
                digit *= 2;
                if (digit > 9) {
                    digit -= 9;
                }
            }
            sum += digit;
        }
        return (10 - sum % 10) % 10;
    }

    private static void Validate(string value) {
        ArgumentNullException.ThrowIfNull(value);
        if (value.Length != 15 || !value.All(char.IsAsciiDigit)) {
            throw new ArgumentException("An IMEI has exactly 15 digits.", nameof(value));
        }
        if (LuhnCheckDigit(value.Substring(0, 14)) != value[14] - '0') {
            throw new ArgumentException($"'{value}' fails the Luhn check.", nameof(value));
        }
    }

    public override bool Equals(object? obj) => obj is Imei other && other.Value == Value;

    public override int GetHashCode() => Value.GetHashCode(StringComparison.Ordinal);

}
