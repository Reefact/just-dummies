using System.Text.RegularExpressions;

namespace Freight.Domain;

/// <summary>An ISO 6346 container identification: owner code, category, serial number and check digit.</summary>
public sealed class ContainerCode : IEquatable<ContainerCode> {

    private static readonly Regex Format = new("^[A-Z]{3}[UJZ][0-9]{7}$", RegexOptions.CultureInvariant);

    public ContainerCode(string value) {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        if (!Format.IsMatch(value)) {
            throw new FormatException($"'{value}' is not an ISO 6346 container code (AAAU0000000).");
        }
        Validate(value);

        Value = value;
    }

    public string Value { get; }

    public string OwnerCode => Value.Substring(0, 3);

    public char Category => Value[3];

    public static ContainerCode Create(string ownerCode, char category, int serialNumber) {
        string withoutCheckDigit = $"{ownerCode}{category}{serialNumber:D6}";

        return new ContainerCode(withoutCheckDigit + CheckDigitOf(withoutCheckDigit));
    }

    public static int CheckDigitOf(string firstTenCharacters) {
        int sum = 0;
        for (int position = 0; position < 10; position++) {
            sum += CharacterValue(firstTenCharacters[position]) << position;
        }

        return sum % 11 % 10;
    }

    private static void Validate(string value) {
        int expected = CheckDigitOf(value);
        int actual   = value[10] - '0';
        if (expected != actual) {
            throw new ArgumentException($"Check digit of '{value}' should be {expected}, not {actual}.", nameof(value));
        }
    }

    private static int CharacterValue(char character) {
        if (char.IsAsciiDigit(character)) {
            return character - '0';
        }
        // A=10, skipping every multiple of eleven.
        int value = 10;
        for (char letter = 'A'; letter < character; letter++) {
            value++;
            if (value % 11 == 0) {
                value++;
            }
        }

        return value;
    }

    public bool Equals(ContainerCode? other) => other is not null && Value == other.Value;

    public override bool Equals(object? obj) => Equals(obj as ContainerCode);

    public override int GetHashCode() => Value.GetHashCode(StringComparison.Ordinal);

    public override string ToString() => Value;

}
