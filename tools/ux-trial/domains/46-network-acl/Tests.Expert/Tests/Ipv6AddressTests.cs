using JustDummies;

using NetworkAcl.Domain;
using NetworkAcl.Tests.Expert.Arbitrary;

using Xunit;

using static NetworkAcl.Tests.Expert.Shorthand;

namespace NetworkAcl.Tests.Expert.Tests;

public sealed class Ipv6AddressTests {

    [Theory]
    [InlineData("::", 0x0000000000000000UL, 0x0000000000000000UL)]
    [InlineData("::1", 0x0000000000000000UL, 0x0000000000000001UL)]
    [InlineData("1::", 0x0001000000000000UL, 0x0000000000000000UL)]
    [InlineData("2001:db8::1", 0x20010db800000000UL, 0x0000000000000001UL)]
    [InlineData("2001:DB8::1", 0x20010db800000000UL, 0x0000000000000001UL)]
    [InlineData("2001:0db8:0000:0000:0000:0000:0000:0001", 0x20010db800000000UL, 0x0000000000000001UL)]
    [InlineData("1:2:3:4:5:6:7:8", 0x0001000200030004UL, 0x0005000600070008UL)]
    [InlineData("1:2:3:4:5:6:7::", 0x0001000200030004UL, 0x0005000600070000UL)]   // "::" standing for exactly one group
    [InlineData("::2:3:4:5:6:7:8", 0x0000000200030004UL, 0x0005000600070008UL)]
    [InlineData("a:B:c:D:e:F:0:1", 0x000a000b000c000dUL, 0x000e000f00000001UL)]
    [InlineData("0:0:0:0:0:0:0:0", 0x0000000000000000UL, 0x0000000000000000UL)]
    [InlineData("ffff:ffff:ffff:ffff:ffff:ffff:ffff:ffff", 0xffffffffffffffffUL, 0xffffffffffffffffUL)]
    public void Parse_reads_the_rfc_4291_text_forms(string text, ulong expectedHigh, ulong expectedLow) {
        // Act
        Ipv6Address address = Ipv6Address.Parse(text);

        // Assert
        Assert.Equal(Bits(expectedHigh, expectedLow), address.Value);
    }

    [Theory]
    [InlineData("::ffff:192.0.2.1", 0x0000000000000000UL, 0x0000ffffc0000201UL)]
    [InlineData("::1.2.3.4", 0x0000000000000000UL, 0x0000000001020304UL)]
    [InlineData("64:ff9b::192.0.2.33", 0x0064ff9b00000000UL, 0x00000000c0000221UL)]
    [InlineData("1:2:3:4:5:6:1.2.3.4", 0x0001000200030004UL, 0x0005000601020304UL)]
    [InlineData("1:2:3:4:5::1.2.3.4", 0x0001000200030004UL, 0x0005000001020304UL)]
    public void Parse_counts_a_dotted_quad_tail_as_two_groups(string text, ulong expectedHigh, ulong expectedLow) {
        // Act
        Ipv6Address address = Ipv6Address.Parse(text);

        // Assert
        Assert.Equal(Bits(expectedHigh, expectedLow), address.Value);
    }

    [Theory]
    [InlineData("")]
    [InlineData(":")]
    [InlineData(":::")]
    [InlineData("1::2::3")]
    [InlineData("::1::")]
    [InlineData("1:2:3:4:5:6:7")]               // too few groups without "::"
    [InlineData("1:2:3:4:5:6:7:8:9")]           // too many groups
    [InlineData("1:2:3:4:5:6:7:8::")]           // "::" would stand for zero groups
    [InlineData("1:2:3:4:5:6:7::8")]            // idem, in the middle
    [InlineData("::1:2:3:4:5:6:7:8")]           // idem, at the start
    [InlineData("12345::")]
    [InlineData("g::1")]
    [InlineData("1:2:3:4:5:6:7:")]
    [InlineData(":1:2:3:4:5:6:7")]
    [InlineData("1::2:")]
    [InlineData(":1::2")]
    [InlineData("fe80::1%eth0")]
    [InlineData("::ffff:1.2.3.4:5")]            // the dotted tail must be the very last part
    [InlineData("1.2.3.4::")]                   // never before the "::"
    [InlineData("::1.2.3")]
    [InlineData("::1.2.3.256")]
    [InlineData("::01.2.3.4")]
    [InlineData("1:2:3:4:5:6::1.2.3.4")]        // 6 groups + 2 for the tail leave no room for "::"
    [InlineData(" ::1")]
    [InlineData("::1 ")]
    [InlineData("::1\n")]
    [InlineData("0x1::")]
    public void Parse_refuses_text_that_is_not_an_ipv6_address(string text) {
        Assert.Throws<FormatException>(() => Ipv6Address.Parse(text));
    }

    [Fact]
    public void Parse_refuses_a_missing_text() {
        Assert.Throws<ArgumentNullException>(() => Ipv6Address.Parse(null!));
    }

    [Theory]
    [InlineData(0x0000000000000000UL, 0x0000000000000000UL, "::")]
    [InlineData(0x0000000000000000UL, 0x0000000000000001UL, "::1")]
    [InlineData(0x20010db800000000UL, 0x0000000000000001UL, "2001:db8::1")]
    [InlineData(0x0001000200030004UL, 0x0005000600000000UL, "1:2:3:4:5:6::")]          // trailing run
    [InlineData(0x0000000000000000UL, 0x0001000200030004UL, "::1:2:3:4")]              // leading run
    [InlineData(0x20010db800000001UL, 0x0001000100010001UL, "2001:db8:0:1:1:1:1:1")]   // a single zero group is not compressed
    [InlineData(0x2001000000000001UL, 0x0000000000000001UL, "2001:0:0:1::1")]          // the longest run wins
    [InlineData(0x0001000000000002UL, 0x0000000000030004UL, "1::2:0:0:3:4")]           // on a tie, the first
    [InlineData(0x0001000000020000UL, 0x0000000300040005UL, "1:0:2::3:4:5")]           // a longer later run beats an earlier lone zero
    [InlineData(0x0000000000000000UL, 0x0000ffffc0000201UL, "::ffff:c000:201")]        // the IPv4 tail is rendered as hex
    [InlineData(0xabcdef0123456789UL, 0x0123456789abcdefUL, "abcd:ef01:2345:6789:123:4567:89ab:cdef")]   // lower case, no leading zeros
    [InlineData(0xffffffffffffffffUL, 0xffffffffffffffffUL, "ffff:ffff:ffff:ffff:ffff:ffff:ffff:ffff")]
    public void ToString_writes_the_rfc_5952_canonical_form(ulong high, ulong low, string expectedText) {
        // Arrange
        Ipv6Address address = new Ipv6Address(Bits(high, low));

        // Act & Assert
        Assert.Equal(expectedText, address.ToString());
    }

    [Theory]
    [InlineData("2001:0DB8:0000:0000:0000:0000:0000:0001", "2001:db8::1")]
    [InlineData("0:0:0:0:0:0:0:0", "::")]
    [InlineData("::FFFF:192.0.2.1", "::ffff:c000:201")]
    [InlineData("1:0:0:0:0:0:0:1", "1::1")]
    [InlineData("1:0:0:2:0:0:3:4", "1::2:0:0:3:4")]
    public void A_parsed_address_is_written_back_canonically(string text, string expectedCanonicalText) {
        Assert.Equal(expectedCanonicalText, Ipv6Address.Parse(text).ToString());
    }

    [Theory]
    [InlineData(0x0000000000000000UL, false)]
    [InlineData(0xFEFFFFFFFFFFFFFFUL, false)]   // feff:ffff:...: the last address below ff00::/8
    [InlineData(0xFF00000000000000UL, true)]    // ff00::
    [InlineData(0xFFFFFFFFFFFFFFFFUL, true)]
    public void IsMulticast_is_true_exactly_inside_ff00_slash_8(ulong high, bool expected) {
        // Arrange
        Ipv6Address address = new Ipv6Address(Bits(high, 0xFFFFFFFFFFFFFFFFUL));

        // Act & Assert
        Assert.Equal(expected, address.IsMulticast);
    }

    [Fact]
    public void An_address_whose_first_byte_is_ff_is_multicast_whatever_the_120_bits_after_it() {
        // Arrange
        UInt128 anyLowBits = Any.UInt128().LessThan(UInt128.One << 120).Generate();
        Ipv6Address address = new Ipv6Address((UInt128)0xFF << 120 | anyLowBits);

        // Act & Assert
        Assert.True(address.IsMulticast);
    }

    [Fact]
    public void Addresses_are_ordered_by_their_128_bits() {
        // Arrange
        Ipv6Address low  = Ipv6Address.Parse("::ffff:ffff:ffff:ffff");
        Ipv6Address high = Ipv6Address.Parse("0:0:0:1::");   // one bit above the low half

        // Act & Assert
        Assert.True(low.CompareTo(high) < 0);
        Assert.True(high.CompareTo(low) > 0);
        Assert.Equal(0, high.CompareTo(Ipv6Address.Parse("0:0:0:1::")));
    }

    [Fact]
    public void Any_address_sorts_after_a_missing_one() {
        // Arrange
        Ipv6Address anyAddress = Any.Ipv6Address().Generate();

        // Act & Assert
        Assert.True(anyAddress.CompareTo(null) > 0);
    }

    [Fact]
    public void Addresses_built_from_the_same_bits_are_equal_and_hash_alike() {
        // Arrange
        UInt128 anyBits = Any.UInt128().Generate();
        Ipv6Address first  = new Ipv6Address(anyBits);
        Ipv6Address second = new Ipv6Address(anyBits);

        // Act & Assert
        Assert.Equal(first, second);
        Assert.True(first.Equals((object)second));
        Assert.Equal(first.GetHashCode(), second.GetHashCode());
    }

    [Fact]
    public void Addresses_that_differ_in_the_lowest_bit_only_are_not_equal() {
        // Arrange
        Ipv6Address first  = Ipv6Address.Parse("2001:db8::");
        Ipv6Address second = Ipv6Address.Parse("2001:db8::1");

        // Act & Assert
        Assert.NotEqual(first, second);
        Assert.False(first.Equals(null));
    }

}
