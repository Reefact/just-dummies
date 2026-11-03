using JustDummies;

using NetworkAcl.Domain;
using NetworkAcl.Tests.Expert.Arbitrary;

using Xunit;

using static NetworkAcl.Tests.Expert.Shorthand;

namespace NetworkAcl.Tests.Expert.Tests;

public sealed class FirewallPolicyInsertTests {

    /// <summary>A rule whose id is the point (tests read the order back) and whose body is a dummy.</summary>
    private static FirewallRule Named(string id) => Any.FirewallRule().WithId(Id(id)).Generate();

    private static string[] Order(FirewallPolicy policy) => policy.Rules.Select(rule => rule.Id.Value).ToArray();

    [Fact]
    public void Append_puts_the_rule_last() {
        // Arrange
        FirewallPolicy policy = Policy(RuleAction.Deny, strict: false);

        // Act
        policy.Append(Named("first"));
        policy.Append(Named("second"));

        // Assert
        Assert.Equal(["first", "second"], Order(policy));
        Assert.Equal(2, policy.Count);
    }

    [Fact]
    public void Insert_at_zero_puts_the_rule_first_and_at_count_puts_it_last() {
        // Arrange
        FirewallPolicy policy = Policy(RuleAction.Deny, strict: false, Named("middle"));

        // Act
        policy.Insert(0, Named("head"));
        policy.Insert(policy.Count, Named("tail"));

        // Assert
        Assert.Equal(["head", "middle", "tail"], Order(policy));
    }

    [Fact]
    public void Insert_in_the_middle_pushes_the_following_rules_down() {
        // Arrange
        FirewallPolicy policy = Policy(RuleAction.Deny, strict: false, Named("a"), Named("b"), Named("c"));

        // Act
        policy.Insert(1, Named("inserted"));

        // Assert
        Assert.Equal(["a", "inserted", "b", "c"], Order(policy));
    }

    [Fact]
    public void Each_accepted_change_adds_one_to_the_revision() {
        // Arrange
        FirewallPolicy policy = Policy(RuleAction.Allow, strict: false);

        // Act & Assert
        policy.Append(Named("a"));
        Assert.Equal(1L, policy.Revision);
        policy.Insert(0, Named("b"));
        Assert.Equal(2L, policy.Revision);
    }

    [Fact]
    public void IndexOf_and_Find_locate_a_rule_by_its_id() {
        // Arrange
        FirewallRule second = Named("second");
        FirewallPolicy policy = Policy(RuleAction.Deny, strict: false, Named("first"), second);

        // Act & Assert
        Assert.Equal(0, policy.IndexOf(Id("first")));
        Assert.Equal(1, policy.IndexOf(Id("second")));
        Assert.Same(second, policy.Find(Id("second")));
    }

    [Fact]
    public void IndexOf_is_minus_one_and_Find_is_null_when_the_rule_is_absent() {
        // Arrange
        FirewallPolicy policy = Policy(RuleAction.Deny, strict: false, Named("first"));

        // Act & Assert
        Assert.Equal(-1, policy.IndexOf(Id("absent")));
        Assert.Null(policy.Find(Id("absent")));
    }

    [Fact]
    public void IndexOf_and_Find_refuse_a_missing_id() {
        // Arrange
        FirewallPolicy policy = Policy(RuleAction.Deny);

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => policy.IndexOf(null!));
        Assert.Throws<ArgumentNullException>(() => policy.Find(null!));
    }

    [Fact]
    public void A_missing_rule_is_refused() {
        // Arrange
        FirewallPolicy policy = Policy(RuleAction.Deny);

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => policy.Append(null!));
        Assert.Throws<ArgumentNullException>(() => policy.Insert(0, null!));
    }

    [Fact]
    public void A_rule_whose_id_is_already_in_the_policy_is_refused_even_when_the_rule_differs() {
        // Arrange
        FirewallPolicy policy = Policy(RuleAction.Deny, strict: false, Named("web"));

        // Act
        PolicyRefusedException refusal = Assert.Throws<PolicyRefusedException>(() => policy.Append(Named("web")));

        // Assert
        Assert.Equal(PolicyRefusalReason.DuplicateRuleId, refusal.Reason);
        Assert.IsAssignableFrom<InvalidOperationException>(refusal);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(3)]
    [InlineData(int.MaxValue)]
    public void A_position_outside_zero_to_count_is_refused(int position) {
        // Arrange
        FirewallPolicy policy = Policy(RuleAction.Deny, strict: false, Named("a"), Named("b"));

        // Act
        PolicyRefusedException refusal = Assert.Throws<PolicyRefusedException>(() => policy.Insert(position, Named("c")));

        // Assert
        Assert.Equal(PolicyRefusalReason.PositionOutOfRange, refusal.Reason);
    }

    [Fact]
    public void A_policy_at_capacity_refuses_another_rule_until_one_is_removed() {
        // Arrange
        FirewallPolicy policy = new FirewallPolicy("tiny", RuleAction.Deny, 2, false);
        policy.Append(Named("a"));
        policy.Append(Named("b"));

        // Act
        PolicyRefusedException refusal = Assert.Throws<PolicyRefusedException>(() => policy.Append(Named("c")));
        policy.Remove(Id("a"));
        policy.Append(Named("c"));

        // Assert
        Assert.Equal(PolicyRefusalReason.CapacityReached, refusal.Reason);
        Assert.Equal(["b", "c"], Order(policy));
    }

    [Fact]
    public void A_policy_of_capacity_one_holds_exactly_one_rule() {
        // Arrange
        FirewallPolicy policy = new FirewallPolicy("single", RuleAction.Deny, 1, false);

        // Act
        policy.Append(Named("only"));

        // Assert
        Assert.Equal(PolicyRefusalReason.CapacityReached, Assert.Throws<PolicyRefusedException>(() => policy.Append(Named("other"))).Reason);
    }

    [Fact]
    public void A_refused_change_leaves_the_policy_and_its_revision_untouched() {
        // Arrange
        FirewallPolicy policy = Policy(RuleAction.Deny, strict: false, Named("a"), Named("b"));
        long revision = policy.Revision;

        // Act
        Assert.Throws<PolicyRefusedException>(() => policy.Append(Named("a")));
        Assert.Throws<PolicyRefusedException>(() => policy.Insert(9, Named("c")));

        // Assert
        Assert.Equal(["a", "b"], Order(policy));
        Assert.Equal(revision, policy.Revision);
    }

    [Fact]
    public void A_duplicate_is_reported_before_a_bad_position_and_a_bad_position_before_a_full_policy() {
        // Arrange
        FirewallPolicy policy = new FirewallPolicy("tiny", RuleAction.Deny, 1, false);
        policy.Append(Named("a"));

        // Act
        PolicyRefusedException duplicateAtBadPosition = Assert.Throws<PolicyRefusedException>(() => policy.Insert(7, Named("a")));
        PolicyRefusedException badPositionWhenFull     = Assert.Throws<PolicyRefusedException>(() => policy.Insert(7, Named("b")));
        PolicyRefusedException fullAtGoodPosition      = Assert.Throws<PolicyRefusedException>(() => policy.Insert(0, Named("b")));

        // Assert
        Assert.Equal(PolicyRefusalReason.DuplicateRuleId, duplicateAtBadPosition.Reason);
        Assert.Equal(PolicyRefusalReason.PositionOutOfRange, badPositionWhenFull.Reason);
        Assert.Equal(PolicyRefusalReason.CapacityReached, fullAtGoodPosition.Reason);
    }

    [Fact]
    public void A_lenient_policy_accepts_a_rule_that_an_earlier_rule_covers() {
        // Arrange
        FirewallPolicy policy = Policy(RuleAction.Deny, strict: false, Rule(id: "broad", source: "10.0.0.0/8"));

        // Act
        policy.Append(Rule(id: "narrow", source: "10.1.0.0/16"));

        // Assert
        Assert.Equal(["broad", "narrow"], Order(policy));
        Assert.False(policy.IsReachable(Id("narrow")));
    }

}

public sealed class FirewallPolicyStrictModeTests {

    private static string[] Order(FirewallPolicy policy) => policy.Rules.Select(rule => rule.Id.Value).ToArray();

    [Theory]
    [InlineData(RuleAction.Allow)]   // same action: redundant
    [InlineData(RuleAction.Deny)]    // opposite action: shadowed
    public void A_strict_policy_refuses_a_rule_an_earlier_rule_covers(RuleAction action) {
        // Arrange
        FirewallPolicy policy = Policy(RuleAction.Deny, strict: true, Rule(id: "broad", action: RuleAction.Allow, source: "10.0.0.0/8"));

        // Act
        PolicyRefusedException refusal = Assert.Throws<PolicyRefusedException>(() => policy.Append(Rule(id: "narrow", action: action, source: "10.1.0.0/16")));

        // Assert
        Assert.Equal(PolicyRefusalReason.RuleWouldBeUnreachable, refusal.Reason);
        Assert.Contains("broad", refusal.Message);
        Assert.Equal(["broad"], Order(policy));
    }

    [Fact]
    public void A_strict_policy_refuses_a_rule_with_the_same_geometry_as_an_earlier_one() {
        // Arrange
        FirewallPolicy policy = Policy(RuleAction.Deny, strict: true, Rule(id: "first", source: "10.0.0.0/8"));

        // Act
        PolicyRefusedException refusal = Assert.Throws<PolicyRefusedException>(() => policy.Append(Rule(id: "twin", action: RuleAction.Deny, source: "10.0.0.0/8")));

        // Assert
        Assert.Equal(PolicyRefusalReason.RuleWouldBeUnreachable, refusal.Reason);
    }

    [Fact]
    public void A_strict_policy_refuses_to_put_a_rule_in_front_of_one_it_covers() {
        // Arrange
        FirewallPolicy policy = Policy(RuleAction.Deny, strict: true, Rule(id: "narrow", source: "10.1.0.0/16"));

        // Act
        PolicyRefusedException refusal = Assert.Throws<PolicyRefusedException>(() => policy.Insert(0, Rule(id: "broad", source: "10.0.0.0/8")));

        // Assert
        Assert.Equal(PolicyRefusalReason.RuleWouldShadowAnother, refusal.Reason);
        Assert.Contains("narrow", refusal.Message);
        Assert.Equal(["narrow"], Order(policy));
    }

    [Fact]
    public void A_strict_policy_accepts_the_specific_rule_first_and_the_general_one_after() {
        // Arrange
        FirewallPolicy policy = Policy(RuleAction.Deny, strict: true);

        // Act
        policy.Append(Rule(id: "narrow", action: RuleAction.Deny, source: "10.1.0.0/16"));
        policy.Append(Rule(id: "broad", action: RuleAction.Allow, source: "10.0.0.0/8"));

        // Assert
        Assert.Equal(["narrow", "broad"], Order(policy));
    }

    [Fact]
    public void A_strict_policy_accepts_rules_that_overlap_without_one_covering_the_other() {
        // Arrange
        FirewallPolicy policy = Policy(RuleAction.Deny, strict: true);

        // Act
        policy.Append(Rule(id: "low", protocol: IpProtocols.Tcp, destinationPorts: Ports(80, 90)));
        policy.Append(Rule(id: "high", protocol: IpProtocols.Tcp, destinationPorts: Ports(85, 95)));

        // Assert
        Assert.Equal(["low", "high"], Order(policy));
    }

    [Fact]
    public void A_strict_policy_accepts_a_rule_with_flags_after_one_without_but_not_the_reverse() {
        // Arrange
        FirewallRule flagged   = Rule(id: "flagged", protocol: IpProtocols.Tcp, tcpFlags: TcpFlagMatch.NewConnection);
        FirewallRule unflagged = Rule(id: "unflagged", protocol: IpProtocols.Tcp);

        FirewallPolicy flaggedFirst   = Policy(RuleAction.Deny, strict: true, flagged);
        FirewallPolicy unflaggedFirst = Policy(RuleAction.Deny, strict: true, unflagged);

        // Act
        flaggedFirst.Append(unflagged);
        PolicyRefusedException refusal = Assert.Throws<PolicyRefusedException>(() => unflaggedFirst.Append(flagged));

        // Assert
        Assert.Equal(["flagged", "unflagged"], Order(flaggedFirst));
        Assert.Equal(PolicyRefusalReason.RuleWouldBeUnreachable, refusal.Reason);
    }

    [Fact]
    public void A_strict_policy_does_not_confuse_rules_of_different_families() {
        // Arrange
        FirewallPolicy policy = Policy(RuleAction.Deny, strict: true, Rule(id: "everything-v4"));

        // Act
        policy.Append(Rule(id: "everything-v6", source: "::/0", destination: "::/0"));

        // Assert
        Assert.Equal(["everything-v4", "everything-v6"], Order(policy));
    }

    [Fact]
    public void In_a_strict_policy_capacity_is_reported_before_unreachability() {
        // Arrange
        FirewallPolicy policy = new FirewallPolicy("tiny", RuleAction.Deny, 1, true);
        policy.Append(Rule(id: "broad", source: "10.0.0.0/8"));

        // Act
        PolicyRefusedException refusal = Assert.Throws<PolicyRefusedException>(() => policy.Append(Rule(id: "narrow", source: "10.1.0.0/16")));

        // Assert
        Assert.Equal(PolicyRefusalReason.CapacityReached, refusal.Reason);
    }

    [Fact]
    public void Removing_a_rule_is_never_refused_in_a_strict_policy() {
        // Arrange
        FirewallPolicy policy = Policy(RuleAction.Deny, strict: true,
                                       Rule(id: "narrow", source: "10.1.0.0/16"),
                                       Rule(id: "other", source: "192.168.0.0/16"),
                                       Rule(id: "broad", source: "10.0.0.0/8"));

        // Act
        policy.Remove(Id("narrow"));
        policy.Remove(Id("broad"));

        // Assert
        Assert.Equal(["other"], Order(policy));
    }

}

public sealed class FirewallPolicyMoveAndRemoveTests {

    private static FirewallRule Named(string id) => Any.FirewallRule().WithId(Id(id)).Generate();

    private static string[] Order(FirewallPolicy policy) => policy.Rules.Select(rule => rule.Id.Value).ToArray();

    [Theory]
    [InlineData("a", 2, new[] { "b", "c", "a" })]
    [InlineData("c", 0, new[] { "c", "a", "b" })]
    [InlineData("a", 1, new[] { "b", "a", "c" })]
    [InlineData("b", 0, new[] { "b", "a", "c" })]
    [InlineData("c", 1, new[] { "a", "c", "b" })]
    public void Move_leaves_the_rule_at_the_requested_position(string id, int position, string[] expectedOrder) {
        // Arrange
        FirewallPolicy policy = Policy(RuleAction.Deny, strict: false, Named("a"), Named("b"), Named("c"));

        // Act
        policy.Move(Id(id), position);

        // Assert
        Assert.Equal(expectedOrder, Order(policy));
        Assert.Equal(4L, policy.Revision);   // three appends and one move
    }

    [Fact]
    public void Moving_a_rule_to_where_it_already_is_changes_nothing_not_even_the_revision() {
        // Arrange
        FirewallPolicy policy = Policy(RuleAction.Deny, strict: false, Named("a"), Named("b"));
        long revision = policy.Revision;

        // Act
        policy.Move(Id("b"), 1);

        // Assert
        Assert.Equal(["a", "b"], Order(policy));
        Assert.Equal(revision, policy.Revision);
    }

    [Fact]
    public void Move_refuses_an_unknown_rule() {
        // Arrange
        FirewallPolicy policy = Policy(RuleAction.Deny, strict: false, Named("a"));

        // Act
        PolicyRefusedException refusal = Assert.Throws<PolicyRefusedException>(() => policy.Move(Id("absent"), 0));

        // Assert
        Assert.Equal(PolicyRefusalReason.RuleNotFound, refusal.Reason);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(3)]      // count, which Insert accepts but Move does not: the rule must end up on an existing slot
    [InlineData(100)]
    public void Move_refuses_a_position_outside_zero_to_count_minus_one(int position) {
        // Arrange
        FirewallPolicy policy = Policy(RuleAction.Deny, strict: false, Named("a"), Named("b"), Named("c"));

        // Act
        PolicyRefusedException refusal = Assert.Throws<PolicyRefusedException>(() => policy.Move(Id("a"), position));

        // Assert
        Assert.Equal(PolicyRefusalReason.PositionOutOfRange, refusal.Reason);
        Assert.Equal(["a", "b", "c"], Order(policy));
    }

    [Fact]
    public void Move_reports_an_unknown_rule_before_a_bad_position() {
        // Arrange
        FirewallPolicy policy = Policy(RuleAction.Deny, strict: false, Named("a"));

        // Act
        PolicyRefusedException refusal = Assert.Throws<PolicyRefusedException>(() => policy.Move(Id("absent"), 99));

        // Assert
        Assert.Equal(PolicyRefusalReason.RuleNotFound, refusal.Reason);
    }

    [Fact]
    public void Move_refuses_a_missing_id() {
        Assert.Throws<ArgumentNullException>(() => Policy(RuleAction.Deny).Move(null!, 0));
    }

    [Fact]
    public void A_moved_rule_keeps_the_hits_it_had_counted() {
        // Arrange
        FirewallPolicy policy = Policy(RuleAction.Deny, strict: false, Rule(id: "web", protocol: IpProtocols.Tcp), Rule(id: "other", protocol: IpProtocols.Udp));
        policy.Evaluate(Any.Ipv4Packet().WithProtocol(IpProtocols.Tcp).Generate());

        // Act
        policy.Move(Id("web"), 1);

        // Assert
        Assert.Equal(1L, policy.HitsOf(Id("web")));
    }

    [Fact]
    public void A_strict_policy_refuses_to_move_a_rule_in_front_of_one_it_covers() {
        // Arrange
        FirewallPolicy policy = Policy(RuleAction.Deny, strict: true,
                                       Rule(id: "narrow", source: "10.1.0.0/16"),
                                       Rule(id: "broad", source: "10.0.0.0/8"));
        long revision = policy.Revision;

        // Act
        PolicyRefusedException refusal = Assert.Throws<PolicyRefusedException>(() => policy.Move(Id("broad"), 0));

        // Assert
        Assert.Equal(PolicyRefusalReason.RuleWouldShadowAnother, refusal.Reason);
        Assert.Equal(["narrow", "broad"], Order(policy));
        Assert.Equal(revision, policy.Revision);
    }

    [Fact]
    public void A_strict_policy_refuses_to_move_a_rule_behind_one_that_covers_it() {
        // Arrange
        FirewallPolicy policy = Policy(RuleAction.Deny, strict: true,
                                       Rule(id: "narrow", source: "10.1.0.0/16"),
                                       Rule(id: "other", source: "192.168.0.0/16"),
                                       Rule(id: "broad", source: "10.0.0.0/8"));

        // Act
        PolicyRefusedException refusal = Assert.Throws<PolicyRefusedException>(() => policy.Move(Id("narrow"), 2));

        // Assert
        Assert.Equal(PolicyRefusalReason.RuleWouldBeUnreachable, refusal.Reason);
        Assert.Contains("broad", refusal.Message);
        Assert.Equal(["narrow", "other", "broad"], Order(policy));
    }

    [Fact]
    public void A_strict_policy_accepts_a_move_that_keeps_every_rule_reachable() {
        // Arrange
        FirewallPolicy policy = Policy(RuleAction.Deny, strict: true,
                                       Rule(id: "narrow", source: "10.1.0.0/16"),
                                       Rule(id: "other", source: "192.168.0.0/16"),
                                       Rule(id: "broad", source: "10.0.0.0/8"));

        // Act
        policy.Move(Id("other"), 2);

        // Assert
        Assert.Equal(["narrow", "broad", "other"], Order(policy));
    }

    [Fact]
    public void Remove_returns_the_rule_and_closes_the_gap() {
        // Arrange
        FirewallRule b = Named("b");
        FirewallPolicy policy = Policy(RuleAction.Deny, strict: false, Named("a"), b, Named("c"));

        // Act
        FirewallRule removed = policy.Remove(Id("b"));

        // Assert
        Assert.Same(b, removed);
        Assert.Equal(["a", "c"], Order(policy));
        Assert.Equal(4L, policy.Revision);
    }

    [Fact]
    public void Remove_refuses_an_unknown_rule() {
        // Arrange
        FirewallPolicy policy = Policy(RuleAction.Deny, strict: false, Named("a"));

        // Act
        PolicyRefusedException refusal = Assert.Throws<PolicyRefusedException>(() => policy.Remove(Id("absent")));

        // Assert
        Assert.Equal(PolicyRefusalReason.RuleNotFound, refusal.Reason);
        Assert.Equal(1L, policy.Revision);
    }

    [Fact]
    public void Remove_refuses_a_missing_id() {
        Assert.Throws<ArgumentNullException>(() => Policy(RuleAction.Deny).Remove(null!));
    }

    [Fact]
    public void Remove_drops_the_counter_and_a_rule_added_back_starts_from_zero() {
        // Arrange
        FirewallPolicy policy = Policy(RuleAction.Deny, strict: false, Rule(id: "web", protocol: IpProtocols.Tcp));
        policy.Evaluate(Any.Ipv4Packet().WithProtocol(IpProtocols.Tcp).Generate());

        // Act
        policy.Remove(Id("web"));
        PolicyRefusedException refusal = Assert.Throws<PolicyRefusedException>(() => policy.HitsOf(Id("web")));
        policy.Append(Rule(id: "web", protocol: IpProtocols.Tcp));

        // Assert
        Assert.Equal(PolicyRefusalReason.RuleNotFound, refusal.Reason);
        Assert.Equal(0L, policy.HitsOf(Id("web")));
    }

}
