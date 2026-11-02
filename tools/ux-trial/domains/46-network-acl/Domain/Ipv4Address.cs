using System.Globalization;

namespace NetworkAcl.Domain;

/// <summary>An IPv4 address, held as its 32 bits in network order (the first octet is the most significant byte).</summary>
public sealed class Ipv4Address : IEquatable<Ipv4Address>, IComparable<Ipv4Address>
{
    public const int BitLength = 32;

    public Ipv4Address(uint value)
    {
        Value = value;
    }

    public uint Value { get; }

    /// <summary>224.0.0.0/4.</summary>
    public bool IsMulticast => (Value & 0xF000_0000u) == 0xE000_0000u;

    /// <summary>The limited broadcast address 255.255.255.255.</summary>
    public bool IsLimitedBroadcast => Value == uint.MaxValue;

    /// <summary>Parses dotted-quad notation: four decimal octets 0-255, without leading zeros (they read as octal elsewhere).</summary>
    public static Ipv4Address Parse(string text)
    {
        ArgumentNullException.ThrowIfNull(text);
        string[] parts = text.Split('.');
        if (parts.Length != 4)
        {
            throw new FormatException($"An IPv4 address has four dot-separated octets, got '{text}'.");
        }

        uint value = 0;
        foreach (string part in parts)
        {
            if (part.Length is 0 or > 3 || !part.All(char.IsAsciiDigit))
            {
                throw new FormatException($"'{part}' is not a decimal octet in '{text}'.");
            }

            if (part.Length > 1 && part[0] == '0')
            {
                throw new FormatException($"Octet '{part}' has a leading zero in '{text}'.");
            }

            int octet = int.Parse(part, NumberStyles.None, CultureInfo.InvariantCulture);
            if (octet > 255)
            {
                throw new FormatException($"Octet {octet} is over 255 in '{text}'.");
            }

            value = (value << 8) | (uint)octet;
        }

        return new Ipv4Address(value);
    }

    public int CompareTo(Ipv4Address? other) => other is null ? 1 : Value.CompareTo(other.Value);

    public bool Equals(Ipv4Address? other) => other is not null && other.Value == Value;

    public override bool Equals(object? obj) => Equals(obj as Ipv4Address);

    public override int GetHashCode() => Value.GetHashCode();

    public override string ToString() =>
        string.Create(CultureInfo.InvariantCulture, $"{Value >> 24}.{(Value >> 16) & 0xFF}.{(Value >> 8) & 0xFF}.{Value & 0xFF}");
}
