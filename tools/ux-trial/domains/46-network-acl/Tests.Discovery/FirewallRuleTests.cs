using NetworkAcl.Domain;

namespace NetworkAcl.Tests.Discovery;

public sealed class FirewallRuleTests {

    private static readonly AddressPrefix V4Any = AddressPrefix.Any(IpVersion.V4);
    private static readonly AddressPrefix V6Any = AddressPrefix.Any(IpVersion.V6);

    // ---- construction

    [Fact]
    public void A_rule_reports_what_it_was_given() {
        // Arrange
        AddressPrefix source      = AddressPrefix.Parse("10.0.0.0/8");
        AddressPrefix destination = AddressPrefix.Parse("192.0.2.0/24");
        PortRange     ports       = new PortRange(1024, 65535);

        // Act
        FirewallRule rule = new FirewallRule(new RuleId("web-out"), RuleAction.Allow, source, destination, IpProtocols.Tcp, ports, PortRange.Single(443), TcpFlagMatch.NewConnection);

        // Assert
        Assert.Equal(new RuleId("web-out"), rule.Id);
        Assert.Equal(RuleAction.Allow, rule.Action);
        Assert.Equal(source, rule.Source);
        Assert.Equal(destination, rule.Destination);
        Assert.Equal(IpProtocols.Tcp, rule.Protocol);
        Assert.Equal(ports, rule.SourcePorts);
        Assert.Equal(PortRange.Single(443), rule.DestinationPorts);
        Assert.Equal(TcpFlagMatch.NewConnection, rule.TcpFlags);
        Assert.Equal(IpVersion.V4, rule.Version);
    }

    [Fact]
    public void The_version_of_a_rule_is_the_family_of_its_prefixes() {
        FirewallRule v6 = Build.Rule("v6", RuleAction.Deny, "::/0", "::/0");

        Assert.Equal(IpVersion.V6, v6.Version);
    }

    [Fact]
    public void A_null_argument_is_refused() {
        RuleId id = new RuleId("r");

        Assert.Throws<ArgumentNullException>(() => new FirewallRule(null!, RuleAction.Allow, V4Any, V4Any, null, PortRange.All, PortRange.All, null));
        Assert.Throws<ArgumentNullException>(() => new FirewallRule(id, RuleAction.Allow, null!, V4Any, null, PortRange.All, PortRange.All, null));
        Assert.Throws<ArgumentNullException>(() => new FirewallRule(id, RuleAction.Allow, V4Any, null!, null, PortRange.All, PortRange.All, null));
        Assert.Throws<ArgumentNullException>(() => new FirewallRule(id, RuleAction.Allow, V4Any, V4Any, null, null!, PortRange.All, null));
        Assert.Throws<ArgumentNullException>(() => new FirewallRule(id, RuleAction.Allow, V4Any, V4Any, null, PortRange.All, null!, null));
    }

    [Fact]
    public void An_undefined_action_is_refused() {
        RuleId anyId = Any.RuleId().Generate();

        Assert.Throws<ArgumentOutOfRangeException>(() => new FirewallRule(anyId, (RuleAction)3, V4Any, V4Any, null, PortRange.All, PortRange.All, null));
    }

    [Fact]
    public void A_rule_cannot_mix_families() {
        RuleId anyId = Any.RuleId().Generate();

        Assert.Throws<ArgumentException>(() => new FirewallRule(anyId, RuleAction.Allow, V4Any, V6Any, null, PortRange.All, PortRange.All, null));
        Assert.Throws<ArgumentException>(() => new FirewallRule(anyId, RuleAction.Allow, V6Any, V4Any, null, PortRange.All, PortRange.All, null));
    }

    [Fact]
    public void The_reserved_protocol_is_out_of_range() {
        RuleId anyId = Any.RuleId().Generate();

        Assert.Throws<ArgumentOutOfRangeException>(() => new FirewallRule(anyId, RuleAction.Allow, V4Any, V4Any, IpProtocols.Reserved, PortRange.All, PortRange.All, null));
    }

    [Fact]
    public void Icmp_needs_an_ipv4_rule_and_icmpv6_an_ipv6_one() {
        RuleId anyId = Any.RuleId().Generate();

        Assert.Throws<ArgumentException>(() => new FirewallRule(anyId, RuleAction.Allow, V6Any, V6Any, IpProtocols.Icmp, PortRange.All, PortRange.All, null));
        Assert.Throws<ArgumentException>(() => new FirewallRule(anyId, RuleAction.Allow, V4Any, V4Any, IpProtocols.Icmpv6, PortRange.All, PortRange.All, null));
        Assert.Equal(IpProtocols.Icmp, new FirewallRule(anyId, RuleAction.Allow, V4Any, V4Any, IpProtocols.Icmp, PortRange.All, PortRange.All, null).Protocol);
        Assert.Equal(IpProtocols.Icmpv6, new FirewallRule(anyId, RuleAction.Allow, V6Any, V6Any, IpProtocols.Icmpv6, PortRange.All, PortRange.All, null).Protocol);
    }

    [Theory]
    [InlineData(null)]
    [InlineData(IpProtocols.Icmp)]
    [InlineData((byte)47)]
    public void A_narrowed_source_port_range_needs_a_protocol_with_ports(byte? protocol) {
        RuleId anyId = Any.RuleId().Generate();

        Assert.Throws<ArgumentException>(() => new FirewallRule(anyId, RuleAction.Allow, V4Any, V4Any, protocol, new PortRange(1, 2), PortRange.All, null));
        Assert.Throws<ArgumentException>(() => new FirewallRule(anyId, RuleAction.Allow, V4Any, V4Any, protocol, PortRange.All, new PortRange(1, 2), null));
    }

    [Theory]
    [InlineData(IpProtocols.Tcp)]
    [InlineData(IpProtocols.Udp)]
    [InlineData(IpProtocols.Sctp)]
    public void A_protocol_with_ports_accepts_narrowed_ranges(byte protocol) {
        FirewallRule rule = Build.Rule("p", RuleAction.Allow, protocol: protocol, sourcePorts: "1024-65535", destinationPorts: "53");

        Assert.Equal(new PortRange(1024, 65535), rule.SourcePorts);
        Assert.Equal(PortRange.Single(53), rule.DestinationPorts);
    }

    [Theory]
    [InlineData(null)]
    [InlineData(IpProtocols.Udp)]
    [InlineData(IpProtocols.Icmp)]
    [InlineData(IpProtocols.Sctp)]
    public void A_flags_test_needs_protocol_tcp(byte? protocol) {
        RuleId anyId = Any.RuleId().Generate();

        Assert.Throws<ArgumentException>(() => new FirewallRule(anyId, RuleAction.Allow, V4Any, V4Any, protocol, PortRange.All, PortRange.All, TcpFlagMatch.Established));
    }

    [Fact]
    public void A_flags_test_is_accepted_with_protocol_tcp() {
        FirewallRule rule = Build.Rule("est", RuleAction.Allow, protocol: IpProtocols.Tcp, flags: TcpFlagMatch.Established);

        Assert.Equal(TcpFlagMatch.Established, rule.TcpFlags);
    }

    [Fact]
    public void ToString_lists_every_field() {
        FirewallRule rule = Build.Rule("web-out", RuleAction.Allow, "10.0.0.0/8", "0.0.0.0/0", IpProtocols.Tcp, "1024-65535", "443", TcpFlagMatch.NewConnection);

        Assert.Equal("web-out: Allow 10.0.0.0/8 -> 0.0.0.0/0 proto 6 sport 1024-65535 dport 443 flags Syn, Ack/Syn", rule.ToString());
        Assert.Equal("x: Deny 0.0.0.0/0 -> 0.0.0.0/0 proto any sport any dport any", Build.Rule("x", RuleAction.Deny).ToString());
    }

    // ---- matching

    [Fact]
    public void A_rule_open_on_every_field_matches_an_ipv4_packet() {
        // Arrange
        Ipv4Packet anyPacket = Any.Ipv4Packet().Generate();
        FirewallRule rule    = Build.Rule("open", RuleAction.Allow);

        // Act
        bool matches = rule.Matches(anyPacket);

        // Assert
        Assert.True(matches);
    }

    [Fact]
    public void An_ipv4_rule_never_matches_an_ipv6_packet_and_the_other_way_round() {
        Ipv4Packet anyV4Packet = Any.Ipv4Packet().Generate();
        Ipv6Packet anyV6Packet = Any.Ipv6Packet().Generate();

        Assert.False(Build.Rule("v4", RuleAction.Allow).Matches(anyV6Packet));
        Assert.False(Build.Rule("v6", RuleAction.Allow, "::/0", "::/0").Matches(anyV4Packet));
        Assert.True(Build.Rule("v6", RuleAction.Allow, "::/0", "::/0").Matches(anyV6Packet));
    }

    [Theory]
    [InlineData("10.0.0.0/8", "10.200.1.1", true)]
    [InlineData("10.0.0.0/8", "11.0.0.1", false)]
    [InlineData("10.0.0.0/8", "9.255.255.255", false)]
    [InlineData("192.0.2.7/32", "192.0.2.7", true)]
    [InlineData("192.0.2.7/32", "192.0.2.8", false)]
    public void The_source_prefix_selects_on_the_source_address(string sourcePrefix, string source, bool expected) {
        // Arrange: destination, ports, flags and payload are dummies, the rule does not look at them
        Ipv4Packet packet = Any.Ipv4Packet().WithSource(Ipv4Address.Parse(source)).Generate();
        FirewallRule rule = Build.Rule("src", RuleAction.Allow, source: sourcePrefix);

        // Assert
        Assert.Equal(expected, rule.Matches(packet));
    }

    [Theory]
    [InlineData("192.168.0.0/16", "192.168.255.255", true)]
    [InlineData("192.168.0.0/16", "192.169.0.0", false)]
    [InlineData("0.0.0.0/0", "8.8.8.8", true)]
    public void The_destination_prefix_selects_on_the_destination_address(string destinationPrefix, string destination, bool expected) {
        Ipv4Packet packet = Any.Ipv4Packet().WithDestination(Ipv4Address.Parse(destination)).Generate();
        FirewallRule rule = Build.Rule("dst", RuleAction.Allow, destination: destinationPrefix);

        Assert.Equal(expected, rule.Matches(packet));
    }

    [Fact]
    public void The_source_and_destination_prefixes_both_have_to_match() {
        FirewallRule rule = Build.Rule("both", RuleAction.Allow, "10.0.0.0/8", "192.0.2.0/24");

        Assert.True(rule.Matches(Build.Packet4("10.1.1.1", "192.0.2.5")));
        Assert.False(rule.Matches(Build.Packet4("10.1.1.1", "192.0.3.5")));
        Assert.False(rule.Matches(Build.Packet4("11.1.1.1", "192.0.2.5")));
        Assert.False(rule.Matches(Build.Packet4("192.0.2.5", "10.1.1.1")));
    }

    [Fact]
    public void An_ipv6_rule_matches_on_ipv6_prefixes() {
        FirewallRule rule = Build.Rule("v6", RuleAction.Allow, "2001:db8::/32", "::/0");
        Ipv6Packet   inside  = Any.Ipv6Packet().WithSource(Ipv6Address.Parse("2001:db8:1::5")).Generate();
        Ipv6Packet   outside = Any.Ipv6Packet().WithSource(Ipv6Address.Parse("2001:db9::5")).Generate();

        Assert.True(rule.Matches(inside));
        Assert.False(rule.Matches(outside));
    }

    [Theory]
    [InlineData(IpProtocols.Udp, true)]
    [InlineData(IpProtocols.Tcp, false)]
    [InlineData(IpProtocols.Icmp, false)]
    [InlineData((byte)47, false)]
    public void A_protocol_selects_on_the_protocol_number(byte packetProtocol, bool expected) {
        Ipv4Packet packet = Any.Ipv4Packet().WithProtocol(packetProtocol).Generate();
        FirewallRule rule = Build.Rule("udp", RuleAction.Allow, protocol: IpProtocols.Udp);

        Assert.Equal(expected, rule.Matches(packet));
    }

    [Theory]
    [InlineData(IpProtocols.Tcp)]
    [InlineData(IpProtocols.Udp)]
    [InlineData(IpProtocols.Icmp)]
    [InlineData((byte)47)]
    public void A_null_protocol_matches_every_protocol_including_those_without_ports(byte packetProtocol) {
        Ipv4Packet packet = Any.Ipv4Packet().WithProtocol(packetProtocol).Generate();

        Assert.True(Build.Rule("any-proto", RuleAction.Allow).Matches(packet));
    }

    [Theory]
    [InlineData(442, false)]
    [InlineData(443, true)]
    [InlineData(444, false)]
    [InlineData(0, false)]
    [InlineData(65535, false)]
    public void A_destination_port_selects_on_the_destination_port(ushort port, bool expected) {
        Ipv4Packet packet = Any.Ipv4Packet().WithProtocol(IpProtocols.Tcp).WithDestinationPort(port).Generate();
        FirewallRule rule = Build.Rule("https", RuleAction.Allow, protocol: IpProtocols.Tcp, destinationPorts: "443");

        Assert.Equal(expected, rule.Matches(packet));
    }

    [Theory]
    [InlineData(1023, false)]
    [InlineData(1024, true)]
    [InlineData(40000, true)]
    [InlineData(65535, true)]
    [InlineData(0, false)]
    public void A_source_port_range_includes_both_ends(ushort port, bool expected) {
        Ipv4Packet packet = Any.Ipv4Packet().WithProtocol(IpProtocols.Udp).WithSourcePort(port).Generate();
        FirewallRule rule = Build.Rule("ephemeral", RuleAction.Allow, protocol: IpProtocols.Udp, sourcePorts: "1024-65535");

        Assert.Equal(expected, rule.Matches(packet));
    }

    [Theory]
    [InlineData(TcpFlags.Syn, true)]
    [InlineData(TcpFlags.Syn | TcpFlags.Psh, true)]
    [InlineData(TcpFlags.Syn | TcpFlags.Ack, false)]
    [InlineData(TcpFlags.Ack, false)]
    [InlineData(TcpFlags.Rst | TcpFlags.Ack, false)]
    public void A_flags_test_selects_on_the_segment_flags(TcpFlags flags, bool expected) {
        Ipv4Packet packet = Any.Ipv4Packet().WithProtocol(IpProtocols.Tcp).WithTcpFlags(flags).Generate();
        FirewallRule rule = Build.Rule("new", RuleAction.Allow, protocol: IpProtocols.Tcp, flags: TcpFlagMatch.NewConnection);

        Assert.Equal(expected, rule.Matches(packet));
    }

    [Fact]
    public void A_rule_without_a_flags_test_matches_every_tcp_flag_combination() {
        FirewallRule rule = Build.Rule("tcp", RuleAction.Allow, protocol: IpProtocols.Tcp);

        Assert.True(rule.Matches(Any.Ipv4Packet().WithProtocol(IpProtocols.Tcp).WithTcpFlags(TcpFlags.Syn).Generate()));
        Assert.True(rule.Matches(Any.Ipv4Packet().WithProtocol(IpProtocols.Tcp).WithTcpFlags(TcpFlags.Rst | TcpFlags.Ack).Generate()));
    }

    [Fact]
    public void A_rule_on_icmp_matches_an_icmp_packet() {
        FirewallRule rule = Build.Rule("ping", RuleAction.Allow, protocol: IpProtocols.Icmp);

        Assert.True(rule.Matches(Any.Ipv4Packet().WithProtocol(IpProtocols.Icmp).Generate()));
        Assert.False(rule.Matches(Any.Ipv4Packet().WithProtocol(IpProtocols.Udp).Generate()));
    }

    [Fact]
    public void Matches_refuses_a_null_packet() {
        Assert.Throws<ArgumentNullException>(() => Build.Rule("r", RuleAction.Allow).Matches(null!));
    }

    // ---- covering

    [Theory]
    [InlineData("0.0.0.0/0", "10.0.0.0/8", true)]
    [InlineData("10.0.0.0/8", "10.1.0.0/16", true)]
    [InlineData("10.0.0.0/8", "10.0.0.0/8", true)]
    [InlineData("10.1.0.0/16", "10.0.0.0/8", false)]
    [InlineData("10.0.0.0/8", "11.0.0.0/8", false)]
    public void Covers_compares_source_prefixes(string wide, string narrow, bool expected) {
        Assert.Equal(expected, Build.Rule("w", RuleAction.Allow, source: wide).Covers(Build.Rule("n", RuleAction.Allow, source: narrow)));
    }

    [Theory]
    [InlineData("0.0.0.0/0", "10.0.0.0/8", true)]
    [InlineData("10.1.0.0/16", "10.0.0.0/8", false)]
    public void Covers_compares_destination_prefixes(string wide, string narrow, bool expected) {
        Assert.Equal(expected, Build.Rule("w", RuleAction.Allow, destination: wide).Covers(Build.Rule("n", RuleAction.Allow, destination: narrow)));
    }

    [Fact]
    public void A_rule_covers_itself_and_covers_a_copy_with_another_action() {
        FirewallRule allow = Build.Rule("a", RuleAction.Allow, "10.0.0.0/8", protocol: IpProtocols.Tcp, destinationPorts: "80");
        FirewallRule deny  = Build.Rule("b", RuleAction.Deny, "10.0.0.0/8", protocol: IpProtocols.Tcp, destinationPorts: "80");

        Assert.True(allow.Covers(allow));
        Assert.True(allow.Covers(deny));
    }

    [Fact]
    public void A_null_protocol_covers_any_protocol_but_a_set_one_does_not_cover_a_null_one() {
        FirewallRule anyProtocol = Build.Rule("a", RuleAction.Allow);
        FirewallRule tcp         = Build.Rule("t", RuleAction.Allow, protocol: IpProtocols.Tcp);
        FirewallRule udp         = Build.Rule("u", RuleAction.Allow, protocol: IpProtocols.Udp);

        Assert.True(anyProtocol.Covers(tcp));
        Assert.False(tcp.Covers(anyProtocol));
        Assert.False(tcp.Covers(udp));
    }

    [Fact]
    public void A_wider_port_range_covers_a_narrower_one() {
        FirewallRule wide   = Build.Rule("w", RuleAction.Allow, protocol: IpProtocols.Tcp, destinationPorts: "1-1023");
        FirewallRule narrow = Build.Rule("n", RuleAction.Allow, protocol: IpProtocols.Tcp, destinationPorts: "80-443");
        FirewallRule other  = Build.Rule("o", RuleAction.Allow, protocol: IpProtocols.Tcp, destinationPorts: "1000-2000");

        Assert.True(wide.Covers(narrow));
        Assert.False(narrow.Covers(wide));
        Assert.False(wide.Covers(other));
    }

    [Fact]
    public void A_wider_source_port_range_covers_a_narrower_one() {
        FirewallRule wide   = Build.Rule("w", RuleAction.Allow, protocol: IpProtocols.Udp, sourcePorts: "1024-65535");
        FirewallRule narrow = Build.Rule("n", RuleAction.Allow, protocol: IpProtocols.Udp, sourcePorts: "5000");

        Assert.True(wide.Covers(narrow));
        Assert.False(narrow.Covers(wide));
    }

    [Fact]
    public void A_missing_flags_test_covers_a_set_one_but_a_set_one_never_covers_a_missing_one() {
        FirewallRule noFlags   = Build.Rule("n", RuleAction.Allow, protocol: IpProtocols.Tcp);
        FirewallRule withFlags = Build.Rule("f", RuleAction.Allow, protocol: IpProtocols.Tcp, flags: TcpFlagMatch.Established);

        Assert.True(noFlags.Covers(withFlags));
        Assert.False(withFlags.Covers(noFlags));
    }

    [Fact]
    public void A_flags_test_covers_another_when_it_examines_no_more_flags_and_agrees() {
        TcpFlagMatch synOnly = new TcpFlagMatch(TcpFlags.Syn, TcpFlags.Syn);
        FirewallRule loose   = Build.Rule("l", RuleAction.Allow, protocol: IpProtocols.Tcp, flags: synOnly);
        FirewallRule strict  = Build.Rule("s", RuleAction.Allow, protocol: IpProtocols.Tcp, flags: TcpFlagMatch.NewConnection);

        Assert.True(loose.Covers(strict));
        Assert.False(strict.Covers(loose));
    }

    [Fact]
    public void Rules_of_different_families_never_cover_each_other() {
        FirewallRule v4 = Build.Rule("v4", RuleAction.Allow);
        FirewallRule v6 = Build.Rule("v6", RuleAction.Allow, "::/0", "::/0");

        Assert.False(v4.Covers(v6));
        Assert.False(v6.Covers(v4));
    }

    [Fact]
    public void Covers_and_overlaps_refuse_null() {
        FirewallRule rule = Build.Rule("r", RuleAction.Allow);

        Assert.Throws<ArgumentNullException>(() => rule.Covers(null!));
        Assert.Throws<ArgumentNullException>(() => rule.Overlaps(null!));
    }

    // ---- overlapping

    [Fact]
    public void Nested_prefixes_overlap_and_disjoint_ones_do_not() {
        FirewallRule wide     = Build.Rule("w", RuleAction.Allow, "10.0.0.0/8");
        FirewallRule narrow   = Build.Rule("n", RuleAction.Allow, "10.1.0.0/16");
        FirewallRule disjoint = Build.Rule("d", RuleAction.Allow, "11.0.0.0/8");

        Assert.True(wide.Overlaps(narrow));
        Assert.True(narrow.Overlaps(wide));
        Assert.False(wide.Overlaps(disjoint));
    }

    [Fact]
    public void Disjoint_destination_prefixes_do_not_overlap() {
        FirewallRule left  = Build.Rule("l", RuleAction.Allow, destination: "10.0.0.0/9");
        FirewallRule right = Build.Rule("r", RuleAction.Allow, destination: "10.128.0.0/9");

        Assert.False(left.Overlaps(right));
    }

    [Fact]
    public void Different_set_protocols_do_not_overlap_but_a_null_one_overlaps_both() {
        FirewallRule tcp = Build.Rule("t", RuleAction.Allow, protocol: IpProtocols.Tcp);
        FirewallRule udp = Build.Rule("u", RuleAction.Allow, protocol: IpProtocols.Udp);
        FirewallRule any = Build.Rule("a", RuleAction.Allow);

        Assert.False(tcp.Overlaps(udp));
        Assert.True(any.Overlaps(tcp));
        Assert.True(udp.Overlaps(any));
    }

    [Fact]
    public void Port_ranges_that_share_a_port_overlap() {
        FirewallRule low  = Build.Rule("l", RuleAction.Allow, protocol: IpProtocols.Tcp, destinationPorts: "1-100");
        FirewallRule high = Build.Rule("h", RuleAction.Allow, protocol: IpProtocols.Tcp, destinationPorts: "100-200");
        FirewallRule far  = Build.Rule("f", RuleAction.Allow, protocol: IpProtocols.Tcp, destinationPorts: "101-200");

        Assert.True(low.Overlaps(high));
        Assert.False(low.Overlaps(far));
    }

    [Fact]
    public void Source_port_ranges_that_do_not_share_a_port_do_not_overlap() {
        FirewallRule low  = Build.Rule("l", RuleAction.Allow, protocol: IpProtocols.Udp, sourcePorts: "1-100");
        FirewallRule high = Build.Rule("h", RuleAction.Allow, protocol: IpProtocols.Udp, sourcePorts: "200-300");

        Assert.False(low.Overlaps(high));
    }

    [Fact]
    public void Flags_tests_overlap_unless_they_disagree_on_a_flag_both_examine() {
        FirewallRule noFlags = Build.Rule("n", RuleAction.Allow, protocol: IpProtocols.Tcp);
        FirewallRule est     = Build.Rule("e", RuleAction.Allow, protocol: IpProtocols.Tcp, flags: TcpFlagMatch.Established);
        FirewallRule fresh   = Build.Rule("f", RuleAction.Allow, protocol: IpProtocols.Tcp, flags: TcpFlagMatch.NewConnection);
        FirewallRule push    = Build.Rule("p", RuleAction.Allow, protocol: IpProtocols.Tcp, flags: new TcpFlagMatch(TcpFlags.Psh, TcpFlags.Psh));

        Assert.True(noFlags.Overlaps(est));
        Assert.True(est.Overlaps(push));
        Assert.False(est.Overlaps(fresh));
    }

    [Fact]
    public void Rules_of_different_families_do_not_overlap() {
        Assert.False(Build.Rule("v4", RuleAction.Allow).Overlaps(Build.Rule("v6", RuleAction.Allow, "::/0", "::/0")));
    }

    // ---- scaffolded rules

    [Fact]
    public void A_scaffolded_rule_matches_a_packet_aimed_at_its_own_prefixes() {
        // Arrange: pin the family and what the test is about; the rest is arbitrary
        FirewallRule rule = Any.FirewallRule().WithSource(AddressPrefix.Parse("10.0.0.0/8")).WithDestination(AddressPrefix.Parse("192.0.2.0/24"))
                               .WithProtocol(IpProtocols.Tcp).WithSourcePorts(PortRange.All).WithDestinationPorts(PortRange.All).WithTcpFlags(null).Generate();
        Ipv4Packet packet = Any.Ipv4Packet().WithSource(Ipv4Address.Parse("10.4.4.4")).WithDestination(Ipv4Address.Parse("192.0.2.77")).WithProtocol(IpProtocols.Tcp).Generate();

        // Act
        bool matches = rule.Matches(packet);

        // Assert
        Assert.True(matches);
    }

}
