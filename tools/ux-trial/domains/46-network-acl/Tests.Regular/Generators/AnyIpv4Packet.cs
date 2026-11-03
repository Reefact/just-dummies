using JustDummies;

namespace NetworkAcl.Domain;

/// <summary>
///     A generator of arbitrary <see cref="Ipv4Packet" /> values. Written by hand: the ports, the flags and the payload
///     ceiling all depend on the protocol. The source is a unicast one; the payload stays small.
/// </summary>
public sealed class AnyIpv4Packet : IAny<Ipv4Packet> {

    private readonly Ipv4Address?          _source;
    private readonly Ipv4Address?          _destination;
    private readonly Support.TransportPins _pins;

    public AnyIpv4Packet()
        : this(null, null, new Support.TransportPins()) { }

    private AnyIpv4Packet(Ipv4Address? source, Ipv4Address? destination, Support.TransportPins pins) {
        _source      = source;
        _destination = destination;
        _pins        = pins;
    }

    public AnyIpv4Packet WithSource(Ipv4Address source) {
        return new AnyIpv4Packet(source, _destination, _pins);
    }

    public AnyIpv4Packet WithDestination(Ipv4Address destination) {
        return new AnyIpv4Packet(_source, destination, _pins);
    }

    public AnyIpv4Packet WithProtocol(byte protocol) {
        return new AnyIpv4Packet(_source, _destination, new Support.TransportPins { Protocol = protocol, SourcePort = _pins.SourcePort, DestinationPort = _pins.DestinationPort, Flags = _pins.Flags, Payload = _pins.Payload });
    }

    public AnyIpv4Packet WithPorts(ushort sourcePort, ushort destinationPort) {
        return new AnyIpv4Packet(_source, _destination, new Support.TransportPins { Protocol = _pins.Protocol, SourcePort = sourcePort, DestinationPort = destinationPort, Flags = _pins.Flags, Payload = _pins.Payload });
    }

    public AnyIpv4Packet WithDestinationPort(ushort destinationPort) {
        return new AnyIpv4Packet(_source, _destination, new Support.TransportPins { Protocol = _pins.Protocol, SourcePort = _pins.SourcePort, DestinationPort = destinationPort, Flags = _pins.Flags, Payload = _pins.Payload });
    }

    public AnyIpv4Packet WithTcpFlags(TcpFlags flags) {
        return new AnyIpv4Packet(_source, _destination, new Support.TransportPins { Protocol = _pins.Protocol, SourcePort = _pins.SourcePort, DestinationPort = _pins.DestinationPort, Flags = flags, Payload = _pins.Payload });
    }

    public AnyIpv4Packet WithPayload(byte[] payload) {
        return new AnyIpv4Packet(_source, _destination, new Support.TransportPins { Protocol = _pins.Protocol, SourcePort = _pins.SourcePort, DestinationPort = _pins.DestinationPort, Flags = _pins.Flags, Payload = payload });
    }

    public Ipv4Packet Generate() {
        // Unicast sources only: below 224.0.0.0, which leaves out multicast, the reserved block and the limited broadcast.
        IAny<Ipv4Address> sources      = _source is { } source ? Any.Fixed(source) : Any.UInt32().LessThan(0xE000_0000u).As(value => new Ipv4Address(value));
        IAny<Ipv4Address> destinations = _destination is { } destination ? Any.Fixed(destination) : new AnyIpv4Address();

        return Any.Combine(sources,
                           destinations,
                           Support.Transport(IpVersion.V4, _pins),
                           (src, dst, parts) => new Ipv4Packet(src, dst, parts.Protocol, parts.SourcePort, parts.DestinationPort, parts.Flags, parts.Payload)).Generate();
    }

}
