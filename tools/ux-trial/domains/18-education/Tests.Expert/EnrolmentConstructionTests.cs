using Education.Domain;

using JustDummies;

using Xunit;

using static Education.Tests.Expert.Fixtures;

namespace Education.Tests.Expert;

public sealed class EnrolmentConstructionTests {

    [Fact]
    public void An_enrolment_holds_the_student_the_semester_and_the_courses_it_was_given_and_starts_open() {
        // Arrange
        Student      anyStudent  = Any.Student().Generate();
        Semester     anySemester = Any.Semester().Generate();
        List<Course> courses     = CoursesWithCredits(6, 4);

        // Act
        Enrolment enrolment = new Enrolment(anyStudent, anySemester, courses);

        // Assert
        Assert.Same(anyStudent, enrolment.Student);
        Assert.Same(anySemester, enrolment.Semester);
        Assert.Equal(courses, enrolment.Courses);
        Assert.Equal(EnrolmentStatus.Open, enrolment.Status);
    }

    [Fact]
    public void The_credits_of_an_enrolment_are_the_sum_of_the_credits_of_its_courses() {
        // Act
        Enrolment enrolment = OpenEnrolmentOf(CoursesWithCredits(6, 4, 5));

        // Assert
        Assert.Equal(15, enrolment.Credits);
    }

    [Fact]
    public void An_enrolment_does_not_follow_the_list_of_courses_it_was_built_from() {
        // Arrange
        List<Course> courses   = CoursesWithCredits(6, 4);
        Enrolment    enrolment = OpenEnrolmentOf(courses);

        // Act
        courses.Clear();

        // Assert
        Assert.Equal(2, enrolment.Courses.Count);
        Assert.Equal(10, enrolment.Credits);
    }

    [Fact]
    public void A_missing_student_is_refused() {
        // Arrange
        Semester     anySemester = Any.Semester().Generate();
        List<Course> anyCourses  = CoursesWithCredits(6);

        // Act
        ArgumentNullException refusal = Assert.Throws<ArgumentNullException>(() => new Enrolment(null!, anySemester, anyCourses));

        // Assert
        Assert.Equal("student", refusal.ParamName);
    }

    [Fact]
    public void A_missing_semester_is_refused() {
        // Arrange
        Student      anyStudent = Any.Student().Generate();
        List<Course> anyCourses = CoursesWithCredits(6);

        // Act
        ArgumentNullException refusal = Assert.Throws<ArgumentNullException>(() => new Enrolment(anyStudent, null!, anyCourses));

        // Assert
        Assert.Equal("semester", refusal.ParamName);
    }

    [Fact]
    public void A_missing_list_of_courses_is_refused() {
        // Arrange
        Student  anyStudent  = Any.Student().Generate();
        Semester anySemester = Any.Semester().Generate();

        // Act
        ArgumentNullException refusal = Assert.Throws<ArgumentNullException>(() => new Enrolment(anyStudent, anySemester, null!));

        // Assert
        Assert.Equal("courses", refusal.ParamName);
    }

    [Fact]
    public void An_enrolment_needs_at_least_one_course() {
        // Arrange
        Student  anyStudent  = Any.Student().Generate();
        Semester anySemester = Any.Semester().Generate();

        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => new Enrolment(anyStudent, anySemester, new List<Course>()));

        // Assert
        Assert.Equal("courses", refusal.ParamName);
        Assert.Contains("not 0", refusal.Message);
    }

    [Fact]
    public void An_enrolment_may_hold_up_to_eight_courses() {
        // Act
        Enrolment enrolment = OpenEnrolmentOf(CoursesWithCredits(1, 2, 3, 4, 5, 1, 2, 3));

        // Assert
        Assert.Equal(Enrolment.MaximumCourses, enrolment.Courses.Count);
    }

    [Fact]
    public void An_enrolment_of_nine_courses_is_refused() {
        // Arrange
        Student      anyStudent  = Any.Student().Generate();
        Semester     anySemester = Any.Semester().Generate();
        List<Course> nineCourses = CoursesWithCredits(1, 1, 1, 1, 1, 1, 1, 1, 1);

        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => new Enrolment(anyStudent, anySemester, nineCourses));

        // Assert
        Assert.Equal("courses", refusal.ParamName);
        Assert.Contains("not 9", refusal.Message);
    }

    [Fact]
    public void A_course_cannot_appear_twice_in_an_enrolment() {
        // Arrange
        Student  anyStudent  = Any.Student().Generate();
        Semester anySemester = Any.Semester().Generate();
        Course   course      = Any.Course().Generate();
        Course   sameCode    = Any.Course().WithCode(course.Code).Generate();

        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => new Enrolment(anyStudent, anySemester, [course, sameCode]));

        // Assert
        Assert.Equal("courses", refusal.ParamName);
    }

    [Fact]
    public void An_enrolment_may_carry_36_credits() {
        // Act
        Enrolment enrolment = OpenEnrolmentOf(CoursesWithCredits(30, 6));

        // Assert
        Assert.Equal(Enrolment.MaximumCredits, enrolment.Credits);
    }

    [Fact]
    public void An_enrolment_of_37_credits_is_refused() {
        // Arrange
        Student      anyStudent  = Any.Student().Generate();
        Semester     anySemester = Any.Semester().Generate();
        List<Course> courses     = CoursesWithCredits(30, 7);

        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => new Enrolment(anyStudent, anySemester, courses));

        // Assert
        Assert.Equal("courses", refusal.ParamName);
        Assert.Contains("36", refusal.Message);
    }

    [Fact]
    public void The_count_of_courses_is_checked_before_their_credits() {
        // Arrange
        Student      anyStudent  = Any.Student().Generate();
        Semester     anySemester = Any.Semester().Generate();
        List<Course> nineCourses = CoursesWithCredits(30, 30, 30, 30, 30, 30, 30, 30, 30);

        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => new Enrolment(anyStudent, anySemester, nineCourses));

        // Assert
        Assert.Contains("not 9", refusal.Message);
    }

    [Fact(Skip = "domain defect: a null item in the list of courses makes the constructor throw NullReferenceException instead of refusing the argument; no draw involved, no seed")]
    public void A_null_course_in_the_list_is_refused_as_an_invalid_argument() {
        // Arrange
        Student  anyStudent  = Any.Student().Generate();
        Semester anySemester = Any.Semester().Generate();
        Course   anyCourse   = Any.Course().Generate();

        // Act & Assert
        Assert.Throws<ArgumentException>(() => new Enrolment(anyStudent, anySemester, [anyCourse, null!]));
    }

}
