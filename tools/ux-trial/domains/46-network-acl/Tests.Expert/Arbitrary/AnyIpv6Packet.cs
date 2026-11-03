using System;

using JustDummies;

using NetworkAcl.Domain;

namespace NetworkAcl.Tests.Expert.Arbitrary;

/// <summary>
///     Arbitrary <see cref="Ipv6Packet" /> values. Written by hand: the scaffold left six of seven parameters "to verify"
///     because the protocol decides whether ports and TCP flags may be non-zero. Here an unpinned port or flags set follows the
///     protocol (ports only for TCP/UDP/SCTP, flags only for TCP), while a pinned one is handed to the constructor as it is,
///     so a contradictory pin is refused by the domain instead of being silently dropped.
/// </summary>
public sealed class AnyIpv6Packet : IAny<Ipv6Packet> {

    private const byte Gre = 47;

    private Ipv6Address? _source;
    private Ipv6Address? _destination;
    private byte?        _protocol;
    private ushort?      _sourcePort;
    private ushort?      _destinationPort;
    private TcpFlags?    _tcpFlags;
    private byte[]?      _payload;

    public AnyIpv6Packet WithSource(Ipv6Address value) => Copy(copy => copy._source = value);

    public AnyIpv6Packet WithDestination(Ipv6Address value) => Copy(copy => copy._destination = value);

    public AnyIpv6Packet WithProtocol(byte value) => Copy(copy => copy._protocol = value);

    public AnyIpv6Packet WithSourcePort(ushort value) => Copy(copy => copy._sourcePort = value);

    public AnyIpv6Packet WithDestinationPort(ushort value) => Copy(copy => copy._destinationPort = value);

    public AnyIpv6Packet WithTcpFlags(TcpFlags value) => Copy(copy => copy._tcpFlags = value);

    public AnyIpv6Packet WithPayload(byte[] value) => Copy(copy => copy._payload = value);

    public Ipv6Packet Generate() {
        Ipv6Address source      = _source ?? new AnyIpv6Address().WithValue(value => value.LessThan(new UInt128(0xFF00_0000_0000_0000UL, 0UL))).Generate();
        Ipv6Address destination = _destination ?? new AnyIpv6Address().Generate();
        byte        protocol    = _protocol ?? Any.Byte().OneOf(IpProtocols.Tcp, IpProtocols.Udp, IpProtocols.Sctp, IpProtocols.Icmpv6, Gre).Generate();
        bool        carriesPorts = IpProtocols.CarriesPorts(protocol);

        ushort   sourcePort      = _sourcePort ?? (carriesPorts ? Any.UInt16().Generate() : (ushort)0);
        ushort   destinationPort = _destinationPort ?? (carriesPorts ? Any.UInt16().Generate() : (ushort)0);
        TcpFlags tcpFlags        = _tcpFlags ?? (protocol == IpProtocols.Tcp ? AnyTcpSegmentFlags.Segment().Generate() : TcpFlags.None);
        byte[]   payload         = _payload ?? Any.ArrayOf(Any.Byte()).WithMaxCount(64).Generate();

        return new Ipv6Packet(source, destination, protocol, sourcePort, destinationPort, tcpFlags, payload);
    }

    private AnyIpv6Packet Copy(Action<AnyIpv6Packet> change) {
        AnyIpv6Packet copy = (AnyIpv6Packet)MemberwiseClone();
        change(copy);

        return copy;
    }

}
