using Education.Domain;
using JustDummies;
using JustDummies.Xunit;
using Xunit;

namespace Education.Tests;

[Reproducible]
public sealed class EnrolmentTests {

    // Courses worth the given credits, with distinct, arbitrary codes and titles.
    private static List<Course> CoursesWorth(params int[] credits) {
        List<string> anyCodes = Any.ListOf(Any.StringMatching("[A-Z]{3}[0-9]{3}")).WithCount(credits.Length).Distinct().Generate();

        return credits.Select((credit, index) => new AnyCourse().WithCode(anyCodes[index]).WithCredits(credit).Generate()).ToList();
    }

    private static Enrolment ClosedEnrolment(IReadOnlyList<Course> courses, params decimal[] grades) {
        Enrolment enrolment = Any.Enrolment().WithCourses(courses).Generate();
        for (int index = 0; index < courses.Count; index++) {
            enrolment.RecordGrade(courses[index].Code, new Grade(grades[index]));
        }
        enrolment.Close();

        return enrolment;
    }

    [Fact]
    public void The_average_weighs_each_grade_by_the_credits_of_its_course() {
        // Arrange
        List<Course> courses   = CoursesWorth(6, 3);
        Enrolment    enrolment = ClosedEnrolment(courses, 12m, 15m);

        // Act
        decimal average = enrolment.Average();

        // Assert
        Assert.Equal(13m, average);   // (12 x 6 + 15 x 3) / 9
    }

    [Fact]
    public void The_average_is_rounded_to_two_decimal_places() {
        // Arrange
        Enrolment enrolment = ClosedEnrolment(CoursesWorth(1, 1, 1), 10m, 10m, 10.5m);

        // Act
        decimal average = enrolment.Average();

        // Assert
        Assert.Equal(10.17m, average);   // 30.5 / 3 = 10.1666...
    }

    [Fact]
    public void An_eliminatory_grade_fails_the_semester_even_with_a_passing_average() {
        // Arrange
        Enrolment enrolment = ClosedEnrolment(CoursesWorth(3, 3), 4.5m, 16m);

        // Act
        bool passed = enrolment.IsPassed();

        // Assert
        Assert.Equal(10.25m, enrolment.Average());
        Assert.False(passed);
    }

    [Theory]
    [InlineData(9.5, Honours.None)]
    [InlineData(10, Honours.Pass)]
    [InlineData(12, Honours.Fair)]
    [InlineData(14.5, Honours.Good)]
    [InlineData(16, Honours.VeryGood)]
    public void Honours_follow_the_average(double grade, Honours expected) {
        // Arrange
        Enrolment enrolment = ClosedEnrolment(CoursesWorth(Any.Int32().Between(1, 6).Generate()), (decimal)grade);

        // Act
        Honours honours = enrolment.Honours();

        // Assert
        Assert.Equal(expected, honours);
    }

    [Fact]
    public void A_passed_semester_acquires_every_credit_by_compensation() {
        // Arrange
        Enrolment enrolment = ClosedEnrolment(CoursesWorth(6, 6), 8m, 14m);

        // Act
        int acquired = enrolment.AcquiredCredits();

        // Assert
        Assert.Equal(12, acquired);
    }

    [Fact]
    public void A_failed_semester_acquires_only_the_credits_of_the_courses_passed() {
        // Arrange
        Enrolment enrolment = ClosedEnrolment(CoursesWorth(6, 3, 3), 11m, 7m, 6m);

        // Act
        int acquired = enrolment.AcquiredCredits();

        // Assert
        Assert.Equal(6, acquired);
    }

    [Fact]
    public void A_grade_cannot_be_recorded_once_the_enrolment_is_closed() {
        // Arrange
        Enrolment anyEnrolment = Any.Enrolment().Generate();
        foreach (Course course in anyEnrolment.Courses) {
            anyEnrolment.RecordGrade(course.Code, new AnyGrade().Generate());
        }
        anyEnrolment.Close();
        Grade anyGrade = new AnyGrade().Generate();

        // Act
        Action recording = () => anyEnrolment.RecordGrade(anyEnrolment.Courses[0].Code, anyGrade);

        // Assert
        Assert.Throws<InvalidOperationException>(recording);
    }

    [Fact]
    public void An_enrolment_with_an_ungraded_course_cannot_be_closed() {
        // Arrange
        Enrolment anyEnrolment = Any.Enrolment().Generate();

        // Act
        Action closing = anyEnrolment.Close;

        // Assert
        Assert.Throws<InvalidOperationException>(closing);
        Assert.Equal(EnrolmentStatus.Open, anyEnrolment.Status);
    }

    [Fact]
    public void A_grade_for_a_course_the_student_did_not_take_is_refused() {
        // Arrange
        Enrolment anyEnrolment = Any.Enrolment().Generate();
        string    anyOtherCode = Any.StringMatching("[A-Z]{3}[0-9]{3}")
                                    .Except(anyEnrolment.Courses.Select(course => course.Code).ToArray())
                                    .Generate();

        // Act
        Action recording = () => anyEnrolment.RecordGrade(anyOtherCode, new AnyGrade().Generate());

        // Assert
        Assert.Throws<InvalidOperationException>(recording);
    }

    [Fact]
    public void An_enrolment_holds_at_most_eight_courses() {
        // Arrange
        List<Course> anyNineCourses = Any.ListOf(new AnyCourse().WithCredits(credits => credits.LessThanOrEqualTo(4)))
                                         .WithCount(9)
                                         .Distinct()
                                         .Generate();

        // Act
        Action enrolling = () => Any.Enrolment().WithCourses(anyNineCourses).Generate();

        // Assert
        Assert.Throws<ArgumentException>(enrolling);
    }

    [Fact]
    public void The_same_course_cannot_be_taken_twice_in_one_enrolment() {
        // Arrange
        string anyCode      = Any.StringMatching("[A-Z]{3}[0-9]{3}").Generate();
        Course anyCourse    = new AnyCourse().WithCode(anyCode).WithCredits(credits => credits.LessThanOrEqualTo(6)).Generate();
        Course anySameAgain = new AnyCourse().WithCode(anyCode).WithCredits(credits => credits.LessThanOrEqualTo(6)).Generate();

        // Act
        Action enrolling = () => Any.Enrolment().WithCourses(new List<Course> { anyCourse, anySameAgain }).Generate();

        // Assert
        ArgumentException exception = Assert.Throws<ArgumentException>(enrolling);
        Assert.Contains("twice", exception.Message);
    }

    [Fact]
    public void An_enrolment_carries_at_most_thirty_six_credits() {
        // Arrange
        List<Course> courses = CoursesWorth(30, 7);

        // Act
        Action enrolling = () => Any.Enrolment().WithCourses(courses).Generate();

        // Assert
        Assert.Throws<ArgumentException>(enrolling);
    }
}
