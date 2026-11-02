using System.Globalization;

namespace NetworkAcl.Domain;

/// <summary>
/// A CIDR prefix of either family: a network address and a prefix length (0-32 for IPv4, 0-128 for IPv6).
/// The host bits — those past the prefix length — must be zero: <c>10.0.0.0/8</c>, never <c>10.1.2.3/8</c>.
/// </summary>
public sealed class AddressPrefix : IEquatable<AddressPrefix>
{
    private readonly UInt128 _network;

    public AddressPrefix(Ipv4Address network, int prefixLength)
    {
        ArgumentNullException.ThrowIfNull(network);
        ArgumentOutOfRangeException.ThrowIfNegative(prefixLength);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(prefixLength, Ipv4Address.BitLength);
        uint hostBits = network.Value & ~Ipv4Mask(prefixLength);
        if (hostBits != 0)
        {
            throw new ArgumentException($"{network}/{prefixLength} has host bits set; the network is {new Ipv4Address(network.Value & Ipv4Mask(prefixLength))}.", nameof(network));
        }

        Version = IpVersion.V4;
        PrefixLength = prefixLength;
        _network = network.Value;
    }

    public AddressPrefix(Ipv6Address network, int prefixLength)
    {
        ArgumentNullException.ThrowIfNull(network);
        if (prefixLength < 0 || prefixLength > Ipv6Address.BitLength)
        {
            throw new ArgumentOutOfRangeException(nameof(prefixLength), prefixLength, "An IPv6 prefix length is between 0 and 128.");
        }

        if ((network.Value & ~Ipv6Mask(prefixLength)) != UInt128.Zero)
        {
            throw new ArgumentException($"{network}/{prefixLength} has host bits set.", nameof(network));
        }

        Version = IpVersion.V6;
        PrefixLength = prefixLength;
        _network = network.Value;
    }

    public IpVersion Version { get; }

    public int PrefixLength { get; }

    public int AddressBits => Version == IpVersion.V4 ? Ipv4Address.BitLength : Ipv6Address.BitLength;

    /// <summary>Number of host bits, i.e. log2 of the number of addresses covered.</summary>
    public int HostBits => AddressBits - PrefixLength;

    public bool IsAnyAddress => PrefixLength == 0;

    /// <summary>The network address when the prefix is IPv4, otherwise null.</summary>
    public Ipv4Address? Ipv4Network => Version == IpVersion.V4 ? new Ipv4Address((uint)_network) : null;

    /// <summary>The network address when the prefix is IPv6, otherwise null.</summary>
    public Ipv6Address? Ipv6Network => Version == IpVersion.V6 ? new Ipv6Address(_network) : null;

    /// <summary>0.0.0.0/0 or ::/0.</summary>
    public static AddressPrefix Any(IpVersion version) => version switch
    {
        IpVersion.V4 => new AddressPrefix(new Ipv4Address(0), 0),
        IpVersion.V6 => new AddressPrefix(new Ipv6Address(UInt128.Zero), 0),
        _ => throw new ArgumentOutOfRangeException(nameof(version), version, "Unknown IP version."),
    };

    /// <summary>A single host: /32 or /128.</summary>
    public static AddressPrefix Host(Ipv4Address address) => new(address, Ipv4Address.BitLength);

    /// <summary>A single host: /32 or /128.</summary>
    public static AddressPrefix Host(Ipv6Address address) => new(address, Ipv6Address.BitLength);

    /// <summary>Parses <c>address/length</c>; the family is told by the address (a colon means IPv6).</summary>
    public static AddressPrefix Parse(string text)
    {
        ArgumentNullException.ThrowIfNull(text);
        int slash = text.IndexOf('/', StringComparison.Ordinal);
        if (slash < 0 || slash != text.LastIndexOf('/'))
        {
            throw new FormatException($"A prefix reads 'address/length', got '{text}'.");
        }

        string lengthText = text[(slash + 1)..];
        if (lengthText.Length is 0 or > 3 || !lengthText.All(char.IsAsciiDigit))
        {
            throw new FormatException($"'{lengthText}' is not a prefix length in '{text}'.");
        }

        int length = int.Parse(lengthText, NumberStyles.None, CultureInfo.InvariantCulture);
        string address = text[..slash];
        return address.Contains(':')
            ? new AddressPrefix(Ipv6Address.Parse(address), length)
            : new AddressPrefix(Ipv4Address.Parse(address), length);
    }

    public bool Contains(Ipv4Address address)
    {
        ArgumentNullException.ThrowIfNull(address);
        return Version == IpVersion.V4 && (address.Value & Ipv4Mask(PrefixLength)) == (uint)_network;
    }

    public bool Contains(Ipv6Address address)
    {
        ArgumentNullException.ThrowIfNull(address);
        return Version == IpVersion.V6 && (address.Value & Ipv6Mask(PrefixLength)) == _network;
    }

    /// <summary>True when every address of <paramref name="other"/> is in this prefix (same family, shorter or equal length).</summary>
    public bool Covers(AddressPrefix other)
    {
        ArgumentNullException.ThrowIfNull(other);
        return other.Version == Version
            && PrefixLength <= other.PrefixLength
            && (other._network & Mask(PrefixLength)) == _network;
    }

    /// <summary>Two CIDR prefixes of the same family either nest or are disjoint.</summary>
    public bool Overlaps(AddressPrefix other)
    {
        ArgumentNullException.ThrowIfNull(other);
        return Covers(other) || other.Covers(this);
    }

    internal bool ContainsBits(UInt128 address) => (address & Mask(PrefixLength)) == _network;

    private UInt128 Mask(int length) => Version == IpVersion.V4 ? Ipv4Mask(length) : Ipv6Mask(length);

    private static uint Ipv4Mask(int length) => length == 0 ? 0u : uint.MaxValue << (Ipv4Address.BitLength - length);

    private static UInt128 Ipv6Mask(int length) => length == 0 ? UInt128.Zero : UInt128.MaxValue << (Ipv6Address.BitLength - length);

    public bool Equals(AddressPrefix? other) =>
        other is not null && other.Version == Version && other.PrefixLength == PrefixLength && other._network == _network;

    public override bool Equals(object? obj) => Equals(obj as AddressPrefix);

    public override int GetHashCode() => HashCode.Combine(Version, PrefixLength, _network);

    public override string ToString() =>
        Version == IpVersion.V4
            ? $"{new Ipv4Address((uint)_network)}/{PrefixLength}"
            : $"{new Ipv6Address(_network)}/{PrefixLength}";
}
