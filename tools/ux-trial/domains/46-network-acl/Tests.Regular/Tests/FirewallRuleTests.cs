using JustDummies;

using NetworkAcl.Domain;

using Xunit;

namespace NetworkAcl.Tests.Regular;

public sealed class FirewallRuleTests {

    private static AddressPrefix P(string text) {
        return AddressPrefix.Parse(text);
    }

    private static FirewallRule Build(RuleAction action, string source, string destination, byte? protocol, PortRange? sourcePorts = null, PortRange? destinationPorts = null, TcpFlagMatch? flags = null) {
        return new FirewallRule(Any.RuleId().Generate(), action, P(source), P(destination), protocol, sourcePorts ?? PortRange.All, destinationPorts ?? PortRange.All, flags);
    }

    [Fact]
    public void A_rule_cannot_mix_families() {
        // Arrange
        RuleId anyId = Any.RuleId().Generate();

        // Act
        ArgumentException v4ToV6 = Assert.Throws<ArgumentException>(() => new FirewallRule(anyId, RuleAction.Allow, P("10.0.0.0/8"), P("::/0"), null, PortRange.All, PortRange.All, null));
        ArgumentException v6ToV4 = Assert.Throws<ArgumentException>(() => new FirewallRule(anyId, RuleAction.Allow, P("::/0"), P("10.0.0.0/8"), null, PortRange.All, PortRange.All, null));

        // Assert
        Assert.Equal("destination", v4ToV6.ParamName);
        Assert.Equal("destination", v6ToV4.ParamName);
    }

    [Fact]
    public void A_rule_is_of_the_family_of_its_prefixes() {
        // Arrange
        FirewallRule anyV4 = Any.FirewallRule().OfVersion(IpVersion.V4).Generate();
        FirewallRule anyV6 = Any.FirewallRule().OfVersion(IpVersion.V6).Generate();

        // Assert
        Assert.Equal(IpVersion.V4, anyV4.Version);
        Assert.Equal(IpVersion.V6, anyV6.Version);
        Assert.Equal(IpVersion.V4, anyV4.Source.Version);
        Assert.Equal(IpVersion.V6, anyV6.Destination.Version);
    }

    [Fact]
    public void The_reserved_protocol_is_refused() {
        // Arrange
        RuleId anyId = Any.RuleId().Generate();

        // Act
        ArgumentOutOfRangeException thrown = Assert.Throws<ArgumentOutOfRangeException>(() => new FirewallRule(anyId, RuleAction.Deny, P("10.0.0.0/8"), P("10.0.0.0/8"), 255, PortRange.All, PortRange.All, null));

        // Assert
        Assert.Equal("protocol", thrown.ParamName);
    }

    [Fact]
    public void Icmpv6_does_not_run_over_an_ipv4_rule_nor_icmp_over_an_ipv6_one() {
        // Arrange
        RuleId anyId = Any.RuleId().Generate();

        // Act & Assert
        Assert.Throws<ArgumentException>(() => new FirewallRule(anyId, RuleAction.Allow, P("10.0.0.0/8"), P("10.0.0.0/8"), IpProtocols.Icmpv6, PortRange.All, PortRange.All, null));
        Assert.Throws<ArgumentException>(() => new FirewallRule(anyId, RuleAction.Allow, P("::/0"), P("::/0"), IpProtocols.Icmp, PortRange.All, PortRange.All, null));
        Assert.Equal(IpProtocols.Icmp, Build(RuleAction.Allow, "0.0.0.0/0", "0.0.0.0/0", IpProtocols.Icmp).Protocol);
        Assert.Equal(IpProtocols.Icmpv6, Build(RuleAction.Allow, "::/0", "::/0", IpProtocols.Icmpv6).Protocol);
    }

    [Fact]
    public void The_family_is_checked_before_the_protocol() {
        // Arrange
        RuleId anyId = Any.RuleId().Generate();

        // Act
        ArgumentException thrown = Assert.Throws<ArgumentException>(() => new FirewallRule(anyId, RuleAction.Allow, P("10.0.0.0/8"), P("::/0"), 255, PortRange.All, PortRange.All, null));

        // Assert
        Assert.Equal("destination", thrown.ParamName);
    }

    [Theory]
    [InlineData(null)]
    [InlineData(IpProtocols.Icmp)]
    [InlineData((byte)47)]
    public void Ports_can_only_be_narrowed_for_a_port_bearing_protocol(byte? protocol) {
        // Arrange
        RuleId    anyId   = Any.RuleId().Generate();
        PortRange anyPort = PortRange.Single(Any.UInt16().Generate());

        // Act
        ArgumentException sourceNarrowed = Assert.Throws<ArgumentException>(() => new FirewallRule(anyId, RuleAction.Allow, P("10.0.0.0/8"), P("10.0.0.0/8"), protocol, anyPort, PortRange.All, null));
        ArgumentException destinationNarrowed = Assert.Throws<ArgumentException>(() => new FirewallRule(anyId, RuleAction.Allow, P("10.0.0.0/8"), P("10.0.0.0/8"), protocol, PortRange.All, anyPort, null));

        // Assert
        Assert.Equal("protocol", sourceNarrowed.ParamName);
        Assert.Equal("protocol", destinationNarrowed.ParamName);
    }

    [Theory]
    [InlineData(IpProtocols.Tcp)]
    [InlineData(IpProtocols.Udp)]
    [InlineData(IpProtocols.Sctp)]
    public void Ports_can_be_narrowed_for_tcp_udp_and_sctp(byte protocol) {
        // Arrange
        PortRange anyRange = Any.PortRange().Generate();

        // Act
        FirewallRule rule = Build(RuleAction.Allow, "10.0.0.0/8", "10.0.0.0/8", protocol, anyRange, anyRange);

        // Assert
        Assert.Equal(anyRange, rule.SourcePorts);
        Assert.Equal(anyRange, rule.DestinationPorts);
    }

    [Theory]
    [InlineData(null)]
    [InlineData(IpProtocols.Udp)]
    [InlineData(IpProtocols.Icmp)]
    public void A_flags_test_needs_protocol_tcp(byte? protocol) {
        // Arrange
        RuleId anyId = Any.RuleId().Generate();

        // Act
        ArgumentException thrown = Assert.Throws<ArgumentException>(() => new FirewallRule(anyId, RuleAction.Allow, P("10.0.0.0/8"), P("10.0.0.0/8"), protocol, PortRange.All, PortRange.All, TcpFlagMatch.Established));

        // Assert
        Assert.Equal("tcpFlags", thrown.ParamName);
    }

    [Fact]
    public void A_tcp_rule_may_carry_a_flags_test() {
        // Act
        FirewallRule rule = Build(RuleAction.Allow, "10.0.0.0/8", "10.0.0.0/8", IpProtocols.Tcp, flags: TcpFlagMatch.NewConnection);

        // Assert
        Assert.Equal(TcpFlagMatch.NewConnection, rule.TcpFlags);
    }

    [Fact]
    public void An_undefined_action_is_refused() {
        // Arrange
        RuleAction undefined = (RuleAction)Any.Int32().Between(2, 100).Generate();
        RuleId     anyId     = Any.RuleId().Generate();

        // Act
        ArgumentOutOfRangeException thrown = Assert.Throws<ArgumentOutOfRangeException>(() => new FirewallRule(anyId, undefined, P("10.0.0.0/8"), P("10.0.0.0/8"), null, PortRange.All, PortRange.All, null));

        // Assert
        Assert.Equal("action", thrown.ParamName);
    }

    [Fact]
    public void Missing_parts_are_refused() {
        // Arrange
        RuleId anyId = Any.RuleId().Generate();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new FirewallRule(null!, RuleAction.Allow, P("10.0.0.0/8"), P("10.0.0.0/8"), null, PortRange.All, PortRange.All, null));
        Assert.Throws<ArgumentNullException>(() => new FirewallRule(anyId, RuleAction.Allow, null!, P("10.0.0.0/8"), null, PortRange.All, PortRange.All, null));
        Assert.Throws<ArgumentNullException>(() => new FirewallRule(anyId, RuleAction.Allow, P("10.0.0.0/8"), null!, null, PortRange.All, PortRange.All, null));
        Assert.Throws<ArgumentNullException>(() => new FirewallRule(anyId, RuleAction.Allow, P("10.0.0.0/8"), P("10.0.0.0/8"), null, null!, PortRange.All, null));
        Assert.Throws<ArgumentNullException>(() => new FirewallRule(anyId, RuleAction.Allow, P("10.0.0.0/8"), P("10.0.0.0/8"), null, PortRange.All, null!, null));
    }

    [Fact]
    public void A_drawn_rule_is_always_constructible() {
        // Arrange: the generator builds through the real constructor, so drawing is the test.
        List<FirewallRule> anyRules = Any.ListOf(Any.FirewallRule()).WithCount(30).Generate();

        // Assert
        Assert.All(anyRules, rule => Assert.Equal(rule.Source.Version, rule.Destination.Version));
        Assert.All(anyRules, rule => Assert.True(rule.TcpFlags is null || rule.Protocol == IpProtocols.Tcp));
    }

    // ---- Matches ---------------------------------------------------------------------------------------------

    [Fact]
    public void A_packet_inside_every_field_of_the_rule_matches() {
        // Arrange
        FirewallRule rule = Build(RuleAction.Allow, "10.0.0.0/8", "192.168.1.0/24", IpProtocols.Tcp, new PortRange(1024, 65535), PortRange.Single(443), TcpFlagMatch.NewConnection);

        Ipv4Packet packet = Any.Ipv4Packet()
                               .WithSource(Ipv4Address.Parse("10.20.30.40"))
                               .WithDestination(Ipv4Address.Parse("192.168.1.77"))
                               .WithProtocol(IpProtocols.Tcp)
                               .WithPorts(50000, 443)
                               .WithTcpFlags(TcpFlags.Syn)
                               .Generate();

        // Assert
        Assert.True(rule.Matches(packet));
    }

    [Fact]
    public void A_packet_of_the_other_family_never_matches() {
        // Arrange
        FirewallRule anyEverythingV4 = Build(RuleAction.Allow, "0.0.0.0/0", "0.0.0.0/0", null);
        FirewallRule anyEverythingV6 = Build(RuleAction.Allow, "::/0", "::/0", null);
        Ipv4Packet   anyV4Packet     = Any.Ipv4Packet().Generate();
        Ipv6Packet   anyV6Packet     = Any.Ipv6Packet().Generate();

        // Assert
        Assert.True(anyEverythingV4.Matches(anyV4Packet));
        Assert.True(anyEverythingV6.Matches(anyV6Packet));
        Assert.False(anyEverythingV4.Matches(anyV6Packet));
        Assert.False(anyEverythingV6.Matches(anyV4Packet));
    }

    [Fact]
    public void A_rule_on_everything_matches_every_packet_of_its_family() {
        // Arrange
        FirewallRule everything = Build(RuleAction.Deny, "0.0.0.0/0", "0.0.0.0/0", null);
        Ipv4Packet   anyPacket  = Any.Ipv4Packet().Generate();

        // Assert
        Assert.True(everything.Matches(anyPacket));
    }

    [Fact]
    public void The_source_and_the_destination_are_each_checked() {
        // Arrange
        FirewallRule rule = Build(RuleAction.Allow, "10.0.0.0/8", "192.168.0.0/16", null);

        // Act
        Ipv4Packet inside      = Any.Ipv4Packet().WithSource(Ipv4Address.Parse("10.1.1.1")).WithDestination(Ipv4Address.Parse("192.168.5.5")).Generate();
        Ipv4Packet wrongSource = Any.Ipv4Packet().WithSource(Ipv4Address.Parse("11.1.1.1")).WithDestination(Ipv4Address.Parse("192.168.5.5")).Generate();
        Ipv4Packet wrongTarget = Any.Ipv4Packet().WithSource(Ipv4Address.Parse("10.1.1.1")).WithDestination(Ipv4Address.Parse("192.169.5.5")).Generate();
        Ipv4Packet swapped     = Any.Ipv4Packet().WithSource(Ipv4Address.Parse("192.168.5.5")).WithDestination(Ipv4Address.Parse("10.1.1.1")).Generate();

        // Assert
        Assert.True(rule.Matches(inside));
        Assert.False(rule.Matches(wrongSource));
        Assert.False(rule.Matches(wrongTarget));
        Assert.False(rule.Matches(swapped));
    }

    [Fact]
    public void The_protocol_is_checked_unless_the_rule_leaves_it_open() {
        // Arrange
        FirewallRule udpOnly = Build(RuleAction.Allow, "0.0.0.0/0", "0.0.0.0/0", IpProtocols.Udp);
        FirewallRule anyProto = Build(RuleAction.Allow, "0.0.0.0/0", "0.0.0.0/0", null);
        Ipv4Packet   udp     = Any.Ipv4Packet().WithProtocol(IpProtocols.Udp).Generate();
        Ipv4Packet   tcp     = Any.Ipv4Packet().WithProtocol(IpProtocols.Tcp).Generate();
        Ipv4Packet   icmp    = Any.Ipv4Packet().WithProtocol(IpProtocols.Icmp).Generate();

        // Assert
        Assert.True(udpOnly.Matches(udp));
        Assert.False(udpOnly.Matches(tcp));
        Assert.False(udpOnly.Matches(icmp));
        Assert.True(anyProto.Matches(udp));
        Assert.True(anyProto.Matches(tcp));
        Assert.True(anyProto.Matches(icmp));
    }

    [Theory]
    [InlineData(1000, true)]
    [InlineData(1500, true)]
    [InlineData(2000, true)]
    [InlineData(999, false)]
    [InlineData(2001, false)]
    [InlineData(0, false)]
    [InlineData(65535, false)]
    public void The_destination_port_range_is_inclusive(ushort port, bool expected) {
        // Arrange
        FirewallRule rule   = Build(RuleAction.Allow, "0.0.0.0/0", "0.0.0.0/0", IpProtocols.Udp, destinationPorts: new PortRange(1000, 2000));
        Ipv4Packet   packet = Any.Ipv4Packet().WithProtocol(IpProtocols.Udp).WithDestinationPort(port).Generate();

        // Assert
        Assert.Equal(expected, rule.Matches(packet));
    }

    [Theory]
    [InlineData(53, true)]
    [InlineData(54, false)]
    [InlineData(52, false)]
    public void The_source_port_range_is_checked_too(ushort port, bool expected) {
        // Arrange
        FirewallRule rule   = Build(RuleAction.Allow, "0.0.0.0/0", "0.0.0.0/0", IpProtocols.Udp, sourcePorts: PortRange.Single(53));
        Ipv4Packet   packet = Any.Ipv4Packet().WithProtocol(IpProtocols.Udp).WithPorts(port, 9).Generate();

        // Assert
        Assert.Equal(expected, rule.Matches(packet));
    }

    [Fact]
    public void A_rule_without_a_protocol_matches_a_packet_whose_ports_are_zero() {
        // Arrange
        FirewallRule rule = Build(RuleAction.Allow, "0.0.0.0/0", "0.0.0.0/0", null);
        Ipv4Packet   icmp = Any.Ipv4Packet().WithProtocol(IpProtocols.Icmp).Generate();

        // Assert
        Assert.Equal(0, icmp.SourcePort);
        Assert.True(rule.Matches(icmp));
    }

    [Theory]
    [InlineData(TcpFlags.Syn, true)]
    [InlineData(TcpFlags.Syn | TcpFlags.Psh, true)]
    [InlineData(TcpFlags.Syn | TcpFlags.Ack, false)]
    [InlineData(TcpFlags.Ack, false)]
    [InlineData(TcpFlags.Rst, false)]
    public void A_flags_test_is_applied_to_the_segment(TcpFlags flags, bool expected) {
        // Arrange
        FirewallRule rule   = Build(RuleAction.Allow, "0.0.0.0/0", "0.0.0.0/0", IpProtocols.Tcp, flags: TcpFlagMatch.NewConnection);
        Ipv4Packet   packet = Any.Ipv4Packet().WithProtocol(IpProtocols.Tcp).WithTcpFlags(flags).Generate();

        // Assert
        Assert.Equal(expected, rule.Matches(packet));
    }

    [Fact]
    public void A_tcp_rule_without_a_flags_test_matches_every_segment() {
        // Arrange
        FirewallRule rule        = Build(RuleAction.Allow, "0.0.0.0/0", "0.0.0.0/0", IpProtocols.Tcp);
        TcpFlags     anyFlags    = Any.ElementOf(Support.ValidTcpFlags).Generate();
        Ipv4Packet   anySegment  = Any.Ipv4Packet().WithProtocol(IpProtocols.Tcp).WithTcpFlags(anyFlags).Generate();

        // Assert
        Assert.True(rule.Matches(anySegment));
    }

    [Fact]
    public void Matches_refuses_null() {
        // Arrange
        FirewallRule anyRule = Any.FirewallRule().Generate();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => anyRule.Matches(null!));
    }

    // ---- Covers ----------------------------------------------------------------------------------------------

    [Fact]
    public void A_rule_covers_itself_whatever_its_action() {
        // Arrange
        FirewallRule anyRule = Any.FirewallRule().Generate();
        FirewallRule opposite = new FirewallRule(Any.RuleId().Generate(),
                                                 anyRule.Action == RuleAction.Allow ? RuleAction.Deny : RuleAction.Allow,
                                                 anyRule.Source,
                                                 anyRule.Destination,
                                                 anyRule.Protocol,
                                                 anyRule.SourcePorts,
                                                 anyRule.DestinationPorts,
                                                 anyRule.TcpFlags);

        // Assert
        Assert.True(anyRule.Covers(anyRule));
        Assert.True(anyRule.Covers(opposite));
        Assert.True(opposite.Covers(anyRule));
    }

    [Fact]
    public void A_rule_on_everything_covers_any_rule_of_its_family_and_not_of_the_other() {
        // Arrange
        FirewallRule everythingV4 = Build(RuleAction.Allow, "0.0.0.0/0", "0.0.0.0/0", null);
        FirewallRule anyV4        = Any.FirewallRule().OfVersion(IpVersion.V4).Generate();
        FirewallRule anyV6        = Any.FirewallRule().OfVersion(IpVersion.V6).Generate();

        // Assert
        Assert.True(everythingV4.Covers(anyV4));
        Assert.False(everythingV4.Covers(anyV6));
    }

    [Theory]
    [InlineData("10.0.0.0/8", "10.1.0.0/16", "0.0.0.0/0", "0.0.0.0/0", true, false)]
    [InlineData("0.0.0.0/0", "0.0.0.0/0", "10.0.0.0/8", "10.1.0.0/16", true, false)]
    [InlineData("10.0.0.0/8", "11.0.0.0/8", "0.0.0.0/0", "0.0.0.0/0", false, false)]
    [InlineData("10.0.0.0/8", "10.0.0.0/8", "0.0.0.0/0", "0.0.0.0/0", true, true)]
    public void Source_and_destination_prefixes_must_each_nest(string widerSource, string narrowerSource, string widerDest, string narrowerDest, bool narrowerIsCovered, bool symmetric) {
        // Arrange
        FirewallRule wider    = Build(RuleAction.Allow, widerSource, widerDest, null);
        FirewallRule narrower = Build(RuleAction.Allow, narrowerSource, narrowerDest, null);

        // Assert
        Assert.Equal(narrowerIsCovered, wider.Covers(narrower));
        Assert.Equal(symmetric, narrower.Covers(wider));
    }

    [Fact]
    public void An_open_protocol_covers_a_given_one_but_not_the_reverse() {
        // Arrange
        FirewallRule open = Build(RuleAction.Allow, "10.0.0.0/8", "10.0.0.0/8", null);
        FirewallRule tcp  = Build(RuleAction.Allow, "10.0.0.0/8", "10.0.0.0/8", IpProtocols.Tcp);
        FirewallRule udp  = Build(RuleAction.Allow, "10.0.0.0/8", "10.0.0.0/8", IpProtocols.Udp);

        // Assert
        Assert.True(open.Covers(tcp));
        Assert.False(tcp.Covers(open));
        Assert.False(tcp.Covers(udp));
        Assert.False(udp.Covers(tcp));
    }

    [Fact]
    public void A_wider_port_range_covers_a_narrower_one() {
        // Arrange
        FirewallRule wide   = Build(RuleAction.Allow, "10.0.0.0/8", "10.0.0.0/8", IpProtocols.Tcp, destinationPorts: new PortRange(1, 1024));
        FirewallRule narrow = Build(RuleAction.Allow, "10.0.0.0/8", "10.0.0.0/8", IpProtocols.Tcp, destinationPorts: PortRange.Single(80));
        FirewallRule shifted = Build(RuleAction.Allow, "10.0.0.0/8", "10.0.0.0/8", IpProtocols.Tcp, destinationPorts: new PortRange(1000, 2000));

        // Assert
        Assert.True(wide.Covers(narrow));
        Assert.False(narrow.Covers(wide));
        Assert.False(wide.Covers(shifted));
    }

    [Fact]
    public void The_source_ports_are_compared_as_well() {
        // Arrange
        FirewallRule fromDns  = Build(RuleAction.Allow, "10.0.0.0/8", "10.0.0.0/8", IpProtocols.Udp, sourcePorts: PortRange.Single(53));
        FirewallRule fromAny  = Build(RuleAction.Allow, "10.0.0.0/8", "10.0.0.0/8", IpProtocols.Udp);

        // Assert
        Assert.True(fromAny.Covers(fromDns));
        Assert.False(fromDns.Covers(fromAny));
    }

    [Fact]
    public void A_rule_without_a_flags_test_covers_one_with_it_and_never_the_reverse() {
        // Arrange
        FirewallRule withoutFlags = Build(RuleAction.Allow, "10.0.0.0/8", "10.0.0.0/8", IpProtocols.Tcp);
        FirewallRule withFlags    = Build(RuleAction.Allow, "10.0.0.0/8", "10.0.0.0/8", IpProtocols.Tcp, flags: TcpFlagMatch.Established);

        // Assert
        Assert.True(withoutFlags.Covers(withFlags));
        Assert.False(withFlags.Covers(withoutFlags));
    }

    [Fact]
    public void Flags_tests_are_compared_by_what_they_examine() {
        // Arrange
        FirewallRule established = Build(RuleAction.Allow, "10.0.0.0/8", "10.0.0.0/8", IpProtocols.Tcp, flags: TcpFlagMatch.Established);
        FirewallRule ackAndPsh   = Build(RuleAction.Allow, "10.0.0.0/8", "10.0.0.0/8", IpProtocols.Tcp, flags: new TcpFlagMatch(TcpFlags.Ack | TcpFlags.Psh, TcpFlags.Ack | TcpFlags.Psh));
        FirewallRule opening     = Build(RuleAction.Allow, "10.0.0.0/8", "10.0.0.0/8", IpProtocols.Tcp, flags: TcpFlagMatch.NewConnection);

        // Assert
        Assert.True(established.Covers(ackAndPsh));
        Assert.False(ackAndPsh.Covers(established));
        Assert.False(established.Covers(opening));
        Assert.False(opening.Covers(established));
    }

    [Fact]
    public void A_rule_that_covers_another_matches_every_packet_the_other_matches() {
        // Arrange
        FirewallRule wide   = Build(RuleAction.Allow, "10.0.0.0/8", "0.0.0.0/0", IpProtocols.Tcp, destinationPorts: new PortRange(1, 1024));
        FirewallRule narrow = Build(RuleAction.Deny, "10.1.0.0/16", "192.168.0.0/16", IpProtocols.Tcp, destinationPorts: PortRange.Single(22), flags: TcpFlagMatch.NewConnection);
        Ipv4Packet   packet = Any.Ipv4Packet()
                                 .WithSource(Ipv4Address.Parse("10.1.2.3"))
                                 .WithDestination(Ipv4Address.Parse("192.168.9.9"))
                                 .WithProtocol(IpProtocols.Tcp)
                                 .WithDestinationPort(22)
                                 .WithTcpFlags(TcpFlags.Syn)
                                 .Generate();

        // Assert
        Assert.True(wide.Covers(narrow));
        Assert.True(narrow.Matches(packet));
        Assert.True(wide.Matches(packet));
    }

    [Fact]
    public void Covers_and_Overlaps_refuse_null() {
        // Arrange
        FirewallRule anyRule = Any.FirewallRule().Generate();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => anyRule.Covers(null!));
        Assert.Throws<ArgumentNullException>(() => anyRule.Overlaps(null!));
    }

    // ---- Overlaps --------------------------------------------------------------------------------------------

    [Fact]
    public void A_rule_overlaps_itself() {
        // Arrange
        FirewallRule anyRule = Any.FirewallRule().Generate();

        // Assert
        Assert.True(anyRule.Overlaps(anyRule));
    }

    [Fact]
    public void A_rule_that_covers_another_overlaps_it_both_ways() {
        // Arrange
        FirewallRule wide   = Build(RuleAction.Allow, "10.0.0.0/8", "0.0.0.0/0", null);
        FirewallRule narrow = Build(RuleAction.Deny, "10.1.0.0/16", "192.168.0.0/16", IpProtocols.Udp, destinationPorts: PortRange.Single(53));

        // Assert
        Assert.True(wide.Covers(narrow));
        Assert.True(wide.Overlaps(narrow));
        Assert.True(narrow.Overlaps(wide));
    }

    [Theory]
    [InlineData("10.0.0.0/8", "11.0.0.0/8", "0.0.0.0/0", "0.0.0.0/0")]
    [InlineData("0.0.0.0/0", "0.0.0.0/0", "10.0.0.0/8", "11.0.0.0/8")]
    public void Rules_with_disjoint_prefixes_do_not_overlap(string source, string otherSource, string destination, string otherDestination) {
        // Arrange
        FirewallRule rule  = Build(RuleAction.Allow, source, destination, null);
        FirewallRule other = Build(RuleAction.Allow, otherSource, otherDestination, null);

        // Assert
        Assert.False(rule.Overlaps(other));
        Assert.False(other.Overlaps(rule));
    }

    [Fact]
    public void Rules_on_different_protocols_do_not_overlap_but_an_open_protocol_overlaps_any() {
        // Arrange
        FirewallRule tcp  = Build(RuleAction.Allow, "10.0.0.0/8", "10.0.0.0/8", IpProtocols.Tcp);
        FirewallRule udp  = Build(RuleAction.Allow, "10.0.0.0/8", "10.0.0.0/8", IpProtocols.Udp);
        FirewallRule open = Build(RuleAction.Allow, "10.0.0.0/8", "10.0.0.0/8", null);

        // Assert
        Assert.False(tcp.Overlaps(udp));
        Assert.True(open.Overlaps(tcp));
        Assert.True(tcp.Overlaps(open));
    }

    [Fact]
    public void Rules_on_disjoint_port_ranges_do_not_overlap() {
        // Arrange
        FirewallRule low  = Build(RuleAction.Allow, "10.0.0.0/8", "10.0.0.0/8", IpProtocols.Tcp, destinationPorts: new PortRange(1, 1023));
        FirewallRule high = Build(RuleAction.Allow, "10.0.0.0/8", "10.0.0.0/8", IpProtocols.Tcp, destinationPorts: new PortRange(1024, 65535));
        FirewallRule edge = Build(RuleAction.Allow, "10.0.0.0/8", "10.0.0.0/8", IpProtocols.Tcp, destinationPorts: new PortRange(1023, 1024));

        // Assert
        Assert.False(low.Overlaps(high));
        Assert.True(low.Overlaps(edge));
        Assert.True(high.Overlaps(edge));
    }

    [Fact]
    public void Flags_tests_that_contradict_each_other_do_not_overlap() {
        // Arrange
        FirewallRule established = Build(RuleAction.Allow, "10.0.0.0/8", "10.0.0.0/8", IpProtocols.Tcp, flags: TcpFlagMatch.Established);
        FirewallRule opening     = Build(RuleAction.Allow, "10.0.0.0/8", "10.0.0.0/8", IpProtocols.Tcp, flags: TcpFlagMatch.NewConnection);
        FirewallRule noTest      = Build(RuleAction.Allow, "10.0.0.0/8", "10.0.0.0/8", IpProtocols.Tcp);

        // Assert
        Assert.False(established.Overlaps(opening));
        Assert.True(established.Overlaps(noTest));
        Assert.True(noTest.Overlaps(opening));
    }

    [Fact]
    public void Rules_of_different_families_never_overlap_nor_cover() {
        // Arrange
        FirewallRule v4 = Build(RuleAction.Allow, "0.0.0.0/0", "0.0.0.0/0", null);
        FirewallRule v6 = Build(RuleAction.Allow, "::/0", "::/0", null);

        // Assert
        Assert.False(v4.Overlaps(v6));
        Assert.False(v6.Overlaps(v4));
        Assert.False(v4.Covers(v6));
    }

    [Fact]
    public void Overlap_is_symmetric() {
        // Arrange
        FirewallRule anyLeft  = Any.FirewallRule().OfVersion(IpVersion.V4).Generate();
        FirewallRule anyRight = Any.FirewallRule().OfVersion(IpVersion.V4).Generate();

        // Assert
        Assert.Equal(anyLeft.Overlaps(anyRight), anyRight.Overlaps(anyLeft));
    }

    [Fact]
    public void A_rule_reads_as_its_id_action_prefixes_protocol_ports_and_flags() {
        // Arrange
        FirewallRule rule = new FirewallRule(new RuleId("allow-https"), RuleAction.Allow, P("10.0.0.0/8"), P("192.168.1.0/24"), IpProtocols.Tcp, PortRange.All, PortRange.Single(443), TcpFlagMatch.NewConnection);

        // Assert
        Assert.Equal("allow-https: Allow 10.0.0.0/8 -> 192.168.1.0/24 proto 6 sport any dport 443 flags Syn, Ack/Syn", rule.ToString());
    }

}
