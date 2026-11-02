using System.Text.RegularExpressions;

namespace TokenLedger.Domain;

/// <summary>
/// An account address: <c>0x</c> followed by 40 lower-case hexadecimal digits (20 bytes).
/// The zero address (<c>0x000…0</c>) exists as a value — it is the source of a mint and the
/// destination of a burn — but no account may own it.
/// </summary>
public sealed partial class Address : IEquatable<Address>
{
    public const int HexDigits = 40;

    public Address(string value)
    {
        ArgumentNullException.ThrowIfNull(value);
        if (!Canonical().IsMatch(value))
        {
            throw new FormatException($"'{value}' is not an address: expected 0x followed by {HexDigits} lower-case hexadecimal digits.");
        }

        Value = value;
    }

    public static Address Zero { get; } = new("0x" + new string('0', HexDigits));

    public string Value { get; }

    public bool IsZero => Equals(Zero);

    /// <summary>Reads an address written in any letter case (an EIP-55 checksummed form included) without verifying the checksum.</summary>
    public static Address Parse(string text)
    {
        ArgumentNullException.ThrowIfNull(text);
        string trimmed = text.Trim();
        if (trimmed.StartsWith("0X", StringComparison.Ordinal))
        {
            throw new FormatException("An address starts with a lower-case 0x.");
        }

        return new Address(trimmed.ToLowerInvariant());
    }

    public bool Equals(Address? other) => other is not null && string.Equals(Value, other.Value, StringComparison.Ordinal);

    public override bool Equals(object? obj) => Equals(obj as Address);

    public override int GetHashCode() => Value.GetHashCode(StringComparison.Ordinal);

    public override string ToString() => Value;

    [GeneratedRegex("^0x[0-9a-f]{40}$", RegexOptions.CultureInvariant)]
    private static partial Regex Canonical();
}
