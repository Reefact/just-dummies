namespace Payroll.Tests.Expert;

public sealed class EmployeeIdTests {

    [Fact]
    public void An_employee_id_keeps_the_text_it_was_built_from() {
        // Act
        EmployeeId employeeId = new EmployeeId("EMP-004217");

        // Assert
        Assert.Equal("EMP-004217", employeeId.Value);
        Assert.Equal("EMP-004217", employeeId.ToString());
    }

    [Fact]
    public void An_employee_id_is_refused_when_null() {
        // Act
        ArgumentNullException refusal = Assert.Throws<ArgumentNullException>(() => new EmployeeId(null!));

        // Assert
        Assert.Equal("value", refusal.ParamName);
    }

    [Theory]
    [InlineData("")]
    [InlineData("EMP-12345")]      // one digit short
    [InlineData("EMP-1234567")]    // one digit too many
    [InlineData("EMP-12345A")]     // a letter among the digits
    [InlineData("emp-123456")]     // the prefix is upper case
    [InlineData("EMP123456")]      // no hyphen
    [InlineData(" EMP-123456")]    // leading space
    [InlineData("EMP-123456 ")]    // trailing space
    [InlineData("XEMP-123456")]    // something before the prefix
    public void An_employee_id_is_refused_when_it_does_not_follow_the_EMP_nnnnnn_format(string malformed) {
        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => new EmployeeId(malformed));

        // Assert
        Assert.Equal("value", refusal.ParamName);
    }

    [Fact]
    public void Two_employee_ids_built_from_the_same_text_are_equal_and_hash_alike() {
        // Arrange
        string     anyText = Any.EmployeeId().Generate().Value;
        EmployeeId first   = new EmployeeId(anyText);
        EmployeeId second  = new EmployeeId(anyText);

        // Act & Assert
        Assert.Equal(first, second);
        Assert.Equal(first.GetHashCode(), second.GetHashCode());
    }

    [Fact]
    public void Two_employee_ids_built_from_different_texts_are_not_equal() {
        // Arrange
        EmployeeId anyEmployeeId      = Any.EmployeeId().Generate();
        EmployeeId anyOtherEmployeeId = Any.EmployeeId().WithValue(v => v.DifferentFrom(anyEmployeeId.Value)).Generate();

        // Act & Assert
        Assert.NotEqual(anyEmployeeId, anyOtherEmployeeId);
    }

    [Fact]
    public void An_employee_id_is_not_equal_to_something_that_is_not_an_employee_id() {
        // Arrange
        EmployeeId anyEmployeeId = Any.EmployeeId().Generate();

        // Act & Assert
        Assert.False(anyEmployeeId.Equals(anyEmployeeId.Value));
        Assert.False(anyEmployeeId.Equals(null));
    }

    // The model states that the pattern is applied as written: \d is not restricted to ASCII digits.
    [Fact]
    public void An_employee_id_accepts_non_ASCII_digits_because_the_pattern_is_applied_as_written() {
        // Arrange: Arabic-Indic digits one to six
        string arabicIndicDigits = "EMP-١٢٣٤٥٦";

        // Act
        EmployeeId employeeId = new EmployeeId(arabicIndicDigits);

        // Assert
        Assert.Equal(arabicIndicDigits, employeeId.Value);
    }

    // The model states that $ also matches before a final line feed.
    [Fact]
    public void An_employee_id_accepts_a_final_line_feed_because_the_pattern_is_applied_as_written() {
        // Act
        EmployeeId employeeId = new EmployeeId("EMP-123456\n");

        // Assert
        Assert.Equal("EMP-123456\n", employeeId.Value);
    }

}
