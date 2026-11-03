using NetworkAcl.Domain;

namespace NetworkAcl.Tests.Discovery;

public sealed class IpProtocolsTests {

    [Fact]
    public void The_constants_are_the_iana_numbers() {
        Assert.Equal(1, IpProtocols.Icmp);
        Assert.Equal(6, IpProtocols.Tcp);
        Assert.Equal(17, IpProtocols.Udp);
        Assert.Equal(58, IpProtocols.Icmpv6);
        Assert.Equal(132, IpProtocols.Sctp);
        Assert.Equal(255, IpProtocols.Reserved);
    }

    [Theory]
    [InlineData(6, true)]
    [InlineData(17, true)]
    [InlineData(132, true)]
    [InlineData(1, false)]
    [InlineData(58, false)]
    [InlineData(47, false)]
    [InlineData(0, false)]
    [InlineData(255, false)]
    public void Only_tcp_udp_and_sctp_carry_ports(byte protocol, bool expected) {
        Assert.Equal(expected, IpProtocols.CarriesPorts(protocol));
    }

    [Theory]
    [InlineData(6, 20)]
    [InlineData(17, 8)]
    [InlineData(1, 8)]
    [InlineData(58, 8)]
    [InlineData(132, 12)]
    [InlineData(47, 0)]
    [InlineData(0, 0)]
    [InlineData(255, 0)]
    public void TransportHeaderBytes_is_the_fixed_header_of_the_protocols_modelled(byte protocol, int expected) {
        Assert.Equal(expected, IpProtocols.TransportHeaderBytes(protocol));
    }

    [Theory]
    [InlineData(1, IpVersion.V4, true)]
    [InlineData(1, IpVersion.V6, false)]
    [InlineData(58, IpVersion.V4, false)]
    [InlineData(58, IpVersion.V6, true)]
    [InlineData(6, IpVersion.V4, true)]
    [InlineData(6, IpVersion.V6, true)]
    [InlineData(17, IpVersion.V4, true)]
    [InlineData(17, IpVersion.V6, true)]
    [InlineData(132, IpVersion.V6, true)]
    [InlineData(47, IpVersion.V4, true)]
    [InlineData(47, IpVersion.V6, true)]
    public void Icmp_belongs_to_ipv4_icmpv6_to_ipv6_and_the_rest_to_both(byte protocol, IpVersion version, bool expected) {
        Assert.Equal(expected, IpProtocols.IsAllowedOn(protocol, version));
    }

}
