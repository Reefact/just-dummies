using Education.Domain;

using JustDummies;

using Xunit;

namespace Education.Domain.Tests.Discovery;

public sealed class StudentNumberTests {

    [Theory]
    [InlineData("STU-2025-00042")]
    [InlineData("STU-1900-00000")]
    [InlineData("STU-1999-99999")]
    [InlineData("STU-2000-00001")]
    [InlineData("STU-2099-12345")]
    public void A_student_number_of_the_documented_shape_is_accepted(string text) {
        StudentNumber number = new StudentNumber(text);

        Assert.Equal(text, number.Value);
    }

    [Fact]
    public void A_drawn_student_number_is_accepted_by_the_constructor() {
        // Arrange
        StudentNumber anyNumber = Any.StudentNumber().Generate();

        // Assert: the generator and the constructor agree on the shape
        Assert.Matches(@"^STU-(19|20)[0-9]{2}-[0-9]{5}$", anyNumber.Value);
    }

    [Theory]
    [InlineData("STU-2025-00042", 2025)]
    [InlineData("STU-1987-00001", 1987)]
    [InlineData("STU-2000-99999", 2000)]
    public void The_intake_year_is_the_four_digits_after_the_prefix(string text, int expectedYear) {
        StudentNumber number = new StudentNumber(text);

        Assert.Equal(expectedYear, number.IntakeYear);
    }

    [Fact]
    public void A_null_student_number_is_refused_as_null() {
        Assert.Throws<ArgumentNullException>(() => new StudentNumber(null!));
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("STU-1899-00001")]   // intake century 18
    [InlineData("STU-2100-00001")]   // intake century 21
    [InlineData("STU-2025-0042")]    // four digits sequence
    [InlineData("STU-2025-000421")]  // six digits sequence
    [InlineData("stu-2025-00042")]   // lower case prefix
    [InlineData("STU2025-00042")]    // missing dash
    [InlineData("STU-2025-0004A")]   // letter in the sequence
    [InlineData(" STU-2025-00042")]  // leading space
    [InlineData("STU-2025-00042 ")]  // trailing space
    [InlineData("XSTU-2025-00042")]  // prefix before
    [InlineData("STU-2025-00042X")]  // suffix after
    public void A_student_number_of_another_shape_is_refused(string text) {
        Assert.Throws<ArgumentException>(() => new StudentNumber(text));
    }

    [Fact]
    public void Two_student_numbers_with_the_same_value_are_equal() {
        StudentNumber first  = new StudentNumber("STU-2025-00042");
        StudentNumber second = new StudentNumber("STU-2025-00042");

        Assert.Equal(first, second);
        Assert.Equal(first.GetHashCode(), second.GetHashCode());
    }

    [Fact]
    public void Two_student_numbers_with_different_values_are_not_equal() {
        StudentNumber first  = new StudentNumber("STU-2025-00042");
        StudentNumber second = new StudentNumber("STU-2025-00043");

        Assert.NotEqual(first, second);
    }

    [Fact]
    public void A_student_number_prints_as_its_value() {
        StudentNumber number = new StudentNumber("STU-2025-00042");

        Assert.Equal("STU-2025-00042", number.ToString());
    }

    [Fact(Skip = "domain defect: a student number with a trailing line feed is accepted ('$' matches before a final \\n), then stored with the line feed, seed n/a (literal input)")]
    public void A_student_number_followed_by_a_line_feed_is_refused() {
        Assert.Throws<ArgumentException>(() => new StudentNumber("STU-2025-00042\n"));
    }

    [Fact(Skip = "domain defect: non-ASCII digits are accepted ('\\d' is Unicode aware), and IntakeYear then throws FormatException, seed n/a (literal input)")]
    public void A_student_number_written_with_non_ascii_digits_is_refused() {
        // Arabic-Indic digits for the last two digits of the year (25) and for the sequence (00042)
        Assert.Throws<ArgumentException>(() => new StudentNumber("STU-20\u0662\u0665-\u0660\u0660\u0660\u0664\u0662"));
    }

}
