using JustDummies;

using Payroll.Domain;

using Xunit;

namespace Payroll.Tests.Regular;

public sealed class EmployeeIdTests {

    [Fact]
    public void An_identifier_keeps_the_value_it_was_built_from() {
        // Act
        EmployeeId employeeId = new EmployeeId("EMP-004217");

        // Assert
        Assert.Equal("EMP-004217", employeeId.Value);
        Assert.Equal("EMP-004217", employeeId.ToString());
    }

    [Fact]
    public void Two_identifiers_built_from_the_same_text_are_equal() {
        // Arrange
        string anyText = Any.StringMatching("EMP-[0-9]{6}").Generate();

        // Act
        EmployeeId first  = new EmployeeId(anyText);
        EmployeeId second = new EmployeeId(anyText);

        // Assert
        Assert.Equal(first, second);
        Assert.Equal(first.GetHashCode(), second.GetHashCode());
    }

    [Fact]
    public void Two_identifiers_built_from_different_texts_are_not_equal() {
        // Act
        EmployeeId first  = new EmployeeId("EMP-000001");
        EmployeeId second = new EmployeeId("EMP-000002");

        // Assert
        Assert.NotEqual(first, second);
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
    [InlineData("EMP-12345A")]
    [InlineData("emp-123456")]
    [InlineData("EMP123456")]
    [InlineData(" EMP-123456")]
    [InlineData("EMP-123456 ")]
    [InlineData("XEMP-123456")]
    public void A_text_that_is_not_an_identifier_is_refused(string text) {
        // Act
        ArgumentException exception = Assert.Throws<ArgumentException>(() => new EmployeeId(text));

        // Assert
        Assert.Equal("value", exception.ParamName);
    }

    [Fact]
    public void A_text_of_letters_only_is_refused() {
        // Arrange
        string anyText = Any.String().Alpha().WithLengthBetween(1, 30).Generate();

        // Act & Assert
        Assert.Throws<ArgumentException>(() => new EmployeeId(anyText));
    }

    [Fact]
    public void A_final_line_feed_is_tolerated_because_the_pattern_is_matched_as_written() {
        // Act
        EmployeeId employeeId = new EmployeeId("EMP-123456\n");

        // Assert
        Assert.Equal("EMP-123456\n", employeeId.Value);
    }

}
