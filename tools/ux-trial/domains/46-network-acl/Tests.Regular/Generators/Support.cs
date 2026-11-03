using System;
using System.Collections.Generic;
using System.Linq;

using JustDummies;

namespace NetworkAcl.Domain;

/// <summary>Hand-written pieces the scaffolded generators share: the protocol families and the transport-dependent parts of a packet.</summary>
internal static class Support {

    /// <summary>Every flag set without SYN+FIN, SYN+RST or FIN+RST, None included.</summary>
    public static readonly TcpFlags[] FlagsWithoutForbiddenPair = Enumerable.Range(0, 256)
                                                                           .Select(bits => (TcpFlags)bits)
                                                                           .Where(flags => !flags.HasFlag(TcpFlags.Syn | TcpFlags.Fin)
                                                                                        && !flags.HasFlag(TcpFlags.Syn | TcpFlags.Rst)
                                                                                        && !flags.HasFlag(TcpFlags.Fin | TcpFlags.Rst))
                                                                           .ToArray();

    /// <summary>The flag sets a TCP segment may carry: at least one flag, no forbidden pair.</summary>
    public static readonly TcpFlags[] ValidTcpFlags = FlagsWithoutForbiddenPair.Where(flags => flags != TcpFlags.None).ToArray();

    public static byte IcmpOf(IpVersion version) {
        return version == IpVersion.V4 ? IpProtocols.Icmp : IpProtocols.Icmpv6;
    }

    /// <summary>A protocol number the policy knows nothing special about (not ICMP, TCP, UDP, ICMPv6, SCTP or the reserved 255).</summary>
    public static AnyByte OtherProtocol() {
        return Any.Byte().Except(IpProtocols.Icmp, IpProtocols.Tcp, IpProtocols.Udp, IpProtocols.Icmpv6, IpProtocols.Sctp, IpProtocols.Reserved);
    }

    /// <summary>Any protocol a packet of this family can carry.</summary>
    public static IAny<byte> PacketProtocols(IpVersion version) {
        return Any.Boolean().Then(common => common
                                                ? (IAny<byte>)Any.OneOf(IpProtocols.Tcp, IpProtocols.Udp, IpProtocols.Sctp, IcmpOf(version))
                                                : OtherProtocol());
    }

    public sealed class TransportParts {

        public TransportParts(byte protocol, ushort sourcePort, ushort destinationPort, TcpFlags flags, byte[] payload) {
            Protocol        = protocol;
            SourcePort      = sourcePort;
            DestinationPort = destinationPort;
            Flags           = flags;
            Payload         = payload;
        }

        public byte      Protocol        { get; }
        public ushort    SourcePort      { get; }
        public ushort    DestinationPort { get; }
        public TcpFlags  Flags           { get; }
        public byte[]    Payload         { get; }

    }

    /// <summary>What a test may pin about the protocol-dependent part of a packet; whatever it leaves open is drawn.</summary>
    public sealed class TransportPins {

        public byte?     Protocol        { get; init; }
        public ushort?   SourcePort      { get; init; }
        public ushort?   DestinationPort { get; init; }
        public TcpFlags? Flags           { get; init; }
        public byte[]?   Payload         { get; init; }

    }

    /// <summary>
    ///     Everything a packet draws that depends on its protocol: ports only when the protocol carries them, flags only
    ///     for TCP (and then a valid combination), a small payload.
    /// </summary>
    public static IAny<TransportParts> Transport(IpVersion version, TransportPins pins) {
        IAny<byte> protocols = pins.Protocol is { } pinned ? Any.Fixed(pinned) : PacketProtocols(version);

        return protocols.Then(protocol => {
            IAny<ushort> sourcePorts      = pins.SourcePort is { } sp ? Any.Fixed(sp) : IpProtocols.CarriesPorts(protocol) ? Any.UInt16() : Any.Fixed<ushort>(0);
            IAny<ushort> destinationPorts = pins.DestinationPort is { } dp ? Any.Fixed(dp) : IpProtocols.CarriesPorts(protocol) ? Any.UInt16() : Any.Fixed<ushort>(0);
            IAny<TcpFlags> flags          = pins.Flags is { } f ? Any.Fixed(f) : protocol == IpProtocols.Tcp ? Any.ElementOf(ValidTcpFlags) : Any.Fixed(TcpFlags.None);
            IAny<byte[]> payloads         = pins.Payload is { } payload ? Any.Fixed(payload) : Any.ArrayOf(Any.Byte()).WithCountBetween(0, 300);

            return Any.Combine(sourcePorts,
                               destinationPorts,
                               flags,
                               payloads,
                               (sourcePort, destinationPort, tcpFlags, bytes) => new TransportParts(protocol, sourcePort, destinationPort, tcpFlags, bytes));
        });
    }

}
