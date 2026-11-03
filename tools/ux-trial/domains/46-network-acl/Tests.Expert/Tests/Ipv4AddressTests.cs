using JustDummies;

using NetworkAcl.Domain;
using NetworkAcl.Tests.Expert.Arbitrary;

using Xunit;

namespace NetworkAcl.Tests.Expert.Tests;

public sealed class Ipv4AddressTests {

    [Theory]
    [InlineData("0.0.0.0", 0x00000000u)]
    [InlineData("0.0.0.1", 0x00000001u)]
    [InlineData("1.2.3.4", 0x01020304u)]
    [InlineData("10.0.0.1", 0x0A000001u)]
    [InlineData("192.168.1.255", 0xC0A801FFu)]
    [InlineData("255.255.255.255", 0xFFFFFFFFu)]
    public void Parse_reads_four_decimal_octets_most_significant_first(string text, uint expectedBits) {
        // Act
        Ipv4Address address = Ipv4Address.Parse(text);

        // Assert
        Assert.Equal(expectedBits, address.Value);
    }

    [Theory]
    [InlineData("")]
    [InlineData("1.2.3")]
    [InlineData("1.2.3.4.5")]
    [InlineData("256.0.0.1")]
    [InlineData("1.2.3.1000")]
    [InlineData("1.2.3.")]
    [InlineData(".1.2.3")]
    [InlineData("1..2.3")]
    [InlineData("a.b.c.d")]
    [InlineData("1.2.3.-4")]
    [InlineData("1.2.3.+4")]
    [InlineData("1.2.3.0x4")]
    [InlineData(" 1.2.3.4")]
    [InlineData("1.2.3.4 ")]
    [InlineData("1.2.3.4\n")]
    [InlineData("1,2,3,4")]
    [InlineData("١.2.3.4")]   // an Arabic-Indic digit one: a digit for char.IsDigit, not a decimal octet
    public void Parse_refuses_text_that_is_not_a_dotted_quad(string text) {
        Assert.Throws<FormatException>(() => Ipv4Address.Parse(text));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    public void Parse_refuses_a_leading_zero_in_any_octet_whatever_the_other_octets_are(int position) {
        // Arrange
        // The other three octets are dummies: only the leading zero is on trial.
        string[] octets = Any.ArrayOf(Any.Int32().Between(0, 255).As(octet => octet.ToString())).WithCount(4).Generate();
        octets[position] = "007";
        string text = string.Join(".", octets);

        // Act & Assert
        Assert.Throws<FormatException>(() => Ipv4Address.Parse(text));
    }

    [Fact]
    public void Parse_refuses_a_two_digit_octet_written_with_a_leading_zero() {
        Assert.Throws<FormatException>(() => Ipv4Address.Parse("10.00.0.1"));
    }

    [Fact]
    public void Parse_accepts_a_lone_zero_in_every_position() {
        // Act
        Ipv4Address address = Ipv4Address.Parse("0.0.0.0");

        // Assert
        Assert.Equal(0u, address.Value);
    }

    [Fact]
    public void Parse_refuses_a_missing_text() {
        Assert.Throws<ArgumentNullException>(() => Ipv4Address.Parse(null!));
    }

    [Theory]
    [InlineData(0x00000000u, "0.0.0.0")]
    [InlineData(0x01020304u, "1.2.3.4")]
    [InlineData(0x0A000001u, "10.0.0.1")]
    [InlineData(0xC0A801FFu, "192.168.1.255")]
    [InlineData(0xFFFFFFFFu, "255.255.255.255")]
    public void ToString_writes_the_dotted_quad(uint bits, string expectedText) {
        // Arrange
        Ipv4Address address = new Ipv4Address(bits);

        // Act & Assert
        Assert.Equal(expectedText, address.ToString());
    }

    [Theory]
    [InlineData(0x00000000u, false)]
    [InlineData(0xDFFFFFFFu, false)]   // 223.255.255.255, the last address below 224.0.0.0/4
    [InlineData(0xE0000000u, true)]    // 224.0.0.0
    [InlineData(0xEFFFFFFFu, true)]    // 239.255.255.255
    [InlineData(0xF0000000u, false)]   // 240.0.0.0
    public void IsMulticast_is_true_exactly_inside_224_0_0_0_slash_4(uint bits, bool expected) {
        Assert.Equal(expected, new Ipv4Address(bits).IsMulticast);
    }

    [Fact]
    public void An_address_whose_first_four_bits_are_1110_is_multicast_whatever_the_28_bits_after_them() {
        // Arrange
        uint anyLowBits = Any.UInt32().LessThan(0x1000_0000u).Generate();
        Ipv4Address address = new Ipv4Address(0xE000_0000u | anyLowBits);

        // Act & Assert
        Assert.True(address.IsMulticast);
    }

    [Theory]
    [InlineData(0xFFFFFFFFu, true)]
    [InlineData(0xFFFFFFFEu, false)]
    [InlineData(0xE0000000u, false)]
    [InlineData(0x00000000u, false)]
    public void IsLimitedBroadcast_is_true_for_255_255_255_255_alone(uint bits, bool expected) {
        Assert.Equal(expected, new Ipv4Address(bits).IsLimitedBroadcast);
    }

    [Fact]
    public void Addresses_are_ordered_by_value_not_by_text() {
        // Arrange
        Ipv4Address nine = Ipv4Address.Parse("9.0.0.0");
        Ipv4Address ten  = Ipv4Address.Parse("10.0.0.0");   // "10..." sorts before "9..." as text

        // Act & Assert
        Assert.True(nine.CompareTo(ten) < 0);
        Assert.True(ten.CompareTo(nine) > 0);
        Assert.Equal(0, ten.CompareTo(Ipv4Address.Parse("10.0.0.0")));
    }

    [Fact]
    public void Any_address_sorts_after_a_missing_one() {
        // Arrange
        Ipv4Address anyAddress = Any.Ipv4Address().Generate();

        // Act & Assert
        Assert.True(anyAddress.CompareTo(null) > 0);
    }

    [Fact]
    public void Addresses_built_from_the_same_bits_are_equal_and_hash_alike() {
        // Arrange
        uint anyBits = Any.UInt32().Generate();
        Ipv4Address first  = new Ipv4Address(anyBits);
        Ipv4Address second = new Ipv4Address(anyBits);

        // Act & Assert
        Assert.Equal(first, second);
        Assert.True(first.Equals((object)second));
        Assert.Equal(first.GetHashCode(), second.GetHashCode());
    }

    [Fact]
    public void Addresses_built_from_different_bits_are_not_equal() {
        // Arrange
        Ipv4Address first  = new Ipv4Address(1u);
        Ipv4Address second = new Ipv4Address(2u);

        // Act & Assert
        Assert.NotEqual(first, second);
        Assert.False(first.Equals(null));
        Assert.False(first.Equals((object)"0.0.0.1"));
    }

}
