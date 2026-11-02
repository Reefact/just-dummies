using Education.Domain;

using JustDummies;

using Xunit;

using static Education.Tests.Expert.Fixtures;

namespace Education.Tests.Expert;

public sealed class EnrolmentGradingTests {

    [Fact]
    public void A_recorded_grade_is_found_by_the_code_of_its_course() {
        // Arrange
        List<Course> courses   = CoursesWithCredits(6, 4);
        Enrolment    enrolment = OpenEnrolmentOf(courses);
        Grade        grade     = new Grade(13.5m);

        // Act
        enrolment.RecordGrade(courses[1].Code, grade);

        // Assert
        Assert.Same(grade, enrolment.GradeFor(courses[1].Code));
        Assert.Null(enrolment.GradeFor(courses[0].Code));
    }

    [Fact]
    public void The_grade_of_a_course_the_student_is_not_enrolled_in_is_absent() {
        // Arrange
        Enrolment enrolment = OpenEnrolmentOf(CoursesWithCredits(6));
        Course    foreign   = Any.Course().WithCode("ZZZ999").Generate();

        // Assert
        Assert.Null(enrolment.GradeFor(foreign.Code));
    }

    [Fact]
    public void Asking_for_the_grade_of_a_missing_code_is_refused() {
        // Arrange
        Enrolment anyEnrolment = Any.Enrolment().Generate();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => anyEnrolment.GradeFor(null!));
    }

    [Fact]
    public void An_open_enrolment_lets_a_grade_be_overwritten() {
        // Arrange
        List<Course> courses   = CoursesWithCredits(6);
        Enrolment    enrolment = OpenEnrolmentOf(courses);

        enrolment.RecordGrade(courses[0].Code, new Grade(8m));

        // Act
        enrolment.RecordGrade(courses[0].Code, new Grade(12m));

        // Assert
        Assert.Equal(new Grade(12m), enrolment.GradeFor(courses[0].Code));
    }

    [Fact]
    public void A_grade_for_a_course_the_student_is_not_enrolled_in_is_refused() {
        // Arrange
        Enrolment enrolment = OpenEnrolmentOf(CoursesWithCredits(6));
        Course    foreign   = Any.Course().WithCode("ZZZ999").Generate();
        Grade     anyGrade  = Any.Grade().Generate();

        // Act
        InvalidOperationException refusal = Assert.Throws<InvalidOperationException>(() => enrolment.RecordGrade(foreign.Code, anyGrade));

        // Assert
        Assert.Contains("ZZZ999", refusal.Message);
        Assert.Null(enrolment.GradeFor("ZZZ999"));
    }

    [Fact]
    public void A_missing_grade_is_refused_even_on_a_closed_enrolment() {
        // Arrange
        Enrolment enrolment = ClosedEnrolment((6, 12m));

        // Act & Assert (the exact type: the closed state would be an invalid operation)
        Assert.Throws<ArgumentNullException>(() => enrolment.RecordGrade(enrolment.Courses[0].Code, null!));
    }

    [Fact]
    public void A_missing_grade_is_refused() {
        // Arrange
        List<Course> courses   = CoursesWithCredits(6);
        Enrolment    enrolment = OpenEnrolmentOf(courses);

        // Act
        ArgumentNullException refusal = Assert.Throws<ArgumentNullException>(() => enrolment.RecordGrade(courses[0].Code, null!));

        // Assert
        Assert.Equal("grade", refusal.ParamName);
    }

    [Fact]
    public void A_closed_enrolment_refuses_any_further_grade_and_keeps_the_final_one() {
        // Arrange
        List<Course> courses   = CoursesWithCredits(6);
        Enrolment    enrolment = OpenEnrolmentOf(courses);

        enrolment.RecordGrade(courses[0].Code, new Grade(12m));
        enrolment.Close();

        // Act
        InvalidOperationException refusal = Assert.Throws<InvalidOperationException>(() => enrolment.RecordGrade(courses[0].Code, new Grade(18m)));

        // Assert
        Assert.Contains("closed", refusal.Message);
        Assert.Equal(new Grade(12m), enrolment.GradeFor(courses[0].Code));
    }

    [Fact]
    public void An_enrolment_cannot_be_closed_while_a_course_has_no_grade() {
        // Arrange
        List<Course> courses   = CoursesWithCredits(6, 4, 5);
        Enrolment    enrolment = OpenEnrolmentOf(courses);

        enrolment.RecordGrade(courses[0].Code, new Grade(12m));

        // Act
        InvalidOperationException refusal = Assert.Throws<InvalidOperationException>(() => enrolment.Close());

        // Assert
        Assert.Contains("2 course(s)", refusal.Message);
        Assert.Equal(EnrolmentStatus.Open, enrolment.Status);
    }

    [Fact]
    public void An_enrolment_whose_courses_are_all_graded_closes() {
        // Arrange
        Enrolment enrolment = GradedEnrolment((6, 12m), (4, 9m));

        // Act
        enrolment.Close();

        // Assert
        Assert.Equal(EnrolmentStatus.Closed, enrolment.Status);
    }

    [Fact]
    public void Closing_a_closed_enrolment_again_is_harmless() {
        // Arrange
        Enrolment enrolment = ClosedEnrolment((6, 12m));

        // Act
        enrolment.Close();

        // Assert
        Assert.Equal(EnrolmentStatus.Closed, enrolment.Status);
    }

    [Fact]
    public void A_grade_recorded_twice_for_the_same_course_counts_once_towards_closing() {
        // Arrange
        List<Course> courses   = CoursesWithCredits(6, 4);
        Enrolment    enrolment = OpenEnrolmentOf(courses);

        enrolment.RecordGrade(courses[0].Code, new Grade(12m));
        enrolment.RecordGrade(courses[0].Code, new Grade(14m));

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => enrolment.Close());
    }

}
