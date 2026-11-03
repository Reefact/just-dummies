using JustDummies;

using NetworkAcl.Domain;

using Xunit;

namespace NetworkAcl.Tests.Regular;

public sealed class Ipv4AddressTests {

    [Fact]
    public void Parse_reads_the_octets_in_network_order() {
        // Act
        Ipv4Address address = Ipv4Address.Parse("192.0.2.1");

        // Assert
        Assert.Equal(0xC0000201u, address.Value);
    }

    [Theory]
    [InlineData("0.0.0.0", 0u)]
    [InlineData("255.255.255.255", uint.MaxValue)]
    [InlineData("10.0.0.0", 0x0A000000u)]
    [InlineData("1.20.3.40", 0x01140328u)]
    public void Parse_accepts_a_lone_zero_octet_and_the_extremes(string text, uint expected) {
        // Act
        Ipv4Address address = Ipv4Address.Parse(text);

        // Assert
        Assert.Equal(expected, address.Value);
    }

    [Fact]
    public void An_address_written_then_parsed_is_the_same_address() {
        // Arrange
        Ipv4Address anyAddress = Any.Ipv4Address().Generate();

        // Act
        Ipv4Address parsed = Ipv4Address.Parse(anyAddress.ToString());

        // Assert
        Assert.Equal(anyAddress, parsed);
        Assert.Equal(anyAddress.GetHashCode(), parsed.GetHashCode());
    }

    [Theory]
    [InlineData("")]
    [InlineData("1.2.3")]
    [InlineData("1.2.3.4.5")]
    [InlineData("1..3.4")]
    [InlineData(".2.3.4")]
    [InlineData("1.2.3.")]
    [InlineData("256.0.0.1")]
    [InlineData("1.2.3.999")]
    [InlineData("1.2.3.1000")]
    [InlineData("010.0.0.1")]
    [InlineData("1.2.3.04")]
    [InlineData("1.2.3.00")]
    [InlineData("a.b.c.d")]
    [InlineData("1.2.3.-4")]
    [InlineData("+1.2.3.4")]
    [InlineData(" 1.2.3.4")]
    [InlineData("1.2.3.4 ")]
    [InlineData("1,2,3,4")]
    [InlineData("0x1.2.3.4")]
    public void Parse_refuses_what_is_not_a_dotted_quad(string text) {
        // Act
        Exception? thrown = Record.Exception(() => Ipv4Address.Parse(text));

        // Assert
        Assert.IsType<FormatException>(thrown);
    }

    [Fact]
    public void Parse_refuses_null() {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => Ipv4Address.Parse(null!));
    }

    [Theory]
    [InlineData("223.255.255.255", false)]
    [InlineData("224.0.0.0", true)]
    [InlineData("224.0.0.1", true)]
    [InlineData("239.255.255.255", true)]
    [InlineData("240.0.0.0", false)]
    [InlineData("255.255.255.255", false)]
    [InlineData("0.0.0.0", false)]
    public void Multicast_is_224_0_0_0_slash_4(string text, bool expected) {
        // Act
        bool isMulticast = Ipv4Address.Parse(text).IsMulticast;

        // Assert
        Assert.Equal(expected, isMulticast);
    }

    [Fact]
    public void Any_address_of_the_multicast_block_is_multicast() {
        // Arrange
        Ipv4Address anyMulticast = Any.UInt32().Between(0xE0000000u, 0xEFFFFFFFu).As(value => new Ipv4Address(value)).Generate();

        // Assert
        Assert.True(anyMulticast.IsMulticast);
    }

    [Fact]
    public void Any_address_below_the_multicast_block_is_not_multicast() {
        // Arrange
        Ipv4Address anyUnicast = Any.UInt32().LessThan(0xE0000000u).As(value => new Ipv4Address(value)).Generate();

        // Assert
        Assert.False(anyUnicast.IsMulticast);
    }

    [Fact]
    public void Only_all_ones_is_the_limited_broadcast() {
        // Arrange
        Ipv4Address anyOtherAddress = Any.UInt32().LessThan(uint.MaxValue).As(value => new Ipv4Address(value)).Generate();

        // Assert
        Assert.True(new Ipv4Address(uint.MaxValue).IsLimitedBroadcast);
        Assert.False(anyOtherAddress.IsLimitedBroadcast);
    }

    [Fact]
    public void Addresses_are_ordered_by_value() {
        // Arrange
        uint        anyLow  = Any.UInt32().LessThan(uint.MaxValue).Generate();
        uint        anyHigh = Any.UInt32().GreaterThan(anyLow).Generate();
        Ipv4Address low     = new Ipv4Address(anyLow);
        Ipv4Address high    = new Ipv4Address(anyHigh);

        // Assert
        Assert.True(low.CompareTo(high) < 0);
        Assert.True(high.CompareTo(low) > 0);
        Assert.Equal(0, low.CompareTo(new Ipv4Address(anyLow)));
        Assert.True(low.CompareTo(null) > 0);
    }

    [Fact]
    public void Addresses_with_the_same_bits_are_equal() {
        // Arrange
        uint anyBits = Any.UInt32().Generate();

        // Assert
        Assert.Equal(new Ipv4Address(anyBits), new Ipv4Address(anyBits));
        Assert.False(new Ipv4Address(anyBits).Equals(null));
    }

    [Fact]
    public void ToString_writes_the_dotted_quad() {
        // Act & Assert
        Assert.Equal("203.0.113.7", new Ipv4Address(0xCB007107u).ToString());
        Assert.Equal("0.0.0.0", new Ipv4Address(0).ToString());
    }

}
