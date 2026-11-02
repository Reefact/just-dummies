namespace NetworkAcl.Domain;

/// <summary>
/// A packet presented to the policy: addresses of one family, a protocol number, ports and TCP flags when the
/// protocol has them, and a payload. The datagram never exceeds 65 535 bytes once its headers are counted.
/// </summary>
public abstract class Packet
{
    public const int MaxDatagramBytes = 65_535;

    private readonly byte[] _payload;

    private protected Packet(IpVersion version, byte protocol, ushort sourcePort, ushort destinationPort, TcpFlags tcpFlags, byte[] payload)
    {
        ArgumentNullException.ThrowIfNull(payload);
        if (protocol == IpProtocols.Reserved)
        {
            throw new ArgumentOutOfRangeException(nameof(protocol), protocol, "Protocol 255 is reserved.");
        }

        if (!IpProtocols.IsAllowedOn(protocol, version))
        {
            throw new ArgumentException($"Protocol {protocol} does not run over IP{version}.", nameof(protocol));
        }

        if (!IpProtocols.CarriesPorts(protocol) && (sourcePort != 0 || destinationPort != 0))
        {
            throw new ArgumentException($"Protocol {protocol} has no ports; both must be 0.", nameof(sourcePort));
        }

        if (protocol == IpProtocols.Tcp)
        {
            if (!TcpFlagMatch.IsValidCombination(tcpFlags))
            {
                throw new ArgumentException($"A TCP segment cannot carry flags {tcpFlags}.", nameof(tcpFlags));
            }
        }
        else if (tcpFlags != TcpFlags.None)
        {
            throw new ArgumentException("Only TCP segments carry TCP flags.", nameof(tcpFlags));
        }

        int maxPayload = MaxPayloadBytes(version, protocol);
        if (payload.Length > maxPayload)
        {
            throw new ArgumentOutOfRangeException(nameof(payload), payload.Length, $"An IP{version} packet of protocol {protocol} carries at most {maxPayload} payload bytes.");
        }

        Version = version;
        Protocol = protocol;
        SourcePort = sourcePort;
        DestinationPort = destinationPort;
        TcpFlags = tcpFlags;
        _payload = (byte[])payload.Clone();
    }

    public IpVersion Version { get; }

    public byte Protocol { get; }

    public ushort SourcePort { get; }

    public ushort DestinationPort { get; }

    public TcpFlags TcpFlags { get; }

    public ReadOnlyMemory<byte> Payload => _payload;

    /// <summary>The datagram's length on the wire: IP header (IPv4) or payload length field (IPv6), transport header and payload.</summary>
    public int Length => HeaderBytes(Version, Protocol) + _payload.Length;

    internal abstract UInt128 SourceBits { get; }

    internal abstract UInt128 DestinationBits { get; }

    /// <summary>
    /// 65 535 minus the headers: the IPv4 total length covers its 20-byte header; the IPv6 payload length excludes its
    /// fixed 40-byte header. The transport header (TCP 20, UDP 8, ICMP 8, SCTP 12) counts in both.
    /// </summary>
    public static int MaxPayloadBytes(IpVersion version, byte protocol) => MaxDatagramBytes - HeaderBytes(version, protocol);

    private static int HeaderBytes(IpVersion version, byte protocol) =>
        (version == IpVersion.V4 ? Ipv4Packet.HeaderBytes : 0) + IpProtocols.TransportHeaderBytes(protocol);
}
