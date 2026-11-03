using JustDummies;

using NetworkAcl.Domain;
using NetworkAcl.Tests.Expert.Arbitrary;

using Xunit;

using static NetworkAcl.Tests.Expert.Shorthand;

namespace NetworkAcl.Tests.Expert.Tests;

public sealed class FirewallPolicyEvaluationTests {

    [Theory]
    [InlineData(RuleAction.Allow)]
    [InlineData(RuleAction.Deny)]
    public void A_packet_no_rule_matches_gets_the_default_action_and_no_rule(RuleAction defaultAction) {
        // Arrange
        FirewallPolicy policy = Policy(defaultAction, Any.Boolean().Generate(), Rule(id: "only-udp", protocol: IpProtocols.Udp));
        Ipv4Packet tcp = Any.Ipv4Packet().WithProtocol(IpProtocols.Tcp).Generate();

        // Act
        Verdict verdict = policy.Evaluate(tcp);

        // Assert
        Assert.Equal(new Verdict(defaultAction, null), verdict);
        Assert.True(verdict.IsDefault);
    }

    [Fact]
    public void An_empty_policy_gives_the_default_action() {
        // Arrange
        FirewallPolicy policy = Policy(RuleAction.Allow);
        Ipv4Packet anyPacket = Any.Ipv4Packet().Generate();

        // Act & Assert
        Assert.Equal(new Verdict(RuleAction.Allow, null), policy.Evaluate(anyPacket));
    }

    [Fact]
    public void A_matching_rule_decides_with_its_own_action_even_against_the_default() {
        // Arrange
        FirewallPolicy policy = Policy(RuleAction.Deny, Any.Boolean().Generate(), Rule(id: "allow-dns", action: RuleAction.Allow, protocol: IpProtocols.Udp, destinationPorts: Port(53)));
        Ipv4Packet dns = Any.Ipv4Packet().WithProtocol(IpProtocols.Udp).WithDestinationPort(53).Generate();

        // Act
        Verdict verdict = policy.Evaluate(dns);

        // Assert
        Assert.Equal(new Verdict(RuleAction.Allow, Id("allow-dns")), verdict);
        Assert.True(verdict.IsAllowed);
    }

    [Fact]
    public void The_first_matching_rule_wins_over_a_later_one_that_matches_too() {
        // Arrange: a narrow deny placed before a broad allow (a layout a strict policy accepts too)
        FirewallPolicy policy = Policy(RuleAction.Deny, Any.Boolean().Generate(),
                                       Rule(id: "block-bad-host", action: RuleAction.Deny, source: "10.6.6.6/32"),
                                       Rule(id: "allow-lan", action: RuleAction.Allow, source: "10.0.0.0/8"));
        Ipv4Packet fromBadHost = Any.Ipv4Packet().WithSource(V4("10.6.6.6")).Generate();
        Ipv4Packet fromOtherHost = Any.Ipv4Packet().WithSource(V4("10.6.6.7")).Generate();

        // Act & Assert
        Assert.Equal(new Verdict(RuleAction.Deny, Id("block-bad-host")), policy.Evaluate(fromBadHost));
        Assert.Equal(new Verdict(RuleAction.Allow, Id("allow-lan")), policy.Evaluate(fromOtherHost));
    }

    [Fact]
    public void Moving_a_rule_changes_which_rule_decides() {
        // Arrange
        FirewallPolicy policy = Policy(RuleAction.Deny, strict: false,
                                       Rule(id: "allow-lan", action: RuleAction.Allow, source: "10.0.0.0/8"),
                                       Rule(id: "block-bad-host", action: RuleAction.Deny, source: "10.6.6.6/32"));
        Ipv4Packet fromBadHost = Any.Ipv4Packet().WithSource(V4("10.6.6.6")).Generate();

        // Act
        Verdict before = policy.Evaluate(fromBadHost);
        policy.Move(Id("block-bad-host"), 0);
        Verdict after = policy.Evaluate(fromBadHost);

        // Assert
        Assert.Equal(new Verdict(RuleAction.Allow, Id("allow-lan")), before);
        Assert.Equal(new Verdict(RuleAction.Deny, Id("block-bad-host")), after);
    }

    [Fact]
    public void A_rule_of_one_family_never_decides_a_packet_of_the_other() {
        // Arrange
        FirewallPolicy policy = Policy(RuleAction.Deny, Any.Boolean().Generate(), Rule(id: "allow-all-v4", action: RuleAction.Allow));
        Ipv6Packet anyV6Packet = Any.Ipv6Packet().Generate();

        // Act
        Verdict verdict = policy.Evaluate(anyV6Packet);

        // Assert
        Assert.Equal(new Verdict(RuleAction.Deny, null), verdict);
    }

    [Fact]
    public void An_ipv6_rule_decides_an_ipv6_packet() {
        // Arrange
        FirewallPolicy policy = Policy(RuleAction.Deny, Any.Boolean().Generate(), Rule(id: "allow-icmpv6", action: RuleAction.Allow, source: "::/0", destination: "::/0", protocol: IpProtocols.Icmpv6));
        Ipv6Packet ping = Any.Ipv6Packet().WithProtocol(IpProtocols.Icmpv6).Generate();

        // Act & Assert
        Assert.Equal(new Verdict(RuleAction.Allow, Id("allow-icmpv6")), policy.Evaluate(ping));
    }

    [Fact]
    public void The_deciding_rule_counts_one_hit_and_the_others_none() {
        // Arrange
        FirewallPolicy policy = Policy(RuleAction.Deny, Any.Boolean().Generate(),
                                       Rule(id: "tcp-rule", protocol: IpProtocols.Tcp),
                                       Rule(id: "udp-rule", protocol: IpProtocols.Udp),
                                       Rule(id: "catch-all"));
        Ipv4Packet tcp = Any.Ipv4Packet().WithProtocol(IpProtocols.Tcp).Generate();

        // Act
        policy.Evaluate(tcp);
        policy.Evaluate(tcp);

        // Assert: "catch-all" matches too, but only the first match is counted
        Assert.Equal(2L, policy.HitsOf(Id("tcp-rule")));
        Assert.Equal(0L, policy.HitsOf(Id("udp-rule")));
        Assert.Equal(0L, policy.HitsOf(Id("catch-all")));
        Assert.Equal(0L, policy.DefaultHits);
    }

    [Fact]
    public void A_packet_no_rule_matches_counts_a_default_hit_and_no_rule_hit() {
        // Arrange
        FirewallPolicy policy = Policy(RuleAction.Allow, Any.Boolean().Generate(), Rule(id: "only-udp", protocol: IpProtocols.Udp));
        Ipv4Packet tcp = Any.Ipv4Packet().WithProtocol(IpProtocols.Tcp).Generate();

        // Act
        policy.Evaluate(tcp);
        policy.Evaluate(tcp);
        policy.Evaluate(tcp);

        // Assert
        Assert.Equal(3L, policy.DefaultHits);
        Assert.Equal(0L, policy.HitsOf(Id("only-udp")));
    }

    [Fact]
    public void Evaluating_does_not_touch_the_revision() {
        // Arrange
        FirewallPolicy policy = Policy(RuleAction.Deny, Any.Boolean().Generate(), Rule(id: "any-rule"));
        long revision = policy.Revision;

        // Act
        policy.Evaluate(Any.Ipv4Packet().Generate());

        // Assert
        Assert.Equal(revision, policy.Revision);
    }

    [Fact]
    public void ResetCounters_zeroes_every_counter_and_keeps_the_rules_and_the_revision() {
        // Arrange
        FirewallPolicy policy = Policy(RuleAction.Deny, Any.Boolean().Generate(), Rule(id: "tcp-rule", protocol: IpProtocols.Tcp));
        policy.Evaluate(Any.Ipv4Packet().WithProtocol(IpProtocols.Tcp).Generate());
        policy.Evaluate(Any.Ipv4Packet().WithProtocol(IpProtocols.Udp).Generate());
        long revision = policy.Revision;

        // Act
        policy.ResetCounters();

        // Assert
        Assert.Equal(0L, policy.HitsOf(Id("tcp-rule")));
        Assert.Equal(0L, policy.DefaultHits);
        Assert.Equal(1, policy.Count);
        Assert.Equal(revision, policy.Revision);
    }

    [Fact]
    public void HitsOf_refuses_an_unknown_rule_and_a_missing_id() {
        // Arrange
        FirewallPolicy policy = Policy(RuleAction.Deny);

        // Act
        PolicyRefusedException refusal = Assert.Throws<PolicyRefusedException>(() => policy.HitsOf(Id("absent")));

        // Assert
        Assert.Equal(PolicyRefusalReason.RuleNotFound, refusal.Reason);
        Assert.Throws<ArgumentNullException>(() => policy.HitsOf(null!));
    }

    [Fact]
    public void Evaluate_refuses_a_missing_packet() {
        Assert.Throws<ArgumentNullException>(() => Policy(RuleAction.Deny).Evaluate(null!));
    }

    [Fact]
    public void A_stateless_policy_judges_each_packet_on_its_own() {
        // Arrange: allow only new connections out; the reply (SYN+ACK) is a different packet with no memory of the first
        FirewallPolicy policy = Policy(RuleAction.Deny, Any.Boolean().Generate(),
                                       Rule(id: "allow-new-out", protocol: IpProtocols.Tcp, tcpFlags: TcpFlagMatch.NewConnection));
        Ipv4Packet syn    = Any.Ipv4Packet().WithProtocol(IpProtocols.Tcp).WithTcpFlags(TcpFlags.Syn).Generate();
        Ipv4Packet synAck = Any.Ipv4Packet().WithProtocol(IpProtocols.Tcp).WithTcpFlags(TcpFlags.Syn | TcpFlags.Ack).Generate();

        // Act & Assert
        Assert.True(policy.Evaluate(syn).IsAllowed);
        Assert.False(policy.Evaluate(synAck).IsAllowed);
        Assert.True(policy.Evaluate(syn).IsAllowed);
    }

}

public sealed class FirewallPolicyAnalysisTests {

    [Fact]
    public void An_empty_policy_has_no_anomaly() {
        Assert.Empty(Policy(RuleAction.Deny).FindAnomalies());
    }

    [Fact]
    public void A_rule_covered_by_an_earlier_rule_of_the_opposite_action_is_shadowed() {
        // Arrange
        FirewallPolicy policy = Policy(RuleAction.Deny, strict: false,
                                       Rule(id: "deny-lan", action: RuleAction.Deny, source: "10.0.0.0/8"),
                                       Rule(id: "allow-host", action: RuleAction.Allow, source: "10.1.2.3/32"));

        // Act
        IReadOnlyList<RuleAnomaly> anomalies = policy.FindAnomalies();

        // Assert
        Assert.Equal([new RuleAnomaly(Id("allow-host"), AnomalyKind.Shadowed, Id("deny-lan"))], anomalies);
        Assert.True(anomalies[0].IsUnreachable);
    }

    [Fact]
    public void A_rule_covered_by_an_earlier_rule_of_the_same_action_is_redundant_to_it() {
        // Arrange
        FirewallPolicy policy = Policy(RuleAction.Deny, strict: false,
                                       Rule(id: "allow-lan", action: RuleAction.Allow, source: "10.0.0.0/8"),
                                       Rule(id: "allow-host", action: RuleAction.Allow, source: "10.1.2.3/32"));

        // Act & Assert
        Assert.Equal([new RuleAnomaly(Id("allow-host"), AnomalyKind.RedundantToEarlier, Id("allow-lan"))], policy.FindAnomalies());
    }

    [Fact]
    public void The_first_earlier_rule_that_covers_is_the_one_named() {
        // Arrange
        FirewallPolicy policy = Policy(RuleAction.Deny, strict: false,
                                       Rule(id: "wide", action: RuleAction.Allow, source: "10.0.0.0/8"),
                                       Rule(id: "wider", action: RuleAction.Allow, source: "0.0.0.0/0"),
                                       Rule(id: "host", action: RuleAction.Deny, source: "10.1.2.3/32"));

        // Act
        IReadOnlyList<RuleAnomaly> anomalies = policy.FindAnomalies();

        // Assert: "wider" is itself reported against nothing earlier, "host" names "wide" (the first coverer), not "wider"
        Assert.Contains(new RuleAnomaly(Id("host"), AnomalyKind.Shadowed, Id("wide")), anomalies);
    }

    [Fact]
    public void A_rule_covered_by_a_later_rule_of_the_same_action_is_redundant_to_it_when_nothing_between_decides_otherwise() {
        // Arrange
        FirewallPolicy policy = Policy(RuleAction.Deny, strict: false,
                                       Rule(id: "allow-host", action: RuleAction.Allow, source: "10.1.2.3/32"),
                                       Rule(id: "allow-lan", action: RuleAction.Allow, source: "10.0.0.0/8"));

        // Act & Assert
        Assert.Equal([new RuleAnomaly(Id("allow-host"), AnomalyKind.RedundantToLater, Id("allow-lan"))], policy.FindAnomalies());
    }

    [Fact]
    public void A_rule_between_that_does_not_overlap_does_not_prevent_the_redundancy_to_a_later_rule() {
        // Arrange
        FirewallPolicy policy = Policy(RuleAction.Deny, strict: false,
                                       Rule(id: "allow-host", action: RuleAction.Allow, source: "10.1.2.3/32"),
                                       Rule(id: "deny-elsewhere", action: RuleAction.Deny, source: "192.168.0.0/16"),
                                       Rule(id: "allow-lan", action: RuleAction.Allow, source: "10.0.0.0/8"));

        // Act
        IReadOnlyList<RuleAnomaly> anomalies = policy.FindAnomalies();

        // Assert: "deny-elsewhere" is a separate finding (it agrees with the default), not an obstacle
        Assert.Contains(new RuleAnomaly(Id("allow-host"), AnomalyKind.RedundantToLater, Id("allow-lan")), anomalies);
    }

    [Fact]
    public void A_rule_between_of_the_other_action_that_overlaps_makes_the_earlier_rule_matter() {
        // Arrange: "deny-lan-ssh" carves an exception out of what "allow-lan" later allows
        FirewallPolicy policy = Policy(RuleAction.Deny, strict: false,
                                       Rule(id: "allow-host", action: RuleAction.Allow, source: "10.1.2.3/32"),
                                       Rule(id: "deny-ssh", action: RuleAction.Deny, protocol: IpProtocols.Tcp, destinationPorts: Port(22)),
                                       Rule(id: "allow-lan", action: RuleAction.Allow, source: "10.0.0.0/8"));

        // Act
        IReadOnlyList<RuleAnomaly> anomalies = policy.FindAnomalies();

        // Assert
        Assert.DoesNotContain(anomalies, anomaly => anomaly.Rule.Equals(Id("allow-host")));
    }

    [Fact]
    public void A_rule_between_of_the_same_action_does_not_prevent_the_redundancy_to_a_later_rule() {
        // Arrange
        FirewallPolicy policy = Policy(RuleAction.Deny, strict: false,
                                       Rule(id: "allow-host", action: RuleAction.Allow, source: "10.1.2.3/32"),
                                       Rule(id: "allow-ssh", action: RuleAction.Allow, protocol: IpProtocols.Tcp, destinationPorts: Port(22)),
                                       Rule(id: "allow-lan", action: RuleAction.Allow, source: "10.0.0.0/8"));

        // Act & Assert
        Assert.Contains(new RuleAnomaly(Id("allow-host"), AnomalyKind.RedundantToLater, Id("allow-lan")), policy.FindAnomalies());
    }

    [Fact]
    public void A_last_rule_with_the_default_action_is_redundant_to_the_default() {
        // Arrange
        FirewallPolicy policy = Policy(RuleAction.Deny, strict: false,
                                       Rule(id: "allow-web", action: RuleAction.Allow, protocol: IpProtocols.Tcp, destinationPorts: Port(80)),
                                       Rule(id: "deny-telnet", action: RuleAction.Deny, protocol: IpProtocols.Tcp, destinationPorts: Port(23)));

        // Act
        IReadOnlyList<RuleAnomaly> anomalies = policy.FindAnomalies();

        // Assert
        RuleAnomaly anomaly = Assert.Single(anomalies);
        Assert.Equal(new RuleAnomaly(Id("deny-telnet"), AnomalyKind.RedundantToDefault, null), anomaly);
        Assert.False(anomaly.IsUnreachable);
    }

    [Fact]
    public void A_later_overlapping_rule_of_the_other_action_makes_a_default_action_rule_matter() {
        // Arrange: "deny-lan" shields the LAN from the broader "allow-tcp" that follows
        FirewallPolicy policy = Policy(RuleAction.Deny, strict: false,
                                       Rule(id: "deny-lan", action: RuleAction.Deny, source: "10.0.0.0/8", protocol: IpProtocols.Tcp),
                                       Rule(id: "allow-tcp", action: RuleAction.Allow, protocol: IpProtocols.Tcp));

        // Act & Assert
        Assert.Empty(policy.FindAnomalies());
    }

    [Fact]
    public void A_later_disjoint_rule_of_the_other_action_does_not_make_a_default_action_rule_matter() {
        // Arrange
        FirewallPolicy policy = Policy(RuleAction.Deny, strict: false,
                                       Rule(id: "deny-lan", action: RuleAction.Deny, source: "10.0.0.0/8"),
                                       Rule(id: "allow-dmz", action: RuleAction.Allow, source: "192.0.2.0/24"));

        // Act & Assert
        Assert.Equal([new RuleAnomaly(Id("deny-lan"), AnomalyKind.RedundantToDefault, null)], policy.FindAnomalies());
    }

    [Fact]
    public void Redundancy_to_the_default_follows_the_default_action() {
        // Arrange
        FirewallPolicy permissive = Policy(RuleAction.Allow, strict: false, Rule(id: "allow-lan", action: RuleAction.Allow, source: "10.0.0.0/8"));
        FirewallPolicy restrictive = Policy(RuleAction.Deny, strict: false, Rule(id: "allow-lan", action: RuleAction.Allow, source: "10.0.0.0/8"));

        // Act & Assert
        Assert.Equal([new RuleAnomaly(Id("allow-lan"), AnomalyKind.RedundantToDefault, null)], permissive.FindAnomalies());
        Assert.Empty(restrictive.FindAnomalies());
    }

    [Fact]
    public void A_rule_has_at_most_one_anomaly_and_the_earlier_coverage_comes_first() {
        // Arrange: "host" is covered by an earlier rule, by a later rule of its action, and agrees with the default
        FirewallPolicy policy = Policy(RuleAction.Deny, strict: false,
                                       Rule(id: "deny-lan", action: RuleAction.Deny, source: "10.0.0.0/8"),
                                       Rule(id: "deny-host", action: RuleAction.Deny, source: "10.1.2.3/32"),
                                       Rule(id: "deny-all", action: RuleAction.Deny, source: "0.0.0.0/0"));

        // Act
        IReadOnlyList<RuleAnomaly> anomalies = policy.FindAnomalies();

        // Assert
        Assert.Equal([new RuleAnomaly(Id("deny-lan"), AnomalyKind.RedundantToLater, Id("deny-all")),
                      new RuleAnomaly(Id("deny-host"), AnomalyKind.RedundantToEarlier, Id("deny-lan")),
                      new RuleAnomaly(Id("deny-all"), AnomalyKind.RedundantToDefault, null)],
                     anomalies);
    }

    [Fact]
    public void Anomalies_come_in_rule_order() {
        // Arrange
        FirewallPolicy policy = Policy(RuleAction.Allow, strict: false,
                                       Rule(id: "deny-lan", action: RuleAction.Deny, source: "10.0.0.0/8"),
                                       Rule(id: "allow-dmz", action: RuleAction.Allow, source: "192.0.2.0/24"),
                                       Rule(id: "allow-lan-host", action: RuleAction.Allow, source: "10.1.2.3/32"));

        // Act
        IReadOnlyList<RuleAnomaly> anomalies = policy.FindAnomalies();

        // Assert
        Assert.Equal(["allow-dmz", "allow-lan-host"], anomalies.Select(anomaly => anomaly.Rule.Value).ToArray());
    }

    [Fact]
    public void A_clean_policy_has_no_anomaly() {
        // Arrange
        FirewallPolicy policy = Policy(RuleAction.Deny, strict: false,
                                       Rule(id: "allow-web", action: RuleAction.Allow, protocol: IpProtocols.Tcp, destinationPorts: Ports(80, 443)),
                                       Rule(id: "allow-dns", action: RuleAction.Allow, protocol: IpProtocols.Udp, destinationPorts: Port(53)));

        // Act & Assert
        Assert.Empty(policy.FindAnomalies());
    }

    [Fact]
    public void A_rule_hidden_only_by_several_earlier_rules_together_is_not_reported() {
        // Arrange: the two /9 halves together are the /8, but no single earlier rule covers it
        FirewallPolicy policy = Policy(RuleAction.Allow, strict: false,
                                       Rule(id: "allow-low-half", action: RuleAction.Allow, source: "10.0.0.0/9"),
                                       Rule(id: "allow-high-half", action: RuleAction.Allow, source: "10.128.0.0/9"),
                                       Rule(id: "deny-lan", action: RuleAction.Deny, source: "10.0.0.0/8"));

        // Act
        IReadOnlyList<RuleAnomaly> anomalies = policy.FindAnomalies();

        // Assert
        Assert.DoesNotContain(anomalies, anomaly => anomaly.Rule.Equals(Id("deny-lan")));
        Assert.True(policy.IsReachable(Id("deny-lan")));
    }

    [Fact]
    public void IsReachable_is_true_for_the_first_rule_and_false_once_an_earlier_rule_covers_it() {
        // Arrange
        FirewallPolicy policy = Policy(RuleAction.Deny, strict: false,
                                       Rule(id: "broad", source: "10.0.0.0/8"),
                                       Rule(id: "narrow", source: "10.1.0.0/16"),
                                       Rule(id: "elsewhere", source: "192.168.0.0/16"));

        // Act & Assert
        Assert.True(policy.IsReachable(Id("broad")));
        Assert.False(policy.IsReachable(Id("narrow")));
        Assert.True(policy.IsReachable(Id("elsewhere")));
    }

    [Fact]
    public void IsReachable_depends_on_position_not_on_the_action() {
        // Arrange
        FirewallPolicy policy = Policy(RuleAction.Deny, strict: false,
                                       Rule(id: "deny-lan", action: RuleAction.Deny, source: "10.0.0.0/8"),
                                       Rule(id: "allow-host", action: RuleAction.Allow, source: "10.1.2.3/32"));

        // Act
        bool before = policy.IsReachable(Id("allow-host"));
        policy.Move(Id("allow-host"), 0);
        bool after = policy.IsReachable(Id("allow-host"));

        // Assert
        Assert.False(before);
        Assert.True(after);
    }

    [Fact]
    public void IsReachable_refuses_an_unknown_rule() {
        // Arrange
        FirewallPolicy policy = Policy(RuleAction.Deny);

        // Act
        PolicyRefusedException refusal = Assert.Throws<PolicyRefusedException>(() => policy.IsReachable(Id("absent")));

        // Assert
        Assert.Equal(PolicyRefusalReason.RuleNotFound, refusal.Reason);
    }

    [Fact]
    public void IsReachable_refuses_a_missing_id() {
        Assert.Throws<ArgumentNullException>(() => Policy(RuleAction.Deny).IsReachable(null!));
    }

}
