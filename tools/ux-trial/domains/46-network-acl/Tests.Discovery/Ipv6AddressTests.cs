using NetworkAcl.Domain;

namespace NetworkAcl.Tests.Discovery;

public sealed class Ipv6AddressTests {

    [Theory]
    [InlineData("::", "00000000000000000000000000000000")]
    [InlineData("::1", "00000000000000000000000000000001")]
    [InlineData("1::", "00010000000000000000000000000000")]
    [InlineData("2001:db8::1", "20010db8000000000000000000000001")]
    [InlineData("2001:DB8::1", "20010db8000000000000000000000001")]
    [InlineData("2001:0db8:0000:0000:0000:0000:0000:0001", "20010db8000000000000000000000001")]
    [InlineData("1:2:3:4:5:6:7:8", "00010002000300040005000600070008")]
    [InlineData("1:2:3:4:5:6:7::", "00010002000300040005000600070000")]
    [InlineData("::2:3:4:5:6:7:8", "00000002000300040005000600070008")]
    [InlineData("fe80::1", "fe800000000000000000000000000001")]
    [InlineData("ffff:ffff:ffff:ffff:ffff:ffff:ffff:ffff", "ffffffffffffffffffffffffffffffff")]
    [InlineData("::ffff:192.0.2.1", "00000000000000000000ffffc0000201")]
    [InlineData("64:ff9b::192.0.2.33", "0064ff9b0000000000000000c0000221")]
    [InlineData("1:2:3:4:5:6:1.2.3.4", "00010002000300040005000601020304")]
    [InlineData("::1.2.3.4", "00000000000000000000000001020304")]
    public void Parse_reads_the_rfc_4291_text_forms(string text, string expectedHex) {
        Assert.Equal(Build.Hex(expectedHex), Ipv6Address.Parse(text).Value);
    }

    [Theory]
    [InlineData("")]
    [InlineData(":")]
    [InlineData(":::")]
    [InlineData("1::2::3")]
    [InlineData("1:2:3:4:5:6:7")]
    [InlineData("1:2:3:4:5:6:7:8:9")]
    [InlineData("1:2:3:4:5:6:7:8::")]
    [InlineData("::1:2:3:4:5:6:7:8")]
    [InlineData("12345::")]
    [InlineData("::g")]
    [InlineData("1:2:3:4:5:6:7:")]
    [InlineData(":1:2:3:4:5:6:7")]
    [InlineData("1::2:")]
    [InlineData(":1::2")]
    [InlineData("1:::2")]
    [InlineData("::1.2.3.4:5")]
    [InlineData("1.2.3.4::")]
    [InlineData("1.2.3.4::1")]
    [InlineData("1:2:3:4:5:6:7:1.2.3.4")]
    [InlineData("::ffff:192.0.2.01")]
    [InlineData("::ffff:192.0.2")]
    [InlineData("::ffff:256.0.2.1")]
    [InlineData("1:2:3:4:5:6:7:8:")]
    [InlineData(" ::1")]
    [InlineData("::1 ")]
    public void Parse_refuses_every_syntax_error(string text) {
        Assert.Throws<FormatException>(() => Ipv6Address.Parse(text));
    }

    [Theory]
    [InlineData("fe80::1%eth0")]
    [InlineData("fe80::1%1")]
    [InlineData("::%x")]
    public void Parse_refuses_a_zone_index_because_a_policy_matches_addresses_not_interfaces(string text) {
        Assert.Throws<FormatException>(() => Ipv6Address.Parse(text));
    }

    [Fact]
    public void Parse_refuses_null() {
        Assert.Throws<ArgumentNullException>(() => Ipv6Address.Parse(null!));
    }

    [Fact]
    public void A_double_colon_may_stand_for_exactly_one_zero_group() {
        Ipv6Address address = Ipv6Address.Parse("1:2:3:4:5:6:7::");

        Assert.Equal("1:2:3:4:5:6:7:0", address.ToString());
    }

    [Theory]
    [InlineData("2001:db8:0:0:1:0:0:1", "2001:db8::1:0:0:1")]
    [InlineData("2001:0:0:1:0:0:0:1", "2001:0:0:1::1")]
    [InlineData("1:0:2:0:3:0:4:0", "1:0:2:0:3:0:4:0")]
    [InlineData("0:0:0:0:0:0:0:0", "::")]
    [InlineData("0:0:0:0:0:0:0:1", "::1")]
    [InlineData("1:0:0:0:0:0:0:0", "1::")]
    [InlineData("0:0:1:0:0:0:0:0", "0:0:1::")]
    [InlineData("0:0:0:1:0:0:0:0", "0:0:0:1::")]
    [InlineData("0:0:0:1:0:0:0:1", "::1:0:0:0:1")]
    [InlineData("ABCD:0:0:0:0:0:0:EF", "abcd::ef")]
    [InlineData("1:2:3:4:5:6:7:8", "1:2:3:4:5:6:7:8")]
    [InlineData("1:0:3:4:5:6:7:8", "1:0:3:4:5:6:7:8")]
    [InlineData("1:2:3:4:5:6:0:0", "1:2:3:4:5:6::")]
    [InlineData("::ffff:192.0.2.1", "::ffff:c000:201")]
    [InlineData("0001:0002:0003:0004:0005:0006:0007:0008", "1:2:3:4:5:6:7:8")]
    [InlineData("2001:db8:0:0:0:0:2:1", "2001:db8::2:1")]
    public void ToString_is_the_rfc_5952_canonical_form(string input, string canonical) {
        Assert.Equal(canonical, Ipv6Address.Parse(input).ToString());
    }

    [Theory]
    [InlineData("fe00::", false)]
    [InlineData("feff:ffff:ffff:ffff:ffff:ffff:ffff:ffff", false)]
    [InlineData("ff00::", true)]
    [InlineData("ff02::1", true)]
    [InlineData("ffff:ffff:ffff:ffff:ffff:ffff:ffff:ffff", true)]
    [InlineData("::", false)]
    [InlineData("2001:db8::1", false)]
    public void IsMulticast_covers_ff00_slash_8_exactly(string text, bool expected) {
        Assert.Equal(expected, Ipv6Address.Parse(text).IsMulticast);
    }

    [Fact]
    public void Two_addresses_with_the_same_bits_are_equal_and_hash_alike() {
        UInt128 anyBits = Any.UInt128().Generate();

        Ipv6Address first  = new Ipv6Address(anyBits);
        Ipv6Address second = new Ipv6Address(anyBits);

        Assert.Equal(first, second);
        Assert.Equal(first.GetHashCode(), second.GetHashCode());
    }

    [Fact]
    public void Addresses_that_differ_by_one_bit_are_different() {
        Assert.NotEqual(Ipv6Address.Parse("::1"), Ipv6Address.Parse("::3"));
        Assert.NotEqual(Ipv6Address.Parse("::1"), Ipv6Address.Parse("8000::1"));
    }

    [Theory]
    [InlineData("::1", "::2", -1)]
    [InlineData("::2", "::1", 1)]
    [InlineData("::2", "::2", 0)]
    [InlineData("1::", "::ffff", 1)]
    [InlineData("fe80::", "ff00::", -1)]
    public void Addresses_are_ordered_by_value(string left, string right, int sign) {
        Assert.Equal(sign, Math.Sign(Ipv6Address.Parse(left).CompareTo(Ipv6Address.Parse(right))));
    }

    [Fact]
    public void Every_address_sorts_after_null() {
        Ipv6Address anyAddress = Any.Ipv6Address().Generate();

        Assert.True(anyAddress.CompareTo(null) > 0);
    }

}
