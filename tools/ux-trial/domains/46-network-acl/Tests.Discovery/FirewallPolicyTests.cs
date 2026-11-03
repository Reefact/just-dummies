using NetworkAcl.Domain;

namespace NetworkAcl.Tests.Discovery;

public sealed class FirewallPolicyTests {

    private static FirewallPolicy Lax(RuleAction defaultAction = RuleAction.Deny, int capacity = 100) {
        return new FirewallPolicy("lax", defaultAction, capacity, strict: false);
    }

    private static FirewallPolicy Strict(RuleAction defaultAction = RuleAction.Deny, int capacity = 100) {
        return new FirewallPolicy("strict", defaultAction, capacity, strict: true);
    }

    private static string[] Ids(FirewallPolicy policy) {
        return policy.Rules.Select(r => r.Id.Value).ToArray();
    }

    // ---- construction

    [Fact]
    public void A_new_policy_is_empty_and_untouched() {
        // Arrange
        FirewallPolicy policy = new FirewallPolicy("edge", RuleAction.Deny, 50, strict: true);

        // Assert
        Assert.Equal("edge", policy.Name);
        Assert.Equal(RuleAction.Deny, policy.DefaultAction);
        Assert.Equal(50, policy.Capacity);
        Assert.True(policy.IsStrict);
        Assert.Equal(0, policy.Count);
        Assert.Empty(policy.Rules);
        Assert.Equal(0, policy.Revision);
        Assert.Equal(0, policy.DefaultHits);
    }

    [Fact]
    public void A_scaffolded_policy_starts_empty_whatever_its_name_default_capacity_and_strictness() {
        // Arrange
        FirewallPolicy anyPolicy = Any.FirewallPolicy().Generate();

        // Assert
        Assert.Equal(0, anyPolicy.Count);
        Assert.Equal(0, anyPolicy.Revision);
        Assert.Equal(0, anyPolicy.DefaultHits);
    }

    [Fact]
    public void A_null_name_is_refused() {
        Assert.Throws<ArgumentNullException>(() => new FirewallPolicy(null!, RuleAction.Deny, 10, false));
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("\t\n")]
    public void A_blank_name_is_refused(string name) {
        Assert.Throws<ArgumentException>(() => new FirewallPolicy(name, RuleAction.Deny, 10, false));
    }

    [Fact]
    public void A_name_of_64_characters_is_accepted_and_65_is_not() {
        Assert.Equal(64, new FirewallPolicy(new string('n', 64), RuleAction.Deny, 10, false).Name.Length);
        Assert.Throws<ArgumentException>(() => new FirewallPolicy(new string('n', 65), RuleAction.Deny, 10, false));
    }

    [Fact]
    public void An_undefined_default_action_is_refused() {
        string anyName = Any.String().NotBlank().WithMaxLength(FirewallPolicy.MaxNameLength).Generate();

        Assert.Throws<ArgumentOutOfRangeException>(() => new FirewallPolicy(anyName, (RuleAction)2, 10, false));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(10001)]
    [InlineData(int.MaxValue)]
    [InlineData(int.MinValue)]
    public void A_capacity_outside_1_to_10000_is_refused(int capacity) {
        string anyName = Any.String().NotBlank().WithMaxLength(FirewallPolicy.MaxNameLength).Generate();

        Assert.Throws<ArgumentOutOfRangeException>(() => new FirewallPolicy(anyName, RuleAction.Deny, capacity, false));
    }

    [Theory]
    [InlineData(1)]
    [InlineData(10000)]
    public void The_capacity_limits_themselves_are_accepted(int capacity) {
        Assert.Equal(capacity, new FirewallPolicy("p", RuleAction.Deny, capacity, false).Capacity);
    }

    // ---- insert and append

    [Fact]
    public void Append_puts_the_rule_last() {
        FirewallPolicy policy = Lax();

        policy.Append(Build.Rule("first", RuleAction.Allow));
        policy.Append(Build.Rule("second", RuleAction.Allow));
        policy.Append(Build.Rule("third", RuleAction.Allow));

        Assert.Equal(new[] { "first", "second", "third" }, Ids(policy));
    }

    [Fact]
    public void Insert_at_zero_puts_the_rule_first_and_at_count_last() {
        FirewallPolicy policy = Lax();
        policy.Append(Build.Rule("b", RuleAction.Allow));

        policy.Insert(0, Build.Rule("a", RuleAction.Allow));
        policy.Insert(policy.Count, Build.Rule("d", RuleAction.Allow));
        policy.Insert(2, Build.Rule("c", RuleAction.Allow));

        Assert.Equal(new[] { "a", "b", "c", "d" }, Ids(policy));
    }

    [Fact]
    public void Each_accepted_insert_adds_one_revision_and_a_zero_counter() {
        FirewallPolicy policy = Lax();
        FirewallRule   anyRule = Any.FirewallRule().WithId(new RuleId("only")).Generate();

        policy.Append(anyRule);

        Assert.Equal(1, policy.Revision);
        Assert.Equal(0, policy.HitsOf(new RuleId("only")));
    }

    [Fact]
    public void A_duplicate_id_is_refused_and_changes_nothing() {
        FirewallPolicy policy = Lax();
        policy.Append(Build.Rule("dns", RuleAction.Allow));

        Build.Refused(PolicyRefusalReason.DuplicateRuleId, () => policy.Append(Build.Rule("dns", RuleAction.Deny)));

        Assert.Equal(new[] { "dns" }, Ids(policy));
        Assert.Equal(1, policy.Revision);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(2)]
    [InlineData(int.MaxValue)]
    public void An_insert_position_outside_0_to_count_is_refused(int position) {
        FirewallPolicy policy = Lax();
        policy.Append(Build.Rule("a", RuleAction.Allow));

        Build.Refused(PolicyRefusalReason.PositionOutOfRange, () => policy.Insert(position, Build.Rule("b", RuleAction.Allow)));

        Assert.Equal(1, policy.Count);
    }

    [Fact]
    public void A_duplicate_id_is_reported_before_a_bad_position() {
        FirewallPolicy policy = Lax();
        policy.Append(Build.Rule("a", RuleAction.Allow));

        Build.Refused(PolicyRefusalReason.DuplicateRuleId, () => policy.Insert(99, Build.Rule("a", RuleAction.Allow)));
    }

    [Fact]
    public void A_full_policy_refuses_one_more_rule() {
        // Arrange
        FirewallPolicy policy = Lax(capacity: 2);
        policy.Append(Build.Rule("a", RuleAction.Allow));
        policy.Append(Build.Rule("b", RuleAction.Allow));
        FirewallRule anyThird = Any.FirewallRule().WithId(new RuleId("c")).Generate();

        // Assert
        Build.Refused(PolicyRefusalReason.CapacityReached, () => policy.Append(anyThird));
        Assert.Equal(2, policy.Count);
    }

    [Fact]
    public void A_bad_position_is_reported_before_a_full_policy() {
        FirewallPolicy policy = Lax(capacity: 1);
        policy.Append(Build.Rule("a", RuleAction.Allow));

        Build.Refused(PolicyRefusalReason.PositionOutOfRange, () => policy.Insert(5, Build.Rule("b", RuleAction.Allow)));
    }

    [Fact]
    public void A_full_policy_reports_its_capacity_before_any_strict_check() {
        FirewallPolicy policy = Strict(capacity: 1);
        policy.Append(Build.Rule("wide", RuleAction.Allow, "10.0.0.0/8"));

        Build.Refused(PolicyRefusalReason.CapacityReached, () => policy.Append(Build.Rule("narrow", RuleAction.Allow, "10.1.0.0/16")));
    }

    [Fact]
    public void A_policy_can_be_filled_to_its_capacity() {
        // Arrange: the ids only need to differ
        List<RuleId> anyIds = Any.ListOf(Any.RuleId()).WithCount(5).Distinct().Generate();
        FirewallPolicy policy = Lax(capacity: 5);

        // Act
        foreach (RuleId id in anyIds) { policy.Append(Any.FirewallRule().WithId(id).Generate()); }

        // Assert
        Assert.Equal(5, policy.Count);
        Assert.Equal(5, policy.Revision);
    }

    [Fact]
    public void A_null_rule_is_refused() {
        Assert.Throws<ArgumentNullException>(() => Lax().Append(null!));
        Assert.Throws<ArgumentNullException>(() => Lax().Insert(0, null!));
    }

    // ---- lookup

    [Fact]
    public void IndexOf_and_Find_locate_a_rule_by_id() {
        FirewallPolicy policy = Lax();
        FirewallRule   second = Build.Rule("second", RuleAction.Deny);
        policy.Append(Build.Rule("first", RuleAction.Allow));
        policy.Append(second);

        Assert.Equal(1, policy.IndexOf(new RuleId("second")));
        Assert.Same(second, policy.Find(new RuleId("second")));
    }

    [Fact]
    public void IndexOf_is_minus_one_and_Find_is_null_for_an_absent_rule() {
        FirewallPolicy policy = Lax();
        RuleId         anyAbsent = Any.RuleId().Generate();

        Assert.Equal(-1, policy.IndexOf(anyAbsent));
        Assert.Null(policy.Find(anyAbsent));
    }

    [Fact]
    public void Lookups_refuse_a_null_id() {
        FirewallPolicy policy = Lax();

        Assert.Throws<ArgumentNullException>(() => policy.IndexOf(null!));
        Assert.Throws<ArgumentNullException>(() => policy.HitsOf(null!));
        Assert.Throws<ArgumentNullException>(() => policy.Remove(null!));
        Assert.Throws<ArgumentNullException>(() => policy.IsReachable(null!));
    }

    [Fact]
    public void HitsOf_an_absent_rule_is_refused() {
        FirewallPolicy policy = Lax();
        RuleId         anyAbsent = Any.RuleId().Generate();

        Build.Refused(PolicyRefusalReason.RuleNotFound, () => policy.HitsOf(anyAbsent));
    }

    // ---- strict mode on insert

    [Fact]
    public void A_strict_policy_refuses_a_rule_an_earlier_one_covers() {
        FirewallPolicy policy = Strict();
        policy.Append(Build.Rule("wide", RuleAction.Allow, "10.0.0.0/8"));

        Build.Refused(PolicyRefusalReason.RuleWouldBeUnreachable, () => policy.Append(Build.Rule("narrow", RuleAction.Deny, "10.1.0.0/16")));

        Assert.Equal(new[] { "wide" }, Ids(policy));
        Assert.Equal(1, policy.Revision);
    }

    [Fact]
    public void A_strict_policy_refuses_a_rule_that_would_cover_a_later_one() {
        FirewallPolicy policy = Strict();
        policy.Append(Build.Rule("narrow", RuleAction.Deny, "10.1.0.0/16"));

        Build.Refused(PolicyRefusalReason.RuleWouldShadowAnother, () => policy.Insert(0, Build.Rule("wide", RuleAction.Allow, "10.0.0.0/8")));

        Assert.Equal(new[] { "narrow" }, Ids(policy));
    }

    [Fact]
    public void A_strict_policy_accepts_the_specific_rule_before_the_general_one() {
        FirewallPolicy policy = Strict();

        policy.Append(Build.Rule("narrow", RuleAction.Deny, "10.1.0.0/16"));
        policy.Append(Build.Rule("wide", RuleAction.Allow, "10.0.0.0/8"));

        Assert.Equal(new[] { "narrow", "wide" }, Ids(policy));
    }

    [Fact]
    public void A_strict_policy_accepts_disjoint_rules_in_any_order() {
        FirewallPolicy policy = Strict();

        policy.Append(Build.Rule("ten", RuleAction.Allow, "10.0.0.0/8"));
        policy.Insert(0, Build.Rule("eleven", RuleAction.Allow, "11.0.0.0/8"));

        Assert.Equal(new[] { "eleven", "ten" }, Ids(policy));
    }

    [Fact]
    public void A_lax_policy_accepts_unreachable_and_shadowing_rules() {
        FirewallPolicy policy = Lax();

        policy.Append(Build.Rule("wide", RuleAction.Allow, "10.0.0.0/8"));
        policy.Append(Build.Rule("narrow", RuleAction.Deny, "10.1.0.0/16"));
        policy.Insert(0, Build.Rule("wider", RuleAction.Deny));

        Assert.Equal(3, policy.Count);
    }

    [Fact]
    public void A_strict_policy_keeps_rules_of_the_other_family_apart() {
        FirewallPolicy policy = Strict();

        policy.Append(Build.Rule("all-v4", RuleAction.Allow));
        policy.Append(Build.Rule("all-v6", RuleAction.Allow, "::/0", "::/0"));

        Assert.Equal(2, policy.Count);
    }

    [Fact]
    public void A_strict_policy_tells_rules_apart_by_port_and_protocol() {
        FirewallPolicy policy = Strict();

        policy.Append(Build.Rule("tcp-80", RuleAction.Allow, protocol: IpProtocols.Tcp, destinationPorts: "80"));
        policy.Append(Build.Rule("tcp-443", RuleAction.Allow, protocol: IpProtocols.Tcp, destinationPorts: "443"));
        policy.Append(Build.Rule("udp-80", RuleAction.Allow, protocol: IpProtocols.Udp, destinationPorts: "80"));
        policy.Append(Build.Rule("any-proto", RuleAction.Allow));

        Build.Refused(PolicyRefusalReason.RuleWouldBeUnreachable, () => policy.Append(Build.Rule("tcp-80-again", RuleAction.Deny, protocol: IpProtocols.Tcp, destinationPorts: "80")));
    }

    // ---- move

    [Fact]
    public void Move_puts_a_rule_at_its_new_place() {
        FirewallPolicy policy = Lax();
        foreach (string id in new[] { "a", "b", "c", "d" }) { policy.Append(Build.Rule(id, RuleAction.Allow)); }

        policy.Move(new RuleId("a"), 2);
        Assert.Equal(new[] { "b", "c", "a", "d" }, Ids(policy));

        policy.Move(new RuleId("d"), 0);
        Assert.Equal(new[] { "d", "b", "c", "a" }, Ids(policy));

        policy.Move(new RuleId("d"), 3);
        Assert.Equal(new[] { "b", "c", "a", "d" }, Ids(policy));
    }

    [Fact]
    public void An_effective_move_adds_exactly_one_revision() {
        FirewallPolicy policy = Lax();
        policy.Append(Build.Rule("a", RuleAction.Allow));
        policy.Append(Build.Rule("b", RuleAction.Allow));

        policy.Move(new RuleId("a"), 1);

        Assert.Equal(3, policy.Revision);
    }

    [Fact]
    public void Moving_a_rule_to_where_it_already_is_does_nothing() {
        FirewallPolicy policy = Lax();
        policy.Append(Build.Rule("a", RuleAction.Allow));
        policy.Append(Build.Rule("b", RuleAction.Allow));

        policy.Move(new RuleId("b"), 1);

        Assert.Equal(2, policy.Revision);
        Assert.Equal(new[] { "a", "b" }, Ids(policy));
    }

    [Fact]
    public void Moving_an_unknown_rule_is_refused() {
        FirewallPolicy policy = Lax();
        RuleId         anyAbsent = Any.RuleId().Generate();

        Build.Refused(PolicyRefusalReason.RuleNotFound, () => policy.Move(anyAbsent, 0));
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(2)]
    [InlineData(100)]
    public void A_move_position_outside_0_to_count_minus_one_is_refused(int position) {
        FirewallPolicy policy = Lax();
        policy.Append(Build.Rule("a", RuleAction.Allow));
        policy.Append(Build.Rule("b", RuleAction.Allow));

        Build.Refused(PolicyRefusalReason.PositionOutOfRange, () => policy.Move(new RuleId("a"), position));

        Assert.Equal(new[] { "a", "b" }, Ids(policy));
    }

    [Fact]
    public void An_unknown_rule_is_reported_before_a_bad_position() {
        FirewallPolicy policy = Lax();
        RuleId         anyAbsent = Any.RuleId().Generate();

        Build.Refused(PolicyRefusalReason.RuleNotFound, () => policy.Move(anyAbsent, 99));
    }

    [Fact]
    public void A_strict_policy_refuses_to_move_a_rule_behind_one_that_covers_it() {
        FirewallPolicy policy = Strict();
        policy.Append(Build.Rule("narrow", RuleAction.Deny, "10.1.0.0/16"));
        policy.Append(Build.Rule("wide", RuleAction.Allow, "10.0.0.0/8"));

        Build.Refused(PolicyRefusalReason.RuleWouldBeUnreachable, () => policy.Move(new RuleId("narrow"), 1));

        Assert.Equal(new[] { "narrow", "wide" }, Ids(policy));
        Assert.Equal(2, policy.Revision);
    }

    [Fact]
    public void A_strict_policy_refuses_to_move_a_rule_before_one_it_would_cover() {
        FirewallPolicy policy = Strict();
        policy.Append(Build.Rule("narrow", RuleAction.Deny, "10.1.0.0/16"));
        policy.Append(Build.Rule("wide", RuleAction.Allow, "10.0.0.0/8"));

        Build.Refused(PolicyRefusalReason.RuleWouldShadowAnother, () => policy.Move(new RuleId("wide"), 0));

        Assert.Equal(new[] { "narrow", "wide" }, Ids(policy));
    }

    [Fact]
    public void A_strict_policy_lets_disjoint_rules_swap() {
        FirewallPolicy policy = Strict();
        policy.Append(Build.Rule("ten", RuleAction.Allow, "10.0.0.0/8"));
        policy.Append(Build.Rule("eleven", RuleAction.Allow, "11.0.0.0/8"));

        policy.Move(new RuleId("eleven"), 0);

        Assert.Equal(new[] { "eleven", "ten" }, Ids(policy));
    }

    [Fact]
    public void A_lax_policy_lets_a_rule_move_anywhere() {
        FirewallPolicy policy = Lax();
        policy.Append(Build.Rule("narrow", RuleAction.Deny, "10.1.0.0/16"));
        policy.Append(Build.Rule("wide", RuleAction.Allow, "10.0.0.0/8"));

        policy.Move(new RuleId("wide"), 0);

        Assert.Equal(new[] { "wide", "narrow" }, Ids(policy));
    }

    // ---- remove

    [Fact]
    public void Remove_returns_the_rule_and_forgets_it() {
        FirewallPolicy policy = Lax();
        FirewallRule   second = Build.Rule("b", RuleAction.Deny);
        policy.Append(Build.Rule("a", RuleAction.Allow));
        policy.Append(second);
        policy.Append(Build.Rule("c", RuleAction.Allow));

        FirewallRule removed = policy.Remove(new RuleId("b"));

        Assert.Same(second, removed);
        Assert.Equal(new[] { "a", "c" }, Ids(policy));
        Assert.Equal(4, policy.Revision);
        Assert.Equal(-1, policy.IndexOf(new RuleId("b")));
        Build.Refused(PolicyRefusalReason.RuleNotFound, () => policy.HitsOf(new RuleId("b")));
    }

    [Fact]
    public void Removing_an_unknown_rule_is_refused_and_changes_nothing() {
        FirewallPolicy policy = Lax();
        policy.Append(Build.Rule("a", RuleAction.Allow));
        RuleId anyAbsent = Any.RuleId().DifferentFrom(new RuleId("a")).Generate();

        Build.Refused(PolicyRefusalReason.RuleNotFound, () => policy.Remove(anyAbsent));

        Assert.Equal(1, policy.Revision);
    }

    [Fact]
    public void A_strict_policy_never_refuses_a_removal() {
        FirewallPolicy policy = Strict();
        policy.Append(Build.Rule("narrow", RuleAction.Deny, "10.1.0.0/16"));
        policy.Append(Build.Rule("wide", RuleAction.Allow, "10.0.0.0/8"));

        policy.Remove(new RuleId("narrow"));
        policy.Remove(new RuleId("wide"));

        Assert.Equal(0, policy.Count);
    }

    [Fact]
    public void A_removed_rule_can_be_inserted_again_with_a_fresh_counter() {
        FirewallPolicy policy = Lax();
        policy.Append(Build.Rule("web", RuleAction.Allow, protocol: IpProtocols.Tcp, destinationPorts: "443"));
        policy.Evaluate(Build.Packet4("10.0.0.1", "192.0.2.1"));
        Assert.Equal(1, policy.HitsOf(new RuleId("web")));

        FirewallRule removed = policy.Remove(new RuleId("web"));
        policy.Append(removed);

        Assert.Equal(0, policy.HitsOf(new RuleId("web")));
    }

    // ---- evaluation

    [Fact]
    public void With_no_rule_the_default_action_decides_and_is_counted() {
        FirewallPolicy policy = Lax(RuleAction.Deny);
        Ipv4Packet     anyPacket = Any.Ipv4Packet().Generate();

        Verdict verdict = policy.Evaluate(anyPacket);

        Assert.Equal(RuleAction.Deny, verdict.Action);
        Assert.True(verdict.IsDefault);
        Assert.False(verdict.IsAllowed);
        Assert.Equal(1, policy.DefaultHits);
    }

    [Fact]
    public void An_allow_by_default_policy_lets_an_unmatched_packet_through() {
        FirewallPolicy policy = Lax(RuleAction.Allow);
        Ipv6Packet     anyPacket = Any.Ipv6Packet().Generate();

        Verdict verdict = policy.Evaluate(anyPacket);

        Assert.True(verdict.IsAllowed);
        Assert.True(verdict.IsDefault);
    }

    [Fact]
    public void The_first_matching_rule_decides() {
        FirewallPolicy policy = Lax(RuleAction.Allow);
        policy.Append(Build.Rule("block-10", RuleAction.Deny, "10.0.0.0/8"));
        policy.Append(Build.Rule("allow-all", RuleAction.Allow));

        Verdict verdict = policy.Evaluate(Build.Packet4("10.1.2.3", "192.0.2.1"));

        Assert.Equal(new Verdict(RuleAction.Deny, new RuleId("block-10")), verdict);
        Assert.Equal(1, policy.HitsOf(new RuleId("block-10")));
        Assert.Equal(0, policy.HitsOf(new RuleId("allow-all")));
    }

    [Fact]
    public void Swapping_two_overlapping_rules_swaps_the_outcome() {
        FirewallPolicy policy = Lax(RuleAction.Deny);
        policy.Append(Build.Rule("allow-all", RuleAction.Allow));
        policy.Append(Build.Rule("block-10", RuleAction.Deny, "10.0.0.0/8"));
        Ipv4Packet fromTen = Build.Packet4("10.1.2.3", "192.0.2.1");

        Assert.Equal("allow-all", policy.Evaluate(fromTen).MatchedRule!.Value);

        policy.Move(new RuleId("block-10"), 0);

        Assert.Equal("block-10", policy.Evaluate(fromTen).MatchedRule!.Value);
    }

    [Fact]
    public void A_packet_no_rule_matches_falls_through_to_the_default_with_no_rule_named() {
        FirewallPolicy policy = Lax(RuleAction.Deny);
        policy.Append(Build.Rule("only-dns", RuleAction.Allow, protocol: IpProtocols.Udp, destinationPorts: "53"));

        Verdict verdict = policy.Evaluate(Build.Packet4("10.0.0.1", "192.0.2.1", IpProtocols.Tcp, 40000, 443));

        Assert.True(verdict.IsDefault);
        Assert.Null(verdict.MatchedRule);
        Assert.Equal(1, policy.DefaultHits);
        Assert.Equal(0, policy.HitsOf(new RuleId("only-dns")));
    }

    [Fact]
    public void Hits_accumulate_per_rule_and_for_the_default() {
        FirewallPolicy policy = Lax(RuleAction.Deny);
        policy.Append(Build.Rule("dns", RuleAction.Allow, protocol: IpProtocols.Udp, destinationPorts: "53"));
        policy.Append(Build.Rule("web", RuleAction.Allow, protocol: IpProtocols.Tcp, destinationPorts: "443"));

        for (int i = 0; i < 3; i++) { policy.Evaluate(Build.Packet4("10.0.0.1", "192.0.2.1", IpProtocols.Udp, 5000, 53)); }
        for (int i = 0; i < 2; i++) { policy.Evaluate(Build.Packet4("10.0.0.1", "192.0.2.1", IpProtocols.Tcp, 5000, 443)); }
        policy.Evaluate(Build.Packet4("10.0.0.1", "192.0.2.1", IpProtocols.Tcp, 5000, 22));

        Assert.Equal(3, policy.HitsOf(new RuleId("dns")));
        Assert.Equal(2, policy.HitsOf(new RuleId("web")));
        Assert.Equal(1, policy.DefaultHits);
    }

    [Fact]
    public void Evaluating_does_not_change_the_revision() {
        FirewallPolicy policy = Lax();
        policy.Append(Build.Rule("a", RuleAction.Allow));
        Ipv4Packet anyPacket = Any.Ipv4Packet().Generate();

        policy.Evaluate(anyPacket);

        Assert.Equal(1, policy.Revision);
    }

    [Fact]
    public void A_v4_rule_does_not_decide_an_ipv6_packet() {
        FirewallPolicy policy = Lax(RuleAction.Deny);
        policy.Append(Build.Rule("all-v4", RuleAction.Allow));
        Ipv6Packet anyPacket = Any.Ipv6Packet().Generate();

        Verdict verdict = policy.Evaluate(anyPacket);

        Assert.True(verdict.IsDefault);
    }

    [Fact]
    public void Evaluate_refuses_a_null_packet() {
        Assert.Throws<ArgumentNullException>(() => Lax().Evaluate(null!));
    }

    [Fact]
    public void Each_evaluated_packet_is_counted_exactly_once_whatever_decides_it() {
        // Arrange: the rules and packets are arbitrary, only their number matters here
        List<RuleId> anyIds = Any.ListOf(Any.RuleId()).WithCount(6).Distinct().Generate();
        FirewallPolicy policy = Any.FirewallPolicy().WithStrict(false).WithCapacity(10).Generate();
        foreach (RuleId id in anyIds) { policy.Append(Any.FirewallRule().WithId(id).Generate()); }

        // Act
        for (int i = 0; i < 20; i++) {
            policy.Evaluate(Any.Ipv4Packet().Generate());
            policy.Evaluate(Any.Ipv6Packet().Generate());
        }

        // Assert
        long counted = policy.DefaultHits + anyIds.Sum(id => policy.HitsOf(id));
        Assert.Equal(40, counted);
    }

    [Fact]
    public void ResetCounters_zeroes_every_counter_and_leaves_the_rules_and_revision() {
        FirewallPolicy policy = Lax();
        policy.Append(Build.Rule("all", RuleAction.Allow, "10.0.0.0/8"));
        policy.Evaluate(Build.Packet4("10.0.0.1", "192.0.2.1"));
        policy.Evaluate(Build.Packet4("11.0.0.1", "192.0.2.1"));

        policy.ResetCounters();

        Assert.Equal(0, policy.HitsOf(new RuleId("all")));
        Assert.Equal(0, policy.DefaultHits);
        Assert.Equal(1, policy.Count);
        Assert.Equal(1, policy.Revision);
    }

    // ---- anomalies

    [Fact]
    public void A_policy_with_no_rule_has_no_anomaly() {
        Assert.Empty(Lax().FindAnomalies());
    }

    [Fact]
    public void A_rule_covered_by_an_earlier_one_with_the_opposite_action_is_shadowed() {
        FirewallPolicy policy = Lax(RuleAction.Deny);
        policy.Append(Build.Rule("allow-ten", RuleAction.Allow, "10.0.0.0/8"));
        policy.Append(Build.Rule("deny-ten-one", RuleAction.Deny, "10.1.0.0/16"));

        IReadOnlyList<RuleAnomaly> anomalies = policy.FindAnomalies();

        RuleAnomaly anomaly = Assert.Single(anomalies);
        Assert.Equal(new RuleAnomaly(new RuleId("deny-ten-one"), AnomalyKind.Shadowed, new RuleId("allow-ten")), anomaly);
        Assert.True(anomaly.IsUnreachable);
    }

    [Fact]
    public void A_rule_covered_by_an_earlier_one_with_the_same_action_is_redundant_to_it() {
        FirewallPolicy policy = Lax(RuleAction.Deny);
        policy.Append(Build.Rule("allow-ten", RuleAction.Allow, "10.0.0.0/8"));
        policy.Append(Build.Rule("allow-ten-one", RuleAction.Allow, "10.1.0.0/16"));

        RuleAnomaly anomaly = Assert.Single(policy.FindAnomalies());

        Assert.Equal(new RuleAnomaly(new RuleId("allow-ten-one"), AnomalyKind.RedundantToEarlier, new RuleId("allow-ten")), anomaly);
    }

    [Fact]
    public void The_first_covering_earlier_rule_is_the_one_named() {
        FirewallPolicy policy = Lax(RuleAction.Deny);
        policy.Append(Build.Rule("first", RuleAction.Allow, "10.0.0.0/8"));
        policy.Append(Build.Rule("second", RuleAction.Allow, "10.0.0.0/8"));
        policy.Append(Build.Rule("third", RuleAction.Allow, "10.1.0.0/16"));

        IReadOnlyList<RuleAnomaly> anomalies = policy.FindAnomalies();

        Assert.Contains(new RuleAnomaly(new RuleId("third"), AnomalyKind.RedundantToEarlier, new RuleId("first")), anomalies);
        Assert.Contains(new RuleAnomaly(new RuleId("second"), AnomalyKind.RedundantToEarlier, new RuleId("first")), anomalies);
    }

    [Fact]
    public void A_rule_covered_by_a_later_one_of_the_same_action_is_redundant_to_it() {
        FirewallPolicy policy = Lax(RuleAction.Deny);
        policy.Append(Build.Rule("allow-ten-one", RuleAction.Allow, "10.1.0.0/16"));
        policy.Append(Build.Rule("allow-ten", RuleAction.Allow, "10.0.0.0/8"));

        RuleAnomaly anomaly = Assert.Single(policy.FindAnomalies());

        Assert.Equal(new RuleAnomaly(new RuleId("allow-ten-one"), AnomalyKind.RedundantToLater, new RuleId("allow-ten")), anomaly);
        Assert.False(anomaly.IsUnreachable);
    }

    [Fact]
    public void An_overlapping_rule_of_the_other_action_in_between_protects_the_rule_from_redundancy_to_a_later_one() {
        FirewallPolicy policy = Lax(RuleAction.Allow);
        policy.Append(Build.Rule("allow-ten-one", RuleAction.Allow, "10.1.0.0/16"));
        policy.Append(Build.Rule("deny-ten-one-one", RuleAction.Deny, "10.1.1.0/24"));
        policy.Append(Build.Rule("allow-ten", RuleAction.Allow, "10.0.0.0/8"));

        IReadOnlyList<RuleAnomaly> anomalies = policy.FindAnomalies();

        Assert.DoesNotContain(anomalies, a => a.Rule.Value == "allow-ten-one");
    }

    [Fact]
    public void A_rule_of_the_other_action_that_does_not_overlap_in_between_changes_nothing() {
        FirewallPolicy policy = Lax(RuleAction.Allow);
        policy.Append(Build.Rule("allow-ten-one", RuleAction.Allow, "10.1.0.0/16"));
        policy.Append(Build.Rule("deny-elsewhere", RuleAction.Deny, "172.16.0.0/12"));
        policy.Append(Build.Rule("allow-ten", RuleAction.Allow, "10.0.0.0/8"));

        Assert.Contains(new RuleAnomaly(new RuleId("allow-ten-one"), AnomalyKind.RedundantToLater, new RuleId("allow-ten")), policy.FindAnomalies());
    }

    [Fact]
    public void A_rule_that_does_what_the_default_does_is_redundant_to_it() {
        FirewallPolicy policy = Lax(RuleAction.Deny);
        policy.Append(Build.Rule("deny-ten", RuleAction.Deny, "10.0.0.0/8"));

        RuleAnomaly anomaly = Assert.Single(policy.FindAnomalies());

        Assert.Equal(new RuleAnomaly(new RuleId("deny-ten"), AnomalyKind.RedundantToDefault, null), anomaly);
    }

    [Fact]
    public void A_later_overlapping_rule_of_the_other_action_keeps_a_default_action_rule_useful() {
        FirewallPolicy policy = Lax(RuleAction.Deny);
        policy.Append(Build.Rule("deny-ten-one", RuleAction.Deny, "10.1.0.0/16"));
        policy.Append(Build.Rule("allow-ten", RuleAction.Allow, "10.0.0.0/8"));

        Assert.Empty(policy.FindAnomalies());
    }

    [Fact]
    public void A_later_non_overlapping_rule_of_the_other_action_does_not_keep_it_useful() {
        FirewallPolicy policy = Lax(RuleAction.Deny);
        policy.Append(Build.Rule("deny-ten", RuleAction.Deny, "10.0.0.0/8"));
        policy.Append(Build.Rule("allow-eleven", RuleAction.Allow, "11.0.0.0/8"));

        RuleAnomaly anomaly = Assert.Single(policy.FindAnomalies());

        Assert.Equal(AnomalyKind.RedundantToDefault, anomaly.Kind);
        Assert.Equal("deny-ten", anomaly.Rule.Value);
    }

    [Fact]
    public void A_rule_that_differs_from_the_default_and_is_not_covered_has_no_anomaly() {
        FirewallPolicy policy = Lax(RuleAction.Deny);
        policy.Append(Build.Rule("allow-web", RuleAction.Allow, protocol: IpProtocols.Tcp, destinationPorts: "443"));

        Assert.Empty(policy.FindAnomalies());
    }

    [Fact]
    public void At_most_one_anomaly_is_reported_per_rule_the_earlier_cover_first() {
        // "late" is covered by an earlier rule AND by a later rule: only the earlier cover is reported
        FirewallPolicy policy = Lax(RuleAction.Deny);
        policy.Append(Build.Rule("early", RuleAction.Allow, "10.0.0.0/8"));
        policy.Append(Build.Rule("late", RuleAction.Allow, "10.1.0.0/16"));
        policy.Append(Build.Rule("later", RuleAction.Allow, "10.0.0.0/8"));

        IReadOnlyList<RuleAnomaly> anomalies = policy.FindAnomalies();

        Assert.Equal(1, anomalies.Count(a => a.Rule.Value == "late"));
        Assert.Contains(new RuleAnomaly(new RuleId("late"), AnomalyKind.RedundantToEarlier, new RuleId("early")), anomalies);
    }

    [Fact]
    public void Anomalies_come_in_rule_order() {
        FirewallPolicy policy = Lax(RuleAction.Deny);
        policy.Append(Build.Rule("a", RuleAction.Allow, "10.0.0.0/8"));
        policy.Append(Build.Rule("b", RuleAction.Deny, "10.1.0.0/16"));
        policy.Append(Build.Rule("c", RuleAction.Allow, "10.2.0.0/16"));
        policy.Append(Build.Rule("d", RuleAction.Allow, "192.0.2.0/24"));

        string[] ruleOrder = policy.FindAnomalies().Select(a => a.Rule.Value).ToArray();

        Assert.Equal(new[] { "b", "c" }, ruleOrder);
    }

    [Fact]
    public void A_rule_hidden_only_by_several_earlier_rules_together_is_not_reported() {
        // 10.0.0.0/8 is wholly taken by the two halves before it, but no single rule covers it
        FirewallPolicy policy = Lax(RuleAction.Allow);
        policy.Append(Build.Rule("low-half", RuleAction.Allow, "10.0.0.0/9"));
        policy.Append(Build.Rule("high-half", RuleAction.Allow, "10.128.0.0/9"));
        policy.Append(Build.Rule("whole", RuleAction.Deny, "10.0.0.0/8"));

        Assert.DoesNotContain(policy.FindAnomalies(), a => a.Rule.Value == "whole");
        Assert.True(policy.IsReachable(new RuleId("whole")));
    }

    [Fact]
    public void Rules_of_the_other_family_never_make_a_rule_useless() {
        FirewallPolicy policy = Lax(RuleAction.Allow);
        policy.Append(Build.Rule("all-v6", RuleAction.Deny, "::/0", "::/0"));
        policy.Append(Build.Rule("deny-v4", RuleAction.Deny, "10.0.0.0/8"));

        Assert.DoesNotContain(policy.FindAnomalies(), a => a.Rule.Value == "deny-v4");
    }

    [Fact]
    public void IsReachable_is_true_for_the_first_rule_and_for_rules_no_earlier_rule_covers() {
        FirewallPolicy policy = Lax();
        policy.Append(Build.Rule("ten", RuleAction.Allow, "10.0.0.0/8"));
        policy.Append(Build.Rule("eleven", RuleAction.Allow, "11.0.0.0/8"));

        Assert.True(policy.IsReachable(new RuleId("ten")));
        Assert.True(policy.IsReachable(new RuleId("eleven")));
    }

    [Fact]
    public void IsReachable_is_false_when_an_earlier_rule_covers_it() {
        FirewallPolicy policy = Lax();
        policy.Append(Build.Rule("ten", RuleAction.Allow, "10.0.0.0/8"));
        policy.Append(Build.Rule("ten-one", RuleAction.Deny, "10.1.0.0/16"));

        Assert.False(policy.IsReachable(new RuleId("ten-one")));
    }

    [Fact]
    public void IsReachable_of_an_absent_rule_is_refused() {
        FirewallPolicy policy = Lax();
        RuleId         anyAbsent = Any.RuleId().Generate();

        Build.Refused(PolicyRefusalReason.RuleNotFound, () => policy.IsReachable(anyAbsent));
    }

    [Fact]
    public void A_rule_is_reachable_exactly_when_it_is_not_flagged_unreachable() {
        FirewallPolicy policy = Lax(RuleAction.Deny);
        policy.Append(Build.Rule("a", RuleAction.Allow, "10.0.0.0/8"));
        policy.Append(Build.Rule("b", RuleAction.Deny, "10.1.0.0/16"));
        policy.Append(Build.Rule("c", RuleAction.Allow, "10.2.0.0/16"));
        policy.Append(Build.Rule("d", RuleAction.Allow, "172.16.0.0/12"));

        HashSet<string> unreachable = policy.FindAnomalies().Where(a => a.IsUnreachable).Select(a => a.Rule.Value).ToHashSet();

        foreach (FirewallRule rule in policy.Rules) {
            Assert.Equal(!unreachable.Contains(rule.Id.Value), policy.IsReachable(rule.Id));
        }
    }

    [Fact]
    public void A_strict_policy_has_no_unreachable_rule() {
        FirewallPolicy policy = Strict(RuleAction.Allow);
        policy.Append(Build.Rule("ssh", RuleAction.Deny, protocol: IpProtocols.Tcp, destinationPorts: "22"));
        policy.Append(Build.Rule("lan", RuleAction.Allow, "192.168.0.0/16"));
        policy.Append(Build.Rule("v6", RuleAction.Deny, "2001:db8::/32", "::/0"));
        policy.Move(new RuleId("lan"), 0);
        policy.Insert(1, Build.Rule("telnet", RuleAction.Deny, protocol: IpProtocols.Tcp, destinationPorts: "23"));

        Assert.DoesNotContain(policy.FindAnomalies(), a => a.IsUnreachable);
        foreach (FirewallRule rule in policy.Rules) { Assert.True(policy.IsReachable(rule.Id)); }
    }

}
