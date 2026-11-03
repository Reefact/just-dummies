using JustDummies;

using NetworkAcl.Domain;
using NetworkAcl.Tests.Expert.Arbitrary;

using Xunit;

using static NetworkAcl.Tests.Expert.Shorthand;

namespace NetworkAcl.Tests.Expert.Tests;

public sealed class AddressPrefixTests {

    [Fact]
    public void An_ipv4_prefix_exposes_its_family_network_and_length() {
        // Act
        AddressPrefix prefix = new AddressPrefix(V4("10.0.0.0"), 8);

        // Assert
        Assert.Equal(IpVersion.V4, prefix.Version);
        Assert.Equal(8, prefix.PrefixLength);
        Assert.Equal(24, prefix.HostBits);
        Assert.Equal(V4("10.0.0.0"), prefix.Ipv4Network);
        Assert.Null(prefix.Ipv6Network);
    }

    [Fact]
    public void An_ipv6_prefix_exposes_its_family_network_and_length() {
        // Act
        AddressPrefix prefix = new AddressPrefix(V6("2001:db8::"), 32);

        // Assert
        Assert.Equal(IpVersion.V6, prefix.Version);
        Assert.Equal(32, prefix.PrefixLength);
        Assert.Equal(96, prefix.HostBits);
        Assert.Equal(V6("2001:db8::"), prefix.Ipv6Network);
        Assert.Null(prefix.Ipv4Network);
    }

    [Theory]
    [InlineData(0x00000000u, 0)]
    [InlineData(0x80000000u, 1)]
    [InlineData(0x0A000000u, 8)]
    [InlineData(0xC0A80180u, 25)]
    [InlineData(0x0A000000u, 31)]
    [InlineData(0x0A000001u, 32)]   // a /32 may carry any address
    [InlineData(0xFFFFFFFFu, 32)]
    public void An_ipv4_prefix_accepts_a_network_whose_host_bits_are_zero(uint network, int length) {
        // Act
        AddressPrefix prefix = new AddressPrefix(new Ipv4Address(network), length);

        // Assert
        Assert.Equal(length, prefix.PrefixLength);
    }

    [Theory]
    [InlineData(0x01000000u, 0)]    // 1.0.0.0/0
    [InlineData(0x0A010000u, 8)]    // 10.1.0.0/8
    [InlineData(0x0A000001u, 31)]   // the odd address of a /31
    [InlineData(0xC0A80101u, 25)]   // 192.168.1.1/25
    public void An_ipv4_prefix_refuses_a_network_with_a_host_bit_set(uint network, int length) {
        Assert.Throws<ArgumentException>(() => new AddressPrefix(new Ipv4Address(network), length));
    }

    [Fact]
    public void An_ipv4_prefix_with_a_host_bit_set_is_refused_with_an_argument_exception_that_names_the_network() {
        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => new AddressPrefix(V4("10.1.0.0"), 8));

        // Assert
        Assert.Equal("network", refusal.ParamName);
        Assert.Contains("10.0.0.0", refusal.Message);   // the message offers the network the caller probably meant
    }

    [Theory]
    [InlineData(0x0000000000000000UL, 0x0000000000000001UL, 127)]   // ::1/127
    [InlineData(0x20010db800000000UL, 0x0000000000000001UL, 32)]    // 2001:db8::1/32
    [InlineData(0x0000000000000001UL, 0x0000000000000000UL, 0)]     // anything but :: is refused at /0
    [InlineData(0x8000000000000000UL, 0x0000000000000001UL, 1)]
    public void An_ipv6_prefix_refuses_a_network_with_a_host_bit_set(ulong high, ulong low, int length) {
        Assert.Throws<ArgumentException>(() => new AddressPrefix(new Ipv6Address(Bits(high, low)), length));
    }

    [Theory]
    [InlineData(0x0000000000000000UL, 0x0000000000000000UL, 0)]
    [InlineData(0x8000000000000000UL, 0x0000000000000000UL, 1)]
    [InlineData(0x20010db800000000UL, 0x0000000000000000UL, 32)]
    [InlineData(0x0000000000000000UL, 0x0000000000000001UL, 128)]   // a /128 may carry any address
    [InlineData(0xffffffffffffffffUL, 0xffffffffffffffffUL, 128)]
    public void An_ipv6_prefix_accepts_a_network_whose_host_bits_are_zero(ulong high, ulong low, int length) {
        // Act
        AddressPrefix prefix = new AddressPrefix(new Ipv6Address(Bits(high, low)), length);

        // Assert
        Assert.Equal(length, prefix.PrefixLength);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(33)]
    [InlineData(int.MinValue)]
    [InlineData(int.MaxValue)]
    public void An_ipv4_prefix_length_is_between_0_and_32(int length) {
        // Arrange
        Ipv4Address anyAddress = Any.Ipv4Address().Generate();

        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => new AddressPrefix(anyAddress, length));
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(129)]
    [InlineData(int.MinValue)]
    [InlineData(int.MaxValue)]
    public void An_ipv6_prefix_length_is_between_0_and_128(int length) {
        // Arrange
        Ipv6Address anyAddress = Any.Ipv6Address().Generate();

        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => new AddressPrefix(anyAddress, length));
    }

    [Fact]
    public void The_length_is_checked_before_the_host_bits() {
        // Arrange: both rules are broken (host bits set, length too long); the length is reported.
        Ipv4Address withHostBits = V4("10.0.0.1");

        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => new AddressPrefix(withHostBits, 33));
    }

    [Fact]
    public void A_missing_network_is_refused() {
        Assert.Throws<ArgumentNullException>(() => new AddressPrefix((Ipv4Address)null!, 8));
        Assert.Throws<ArgumentNullException>(() => new AddressPrefix((Ipv6Address)null!, 8));
    }

    [Theory]
    [InlineData("10.0.0.0/8", IpVersion.V4, 8)]
    [InlineData("0.0.0.0/0", IpVersion.V4, 0)]
    [InlineData("192.168.1.1/32", IpVersion.V4, 32)]
    [InlineData("2001:db8::/32", IpVersion.V6, 32)]
    [InlineData("::/0", IpVersion.V6, 0)]
    [InlineData("::1/128", IpVersion.V6, 128)]
    [InlineData("::ffff:192.0.2.1/128", IpVersion.V6, 128)]
    public void Parse_tells_the_family_by_the_colon_and_reads_the_length(string text, IpVersion expectedVersion, int expectedLength) {
        // Act
        AddressPrefix prefix = AddressPrefix.Parse(text);

        // Assert
        Assert.Equal(expectedVersion, prefix.Version);
        Assert.Equal(expectedLength, prefix.PrefixLength);
    }

    [Theory]
    [InlineData("10.0.0.0/8", "10.0.0.0/8")]
    [InlineData("2001:0DB8:0:0:0:0:0:0/32", "2001:db8::/32")]
    [InlineData("10.0.0.0/008", "10.0.0.0/8")]   // a length is 1 to 3 digits, leading zeros included
    public void A_parsed_prefix_is_written_back_in_canonical_form(string text, string expectedText) {
        Assert.Equal(expectedText, AddressPrefix.Parse(text).ToString());
    }

    [Theory]
    [InlineData("")]
    [InlineData("10.0.0.0")]
    [InlineData("10.0.0.0/")]
    [InlineData("/8")]
    [InlineData("10.0.0.0/8/8")]
    [InlineData("10.0.0.0/a")]
    [InlineData("10.0.0.0/-1")]
    [InlineData("10.0.0.0/+8")]
    [InlineData("10.0.0.0/ 8")]
    [InlineData("10.0.0.0/8\n")]
    [InlineData("10.0.0.0/1234")]
    [InlineData("10.0.0/8")]
    [InlineData("010.0.0.0/8")]
    [InlineData("2001:db8::1::/64")]
    [InlineData("::/")]
    public void Parse_refuses_text_that_is_not_address_slash_length(string text) {
        Assert.Throws<FormatException>(() => AddressPrefix.Parse(text));
    }

    [Fact]
    public void Parse_lets_the_host_bits_refusal_through() {
        Assert.Throws<ArgumentException>(() => AddressPrefix.Parse("10.1.0.0/8"));
    }

    [Theory]
    [InlineData("10.0.0.0/33")]
    [InlineData("10.0.0.0/999")]
    [InlineData("::/129")]
    public void Parse_lets_the_length_range_refusal_through(string text) {
        Assert.Throws<ArgumentOutOfRangeException>(() => AddressPrefix.Parse(text));
    }

    [Fact]
    public void Parse_refuses_a_missing_text() {
        Assert.Throws<ArgumentNullException>(() => AddressPrefix.Parse(null!));
    }

    [Fact]
    public void Any_gives_the_zero_length_prefix_of_each_family() {
        Assert.Equal(Prefix("0.0.0.0/0"), AddressPrefix.Any(IpVersion.V4));
        Assert.Equal(Prefix("::/0"), AddressPrefix.Any(IpVersion.V6));
        Assert.True(AddressPrefix.Any(IpVersion.V4).IsAnyAddress);
        Assert.False(Prefix("0.0.0.0/1").IsAnyAddress);
    }

    [Fact]
    public void Any_refuses_an_undefined_family() {
        Assert.Throws<ArgumentOutOfRangeException>(() => AddressPrefix.Any((IpVersion)5));
    }

    [Fact]
    public void Host_gives_the_full_length_prefix_of_one_address() {
        Assert.Equal(Prefix("10.1.2.3/32"), AddressPrefix.Host(V4("10.1.2.3")));
        Assert.Equal(Prefix("2001:db8::7/128"), AddressPrefix.Host(V6("2001:db8::7")));
        Assert.Equal(0, AddressPrefix.Host(V4("10.1.2.3")).HostBits);
    }

    [Theory]
    [InlineData("10.0.0.0", true)]
    [InlineData("10.0.0.1", true)]
    [InlineData("10.255.255.255", true)]
    [InlineData("9.255.255.255", false)]
    [InlineData("11.0.0.0", false)]
    public void An_ipv4_prefix_contains_the_addresses_between_its_first_and_last(string address, bool expected) {
        Assert.Equal(expected, Prefix("10.0.0.0/8").Contains(V4(address)));
    }

    [Theory]
    [InlineData("192.168.1.127", false)]
    [InlineData("192.168.1.128", true)]
    [InlineData("192.168.1.255", true)]
    [InlineData("192.168.2.0", false)]
    public void A_prefix_ending_inside_an_octet_cuts_at_its_bit(string address, bool expected) {
        Assert.Equal(expected, Prefix("192.168.1.128/25").Contains(V4(address)));
    }

    [Theory]
    [InlineData("2001:db8::", true)]
    [InlineData("2001:db8:ffff:ffff:ffff:ffff:ffff:ffff", true)]
    [InlineData("2001:db7:ffff:ffff:ffff:ffff:ffff:ffff", false)]
    [InlineData("2001:db9::", false)]
    public void An_ipv6_prefix_contains_the_addresses_between_its_first_and_last(string address, bool expected) {
        Assert.Equal(expected, Prefix("2001:db8::/32").Contains(V6(address)));
    }

    [Fact]
    public void The_zero_length_prefix_contains_any_address_of_its_family() {
        // Arrange
        Ipv4Address anyV4 = Any.Ipv4Address().Generate();
        Ipv6Address anyV6 = Any.Ipv6Address().Generate();

        // Act & Assert
        Assert.True(AddressPrefix.Any(IpVersion.V4).Contains(anyV4));
        Assert.True(AddressPrefix.Any(IpVersion.V6).Contains(anyV6));
    }

    [Fact]
    public void A_host_prefix_contains_its_address_and_not_the_next_one() {
        // Arrange
        AddressPrefix host = AddressPrefix.Host(V4("10.0.0.7"));

        // Act & Assert
        Assert.True(host.Contains(V4("10.0.0.7")));
        Assert.False(host.Contains(V4("10.0.0.6")));
        Assert.False(host.Contains(V4("10.0.0.8")));
    }

    [Fact]
    public void A_prefix_never_contains_an_address_of_the_other_family_not_even_the_zero_length_one() {
        // Arrange
        Ipv4Address anyV4 = Any.Ipv4Address().Generate();
        Ipv6Address anyV6 = Any.Ipv6Address().Generate();

        // Act & Assert
        Assert.False(AddressPrefix.Any(IpVersion.V4).Contains(anyV6));
        Assert.False(AddressPrefix.Any(IpVersion.V6).Contains(anyV4));
    }

    [Fact]
    public void Contains_refuses_a_missing_address() {
        Assert.Throws<ArgumentNullException>(() => Prefix("10.0.0.0/8").Contains((Ipv4Address)null!));
        Assert.Throws<ArgumentNullException>(() => Prefix("::/0").Contains((Ipv6Address)null!));
    }

    [Theory]
    [InlineData("10.0.0.0/8", "10.0.0.0/8", true)]       // itself
    [InlineData("10.0.0.0/8", "10.1.0.0/16", true)]      // nested
    [InlineData("10.0.0.0/8", "10.255.255.255/32", true)]
    [InlineData("10.1.0.0/16", "10.0.0.0/8", false)]     // the wider one is not covered
    [InlineData("10.0.0.0/9", "10.128.0.0/9", false)]    // siblings
    [InlineData("10.0.0.0/8", "11.0.0.0/8", false)]      // disjoint
    [InlineData("0.0.0.0/0", "192.168.0.0/16", true)]
    [InlineData("2001:db8::/32", "2001:db8:1::/48", true)]
    [InlineData("2001:db8:1::/48", "2001:db8::/32", false)]
    [InlineData("::/0", "2001:db8::/32", true)]
    public void Covers_means_the_other_prefix_is_nested_inside_this_one(string covering, string covered, bool expected) {
        Assert.Equal(expected, Prefix(covering).Covers(Prefix(covered)));
    }

    [Fact]
    public void A_prefix_never_covers_a_prefix_of_the_other_family_even_the_zero_length_one() {
        // Arrange
        AddressPrefix anyV6 = Any.AddressPrefix().OfFamily(IpVersion.V6).Generate();
        AddressPrefix anyV4 = Any.AddressPrefix().OfFamily(IpVersion.V4).Generate();

        // Act & Assert
        Assert.False(AddressPrefix.Any(IpVersion.V4).Covers(anyV6));
        Assert.False(AddressPrefix.Any(IpVersion.V6).Covers(anyV4));
        Assert.False(AddressPrefix.Any(IpVersion.V4).Covers(AddressPrefix.Any(IpVersion.V6)));
    }

    [Fact]
    public void The_zero_length_prefix_covers_any_prefix_of_its_family() {
        // Arrange
        AddressPrefix anyV4 = Any.AddressPrefix().OfFamily(IpVersion.V4).Generate();
        AddressPrefix anyV6 = Any.AddressPrefix().OfFamily(IpVersion.V6).Generate();

        // Act & Assert
        Assert.True(AddressPrefix.Any(IpVersion.V4).Covers(anyV4));
        Assert.True(AddressPrefix.Any(IpVersion.V6).Covers(anyV6));
    }

    [Theory]
    [InlineData("10.0.0.0/8", "10.1.0.0/16", true)]
    [InlineData("10.1.0.0/16", "10.0.0.0/8", true)]      // nesting counts in both directions
    [InlineData("10.0.0.0/8", "10.0.0.0/8", true)]
    [InlineData("10.0.0.0/9", "10.128.0.0/9", false)]    // adjacent siblings share nothing
    [InlineData("10.0.0.0/8", "192.168.0.0/16", false)]
    [InlineData("2001:db8::/32", "2001:db8:1::/48", true)]
    [InlineData("2001:db8::/33", "2001:db8:8000::/33", false)]
    public void Overlaps_means_one_prefix_nests_inside_the_other(string first, string second, bool expected) {
        Assert.Equal(expected, Prefix(first).Overlaps(Prefix(second)));
    }

    [Fact]
    public void Prefixes_of_different_families_do_not_overlap() {
        // Arrange
        AddressPrefix anyV6 = Any.AddressPrefix().OfFamily(IpVersion.V6).Generate();

        // Act & Assert
        Assert.False(AddressPrefix.Any(IpVersion.V4).Overlaps(anyV6));
        Assert.False(anyV6.Overlaps(AddressPrefix.Any(IpVersion.V4)));
    }

    [Fact]
    public void Covers_and_Overlaps_refuse_a_missing_prefix() {
        Assert.Throws<ArgumentNullException>(() => Prefix("10.0.0.0/8").Covers(null!));
        Assert.Throws<ArgumentNullException>(() => Prefix("10.0.0.0/8").Overlaps(null!));
    }

    [Fact]
    public void Prefixes_with_the_same_family_network_and_length_are_equal_and_hash_alike() {
        // Arrange
        AddressPrefix parsed      = Prefix("10.0.0.0/8");
        AddressPrefix constructed = new AddressPrefix(new Ipv4Address(0x0A000000u), 8);

        // Act & Assert
        Assert.Equal(parsed, constructed);
        Assert.True(parsed.Equals((object)constructed));
        Assert.Equal(parsed.GetHashCode(), constructed.GetHashCode());
    }

    [Fact]
    public void Prefixes_differing_by_length_or_family_are_not_equal() {
        // Arrange
        AddressPrefix tenSlash8 = Prefix("10.0.0.0/8");

        // Act & Assert
        Assert.NotEqual(tenSlash8, Prefix("10.0.0.0/9"));
        Assert.NotEqual(Prefix("0.0.0.0/0"), Prefix("::/0"));
        Assert.False(tenSlash8.Equals(null));
    }

}
