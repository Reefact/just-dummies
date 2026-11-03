using JustDummies;

using NetworkAcl.Domain;

using Xunit;

namespace NetworkAcl.Tests.Regular;

public sealed class AddressPrefixTests {

    [Fact]
    public void A_prefix_with_host_bits_set_is_refused() {
        // Arrange
        AddressPrefix anyPrefix = Any.AddressPrefix().OfVersion(IpVersion.V4).WithLengthBetween(0, 31).Generate();
        Ipv4Address   dirty     = new Ipv4Address(anyPrefix.Ipv4Network!.Value | 1u);

        // Act
        Exception? thrown = Record.Exception(() => new AddressPrefix(dirty, anyPrefix.PrefixLength));

        // Assert
        Assert.IsType<ArgumentException>(thrown);
    }

    [Fact]
    public void An_ipv6_prefix_with_host_bits_set_is_refused() {
        // Arrange
        AddressPrefix anyPrefix = Any.AddressPrefix().OfVersion(IpVersion.V6).WithLengthBetween(0, 127).Generate();
        Ipv6Address   dirty     = new Ipv6Address(anyPrefix.Ipv6Network!.Value | UInt128.One);

        // Act
        Exception? thrown = Record.Exception(() => new AddressPrefix(dirty, anyPrefix.PrefixLength));

        // Assert
        Assert.IsType<ArgumentException>(thrown);
    }

    [Fact]
    public void Ten_dot_one_slash_eight_has_host_bits() {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => new AddressPrefix(Ipv4Address.Parse("10.1.0.0"), 8));
        Assert.Equal("10.0.0.0/8", new AddressPrefix(Ipv4Address.Parse("10.0.0.0"), 8).ToString());
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(33)]
    [InlineData(int.MaxValue)]
    [InlineData(int.MinValue)]
    public void An_ipv4_length_outside_0_to_32_is_out_of_range(int length) {
        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => new AddressPrefix(new Ipv4Address(0), length));
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(129)]
    [InlineData(int.MaxValue)]
    public void An_ipv6_length_outside_0_to_128_is_out_of_range(int length) {
        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => new AddressPrefix(new Ipv6Address(UInt128.Zero), length));
    }

    [Fact]
    public void The_length_is_checked_before_the_host_bits() {
        // Arrange
        Ipv4Address anyDirtyAddress = Any.UInt32().GreaterThan(0u).As(value => new Ipv4Address(value)).Generate();

        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => new AddressPrefix(anyDirtyAddress, 33));
    }

    [Fact]
    public void Nulls_are_refused() {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new AddressPrefix((Ipv4Address)null!, 8));
        Assert.Throws<ArgumentNullException>(() => new AddressPrefix((Ipv6Address)null!, 8));
    }

    [Fact]
    public void Every_length_of_a_clean_network_is_accepted() {
        // Arrange
        AddressPrefix anyV4 = Any.AddressPrefix().OfVersion(IpVersion.V4).Generate();
        AddressPrefix anyV6 = Any.AddressPrefix().OfVersion(IpVersion.V6).Generate();

        // Assert
        Assert.Equal(IpVersion.V4, anyV4.Version);
        Assert.Equal(IpVersion.V6, anyV6.Version);
        Assert.Equal(32 - anyV4.PrefixLength, anyV4.HostBits);
        Assert.Equal(128 - anyV6.PrefixLength, anyV6.HostBits);
    }

    [Fact]
    public void The_network_of_the_other_family_is_null() {
        // Arrange
        AddressPrefix anyV4 = Any.AddressPrefix().OfVersion(IpVersion.V4).Generate();
        AddressPrefix anyV6 = Any.AddressPrefix().OfVersion(IpVersion.V6).Generate();

        // Assert
        Assert.NotNull(anyV4.Ipv4Network);
        Assert.Null(anyV4.Ipv6Network);
        Assert.NotNull(anyV6.Ipv6Network);
        Assert.Null(anyV6.Ipv4Network);
    }

    [Fact]
    public void A_prefix_written_then_parsed_is_the_same_prefix() {
        // Arrange
        AddressPrefix anyPrefix = Any.AddressPrefix().Generate();

        // Act
        AddressPrefix parsed = AddressPrefix.Parse(anyPrefix.ToString());

        // Assert
        Assert.Equal(anyPrefix, parsed);
        Assert.Equal(anyPrefix.GetHashCode(), parsed.GetHashCode());
    }

    [Theory]
    [InlineData("10.0.0.0/8", IpVersion.V4, 8)]
    [InlineData("0.0.0.0/0", IpVersion.V4, 0)]
    [InlineData("192.0.2.1/32", IpVersion.V4, 32)]
    [InlineData("2001:db8::/32", IpVersion.V6, 32)]
    [InlineData("::/0", IpVersion.V6, 0)]
    [InlineData("::1/128", IpVersion.V6, 128)]
    [InlineData("::ffff:10.0.0.0/104", IpVersion.V6, 104)]
    [InlineData("10.0.0.0/008", IpVersion.V4, 8)]
    public void Parse_reads_the_family_from_the_address(string text, IpVersion version, int length) {
        // Act
        AddressPrefix prefix = AddressPrefix.Parse(text);

        // Assert
        Assert.Equal(version, prefix.Version);
        Assert.Equal(length, prefix.PrefixLength);
    }

    [Theory]
    [InlineData("")]
    [InlineData("10.0.0.0")]
    [InlineData("/8")]
    [InlineData("10.0.0.0/")]
    [InlineData("10.0.0.0/8/8")]
    [InlineData("10.0.0.0/1234")]
    [InlineData("10.0.0.0/-1")]
    [InlineData("10.0.0.0/+8")]
    [InlineData("10.0.0.0/ 8")]
    [InlineData("10.0.0.0/8a")]
    [InlineData("10.0.0/8")]
    [InlineData("010.0.0.0/8")]
    [InlineData("2001:db8::")]
    [InlineData("2001:db8::1::/32")]
    [InlineData("::1%eth0/128")]
    public void Parse_refuses_a_syntax_error(string text) {
        // Act
        Exception? thrown = Record.Exception(() => AddressPrefix.Parse(text));

        // Assert
        Assert.IsType<FormatException>(thrown);
    }

    [Fact]
    public void Parse_lets_the_constructor_refusals_through() {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => AddressPrefix.Parse("10.1.0.0/8"));
        Assert.Throws<ArgumentException>(() => AddressPrefix.Parse("2001:db8::1/32"));
        Assert.Throws<ArgumentOutOfRangeException>(() => AddressPrefix.Parse("10.0.0.0/33"));
        Assert.Throws<ArgumentOutOfRangeException>(() => AddressPrefix.Parse("::/129"));
        Assert.Throws<ArgumentOutOfRangeException>(() => AddressPrefix.Parse("10.0.0.0/999"));
    }

    [Fact]
    public void Parse_refuses_null() {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => AddressPrefix.Parse(null!));
    }

    [Theory]
    [InlineData(IpVersion.V4, "0.0.0.0/0")]
    [InlineData(IpVersion.V6, "::/0")]
    public void Any_is_the_prefix_of_length_zero(IpVersion version, string text) {
        // Act
        AddressPrefix everything = AddressPrefix.Any(version);

        // Assert
        Assert.Equal(text, everything.ToString());
        Assert.True(everything.IsAnyAddress);
        Assert.Equal(AddressPrefix.Parse(text), everything);
    }

    [Fact]
    public void Any_refuses_an_unknown_version() {
        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => AddressPrefix.Any((IpVersion)5));
    }

    [Fact]
    public void A_host_prefix_is_the_whole_address() {
        // Arrange
        Ipv4Address anyV4 = Any.Ipv4Address().Generate();
        Ipv6Address anyV6 = Any.Ipv6Address().Generate();

        // Act
        AddressPrefix host4 = AddressPrefix.Host(anyV4);
        AddressPrefix host6 = AddressPrefix.Host(anyV6);

        // Assert
        Assert.Equal(32, host4.PrefixLength);
        Assert.Equal(0, host4.HostBits);
        Assert.Equal(anyV4, host4.Ipv4Network);
        Assert.Equal(128, host6.PrefixLength);
        Assert.Equal(anyV6, host6.Ipv6Network);
        Assert.False(host4.IsAnyAddress);
    }

    [Fact]
    public void A_prefix_contains_the_addresses_that_share_its_network_bits() {
        // Arrange
        AddressPrefix anyPrefix = Any.AddressPrefix().OfVersion(IpVersion.V4).WithLengthBetween(1, 31).Generate();
        uint          anyHost   = Any.UInt32().Generate();
        uint          hostMask  = uint.MaxValue >> anyPrefix.PrefixLength;

        // Act
        Ipv4Address inside  = new Ipv4Address(anyPrefix.Ipv4Network!.Value | (anyHost & hostMask));
        Ipv4Address outside = new Ipv4Address(anyPrefix.Ipv4Network!.Value ^ (0x8000_0000u >> (anyPrefix.PrefixLength - 1)));

        // Assert
        Assert.True(anyPrefix.Contains(inside));
        Assert.False(anyPrefix.Contains(outside));
    }

    [Fact]
    public void An_ipv6_prefix_contains_the_addresses_that_share_its_network_bits() {
        // Arrange
        AddressPrefix anyPrefix = Any.AddressPrefix().OfVersion(IpVersion.V6).WithLengthBetween(1, 127).Generate();
        UInt128       anyHost   = Any.UInt128().Generate();
        UInt128       hostMask  = UInt128.MaxValue >> anyPrefix.PrefixLength;

        // Act
        Ipv6Address inside  = new Ipv6Address(anyPrefix.Ipv6Network!.Value | (anyHost & hostMask));
        Ipv6Address outside = new Ipv6Address(anyPrefix.Ipv6Network!.Value ^ ((UInt128)1 << (128 - anyPrefix.PrefixLength)));

        // Assert
        Assert.True(anyPrefix.Contains(inside));
        Assert.False(anyPrefix.Contains(outside));
    }

    [Fact]
    public void A_host_prefix_contains_only_its_address() {
        // Arrange
        uint        anyBits  = Any.UInt32().LessThan(uint.MaxValue).Generate();
        Ipv4Address address  = new Ipv4Address(anyBits);
        Ipv4Address neighbor = new Ipv4Address(anyBits + 1);

        // Act
        AddressPrefix host = AddressPrefix.Host(address);

        // Assert
        Assert.True(host.Contains(address));
        Assert.False(host.Contains(neighbor));
    }

    [Fact]
    public void The_any_prefix_contains_every_address_of_its_family_and_none_of_the_other() {
        // Arrange
        Ipv4Address anyV4 = Any.Ipv4Address().Generate();
        Ipv6Address anyV6 = Any.Ipv6Address().Generate();

        // Assert
        Assert.True(AddressPrefix.Any(IpVersion.V4).Contains(anyV4));
        Assert.False(AddressPrefix.Any(IpVersion.V4).Contains(anyV6));
        Assert.True(AddressPrefix.Any(IpVersion.V6).Contains(anyV6));
        Assert.False(AddressPrefix.Any(IpVersion.V6).Contains(anyV4));
    }

    [Fact]
    public void Contains_refuses_null() {
        // Arrange
        AddressPrefix anyPrefix = Any.AddressPrefix().Generate();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => anyPrefix.Contains((Ipv4Address)null!));
        Assert.Throws<ArgumentNullException>(() => anyPrefix.Contains((Ipv6Address)null!));
    }

    [Theory]
    [InlineData("10.0.0.0/8", "10.0.0.0/8", true, true)]
    [InlineData("10.0.0.0/8", "10.1.0.0/16", true, true)]
    [InlineData("10.1.0.0/16", "10.0.0.0/8", false, true)]
    [InlineData("10.0.0.0/8", "11.0.0.0/8", false, false)]
    [InlineData("10.0.0.0/9", "10.128.0.0/9", false, false)]
    [InlineData("10.1.2.3/32", "10.1.2.0/24", false, true)]
    [InlineData("0.0.0.0/0", "255.255.255.255/32", true, true)]
    [InlineData("0.0.0.0/0", "0.0.0.0/0", true, true)]
    [InlineData("128.0.0.0/1", "0.0.0.0/1", false, false)]
    [InlineData("2001:db8::/32", "2001:db8:1::/48", true, true)]
    [InlineData("2001:db8:1::/48", "2001:db8::/32", false, true)]
    [InlineData("2001:db8::/32", "2001:db9::/32", false, false)]
    [InlineData("::/0", "ff00::/8", true, true)]
    [InlineData("::1/128", "::/0", false, true)]
    public void Prefixes_cover_what_they_nest_and_overlap_when_nested_either_way(string left, string right, bool covers, bool overlaps) {
        // Arrange
        AddressPrefix a = AddressPrefix.Parse(left);
        AddressPrefix b = AddressPrefix.Parse(right);

        // Assert
        Assert.Equal(covers, a.Covers(b));
        Assert.Equal(overlaps, a.Overlaps(b));
        Assert.Equal(overlaps, b.Overlaps(a));
    }

    [Theory]
    [InlineData("0.0.0.0/0", "::/0")]
    [InlineData("::/0", "0.0.0.0/0")]
    [InlineData("0.0.0.0/0", "::ffff:0.0.0.0/96")]
    [InlineData("10.0.0.0/8", "::a00:0/104")]
    public void Prefixes_of_different_families_never_cover_nor_overlap(string left, string right) {
        // Arrange
        AddressPrefix a = AddressPrefix.Parse(left);
        AddressPrefix b = AddressPrefix.Parse(right);

        // Assert
        Assert.False(a.Covers(b));
        Assert.False(a.Overlaps(b));
    }

    [Fact]
    public void A_prefix_covers_and_overlaps_itself() {
        // Arrange
        AddressPrefix anyPrefix = Any.AddressPrefix().Generate();

        // Assert
        Assert.True(anyPrefix.Covers(anyPrefix));
        Assert.True(anyPrefix.Overlaps(anyPrefix));
    }

    [Fact]
    public void The_any_prefix_covers_every_prefix_of_its_family() {
        // Arrange
        AddressPrefix anyPrefix = Any.AddressPrefix().Generate();

        // Assert
        Assert.True(AddressPrefix.Any(anyPrefix.Version).Covers(anyPrefix));
    }

    [Fact]
    public void Overlap_is_symmetric() {
        // Arrange
        AddressPrefix anyLeft  = Any.AddressPrefix().OfVersion(IpVersion.V4).WithLengthBetween(0, 12).Generate();
        AddressPrefix anyRight = Any.AddressPrefix().OfVersion(IpVersion.V4).WithLengthBetween(0, 12).Generate();

        // Assert
        Assert.Equal(anyLeft.Overlaps(anyRight), anyRight.Overlaps(anyLeft));
    }

    [Fact]
    public void Prefixes_are_equal_by_family_length_and_network() {
        // Arrange
        AddressPrefix anyPrefix = Any.AddressPrefix().Generate();
        AddressPrefix sameAgain = AddressPrefix.Parse(anyPrefix.ToString());

        // Assert
        Assert.True(anyPrefix.Equals(sameAgain));
        Assert.False(anyPrefix.Equals(null));
        Assert.False(AddressPrefix.Parse("0.0.0.0/0").Equals(AddressPrefix.Parse("::/0")));
    }

    [Fact]
    public void Covers_and_Overlaps_refuse_null() {
        // Arrange
        AddressPrefix anyPrefix = Any.AddressPrefix().Generate();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => anyPrefix.Covers(null!));
        Assert.Throws<ArgumentNullException>(() => anyPrefix.Overlaps(null!));
    }

}
