using System;

using JustDummies;

using NetworkAcl.Domain;

namespace NetworkAcl.Tests.Expert.Arbitrary;

/// <summary>
///     Arbitrary <see cref="Ipv4Packet" /> values. Written by hand: the scaffold left six of seven parameters "to verify"
///     because the protocol decides whether ports and TCP flags may be non-zero. Here an unpinned port or flags set follows the
///     protocol (ports only for TCP/UDP/SCTP, flags only for TCP), while a pinned one is handed to the constructor as it is,
///     so a contradictory pin is refused by the domain instead of being silently dropped.
/// </summary>
public sealed class AnyIpv4Packet : IAny<Ipv4Packet> {

    private const byte Gre = 47;

    private Ipv4Address? _source;
    private Ipv4Address? _destination;
    private byte?        _protocol;
    private ushort?      _sourcePort;
    private ushort?      _destinationPort;
    private TcpFlags?    _tcpFlags;
    private byte[]?      _payload;

    public AnyIpv4Packet WithSource(Ipv4Address value) => Copy(copy => copy._source = value);

    public AnyIpv4Packet WithDestination(Ipv4Address value) => Copy(copy => copy._destination = value);

    public AnyIpv4Packet WithProtocol(byte value) => Copy(copy => copy._protocol = value);

    public AnyIpv4Packet WithSourcePort(ushort value) => Copy(copy => copy._sourcePort = value);

    public AnyIpv4Packet WithDestinationPort(ushort value) => Copy(copy => copy._destinationPort = value);

    public AnyIpv4Packet WithTcpFlags(TcpFlags value) => Copy(copy => copy._tcpFlags = value);

    public AnyIpv4Packet WithPayload(byte[] value) => Copy(copy => copy._payload = value);

    public Ipv4Packet Generate() {
        Ipv4Address source      = _source ?? new AnyIpv4Address().WithValue(value => value.LessThan(0xE000_0000u)).Generate();
        Ipv4Address destination = _destination ?? new AnyIpv4Address().Generate();
        byte        protocol    = _protocol ?? Any.Byte().OneOf(IpProtocols.Tcp, IpProtocols.Udp, IpProtocols.Sctp, IpProtocols.Icmp, Gre).Generate();
        bool        carriesPorts = IpProtocols.CarriesPorts(protocol);

        ushort   sourcePort      = _sourcePort ?? (carriesPorts ? Any.UInt16().Generate() : (ushort)0);
        ushort   destinationPort = _destinationPort ?? (carriesPorts ? Any.UInt16().Generate() : (ushort)0);
        TcpFlags tcpFlags        = _tcpFlags ?? (protocol == IpProtocols.Tcp ? AnyTcpSegmentFlags.Segment().Generate() : TcpFlags.None);
        byte[]   payload         = _payload ?? Any.ArrayOf(Any.Byte()).WithMaxCount(64).Generate();

        return new Ipv4Packet(source, destination, protocol, sourcePort, destinationPort, tcpFlags, payload);
    }

    private AnyIpv4Packet Copy(Action<AnyIpv4Packet> change) {
        AnyIpv4Packet copy = (AnyIpv4Packet)MemberwiseClone();
        change(copy);

        return copy;
    }

}
