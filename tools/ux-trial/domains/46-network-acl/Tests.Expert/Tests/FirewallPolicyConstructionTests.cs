using JustDummies;

using NetworkAcl.Domain;

using Xunit;

namespace NetworkAcl.Tests.Expert.Tests;

public sealed class FirewallPolicyConstructionTests {

    [Fact]
    public void A_new_policy_exposes_its_settings_and_is_empty() {
        // Act
        FirewallPolicy policy = new FirewallPolicy("edge", RuleAction.Deny, 100, true);

        // Assert
        Assert.Equal("edge", policy.Name);
        Assert.Equal(RuleAction.Deny, policy.DefaultAction);
        Assert.Equal(100, policy.Capacity);
        Assert.True(policy.IsStrict);
        Assert.Equal(0, policy.Count);
        Assert.Empty(policy.Rules);
        Assert.Equal(0L, policy.Revision);
        Assert.Equal(0L, policy.DefaultHits);
    }

    [Fact]
    public void A_policy_may_be_lenient() {
        Assert.False(new FirewallPolicy("lab", RuleAction.Allow, 1, false).IsStrict);
    }

    [Fact]
    public void A_missing_name_is_refused() {
        // Arrange
        int anyCapacity = Any.Int32().Between(1, FirewallPolicy.MaxCapacity).Generate();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new FirewallPolicy(null!, RuleAction.Allow, anyCapacity, Any.Boolean().Generate()));
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("\t\r\n")]
    public void A_blank_name_is_refused(string name) {
        // Arrange
        int anyCapacity = Any.Int32().Between(1, FirewallPolicy.MaxCapacity).Generate();

        // Act & Assert: an empty or blank name is an argument error, not a missing argument
        Assert.Throws<ArgumentException>(() => new FirewallPolicy(name, RuleAction.Allow, anyCapacity, Any.Boolean().Generate()));
    }

    [Fact]
    public void A_name_of_64_characters_is_accepted() {
        // Arrange: only the length matters, not what the characters are.
        string anySixtyFourCharacters = Any.String().NotBlank().WithLength(FirewallPolicy.MaxNameLength).Generate();

        // Act
        FirewallPolicy policy = new FirewallPolicy(anySixtyFourCharacters, RuleAction.Allow, 10, false);

        // Assert
        Assert.Equal(64, policy.Name.Length);
    }

    [Fact]
    public void A_name_of_65_characters_is_refused() {
        // Arrange
        string anySixtyFiveCharacters = Any.String().NotBlank().WithLength(FirewallPolicy.MaxNameLength + 1).Generate();

        // Act & Assert
        Assert.Throws<ArgumentException>(() => new FirewallPolicy(anySixtyFiveCharacters, RuleAction.Allow, 10, false));
    }

    [Theory]
    [InlineData(1)]
    [InlineData(10_000)]
    public void A_capacity_from_1_to_10000_is_accepted(int capacity) {
        Assert.Equal(capacity, new FirewallPolicy("edge", RuleAction.Allow, capacity, false).Capacity);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(10_001)]
    [InlineData(int.MinValue)]
    [InlineData(int.MaxValue)]
    public void A_capacity_outside_1_to_10000_is_refused(int capacity) {
        Assert.Throws<ArgumentOutOfRangeException>(() => new FirewallPolicy("edge", RuleAction.Allow, capacity, false));
    }

    [Fact]
    public void An_undefined_default_action_is_refused() {
        // Arrange
        int anyCapacity = Any.Int32().Between(1, FirewallPolicy.MaxCapacity).Generate();

        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => new FirewallPolicy("edge", (RuleAction)4, anyCapacity, false));
    }

    [Fact]
    public void The_name_is_checked_before_the_default_action_before_the_capacity() {
        // Act & Assert: each call breaks the rule named and the ones after it
        Assert.Throws<ArgumentException>(() => new FirewallPolicy(" ", (RuleAction)4, 0, false));
        ArgumentOutOfRangeException action = Assert.Throws<ArgumentOutOfRangeException>(() => new FirewallPolicy("edge", (RuleAction)4, 0, false));
        Assert.Equal("defaultAction", action.ParamName);
    }

}
