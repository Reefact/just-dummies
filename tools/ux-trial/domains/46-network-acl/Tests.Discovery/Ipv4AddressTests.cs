using NetworkAcl.Domain;

namespace NetworkAcl.Tests.Discovery;

public sealed class Ipv4AddressTests {

    [Theory]
    [InlineData("0.0.0.0", 0u)]
    [InlineData("255.255.255.255", 4294967295u)]
    [InlineData("192.0.2.1", 3221225985u)]
    [InlineData("10.0.0.0", 167772160u)]
    [InlineData("1.2.3.4", 16909060u)]
    [InlineData("0.0.0.1", 1u)]
    [InlineData("127.0.0.1", 2130706433u)]
    public void Parse_reads_four_decimal_octets_in_network_order(string text, uint expected) {
        // Act
        Ipv4Address address = Ipv4Address.Parse(text);

        // Assert
        Assert.Equal(expected, address.Value);
    }

    [Theory]
    [InlineData("")]
    [InlineData("1.2.3")]
    [InlineData("1.2.3.4.5")]
    [InlineData("256.0.0.1")]
    [InlineData("1.2.3.256")]
    [InlineData("1..2.3")]
    [InlineData(".1.2.3")]
    [InlineData("1.2.3.")]
    [InlineData("a.b.c.d")]
    [InlineData("1.2.3.x")]
    [InlineData("-1.2.3.4")]
    [InlineData("+1.2.3.4")]
    [InlineData(" 1.2.3.4")]
    [InlineData("1.2.3.4 ")]
    [InlineData("1000.2.3.4")]
    [InlineData("1,2,3,4")]
    public void Parse_refuses_anything_but_four_decimal_octets(string text) {
        Assert.Throws<FormatException>(() => Ipv4Address.Parse(text));
    }

    [Theory]
    [InlineData("010.0.0.1")]
    [InlineData("1.02.3.4")]
    [InlineData("1.2.3.04")]
    [InlineData("00.0.0.0")]
    [InlineData("0.0.0.00")]
    public void Parse_refuses_a_leading_zero_because_other_parsers_read_it_as_octal(string text) {
        Assert.Throws<FormatException>(() => Ipv4Address.Parse(text));
    }

    [Fact]
    public void Parse_accepts_a_lone_zero_octet() {
        Ipv4Address address = Ipv4Address.Parse("10.0.0.0");

        Assert.Equal("10.0.0.0", address.ToString());
    }

    [Fact]
    public void Parse_refuses_null() {
        Assert.Throws<ArgumentNullException>(() => Ipv4Address.Parse(null!));
    }

    [Theory]
    [InlineData(0u, "0.0.0.0")]
    [InlineData(4294967295u, "255.255.255.255")]
    [InlineData(3221225985u, "192.0.2.1")]
    [InlineData(16909060u, "1.2.3.4")]
    public void ToString_writes_a_dotted_quad(uint value, string expected) {
        Assert.Equal(expected, new Ipv4Address(value).ToString());
    }

    [Theory]
    [InlineData("223.255.255.255", false)]
    [InlineData("224.0.0.0", true)]
    [InlineData("224.0.0.1", true)]
    [InlineData("239.255.255.255", true)]
    [InlineData("240.0.0.0", false)]
    [InlineData("0.0.0.0", false)]
    [InlineData("10.0.0.1", false)]
    [InlineData("255.255.255.255", false)]
    public void IsMulticast_covers_224_0_0_0_slash_4_exactly(string text, bool expected) {
        Assert.Equal(expected, Ipv4Address.Parse(text).IsMulticast);
    }

    [Theory]
    [InlineData("255.255.255.255", true)]
    [InlineData("255.255.255.254", false)]
    [InlineData("0.0.0.0", false)]
    public void IsLimitedBroadcast_is_only_the_all_ones_address(string text, bool expected) {
        Assert.Equal(expected, Ipv4Address.Parse(text).IsLimitedBroadcast);
    }

    [Fact]
    public void Two_addresses_with_the_same_bits_are_equal_and_hash_alike() {
        // Arrange
        uint anyBits = Any.UInt32().Generate();

        // Act
        Ipv4Address first  = new Ipv4Address(anyBits);
        Ipv4Address second = new Ipv4Address(anyBits);

        // Assert
        Assert.Equal(first, second);
        Assert.Equal(first.GetHashCode(), second.GetHashCode());
        Assert.True(first.Equals((object)second));
    }

    [Fact]
    public void Addresses_with_different_bits_are_not_equal() {
        Assert.NotEqual(Ipv4Address.Parse("10.0.0.1"), Ipv4Address.Parse("10.0.0.2"));
    }

    [Fact]
    public void An_address_is_not_equal_to_null_or_to_another_kind_of_object() {
        // Arrange
        Ipv4Address anyAddress = Any.Ipv4Address().Generate();

        // Assert
        Assert.False(anyAddress.Equals(null));
        Assert.False(anyAddress.Equals("text"));
    }

    [Theory]
    [InlineData("1.2.3.4", "1.2.3.5", -1)]
    [InlineData("1.2.3.5", "1.2.3.4", 1)]
    [InlineData("1.2.3.4", "1.2.3.4", 0)]
    [InlineData("9.255.255.255", "10.0.0.0", -1)]
    [InlineData("128.0.0.0", "127.255.255.255", 1)]
    public void Addresses_are_ordered_by_value_not_by_text(string left, string right, int sign) {
        Assert.Equal(sign, Math.Sign(Ipv4Address.Parse(left).CompareTo(Ipv4Address.Parse(right))));
    }

    [Fact]
    public void Every_address_sorts_after_null() {
        Ipv4Address anyAddress = Any.Ipv4Address().Generate();

        Assert.True(anyAddress.CompareTo(null) > 0);
    }

}
