using JustDummies;

using NetworkAcl.Domain;

using Xunit;

namespace NetworkAcl.Tests.Regular;

public sealed class Ipv6AddressTests {

    [Fact]
    public void An_address_written_then_parsed_is_the_same_address() {
        // Arrange
        Ipv6Address anyAddress = Any.Ipv6Address().Generate();

        // Act
        Ipv6Address parsed = Ipv6Address.Parse(anyAddress.ToString());

        // Assert
        Assert.Equal(anyAddress, parsed);
    }

    [Fact]
    public void The_canonical_form_is_lower_case() {
        // Arrange
        Ipv6Address anyAddress = Any.Ipv6Address().Generate();

        // Act
        string text = anyAddress.ToString();

        // Assert
        Assert.Equal(text.ToLowerInvariant(), text);
    }

    [Fact]
    public void The_canonical_form_has_at_most_one_compression() {
        // Arrange
        Ipv6Address anyAddress = Any.Ipv6Address().Generate();

        // Act
        string text = anyAddress.ToString();

        // Assert
        Assert.True(text.Split("::").Length <= 2, text);
        Assert.DoesNotContain(":::", text);
    }

    [Fact]
    public void Parse_is_case_insensitive() {
        // Arrange
        Ipv6Address anyAddress = Any.Ipv6Address().Generate();

        // Act
        Ipv6Address upper = Ipv6Address.Parse(anyAddress.ToString().ToUpperInvariant());

        // Assert
        Assert.Equal(anyAddress, upper);
    }

    [Theory]
    [InlineData("::", "::")]
    [InlineData("::1", "::1")]
    [InlineData("1::", "1::")]
    [InlineData("0:0:0:0:0:0:0:0", "::")]
    [InlineData("0:0:0:0:0:0:0:1", "::1")]
    [InlineData("2001:0db8:0000:0000:0000:ff00:0042:8329", "2001:db8::ff00:42:8329")]
    [InlineData("2001:DB8::1", "2001:db8::1")]
    [InlineData("1:2:3:4:5:6:7:8", "1:2:3:4:5:6:7:8")]
    public void Parse_then_ToString_gives_the_canonical_form(string text, string canonical) {
        // Act
        string written = Ipv6Address.Parse(text).ToString();

        // Assert
        Assert.Equal(canonical, written);
    }

    [Theory]
    [InlineData("1:0:0:2:0:0:3:4", "1::2:0:0:3:4")]
    [InlineData("1:0:0:2:0:0:0:3", "1:0:0:2::3")]
    [InlineData("1:0:2:3:4:5:6:7", "1:0:2:3:4:5:6:7")]
    [InlineData("0:0:1:0:0:0:1:0", "0:0:1::1:0")]
    [InlineData("1:0:0:2:3:4:5:0", "1::2:3:4:5:0")]
    [InlineData("0:1:0:0:0:0:0:0", "0:1::")]
    public void The_longest_run_of_zero_groups_is_compressed_and_the_first_on_a_tie(string text, string canonical) {
        // Act
        string written = Ipv6Address.Parse(text).ToString();

        // Assert
        Assert.Equal(canonical, written);
    }

    [Theory]
    [InlineData("::ffff:192.0.2.1", "::ffff:c000:201")]
    [InlineData("64:ff9b::192.0.2.33", "64:ff9b::c000:221")]
    [InlineData("1:2:3:4:5:6:1.2.3.4", "1:2:3:4:5:6:102:304")]
    [InlineData("::1.2.3.4", "::102:304")]
    public void An_ipv4_tail_counts_as_two_groups_and_is_written_in_hex(string text, string canonical) {
        // Act
        string written = Ipv6Address.Parse(text).ToString();

        // Assert
        Assert.Equal(canonical, written);
    }

    [Fact]
    public void Parse_reads_the_groups_most_significant_first() {
        // Act
        Ipv6Address address = Ipv6Address.Parse("1:2:3:4:5:6:7:8");

        // Assert
        Assert.Equal(new UInt128(0x0001_0002_0003_0004UL, 0x0005_0006_0007_0008UL), address.Value);
    }

    [Theory]
    [InlineData("1:2:3:4:5:6:7::", "1:2:3:4:5:6:7:0")]
    [InlineData("::2:3:4:5:6:7:8", "0:2:3:4:5:6:7:8")]
    [InlineData("1:2:3::5:6:7:8", "1:2:3:0:5:6:7:8")]
    public void The_compression_may_stand_for_a_single_zero_group_which_is_written_out(string text, string canonical) {
        // Act
        string written = Ipv6Address.Parse(text).ToString();

        // Assert
        Assert.Equal(canonical, written);
    }

    [Theory]
    [InlineData("")]
    [InlineData(":")]
    [InlineData(":::")]
    [InlineData("1::2::3")]
    [InlineData("1:::2")]
    [InlineData("1:2:3:4:5:6:7")]
    [InlineData("1:2:3:4:5:6:7:8:9")]
    [InlineData("1:2:3:4::5:6:7:8")]
    [InlineData("1:2:3:4:5:6:7:8::")]
    [InlineData("::1:2:3:4:5:6:7:8")]
    [InlineData("12345::")]
    [InlineData("g::1")]
    [InlineData("1:2:3:4:5:6:7:")]
    [InlineData(":1:2:3:4:5:6:7")]
    [InlineData("1::2:")]
    [InlineData("::1.2.3")]
    [InlineData("::1.2.3.256")]
    [InlineData("::01.2.3.4")]
    [InlineData("1.2.3.4::")]
    [InlineData("1.2.3.4::1")]
    [InlineData("::1.2.3.4:5")]
    [InlineData("1:2:3:4:5:1.2.3.4")]
    [InlineData("fe80::1%eth0")]
    [InlineData("fe80::1%")]
    [InlineData(" ::1")]
    [InlineData("::1 ")]
    [InlineData("::-1")]
    [InlineData("::+1")]
    public void Parse_refuses_what_is_not_an_ipv6_text_form(string text) {
        // Act
        Exception? thrown = Record.Exception(() => Ipv6Address.Parse(text));

        // Assert
        Assert.IsType<FormatException>(thrown);
    }

    [Fact]
    public void Parse_refuses_null() {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => Ipv6Address.Parse(null!));
    }

    [Fact]
    public void Multicast_is_ff00_slash_8() {
        // Arrange
        UInt128     firstMulticast = (UInt128)0xFF << 120;
        Ipv6Address anyMulticast   = Any.UInt128().GreaterThanOrEqualTo(firstMulticast).As(value => new Ipv6Address(value)).Generate();
        Ipv6Address anyUnicast     = Any.UInt128().LessThan(firstMulticast).As(value => new Ipv6Address(value)).Generate();

        // Assert
        Assert.True(anyMulticast.IsMulticast);
        Assert.False(anyUnicast.IsMulticast);
        Assert.True(Ipv6Address.Parse("ff00::").IsMulticast);
        Assert.True(Ipv6Address.Parse("ff02::1").IsMulticast);
        Assert.False(Ipv6Address.Parse("feff:ffff:ffff:ffff:ffff:ffff:ffff:ffff").IsMulticast);
    }

    [Fact]
    public void Addresses_are_ordered_by_value() {
        // Arrange
        UInt128     anyLow  = Any.UInt128().LessThan(UInt128.MaxValue).Generate();
        UInt128     anyHigh = Any.UInt128().GreaterThan(anyLow).Generate();
        Ipv6Address low     = new Ipv6Address(anyLow);
        Ipv6Address high    = new Ipv6Address(anyHigh);

        // Assert
        Assert.True(low.CompareTo(high) < 0);
        Assert.True(high.CompareTo(low) > 0);
        Assert.Equal(0, low.CompareTo(new Ipv6Address(anyLow)));
        Assert.True(low.CompareTo(null) > 0);
    }

}
