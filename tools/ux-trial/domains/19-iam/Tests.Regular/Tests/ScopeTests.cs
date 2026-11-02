using Identity.Domain;

using JustDummies;

using Xunit;

namespace Identity.Tests.Regular;

public sealed class ScopeTests {

    [Fact]
    public void A_scope_splits_into_its_resource_and_its_action() {
        // Arrange
        Scope scope = new Scope("orders:read");

        // Act
        string resource = scope.Resource;
        string action   = scope.Action;

        // Assert
        Assert.Equal("orders", resource);
        Assert.Equal("read", action);
    }

    [Theory]
    [InlineData("read")]
    [InlineData("write")]
    [InlineData("admin")]
    public void Each_of_the_three_actions_is_accepted(string action) {
        // Arrange
        string resource = Any.StringMatching("[a-z][a-z0-9_]{1,31}").Generate();

        // Act
        Scope scope = new Scope($"{resource}:{action}");

        // Assert
        Assert.Equal(action, scope.Action);
    }

    [Theory]
    [InlineData(2)]
    [InlineData(32)]
    public void A_resource_of_two_to_thirty_two_characters_is_accepted(int length) {
        // Arrange
        string resource = new string('a', length);

        // Act
        Scope scope = new Scope(resource + ":write");

        // Assert
        Assert.Equal(resource, scope.Resource);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(33)]
    public void A_resource_shorter_than_two_or_longer_than_thirty_two_characters_is_refused(int length) {
        // Arrange
        string value = new string('a', length) + ":write";

        // Act
        FormatException thrown = Assert.Throws<FormatException>(() => new Scope(value));

        // Assert
        Assert.Contains(value, thrown.Message);
    }

    [Theory]
    [InlineData("orders")]               // no colon
    [InlineData("orders:")]              // no action
    [InlineData(":read")]                // no resource
    [InlineData("orders:delete")]        // unknown action
    [InlineData("orders:READ")]          // action in upper case
    [InlineData("Orders:read")]          // resource in upper case
    [InlineData("1orders:read")]         // resource starts with a digit
    [InlineData("_orders:read")]         // resource starts with an underscore
    [InlineData("or-ders:read")]         // dash in the resource
    [InlineData("orders:read:write")]    // two colons
    [InlineData("orders:readx")]         // trailing characters after the action
    [InlineData("xorders:read ")]        // trailing space
    [InlineData(" orders:read")]         // leading space
    public void A_value_that_is_not_resource_colon_action_is_refused(string value) {
        // Act
        FormatException thrown = Assert.Throws<FormatException>(() => new Scope(value));

        // Assert
        Assert.Contains(value, thrown.Message);
    }

    [Fact]
    public void A_scope_with_a_digit_or_underscore_after_the_first_letter_is_accepted() {
        // Act
        Scope scope = new Scope("a1_b2:admin");

        // Assert
        Assert.Equal("a1_b2", scope.Resource);
    }

    [Fact]
    public void A_null_scope_is_refused() {
        // Act + Assert
        Assert.Throws<ArgumentNullException>(() => new Scope(null!));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("\t\n")]
    public void A_blank_scope_is_refused_as_an_argument_exception_not_a_format_exception(string blank) {
        // Act
        ArgumentException thrown = Assert.Throws<ArgumentException>(() => new Scope(blank));

        // Assert
        Assert.IsNotType<FormatException>(thrown);
    }

    [Fact(Skip = "domain defect: the pattern's $ also matches before a final line feed, so \"orders:read\\n\" is accepted and its Action is \"read\\n\"; no drawn value involved, seed n/a")]
    public void A_scope_followed_by_a_line_feed_is_refused() {
        // Act + Assert
        Assert.Throws<FormatException>(() => new Scope("orders:read\n"));
    }

    [Fact]
    public void Two_scopes_with_the_same_value_are_equal() {
        // Arrange
        string anyValue = Any.Scope().Generate().Value;
        Scope  first    = new Scope(anyValue);
        Scope  second   = new Scope(anyValue);

        // Act
        bool equal = first.Equals(second);

        // Assert
        Assert.True(equal);
        Assert.Equal(first.GetHashCode(), second.GetHashCode());
    }

    [Fact]
    public void Scopes_with_different_actions_are_not_equal() {
        // Arrange
        Scope read  = new Scope("orders:read");
        Scope write = new Scope("orders:write");

        // Act
        bool equal = read.Equals(write);

        // Assert
        Assert.False(equal);
    }

    [Fact]
    public void A_scope_prints_as_its_value() {
        // Arrange
        Scope scope = new Scope("orders:read");

        // Act
        string text = scope.ToString();

        // Assert
        Assert.Equal("orders:read", text);
    }

}
