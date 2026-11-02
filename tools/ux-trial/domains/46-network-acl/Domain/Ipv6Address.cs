using System.Globalization;
using System.Text;

namespace NetworkAcl.Domain;

/// <summary>An IPv6 address, held as its 128 bits in network order.</summary>
public sealed class Ipv6Address : IEquatable<Ipv6Address>, IComparable<Ipv6Address>
{
    public const int BitLength = 128;

    private const int GroupCount = 8;

    public Ipv6Address(UInt128 value)
    {
        Value = value;
    }

    public UInt128 Value { get; }

    /// <summary>ff00::/8.</summary>
    public bool IsMulticast => (Value >> 120) == 0xFF;

    /// <summary>
    /// Parses the RFC 4291 text forms: eight groups of one to four hex digits separated by colons, at most one
    /// <c>::</c> standing for one or more zero groups, and an optional dotted-quad IPv4 tail counting as two groups
    /// (<c>::ffff:192.0.2.1</c>). Zone indices (<c>%eth0</c>) are refused: a policy matches addresses, not interfaces.
    /// </summary>
    public static Ipv6Address Parse(string text)
    {
        ArgumentNullException.ThrowIfNull(text);
        if (text.Contains('%'))
        {
            throw new FormatException($"A zone index is not part of an address: '{text}'.");
        }

        int compression = text.IndexOf("::", StringComparison.Ordinal);
        if (compression >= 0 && text.IndexOf("::", compression + 1, StringComparison.Ordinal) >= 0)
        {
            throw new FormatException($"'::' may appear only once in '{text}'.");
        }

        List<ushort> head;
        List<ushort> tail;
        if (compression < 0)
        {
            head = ParseGroups(text, text, allowIpv4Tail: true);
            tail = [];
            if (head.Count != GroupCount)
            {
                throw new FormatException($"An uncompressed IPv6 address has 8 groups, '{text}' has {head.Count}.");
            }
        }
        else
        {
            string tailText = text[(compression + 2)..];
            head = ParseGroups(text[..compression], text, allowIpv4Tail: false);
            tail = ParseGroups(tailText, text, allowIpv4Tail: true);
            if (head.Count + tail.Count > GroupCount - 1)
            {
                throw new FormatException($"'::' must stand for at least one zero group in '{text}'.");
            }
        }

        UInt128 value = UInt128.Zero;
        foreach (ushort group in head)
        {
            value = (value << 16) | group;
        }

        value <<= 16 * (GroupCount - head.Count - tail.Count);
        foreach (ushort group in tail)
        {
            value = (value << 16) | group;
        }

        return new Ipv6Address(value);
    }

    private static List<ushort> ParseGroups(string part, string text, bool allowIpv4Tail)
    {
        List<ushort> groups = [];
        if (part.Length == 0)
        {
            return groups;
        }

        string[] pieces = part.Split(':');
        for (int i = 0; i < pieces.Length; i++)
        {
            string piece = pieces[i];
            if (piece.Contains('.'))
            {
                if (!allowIpv4Tail || i != pieces.Length - 1)
                {
                    throw new FormatException($"An embedded IPv4 address may only end '{text}'.");
                }

                uint ipv4 = Ipv4Address.Parse(piece).Value;
                groups.Add((ushort)(ipv4 >> 16));
                groups.Add((ushort)(ipv4 & 0xFFFF));
                continue;
            }

            if (piece.Length is 0 or > 4 || !piece.All(char.IsAsciiHexDigit))
            {
                throw new FormatException($"'{piece}' is not a group of one to four hex digits in '{text}'.");
            }

            groups.Add(ushort.Parse(piece, NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture));
        }

        return groups;
    }

    public int CompareTo(Ipv6Address? other) => other is null ? 1 : Value.CompareTo(other.Value);

    public bool Equals(Ipv6Address? other) => other is not null && other.Value == Value;

    public override bool Equals(object? obj) => Equals(obj as Ipv6Address);

    public override int GetHashCode() => Value.GetHashCode();

    /// <summary>The RFC 5952 canonical form: lower-case, no leading zeros, the longest run of two or more zero groups compressed.</summary>
    public override string ToString()
    {
        ushort[] groups = new ushort[GroupCount];
        for (int i = 0; i < GroupCount; i++)
        {
            groups[i] = (ushort)(Value >> (16 * (GroupCount - 1 - i)));
        }

        int bestStart = -1;
        int bestLength = 1;
        for (int i = 0; i < GroupCount;)
        {
            if (groups[i] != 0)
            {
                i++;
                continue;
            }

            int start = i;
            while (i < GroupCount && groups[i] == 0)
            {
                i++;
            }

            if (i - start > bestLength)
            {
                bestStart = start;
                bestLength = i - start;
            }
        }

        StringBuilder builder = new();
        for (int i = 0; i < GroupCount; i++)
        {
            if (i == bestStart)
            {
                builder.Append("::");
                i += bestLength - 1;
                continue;
            }

            if (builder.Length > 0 && builder[^1] != ':')
            {
                builder.Append(':');
            }

            builder.Append(groups[i].ToString("x", CultureInfo.InvariantCulture));
        }

        return builder.ToString();
    }
}
