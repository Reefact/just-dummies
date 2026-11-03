using JustDummies;

using NetworkAcl.Domain;
using NetworkAcl.Tests.Expert.Arbitrary;

using Xunit;

using static NetworkAcl.Tests.Expert.Shorthand;

namespace NetworkAcl.Tests.Expert.Tests;

public sealed class Ipv4PacketTests {

    [Fact]
    public void A_packet_exposes_what_it_was_built_from() {
        // Arrange
        byte[] anyPayload = Any.ArrayOf(Any.Byte()).WithCountBetween(1, 64).Generate();

        // Act
        Ipv4Packet packet = new Ipv4Packet(V4("10.0.0.1"), V4("192.0.2.1"), IpProtocols.Tcp, 51000, 443, TcpFlags.Syn, anyPayload);

        // Assert
        Assert.Equal(IpVersion.V4, packet.Version);
        Assert.Equal(V4("10.0.0.1"), packet.Source);
        Assert.Equal(V4("192.0.2.1"), packet.Destination);
        Assert.Equal(IpProtocols.Tcp, packet.Protocol);
        Assert.Equal((ushort)51000, packet.SourcePort);
        Assert.Equal((ushort)443, packet.DestinationPort);
        Assert.Equal(TcpFlags.Syn, packet.TcpFlags);
    }

    [Fact]
    public void The_payload_is_copied_so_later_changes_to_the_array_do_not_reach_the_packet() {
        // Arrange
        byte[] payload = [1, 2, 3];
        Ipv4Packet packet = Any.Ipv4Packet().WithPayload(payload).Generate();

        // Act
        payload[0] = 99;

        // Assert
        Assert.Equal(new byte[] { 1, 2, 3 }, packet.Payload.ToArray());
    }

    [Fact]
    public void The_length_adds_the_ip_header_the_transport_header_and_the_payload() {
        // Arrange
        byte[] anyPayload = Any.ArrayOf(Any.Byte()).WithCount(100).Generate();
        Ipv4Packet tcp = Any.Ipv4Packet().WithProtocol(IpProtocols.Tcp).WithPayload(anyPayload).Generate();
        Ipv4Packet udp = Any.Ipv4Packet().WithProtocol(IpProtocols.Udp).WithPayload(anyPayload).Generate();

        // Act & Assert
        Assert.Equal(20 + 20 + 100, tcp.Length);
        Assert.Equal(20 + 8 + 100, udp.Length);
    }

    [Fact]
    public void An_empty_payload_is_accepted() {
        // Act
        Ipv4Packet packet = Any.Ipv4Packet().WithProtocol(IpProtocols.Udp).WithPayload([]).Generate();

        // Assert
        Assert.Equal(20 + 8, packet.Length);
    }

    [Fact]
    public void A_missing_payload_is_refused() {
        // Arrange
        Ipv4Address anySource = Any.Ipv4Address().WithValue(value => value.LessThan(0xE000_0000u)).Generate();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new Ipv4Packet(anySource, V4("10.0.0.1"), IpProtocols.Udp, 1, 2, TcpFlags.None, null!));
    }

    [Fact]
    public void A_missing_source_or_destination_is_refused() {
        // Arrange
        Ipv4Address anyAddress = Any.Ipv4Address().WithValue(value => value.LessThan(0xE000_0000u)).Generate();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new Ipv4Packet(null!, anyAddress, IpProtocols.Udp, 1, 2, TcpFlags.None, []));
        Assert.Throws<ArgumentNullException>(() => new Ipv4Packet(anyAddress, null!, IpProtocols.Udp, 1, 2, TcpFlags.None, []));
    }

    [Fact]
    public void The_reserved_protocol_255_is_refused() {
        // Arrange
        Ipv4Address anyAddress = Any.Ipv4Address().WithValue(value => value.LessThan(0xE000_0000u)).Generate();

        // Act
        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(() => new Ipv4Packet(anyAddress, anyAddress, IpProtocols.Reserved, 0, 0, TcpFlags.None, []));

        // Assert
        Assert.Equal("protocol", refusal.ParamName);
    }

    [Fact]
    public void Icmpv6_is_refused_over_ipv4() {
        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => Any.Ipv4Packet().WithProtocol(IpProtocols.Icmpv6).WithSourcePort(0).WithDestinationPort(0).WithTcpFlags(TcpFlags.None).Generate());

        // Assert
        Assert.Equal("protocol", refusal.ParamName);
    }

    [Theory]
    [InlineData(IpProtocols.Icmp)]
    [InlineData(47)]
    [InlineData(0)]
    public void A_protocol_without_ports_refuses_a_source_port(byte protocol) {
        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => Any.Ipv4Packet().WithProtocol(protocol).WithSourcePort(1).WithDestinationPort(0).Generate());

        // Assert
        Assert.Equal("sourcePort", refusal.ParamName);
    }

    [Theory]
    [InlineData(IpProtocols.Icmp)]
    [InlineData(47)]
    public void A_protocol_without_ports_refuses_a_destination_port(byte protocol) {
        Assert.Throws<ArgumentException>(() => Any.Ipv4Packet().WithProtocol(protocol).WithSourcePort(0).WithDestinationPort(1).Generate());
    }

    [Theory]
    [InlineData(IpProtocols.Tcp)]
    [InlineData(IpProtocols.Udp)]
    [InlineData(IpProtocols.Sctp)]
    public void A_protocol_with_ports_accepts_any_port_including_the_extremes(byte protocol) {
        // Act
        Ipv4Packet packet = Any.Ipv4Packet().WithProtocol(protocol).WithSourcePort(0).WithDestinationPort(ushort.MaxValue).Generate();

        // Assert
        Assert.Equal((ushort)0, packet.SourcePort);
        Assert.Equal(ushort.MaxValue, packet.DestinationPort);
    }

    [Theory]
    [InlineData(TcpFlags.None)]
    [InlineData(TcpFlags.Syn | TcpFlags.Fin)]
    [InlineData(TcpFlags.Syn | TcpFlags.Rst)]
    [InlineData(TcpFlags.Fin | TcpFlags.Rst)]
    [InlineData(TcpFlags.Syn | TcpFlags.Fin | TcpFlags.Ack)]
    public void A_tcp_segment_refuses_flags_that_no_valid_segment_carries(TcpFlags flags) {
        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => Any.Ipv4Packet().WithProtocol(IpProtocols.Tcp).WithTcpFlags(flags).Generate());

        // Assert
        Assert.Equal("tcpFlags", refusal.ParamName);
    }

    [Theory]
    [InlineData(IpProtocols.Udp)]
    [InlineData(IpProtocols.Icmp)]
    [InlineData(IpProtocols.Sctp)]
    [InlineData(47)]
    public void A_non_tcp_packet_refuses_any_tcp_flag(byte protocol) {
        // Arrange
        ushort anyPort = IpProtocols.CarriesPorts(protocol) ? Any.UInt16().Generate() : (ushort)0;

        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => Any.Ipv4Packet().WithProtocol(protocol).WithSourcePort(anyPort).WithDestinationPort(anyPort).WithTcpFlags(TcpFlags.Ack).Generate());

        // Assert
        Assert.Equal("tcpFlags", refusal.ParamName);
    }

    [Theory]
    [InlineData(IpProtocols.Tcp, 65_495)]
    [InlineData(IpProtocols.Udp, 65_507)]
    [InlineData(IpProtocols.Icmp, 65_507)]
    [InlineData(IpProtocols.Sctp, 65_503)]
    [InlineData(47, 65_515)]
    public void The_payload_fills_the_datagram_up_to_65535_bytes_headers_included(byte protocol, int maxPayload) {
        // Arrange
        byte[] anyPayloadAtTheLimit = Any.ArrayOf(Any.Byte()).WithCount(maxPayload).Generate();

        // Act
        Ipv4Packet packet = Any.Ipv4Packet().WithProtocol(protocol).WithSourcePort(0).WithDestinationPort(0).WithTcpFlags(protocol == IpProtocols.Tcp ? TcpFlags.Ack : TcpFlags.None).WithPayload(anyPayloadAtTheLimit).Generate();

        // Assert
        Assert.Equal(Packet.MaxDatagramBytes, packet.Length);
        Assert.Equal(maxPayload, Packet.MaxPayloadBytes(IpVersion.V4, protocol));
    }

    [Theory]
    [InlineData(IpProtocols.Tcp, 65_495)]
    [InlineData(IpProtocols.Udp, 65_507)]
    [InlineData(IpProtocols.Icmp, 65_507)]
    [InlineData(IpProtocols.Sctp, 65_503)]
    [InlineData(47, 65_515)]
    public void One_payload_byte_more_than_the_limit_is_refused(byte protocol, int maxPayload) {
        // Arrange
        byte[] anyPayloadOverTheLimit = Any.ArrayOf(Any.Byte()).WithCount(maxPayload + 1).Generate();
        AnyIpv4Packet generator = Any.Ipv4Packet().WithProtocol(protocol).WithSourcePort(0).WithDestinationPort(0).WithTcpFlags(protocol == IpProtocols.Tcp ? TcpFlags.Ack : TcpFlags.None).WithPayload(anyPayloadOverTheLimit);

        // Act
        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(() => generator.Generate());

        // Assert
        Assert.Equal("payload", refusal.ParamName);
    }

    [Theory]
    [InlineData(0xE0000000u)]   // 224.0.0.0
    [InlineData(0xEFFFFFFFu)]   // 239.255.255.255
    [InlineData(0xFFFFFFFFu)]   // 255.255.255.255
    public void A_multicast_or_broadcast_source_is_refused(uint sourceBits) {
        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => Any.Ipv4Packet().WithSource(new Ipv4Address(sourceBits)).Generate());

        // Assert
        Assert.Equal("source", refusal.ParamName);
    }

    [Theory]
    [InlineData(0xDFFFFFFFu)]   // 223.255.255.255, the last unicast-range address
    [InlineData(0xF0000000u)]   // 240.0.0.0, reserved but not multicast
    [InlineData(0xFFFFFFFEu)]
    [InlineData(0x00000000u)]
    public void Other_addresses_may_be_a_source(uint sourceBits) {
        // Act
        Ipv4Packet packet = Any.Ipv4Packet().WithSource(new Ipv4Address(sourceBits)).Generate();

        // Assert
        Assert.Equal(sourceBits, packet.Source.Value);
    }

    [Theory]
    [InlineData(0xE0000001u)]
    [InlineData(0xFFFFFFFFu)]
    public void A_multicast_or_broadcast_destination_is_accepted(uint destinationBits) {
        // Act
        Ipv4Packet packet = Any.Ipv4Packet().WithDestination(new Ipv4Address(destinationBits)).Generate();

        // Assert
        Assert.Equal(destinationBits, packet.Destination.Value);
    }

    [Fact]
    public void The_protocol_is_checked_before_the_ports_before_the_flags_before_the_size() {
        // Arrange
        Ipv4Address anySource = Any.Ipv4Address().WithValue(value => value.LessThan(0xE000_0000u)).Generate();
        byte[] anyOversizedPayload = Any.ArrayOf(Any.Byte()).WithCount(Packet.MaxDatagramBytes).Generate();

        // Act: each packet breaks the rule named and every rule after it
        ArgumentException wrongFamily = Assert.Throws<ArgumentException>(() => new Ipv4Packet(anySource, anySource, IpProtocols.Icmpv6, 1, 1, TcpFlags.Syn, anyOversizedPayload));
        ArgumentException portsFirst  = Assert.Throws<ArgumentException>(() => new Ipv4Packet(anySource, anySource, IpProtocols.Icmp, 1, 1, TcpFlags.Syn, anyOversizedPayload));
        ArgumentException flagsFirst  = Assert.Throws<ArgumentException>(() => new Ipv4Packet(anySource, anySource, IpProtocols.Udp, 1, 1, TcpFlags.Syn, anyOversizedPayload));

        // Assert
        Assert.Equal("protocol", wrongFamily.ParamName);
        Assert.Equal("sourcePort", portsFirst.ParamName);
        Assert.Equal("tcpFlags", flagsFirst.ParamName);
    }

    [Fact]
    public void ToString_shows_both_endpoints_and_the_protocol() {
        // Arrange
        Ipv4Packet packet = Any.Ipv4Packet().WithSource(V4("10.0.0.1")).WithDestination(V4("192.0.2.1")).WithProtocol(IpProtocols.Tcp).WithSourcePort(51000).WithDestinationPort(443).Generate();

        // Act & Assert
        Assert.Equal("10.0.0.1:51000 -> 192.0.2.1:443 proto 6", packet.ToString());
    }

}

public sealed class Ipv6PacketTests {

    [Fact]
    public void A_packet_exposes_what_it_was_built_from() {
        // Act
        Ipv6Packet packet = Any.Ipv6Packet().WithSource(V6("2001:db8::1")).WithDestination(V6("2001:db8::2")).WithProtocol(IpProtocols.Udp).WithSourcePort(5353).WithDestinationPort(53).WithTcpFlags(TcpFlags.None).Generate();

        // Assert
        Assert.Equal(IpVersion.V6, packet.Version);
        Assert.Equal(V6("2001:db8::1"), packet.Source);
        Assert.Equal(V6("2001:db8::2"), packet.Destination);
        Assert.Equal(IpProtocols.Udp, packet.Protocol);
        Assert.Equal((ushort)5353, packet.SourcePort);
        Assert.Equal((ushort)53, packet.DestinationPort);
    }

    [Fact]
    public void The_length_excludes_the_fixed_ipv6_header() {
        // Arrange
        byte[] anyPayload = Any.ArrayOf(Any.Byte()).WithCount(100).Generate();

        // Act
        Ipv6Packet tcp = Any.Ipv6Packet().WithProtocol(IpProtocols.Tcp).WithPayload(anyPayload).Generate();
        Ipv6Packet icmpv6 = Any.Ipv6Packet().WithProtocol(IpProtocols.Icmpv6).WithPayload(anyPayload).Generate();

        // Assert
        Assert.Equal(20 + 100, tcp.Length);
        Assert.Equal(8 + 100, icmpv6.Length);
    }

    [Fact]
    public void Icmp_is_refused_over_ipv6() {
        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => Any.Ipv6Packet().WithProtocol(IpProtocols.Icmp).WithSourcePort(0).WithDestinationPort(0).WithTcpFlags(TcpFlags.None).Generate());

        // Assert
        Assert.Equal("protocol", refusal.ParamName);
    }

    [Fact]
    public void The_reserved_protocol_255_is_refused() {
        Assert.Throws<ArgumentOutOfRangeException>(() => Any.Ipv6Packet().WithProtocol(IpProtocols.Reserved).WithSourcePort(0).WithDestinationPort(0).WithTcpFlags(TcpFlags.None).Generate());
    }

    [Theory]
    [InlineData(IpProtocols.Tcp, 65_515)]
    [InlineData(IpProtocols.Udp, 65_527)]
    [InlineData(IpProtocols.Icmpv6, 65_527)]
    [InlineData(IpProtocols.Sctp, 65_523)]
    [InlineData(47, 65_535)]
    public void The_payload_fills_the_ipv6_payload_length_up_to_65535_bytes_transport_header_included(byte protocol, int maxPayload) {
        // Arrange
        byte[] anyPayloadAtTheLimit = Any.ArrayOf(Any.Byte()).WithCount(maxPayload).Generate();

        // Act
        Ipv6Packet packet = Any.Ipv6Packet().WithProtocol(protocol).WithSourcePort(0).WithDestinationPort(0).WithTcpFlags(protocol == IpProtocols.Tcp ? TcpFlags.Ack : TcpFlags.None).WithPayload(anyPayloadAtTheLimit).Generate();

        // Assert
        Assert.Equal(Packet.MaxDatagramBytes, packet.Length);
        Assert.Equal(maxPayload, Packet.MaxPayloadBytes(IpVersion.V6, protocol));
    }

    [Theory]
    [InlineData(IpProtocols.Tcp, 65_515)]
    [InlineData(IpProtocols.Udp, 65_527)]
    [InlineData(IpProtocols.Icmpv6, 65_527)]
    [InlineData(IpProtocols.Sctp, 65_523)]
    [InlineData(47, 65_535)]
    public void One_payload_byte_more_than_the_limit_is_refused(byte protocol, int maxPayload) {
        // Arrange
        byte[] anyPayloadOverTheLimit = Any.ArrayOf(Any.Byte()).WithCount(maxPayload + 1).Generate();
        AnyIpv6Packet generator = Any.Ipv6Packet().WithProtocol(protocol).WithSourcePort(0).WithDestinationPort(0).WithTcpFlags(protocol == IpProtocols.Tcp ? TcpFlags.Ack : TcpFlags.None).WithPayload(anyPayloadOverTheLimit);

        // Act
        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(() => generator.Generate());

        // Assert
        Assert.Equal("payload", refusal.ParamName);
    }

    [Fact]
    public void A_multicast_source_is_refused() {
        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => Any.Ipv6Packet().WithSource(V6("ff02::1")).Generate());

        // Assert
        Assert.Equal("source", refusal.ParamName);
    }

    [Theory]
    [InlineData("feff:ffff:ffff:ffff:ffff:ffff:ffff:ffff")]
    [InlineData("::")]
    [InlineData("fe80::1")]
    public void Other_addresses_may_be_a_source(string source) {
        // Act
        Ipv6Packet packet = Any.Ipv6Packet().WithSource(V6(source)).Generate();

        // Assert
        Assert.Equal(V6(source), packet.Source);
    }

    [Fact]
    public void A_multicast_destination_is_accepted() {
        // Act
        Ipv6Packet packet = Any.Ipv6Packet().WithDestination(V6("ff02::1")).Generate();

        // Assert
        Assert.Equal(V6("ff02::1"), packet.Destination);
    }

    [Fact]
    public void A_protocol_without_ports_refuses_ports_and_a_tcp_segment_refuses_empty_flags() {
        Assert.Throws<ArgumentException>(() => Any.Ipv6Packet().WithProtocol(IpProtocols.Icmpv6).WithSourcePort(7).WithDestinationPort(0).Generate());
        Assert.Throws<ArgumentException>(() => Any.Ipv6Packet().WithProtocol(IpProtocols.Tcp).WithTcpFlags(TcpFlags.None).Generate());
        Assert.Throws<ArgumentException>(() => Any.Ipv6Packet().WithProtocol(IpProtocols.Udp).WithTcpFlags(TcpFlags.Ack).Generate());
    }

    [Fact]
    public void ToString_brackets_the_addresses() {
        // Arrange
        Ipv6Packet packet = Any.Ipv6Packet().WithSource(V6("::1")).WithDestination(V6("2001:db8::1")).WithProtocol(IpProtocols.Tcp).WithSourcePort(51000).WithDestinationPort(443).Generate();

        // Act & Assert
        Assert.Equal("[::1]:51000 -> [2001:db8::1]:443 proto 6", packet.ToString());
    }

}
