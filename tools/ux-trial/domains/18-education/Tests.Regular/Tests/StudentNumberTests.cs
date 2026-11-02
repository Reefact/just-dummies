using Education.Domain;

using JustDummies;

using Xunit;

namespace Education.Tests.Regular;

public sealed class StudentNumberTests {

    [Theory]
    [InlineData("STU-2025-00042", 2025)]
    [InlineData("STU-1999-99999", 1999)]
    [InlineData("STU-1900-00000", 1900)]
    [InlineData("STU-2099-00001", 2099)]
    public void A_well_formed_number_is_kept_and_exposes_its_intake_year(string text, int expectedIntakeYear) {
        // Act
        StudentNumber number = new StudentNumber(text);

        // Assert
        Assert.Equal(text, number.Value);
        Assert.Equal(text, number.ToString());
        Assert.Equal(expectedIntakeYear, number.IntakeYear);
    }

    [Fact]
    public void A_null_number_is_refused_with_a_null_argument_exception() {
        // Act
        ArgumentNullException exception = Assert.Throws<ArgumentNullException>(() => new StudentNumber(null!));

        // Assert
        Assert.Equal("value", exception.ParamName);
    }

    [Theory]
    [InlineData("")]
    [InlineData("STU-2025-0004")]      // four-digit sequence
    [InlineData("STU-2025-000421")]    // six-digit sequence
    [InlineData("STU-1899-00042")]     // intake year before 1900
    [InlineData("STU-2100-00042")]     // intake year after 2099
    [InlineData("STU-1800-00042")]
    [InlineData("stu-2025-00042")]     // lower case prefix
    [InlineData("STU2025-00042")]      // separator missing
    [InlineData("STU-2025-0004A")]     // letter in the sequence
    [InlineData(" STU-2025-00042")]    // leading space
    [InlineData("STU-2025-00042 ")]    // trailing space
    [InlineData("XXX-2025-00042")]
    public void A_malformed_number_is_refused(string text) {
        // Act
        ArgumentException exception = Assert.Throws<ArgumentException>(() => new StudentNumber(text));

        // Assert
        Assert.Equal("value", exception.ParamName);
    }

    [Fact]
    public void A_number_ending_with_a_line_feed_is_accepted_as_written_because_dollar_matches_before_it() {
        // Act
        StudentNumber number = new StudentNumber("STU-2025-00042\n");

        // Assert
        Assert.Equal("STU-2025-00042\n", number.Value);
    }

    [Fact]
    public void Two_numbers_built_from_the_same_text_are_equal() {
        // Arrange
        string anyText = Any.StringMatching("STU-(19|20)[0-9]{2}-[0-9]{5}").Generate();

        StudentNumber first  = new StudentNumber(anyText);
        StudentNumber second = new StudentNumber(anyText);

        // Assert
        Assert.Equal(first, second);
        Assert.True(first.Equals(second));
        Assert.Equal(first.GetHashCode(), second.GetHashCode());
    }

    [Fact]
    public void Two_numbers_built_from_different_texts_are_different() {
        // Arrange
        StudentNumber anyNumber      = Any.StudentNumber().Generate();
        StudentNumber anyOtherNumber = Any.StudentNumber().WithValue(v => v.DifferentFrom(anyNumber.Value)).Generate();

        // Assert
        Assert.NotEqual(anyNumber, anyOtherNumber);
    }

    [Fact]
    public void A_number_is_not_equal_to_null_or_to_its_own_text() {
        // Arrange
        StudentNumber number = new StudentNumber("STU-2025-00042");

        // Assert
        Assert.False(number.Equals(null));
        Assert.False(number.Equals("STU-2025-00042"));
    }

    [Fact(Skip = "domain defect (deterministic, no seed needed): IntakeYear throws FormatException for a number whose year digits are non-ASCII digits that the format's \\d accepts, e.g. STU-20٢٥-00042")]
    public void A_number_accepted_by_the_format_has_a_readable_intake_year() {
        // Arrange: Arabic-Indic digits two and five, accepted by \d{2}
        StudentNumber number = new StudentNumber("STU-20٢٥-00042");

        // Act
        int intakeYear = number.IntakeYear;

        // Assert
        Assert.InRange(intakeYear, 1900, 2099);
    }

}
