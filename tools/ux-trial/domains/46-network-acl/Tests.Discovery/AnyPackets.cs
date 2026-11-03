using System;

using JustDummies;

namespace NetworkAcl.Domain;

/// <summary>What the two packet generators share: the transport part of a packet, drawn consistently.</summary>
internal sealed class PacketParts {

    public byte?     Protocol;
    public ushort?   SourcePort;
    public ushort?   DestinationPort;
    public TcpFlags? Flags;
    public byte[]?   Payload;

    public PacketParts Clone() {
        return (PacketParts)MemberwiseClone();
    }

    public (byte Protocol, ushort SourcePort, ushort DestinationPort, TcpFlags Flags, byte[] Payload) Draw(IpVersion version) {
        byte familyIcmp = version == IpVersion.V4 ? IpProtocols.Icmp : IpProtocols.Icmpv6;
        byte protocol   = Protocol ?? Any.OneOf(IpProtocols.Tcp, IpProtocols.Udp, IpProtocols.Sctp, familyIcmp, (byte)47, (byte)89).Generate();

        ushort sourcePort      = SourcePort      ?? (IpProtocols.CarriesPorts(protocol) ? Any.UInt16().Generate() : (ushort)0);
        ushort destinationPort = DestinationPort ?? (IpProtocols.CarriesPorts(protocol) ? Any.UInt16().Generate() : (ushort)0);

        TcpFlags flags = Flags ?? (protocol == IpProtocols.Tcp ? ValidFlags() : TcpFlags.None);
        byte[]   data  = Payload ?? Any.ArrayOf(Any.Byte()).WithCountBetween(0, 64).Generate();

        return (protocol, sourcePort, destinationPort, flags, data);
    }

    private static TcpFlags ValidFlags() {
        byte exclusive = Any.OneOf((byte)TcpFlags.None, (byte)TcpFlags.Fin, (byte)TcpFlags.Syn, (byte)TcpFlags.Rst).Generate();
        byte rest      = (byte)(Any.Byte().Generate() & 0xF8);
        TcpFlags flags = (TcpFlags)(exclusive | rest);

        return flags == TcpFlags.None ? TcpFlags.Ack : flags;
    }

}

/// <summary>A generator of arbitrary valid <see cref="Ipv4Packet" /> values. Written by hand: protocol, ports, flags and payload size are tied.</summary>
public sealed class AnyIpv4Packet : IAny<Ipv4Packet> {

    private readonly IAny<Ipv4Address> _source;
    private readonly IAny<Ipv4Address> _destination;
    private readonly PacketParts       _parts;

    public AnyIpv4Packet()
        : this(Any.UInt32().Between(0u, 0xDFFF_FFFFu).As(v => new Ipv4Address(v)), new AnyIpv4Address(), new PacketParts()) { }

    private AnyIpv4Packet(IAny<Ipv4Address> source, IAny<Ipv4Address> destination, PacketParts parts) {
        _source      = source;
        _destination = destination;
        _parts       = parts;
    }

    public AnyIpv4Packet WithSource(Ipv4Address value)      => new(Any.Fixed(value), _destination, _parts);
    public AnyIpv4Packet WithDestination(Ipv4Address value) => new(_source, Any.Fixed(value), _parts);
    public AnyIpv4Packet WithProtocol(byte value)           => With(p => p.Protocol = value);
    public AnyIpv4Packet WithSourcePort(ushort value)       => With(p => p.SourcePort = value);
    public AnyIpv4Packet WithDestinationPort(ushort value)  => With(p => p.DestinationPort = value);
    public AnyIpv4Packet WithTcpFlags(TcpFlags value)       => With(p => p.Flags = value);
    public AnyIpv4Packet WithPayload(byte[] value)          => With(p => p.Payload = value);

    public Ipv4Packet Generate() {
        (byte protocol, ushort sourcePort, ushort destinationPort, TcpFlags flags, byte[] payload) = _parts.Draw(IpVersion.V4);

        return new Ipv4Packet(_source.Generate(), _destination.Generate(), protocol, sourcePort, destinationPort, flags, payload);
    }

    private AnyIpv4Packet With(Action<PacketParts> change) {
        PacketParts parts = _parts.Clone();
        change(parts);

        return new AnyIpv4Packet(_source, _destination, parts);
    }

}

/// <summary>A generator of arbitrary valid <see cref="Ipv6Packet" /> values. Written by hand, like its IPv4 sibling.</summary>
public sealed class AnyIpv6Packet : IAny<Ipv6Packet> {

    private readonly IAny<Ipv6Address> _source;
    private readonly IAny<Ipv6Address> _destination;
    private readonly PacketParts       _parts;

    public AnyIpv6Packet()
        : this(Any.UInt128().Between(UInt128.Zero, (UInt128.One << 127) - 1).As(v => new Ipv6Address(v)), new AnyIpv6Address(), new PacketParts()) { }

    private AnyIpv6Packet(IAny<Ipv6Address> source, IAny<Ipv6Address> destination, PacketParts parts) {
        _source      = source;
        _destination = destination;
        _parts       = parts;
    }

    public AnyIpv6Packet WithSource(Ipv6Address value)      => new(Any.Fixed(value), _destination, _parts);
    public AnyIpv6Packet WithDestination(Ipv6Address value) => new(_source, Any.Fixed(value), _parts);
    public AnyIpv6Packet WithProtocol(byte value)           => With(p => p.Protocol = value);
    public AnyIpv6Packet WithSourcePort(ushort value)       => With(p => p.SourcePort = value);
    public AnyIpv6Packet WithDestinationPort(ushort value)  => With(p => p.DestinationPort = value);
    public AnyIpv6Packet WithTcpFlags(TcpFlags value)       => With(p => p.Flags = value);
    public AnyIpv6Packet WithPayload(byte[] value)          => With(p => p.Payload = value);

    public Ipv6Packet Generate() {
        (byte protocol, ushort sourcePort, ushort destinationPort, TcpFlags flags, byte[] payload) = _parts.Draw(IpVersion.V6);

        return new Ipv6Packet(_source.Generate(), _destination.Generate(), protocol, sourcePort, destinationPort, flags, payload);
    }

    private AnyIpv6Packet With(Action<PacketParts> change) {
        PacketParts parts = _parts.Clone();
        change(parts);

        return new AnyIpv6Packet(_source, _destination, parts);
    }

}
