using JustDummies;

using NetworkAcl.Domain;

using Xunit;

namespace NetworkAcl.Tests.Regular;

public sealed class PacketTests {

    private static Ipv4Address AnyUnicastV4() {
        return Any.UInt32().LessThan(0xE0000000u).As(value => new Ipv4Address(value)).Generate();
    }

    private static Ipv6Address AnyUnicastV6() {
        return Any.UInt128().LessThan((UInt128)0xFF << 120).As(value => new Ipv6Address(value)).Generate();
    }

    private static Ipv4Packet V4(byte protocol, ushort sourcePort, ushort destinationPort, TcpFlags flags, byte[] payload) {
        return new Ipv4Packet(AnyUnicastV4(), Any.Ipv4Address().Generate(), protocol, sourcePort, destinationPort, flags, payload);
    }

    private static Ipv6Packet V6(byte protocol, ushort sourcePort, ushort destinationPort, TcpFlags flags, byte[] payload) {
        return new Ipv6Packet(AnyUnicastV6(), Any.Ipv6Address().Generate(), protocol, sourcePort, destinationPort, flags, payload);
    }

    private static Ipv4Packet WithPayload4(byte protocol, byte[] payload) {
        return V4(protocol, 0, 0, protocol == IpProtocols.Tcp ? TcpFlags.Ack : TcpFlags.None, payload);
    }

    private static Ipv6Packet WithPayload6(byte protocol, byte[] payload) {
        return V6(protocol, 0, 0, protocol == IpProtocols.Tcp ? TcpFlags.Ack : TcpFlags.None, payload);
    }

    [Fact]
    public void A_drawn_ipv4_packet_is_a_well_formed_one() {
        // Arrange
        Ipv4Packet anyPacket = Any.Ipv4Packet().Generate();

        // Assert
        Assert.Equal(IpVersion.V4, anyPacket.Version);
        Assert.False(anyPacket.Source.IsMulticast);
        Assert.True(anyPacket.Length <= Packet.MaxDatagramBytes);
    }

    [Fact]
    public void A_drawn_ipv6_packet_is_a_well_formed_one() {
        // Arrange
        Ipv6Packet anyPacket = Any.Ipv6Packet().Generate();

        // Assert
        Assert.Equal(IpVersion.V6, anyPacket.Version);
        Assert.False(anyPacket.Source.IsMulticast);
        Assert.True(anyPacket.Length <= Packet.MaxDatagramBytes);
    }

    [Theory]
    [InlineData(IpVersion.V4, IpProtocols.Tcp, 65495)]
    [InlineData(IpVersion.V4, IpProtocols.Udp, 65507)]
    [InlineData(IpVersion.V4, IpProtocols.Icmp, 65507)]
    [InlineData(IpVersion.V4, IpProtocols.Sctp, 65503)]
    [InlineData(IpVersion.V4, 47, 65515)]
    [InlineData(IpVersion.V6, IpProtocols.Tcp, 65515)]
    [InlineData(IpVersion.V6, IpProtocols.Udp, 65527)]
    [InlineData(IpVersion.V6, IpProtocols.Icmpv6, 65527)]
    [InlineData(IpVersion.V6, IpProtocols.Sctp, 65523)]
    [InlineData(IpVersion.V6, 47, 65535)]
    public void The_largest_payload_leaves_room_for_the_headers(IpVersion version, byte protocol, int expected) {
        // Act & Assert
        Assert.Equal(expected, Packet.MaxPayloadBytes(version, protocol));
    }

    [Theory]
    [InlineData(IpProtocols.Tcp)]
    [InlineData(IpProtocols.Udp)]
    [InlineData(IpProtocols.Icmp)]
    [InlineData(IpProtocols.Sctp)]
    [InlineData(47)]
    public void An_ipv4_payload_of_exactly_the_maximum_is_accepted_and_one_more_byte_is_not(byte protocol) {
        // Arrange
        int max = Packet.MaxPayloadBytes(IpVersion.V4, protocol);

        // Act
        Ipv4Packet full = WithPayload4(protocol, new byte[max]);
        ArgumentOutOfRangeException tooBig = Assert.Throws<ArgumentOutOfRangeException>(() => WithPayload4(protocol, new byte[max + 1]));

        // Assert
        Assert.Equal(Packet.MaxDatagramBytes, full.Length);
        Assert.Equal("payload", tooBig.ParamName);
    }

    [Theory]
    [InlineData(IpProtocols.Tcp)]
    [InlineData(IpProtocols.Udp)]
    [InlineData(IpProtocols.Icmpv6)]
    [InlineData(IpProtocols.Sctp)]
    [InlineData(47)]
    public void An_ipv6_payload_of_exactly_the_maximum_is_accepted_and_one_more_byte_is_not(byte protocol) {
        // Arrange
        int max = Packet.MaxPayloadBytes(IpVersion.V6, protocol);

        // Act
        Ipv6Packet full = WithPayload6(protocol, new byte[max]);
        ArgumentOutOfRangeException tooBig = Assert.Throws<ArgumentOutOfRangeException>(() => WithPayload6(protocol, new byte[max + 1]));

        // Assert
        Assert.Equal(Packet.MaxDatagramBytes, full.Length);
        Assert.Equal("payload", tooBig.ParamName);
    }

    [Theory]
    [InlineData(IpProtocols.Tcp, 20)]
    [InlineData(IpProtocols.Udp, 8)]
    [InlineData(IpProtocols.Icmp, 8)]
    [InlineData(IpProtocols.Sctp, 12)]
    [InlineData(47, 0)]
    public void An_ipv4_datagram_counts_its_ip_header_its_transport_header_and_its_payload(byte protocol, int transportHeader) {
        // Arrange
        int anyPayloadLength = Any.Int32().Between(0, 1000).Generate();

        // Act
        Ipv4Packet packet = Any.Ipv4Packet().WithProtocol(protocol).WithPayload(new byte[anyPayloadLength]).Generate();

        // Assert
        Assert.Equal(20 + transportHeader + anyPayloadLength, packet.Length);
    }

    [Theory]
    [InlineData(IpProtocols.Tcp, 20)]
    [InlineData(IpProtocols.Udp, 8)]
    [InlineData(IpProtocols.Icmpv6, 8)]
    [InlineData(IpProtocols.Sctp, 12)]
    [InlineData(47, 0)]
    public void An_ipv6_datagram_counts_its_transport_header_and_its_payload_only(byte protocol, int transportHeader) {
        // Arrange
        int anyPayloadLength = Any.Int32().Between(0, 1000).Generate();

        // Act
        Ipv6Packet packet = Any.Ipv6Packet().WithProtocol(protocol).WithPayload(new byte[anyPayloadLength]).Generate();

        // Assert
        Assert.Equal(transportHeader + anyPayloadLength, packet.Length);
    }

    [Fact]
    public void The_payload_is_copied() {
        // Arrange
        byte[] original = Any.ArrayOf(Any.Byte()).WithCountBetween(1, 50).Generate();
        byte[] snapshot = (byte[])original.Clone();
        Packet packet   = Any.Ipv4Packet().WithPayload(original).Generate();

        // Act
        original[0] ^= 0xFF;

        // Assert
        Assert.Equal(snapshot, packet.Payload.ToArray());
    }

    [Fact]
    public void Null_arguments_are_refused() {
        // Arrange
        Ipv4Address anyAddress = AnyUnicastV4();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new Ipv4Packet(anyAddress, anyAddress, IpProtocols.Udp, 1, 1, TcpFlags.None, null!));
        Assert.Throws<ArgumentNullException>(() => new Ipv4Packet(null!, anyAddress, IpProtocols.Udp, 1, 1, TcpFlags.None, []));
        Assert.Throws<ArgumentNullException>(() => new Ipv4Packet(anyAddress, null!, IpProtocols.Udp, 1, 1, TcpFlags.None, []));
        Assert.Throws<ArgumentNullException>(() => new Ipv6Packet(null!, AnyUnicastV6(), IpProtocols.Udp, 1, 1, TcpFlags.None, []));
        Assert.Throws<ArgumentNullException>(() => new Ipv6Packet(AnyUnicastV6(), null!, IpProtocols.Udp, 1, 1, TcpFlags.None, []));
    }

    [Fact]
    public void The_reserved_protocol_is_out_of_range_on_both_families() {
        // Act
        ArgumentOutOfRangeException v4 = Assert.Throws<ArgumentOutOfRangeException>(() => V4(255, 0, 0, TcpFlags.None, []));
        ArgumentOutOfRangeException v6 = Assert.Throws<ArgumentOutOfRangeException>(() => V6(255, 0, 0, TcpFlags.None, []));

        // Assert
        Assert.Equal("protocol", v4.ParamName);
        Assert.Equal("protocol", v6.ParamName);
    }

    [Fact]
    public void Icmpv6_does_not_run_over_ipv4_and_icmp_does_not_run_over_ipv6() {
        // Act
        ArgumentException v4 = Assert.Throws<ArgumentException>(() => V4(IpProtocols.Icmpv6, 0, 0, TcpFlags.None, []));
        ArgumentException v6 = Assert.Throws<ArgumentException>(() => V6(IpProtocols.Icmp, 0, 0, TcpFlags.None, []));

        // Assert
        Assert.Equal("protocol", v4.ParamName);
        Assert.Equal("protocol", v6.ParamName);
    }

    [Theory]
    [InlineData(IpProtocols.Icmp)]
    [InlineData(47)]
    [InlineData(0)]
    public void A_protocol_without_ports_refuses_a_source_port(byte protocol) {
        // Arrange
        ushort anyPort = Any.UInt16().GreaterThan((ushort)0).Generate();

        // Act
        ArgumentException thrown = Assert.Throws<ArgumentException>(() => V4(protocol, anyPort, 0, TcpFlags.None, []));

        // Assert
        Assert.Equal("sourcePort", thrown.ParamName);
    }

    [Theory]
    [InlineData(IpProtocols.Icmp)]
    [InlineData(47)]
    [InlineData(0)]
    public void A_protocol_without_ports_refuses_a_destination_port_too(byte protocol) {
        // Arrange
        ushort anyPort = Any.UInt16().GreaterThan((ushort)0).Generate();

        // Act & Assert
        Assert.Throws<ArgumentException>(() => V4(protocol, 0, anyPort, TcpFlags.None, []));
    }

    [Theory]
    [InlineData(IpProtocols.Tcp)]
    [InlineData(IpProtocols.Udp)]
    [InlineData(IpProtocols.Sctp)]
    public void A_protocol_with_ports_takes_any_port_including_zero_and_the_last(byte protocol) {
        // Arrange
        TcpFlags flags = protocol == IpProtocols.Tcp ? TcpFlags.Ack : TcpFlags.None;
        ushort   anyPort = Any.UInt16().Generate();

        // Act
        Ipv4Packet extremes = V4(protocol, 0, ushort.MaxValue, flags, []);
        Ipv4Packet drawn    = V4(protocol, anyPort, anyPort, flags, []);

        // Assert
        Assert.Equal(0, extremes.SourcePort);
        Assert.Equal(ushort.MaxValue, extremes.DestinationPort);
        Assert.Equal(anyPort, drawn.SourcePort);
    }

    [Fact]
    public void A_tcp_segment_must_carry_a_valid_flag_combination() {
        // Act
        ArgumentException none = Assert.Throws<ArgumentException>(() => V4(IpProtocols.Tcp, 1, 1, TcpFlags.None, []));
        ArgumentException synFin = Assert.Throws<ArgumentException>(() => V4(IpProtocols.Tcp, 1, 1, TcpFlags.Syn | TcpFlags.Fin, []));
        ArgumentException synRst = Assert.Throws<ArgumentException>(() => V6(IpProtocols.Tcp, 1, 1, TcpFlags.Syn | TcpFlags.Rst, []));
        ArgumentException finRst = Assert.Throws<ArgumentException>(() => V6(IpProtocols.Tcp, 1, 1, TcpFlags.Fin | TcpFlags.Rst | TcpFlags.Ack, []));

        // Assert
        Assert.Equal("tcpFlags", none.ParamName);
        Assert.Equal("tcpFlags", synFin.ParamName);
        Assert.Equal("tcpFlags", synRst.ParamName);
        Assert.Equal("tcpFlags", finRst.ParamName);
    }

    [Fact]
    public void Any_valid_tcp_flag_set_is_accepted() {
        // Arrange
        TcpFlags anyValidFlags = Any.ElementOf(Support.ValidTcpFlags).Generate();

        // Act
        Ipv4Packet packet = Any.Ipv4Packet().WithProtocol(IpProtocols.Tcp).WithTcpFlags(anyValidFlags).Generate();

        // Assert
        Assert.Equal(anyValidFlags, packet.TcpFlags);
    }

    [Theory]
    [InlineData(IpProtocols.Udp)]
    [InlineData(IpProtocols.Icmp)]
    [InlineData(IpProtocols.Sctp)]
    [InlineData(47)]
    public void Only_tcp_carries_flags(byte protocol) {
        // Arrange
        TcpFlags anyFlag = Any.ElementOf(Support.ValidTcpFlags).Generate();

        // Act
        ArgumentException thrown = Assert.Throws<ArgumentException>(() => V4(protocol, 0, 0, anyFlag, []));

        // Assert
        Assert.Equal("tcpFlags", thrown.ParamName);
    }

    [Fact]
    public void The_checks_run_in_the_documented_order() {
        // Arrange: each packet breaks its own rule and every rule after it.
        byte[] oversized = new byte[Packet.MaxDatagramBytes];

        // Act
        string? reserved    = Assert.Throws<ArgumentOutOfRangeException>(() => V4(255, 1, 1, TcpFlags.Syn | TcpFlags.Fin, oversized)).ParamName;
        string? family      = Assert.Throws<ArgumentException>(() => V6(IpProtocols.Icmp, 1, 1, TcpFlags.Syn | TcpFlags.Fin, oversized)).ParamName;
        string? ports       = Assert.Throws<ArgumentException>(() => V4(47, 1, 1, TcpFlags.Syn | TcpFlags.Fin, oversized)).ParamName;
        string? flags       = Assert.Throws<ArgumentException>(() => V4(IpProtocols.Tcp, 1, 1, TcpFlags.Syn | TcpFlags.Fin, oversized)).ParamName;
        string? payloadSize = Assert.Throws<ArgumentOutOfRangeException>(() => V4(IpProtocols.Tcp, 1, 1, TcpFlags.Ack, oversized)).ParamName;

        // Assert
        Assert.Equal("protocol", reserved);
        Assert.Equal("protocol", family);
        Assert.Equal("sourcePort", ports);
        Assert.Equal("tcpFlags", flags);
        Assert.Equal("payload", payloadSize);
    }

    [Theory]
    [InlineData("224.0.0.0")]
    [InlineData("224.0.0.1")]
    [InlineData("239.255.255.255")]
    [InlineData("255.255.255.255")]
    public void An_ipv4_source_may_be_neither_multicast_nor_the_limited_broadcast(string text) {
        // Act
        ArgumentException thrown = Assert.Throws<ArgumentException>(() => new Ipv4Packet(Ipv4Address.Parse(text), AnyUnicastV4(), IpProtocols.Udp, 1, 1, TcpFlags.None, []));

        // Assert
        Assert.Equal("source", thrown.ParamName);
    }

    [Theory]
    [InlineData("223.255.255.255")]
    [InlineData("240.0.0.0")]
    [InlineData("255.255.255.254")]
    [InlineData("0.0.0.0")]
    public void Any_other_ipv4_source_is_accepted(string text) {
        // Act
        Ipv4Packet packet = new Ipv4Packet(Ipv4Address.Parse(text), AnyUnicastV4(), IpProtocols.Udp, 1, 1, TcpFlags.None, []);

        // Assert
        Assert.Equal(text, packet.Source.ToString());
    }

    [Fact]
    public void An_ipv4_destination_may_be_multicast_or_the_broadcast() {
        // Act
        Ipv4Packet multicast = Any.Ipv4Packet().WithDestination(Ipv4Address.Parse("224.0.0.251")).Generate();
        Ipv4Packet broadcast = Any.Ipv4Packet().WithDestination(Ipv4Address.Parse("255.255.255.255")).Generate();

        // Assert
        Assert.True(multicast.Destination.IsMulticast);
        Assert.True(broadcast.Destination.IsLimitedBroadcast);
    }

    [Theory]
    [InlineData("ff00::")]
    [InlineData("ff02::1")]
    [InlineData("ffff:ffff:ffff:ffff:ffff:ffff:ffff:ffff")]
    public void An_ipv6_source_may_not_be_multicast(string text) {
        // Act
        ArgumentException thrown = Assert.Throws<ArgumentException>(() => new Ipv6Packet(Ipv6Address.Parse(text), AnyUnicastV6(), IpProtocols.Udp, 1, 1, TcpFlags.None, []));

        // Assert
        Assert.Equal("source", thrown.ParamName);
    }

    [Fact]
    public void An_ipv6_destination_may_be_multicast_and_the_last_unicast_source_is_accepted() {
        // Act
        Ipv6Packet packet = new Ipv6Packet(Ipv6Address.Parse("feff:ffff:ffff:ffff:ffff:ffff:ffff:ffff"), Ipv6Address.Parse("ff02::1"), IpProtocols.Udp, 1, 1, TcpFlags.None, []);

        // Assert
        Assert.True(packet.Destination.IsMulticast);
    }

    [Fact]
    public void A_packet_reads_as_its_endpoints_and_protocol() {
        // Arrange
        Ipv4Packet v4 = Any.Ipv4Packet().WithSource(Ipv4Address.Parse("10.0.0.1")).WithDestination(Ipv4Address.Parse("10.0.0.2")).WithProtocol(IpProtocols.Udp).WithPorts(5353, 53).Generate();
        Ipv6Packet v6 = Any.Ipv6Packet().WithSource(Ipv6Address.Parse("2001:db8::1")).WithDestination(Ipv6Address.Parse("2001:db8::2")).WithProtocol(IpProtocols.Udp).WithPorts(5353, 53).Generate();

        // Assert
        Assert.Equal("10.0.0.1:5353 -> 10.0.0.2:53 proto 17", v4.ToString());
        Assert.Equal("[2001:db8::1]:5353 -> [2001:db8::2]:53 proto 17", v6.ToString());
    }

}
