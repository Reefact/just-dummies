using Education.Domain;

using JustDummies;

using Xunit;

namespace Education.Tests.Regular;

public sealed class CourseTests {

    [Fact]
    public void A_valid_course_exposes_what_it_was_built_with() {
        // Act
        Course course = new Course("MAT101", "Linear algebra", 6);

        // Assert
        Assert.Equal("MAT101", course.Code);
        Assert.Equal("Linear algebra", course.Title);
        Assert.Equal(6, course.Credits);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(30)]
    public void The_credit_bounds_are_inclusive(int credits) {
        // Arrange
        string anyTitle = Any.String().NotBlank().Generate();

        // Act
        Course course = new Course("MAT101", anyTitle, credits);

        // Assert
        Assert.Equal(credits, course.Credits);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(31)]
    [InlineData(int.MinValue)]
    [InlineData(int.MaxValue)]
    public void Credits_outside_one_to_thirty_are_refused(int credits) {
        // Arrange
        string anyTitle = Any.String().NotBlank().Generate();

        // Act
        ArgumentOutOfRangeException exception = Assert.Throws<ArgumentOutOfRangeException>(() => new Course("MAT101", anyTitle, credits));

        // Assert
        Assert.Equal("credits", exception.ParamName);
    }

    [Theory]
    [InlineData("")]
    [InlineData("MAT10")]
    [InlineData("MAT1011")]
    [InlineData("MA101")]
    [InlineData("MATH01")]
    [InlineData("mat101")]
    [InlineData("MAT10A")]
    [InlineData("MAT 101")]
    [InlineData(" MAT101")]
    public void A_malformed_code_is_refused(string code) {
        // Arrange
        string anyTitle = Any.String().NotBlank().Generate();

        // Act
        ArgumentException exception = Assert.Throws<ArgumentException>(() => new Course(code, anyTitle, 6));

        // Assert
        Assert.Equal("code", exception.ParamName);
    }

    [Fact]
    public void A_code_followed_by_a_final_line_feed_is_accepted_as_written_because_dollar_matches_before_it() {
        // Arrange
        string anyTitle = Any.String().NotBlank().Generate();

        // Act
        Course course = new Course("MAT101\n", anyTitle, 6);

        // Assert
        Assert.Equal("MAT101\n", course.Code);
    }

    [Fact]
    public void A_null_code_is_refused_like_an_empty_one() {
        // Arrange
        string anyTitle = Any.String().NotBlank().Generate();

        // Act
        ArgumentException exception = Assert.Throws<ArgumentException>(() => new Course(null!, anyTitle, 6));

        // Assert
        Assert.IsNotType<ArgumentNullException>(exception);
        Assert.Equal("code", exception.ParamName);
    }

    [Fact]
    public void A_null_title_is_refused_with_a_null_argument_exception() {
        // Act
        ArgumentNullException exception = Assert.Throws<ArgumentNullException>(() => new Course("MAT101", null!, 6));

        // Assert
        Assert.Equal("title", exception.ParamName);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("\t \n")]
    public void A_blank_title_is_refused(string title) {
        // Act
        ArgumentException exception = Assert.Throws<ArgumentException>(() => new Course("MAT101", title, 6));

        // Assert
        Assert.IsNotType<ArgumentNullException>(exception);
        Assert.Equal("title", exception.ParamName);
    }

    [Fact]
    public void Two_courses_with_the_same_code_are_equal_whatever_their_title_and_credits() {
        // Arrange
        Course anyCourse = Any.Course().Generate();
        Course anySameCodeCourse = Any.Course()
                                      .WithCode(anyCourse.Code)
                                      .WithTitle(t => t.DifferentFrom(anyCourse.Title))
                                      .WithCredits(c => c.DifferentFrom(anyCourse.Credits))
                                      .Generate();

        // Assert
        Assert.Equal(anyCourse, anySameCodeCourse);
        Assert.Equal(anyCourse.GetHashCode(), anySameCodeCourse.GetHashCode());
    }

    [Fact]
    public void Two_courses_with_different_codes_are_different_even_with_the_same_title_and_credits() {
        // Arrange
        Course anyCourse = Any.Course().Generate();
        Course anyOtherCourse = Any.Course()
                                   .WithCode(c => c.DifferentFrom(anyCourse.Code))
                                   .WithTitle(anyCourse.Title)
                                   .WithCredits(anyCourse.Credits)
                                   .Generate();

        // Assert
        Assert.NotEqual(anyCourse, anyOtherCourse);
    }

    [Fact]
    public void A_course_is_not_equal_to_null_or_to_its_code() {
        // Arrange
        Course anyCourse = Any.Course().Generate();

        // Assert
        Assert.False(anyCourse.Equals(null));
        Assert.False(anyCourse.Equals(anyCourse.Code));
    }

}
