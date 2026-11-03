using System;

using JustDummies;

namespace NetworkAcl.Domain;

/// <summary>
///     A generator of arbitrary <see cref="Ipv6Packet" /> values. Written by hand: the ports, the flags and the payload
///     ceiling all depend on the protocol. The source is never multicast; the payload stays small.
/// </summary>
public sealed class AnyIpv6Packet : IAny<Ipv6Packet> {

    // ff00::/8 starts here: every source below it is a non-multicast one.
    private static readonly UInt128 FirstMulticast = (UInt128)0xFF << 120;

    private readonly Ipv6Address?          _source;
    private readonly Ipv6Address?          _destination;
    private readonly Support.TransportPins _pins;

    public AnyIpv6Packet()
        : this(null, null, new Support.TransportPins()) { }

    private AnyIpv6Packet(Ipv6Address? source, Ipv6Address? destination, Support.TransportPins pins) {
        _source      = source;
        _destination = destination;
        _pins        = pins;
    }

    public AnyIpv6Packet WithSource(Ipv6Address source) {
        return new AnyIpv6Packet(source, _destination, _pins);
    }

    public AnyIpv6Packet WithDestination(Ipv6Address destination) {
        return new AnyIpv6Packet(_source, destination, _pins);
    }

    public AnyIpv6Packet WithProtocol(byte protocol) {
        return new AnyIpv6Packet(_source, _destination, new Support.TransportPins { Protocol = protocol, SourcePort = _pins.SourcePort, DestinationPort = _pins.DestinationPort, Flags = _pins.Flags, Payload = _pins.Payload });
    }

    public AnyIpv6Packet WithPorts(ushort sourcePort, ushort destinationPort) {
        return new AnyIpv6Packet(_source, _destination, new Support.TransportPins { Protocol = _pins.Protocol, SourcePort = sourcePort, DestinationPort = destinationPort, Flags = _pins.Flags, Payload = _pins.Payload });
    }

    public AnyIpv6Packet WithDestinationPort(ushort destinationPort) {
        return new AnyIpv6Packet(_source, _destination, new Support.TransportPins { Protocol = _pins.Protocol, SourcePort = _pins.SourcePort, DestinationPort = destinationPort, Flags = _pins.Flags, Payload = _pins.Payload });
    }

    public AnyIpv6Packet WithTcpFlags(TcpFlags flags) {
        return new AnyIpv6Packet(_source, _destination, new Support.TransportPins { Protocol = _pins.Protocol, SourcePort = _pins.SourcePort, DestinationPort = _pins.DestinationPort, Flags = flags, Payload = _pins.Payload });
    }

    public AnyIpv6Packet WithPayload(byte[] payload) {
        return new AnyIpv6Packet(_source, _destination, new Support.TransportPins { Protocol = _pins.Protocol, SourcePort = _pins.SourcePort, DestinationPort = _pins.DestinationPort, Flags = _pins.Flags, Payload = payload });
    }

    public Ipv6Packet Generate() {
        IAny<Ipv6Address> sources      = _source is { } source ? Any.Fixed(source) : Any.UInt128().LessThan(FirstMulticast).As(value => new Ipv6Address(value));
        IAny<Ipv6Address> destinations = _destination is { } destination ? Any.Fixed(destination) : new AnyIpv6Address();

        return Any.Combine(sources,
                           destinations,
                           Support.Transport(IpVersion.V6, _pins),
                           (src, dst, parts) => new Ipv6Packet(src, dst, parts.Protocol, parts.SourcePort, parts.DestinationPort, parts.Flags, parts.Payload)).Generate();
    }

}
