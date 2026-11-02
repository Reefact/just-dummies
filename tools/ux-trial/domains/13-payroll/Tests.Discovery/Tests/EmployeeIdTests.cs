using Payroll.Domain;

using Xunit;

namespace Payroll.Tests.Discovery;

public sealed class EmployeeIdTests {

    [Theory]
    [InlineData("EMP-004217")]
    [InlineData("EMP-000000")]
    [InlineData("EMP-999999")]
    public void A_well_formed_identifier_is_kept_as_written(string text) {
        // Act
        EmployeeId id = new EmployeeId(text);

        // Assert
        Assert.Equal(text, id.Value);
        Assert.Equal(text, id.ToString());
    }

    [Fact]
    public void A_null_identifier_is_refused() {
        // Act
        ArgumentNullException exception = Assert.Throws<ArgumentNullException>(() => new EmployeeId(null!));

        // Assert
        Assert.Equal("value", exception.ParamName);
    }

    [Theory]
    [InlineData("")]
    [InlineData("EMP-12345")]
    [InlineData("EMP-1234567")]
    [InlineData("emp-123456")]
    [InlineData("EMP123456")]
    [InlineData("EMP-12345A")]
    [InlineData(" EMP-123456")]
    [InlineData("EMP-123456 ")]
    [InlineData("XEMP-123456")]
    [InlineData("EMP-123456\n\n")]
    public void A_malformed_identifier_is_refused(string text) {
        // Act
        ArgumentException exception = Assert.Throws<ArgumentException>(() => new EmployeeId(text));

        // Assert
        Assert.Equal("value", exception.ParamName);
    }

    // The model documents both of these as the behaviour of the pattern "as written".
    [Fact]
    public void A_final_line_feed_is_tolerated_because_dollar_matches_before_it() {
        // Act
        EmployeeId id = new EmployeeId("EMP-123456\n");

        // Assert
        Assert.Equal("EMP-123456\n", id.Value);
    }

    [Fact]
    public void Digits_of_other_scripts_are_accepted_because_backslash_d_is_not_ascii_only() {
        // Arabic-Indic digits
        string text = "EMP-٠٠٠٠٠١";

        // Act
        EmployeeId id = new EmployeeId(text);

        // Assert
        Assert.Equal(text, id.Value);
    }

    [Fact]
    public void Two_identifiers_with_the_same_text_are_equal() {
        // Arrange
        EmployeeId first  = new EmployeeId("EMP-004217");
        EmployeeId second = new EmployeeId("EMP-004217");

        // Assert
        Assert.True(first.Equals(second));
        Assert.Equal(first.GetHashCode(), second.GetHashCode());
    }

    [Fact]
    public void Two_identifiers_with_different_texts_are_not_equal() {
        // Arrange
        EmployeeId first  = new EmployeeId("EMP-004217");
        EmployeeId second = new EmployeeId("EMP-004218");

        // Assert
        Assert.False(first.Equals(second));
    }

    [Fact]
    public void An_identifier_is_not_equal_to_something_that_is_not_an_identifier() {
        // Arrange
        EmployeeId id = new EmployeeId("EMP-004217");

        // Assert
        Assert.False(id.Equals("EMP-004217"));
        Assert.False(id.Equals(null));
    }

}
