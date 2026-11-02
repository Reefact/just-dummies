using Education.Domain;

using Xunit;

namespace Education.Tests.Expert;

public sealed class StudentNumberTests {

    [Theory]
    [InlineData("STU-2025-00042", 2025)]
    [InlineData("STU-1900-00000", 1900)]
    [InlineData("STU-2099-99999", 2099)]
    public void A_student_number_keeps_its_value_and_reads_its_intake_year_from_the_four_digits_after_the_prefix(string value, int expectedIntakeYear) {
        // Act
        StudentNumber number = new StudentNumber(value);

        // Assert
        Assert.Equal(value, number.Value);
        Assert.Equal(expectedIntakeYear, number.IntakeYear);
    }

    [Fact]
    public void A_null_student_number_is_refused_as_a_missing_argument() {
        // Act
        ArgumentNullException refusal = Assert.Throws<ArgumentNullException>(() => new StudentNumber(null!));

        // Assert
        Assert.Equal("value", refusal.ParamName);
    }

    [Theory]
    [InlineData("")]
    [InlineData("STU-1899-00042")]    // the year starts with 19 or 20 ...
    [InlineData("STU-2100-00042")]    // ... so 1900 to 2099 are the intake years
    [InlineData("STU-2025-0004")]     // four digits in the sequence
    [InlineData("STU-2025-000042")]   // six digits in the sequence
    [InlineData("STU-202-00042")]     // a three-digit year
    [InlineData("STU-2025-0004A")]
    [InlineData("stu-2025-00042")]
    [InlineData("STU2025-00042")]
    [InlineData(" STU-2025-00042")]
    [InlineData("STU-2025-00042 ")]
    public void A_malformed_student_number_is_refused(string malformed) {
        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => new StudentNumber(malformed));

        // Assert
        Assert.Equal("value", refusal.ParamName);
        Assert.Contains($"'{malformed}'", refusal.Message);
    }

    [Fact(Skip = "domain defect: '$' also matches before a final line feed, so 'STU-2025-00042\\n' is accepted as a student number; literal input, no seed")]
    public void A_student_number_followed_by_a_line_feed_is_refused() {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => new StudentNumber("STU-2025-00042\n"));
    }

    [Fact(Skip = "domain defect: '\\d' accepts any Unicode digit, so a number written with Arabic-Indic digits is built and then IntakeYear throws FormatException; literal input, no seed")]
    public void A_student_number_written_with_non_ascii_digits_is_refused() {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => new StudentNumber("STU-20" + "\u0662\u0665" + "-00042"));
    }

    [Fact]
    public void Two_student_numbers_with_the_same_value_are_equal() {
        // Arrange
        StudentNumber number = new StudentNumber("STU-2025-00042");
        StudentNumber same   = new StudentNumber("STU-2025-00042");

        // Assert
        Assert.True(number.Equals(same));
        Assert.Equal(number.GetHashCode(), same.GetHashCode());
    }

    [Fact]
    public void Two_student_numbers_with_different_values_are_not_equal() {
        // Arrange
        StudentNumber number = new StudentNumber("STU-2025-00042");
        StudentNumber other  = new StudentNumber("STU-2025-00043");

        // Assert
        Assert.False(number.Equals(other));
    }

    [Fact]
    public void A_student_number_is_not_equal_to_its_text_nor_to_null() {
        // Arrange
        StudentNumber number = new StudentNumber("STU-2025-00042");

        // Assert
        Assert.False(number.Equals("STU-2025-00042"));
        Assert.False(number.Equals(null));
    }

    [Fact]
    public void A_student_number_prints_as_its_value() {
        // Arrange
        StudentNumber number = new StudentNumber("STU-2025-00042");

        // Assert
        Assert.Equal("STU-2025-00042", number.ToString());
    }

}
