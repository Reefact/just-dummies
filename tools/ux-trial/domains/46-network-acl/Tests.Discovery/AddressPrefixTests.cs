using NetworkAcl.Domain;

namespace NetworkAcl.Tests.Discovery;

public sealed class AddressPrefixTests {

    [Theory]
    [InlineData("10.0.0.0/8", IpVersion.V4, 8, 24)]
    [InlineData("0.0.0.0/0", IpVersion.V4, 0, 32)]
    [InlineData("192.0.2.1/32", IpVersion.V4, 32, 0)]
    [InlineData("192.168.1.0/24", IpVersion.V4, 24, 8)]
    [InlineData("::/0", IpVersion.V6, 0, 128)]
    [InlineData("::1/128", IpVersion.V6, 128, 0)]
    [InlineData("2001:db8::/32", IpVersion.V6, 32, 96)]
    [InlineData("fe80::/10", IpVersion.V6, 10, 118)]
    [InlineData("::ffff:0:0/96", IpVersion.V6, 96, 32)]
    public void Parse_reads_the_family_the_length_and_the_host_bits(string text, IpVersion version, int length, int hostBits) {
        // Act
        AddressPrefix prefix = AddressPrefix.Parse(text);

        // Assert
        Assert.Equal(version, prefix.Version);
        Assert.Equal(length, prefix.PrefixLength);
        Assert.Equal(hostBits, prefix.HostBits);
    }

    [Theory]
    [InlineData("10.0.0.0/8")]
    [InlineData("0.0.0.0/0")]
    [InlineData("203.0.113.64/26")]
    [InlineData("::/0")]
    [InlineData("2001:db8::/32")]
    [InlineData("2001:db8:0:1::/64")]
    [InlineData("::1/128")]
    public void ToString_writes_back_what_parse_read(string text) {
        Assert.Equal(text, AddressPrefix.Parse(text).ToString());
    }

    [Theory]
    [InlineData("10.0.0.0")]
    [InlineData("10.0.0.0/8/8")]
    [InlineData("10.0.0.0/")]
    [InlineData("10.0.0.0/x")]
    [InlineData("10.0.0.0/-1")]
    [InlineData("10.0.0.0/+8")]
    [InlineData("10.0.0.0/1234")]
    [InlineData("10.0.0.0/ 8")]
    [InlineData("/8")]
    [InlineData("10.0.0/8")]
    [InlineData("010.0.0.0/8")]
    [InlineData("::1/")]
    [InlineData("1::2::3/64")]
    [InlineData("fe80::1%eth0/64")]
    [InlineData("")]
    public void Parse_refuses_what_is_not_an_address_slash_length(string text) {
        Assert.Throws<FormatException>(() => AddressPrefix.Parse(text));
    }

    [Fact]
    public void Parse_refuses_null() {
        Assert.Throws<ArgumentNullException>(() => AddressPrefix.Parse(null!));
    }

    [Theory]
    [InlineData("10.0.0.0/33")]
    [InlineData("0.0.0.0/999")]
    [InlineData("::/129")]
    [InlineData("2001:db8::/200")]
    public void Parse_lets_a_length_outside_the_family_range_through_as_out_of_range(string text) {
        Assert.Throws<ArgumentOutOfRangeException>(() => AddressPrefix.Parse(text));
    }

    [Theory]
    [InlineData("10.1.0.0/8")]
    [InlineData("10.0.0.1/8")]
    [InlineData("192.168.1.1/24")]
    [InlineData("1.2.3.4/0")]
    [InlineData("0.0.0.1/31")]
    [InlineData("2001:db8::1/32")]
    [InlineData("::1/0")]
    [InlineData("8000::/0")]
    public void A_prefix_with_host_bits_set_is_refused(string text) {
        Assert.Throws<ArgumentException>(() => AddressPrefix.Parse(text));
    }

    [Fact]
    public void A_negative_or_too_long_ipv4_length_is_out_of_range() {
        Ipv4Address anyNetwork = new Ipv4Address(0);

        Assert.Throws<ArgumentOutOfRangeException>(() => new AddressPrefix(anyNetwork, -1));
        Assert.Throws<ArgumentOutOfRangeException>(() => new AddressPrefix(anyNetwork, 33));
    }

    [Fact]
    public void A_negative_or_too_long_ipv6_length_is_out_of_range() {
        Ipv6Address anyNetwork = new Ipv6Address(UInt128.Zero);

        Assert.Throws<ArgumentOutOfRangeException>(() => new AddressPrefix(anyNetwork, -1));
        Assert.Throws<ArgumentOutOfRangeException>(() => new AddressPrefix(anyNetwork, 129));
    }

    [Fact]
    public void A_null_network_is_refused() {
        Assert.Throws<ArgumentNullException>(() => new AddressPrefix((Ipv4Address)null!, 8));
        Assert.Throws<ArgumentNullException>(() => new AddressPrefix((Ipv6Address)null!, 8));
    }

    [Fact]
    public void Any_is_the_zero_length_prefix_of_each_family() {
        Assert.Equal("0.0.0.0/0", AddressPrefix.Any(IpVersion.V4).ToString());
        Assert.Equal("::/0", AddressPrefix.Any(IpVersion.V6).ToString());
        Assert.True(AddressPrefix.Any(IpVersion.V4).IsAnyAddress);
        Assert.False(AddressPrefix.Parse("10.0.0.0/8").IsAnyAddress);
    }

    [Fact]
    public void Any_refuses_an_unknown_family() {
        Assert.Throws<ArgumentOutOfRangeException>(() => AddressPrefix.Any((IpVersion)5));
    }

    [Fact]
    public void Host_is_a_full_length_prefix() {
        Assert.Equal("192.0.2.1/32", AddressPrefix.Host(Ipv4Address.Parse("192.0.2.1")).ToString());
        Assert.Equal("2001:db8::1/128", AddressPrefix.Host(Ipv6Address.Parse("2001:db8::1")).ToString());
        Assert.Equal(0, AddressPrefix.Host(Ipv4Address.Parse("192.0.2.1")).HostBits);
    }

    [Fact]
    public void A_host_prefix_of_any_address_is_accepted() {
        // Arrange: no host bit can be set past a full length, whatever the address
        Ipv4Address anyV4 = Any.Ipv4Address().Generate();
        Ipv6Address anyV6 = Any.Ipv6Address().Generate();

        // Act
        AddressPrefix v4 = AddressPrefix.Host(anyV4);
        AddressPrefix v6 = AddressPrefix.Host(anyV6);

        // Assert
        Assert.Equal(IpVersion.V4, v4.Version);
        Assert.Equal(IpVersion.V6, v6.Version);
    }

    [Fact]
    public void The_network_of_the_other_family_is_null() {
        AddressPrefix v4 = AddressPrefix.Parse("10.0.0.0/8");
        AddressPrefix v6 = AddressPrefix.Parse("2001:db8::/32");

        Assert.Equal(Ipv4Address.Parse("10.0.0.0"), v4.Ipv4Network);
        Assert.Null(v4.Ipv6Network);
        Assert.Equal(Ipv6Address.Parse("2001:db8::"), v6.Ipv6Network);
        Assert.Null(v6.Ipv4Network);
    }

    [Theory]
    [InlineData("10.0.0.0/8", "10.0.0.0", true)]
    [InlineData("10.0.0.0/8", "10.255.255.255", true)]
    [InlineData("10.0.0.0/8", "9.255.255.255", false)]
    [InlineData("10.0.0.0/8", "11.0.0.0", false)]
    [InlineData("0.0.0.0/0", "203.0.113.9", true)]
    [InlineData("192.0.2.1/32", "192.0.2.1", true)]
    [InlineData("192.0.2.1/32", "192.0.2.0", false)]
    [InlineData("192.0.2.1/32", "192.0.2.2", false)]
    [InlineData("192.168.4.0/22", "192.168.7.255", true)]
    [InlineData("192.168.4.0/22", "192.168.8.0", false)]
    public void Contains_tells_whether_an_ipv4_address_is_under_the_prefix(string prefix, string address, bool expected) {
        Assert.Equal(expected, AddressPrefix.Parse(prefix).Contains(Ipv4Address.Parse(address)));
    }

    [Theory]
    [InlineData("2001:db8::/32", "2001:db8:ffff::1", true)]
    [InlineData("2001:db8::/32", "2001:db9::", false)]
    [InlineData("::/0", "ffff::1", true)]
    [InlineData("::1/128", "::1", true)]
    [InlineData("::1/128", "::2", false)]
    [InlineData("fe80::/10", "febf::1", true)]
    [InlineData("fe80::/10", "fec0::", false)]
    public void Contains_tells_whether_an_ipv6_address_is_under_the_prefix(string prefix, string address, bool expected) {
        Assert.Equal(expected, AddressPrefix.Parse(prefix).Contains(Ipv6Address.Parse(address)));
    }

    [Fact]
    public void An_ipv4_prefix_contains_no_ipv6_address_not_even_the_all_covering_one() {
        // Arrange: the address of the other family is a dummy, only its family matters
        Ipv6Address anyV6Address = Any.Ipv6Address().Generate();

        // Act
        bool contained = AddressPrefix.Any(IpVersion.V4).Contains(anyV6Address);

        // Assert
        Assert.False(contained);
    }

    [Fact]
    public void An_ipv6_prefix_contains_no_ipv4_address_not_even_the_all_covering_one() {
        Ipv4Address anyV4Address = Any.Ipv4Address().Generate();

        Assert.False(AddressPrefix.Any(IpVersion.V6).Contains(anyV4Address));
    }

    [Fact]
    public void Contains_refuses_a_null_address() {
        AddressPrefix prefix = AddressPrefix.Parse("10.0.0.0/8");

        Assert.Throws<ArgumentNullException>(() => prefix.Contains((Ipv4Address)null!));
        Assert.Throws<ArgumentNullException>(() => prefix.Contains((Ipv6Address)null!));
    }

    [Theory]
    [InlineData("10.0.0.0/8", "10.1.0.0/16", true)]
    [InlineData("10.0.0.0/8", "10.0.0.0/8", true)]
    [InlineData("10.0.0.0/8", "10.0.0.0/9", true)]
    [InlineData("10.0.0.0/8", "10.128.0.0/9", true)]
    [InlineData("10.1.0.0/16", "10.0.0.0/8", false)]
    [InlineData("10.0.0.0/8", "11.0.0.0/8", false)]
    [InlineData("10.0.0.0/9", "10.128.0.0/9", false)]
    [InlineData("0.0.0.0/0", "192.0.2.1/32", true)]
    [InlineData("192.0.2.1/32", "0.0.0.0/0", false)]
    [InlineData("0.0.0.0/0", "::/0", false)]
    [InlineData("::/0", "0.0.0.0/0", false)]
    [InlineData("::/0", "2001:db8::/32", true)]
    [InlineData("2001:db8::/32", "2001:db8:1::/48", true)]
    [InlineData("2001:db8:1::/48", "2001:db8::/32", false)]
    [InlineData("2001:db8::/32", "2001:db9::/32", false)]
    public void Covers_means_the_other_prefix_is_nested_in_this_one(string wide, string narrow, bool expected) {
        Assert.Equal(expected, AddressPrefix.Parse(wide).Covers(AddressPrefix.Parse(narrow)));
    }

    [Theory]
    [InlineData("10.0.0.0/8", "10.1.0.0/16", true)]
    [InlineData("10.1.0.0/16", "10.0.0.0/8", true)]
    [InlineData("10.0.0.0/8", "10.0.0.0/8", true)]
    [InlineData("10.0.0.0/8", "11.0.0.0/8", false)]
    [InlineData("10.0.0.0/9", "10.128.0.0/9", false)]
    [InlineData("0.0.0.0/0", "::/0", false)]
    [InlineData("2001:db8::/32", "2001:db8::1/128", true)]
    [InlineData("2001:db8::/32", "2001:db9::/32", false)]
    public void Overlaps_means_nested_either_way(string left, string right, bool expected) {
        Assert.Equal(expected, AddressPrefix.Parse(left).Overlaps(AddressPrefix.Parse(right)));
    }

    [Fact]
    public void Covers_and_overlaps_refuse_null() {
        AddressPrefix prefix = AddressPrefix.Parse("10.0.0.0/8");

        Assert.Throws<ArgumentNullException>(() => prefix.Covers(null!));
        Assert.Throws<ArgumentNullException>(() => prefix.Overlaps(null!));
    }

    [Fact]
    public void Prefixes_are_equal_when_family_length_and_network_agree() {
        Assert.Equal(AddressPrefix.Parse("10.0.0.0/8"), AddressPrefix.Parse("10.0.0.0/8"));
        Assert.Equal(AddressPrefix.Parse("10.0.0.0/8").GetHashCode(), AddressPrefix.Parse("10.0.0.0/8").GetHashCode());
        Assert.NotEqual(AddressPrefix.Parse("10.0.0.0/8"), AddressPrefix.Parse("10.0.0.0/9"));
        Assert.NotEqual(AddressPrefix.Parse("10.0.0.0/8"), AddressPrefix.Parse("11.0.0.0/8"));
    }

    [Fact]
    public void The_all_covering_prefixes_of_the_two_families_are_not_equal() {
        Assert.NotEqual(AddressPrefix.Any(IpVersion.V4), AddressPrefix.Any(IpVersion.V6));
        Assert.False(AddressPrefix.Any(IpVersion.V4).Equals((object)AddressPrefix.Any(IpVersion.V6)));
    }


}
