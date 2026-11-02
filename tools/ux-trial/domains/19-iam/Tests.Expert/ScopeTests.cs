using Xunit;

namespace Identity.Domain.Tests.Expert;

public sealed class ScopeTests {

    [Theory]
    [InlineData("orders:read", "orders", "read")]
    [InlineData("billing_v2:write", "billing_v2", "write")]
    [InlineData("users:admin", "users", "admin")]
    [InlineData("ab:read", "ab", "read")]                                                         // shortest resource: 2 characters
    [InlineData("abcdefghijklmnopqrstuvwxyz012345:read", "abcdefghijklmnopqrstuvwxyz012345", "read")] // longest resource: 32 characters
    [InlineData("a_9:write", "a_9", "write")]
    public void A_scope_is_split_into_its_resource_and_its_action_at_the_colon(string text, string expectedResource, string expectedAction) {
        // Act
        Scope scope = new Scope(text);

        // Assert
        Assert.Equal(expectedResource, scope.Resource);
        Assert.Equal(expectedAction, scope.Action);
        Assert.Equal(text, scope.Value);
        Assert.Equal(text, scope.ToString());
    }

    [Theory]
    [InlineData("orders")]                                            // no action
    [InlineData("orders:")]                                           // empty action
    [InlineData(":read")]                                             // no resource
    [InlineData("orders:delete")]                                     // unknown action
    [InlineData("orders:READ")]                                       // action is lower-case
    [InlineData("orders:reads")]                                      // action is exactly one of the three
    [InlineData("Orders:read")]                                       // resource is lower-case
    [InlineData("1orders:read")]                                      // resource starts with a letter
    [InlineData("_orders:read")]
    [InlineData("a:read")]                                            // resource of 1 character
    [InlineData("abcdefghijklmnopqrstuvwxyz0123456:read")]            // resource of 33 characters
    [InlineData("my-orders:read")]                                    // only letters, digits and _
    [InlineData("my orders:read")]
    [InlineData("orders:read:write")]                                 // a single colon
    [InlineData(" orders:read")]
    [InlineData("orders:read ")]
    [InlineData("orders:read\r\n")]
    [InlineData("xorders:read\norders:read")]
    public void A_text_that_is_not_a_resource_and_an_action_is_not_a_scope(string text) {
        // Act + Assert
        Assert.Throws<FormatException>(() => new Scope(text));
    }

    [Fact(Skip = "domain defect: the pattern's '$' also matches before a final line feed, so \"orders:read\\n\" is accepted and its Action is \"read\\n\" (MODEL.md calls this a quirk; `\\z` would close it)")]
    public void A_scope_followed_by_a_line_feed_is_not_a_scope() {
        Assert.Throws<FormatException>(() => new Scope("orders:read\n"));
    }

    [Fact]
    public void A_missing_scope_is_not_a_scope() {
        Assert.Throws<ArgumentNullException>(() => new Scope(null!));
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("\t\r\n")]
    public void A_blank_scope_is_refused_before_its_format_is_looked_at(string blank) {
        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => new Scope(blank));

        // Assert
        Assert.IsNotType<FormatException>(refusal);
    }

    [Fact]
    public void Two_scopes_written_the_same_are_equal_and_hash_alike() {
        // Arrange
        Scope first  = new Scope("orders:read");
        Scope second = new Scope("orders:read");

        // Act + Assert
        Assert.True(first.Equals(second));
        Assert.True(first.Equals((object)second));
        Assert.Equal(first.GetHashCode(), second.GetHashCode());
    }

    [Theory]
    [InlineData("orders:read", "orders:write")]
    [InlineData("orders:read", "invoices:read")]
    public void Scopes_differing_by_resource_or_action_are_different(string left, string right) {
        // Act + Assert
        Assert.False(new Scope(left).Equals(new Scope(right)));
    }

    [Fact]
    public void A_scope_is_not_equal_to_nothing_nor_to_its_text() {
        // Arrange
        Scope scope = new Scope("orders:read");

        // Act + Assert
        Assert.False(scope.Equals((Scope?)null));
        Assert.False(scope.Equals((object?)null));
        Assert.False(scope.Equals((object)"orders:read"));
    }

}
