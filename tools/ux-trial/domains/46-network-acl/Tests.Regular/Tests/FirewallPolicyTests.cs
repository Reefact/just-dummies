using JustDummies;

using NetworkAcl.Domain;

using Xunit;

using static NetworkAcl.Tests.Regular.PolicyFixtures;

namespace NetworkAcl.Tests.Regular;

public sealed class FirewallPolicyTests {

    // ---- Construction ----------------------------------------------------------------------------------------

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("\t\r\n")]
    public void A_blank_name_is_refused(string name) {
        // Arrange
        RuleAction anyAction = Any.Enum<RuleAction>().Generate();

        // Act
        ArgumentException thrown = Assert.Throws<ArgumentException>(() => new FirewallPolicy(name, anyAction, 10, false));

        // Assert
        Assert.Equal("name", thrown.ParamName);
    }

    [Fact]
    public void A_missing_name_is_refused() {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new FirewallPolicy(null!, RuleAction.Deny, 10, false));
    }

    [Fact]
    public void A_name_may_have_64_characters_and_not_65() {
        // Arrange
        string longest = Any.String().Alpha().WithLength(FirewallPolicy.MaxNameLength).Generate();
        string tooLong = Any.String().Alpha().WithLength(FirewallPolicy.MaxNameLength + 1).Generate();

        // Act
        FirewallPolicy policy = new FirewallPolicy(longest, RuleAction.Deny, 10, false);
        ArgumentException thrown = Assert.Throws<ArgumentException>(() => new FirewallPolicy(tooLong, RuleAction.Deny, 10, false));

        // Assert
        Assert.Equal(longest, policy.Name);
        Assert.Equal("name", thrown.ParamName);
    }

    [Fact]
    public void An_undefined_default_action_is_refused() {
        // Arrange
        RuleAction undefined = (RuleAction)Any.Int32().Between(2, 100).Generate();

        // Act
        ArgumentOutOfRangeException thrown = Assert.Throws<ArgumentOutOfRangeException>(() => new FirewallPolicy("edge", undefined, 10, false));

        // Assert
        Assert.Equal("defaultAction", thrown.ParamName);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(10001)]
    [InlineData(int.MaxValue)]
    [InlineData(int.MinValue)]
    public void A_capacity_outside_1_to_10000_is_out_of_range(int capacity) {
        // Act
        ArgumentOutOfRangeException thrown = Assert.Throws<ArgumentOutOfRangeException>(() => new FirewallPolicy("edge", RuleAction.Deny, capacity, false));

        // Assert
        Assert.Equal("capacity", thrown.ParamName);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(10000)]
    public void The_extreme_capacities_are_accepted(int capacity) {
        // Act
        FirewallPolicy policy = new FirewallPolicy("edge", RuleAction.Deny, capacity, false);

        // Assert
        Assert.Equal(capacity, policy.Capacity);
    }

    [Fact]
    public void The_name_is_checked_first_then_the_action_then_the_capacity() {
        // Act
        string? name   = Assert.Throws<ArgumentException>(() => new FirewallPolicy(" ", (RuleAction)9, 0, false)).ParamName;
        string? action = Assert.Throws<ArgumentOutOfRangeException>(() => new FirewallPolicy("edge", (RuleAction)9, 0, false)).ParamName;

        // Assert
        Assert.Equal("name", name);
        Assert.Equal("defaultAction", action);
    }

    [Fact]
    public void A_new_policy_is_empty_and_untouched() {
        // Arrange
        string     anyName   = Any.String().NotBlank().WithMaxLength(FirewallPolicy.MaxNameLength).Generate();
        RuleAction anyAction = Any.Enum<RuleAction>().Generate();
        int        anyCap    = Any.Int32().Between(1, FirewallPolicy.MaxCapacity).Generate();
        bool       anyStrict = Any.Boolean().Generate();

        // Act
        FirewallPolicy policy = new FirewallPolicy(anyName, anyAction, anyCap, anyStrict);

        // Assert
        Assert.Equal(anyName, policy.Name);
        Assert.Equal(anyAction, policy.DefaultAction);
        Assert.Equal(anyCap, policy.Capacity);
        Assert.Equal(anyStrict, policy.IsStrict);
        Assert.Empty(policy.Rules);
        Assert.Equal(0, policy.Count);
        Assert.Equal(0, policy.Revision);
        Assert.Equal(0, policy.DefaultHits);
    }

    // ---- Insert and Append -----------------------------------------------------------------------------------

    [Fact]
    public void Append_puts_a_rule_last() {
        // Arrange
        FirewallPolicy policy = PolicyOf(RuleAction.Deny, strict: false);
        List<RuleId>   ids    = DistinctIds(3);

        // Act
        policy.Append(Rule(ids[0], RuleAction.Allow, "10.0.0.0/8", "0.0.0.0/0"));
        policy.Append(Rule(ids[1], RuleAction.Allow, "11.0.0.0/8", "0.0.0.0/0"));
        policy.Append(Rule(ids[2], RuleAction.Allow, "12.0.0.0/8", "0.0.0.0/0"));

        // Assert
        Assert.Equal(ids, policy.Rules.Select(rule => rule.Id).ToList());
        Assert.Equal(3, policy.Count);
    }

    [Fact]
    public void Insert_places_a_rule_at_the_position_asked() {
        // Arrange
        FirewallPolicy policy = PolicyOf(RuleAction.Deny, strict: false);
        List<RuleId>   ids    = DistinctIds(4);
        policy.Append(Rule(ids[0], RuleAction.Allow, "10.0.0.0/8", "0.0.0.0/0"));
        policy.Append(Rule(ids[1], RuleAction.Allow, "11.0.0.0/8", "0.0.0.0/0"));

        // Act
        policy.Insert(0, Rule(ids[2], RuleAction.Allow, "12.0.0.0/8", "0.0.0.0/0"));
        policy.Insert(2, Rule(ids[3], RuleAction.Allow, "13.0.0.0/8", "0.0.0.0/0"));

        // Assert
        Assert.Equal(new[] { ids[2], ids[0], ids[3], ids[1] }, policy.Rules.Select(rule => rule.Id).ToArray());
    }

    [Fact]
    public void Inserting_at_the_count_is_appending() {
        // Arrange
        FirewallPolicy policy = PolicyOf(RuleAction.Deny, strict: false);
        List<RuleId>   ids    = DistinctIds(2);
        policy.Append(Rule(ids[0], RuleAction.Allow, "10.0.0.0/8", "0.0.0.0/0"));

        // Act
        policy.Insert(policy.Count, Rule(ids[1], RuleAction.Allow, "11.0.0.0/8", "0.0.0.0/0"));

        // Assert
        Assert.Equal(ids[1], policy.Rules[1].Id);
    }

    [Fact]
    public void Every_accepted_change_adds_one_to_the_revision() {
        // Arrange
        FirewallPolicy policy = PolicyOf(RuleAction.Deny, strict: false);
        List<RuleId>   ids    = DistinctIds(2);

        // Act
        policy.Append(Rule(ids[0], RuleAction.Allow, "10.0.0.0/8", "0.0.0.0/0"));
        long afterAppend = policy.Revision;
        policy.Insert(0, Rule(ids[1], RuleAction.Allow, "11.0.0.0/8", "0.0.0.0/0"));
        long afterInsert = policy.Revision;
        policy.Move(ids[1], 1);
        long afterMove = policy.Revision;
        policy.Remove(ids[0]);
        long afterRemove = policy.Revision;

        // Assert
        Assert.Equal(new long[] { 1, 2, 3, 4 }, new[] { afterAppend, afterInsert, afterMove, afterRemove });
    }

    [Fact]
    public void A_duplicate_id_is_refused_and_changes_nothing() {
        // Arrange
        FirewallPolicy policy = PolicyOf(RuleAction.Deny, strict: false);
        RuleId         anyId  = Any.RuleId().Generate();
        policy.Append(Rule(anyId, RuleAction.Allow, "10.0.0.0/8", "0.0.0.0/0"));
        long revision = policy.Revision;

        // Act
        PolicyRefusedException thrown = Assert.Throws<PolicyRefusedException>(() => policy.Append(Rule(new RuleId(anyId.Value), RuleAction.Deny, "11.0.0.0/8", "0.0.0.0/0")));

        // Assert
        Assert.Equal(PolicyRefusalReason.DuplicateRuleId, thrown.Reason);
        Assert.Equal(1, policy.Count);
        Assert.Equal(revision, policy.Revision);
    }

    [Fact]
    public void The_duplicate_is_noticed_before_the_position() {
        // Arrange
        FirewallPolicy policy = PolicyOf(RuleAction.Deny, strict: false);
        RuleId         anyId  = Any.RuleId().Generate();
        policy.Append(Rule(anyId, RuleAction.Allow, "10.0.0.0/8", "0.0.0.0/0"));

        // Act
        PolicyRefusedException thrown = Assert.Throws<PolicyRefusedException>(() => policy.Insert(99, Rule(anyId, RuleAction.Allow, "10.0.0.0/8", "0.0.0.0/0")));

        // Assert
        Assert.Equal(PolicyRefusalReason.DuplicateRuleId, thrown.Reason);
    }

    [Fact]
    public void A_position_outside_zero_to_count_is_refused() {
        // Arrange
        FirewallPolicy policy = PolicyOf(RuleAction.Deny, strict: false);
        List<RuleId>   ids    = DistinctIds(3);
        policy.Append(Rule(ids[0], RuleAction.Allow, "10.0.0.0/8", "0.0.0.0/0"));

        // Act
        PolicyRefusedException before = Assert.Throws<PolicyRefusedException>(() => policy.Insert(-1, Rule(ids[1], RuleAction.Allow, "11.0.0.0/8", "0.0.0.0/0")));
        PolicyRefusedException after  = Assert.Throws<PolicyRefusedException>(() => policy.Insert(2, Rule(ids[2], RuleAction.Allow, "12.0.0.0/8", "0.0.0.0/0")));

        // Assert
        Assert.Equal(PolicyRefusalReason.PositionOutOfRange, before.Reason);
        Assert.Equal(PolicyRefusalReason.PositionOutOfRange, after.Reason);
        Assert.Equal(1, policy.Count);
    }

    [Fact]
    public void A_full_policy_refuses_one_more_rule_and_the_last_free_slot_is_usable() {
        // Arrange
        FirewallPolicy policy = Any.FirewallPolicy().WithCapacity(2).WithStrict(false).Generate();
        List<RuleId>   ids    = DistinctIds(3);
        policy.Append(Rule(ids[0], RuleAction.Allow, "10.0.0.0/8", "0.0.0.0/0"));
        policy.Append(Rule(ids[1], RuleAction.Allow, "11.0.0.0/8", "0.0.0.0/0"));

        // Act
        PolicyRefusedException thrown = Assert.Throws<PolicyRefusedException>(() => policy.Append(Rule(ids[2], RuleAction.Allow, "12.0.0.0/8", "0.0.0.0/0")));

        // Assert
        Assert.Equal(PolicyRefusalReason.CapacityReached, thrown.Reason);
        Assert.Equal(2, policy.Count);
    }

    [Fact]
    public void A_bad_position_is_noticed_before_a_full_policy() {
        // Arrange
        FirewallPolicy policy = Any.FirewallPolicy().WithCapacity(1).WithStrict(false).Generate();
        List<RuleId>   ids    = DistinctIds(2);
        policy.Append(Rule(ids[0], RuleAction.Allow, "10.0.0.0/8", "0.0.0.0/0"));

        // Act
        PolicyRefusedException thrown = Assert.Throws<PolicyRefusedException>(() => policy.Insert(5, Rule(ids[1], RuleAction.Allow, "11.0.0.0/8", "0.0.0.0/0")));

        // Assert
        Assert.Equal(PolicyRefusalReason.PositionOutOfRange, thrown.Reason);
    }

    [Fact]
    public void A_full_policy_is_noticed_before_the_strict_checks() {
        // Arrange
        FirewallPolicy policy = Any.FirewallPolicy().WithCapacity(1).WithStrict(true).Generate();
        List<RuleId>   ids    = DistinctIds(2);
        policy.Append(Rule(ids[0], RuleAction.Allow, "10.0.0.0/8", "0.0.0.0/0"));

        // Act
        PolicyRefusedException thrown = Assert.Throws<PolicyRefusedException>(() => policy.Append(Rule(ids[1], RuleAction.Allow, "10.1.0.0/16", "0.0.0.0/0")));

        // Assert
        Assert.Equal(PolicyRefusalReason.CapacityReached, thrown.Reason);
    }

    [Fact]
    public void A_missing_rule_is_refused() {
        // Arrange
        FirewallPolicy policy = PolicyOf(RuleAction.Deny, strict: false);

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => policy.Append(null!));
        Assert.Throws<ArgumentNullException>(() => policy.Insert(0, null!));
    }

    [Fact]
    public void Rules_are_kept_in_the_order_they_were_given_when_non_strict_whatever_they_cover() {
        // Arrange
        FirewallPolicy policy = PolicyOf(RuleAction.Deny, strict: false);
        List<RuleId>   ids    = DistinctIds(2);

        // Act
        policy.Append(Rule(ids[0], RuleAction.Allow, "10.0.0.0/8", "0.0.0.0/0"));
        policy.Append(Rule(ids[1], RuleAction.Deny, "10.1.0.0/16", "0.0.0.0/0"));

        // Assert
        Assert.Equal(2, policy.Count);
    }

    // ---- Strict mode -----------------------------------------------------------------------------------------

    [Fact]
    public void A_strict_policy_refuses_a_rule_an_earlier_one_covers() {
        // Arrange
        FirewallPolicy policy = PolicyOf(RuleAction.Deny, strict: true);
        List<RuleId>   ids    = DistinctIds(2);
        policy.Append(Rule(ids[0], RuleAction.Allow, "10.0.0.0/8", "0.0.0.0/0"));

        // Act
        PolicyRefusedException thrown = Assert.Throws<PolicyRefusedException>(() => policy.Append(Rule(ids[1], RuleAction.Allow, "10.1.0.0/16", "0.0.0.0/0")));

        // Assert
        Assert.Equal(PolicyRefusalReason.RuleWouldBeUnreachable, thrown.Reason);
        Assert.Equal(1, policy.Count);
        Assert.Equal(1, policy.Revision);
    }

    [Fact]
    public void A_strict_policy_refuses_a_covered_rule_whatever_its_action() {
        // Arrange
        FirewallPolicy policy = PolicyOf(RuleAction.Deny, strict: true);
        List<RuleId>   ids    = DistinctIds(2);
        policy.Append(Rule(ids[0], RuleAction.Allow, "10.0.0.0/8", "0.0.0.0/0"));

        // Act
        PolicyRefusedException thrown = Assert.Throws<PolicyRefusedException>(() => policy.Append(Rule(ids[1], RuleAction.Deny, "10.1.0.0/16", "0.0.0.0/0")));

        // Assert
        Assert.Equal(PolicyRefusalReason.RuleWouldBeUnreachable, thrown.Reason);
    }

    [Fact]
    public void A_strict_policy_refuses_a_twin_of_an_existing_rule() {
        // Arrange
        FirewallPolicy policy = PolicyOf(RuleAction.Deny, strict: true);
        List<RuleId>   ids    = DistinctIds(2);
        policy.Append(Rule(ids[0], RuleAction.Allow, "10.0.0.0/8", "192.168.0.0/16", IpProtocols.Tcp, PortRange.Single(22)));

        // Act
        PolicyRefusedException thrown = Assert.Throws<PolicyRefusedException>(() => policy.Append(Rule(ids[1], RuleAction.Allow, "10.0.0.0/8", "192.168.0.0/16", IpProtocols.Tcp, PortRange.Single(22))));

        // Assert
        Assert.Equal(PolicyRefusalReason.RuleWouldBeUnreachable, thrown.Reason);
    }

    [Fact]
    public void A_strict_policy_refuses_a_rule_that_would_cover_a_later_one() {
        // Arrange
        FirewallPolicy policy = PolicyOf(RuleAction.Deny, strict: true);
        List<RuleId>   ids    = DistinctIds(2);
        policy.Append(Rule(ids[0], RuleAction.Allow, "10.1.0.0/16", "0.0.0.0/0"));

        // Act
        PolicyRefusedException thrown = Assert.Throws<PolicyRefusedException>(() => policy.Insert(0, Rule(ids[1], RuleAction.Deny, "10.0.0.0/8", "0.0.0.0/0")));

        // Assert
        Assert.Equal(PolicyRefusalReason.RuleWouldShadowAnother, thrown.Reason);
        Assert.Equal(ids[0], policy.Rules[0].Id);
    }

    [Fact]
    public void A_strict_policy_accepts_the_narrow_rule_first_and_the_wide_one_after() {
        // Arrange
        FirewallPolicy policy = PolicyOf(RuleAction.Deny, strict: true);
        List<RuleId>   ids    = DistinctIds(2);

        // Act
        policy.Append(Rule(ids[0], RuleAction.Deny, "10.1.0.0/16", "0.0.0.0/0"));
        policy.Append(Rule(ids[1], RuleAction.Allow, "10.0.0.0/8", "0.0.0.0/0"));

        // Assert
        Assert.Equal(2, policy.Count);
        Assert.DoesNotContain(policy.FindAnomalies(), anomaly => anomaly.IsUnreachable);
    }

    [Fact]
    public void The_position_decides_whether_the_same_rule_is_acceptable() {
        // Arrange
        FirewallPolicy policy = PolicyOf(RuleAction.Deny, strict: true);
        List<RuleId>   ids    = DistinctIds(3);
        policy.Append(Rule(ids[0], RuleAction.Allow, "10.0.0.0/8", "0.0.0.0/0"));
        FirewallRule narrow = Rule(ids[1], RuleAction.Deny, "10.1.0.0/16", "0.0.0.0/0");

        // Act: after the wide rule the narrow one is covered, before it is not
        PolicyRefusedException after = Assert.Throws<PolicyRefusedException>(() => policy.Insert(1, narrow));
        policy.Insert(0, narrow);

        // Assert
        Assert.Equal(PolicyRefusalReason.RuleWouldBeUnreachable, after.Reason);
        Assert.Equal(new[] { ids[1], ids[0] }, policy.Rules.Select(rule => rule.Id).ToArray());
    }

    [Fact]
    public void A_strict_policy_accepts_rules_that_only_partly_overlap() {
        // Arrange
        FirewallPolicy policy = PolicyOf(RuleAction.Deny, strict: true);
        List<RuleId>   ids    = DistinctIds(2);
        policy.Append(Rule(ids[0], RuleAction.Allow, "10.0.0.0/8", "0.0.0.0/0", IpProtocols.Tcp));

        // Act
        policy.Append(Rule(ids[1], RuleAction.Deny, "0.0.0.0/0", "192.168.0.0/16", IpProtocols.Tcp));

        // Assert
        Assert.Equal(2, policy.Count);
    }

    [Fact]
    public void A_non_strict_policy_accepts_a_covered_rule_and_a_covering_one() {
        // Arrange
        FirewallPolicy policy = PolicyOf(RuleAction.Deny, strict: false);
        List<RuleId>   ids    = DistinctIds(3);
        policy.Append(Rule(ids[0], RuleAction.Allow, "10.0.0.0/8", "0.0.0.0/0"));

        // Act
        policy.Append(Rule(ids[1], RuleAction.Deny, "10.1.0.0/16", "0.0.0.0/0"));
        policy.Insert(0, Rule(ids[2], RuleAction.Deny, "0.0.0.0/0", "0.0.0.0/0"));

        // Assert
        Assert.Equal(3, policy.Count);
    }

    // ---- Find, IndexOf, HitsOf -------------------------------------------------------------------------------

    [Fact]
    public void IndexOf_and_Find_locate_a_rule_by_its_id() {
        // Arrange
        FirewallPolicy policy = PolicyOf(RuleAction.Deny, strict: false);
        List<RuleId>   ids    = DistinctIds(3);
        FirewallRule   second = Rule(ids[1], RuleAction.Allow, "11.0.0.0/8", "0.0.0.0/0");
        policy.Append(Rule(ids[0], RuleAction.Allow, "10.0.0.0/8", "0.0.0.0/0"));
        policy.Append(second);

        // Assert
        Assert.Equal(1, policy.IndexOf(new RuleId(ids[1].Value)));
        Assert.Same(second, policy.Find(ids[1]));
        Assert.Equal(-1, policy.IndexOf(ids[2]));
        Assert.Null(policy.Find(ids[2]));
    }

    [Fact]
    public void IndexOf_and_HitsOf_refuse_null() {
        // Arrange
        FirewallPolicy policy = PolicyOf(RuleAction.Deny, strict: false);

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => policy.IndexOf(null!));
        Assert.Throws<ArgumentNullException>(() => policy.HitsOf(null!));
        Assert.Throws<ArgumentNullException>(() => policy.Find(null!));
        Assert.Throws<ArgumentNullException>(() => policy.Remove(null!));
    }

    [Fact]
    public void HitsOf_an_unknown_rule_is_refused() {
        // Arrange
        FirewallPolicy policy = PolicyOf(RuleAction.Deny, strict: false);
        RuleId         anyId  = Any.RuleId().Generate();

        // Act
        PolicyRefusedException thrown = Assert.Throws<PolicyRefusedException>(() => policy.HitsOf(anyId));

        // Assert
        Assert.Equal(PolicyRefusalReason.RuleNotFound, thrown.Reason);
    }

    // ---- Move ------------------------------------------------------------------------------------------------

    private static (FirewallPolicy Policy, List<RuleId> Ids) ThreeDisjointRules(bool strict) {
        FirewallPolicy policy = PolicyOf(RuleAction.Deny, strict);
        List<RuleId>   ids    = DistinctIds(3);
        policy.Append(Rule(ids[0], RuleAction.Allow, "10.0.0.0/8", "0.0.0.0/0"));
        policy.Append(Rule(ids[1], RuleAction.Allow, "11.0.0.0/8", "0.0.0.0/0"));
        policy.Append(Rule(ids[2], RuleAction.Allow, "12.0.0.0/8", "0.0.0.0/0"));

        return (policy, ids);
    }

    [Theory]
    [InlineData(0, 2, new[] { 1, 2, 0 })]
    [InlineData(2, 0, new[] { 2, 0, 1 })]
    [InlineData(0, 1, new[] { 1, 0, 2 })]
    [InlineData(1, 2, new[] { 0, 2, 1 })]
    public void Move_puts_the_rule_at_the_position_among_the_rules(int from, int to, int[] expectedOrder) {
        // Arrange
        (FirewallPolicy policy, List<RuleId> ids) = ThreeDisjointRules(strict: false);

        // Act
        policy.Move(ids[from], to);

        // Assert
        Assert.Equal(expectedOrder.Select(index => ids[index]).ToArray(), policy.Rules.Select(rule => rule.Id).ToArray());
        Assert.Equal(4, policy.Revision);
    }

    [Fact]
    public void Moving_a_rule_where_it_already_is_changes_nothing() {
        // Arrange
        (FirewallPolicy policy, List<RuleId> ids) = ThreeDisjointRules(strict: false);
        long revision = policy.Revision;

        // Act
        policy.Move(ids[1], 1);

        // Assert
        Assert.Equal(revision, policy.Revision);
        Assert.Equal(ids, policy.Rules.Select(rule => rule.Id).ToList());
    }

    [Fact]
    public void Moving_an_unknown_rule_is_refused() {
        // Arrange
        (FirewallPolicy policy, _) = ThreeDisjointRules(strict: false);
        RuleId anyUnknownId = new RuleId("zz" + Any.String().Alpha().InLowerCase().WithLengthBetween(1, 10).Generate());

        // Act
        PolicyRefusedException thrown = Assert.Throws<PolicyRefusedException>(() => policy.Move(anyUnknownId, 0));

        // Assert
        Assert.Equal(PolicyRefusalReason.RuleNotFound, thrown.Reason);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(3)]
    [InlineData(int.MaxValue)]
    public void A_move_to_a_position_outside_zero_to_count_minus_one_is_refused(int position) {
        // Arrange
        (FirewallPolicy policy, List<RuleId> ids) = ThreeDisjointRules(strict: false);

        // Act
        PolicyRefusedException thrown = Assert.Throws<PolicyRefusedException>(() => policy.Move(ids[0], position));

        // Assert
        Assert.Equal(PolicyRefusalReason.PositionOutOfRange, thrown.Reason);
        Assert.Equal(ids, policy.Rules.Select(rule => rule.Id).ToList());
    }

    [Fact]
    public void An_unknown_rule_is_noticed_before_the_position() {
        // Arrange
        (FirewallPolicy policy, _) = ThreeDisjointRules(strict: false);
        RuleId anyUnknownId = new RuleId("zz" + Any.String().Alpha().InLowerCase().WithLengthBetween(1, 10).Generate());

        // Act
        PolicyRefusedException thrown = Assert.Throws<PolicyRefusedException>(() => policy.Move(anyUnknownId, 99));

        // Assert
        Assert.Equal(PolicyRefusalReason.RuleNotFound, thrown.Reason);
    }

    [Fact]
    public void A_strict_policy_refuses_to_move_a_rule_behind_one_that_covers_it() {
        // Arrange
        FirewallPolicy policy = PolicyOf(RuleAction.Deny, strict: true);
        List<RuleId>   ids    = DistinctIds(2);
        policy.Append(Rule(ids[0], RuleAction.Deny, "10.1.0.0/16", "0.0.0.0/0"));
        policy.Append(Rule(ids[1], RuleAction.Allow, "10.0.0.0/8", "0.0.0.0/0"));
        long revision = policy.Revision;

        // Act
        PolicyRefusedException thrown = Assert.Throws<PolicyRefusedException>(() => policy.Move(ids[0], 1));

        // Assert
        Assert.Equal(PolicyRefusalReason.RuleWouldBeUnreachable, thrown.Reason);
        Assert.Equal(ids, policy.Rules.Select(rule => rule.Id).ToList());
        Assert.Equal(revision, policy.Revision);
    }

    [Fact]
    public void A_strict_policy_refuses_to_move_a_rule_ahead_of_one_it_covers() {
        // Arrange
        FirewallPolicy policy = PolicyOf(RuleAction.Deny, strict: true);
        List<RuleId>   ids    = DistinctIds(2);
        policy.Append(Rule(ids[0], RuleAction.Deny, "10.1.0.0/16", "0.0.0.0/0"));
        policy.Append(Rule(ids[1], RuleAction.Allow, "10.0.0.0/8", "0.0.0.0/0"));

        // Act
        PolicyRefusedException thrown = Assert.Throws<PolicyRefusedException>(() => policy.Move(ids[1], 0));

        // Assert
        Assert.Equal(PolicyRefusalReason.RuleWouldShadowAnother, thrown.Reason);
        Assert.Equal(ids, policy.Rules.Select(rule => rule.Id).ToList());
    }

    [Fact]
    public void A_non_strict_policy_lets_a_rule_move_anywhere() {
        // Arrange
        FirewallPolicy policy = PolicyOf(RuleAction.Deny, strict: false);
        List<RuleId>   ids    = DistinctIds(2);
        policy.Append(Rule(ids[0], RuleAction.Deny, "10.1.0.0/16", "0.0.0.0/0"));
        policy.Append(Rule(ids[1], RuleAction.Allow, "10.0.0.0/8", "0.0.0.0/0"));

        // Act
        policy.Move(ids[1], 0);

        // Assert
        Assert.Equal(new[] { ids[1], ids[0] }, policy.Rules.Select(rule => rule.Id).ToArray());
    }

    [Fact]
    public void A_strict_policy_lets_a_rule_move_among_rules_that_do_not_relate_to_it() {
        // Arrange
        (FirewallPolicy policy, List<RuleId> ids) = ThreeDisjointRules(strict: true);

        // Act
        policy.Move(ids[0], 2);
        policy.Move(ids[2], 0);

        // Assert
        Assert.Equal(new[] { ids[2], ids[1], ids[0] }, policy.Rules.Select(rule => rule.Id).ToArray());
    }

    // ---- Remove ----------------------------------------------------------------------------------------------

    [Fact]
    public void Remove_returns_the_rule_and_closes_the_gap() {
        // Arrange
        (FirewallPolicy policy, List<RuleId> ids) = ThreeDisjointRules(strict: false);
        FirewallRule middle = policy.Rules[1];

        // Act
        FirewallRule removed = policy.Remove(ids[1]);

        // Assert
        Assert.Same(middle, removed);
        Assert.Equal(new[] { ids[0], ids[2] }, policy.Rules.Select(rule => rule.Id).ToArray());
        Assert.Equal(4, policy.Revision);
        Assert.Equal(-1, policy.IndexOf(ids[1]));
    }

    [Fact]
    public void Removing_an_unknown_rule_is_refused() {
        // Arrange
        (FirewallPolicy policy, _) = ThreeDisjointRules(strict: false);
        RuleId anyUnknownId = new RuleId("zz" + Any.String().Alpha().InLowerCase().WithLengthBetween(1, 10).Generate());

        // Act
        PolicyRefusedException thrown = Assert.Throws<PolicyRefusedException>(() => policy.Remove(anyUnknownId));

        // Assert
        Assert.Equal(PolicyRefusalReason.RuleNotFound, thrown.Reason);
        Assert.Equal(3, policy.Revision);
    }

    [Fact]
    public void A_removed_rules_counter_goes_with_it() {
        // Arrange
        (FirewallPolicy policy, List<RuleId> ids) = ThreeDisjointRules(strict: false);
        policy.Evaluate(Packet4("10.1.1.1", "8.8.8.8"));

        // Act
        policy.Remove(ids[0]);

        // Assert
        PolicyRefusedException thrown = Assert.Throws<PolicyRefusedException>(() => policy.HitsOf(ids[0]));
        Assert.Equal(PolicyRefusalReason.RuleNotFound, thrown.Reason);
    }

    [Fact]
    public void A_rule_added_back_under_a_removed_id_starts_from_zero_hits() {
        // Arrange
        (FirewallPolicy policy, List<RuleId> ids) = ThreeDisjointRules(strict: false);
        policy.Evaluate(Packet4("10.1.1.1", "8.8.8.8"));
        policy.Remove(ids[0]);

        // Act
        policy.Append(Rule(ids[0], RuleAction.Allow, "10.0.0.0/8", "0.0.0.0/0"));

        // Assert
        Assert.Equal(0, policy.HitsOf(ids[0]));
    }

    [Fact]
    public void Removal_is_never_refused_in_a_strict_policy() {
        // Arrange
        FirewallPolicy policy = PolicyOf(RuleAction.Deny, strict: true);
        List<RuleId>   ids    = DistinctIds(3);
        policy.Append(Rule(ids[0], RuleAction.Deny, "10.1.2.0/24", "0.0.0.0/0"));
        policy.Append(Rule(ids[1], RuleAction.Deny, "10.1.0.0/16", "0.0.0.0/0"));
        policy.Append(Rule(ids[2], RuleAction.Allow, "10.0.0.0/8", "0.0.0.0/0"));

        // Act
        policy.Remove(ids[1]);
        policy.Remove(ids[0]);
        policy.Remove(ids[2]);

        // Assert
        Assert.Equal(0, policy.Count);
    }

    // ---- Evaluate --------------------------------------------------------------------------------------------

    [Fact]
    public void With_no_rule_the_default_action_decides() {
        // Arrange
        RuleAction     anyDefault = Any.Enum<RuleAction>().Generate();
        FirewallPolicy policy     = PolicyOf(anyDefault, strict: false);
        Ipv4Packet     anyPacket  = Any.Ipv4Packet().Generate();

        // Act
        Verdict verdict = policy.Evaluate(anyPacket);

        // Assert
        Assert.True(verdict.IsDefault);
        Assert.Equal(anyDefault, verdict.Action);
        Assert.Null(verdict.MatchedRule);
        Assert.Equal(1, policy.DefaultHits);
    }

    [Fact]
    public void The_first_matching_rule_wins() {
        // Arrange
        FirewallPolicy policy = PolicyOf(RuleAction.Allow, strict: false);
        List<RuleId>   ids    = DistinctIds(3);
        policy.Append(Rule(ids[0], RuleAction.Deny, "10.1.0.0/16", "0.0.0.0/0"));
        policy.Append(Rule(ids[1], RuleAction.Allow, "10.0.0.0/8", "0.0.0.0/0"));
        policy.Append(Rule(ids[2], RuleAction.Deny, "0.0.0.0/0", "0.0.0.0/0"));

        // Act
        Verdict narrow = policy.Evaluate(Packet4("10.1.5.5", "8.8.8.8"));
        Verdict wide   = policy.Evaluate(Packet4("10.2.5.5", "8.8.8.8"));
        Verdict rest   = policy.Evaluate(Packet4("172.16.0.1", "8.8.8.8"));

        // Assert
        Assert.Equal(new Verdict(RuleAction.Deny, ids[0]), narrow);
        Assert.Equal(new Verdict(RuleAction.Allow, ids[1]), wide);
        Assert.Equal(new Verdict(RuleAction.Deny, ids[2]), rest);
    }

    [Fact]
    public void Changing_the_order_changes_the_verdict() {
        // Arrange
        FirewallPolicy policy = PolicyOf(RuleAction.Deny, strict: false);
        List<RuleId>   ids    = DistinctIds(2);
        policy.Append(Rule(ids[0], RuleAction.Allow, "10.0.0.0/8", "0.0.0.0/0"));
        policy.Append(Rule(ids[1], RuleAction.Deny, "10.1.0.0/16", "0.0.0.0/0"));
        Ipv4Packet packet = Packet4("10.1.5.5", "8.8.8.8");

        // Act
        Verdict before = policy.Evaluate(packet);
        policy.Move(ids[1], 0);
        Verdict after = policy.Evaluate(packet);

        // Assert
        Assert.Equal(new Verdict(RuleAction.Allow, ids[0]), before);
        Assert.Equal(new Verdict(RuleAction.Deny, ids[1]), after);
    }

    [Fact]
    public void A_packet_no_rule_matches_gets_the_default_action() {
        // Arrange
        RuleAction     anyDefault = Any.Enum<RuleAction>().Generate();
        FirewallPolicy policy     = PolicyOf(anyDefault, strict: false);
        RuleId         anyId      = Any.RuleId().Generate();
        policy.Append(Rule(anyId, anyDefault == RuleAction.Allow ? RuleAction.Deny : RuleAction.Allow, "10.0.0.0/8", "0.0.0.0/0"));

        // Act
        Verdict verdict = policy.Evaluate(Packet4("172.16.0.1", "8.8.8.8"));

        // Assert
        Assert.True(verdict.IsDefault);
        Assert.Equal(anyDefault, verdict.Action);
        Assert.Equal(1, policy.DefaultHits);
        Assert.Equal(0, policy.HitsOf(anyId));
    }

    [Fact]
    public void Every_evaluation_counts_on_the_rule_that_decided_it() {
        // Arrange
        FirewallPolicy policy    = PolicyOf(RuleAction.Deny, strict: false);
        List<RuleId>   ids       = DistinctIds(2);
        int            anyFirst  = Any.Int32().Between(1, 20).Generate();
        int            anySecond = Any.Int32().Between(1, 20).Generate();
        int            anyOthers = Any.Int32().Between(1, 20).Generate();
        policy.Append(Rule(ids[0], RuleAction.Allow, "10.0.0.0/8", "0.0.0.0/0"));
        policy.Append(Rule(ids[1], RuleAction.Allow, "11.0.0.0/8", "0.0.0.0/0"));

        // Act
        for (int i = 0; i < anyFirst; i++) { policy.Evaluate(Packet4("10.0.0.1", "8.8.8.8")); }
        for (int i = 0; i < anySecond; i++) { policy.Evaluate(Packet4("11.0.0.1", "8.8.8.8")); }
        for (int i = 0; i < anyOthers; i++) { policy.Evaluate(Packet4("12.0.0.1", "8.8.8.8")); }

        // Assert
        Assert.Equal(anyFirst, policy.HitsOf(ids[0]));
        Assert.Equal(anySecond, policy.HitsOf(ids[1]));
        Assert.Equal(anyOthers, policy.DefaultHits);
    }

    [Fact]
    public void A_shadowed_rule_never_collects_a_hit() {
        // Arrange
        FirewallPolicy policy = PolicyOf(RuleAction.Deny, strict: false);
        List<RuleId>   ids    = DistinctIds(2);
        policy.Append(Rule(ids[0], RuleAction.Allow, "10.0.0.0/8", "0.0.0.0/0"));
        policy.Append(Rule(ids[1], RuleAction.Deny, "10.1.0.0/16", "0.0.0.0/0"));

        // Act
        policy.Evaluate(Packet4("10.1.1.1", "8.8.8.8"));

        // Assert
        Assert.Equal(1, policy.HitsOf(ids[0]));
        Assert.Equal(0, policy.HitsOf(ids[1]));
    }

    [Fact]
    public void Evaluating_does_not_change_the_revision() {
        // Arrange
        (FirewallPolicy policy, _) = ThreeDisjointRules(strict: false);
        long revision = policy.Revision;

        // Act
        policy.Evaluate(Any.Ipv4Packet().Generate());

        // Assert
        Assert.Equal(revision, policy.Revision);
    }

    [Fact]
    public void ResetCounters_zeroes_every_counter_and_keeps_the_rules() {
        // Arrange
        (FirewallPolicy policy, List<RuleId> ids) = ThreeDisjointRules(strict: false);
        policy.Evaluate(Packet4("10.0.0.1", "8.8.8.8"));
        policy.Evaluate(Packet4("12.0.0.1", "8.8.8.8"));
        policy.Evaluate(Packet4("99.0.0.1", "8.8.8.8"));
        long revision = policy.Revision;

        // Act
        policy.ResetCounters();

        // Assert
        Assert.All(ids, id => Assert.Equal(0, policy.HitsOf(id)));
        Assert.Equal(0, policy.DefaultHits);
        Assert.Equal(3, policy.Count);
        Assert.Equal(revision, policy.Revision);
    }

    [Fact]
    public void A_rule_of_one_family_never_matches_a_packet_of_the_other() {
        // Arrange
        RuleAction     anyDefault = Any.Enum<RuleAction>().Generate();
        FirewallPolicy policy     = PolicyOf(anyDefault, strict: false);
        RuleId         anyId      = Any.RuleId().Generate();
        policy.Append(Rule(anyId, anyDefault == RuleAction.Allow ? RuleAction.Deny : RuleAction.Allow, "0.0.0.0/0", "0.0.0.0/0"));

        // Act
        Verdict v6 = policy.Evaluate(Any.Ipv6Packet().Generate());
        Verdict v4 = policy.Evaluate(Any.Ipv4Packet().Generate());

        // Assert
        Assert.True(v6.IsDefault);
        Assert.Equal(anyId, v4.MatchedRule);
    }

    [Fact]
    public void Evaluate_refuses_null() {
        // Arrange
        FirewallPolicy policy = PolicyOf(RuleAction.Deny, strict: false);

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => policy.Evaluate(null!));
    }

    [Fact]
    public void A_stateless_edge_policy_lets_replies_in_and_new_connections_to_the_web_server_only() {
        // Arrange
        FirewallPolicy policy = PolicyOf(RuleAction.Deny, strict: true);
        List<RuleId>   ids    = DistinctIds(3);
        policy.Append(Rule(ids[0], RuleAction.Allow, "0.0.0.0/0", "203.0.113.0/24", IpProtocols.Tcp, flags: TcpFlagMatch.Established));
        policy.Append(Rule(ids[1], RuleAction.Allow, "0.0.0.0/0", "203.0.113.10/32", IpProtocols.Tcp, new PortRange(443, 443), TcpFlagMatch.NewConnection));
        policy.Append(Rule(ids[2], RuleAction.Allow, "0.0.0.0/0", "203.0.113.10/32", IpProtocols.Icmp));

        Ipv4Address anyClient = Any.UInt32().LessThan(0xE0000000u).As(value => new Ipv4Address(value)).Generate();

        // Act
        Verdict reply   = policy.Evaluate(Any.Ipv4Packet().WithSource(anyClient).WithDestination(Ipv4Address.Parse("203.0.113.77")).WithProtocol(IpProtocols.Tcp).WithTcpFlags(TcpFlags.Ack | TcpFlags.Psh).Generate());
        Verdict https   = policy.Evaluate(Any.Ipv4Packet().WithSource(anyClient).WithDestination(Ipv4Address.Parse("203.0.113.10")).WithProtocol(IpProtocols.Tcp).WithDestinationPort(443).WithTcpFlags(TcpFlags.Syn).Generate());
        Verdict ssh     = policy.Evaluate(Any.Ipv4Packet().WithSource(anyClient).WithDestination(Ipv4Address.Parse("203.0.113.10")).WithProtocol(IpProtocols.Tcp).WithDestinationPort(22).WithTcpFlags(TcpFlags.Syn).Generate());
        Verdict ping    = policy.Evaluate(Any.Ipv4Packet().WithSource(anyClient).WithDestination(Ipv4Address.Parse("203.0.113.10")).WithProtocol(IpProtocols.Icmp).Generate());
        Verdict udp     = policy.Evaluate(Any.Ipv4Packet().WithSource(anyClient).WithDestination(Ipv4Address.Parse("203.0.113.10")).WithProtocol(IpProtocols.Udp).Generate());

        // Assert
        Assert.Equal(new Verdict(RuleAction.Allow, ids[0]), reply);
        Assert.Equal(new Verdict(RuleAction.Allow, ids[1]), https);
        Assert.True(ssh.IsDefault);
        Assert.Equal(new Verdict(RuleAction.Allow, ids[2]), ping);
        Assert.True(udp.IsDefault);
        Assert.False(ssh.IsAllowed);
    }

    [Fact]
    public void An_ipv6_edge_policy_admits_neighbour_discovery_and_https_to_one_prefix() {
        // Arrange
        FirewallPolicy policy = PolicyOf(RuleAction.Deny, strict: true);
        List<RuleId>   ids    = DistinctIds(2);
        policy.Append(new FirewallRule(ids[0], RuleAction.Allow, P("::/0"), P("::/0"), IpProtocols.Icmpv6, PortRange.All, PortRange.All, null));
        policy.Append(new FirewallRule(ids[1], RuleAction.Allow, P("::/0"), P("2001:db8:100::/48"), IpProtocols.Tcp, PortRange.All, PortRange.Single(443), null));

        Ipv6Address anyClient = Any.UInt128().LessThan((UInt128)0xFF << 120).As(value => new Ipv6Address(value)).Generate();
        Ipv6Address server    = Ipv6Address.Parse("2001:db8:100::80");

        // Act
        Verdict ping  = policy.Evaluate(Any.Ipv6Packet().WithSource(anyClient).WithProtocol(IpProtocols.Icmpv6).Generate());
        Verdict https = policy.Evaluate(Any.Ipv6Packet().WithSource(anyClient).WithDestination(server).WithProtocol(IpProtocols.Tcp).WithDestinationPort(443).Generate());
        Verdict http  = policy.Evaluate(Any.Ipv6Packet().WithSource(anyClient).WithDestination(server).WithProtocol(IpProtocols.Tcp).WithDestinationPort(80).Generate());
        Verdict v4    = policy.Evaluate(Any.Ipv4Packet().WithProtocol(IpProtocols.Tcp).WithDestinationPort(443).Generate());

        // Assert
        Assert.Equal(new Verdict(RuleAction.Allow, ids[0]), ping);
        Assert.Equal(new Verdict(RuleAction.Allow, ids[1]), https);
        Assert.True(http.IsDefault);
        Assert.True(v4.IsDefault);
    }

}
