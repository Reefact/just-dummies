using JustDummies;

using NetworkAcl.Domain;
using NetworkAcl.Tests.Expert.Arbitrary;

using Xunit;

using static NetworkAcl.Tests.Expert.Shorthand;

namespace NetworkAcl.Tests.Expert.Tests;

public sealed class FirewallRuleConstructionTests {

    [Fact]
    public void A_rule_exposes_what_it_was_built_from() {
        // Act
        FirewallRule rule = new FirewallRule(Id("allow-web"), RuleAction.Allow, Prefix("0.0.0.0/0"), Prefix("192.0.2.0/24"), IpProtocols.Tcp, Ports(1024, 65535), Port(443), TcpFlagMatch.NewConnection);

        // Assert
        Assert.Equal(Id("allow-web"), rule.Id);
        Assert.Equal(RuleAction.Allow, rule.Action);
        Assert.Equal(Prefix("0.0.0.0/0"), rule.Source);
        Assert.Equal(Prefix("192.0.2.0/24"), rule.Destination);
        Assert.Equal(IpProtocols.Tcp, rule.Protocol);
        Assert.Equal(Ports(1024, 65535), rule.SourcePorts);
        Assert.Equal(Port(443), rule.DestinationPorts);
        Assert.Equal(TcpFlagMatch.NewConnection, rule.TcpFlags);
    }

    [Theory]
    [InlineData("10.0.0.0/8", "192.0.2.0/24", IpVersion.V4)]
    [InlineData("2001:db8::/32", "::/0", IpVersion.V6)]
    public void The_version_of_a_rule_is_the_family_of_its_prefixes(string source, string destination, IpVersion expected) {
        Assert.Equal(expected, Rule(source: source, destination: destination).Version);
    }

    [Fact]
    public void A_rule_without_protocol_or_flags_test_keeps_them_null() {
        // Act
        FirewallRule rule = Rule();

        // Assert
        Assert.Null(rule.Protocol);
        Assert.Null(rule.TcpFlags);
        Assert.True(rule.SourcePorts.IsAll);
        Assert.True(rule.DestinationPorts.IsAll);
    }

    [Fact]
    public void A_missing_part_is_refused() {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new FirewallRule(null!, RuleAction.Allow, Prefix("0.0.0.0/0"), Prefix("0.0.0.0/0"), null, PortRange.All, PortRange.All, null));
        Assert.Throws<ArgumentNullException>(() => new FirewallRule(Id("r"), RuleAction.Allow, null!, Prefix("0.0.0.0/0"), null, PortRange.All, PortRange.All, null));
        Assert.Throws<ArgumentNullException>(() => new FirewallRule(Id("r"), RuleAction.Allow, Prefix("0.0.0.0/0"), null!, null, PortRange.All, PortRange.All, null));
        Assert.Throws<ArgumentNullException>(() => new FirewallRule(Id("r"), RuleAction.Allow, Prefix("0.0.0.0/0"), Prefix("0.0.0.0/0"), null, null!, PortRange.All, null));
        Assert.Throws<ArgumentNullException>(() => new FirewallRule(Id("r"), RuleAction.Allow, Prefix("0.0.0.0/0"), Prefix("0.0.0.0/0"), null, PortRange.All, null!, null));
    }

    [Fact]
    public void An_undefined_action_is_refused() {
        Assert.Throws<ArgumentOutOfRangeException>(() => Rule(action: (RuleAction)3));
    }

    [Theory]
    [InlineData("10.0.0.0/8", "::/0")]
    [InlineData("::/0", "10.0.0.0/8")]
    public void A_rule_cannot_mix_ipv4_and_ipv6(string source, string destination) {
        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => Rule(source: source, destination: destination));

        // Assert
        Assert.Equal("destination", refusal.ParamName);
    }

    [Fact]
    public void The_reserved_protocol_255_is_refused() {
        // Act
        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(() => Rule(protocol: IpProtocols.Reserved));

        // Assert
        Assert.Equal("protocol", refusal.ParamName);
    }

    [Fact]
    public void An_icmp_rule_is_refused_over_ipv6_and_an_icmpv6_rule_over_ipv4() {
        // Act
        ArgumentException icmpOnV6   = Assert.Throws<ArgumentException>(() => Rule(source: "::/0", destination: "::/0", protocol: IpProtocols.Icmp));
        ArgumentException icmpv6OnV4 = Assert.Throws<ArgumentException>(() => Rule(protocol: IpProtocols.Icmpv6));

        // Assert
        Assert.Equal("protocol", icmpOnV6.ParamName);
        Assert.Equal("protocol", icmpv6OnV4.ParamName);
    }

    [Fact]
    public void Icmp_is_accepted_over_ipv4_and_icmpv6_over_ipv6() {
        Assert.Equal(IpProtocols.Icmp, Rule(protocol: IpProtocols.Icmp).Protocol);
        Assert.Equal(IpProtocols.Icmpv6, Rule(source: "::/0", destination: "::/0", protocol: IpProtocols.Icmpv6).Protocol);
    }

    [Theory]
    [InlineData(null)]
    [InlineData(IpProtocols.Icmp)]
    [InlineData((byte)47)]
    public void Ports_cannot_be_narrowed_without_a_protocol_that_carries_them(byte? protocol) {
        // Act
        ArgumentException sourceRefusal      = Assert.Throws<ArgumentException>(() => Rule(protocol: protocol, sourcePorts: Port(1)));
        ArgumentException destinationRefusal = Assert.Throws<ArgumentException>(() => Rule(protocol: protocol, destinationPorts: Port(1)));

        // Assert
        Assert.Equal("protocol", sourceRefusal.ParamName);
        Assert.Equal("protocol", destinationRefusal.ParamName);
    }

    [Theory]
    [InlineData(IpProtocols.Tcp)]
    [InlineData(IpProtocols.Udp)]
    [InlineData(IpProtocols.Sctp)]
    public void Ports_can_be_narrowed_for_tcp_udp_and_sctp(byte protocol) {
        // Act
        FirewallRule rule = Rule(protocol: protocol, sourcePorts: Ports(1024, 2047), destinationPorts: Port(53));

        // Assert
        Assert.Equal(Ports(1024, 2047), rule.SourcePorts);
        Assert.Equal(Port(53), rule.DestinationPorts);
    }

    [Theory]
    [InlineData(null)]
    [InlineData(IpProtocols.Udp)]
    [InlineData(IpProtocols.Icmp)]
    public void A_flags_test_needs_protocol_tcp(byte? protocol) {
        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => Rule(protocol: protocol, tcpFlags: TcpFlagMatch.Established));

        // Assert
        Assert.Equal("tcpFlags", refusal.ParamName);
    }

    [Fact]
    public void A_flags_test_is_accepted_with_protocol_tcp() {
        Assert.Equal(TcpFlagMatch.Established, Rule(protocol: IpProtocols.Tcp, tcpFlags: TcpFlagMatch.Established).TcpFlags);
    }

    [Fact]
    public void The_checks_run_in_order_action_family_protocol_ports_flags() {
        // Arrange: each rule breaks the check named and all the ones after it.
        AddressPrefix v4 = Prefix("10.0.0.0/8");
        AddressPrefix v6 = Prefix("2001:db8::/32");

        // Act
        Assert.Throws<ArgumentOutOfRangeException>(() => new FirewallRule(Id("r"), (RuleAction)9, v4, v6, IpProtocols.Reserved, Port(1), Port(1), TcpFlagMatch.Established));
        ArgumentException family   = Assert.Throws<ArgumentException>(() => new FirewallRule(Id("r"), RuleAction.Allow, v4, v6, IpProtocols.Icmpv6, Port(1), Port(1), TcpFlagMatch.Established));
        Assert.Throws<ArgumentOutOfRangeException>(() => new FirewallRule(Id("r"), RuleAction.Allow, v4, v4, IpProtocols.Reserved, Port(1), Port(1), TcpFlagMatch.Established));
        ArgumentException protocol = Assert.Throws<ArgumentException>(() => new FirewallRule(Id("r"), RuleAction.Allow, v4, v4, IpProtocols.Icmpv6, Port(1), Port(1), TcpFlagMatch.Established));
        ArgumentException ports    = Assert.Throws<ArgumentException>(() => new FirewallRule(Id("r"), RuleAction.Allow, v4, v4, IpProtocols.Icmp, Port(1), Port(1), TcpFlagMatch.Established));

        // Assert
        Assert.Equal("destination", family.ParamName);
        Assert.Equal("protocol", protocol.ParamName);
        Assert.Equal("protocol", ports.ParamName);
        Assert.Contains("Port ranges", ports.Message);
    }

    [Fact]
    public void ToString_lists_every_field_and_the_flags_test_only_when_there_is_one() {
        Assert.Equal("allow-web: Allow 0.0.0.0/0 -> 192.0.2.0/24 proto 6 sport 1024-65535 dport 443 flags Syn, Ack/Syn",
                     new FirewallRule(Id("allow-web"), RuleAction.Allow, Prefix("0.0.0.0/0"), Prefix("192.0.2.0/24"), IpProtocols.Tcp, Ports(1024, 65535), Port(443), TcpFlagMatch.NewConnection).ToString());
        Assert.Equal("any-any: Deny 0.0.0.0/0 -> 0.0.0.0/0 proto any sport any dport any", Rule(id: "any-any", action: RuleAction.Deny).ToString());
    }

}

public sealed class FirewallRuleMatchingTests {

    [Fact]
    public void A_rule_with_nothing_narrowed_matches_every_ipv4_packet() {
        // Arrange
        Ipv4Packet anyPacket = Any.Ipv4Packet().Generate();

        // Act & Assert
        Assert.True(Rule().Matches(anyPacket));
    }

    [Fact]
    public void A_rule_never_matches_a_packet_of_the_other_family_whatever_its_prefixes() {
        // Arrange
        FirewallRule everythingV4 = Rule();
        FirewallRule everythingV6 = Rule(source: "::/0", destination: "::/0");
        Ipv4Packet anyV4Packet = Any.Ipv4Packet().Generate();
        Ipv6Packet anyV6Packet = Any.Ipv6Packet().Generate();

        // Act & Assert
        Assert.False(everythingV4.Matches(anyV6Packet));
        Assert.False(everythingV6.Matches(anyV4Packet));
        Assert.True(everythingV6.Matches(anyV6Packet));
    }

    [Theory]
    [InlineData("10.0.0.0", true)]
    [InlineData("10.255.255.255", true)]
    [InlineData("9.255.255.255", false)]
    [InlineData("11.0.0.0", false)]
    public void The_source_address_must_be_inside_the_source_prefix(string source, bool expected) {
        // Arrange
        FirewallRule rule = Rule(source: "10.0.0.0/8");
        Ipv4Packet packet = Any.Ipv4Packet().WithSource(V4(source)).Generate();

        // Act & Assert
        Assert.Equal(expected, rule.Matches(packet));
    }

    [Theory]
    [InlineData("192.168.1.127", false)]
    [InlineData("192.168.1.128", true)]
    [InlineData("192.168.1.255", true)]
    [InlineData("192.168.2.0", false)]
    public void The_destination_address_must_be_inside_the_destination_prefix(string destination, bool expected) {
        // Arrange
        FirewallRule rule = Rule(destination: "192.168.1.128/25");
        Ipv4Packet packet = Any.Ipv4Packet().WithDestination(V4(destination)).Generate();

        // Act & Assert
        Assert.Equal(expected, rule.Matches(packet));
    }

    [Theory]
    [InlineData("2001:db8::", true)]
    [InlineData("2001:db8:ffff:ffff:ffff:ffff:ffff:ffff", true)]
    [InlineData("2001:db9::", false)]
    [InlineData("2001:db7:ffff:ffff:ffff:ffff:ffff:ffff", false)]
    public void An_ipv6_rule_applies_the_same_prefix_test_to_ipv6_addresses(string source, bool expected) {
        // Arrange
        FirewallRule rule = Rule(source: "2001:db8::/32", destination: "::/0");
        Ipv6Packet packet = Any.Ipv6Packet().WithSource(V6(source)).Generate();

        // Act & Assert
        Assert.Equal(expected, rule.Matches(packet));
    }

    [Fact]
    public void A_rule_with_a_protocol_matches_that_protocol_only() {
        // Arrange: ports are dummies since neither the rule nor the test is about them; ICMP has none, so it is spelled apart.
        FirewallRule tcpRule = Rule(protocol: IpProtocols.Tcp);

        Ipv4Packet tcp  = Any.Ipv4Packet().WithProtocol(IpProtocols.Tcp).Generate();
        Ipv4Packet udp  = Any.Ipv4Packet().WithProtocol(IpProtocols.Udp).Generate();
        Ipv4Packet icmp = Any.Ipv4Packet().WithProtocol(IpProtocols.Icmp).Generate();

        // Act & Assert
        Assert.True(tcpRule.Matches(tcp));
        Assert.False(tcpRule.Matches(udp));
        Assert.False(tcpRule.Matches(icmp));
    }

    [Fact]
    public void A_rule_without_protocol_matches_a_packet_of_any_protocol_including_one_without_ports() {
        // Arrange
        FirewallRule anyProtocolRule = Rule();
        Ipv4Packet portless = Any.Ipv4Packet().WithProtocol(IpProtocols.Icmp).Generate();
        Ipv4Packet anyPacket = Any.Ipv4Packet().Generate();

        // Act & Assert
        Assert.True(anyProtocolRule.Matches(portless));
        Assert.True(anyProtocolRule.Matches(anyPacket));
    }

    [Theory]
    [InlineData(79, false)]
    [InlineData(80, true)]
    [InlineData(85, true)]
    [InlineData(90, true)]
    [InlineData(91, false)]
    public void The_destination_port_must_be_inside_the_destination_range(ushort port, bool expected) {
        // Arrange
        FirewallRule rule = Rule(protocol: IpProtocols.Tcp, destinationPorts: Ports(80, 90));
        Ipv4Packet packet = Any.Ipv4Packet().WithProtocol(IpProtocols.Tcp).WithDestinationPort(port).Generate();

        // Act & Assert
        Assert.Equal(expected, rule.Matches(packet));
    }

    [Theory]
    [InlineData(1023, false)]
    [InlineData(1024, true)]
    [InlineData(65535, true)]
    public void The_source_port_must_be_inside_the_source_range(ushort port, bool expected) {
        // Arrange
        FirewallRule rule = Rule(protocol: IpProtocols.Udp, sourcePorts: Ports(1024, 65535));
        Ipv4Packet packet = Any.Ipv4Packet().WithProtocol(IpProtocols.Udp).WithSourcePort(port).Generate();

        // Act & Assert
        Assert.Equal(expected, rule.Matches(packet));
    }

    [Fact]
    public void Source_and_destination_ports_are_tested_independently_not_crossed() {
        // Arrange
        FirewallRule rule = Rule(protocol: IpProtocols.Tcp, sourcePorts: Port(1000), destinationPorts: Port(2000));
        Ipv4Packet straight = Any.Ipv4Packet().WithProtocol(IpProtocols.Tcp).WithSourcePort(1000).WithDestinationPort(2000).Generate();
        Ipv4Packet crossed  = Any.Ipv4Packet().WithProtocol(IpProtocols.Tcp).WithSourcePort(2000).WithDestinationPort(1000).Generate();

        // Act & Assert
        Assert.True(rule.Matches(straight));
        Assert.False(rule.Matches(crossed));
    }

    [Theory]
    [InlineData(TcpFlags.Syn, true)]
    [InlineData(TcpFlags.Syn | TcpFlags.Ack, false)]
    [InlineData(TcpFlags.Ack, false)]
    [InlineData(TcpFlags.Ack | TcpFlags.Psh, false)]
    public void A_flags_test_looks_at_the_segments_flags(TcpFlags flags, bool expected) {
        // Arrange
        FirewallRule rule = Rule(protocol: IpProtocols.Tcp, tcpFlags: TcpFlagMatch.NewConnection);
        Ipv4Packet packet = Any.Ipv4Packet().WithProtocol(IpProtocols.Tcp).WithTcpFlags(flags).Generate();

        // Act & Assert
        Assert.Equal(expected, rule.Matches(packet));
    }

    [Fact]
    public void A_flags_test_ignores_flags_outside_its_mask() {
        // Arrange
        TcpFlags anyAuxiliary = AnyTcpSegmentFlags.Auxiliary().Generate();
        FirewallRule rule = Rule(protocol: IpProtocols.Tcp, tcpFlags: TcpFlagMatch.NewConnection);
        Ipv4Packet packet = Any.Ipv4Packet().WithProtocol(IpProtocols.Tcp).WithTcpFlags(TcpFlags.Syn | anyAuxiliary).Generate();

        // Act & Assert
        Assert.True(rule.Matches(packet));
    }

    [Fact]
    public void A_tcp_rule_without_flags_test_matches_a_segment_with_any_valid_flags() {
        // Arrange
        FirewallRule rule = Rule(protocol: IpProtocols.Tcp);
        Ipv4Packet anySegment = Any.Ipv4Packet().WithProtocol(IpProtocols.Tcp).Generate();

        // Act & Assert
        Assert.True(rule.Matches(anySegment));
    }

    [Fact]
    public void A_rule_matches_only_when_every_field_matches() {
        // Arrange
        FirewallRule rule = Rule(source: "10.0.0.0/8", destination: "192.0.2.0/24", protocol: IpProtocols.Tcp, destinationPorts: Port(443), tcpFlags: TcpFlagMatch.NewConnection);
        AnyIpv4Packet matching = Any.Ipv4Packet().WithSource(V4("10.1.2.3")).WithDestination(V4("192.0.2.9")).WithProtocol(IpProtocols.Tcp).WithDestinationPort(443).WithTcpFlags(TcpFlags.Syn);

        // Act & Assert
        Assert.True(rule.Matches(matching.Generate()));
        Assert.False(rule.Matches(matching.WithSource(V4("11.1.2.3")).Generate()));
        Assert.False(rule.Matches(matching.WithDestination(V4("192.0.3.9")).Generate()));
        Assert.False(rule.Matches(matching.WithProtocol(IpProtocols.Udp).WithTcpFlags(TcpFlags.None).Generate()));
        Assert.False(rule.Matches(matching.WithDestinationPort(444).Generate()));
        Assert.False(rule.Matches(matching.WithTcpFlags(TcpFlags.Ack).Generate()));
    }

    [Fact]
    public void A_missing_packet_is_refused() {
        Assert.Throws<ArgumentNullException>(() => Rule().Matches(null!));
    }

}

public sealed class FirewallRuleCoverageTests {

    [Fact]
    public void A_rule_covers_itself_whatever_its_shape() {
        // Arrange
        FirewallRule anyRule = Any.FirewallRule().Generate();

        // Act & Assert
        Assert.True(anyRule.Covers(anyRule));
        Assert.True(anyRule.Overlaps(anyRule));
    }

    [Fact]
    public void The_action_plays_no_part_in_coverage() {
        // Arrange
        FirewallRule allow = Rule(id: "allow", action: RuleAction.Allow, source: "10.0.0.0/8");
        FirewallRule deny  = Rule(id: "deny", action: RuleAction.Deny, source: "10.1.0.0/16");

        // Act & Assert
        Assert.True(allow.Covers(deny));
        Assert.True(allow.Covers(allow));
    }

    [Theory]
    [InlineData("10.0.0.0/8", "10.1.0.0/16", true)]
    [InlineData("10.1.0.0/16", "10.0.0.0/8", false)]
    [InlineData("10.0.0.0/9", "10.128.0.0/9", false)]
    public void A_rule_covers_a_narrower_source_prefix_only(string source, string otherSource, bool expected) {
        Assert.Equal(expected, Rule(source: source).Covers(Rule(source: otherSource)));
    }

    [Theory]
    [InlineData("192.0.2.0/24", "192.0.2.128/25", true)]
    [InlineData("192.0.2.128/25", "192.0.2.0/24", false)]
    public void A_rule_covers_a_narrower_destination_prefix_only(string destination, string otherDestination, bool expected) {
        Assert.Equal(expected, Rule(destination: destination).Covers(Rule(destination: otherDestination)));
    }

    [Fact]
    public void Both_prefixes_must_cover_not_just_one() {
        // Arrange
        FirewallRule broadSourceNarrowDestination = Rule(source: "10.0.0.0/8", destination: "192.0.2.0/25");
        FirewallRule narrowSourceBroadDestination = Rule(source: "10.1.0.0/16", destination: "192.0.2.0/24");

        // Act & Assert
        Assert.False(broadSourceNarrowDestination.Covers(narrowSourceBroadDestination));
        Assert.False(narrowSourceBroadDestination.Covers(broadSourceNarrowDestination));
    }

    [Fact]
    public void A_rule_never_covers_a_rule_of_the_other_family() {
        // Arrange
        FirewallRule everythingV4 = Rule();
        FirewallRule everythingV6 = Rule(source: "::/0", destination: "::/0");

        // Act & Assert
        Assert.False(everythingV4.Covers(everythingV6));
        Assert.False(everythingV6.Covers(everythingV4));
        Assert.False(everythingV4.Overlaps(everythingV6));
    }

    [Fact]
    public void A_rule_without_protocol_covers_any_protocol_but_not_the_other_way_round() {
        // Arrange
        FirewallRule anyProtocol = Rule();
        FirewallRule tcp = Rule(protocol: IpProtocols.Tcp);
        FirewallRule udp = Rule(protocol: IpProtocols.Udp);

        // Act & Assert
        Assert.True(anyProtocol.Covers(tcp));
        Assert.False(tcp.Covers(anyProtocol));
        Assert.True(tcp.Covers(tcp));
        Assert.False(tcp.Covers(udp));
    }

    [Theory]
    [InlineData(1, 100, 10, 20, true)]
    [InlineData(1, 100, 1, 100, true)]
    [InlineData(10, 20, 1, 100, false)]
    [InlineData(10, 20, 15, 25, false)]   // partial overlap is not coverage
    public void A_rule_covers_a_narrower_destination_port_range_only(ushort low, ushort high, ushort otherLow, ushort otherHigh, bool expected) {
        // Arrange
        FirewallRule rule  = Rule(protocol: IpProtocols.Udp, destinationPorts: Ports(low, high));
        FirewallRule other = Rule(protocol: IpProtocols.Udp, destinationPorts: Ports(otherLow, otherHigh));

        // Act & Assert
        Assert.Equal(expected, rule.Covers(other));
    }

    [Fact]
    public void A_narrower_source_port_range_does_not_cover_a_wider_one_even_when_destination_ports_agree() {
        // Arrange
        FirewallRule wide   = Rule(protocol: IpProtocols.Tcp, sourcePorts: Ports(1024, 65535), destinationPorts: Port(80));
        FirewallRule narrow = Rule(protocol: IpProtocols.Tcp, sourcePorts: Ports(2000, 3000), destinationPorts: Port(80));

        // Act & Assert
        Assert.True(wide.Covers(narrow));
        Assert.False(narrow.Covers(wide));
    }

    [Fact]
    public void A_rule_without_flags_test_covers_one_with_a_test_but_a_flags_test_never_covers_one_without() {
        // Arrange
        FirewallRule noTest   = Rule(protocol: IpProtocols.Tcp);
        FirewallRule withTest = Rule(protocol: IpProtocols.Tcp, tcpFlags: TcpFlagMatch.NewConnection);

        // Act & Assert
        Assert.True(noTest.Covers(withTest));
        Assert.False(withTest.Covers(noTest));
    }

    [Fact]
    public void A_flags_test_covers_another_when_it_examines_fewer_flags_and_agrees() {
        // Arrange
        FirewallRule synSet            = Rule(protocol: IpProtocols.Tcp, tcpFlags: new TcpFlagMatch(TcpFlags.Syn, TcpFlags.Syn));
        FirewallRule synSetAckClear    = Rule(protocol: IpProtocols.Tcp, tcpFlags: TcpFlagMatch.NewConnection);
        FirewallRule ackSet            = Rule(protocol: IpProtocols.Tcp, tcpFlags: TcpFlagMatch.Established);

        // Act & Assert
        Assert.True(synSet.Covers(synSetAckClear));
        Assert.False(synSetAckClear.Covers(synSet));
        Assert.False(synSet.Covers(ackSet));
    }

    [Fact]
    public void Covers_and_Overlaps_refuse_a_missing_rule() {
        Assert.Throws<ArgumentNullException>(() => Rule().Covers(null!));
        Assert.Throws<ArgumentNullException>(() => Rule().Overlaps(null!));
    }

    [Theory]
    [InlineData("10.0.0.0/8", "10.1.0.0/16", true)]
    [InlineData("10.1.0.0/16", "10.0.0.0/8", true)]
    [InlineData("10.0.0.0/9", "10.128.0.0/9", false)]
    [InlineData("10.0.0.0/8", "11.0.0.0/8", false)]
    public void Rules_overlap_when_their_source_prefixes_nest(string source, string otherSource, bool expected) {
        Assert.Equal(expected, Rule(source: source).Overlaps(Rule(source: otherSource)));
    }

    [Fact]
    public void Rules_overlap_only_when_every_dimension_overlaps() {
        // Arrange
        FirewallRule web = Rule(source: "10.0.0.0/8", protocol: IpProtocols.Tcp, destinationPorts: Ports(80, 90));

        // Act & Assert
        Assert.True(web.Overlaps(Rule(source: "10.1.0.0/16", protocol: IpProtocols.Tcp, destinationPorts: Ports(90, 100))));   // sharing port 90
        Assert.False(web.Overlaps(Rule(source: "10.1.0.0/16", protocol: IpProtocols.Tcp, destinationPorts: Ports(91, 100))));  // adjacent ports
        Assert.False(web.Overlaps(Rule(source: "11.0.0.0/8", protocol: IpProtocols.Tcp, destinationPorts: Ports(80, 90))));    // disjoint sources
        Assert.False(web.Overlaps(Rule(source: "10.1.0.0/16", protocol: IpProtocols.Udp, destinationPorts: Ports(80, 90))));   // different protocols
        Assert.True(web.Overlaps(Rule(source: "10.1.0.0/16", destinationPorts: PortRange.All, protocol: null)));               // any protocol
        Assert.False(web.Overlaps(Rule(source: "::/0", destination: "::/0", protocol: null)));              // other family
    }

    [Fact]
    public void Source_ports_take_part_in_overlap_too() {
        // Arrange
        FirewallRule low  = Rule(protocol: IpProtocols.Tcp, sourcePorts: Ports(1, 1000));
        FirewallRule high = Rule(protocol: IpProtocols.Tcp, sourcePorts: Ports(1001, 2000));

        // Act & Assert
        Assert.False(low.Overlaps(high));
    }

    [Fact]
    public void Flags_tests_overlap_unless_they_disagree_on_a_flag_both_examine() {
        // Arrange
        FirewallRule newConnection = Rule(protocol: IpProtocols.Tcp, tcpFlags: TcpFlagMatch.NewConnection);
        FirewallRule established   = Rule(protocol: IpProtocols.Tcp, tcpFlags: TcpFlagMatch.Established);
        FirewallRule finSet        = Rule(protocol: IpProtocols.Tcp, tcpFlags: new TcpFlagMatch(TcpFlags.Fin, TcpFlags.Fin));
        FirewallRule noTest        = Rule(protocol: IpProtocols.Tcp);

        // Act & Assert
        Assert.False(newConnection.Overlaps(established));
        Assert.True(newConnection.Overlaps(finSet));
        Assert.True(newConnection.Overlaps(noTest));
        Assert.True(noTest.Overlaps(established));
    }

}
