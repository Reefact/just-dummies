using JustDummies;

using NetworkAcl.Domain;

using Xunit;

using static NetworkAcl.Tests.Regular.PolicyFixtures;

namespace NetworkAcl.Tests.Regular;

public sealed class PolicyAnalysisTests {

    // ---- FindAnomalies ---------------------------------------------------------------------------------------

    [Fact]
    public void An_empty_policy_has_no_anomaly() {
        // Arrange
        FirewallPolicy policy = PolicyOf(Any.Enum<RuleAction>().Generate(), strict: false);

        // Assert
        Assert.Empty(policy.FindAnomalies());
    }

    [Fact]
    public void A_rule_an_earlier_one_covers_with_the_opposite_action_is_shadowed() {
        // Arrange
        FirewallPolicy policy = PolicyOf(RuleAction.Deny, strict: false);
        List<RuleId>   ids    = DistinctIds(2);
        policy.Append(Rule(ids[0], RuleAction.Allow, "10.0.0.0/8", "0.0.0.0/0"));
        policy.Append(Rule(ids[1], RuleAction.Deny, "10.1.0.0/16", "0.0.0.0/0"));

        // Act
        IReadOnlyList<RuleAnomaly> anomalies = policy.FindAnomalies();

        // Assert
        RuleAnomaly anomaly = Assert.Single(anomalies);
        Assert.Equal(new RuleAnomaly(ids[1], AnomalyKind.Shadowed, ids[0]), anomaly);
        Assert.True(anomaly.IsUnreachable);
    }

    [Fact]
    public void A_rule_an_earlier_one_covers_with_the_same_action_is_redundant_to_it() {
        // Arrange
        FirewallPolicy policy = PolicyOf(RuleAction.Deny, strict: false);
        List<RuleId>   ids    = DistinctIds(2);
        policy.Append(Rule(ids[0], RuleAction.Allow, "10.0.0.0/8", "0.0.0.0/0"));
        policy.Append(Rule(ids[1], RuleAction.Allow, "10.1.0.0/16", "0.0.0.0/0"));

        // Act
        IReadOnlyList<RuleAnomaly> anomalies = policy.FindAnomalies();

        // Assert
        Assert.Equal(new RuleAnomaly(ids[1], AnomalyKind.RedundantToEarlier, ids[0]), Assert.Single(anomalies));
    }

    [Fact]
    public void The_first_covering_rule_is_the_one_named() {
        // Arrange
        FirewallPolicy policy = PolicyOf(RuleAction.Deny, strict: false);
        List<RuleId>   ids    = DistinctIds(3);
        policy.Append(Rule(ids[0], RuleAction.Allow, "10.0.0.0/8", "0.0.0.0/0"));
        policy.Append(Rule(ids[1], RuleAction.Deny, "10.0.0.0/8", "0.0.0.0/0"));
        policy.Append(Rule(ids[2], RuleAction.Allow, "10.1.0.0/16", "0.0.0.0/0"));

        // Act
        IReadOnlyList<RuleAnomaly> anomalies = policy.FindAnomalies();

        // Assert
        Assert.Contains(new RuleAnomaly(ids[2], AnomalyKind.RedundantToEarlier, ids[0]), anomalies);
        Assert.Contains(new RuleAnomaly(ids[1], AnomalyKind.Shadowed, ids[0]), anomalies);
    }

    [Fact]
    public void A_rule_a_later_one_of_the_same_action_covers_is_redundant_to_it() {
        // Arrange
        FirewallPolicy policy = PolicyOf(RuleAction.Deny, strict: false);
        List<RuleId>   ids    = DistinctIds(2);
        policy.Append(Rule(ids[0], RuleAction.Allow, "10.1.0.0/16", "0.0.0.0/0"));
        policy.Append(Rule(ids[1], RuleAction.Allow, "10.0.0.0/8", "0.0.0.0/0"));

        // Act
        IReadOnlyList<RuleAnomaly> anomalies = policy.FindAnomalies();

        // Assert
        Assert.Equal(new RuleAnomaly(ids[0], AnomalyKind.RedundantToLater, ids[1]), Assert.Single(anomalies));
        Assert.False(anomalies[0].IsUnreachable);
    }

    [Fact]
    public void A_rule_between_the_two_that_overlaps_it_with_the_other_action_makes_it_necessary() {
        // Arrange
        FirewallPolicy policy = PolicyOf(RuleAction.Deny, strict: false);
        List<RuleId>   ids    = DistinctIds(3);
        policy.Append(Rule(ids[0], RuleAction.Allow, "10.1.0.0/16", "0.0.0.0/0"));
        policy.Append(Rule(ids[1], RuleAction.Deny, "10.1.2.0/24", "0.0.0.0/0"));
        policy.Append(Rule(ids[2], RuleAction.Allow, "10.0.0.0/8", "0.0.0.0/0"));

        // Act
        IReadOnlyList<RuleAnomaly> anomalies = policy.FindAnomalies();

        // Assert: the narrow allow decides 10.1.2.x before the deny, then the wide allow, so the first is no longer removable
        Assert.DoesNotContain(anomalies, anomaly => anomaly.Rule.Equals(ids[0]));
    }

    [Fact]
    public void A_rule_between_that_does_not_overlap_does_not_prevent_the_redundancy() {
        // Arrange
        FirewallPolicy policy = PolicyOf(RuleAction.Deny, strict: false);
        List<RuleId>   ids    = DistinctIds(3);
        policy.Append(Rule(ids[0], RuleAction.Allow, "10.1.0.0/16", "0.0.0.0/0"));
        policy.Append(Rule(ids[1], RuleAction.Deny, "172.16.0.0/12", "0.0.0.0/0"));
        policy.Append(Rule(ids[2], RuleAction.Allow, "10.0.0.0/8", "0.0.0.0/0"));

        // Act
        IReadOnlyList<RuleAnomaly> anomalies = policy.FindAnomalies();

        // Assert
        Assert.Contains(new RuleAnomaly(ids[0], AnomalyKind.RedundantToLater, ids[2]), anomalies);
    }

    [Fact]
    public void A_rule_that_does_what_the_default_does_and_nothing_later_contradicts_is_redundant_to_the_default() {
        // Arrange
        RuleAction     anyDefault = Any.Enum<RuleAction>().Generate();
        FirewallPolicy policy     = PolicyOf(anyDefault, strict: false);
        RuleId         anyId      = Any.RuleId().Generate();
        policy.Append(Rule(anyId, anyDefault, "10.0.0.0/8", "0.0.0.0/0"));

        // Act
        IReadOnlyList<RuleAnomaly> anomalies = policy.FindAnomalies();

        // Assert
        RuleAnomaly anomaly = Assert.Single(anomalies);
        Assert.Equal(AnomalyKind.RedundantToDefault, anomaly.Kind);
        Assert.Null(anomaly.RelatedRule);
        Assert.False(anomaly.IsUnreachable);
    }

    [Fact]
    public void A_later_rule_of_the_other_action_that_overlaps_keeps_a_default_action_rule_useful() {
        // Arrange
        FirewallPolicy policy = PolicyOf(RuleAction.Deny, strict: false);
        List<RuleId>   ids    = DistinctIds(2);
        policy.Append(Rule(ids[0], RuleAction.Deny, "10.1.0.0/16", "0.0.0.0/0"));
        policy.Append(Rule(ids[1], RuleAction.Allow, "10.0.0.0/8", "0.0.0.0/0"));

        // Act
        IReadOnlyList<RuleAnomaly> anomalies = policy.FindAnomalies();

        // Assert
        Assert.Empty(anomalies);
    }

    [Fact]
    public void A_later_rule_of_the_other_action_that_does_not_overlap_does_not_keep_it_useful() {
        // Arrange
        FirewallPolicy policy = PolicyOf(RuleAction.Deny, strict: false);
        List<RuleId>   ids    = DistinctIds(2);
        policy.Append(Rule(ids[0], RuleAction.Deny, "10.1.0.0/16", "0.0.0.0/0"));
        policy.Append(Rule(ids[1], RuleAction.Allow, "172.16.0.0/12", "0.0.0.0/0"));

        // Act
        IReadOnlyList<RuleAnomaly> anomalies = policy.FindAnomalies();

        // Assert
        Assert.Equal(new RuleAnomaly(ids[0], AnomalyKind.RedundantToDefault, null), Assert.Single(anomalies));
    }

    [Fact]
    public void A_rule_opposite_to_the_default_is_not_redundant_to_it() {
        // Arrange
        FirewallPolicy policy = PolicyOf(RuleAction.Deny, strict: false);
        RuleId         anyId  = Any.RuleId().Generate();
        policy.Append(Rule(anyId, RuleAction.Allow, "10.0.0.0/8", "0.0.0.0/0"));

        // Assert
        Assert.Empty(policy.FindAnomalies());
    }

    [Fact]
    public void There_is_at_most_one_anomaly_per_rule_and_they_come_in_rule_order() {
        // Arrange
        FirewallPolicy policy = PolicyOf(RuleAction.Deny, strict: false);
        List<RuleId>   ids    = DistinctIds(4);
        policy.Append(Rule(ids[0], RuleAction.Allow, "10.0.0.0/8", "0.0.0.0/0"));
        policy.Append(Rule(ids[1], RuleAction.Allow, "10.1.0.0/16", "0.0.0.0/0"));
        policy.Append(Rule(ids[2], RuleAction.Deny, "10.1.2.0/24", "0.0.0.0/0"));
        policy.Append(Rule(ids[3], RuleAction.Deny, "172.16.0.0/12", "0.0.0.0/0"));

        // Act
        IReadOnlyList<RuleAnomaly> anomalies = policy.FindAnomalies();

        // Assert
        Assert.Equal(new[] { ids[1], ids[2], ids[3] }, anomalies.Select(anomaly => anomaly.Rule).ToArray());
        Assert.Equal(new[] { AnomalyKind.RedundantToEarlier, AnomalyKind.Shadowed, AnomalyKind.RedundantToDefault }, anomalies.Select(anomaly => anomaly.Kind).ToArray());
    }

    [Fact]
    public void A_rule_hidden_only_by_several_earlier_rules_together_is_not_reported() {
        // Arrange
        FirewallPolicy policy = PolicyOf(RuleAction.Allow, strict: false);
        List<RuleId>   ids    = DistinctIds(3);
        policy.Append(Rule(ids[0], RuleAction.Allow, "10.0.0.0/9", "0.0.0.0/0"));
        policy.Append(Rule(ids[1], RuleAction.Allow, "10.128.0.0/9", "0.0.0.0/0"));
        policy.Append(Rule(ids[2], RuleAction.Deny, "10.0.0.0/8", "0.0.0.0/0"));

        // Act
        IReadOnlyList<RuleAnomaly> anomalies = policy.FindAnomalies();

        // Assert: together the two halves hide the deny, which the analysis does not see
        Assert.DoesNotContain(anomalies, anomaly => anomaly.Rule.Equals(ids[2]));
    }

    [Fact]
    public void An_ipv4_rule_and_an_ipv6_rule_never_make_each_other_useless() {
        // Arrange
        FirewallPolicy policy = PolicyOf(RuleAction.Deny, strict: false);
        List<RuleId>   ids    = DistinctIds(2);
        policy.Append(Rule(ids[0], RuleAction.Allow, "0.0.0.0/0", "0.0.0.0/0"));
        policy.Append(Rule(ids[1], RuleAction.Allow, "::/0", "::/0"));

        // Assert
        Assert.Empty(policy.FindAnomalies());
    }

    // ---- IsReachable -----------------------------------------------------------------------------------------

    [Fact]
    public void A_rule_is_reachable_unless_an_earlier_rule_covers_it() {
        // Arrange
        FirewallPolicy policy = PolicyOf(RuleAction.Deny, strict: false);
        List<RuleId>   ids    = DistinctIds(3);
        policy.Append(Rule(ids[0], RuleAction.Allow, "10.0.0.0/8", "0.0.0.0/0"));
        policy.Append(Rule(ids[1], RuleAction.Deny, "10.1.0.0/16", "0.0.0.0/0"));
        policy.Append(Rule(ids[2], RuleAction.Allow, "172.16.0.0/12", "0.0.0.0/0"));

        // Assert
        Assert.True(policy.IsReachable(ids[0]));
        Assert.False(policy.IsReachable(ids[1]));
        Assert.True(policy.IsReachable(ids[2]));
    }

    [Fact]
    public void Reordering_makes_a_covered_rule_reachable() {
        // Arrange
        FirewallPolicy policy = PolicyOf(RuleAction.Deny, strict: false);
        List<RuleId>   ids    = DistinctIds(2);
        policy.Append(Rule(ids[0], RuleAction.Allow, "10.0.0.0/8", "0.0.0.0/0"));
        policy.Append(Rule(ids[1], RuleAction.Deny, "10.1.0.0/16", "0.0.0.0/0"));

        // Act
        policy.Move(ids[1], 0);

        // Assert
        Assert.True(policy.IsReachable(ids[1]));
        Assert.True(policy.IsReachable(ids[0]));
    }

    [Fact]
    public void IsReachable_of_an_unknown_rule_is_refused() {
        // Arrange
        FirewallPolicy policy = PolicyOf(RuleAction.Deny, strict: false);
        RuleId         anyId  = Any.RuleId().Generate();

        // Act
        PolicyRefusedException thrown = Assert.Throws<PolicyRefusedException>(() => policy.IsReachable(anyId));

        // Assert
        Assert.Equal(PolicyRefusalReason.RuleNotFound, thrown.Reason);
        Assert.Throws<ArgumentNullException>(() => policy.IsReachable(null!));
    }

    // ---- Properties over many rules --------------------------------------------------------------------------

    private static readonly string[] Prefixes = ["0.0.0.0/0", "10.0.0.0/8", "10.1.0.0/16", "10.1.2.0/24", "10.1.2.3/32", "192.168.0.0/16", "172.16.0.0/12"];

    private static IAny<FirewallRule> UniverseRule(RuleId id) {
        // A pool cannot hold null, so the open protocol is the fourth of four numbered choices.
        IAny<byte?> protocols = Any.Int32().Between(0, 3).As(choice => choice switch {
            0 => (byte?)IpProtocols.Tcp,
            1 => IpProtocols.Udp,
            2 => IpProtocols.Icmp,
            _ => null
        });
        IAny<TcpFlagMatch?> flagTests = Any.Int32().Between(0, 3).As(choice => choice switch {
            0 => TcpFlagMatch.Established,
            1 => TcpFlagMatch.NewConnection,
            _ => null
        });

        return Any.Combine(Any.Enum<RuleAction>(),
                           Any.OneOf(Prefixes),
                           Any.OneOf(Prefixes),
                           protocols,
                           Any.OneOf(PortRange.All, new PortRange(1, 1024), PortRange.Single(80), PortRange.Single(443), new PortRange(80, 443)),
                           flagTests,
                           (action, source, destination, protocol, ports, flags) => {
                               bool portBearing = protocol is IpProtocols.Tcp or IpProtocols.Udp;

                               return new FirewallRule(id,
                                                       action,
                                                       P(source),
                                                       P(destination),
                                                       protocol,
                                                       PortRange.All,
                                                       portBearing ? ports : PortRange.All,
                                                       protocol == IpProtocols.Tcp ? flags : null);
                           });
    }

    /// <summary>Rules over a small universe of prefixes, so that coverage and overlap actually occur. Ids are made distinct.</summary>
    private static List<FirewallRule> AnyUniverseRules(int count) {
        return DistinctIds(count).Select(id => UniverseRule(id).Generate()).ToList();
    }

    private static FirewallPolicy PolicyWith(IEnumerable<FirewallRule> rules, RuleAction defaultAction, bool strict) {
        FirewallPolicy policy = PolicyOf(defaultAction, strict);

        foreach (FirewallRule rule in rules) {
            try { policy.Append(rule); } catch (PolicyRefusedException) { /* a strict policy declines what would be unreachable; that is what is under test */ }
        }

        return policy;
    }

    [Fact]
    public void A_strict_policy_built_from_any_rules_never_holds_an_unreachable_rule() {
        // Arrange
        List<FirewallRule> anyRules = AnyUniverseRules(15);
        RuleAction         anyDefault = Any.Enum<RuleAction>().Generate();

        // Act
        FirewallPolicy policy = PolicyWith(anyRules, anyDefault, strict: true);

        // Assert
        Assert.All(policy.Rules, rule => Assert.True(policy.IsReachable(rule.Id)));
        Assert.DoesNotContain(policy.FindAnomalies(), anomaly => anomaly.IsUnreachable);
    }

    [Fact]
    public void A_strict_policy_only_declines_a_rule_for_a_coverage_reason() {
        // Arrange
        List<FirewallRule> anyRules   = AnyUniverseRules(15);
        FirewallPolicy     policy     = PolicyOf(Any.Enum<RuleAction>().Generate(), strict: true);
        List<PolicyRefusalReason> reasons = [];

        // Act
        foreach (FirewallRule rule in anyRules) {
            try { policy.Append(rule); } catch (PolicyRefusedException refusal) { reasons.Add(refusal.Reason); }
        }

        // Assert
        Assert.All(reasons, reason => Assert.Equal(PolicyRefusalReason.RuleWouldBeUnreachable, reason));
        Assert.Equal(anyRules.Count - reasons.Count, policy.Count);
    }

    [Fact]
    public void IsReachable_agrees_with_the_unreachable_anomalies_of_a_non_strict_policy() {
        // Arrange
        FirewallPolicy policy = PolicyWith(AnyUniverseRules(15), Any.Enum<RuleAction>().Generate(), strict: false);

        // Act
        HashSet<RuleId> unreachable = policy.FindAnomalies().Where(anomaly => anomaly.IsUnreachable).Select(anomaly => anomaly.Rule).ToHashSet();

        // Assert
        Assert.All(policy.Rules, rule => Assert.Equal(!unreachable.Contains(rule.Id), policy.IsReachable(rule.Id)));
    }

    [Fact]
    public void An_anomaly_names_a_rule_of_the_policy_on_the_right_side() {
        // Arrange
        FirewallPolicy policy = PolicyWith(AnyUniverseRules(15), Any.Enum<RuleAction>().Generate(), strict: false);

        // Act
        IReadOnlyList<RuleAnomaly> anomalies = policy.FindAnomalies();

        // Assert
        Assert.All(anomalies, anomaly => {
            int index = policy.IndexOf(anomaly.Rule);

            Assert.True(index >= 0);

            if (anomaly.RelatedRule is null) { return; }

            int related = policy.IndexOf(anomaly.RelatedRule);

            Assert.True(related >= 0);
            Assert.Equal(anomaly.IsUnreachable, related < index);
        });
        Assert.Equal(anomalies.Select(anomaly => policy.IndexOf(anomaly.Rule)).OrderBy(position => position).ToList(),
                     anomalies.Select(anomaly => policy.IndexOf(anomaly.Rule)).ToList());
    }

    /// <summary>A packet that the rule matches, at the low or the high end of what the rule accepts.</summary>
    private static Ipv4Packet? WitnessOf(FirewallRule rule, bool high) {
        AddressPrefix source      = rule.Source;
        AddressPrefix destination = rule.Destination;

        uint SourceBits(AddressPrefix prefix) => high ? prefix.Ipv4Network!.Value | (uint)(0xFFFFFFFFUL >> prefix.PrefixLength) : prefix.Ipv4Network!.Value;

        uint sourceBits = Math.Min(SourceBits(source), 0xDFFFFFFFu);

        if (!source.Contains(new Ipv4Address(sourceBits))) { return null; }

        byte protocol = rule.Protocol ?? IpProtocols.Udp;

        ushort sourcePort      = IpProtocols.CarriesPorts(protocol) ? (high ? rule.SourcePorts.High : rule.SourcePorts.Low) : (ushort)0;
        ushort destinationPort = IpProtocols.CarriesPorts(protocol) ? (high ? rule.DestinationPorts.High : rule.DestinationPorts.Low) : (ushort)0;

        TcpFlags flags = TcpFlags.None;

        if (protocol == IpProtocols.Tcp) {
            flags = rule.TcpFlags?.Expected ?? TcpFlags.Ack;

            if (flags == TcpFlags.None) { flags = TcpFlags.Ack; }
        }

        return Any.Ipv4Packet()
                  .WithSource(new Ipv4Address(sourceBits))
                  .WithDestination(new Ipv4Address(SourceBits(destination)))
                  .WithProtocol(protocol)
                  .WithPorts(sourcePort, destinationPort)
                  .WithTcpFlags(flags)
                  .Generate();
    }

    [Fact]
    public void Removing_a_rule_the_analysis_calls_useless_changes_no_verdict() {
        // Arrange
        RuleAction         anyDefault = Any.Enum<RuleAction>().Generate();
        List<FirewallRule> anyRules   = AnyUniverseRules(12);
        FirewallPolicy     policy     = PolicyWith(anyRules, anyDefault, strict: false);

        List<Ipv4Packet> witnesses = policy.Rules.SelectMany(rule => new[] { WitnessOf(rule, false), WitnessOf(rule, true) })
                                                 .OfType<Ipv4Packet>()
                                                 .ToList();

        // Act & Assert
        foreach (RuleAnomaly anomaly in policy.FindAnomalies()) {
            FirewallPolicy without = PolicyWith(policy.Rules.Where(rule => !rule.Id.Equals(anomaly.Rule)), anyDefault, strict: false);

            foreach (Ipv4Packet witness in witnesses) {
                Assert.True(policy.Evaluate(witness).Action == without.Evaluate(witness).Action,
                            $"Removing {anomaly} changed the verdict of {witness}.");
            }
        }
    }

}
