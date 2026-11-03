using NetworkAcl.Domain;

using Xunit;

namespace NetworkAcl.Tests.Regular;

public sealed class IpProtocolsTests {

    [Fact]
    public void The_constants_are_the_iana_numbers() {
        // Assert
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
        // Act & Assert
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
    public void The_transport_header_length_depends_on_the_protocol(byte protocol, int expected) {
        // Act & Assert
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
    public void Icmp_is_ipv4_only_icmpv6_is_ipv6_only_and_the_rest_runs_over_both(byte protocol, IpVersion version, bool expected) {
        // Act & Assert
        Assert.Equal(expected, IpProtocols.IsAllowedOn(protocol, version));
    }

}
