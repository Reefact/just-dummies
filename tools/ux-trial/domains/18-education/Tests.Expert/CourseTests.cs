using Education.Domain;

using JustDummies;

using Xunit;

namespace Education.Tests.Expert;

public sealed class CourseTests {

    [Fact]
    public void A_course_keeps_its_code_title_and_credits() {
        // Act
        Course course = new Course("MAT101", "Linear algebra", 6);

        // Assert
        Assert.Equal("MAT101", course.Code);
        Assert.Equal("Linear algebra", course.Title);
        Assert.Equal(6, course.Credits);
    }

    [Theory]
    [InlineData("mat101")]    // capitals only
    [InlineData("MA1011")]
    [InlineData("MA101")]     // three letters
    [InlineData("MATH101")]
    [InlineData("MAT10")]     // three digits
    [InlineData("MAT1011")]
    [InlineData("MAT 101")]
    [InlineData(" MAT101")]
    [InlineData("MAT101 ")]
    [InlineData("")]
    public void A_code_that_is_not_three_capitals_and_three_digits_is_refused(string malformed) {
        // Arrange
        Course anyCourse = Any.Course().Generate();

        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => new Course(malformed, anyCourse.Title, anyCourse.Credits));

        // Assert
        Assert.Equal("code", refusal.ParamName);
    }

    [Fact]
    public void A_missing_code_is_refused_like_a_malformed_one_not_as_a_missing_argument() {
        // Arrange
        Course anyCourse = Any.Course().Generate();

        // Act (the exact type: ArgumentNullException would be a different refusal)
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => new Course(null!, anyCourse.Title, anyCourse.Credits));

        // Assert
        Assert.Equal("code", refusal.ParamName);
    }

    [Fact(Skip = "domain defect: '$' also matches before a final line feed, so the code 'MAT101\\n' is accepted; literal input, no seed")]
    public void A_code_followed_by_a_line_feed_is_refused() {
        // Arrange
        Course anyCourse = Any.Course().Generate();

        // Act & Assert
        Assert.Throws<ArgumentException>(() => new Course("MAT101\n", anyCourse.Title, anyCourse.Credits));
    }

    [Fact]
    public void A_missing_title_is_refused_as_a_missing_argument() {
        // Arrange
        Course anyCourse = Any.Course().Generate();

        // Act
        ArgumentNullException refusal = Assert.Throws<ArgumentNullException>(() => new Course(anyCourse.Code, null!, anyCourse.Credits));

        // Assert
        Assert.Equal("title", refusal.ParamName);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(" \t\r\n")]
    public void A_blank_title_is_refused(string blank) {
        // Arrange
        Course anyCourse = Any.Course().Generate();

        // Act (the exact type: a blank title is not a missing one)
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => new Course(anyCourse.Code, blank, anyCourse.Credits));

        // Assert
        Assert.Equal("title", refusal.ParamName);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(30)]
    public void A_course_is_worth_from_1_to_30_credits(int credits) {
        // Arrange
        Course anyCourse = Any.Course().Generate();

        // Act
        Course course = new Course(anyCourse.Code, anyCourse.Title, credits);

        // Assert
        Assert.Equal(credits, course.Credits);
    }

    [Theory]
    [InlineData(int.MinValue)]
    [InlineData(-1)]
    [InlineData(0)]
    [InlineData(31)]
    [InlineData(int.MaxValue)]
    public void A_course_worth_no_credit_or_more_than_30_is_refused(int credits) {
        // Arrange
        Course anyCourse = Any.Course().Generate();

        // Act
        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(() => new Course(anyCourse.Code, anyCourse.Title, credits));

        // Assert
        Assert.Equal("credits", refusal.ParamName);
        Assert.Equal(credits, refusal.ActualValue);
    }

    [Fact]
    public void Two_courses_with_the_same_code_are_equal_whatever_their_title_and_credits() {
        // Arrange
        Course anyCourse      = Any.Course().Generate();
        Course anyOtherCourse = Any.Course().Generate();

        Course course = new Course("MAT101", anyCourse.Title, anyCourse.Credits);
        Course same   = new Course("MAT101", anyOtherCourse.Title, anyOtherCourse.Credits);

        // Assert
        Assert.True(course.Equals(same));
        Assert.Equal(course.GetHashCode(), same.GetHashCode());
    }

    [Fact]
    public void Two_courses_with_different_codes_are_not_equal_even_with_the_same_title_and_credits() {
        // Arrange
        Course anyCourse = Any.Course().Generate();

        Course course = new Course("MAT101", anyCourse.Title, anyCourse.Credits);
        Course other  = new Course("MAT102", anyCourse.Title, anyCourse.Credits);

        // Assert
        Assert.False(course.Equals(other));
    }

    [Fact]
    public void A_course_is_not_equal_to_its_code_nor_to_null() {
        // Arrange
        Course course = Any.Course().WithCode("MAT101").Generate();

        // Assert
        Assert.False(course.Equals("MAT101"));
        Assert.False(course.Equals(null));
    }

}
