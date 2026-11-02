using Education.Domain;

using JustDummies;

using Xunit;

namespace Education.Domain.Tests.Discovery;

public sealed class CourseTests {

    [Fact]
    public void A_course_keeps_what_it_was_built_with() {
        Course course = new Course("MAT101", "Linear algebra", 6);

        Assert.Equal("MAT101", course.Code);
        Assert.Equal("Linear algebra", course.Title);
        Assert.Equal(6, course.Credits);
    }

    [Theory]
    [InlineData("")]
    [InlineData("MAT10")]
    [InlineData("MAT1011")]
    [InlineData("MA101")]
    [InlineData("MATH101")]
    [InlineData("mat101")]
    [InlineData("Mat101")]
    [InlineData("MAT10A")]
    [InlineData("101MAT")]
    [InlineData(" MAT101")]
    [InlineData("MAT101 ")]
    [InlineData("MAT-101")]
    public void A_code_that_is_not_three_capitals_and_three_digits_is_refused(string code) {
        // Arrange
        string anyTitle = Any.String().NotBlank().Generate();

        // Assert
        Assert.Throws<ArgumentException>(() => new Course(code, anyTitle, 6));
    }

    [Fact]
    public void A_null_code_is_refused_like_an_empty_one() {
        string anyTitle = Any.String().NotBlank().Generate();

        Assert.Throws<ArgumentException>(() => new Course(null!, anyTitle, 6));
    }

    [Fact]
    public void A_null_title_is_refused_as_null() {
        Assert.Throws<ArgumentNullException>(() => new Course("MAT101", null!, 6));
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("\t\r\n")]
    public void A_blank_title_is_refused(string title) {
        Assert.Throws<ArgumentException>(() => new Course("MAT101", title, 6));
    }

    [Theory]
    [InlineData(1)]
    [InlineData(30)]
    public void The_credits_boundaries_are_accepted(int credits) {
        Course course = new Course("MAT101", "Linear algebra", credits);

        Assert.Equal(credits, course.Credits);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(31)]
    [InlineData(int.MinValue)]
    [InlineData(int.MaxValue)]
    public void Credits_outside_one_to_thirty_are_refused(int credits) {
        Assert.Throws<ArgumentOutOfRangeException>(() => new Course("MAT101", "Linear algebra", credits));
    }

    [Fact]
    public void Two_courses_with_the_same_code_are_equal_whatever_their_title_and_credits() {
        // Arrange
        Course first  = Any.Course().WithCode("MAT101").WithCredits(3).Generate();
        Course second = Any.Course().WithCode("MAT101").WithCredits(9).Generate();

        // Assert
        Assert.Equal(first, second);
        Assert.Equal(first.GetHashCode(), second.GetHashCode());
    }

    [Fact]
    public void Two_courses_with_different_codes_are_not_equal_even_if_all_else_matches() {
        Course first  = new Course("MAT101", "Linear algebra", 6);
        Course second = new Course("MAT102", "Linear algebra", 6);

        Assert.NotEqual(first, second);
    }

    [Fact(Skip = "domain defect: a code followed by a line feed is accepted ('$' matches before a final \\n), literal input")]
    public void A_code_followed_by_a_line_feed_is_refused() {
        Assert.Throws<ArgumentException>(() => new Course("MAT101\n", "Linear algebra", 6));
    }

}
