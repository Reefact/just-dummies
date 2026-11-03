using NetworkAcl.Domain;

namespace NetworkAcl.Tests.Discovery;

public sealed class PacketTests {

    // ---- the limits

    [Theory]
    [InlineData(IpProtocols.Tcp, 65495)]
    [InlineData(IpProtocols.Udp, 65507)]
    [InlineData(IpProtocols.Icmp, 65507)]
    [InlineData(IpProtocols.Sctp, 65503)]
    [InlineData((byte)47, 65515)]
    public void MaxPayloadBytes_on_ipv4_removes_the_ip_header_and_the_transport_header(byte protocol, int expected) {
        Assert.Equal(expected, Packet.MaxPayloadBytes(IpVersion.V4, protocol));
    }

    [Theory]
    [InlineData(IpProtocols.Tcp, 65515)]
    [InlineData(IpProtocols.Udp, 65527)]
    [InlineData(IpProtocols.Icmpv6, 65527)]
    [InlineData(IpProtocols.Sctp, 65523)]
    [InlineData((byte)47, 65535)]
    public void MaxPayloadBytes_on_ipv6_removes_only_the_transport_header(byte protocol, int expected) {
        Assert.Equal(expected, Packet.MaxPayloadBytes(IpVersion.V6, protocol));
    }

    [Theory]
    [InlineData(IpProtocols.Tcp, 65495)]
    [InlineData(IpProtocols.Udp, 65507)]
    [InlineData(IpProtocols.Icmp, 65507)]
    [InlineData(IpProtocols.Sctp, 65503)]
    [InlineData((byte)47, 65515)]
    public void An_ipv4_packet_of_the_maximum_payload_is_accepted_and_one_byte_more_is_not(byte protocol, int max) {
        // Arrange
        Ipv4Packet atTheLimit = Any.Ipv4Packet().WithProtocol(protocol).WithPayload(new byte[max]).Generate();

        // Assert
        Assert.Equal(65535, atTheLimit.Length);
        Assert.Throws<ArgumentOutOfRangeException>(() => Any.Ipv4Packet().WithProtocol(protocol).WithPayload(new byte[max + 1]).Generate());
    }

    [Theory]
    [InlineData(IpProtocols.Tcp, 65515)]
    [InlineData(IpProtocols.Udp, 65527)]
    [InlineData(IpProtocols.Icmpv6, 65527)]
    [InlineData(IpProtocols.Sctp, 65523)]
    [InlineData((byte)47, 65535)]
    public void An_ipv6_packet_of_the_maximum_payload_is_accepted_and_one_byte_more_is_not(byte protocol, int max) {
        Ipv6Packet atTheLimit = Any.Ipv6Packet().WithProtocol(protocol).WithPayload(new byte[max]).Generate();

        Assert.Equal(max + IpProtocols.TransportHeaderBytes(protocol), atTheLimit.Length);
        Assert.Throws<ArgumentOutOfRangeException>(() => Any.Ipv6Packet().WithProtocol(protocol).WithPayload(new byte[max + 1]).Generate());
    }

    // ---- common rules

    [Fact]
    public void A_null_payload_is_refused() {
        Assert.Throws<ArgumentNullException>(() => new Ipv4Packet(Ipv4Address.Parse("10.0.0.1"), Ipv4Address.Parse("10.0.0.2"), IpProtocols.Udp, 1, 2, TcpFlags.None, null!));
        Assert.Throws<ArgumentNullException>(() => new Ipv6Packet(Ipv6Address.Parse("::1"), Ipv6Address.Parse("::2"), IpProtocols.Udp, 1, 2, TcpFlags.None, null!));
    }

    [Fact]
    public void The_reserved_protocol_255_is_out_of_range() {
        Assert.Throws<ArgumentOutOfRangeException>(() => Any.Ipv4Packet().WithProtocol(IpProtocols.Reserved).Generate());
        Assert.Throws<ArgumentOutOfRangeException>(() => Any.Ipv6Packet().WithProtocol(IpProtocols.Reserved).Generate());
    }

    [Fact]
    public void Icmp_does_not_run_over_ipv6() {
        Assert.Throws<ArgumentException>(() => Any.Ipv6Packet().WithProtocol(IpProtocols.Icmp).Generate());
    }

    [Fact]
    public void Icmpv6_does_not_run_over_ipv4() {
        Assert.Throws<ArgumentException>(() => Any.Ipv4Packet().WithProtocol(IpProtocols.Icmpv6).Generate());
    }

    [Theory]
    [InlineData(IpProtocols.Icmp, 1, 0)]
    [InlineData(IpProtocols.Icmp, 0, 1)]
    [InlineData((byte)47, 80, 0)]
    [InlineData((byte)47, 0, 80)]
    [InlineData((byte)0, 65535, 65535)]
    public void A_protocol_without_ports_has_both_ports_at_zero(byte protocol, ushort sourcePort, ushort destinationPort) {
        Assert.Throws<ArgumentException>(() => Any.Ipv4Packet().WithProtocol(protocol).WithSourcePort(sourcePort).WithDestinationPort(destinationPort).Generate());
    }

    [Fact]
    public void A_protocol_without_ports_is_accepted_with_both_at_zero() {
        Ipv4Packet ping = Any.Ipv4Packet().WithProtocol(IpProtocols.Icmp).WithSourcePort(0).WithDestinationPort(0).Generate();

        Assert.Equal(0, ping.SourcePort);
        Assert.Equal(0, ping.DestinationPort);
    }

    [Theory]
    [InlineData(IpProtocols.Tcp)]
    [InlineData(IpProtocols.Udp)]
    [InlineData(IpProtocols.Sctp)]
    public void A_protocol_with_ports_keeps_them_even_at_the_ends_of_the_range(byte protocol) {
        Ipv4Packet packet = Any.Ipv4Packet().WithProtocol(protocol).WithSourcePort(0).WithDestinationPort(65535).Generate();

        Assert.Equal(0, packet.SourcePort);
        Assert.Equal(65535, packet.DestinationPort);
    }

    [Theory]
    [InlineData(TcpFlags.None)]
    [InlineData(TcpFlags.Syn | TcpFlags.Fin)]
    [InlineData(TcpFlags.Syn | TcpFlags.Rst)]
    [InlineData(TcpFlags.Fin | TcpFlags.Rst)]
    [InlineData((TcpFlags)0xFF)]
    public void A_tcp_segment_must_carry_a_valid_flag_combination(TcpFlags flags) {
        Assert.Throws<ArgumentException>(() => Any.Ipv4Packet().WithProtocol(IpProtocols.Tcp).WithTcpFlags(flags).Generate());
        Assert.Throws<ArgumentException>(() => Any.Ipv6Packet().WithProtocol(IpProtocols.Tcp).WithTcpFlags(flags).Generate());
    }

    [Theory]
    [InlineData(TcpFlags.Syn)]
    [InlineData(TcpFlags.Syn | TcpFlags.Ack)]
    [InlineData(TcpFlags.Fin | TcpFlags.Ack)]
    [InlineData(TcpFlags.Rst | TcpFlags.Ack)]
    [InlineData(TcpFlags.Psh | TcpFlags.Urg | TcpFlags.Ece | TcpFlags.Cwr)]
    public void A_tcp_segment_keeps_a_valid_combination(TcpFlags flags) {
        Ipv4Packet packet = Any.Ipv4Packet().WithProtocol(IpProtocols.Tcp).WithTcpFlags(flags).Generate();

        Assert.Equal(flags, packet.TcpFlags);
    }

    [Theory]
    [InlineData(IpProtocols.Udp)]
    [InlineData(IpProtocols.Sctp)]
    [InlineData(IpProtocols.Icmp)]
    [InlineData((byte)47)]
    public void Only_tcp_carries_flags(byte protocol) {
        Assert.Throws<ArgumentException>(() => Any.Ipv4Packet().WithProtocol(protocol).WithTcpFlags(TcpFlags.Ack).Generate());
    }

    [Fact]
    public void A_segment_of_another_protocol_carries_no_flags() {
        Ipv4Packet datagram = Any.Ipv4Packet().WithProtocol(IpProtocols.Udp).Generate();

        Assert.Equal(TcpFlags.None, datagram.TcpFlags);
    }

    // ---- sources

    [Theory]
    [InlineData("224.0.0.1")]
    [InlineData("239.255.255.255")]
    [InlineData("224.0.0.0")]
    [InlineData("255.255.255.255")]
    public void An_ipv4_source_may_be_neither_multicast_nor_the_limited_broadcast(string source) {
        Assert.Throws<ArgumentException>(() => Any.Ipv4Packet().WithSource(Ipv4Address.Parse(source)).Generate());
    }

    [Theory]
    [InlineData("223.255.255.255")]
    [InlineData("240.0.0.0")]
    [InlineData("0.0.0.0")]
    [InlineData("255.255.255.254")]
    public void The_neighbours_of_the_multicast_and_broadcast_ranges_are_valid_sources(string source) {
        Ipv4Packet packet = Any.Ipv4Packet().WithSource(Ipv4Address.Parse(source)).Generate();

        Assert.Equal(Ipv4Address.Parse(source), packet.Source);
    }

    [Fact]
    public void An_ipv4_destination_may_be_multicast_or_broadcast() {
        Ipv4Packet toGroup     = Any.Ipv4Packet().WithDestination(Ipv4Address.Parse("224.0.0.251")).Generate();
        Ipv4Packet toBroadcast = Any.Ipv4Packet().WithDestination(Ipv4Address.Parse("255.255.255.255")).Generate();

        Assert.Equal(Ipv4Address.Parse("224.0.0.251"), toGroup.Destination);
        Assert.Equal(Ipv4Address.Parse("255.255.255.255"), toBroadcast.Destination);
    }

    [Theory]
    [InlineData("ff00::")]
    [InlineData("ff02::1")]
    [InlineData("ffff:ffff:ffff:ffff:ffff:ffff:ffff:ffff")]
    public void An_ipv6_source_may_not_be_multicast(string source) {
        Assert.Throws<ArgumentException>(() => Any.Ipv6Packet().WithSource(Ipv6Address.Parse(source)).Generate());
    }

    [Fact]
    public void An_ipv6_destination_may_be_multicast() {
        Ipv6Packet packet = Any.Ipv6Packet().WithDestination(Ipv6Address.Parse("ff02::1")).Generate();

        Assert.Equal(Ipv6Address.Parse("ff02::1"), packet.Destination);
    }

    [Fact]
    public void A_null_address_is_refused() {
        Assert.Throws<ArgumentNullException>(() => new Ipv4Packet(null!, Ipv4Address.Parse("10.0.0.2"), IpProtocols.Udp, 1, 2, TcpFlags.None, []));
        Assert.Throws<ArgumentNullException>(() => new Ipv4Packet(Ipv4Address.Parse("10.0.0.1"), null!, IpProtocols.Udp, 1, 2, TcpFlags.None, []));
        Assert.Throws<ArgumentNullException>(() => new Ipv6Packet(null!, Ipv6Address.Parse("::2"), IpProtocols.Udp, 1, 2, TcpFlags.None, []));
        Assert.Throws<ArgumentNullException>(() => new Ipv6Packet(Ipv6Address.Parse("::1"), null!, IpProtocols.Udp, 1, 2, TcpFlags.None, []));
    }

    // ---- what a packet reports

    [Fact]
    public void An_ipv4_packet_reports_its_family_and_fields() {
        Ipv4Packet packet = new Ipv4Packet(Ipv4Address.Parse("10.0.0.1"), Ipv4Address.Parse("192.0.2.9"), IpProtocols.Tcp, 40000, 443, TcpFlags.Syn, [1, 2, 3]);

        Assert.Equal(IpVersion.V4, packet.Version);
        Assert.Equal(Ipv4Address.Parse("10.0.0.1"), packet.Source);
        Assert.Equal(Ipv4Address.Parse("192.0.2.9"), packet.Destination);
        Assert.Equal(IpProtocols.Tcp, packet.Protocol);
        Assert.Equal(40000, packet.SourcePort);
        Assert.Equal(443, packet.DestinationPort);
        Assert.Equal(TcpFlags.Syn, packet.TcpFlags);
        Assert.Equal(new byte[] { 1, 2, 3 }, packet.Payload.ToArray());
    }

    [Fact]
    public void An_ipv6_packet_reports_its_family() {
        Ipv6Packet packet = Any.Ipv6Packet().Generate();

        Assert.Equal(IpVersion.V6, packet.Version);
    }

    [Theory]
    [InlineData(IpProtocols.Tcp, 0, 40)]
    [InlineData(IpProtocols.Tcp, 100, 140)]
    [InlineData(IpProtocols.Udp, 100, 128)]
    [InlineData(IpProtocols.Icmp, 56, 84)]
    [InlineData((byte)47, 10, 30)]
    public void An_ipv4_length_counts_ip_header_transport_header_and_payload(byte protocol, int payloadBytes, int expected) {
        Ipv4Packet packet = Any.Ipv4Packet().WithProtocol(protocol).WithPayload(new byte[payloadBytes]).Generate();

        Assert.Equal(expected, packet.Length);
    }

    [Theory]
    [InlineData(IpProtocols.Tcp, 0, 20)]
    [InlineData(IpProtocols.Tcp, 100, 120)]
    [InlineData(IpProtocols.Udp, 100, 108)]
    [InlineData(IpProtocols.Icmpv6, 56, 64)]
    [InlineData((byte)47, 10, 10)]
    public void An_ipv6_length_counts_the_payload_length_field_that_excludes_the_fixed_header(byte protocol, int payloadBytes, int expected) {
        Ipv6Packet packet = Any.Ipv6Packet().WithProtocol(protocol).WithPayload(new byte[payloadBytes]).Generate();

        Assert.Equal(expected, packet.Length);
    }

    [Fact]
    public void The_payload_is_copied_on_the_way_in() {
        // Arrange
        byte[] payload = [1, 2, 3];
        Ipv4Packet packet = Any.Ipv4Packet().WithPayload(payload).Generate();

        // Act: the caller keeps and changes its own array
        payload[0] = 99;

        // Assert
        Assert.Equal(new byte[] { 1, 2, 3 }, packet.Payload.ToArray());
    }

    [Fact]
    public void ToString_writes_endpoints_and_protocol() {
        Ipv4Packet v4 = new Ipv4Packet(Ipv4Address.Parse("10.0.0.1"), Ipv4Address.Parse("192.0.2.9"), IpProtocols.Tcp, 40000, 443, TcpFlags.Syn, []);
        Ipv6Packet v6 = new Ipv6Packet(Ipv6Address.Parse("2001:db8::1"), Ipv6Address.Parse("2001:db8::2"), IpProtocols.Udp, 5353, 53, TcpFlags.None, []);

        Assert.Equal("10.0.0.1:40000 -> 192.0.2.9:443 proto 6", v4.ToString());
        Assert.Equal("[2001:db8::1]:5353 -> [2001:db8::2]:53 proto 17", v6.ToString());
    }

}
