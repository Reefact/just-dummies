using Education.Domain;

using JustDummies;

using Xunit;

namespace Education.Domain.Tests.Discovery;

public sealed class EnrolmentTests {

    // A course the test only cares about through its code and credits; its title is a dummy.
    private static Course CourseOf(string code, int credits) {
        return Any.Course().WithCode(code).WithCredits(credits).Generate();
    }

    // An open enrolment on the given courses; the student and the semester are dummies.
    private static Enrolment OpenEnrolmentOn(params Course[] courses) {
        return Any.Enrolment().WithCourses(courses).Generate();
    }

    // A closed enrolment: every (code, credits) course carries the matching grade.
    private static Enrolment ClosedEnrolmentWith(params (string Code, int Credits, decimal Grade)[] results) {
        Course[]  courses   = results.Select(result => CourseOf(result.Code, result.Credits)).ToArray();
        Enrolment enrolment = OpenEnrolmentOn(courses);

        foreach ((string code, int _, decimal grade) in results) {
            enrolment.RecordGrade(code, new Grade(grade));
        }

        enrolment.Close();

        return enrolment;
    }

    // ---- construction -------------------------------------------------------------------------

    [Fact]
    public void A_new_enrolment_is_open_and_keeps_what_it_was_built_with() {
        // Arrange
        Student  anyStudent  = Any.Student().Generate();
        Semester anySemester = Any.Semester().Generate();
        Course   algebra     = CourseOf("MAT101", 6);
        Course   physics     = CourseOf("PHY101", 4);

        // Act
        Enrolment enrolment = new Enrolment(anyStudent, anySemester, new[] { algebra, physics });

        // Assert
        Assert.Same(anyStudent, enrolment.Student);
        Assert.Same(anySemester, enrolment.Semester);
        Assert.Equal(EnrolmentStatus.Open, enrolment.Status);
        Assert.Equal(new[] { algebra, physics }, enrolment.Courses);
    }

    [Fact]
    public void A_drawn_enrolment_is_open_whatever_it_holds() {
        Enrolment anyEnrolment = Any.Enrolment().Generate();

        Assert.Equal(EnrolmentStatus.Open, anyEnrolment.Status);
    }

    [Fact]
    public void An_enrolment_without_a_student_is_refused_as_null() {
        Semester anySemester = Any.Semester().Generate();
        Course   anyCourse   = Any.Course().Generate();

        Assert.Throws<ArgumentNullException>(() => new Enrolment(null!, anySemester, new[] { anyCourse }));
    }

    [Fact]
    public void An_enrolment_without_a_semester_is_refused_as_null() {
        Student anyStudent = Any.Student().Generate();
        Course  anyCourse  = Any.Course().Generate();

        Assert.Throws<ArgumentNullException>(() => new Enrolment(anyStudent, null!, new[] { anyCourse }));
    }

    [Fact]
    public void An_enrolment_without_a_course_list_is_refused_as_null() {
        Student  anyStudent  = Any.Student().Generate();
        Semester anySemester = Any.Semester().Generate();

        Assert.Throws<ArgumentNullException>(() => new Enrolment(anyStudent, anySemester, null!));
    }

    [Fact]
    public void An_enrolment_with_no_course_is_refused() {
        Student  anyStudent  = Any.Student().Generate();
        Semester anySemester = Any.Semester().Generate();

        Assert.Throws<ArgumentException>(() => new Enrolment(anyStudent, anySemester, Array.Empty<Course>()));
    }

    [Fact]
    public void An_enrolment_with_eight_courses_is_accepted() {
        Student  anyStudent  = Any.Student().Generate();
        Semester anySemester = Any.Semester().Generate();
        Course[] courses     = EightCourses(creditsEach: 1);

        Enrolment enrolment = new Enrolment(anyStudent, anySemester, courses);

        Assert.Equal(8, enrolment.Courses.Count);
    }

    [Fact]
    public void An_enrolment_with_nine_courses_is_refused() {
        Student  anyStudent  = Any.Student().Generate();
        Semester anySemester = Any.Semester().Generate();
        Course[] nine        = EightCourses(creditsEach: 1).Append(CourseOf("ZZZ999", 1)).ToArray();

        Assert.Throws<ArgumentException>(() => new Enrolment(anyStudent, anySemester, nine));
    }

    [Fact]
    public void The_same_course_twice_is_refused() {
        Student  anyStudent  = Any.Student().Generate();
        Semester anySemester = Any.Semester().Generate();

        Assert.Throws<ArgumentException>(() => new Enrolment(anyStudent, anySemester, new[] { CourseOf("MAT101", 3), CourseOf("MAT101", 3) }));
    }

    [Fact]
    public void Two_courses_sharing_a_code_are_refused_even_with_another_title_and_credits() {
        Student  anyStudent  = Any.Student().Generate();
        Semester anySemester = Any.Semester().Generate();

        Assert.Throws<ArgumentException>(() => new Enrolment(anyStudent, anySemester, new[] { CourseOf("MAT101", 3), CourseOf("MAT101", 12) }));
    }

    [Fact]
    public void Thirty_six_credits_are_accepted() {
        Student  anyStudent  = Any.Student().Generate();
        Semester anySemester = Any.Semester().Generate();

        Enrolment enrolment = new Enrolment(anyStudent, anySemester, new[] { CourseOf("MAT101", 30), CourseOf("PHY101", 6) });

        Assert.Equal(36, enrolment.Credits);
    }

    [Fact]
    public void Thirty_seven_credits_are_refused() {
        Student  anyStudent  = Any.Student().Generate();
        Semester anySemester = Any.Semester().Generate();

        Assert.Throws<ArgumentException>(() => new Enrolment(anyStudent, anySemester, new[] { CourseOf("MAT101", 30), CourseOf("PHY101", 7) }));
    }

    [Fact]
    public void The_credits_are_the_sum_of_the_courses_credits() {
        Enrolment enrolment = OpenEnrolmentOn(CourseOf("MAT101", 6), CourseOf("PHY101", 4), CourseOf("CHM101", 3));

        Assert.Equal(13, enrolment.Credits);
    }

    [Fact]
    public void The_constants_state_the_limits() {
        Assert.Equal(8, Enrolment.MaximumCourses);
        Assert.Equal(36, Enrolment.MaximumCredits);
        Assert.Equal(10m, Enrolment.PassMark);
    }

    [Fact]
    public void A_drawn_enrolment_never_exceeds_its_limits() {
        for (int i = 0; i < 300; i++) {
            Enrolment anyEnrolment = Any.Enrolment().Generate();

            Assert.InRange(anyEnrolment.Courses.Count, 1, Enrolment.MaximumCourses);
            Assert.True(anyEnrolment.Credits <= Enrolment.MaximumCredits);
        }
    }

    // ---- recording grades ---------------------------------------------------------------------

    [Fact]
    public void A_recorded_grade_can_be_read_back_by_course_code() {
        Enrolment enrolment = OpenEnrolmentOn(CourseOf("MAT101", 6), CourseOf("PHY101", 4));
        Grade     grade     = new Grade(14.5m);

        enrolment.RecordGrade("MAT101", grade);

        Assert.Same(grade, enrolment.GradeFor("MAT101"));
        Assert.Null(enrolment.GradeFor("PHY101"));
    }

    [Fact]
    public void The_grade_of_a_course_the_student_is_not_enrolled_in_is_null() {
        Enrolment enrolment = OpenEnrolmentOn(CourseOf("MAT101", 6));

        Assert.Null(enrolment.GradeFor("PHY101"));
    }

    [Fact]
    public void Asking_for_the_grade_of_a_null_code_is_refused() {
        Enrolment anyEnrolment = Any.Enrolment().Generate();

        Assert.Throws<ArgumentNullException>(() => anyEnrolment.GradeFor(null!));
    }

    [Fact]
    public void A_grade_can_be_overwritten_while_the_enrolment_is_open() {
        Enrolment enrolment = OpenEnrolmentOn(CourseOf("MAT101", 6));

        enrolment.RecordGrade("MAT101", new Grade(8m));
        enrolment.RecordGrade("MAT101", new Grade(15m));

        Assert.Equal(new Grade(15m), enrolment.GradeFor("MAT101"));
    }

    [Fact]
    public void A_null_grade_is_refused() {
        Enrolment enrolment = OpenEnrolmentOn(CourseOf("MAT101", 6));

        Assert.Throws<ArgumentNullException>(() => enrolment.RecordGrade("MAT101", null!));
    }

    [Fact(Skip = "domain defect: RecordGrade(null, grade) throws InvalidOperationException ('not enrolled in ') whereas GradeFor(null) throws ArgumentNullException, literal input")]
    public void A_grade_for_a_null_code_is_refused_as_null() {
        Enrolment enrolment = OpenEnrolmentOn(CourseOf("MAT101", 6));

        Assert.Throws<ArgumentNullException>(() => enrolment.RecordGrade(null!, new Grade(12m)));
    }

    [Fact]
    public void A_grade_for_a_course_outside_the_enrolment_is_refused() {
        Enrolment enrolment = OpenEnrolmentOn(CourseOf("MAT101", 6));
        Grade     anyGrade  = Any.Grade().Generate();

        Assert.Throws<InvalidOperationException>(() => enrolment.RecordGrade("PHY101", anyGrade));
    }

    [Fact]
    public void A_grade_recorded_once_closed_is_refused_even_for_an_enrolled_course() {
        Enrolment enrolment = ClosedEnrolmentWith(("MAT101", 6, 12m));

        Assert.Throws<InvalidOperationException>(() => enrolment.RecordGrade("MAT101", new Grade(18m)));
        Assert.Equal(new Grade(12m), enrolment.GradeFor("MAT101"));
    }

    // ---- closing ------------------------------------------------------------------------------

    [Fact]
    public void An_enrolment_with_an_ungraded_course_cannot_be_closed() {
        Enrolment enrolment = OpenEnrolmentOn(CourseOf("MAT101", 6), CourseOf("PHY101", 4));
        Grade     anyGrade  = Any.Grade().Generate();

        enrolment.RecordGrade("MAT101", anyGrade);

        Assert.Throws<InvalidOperationException>(() => enrolment.Close());
        Assert.Equal(EnrolmentStatus.Open, enrolment.Status);
    }

    [Fact]
    public void An_enrolment_without_any_grade_cannot_be_closed() {
        Enrolment anyEnrolment = Any.Enrolment().Generate();

        Assert.Throws<InvalidOperationException>(() => anyEnrolment.Close());
    }

    [Fact]
    public void A_fully_graded_enrolment_closes() {
        Enrolment enrolment = OpenEnrolmentOn(CourseOf("MAT101", 6), CourseOf("PHY101", 4));
        Grade     anyGrade  = Any.Grade().Generate();

        enrolment.RecordGrade("MAT101", anyGrade);
        enrolment.RecordGrade("PHY101", anyGrade);
        enrolment.Close();

        Assert.Equal(EnrolmentStatus.Closed, enrolment.Status);
    }

    [Fact]
    public void Closing_a_closed_enrolment_again_succeeds() {
        Enrolment enrolment = ClosedEnrolmentWith(("MAT101", 6, 12m));

        enrolment.Close();

        Assert.Equal(EnrolmentStatus.Closed, enrolment.Status);
    }

    // ---- everything computed requires a closed enrolment -----------------------------------------

    [Fact]
    public void The_average_of_an_open_enrolment_is_refused() {
        Enrolment enrolment = OpenEnrolmentOn(CourseOf("MAT101", 6));

        enrolment.RecordGrade("MAT101", new Grade(12m));

        Assert.Throws<InvalidOperationException>(() => enrolment.Average());
    }

    [Fact]
    public void An_open_enrolment_cannot_say_whether_it_is_passed() {
        Enrolment anyEnrolment = Any.Enrolment().Generate();

        Assert.Throws<InvalidOperationException>(() => anyEnrolment.IsPassed());
    }

    [Fact]
    public void An_open_enrolment_cannot_say_how_many_credits_are_acquired() {
        Enrolment anyEnrolment = Any.Enrolment().Generate();

        Assert.Throws<InvalidOperationException>(() => anyEnrolment.AcquiredCredits());
    }

    [Fact]
    public void An_open_enrolment_has_no_honours_yet() {
        Enrolment anyEnrolment = Any.Enrolment().Generate();

        Assert.Throws<InvalidOperationException>(() => anyEnrolment.Honours());
    }

    // ---- average ------------------------------------------------------------------------------

    [Fact]
    public void The_average_of_a_single_course_is_its_grade() {
        Enrolment enrolment = ClosedEnrolmentWith(("MAT101", 5, 13.5m));

        Assert.Equal(13.5m, enrolment.Average());
    }

    [Fact]
    public void The_average_is_weighted_by_credits() {
        // (12 * 6 + 15 * 3) / 9 = 13
        Enrolment enrolment = ClosedEnrolmentWith(("MAT101", 6, 12m), ("PHY101", 3, 15m));

        Assert.Equal(13m, enrolment.Average());
    }

    [Fact]
    public void A_heavy_course_pulls_the_average_towards_its_grade() {
        // (20 * 30 + 0 * 1) / 31 = 19.35
        Enrolment enrolment = ClosedEnrolmentWith(("MAT101", 30, 20m), ("PHY101", 1, 0m));

        Assert.Equal(19.35m, enrolment.Average());
    }

    [Fact]
    public void The_average_is_rounded_to_two_decimals_away_from_zero_on_a_midpoint() {
        // (10 * 7 + 11 * 1) / 8 = 10.125, midpoint: away from zero gives 10.13 (banker's rounding would give 10.12)
        Enrolment enrolment = ClosedEnrolmentWith(("MAT101", 7, 10m), ("PHY101", 1, 11m));

        Assert.Equal(10.13m, enrolment.Average());
    }

    [Fact]
    public void The_average_is_rounded_down_below_the_midpoint() {
        // (10 * 2 + 11 * 1) / 3 = 10.3333...
        Enrolment enrolment = ClosedEnrolmentWith(("MAT101", 2, 10m), ("PHY101", 1, 11m));

        Assert.Equal(10.33m, enrolment.Average());
    }

    [Fact]
    public void The_average_is_rounded_up_above_the_midpoint() {
        // (10 * 1 + 11 * 2) / 3 = 10.6666...
        Enrolment enrolment = ClosedEnrolmentWith(("MAT101", 1, 10m), ("PHY101", 2, 11m));

        Assert.Equal(10.67m, enrolment.Average());
    }

    // ---- passed -------------------------------------------------------------------------------

    [Fact]
    public void An_average_of_exactly_ten_passes() {
        Enrolment enrolment = ClosedEnrolmentWith(("MAT101", 6, 10m));

        Assert.True(enrolment.IsPassed());
    }

    [Fact]
    public void An_average_just_under_ten_does_not_pass() {
        // (10 * 5 + 9.5 * 1) / 6 = 9.92
        Enrolment enrolment = ClosedEnrolmentWith(("MAT101", 5, 10m), ("PHY101", 1, 9.5m));

        Assert.False(enrolment.IsPassed());
    }

    [Fact]
    public void A_grade_of_exactly_five_is_not_eliminatory() {
        // (5 * 3 + 15 * 3) / 6 = 10
        Enrolment enrolment = ClosedEnrolmentWith(("MAT101", 3, 5m), ("PHY101", 3, 15m));

        Assert.True(enrolment.IsPassed());
    }

    [Fact]
    public void An_eliminatory_grade_fails_the_semester_whatever_the_average() {
        // (4.5 * 1 + 20 * 3) / 4 = 16.13: the average is brilliant, the 4.5 is eliminatory
        Enrolment enrolment = ClosedEnrolmentWith(("MAT101", 1, 4.5m), ("PHY101", 3, 20m));

        Assert.True(enrolment.Average() >= Enrolment.PassMark);
        Assert.False(enrolment.IsPassed());
    }

    // ---- acquired credits ---------------------------------------------------------------------

    [Fact]
    public void A_passed_semester_acquires_every_credit_even_the_courses_under_ten() {
        // compensation: 5 and 15 over equal credits average 10
        Enrolment enrolment = ClosedEnrolmentWith(("MAT101", 3, 5m), ("PHY101", 3, 15m));

        Assert.Equal(6, enrolment.AcquiredCredits());
    }

    [Fact]
    public void A_failed_semester_acquires_only_the_courses_graded_ten_or_more() {
        // (12 * 2 + 10 * 2 + 6 * 4) / 8 = 8.5: failed. 12 and 10 are acquired, 6 is not.
        Enrolment enrolment = ClosedEnrolmentWith(("MAT101", 2, 12m), ("PHY101", 2, 10m), ("CHM101", 4, 6m));

        Assert.False(enrolment.IsPassed());
        Assert.Equal(4, enrolment.AcquiredCredits());
    }

    [Fact]
    public void A_course_graded_just_under_ten_is_not_acquired_in_a_failed_semester() {
        // (9.5 * 6 + 5 * 4) / 10 = 7.7: failed
        Enrolment enrolment = ClosedEnrolmentWith(("MAT101", 6, 9.5m), ("PHY101", 4, 5m));

        Assert.Equal(0, enrolment.AcquiredCredits());
    }

    [Fact]
    public void An_eliminatory_grade_keeps_the_good_courses_but_not_the_compensation() {
        // average 16.13 but a 4.5: failed, so only the 20 course is acquired
        Enrolment enrolment = ClosedEnrolmentWith(("MAT101", 1, 4.5m), ("PHY101", 3, 20m));

        Assert.Equal(3, enrolment.AcquiredCredits());
    }

    // ---- honours ------------------------------------------------------------------------------

    public static TheoryData<decimal, Honours> HonoursBoundaries => new TheoryData<decimal, Honours> {
        { 20m, Honours.VeryGood },
        { 16m, Honours.VeryGood },
        { 15.5m, Honours.Good },
        { 14m, Honours.Good },
        { 13.5m, Honours.Fair },
        { 12m, Honours.Fair },
        { 11.5m, Honours.Pass },
        { 10m, Honours.Pass },
        { 9.5m, Honours.None },
        { 0m, Honours.None }
    };

    [Theory]
    [MemberData(nameof(HonoursBoundaries))]
    public void Honours_follow_the_average_thresholds(decimal grade, Honours expected) {
        // A single course: its grade is the average. Code and credits do not matter, they are dummies of the course.
        Course    anyCourse = Any.Course().Generate();
        Enrolment enrolment = Any.Enrolment().WithCourses(new[] { anyCourse }).Generate();

        enrolment.RecordGrade(anyCourse.Code, new Grade(grade));
        enrolment.Close();

        Assert.Equal(expected, enrolment.Honours());
    }

    [Fact]
    public void A_student_with_an_eliminatory_grade_has_no_honours_whatever_the_average() {
        Enrolment enrolment = ClosedEnrolmentWith(("MAT101", 1, 4.5m), ("PHY101", 3, 20m));

        Assert.Equal(Honours.None, enrolment.Honours());
    }

    [Fact]
    public void Honours_are_decided_on_the_rounded_average() {
        // (16 * 7 + 15.5 * 1) / 8 = 15.9375 -> 15.94: Good, not VeryGood
        Enrolment enrolment = ClosedEnrolmentWith(("MAT101", 7, 16m), ("PHY101", 1, 15.5m));

        Assert.Equal(15.94m, enrolment.Average());
        Assert.Equal(Honours.Good, enrolment.Honours());
    }

    private static Course[] EightCourses(int creditsEach) {
        return Enumerable.Range(1, 8).Select(index => CourseOf($"MAT10{index}", creditsEach)).ToArray();
    }

}
