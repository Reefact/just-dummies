using Education.Domain;

using JustDummies;

using Xunit;

namespace Education.Tests.Regular;

public sealed class EnrolmentTests {

    public sealed class Construction {

        [Fact]
        public void A_new_enrolment_is_open_and_keeps_its_student_semester_and_courses_in_order() {
            // Arrange
            Student      anyStudent  = Any.Student().Generate();
            Semester     anySemester = Any.Semester().Generate();
            List<Course> courses     = EnrolmentBuilder.CoursesWorth(6, 4, 3);

            // Act
            Enrolment enrolment = new Enrolment(anyStudent, anySemester, courses);

            // Assert
            Assert.Same(anyStudent, enrolment.Student);
            Assert.Same(anySemester, enrolment.Semester);
            Assert.Equal(EnrolmentStatus.Open, enrolment.Status);
            Assert.Equal(courses, enrolment.Courses);
        }

        [Fact]
        public void The_credits_are_the_sum_of_the_courses_credits() {
            // Act
            Enrolment enrolment = EnrolmentBuilder.Open(6, 4, 3);

            // Assert
            Assert.Equal(13, enrolment.Credits);
        }

        [Fact]
        public void The_enrolment_keeps_its_own_copy_of_the_courses() {
            // Arrange
            List<Course> courses = EnrolmentBuilder.CoursesWorth(6, 4);

            Enrolment enrolment = new Enrolment(Any.Student().Generate(), Any.Semester().Generate(), courses);

            // Act
            courses.Clear();

            // Assert
            Assert.Equal(2, enrolment.Courses.Count);
            Assert.Equal(10, enrolment.Credits);
        }

        [Fact]
        public void A_null_student_is_refused() {
            // Act
            ArgumentNullException exception = Assert.Throws<ArgumentNullException>(() => new Enrolment(null!, Any.Semester().Generate(), EnrolmentBuilder.CoursesWorth(6)));

            // Assert
            Assert.Equal("student", exception.ParamName);
        }

        [Fact]
        public void A_null_semester_is_refused() {
            // Act
            ArgumentNullException exception = Assert.Throws<ArgumentNullException>(() => new Enrolment(Any.Student().Generate(), null!, EnrolmentBuilder.CoursesWorth(6)));

            // Assert
            Assert.Equal("semester", exception.ParamName);
        }

        [Fact]
        public void A_null_course_list_is_refused() {
            // Act
            ArgumentNullException exception = Assert.Throws<ArgumentNullException>(() => new Enrolment(Any.Student().Generate(), Any.Semester().Generate(), null!));

            // Assert
            Assert.Equal("courses", exception.ParamName);
        }

        [Fact]
        public void An_enrolment_without_course_is_refused() {
            // Act
            ArgumentException exception = Assert.Throws<ArgumentException>(() => new Enrolment(Any.Student().Generate(), Any.Semester().Generate(), new List<Course>()));

            // Assert
            Assert.Equal("courses", exception.ParamName);
        }

        [Fact]
        public void Eight_courses_are_accepted() {
            // Act
            Enrolment enrolment = EnrolmentBuilder.Open(4, 4, 4, 4, 4, 4, 4, 4);

            // Assert
            Assert.Equal(8, enrolment.Courses.Count);
        }

        [Fact]
        public void Nine_courses_are_refused_even_with_credits_to_spare() {
            // Arrange
            List<Course> nineCourses = EnrolmentBuilder.CoursesWorth(1, 1, 1, 1, 1, 1, 1, 1, 1);

            // Act
            ArgumentException exception = Assert.Throws<ArgumentException>(() => new Enrolment(Any.Student().Generate(), Any.Semester().Generate(), nineCourses));

            // Assert
            Assert.Equal("courses", exception.ParamName);
        }

        [Fact]
        public void Thirty_six_credits_are_accepted() {
            // Act
            Enrolment enrolment = EnrolmentBuilder.Open(30, 6);

            // Assert
            Assert.Equal(Enrolment.MaximumCredits, enrolment.Credits);
        }

        [Fact]
        public void Thirty_seven_credits_are_refused() {
            // Arrange
            List<Course> courses = EnrolmentBuilder.CoursesWorth(30, 7);

            // Act
            ArgumentException exception = Assert.Throws<ArgumentException>(() => new Enrolment(Any.Student().Generate(), Any.Semester().Generate(), courses));

            // Assert
            Assert.Equal("courses", exception.ParamName);
        }

        [Fact]
        public void Two_courses_with_the_same_code_are_refused_whatever_their_titles_and_credits() {
            // Arrange
            Course anyCourse         = Any.Course().Generate();
            Course anySameCodeCourse = Any.Course().WithCode(anyCourse.Code).Generate();

            // Act
            ArgumentException exception = Assert.Throws<ArgumentException>(() => new Enrolment(Any.Student().Generate(), Any.Semester().Generate(), [anyCourse, anySameCodeCourse]));

            // Assert
            Assert.Equal("courses", exception.ParamName);
        }

        [Fact]
        public void The_student_is_not_checked_against_the_semester() {
            // Arrange: a student of the 2099 intake, born in 1900, in a semester of 2000
            Student  student  = new Student(new StudentNumber("STU-2099-00001"), "Ada Lovelace", new DateOnly(1900, 1, 1));
            Semester semester = new Semester(2000, Term.Autumn, new DateOnly(2000, 9, 1), new DateOnly(2001, 1, 31));

            // Act
            Enrolment enrolment = new Enrolment(student, semester, EnrolmentBuilder.CoursesWorth(6));

            // Assert
            Assert.Same(student, enrolment.Student);
        }

        [Fact(Skip = "domain defect (deterministic, no seed needed): a null course in the list escapes as a NullReferenceException instead of an ArgumentException (the model states that null items are not checked)")]
        public void A_null_course_in_the_list_is_refused_with_an_argument_exception() {
            // Arrange
            List<Course?> courses = [Any.Course().Generate(), null];

            // Act
            Exception? exception = Record.Exception(() => new Enrolment(Any.Student().Generate(), Any.Semester().Generate(), courses!));

            // Assert
            Assert.IsAssignableFrom<ArgumentException>(exception);
        }

    }

    public sealed class RecordingGrades {

        [Fact]
        public void A_recorded_grade_is_returned_for_its_course() {
            // Arrange
            Enrolment enrolment = EnrolmentBuilder.Open(6, 4);
            Grade     grade     = new Grade(14.5m);

            // Act
            enrolment.RecordGrade(enrolment.Courses[1].Code, grade);

            // Assert
            Assert.Same(grade, enrolment.GradeFor(enrolment.Courses[1].Code));
            Assert.Null(enrolment.GradeFor(enrolment.Courses[0].Code));
        }

        [Fact]
        public void A_grade_can_be_overwritten_while_the_enrolment_is_open() {
            // Arrange
            Enrolment enrolment = EnrolmentBuilder.Open(6);
            string    code      = enrolment.Courses[0].Code;

            enrolment.RecordGrade(code, new Grade(8m));

            // Act
            enrolment.RecordGrade(code, new Grade(12.5m));

            // Assert
            Assert.Equal(new Grade(12.5m), enrolment.GradeFor(code));
        }

        [Fact]
        public void A_null_grade_is_refused() {
            // Arrange
            Enrolment enrolment = EnrolmentBuilder.Open(6);

            // Act
            ArgumentNullException exception = Assert.Throws<ArgumentNullException>(() => enrolment.RecordGrade(enrolment.Courses[0].Code, null!));

            // Assert
            Assert.Equal("grade", exception.ParamName);
            Assert.Null(enrolment.GradeFor(enrolment.Courses[0].Code));
        }

        [Fact]
        public void A_grade_for_a_course_the_student_is_not_enrolled_in_is_refused() {
            // Arrange
            Enrolment enrolment = EnrolmentBuilder.Open(6, 4);
            Course    anyOtherCourse = Any.Course().WithCode(c => c.Except(enrolment.Courses[0].Code, enrolment.Courses[1].Code)).Generate();

            // Act
            InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() => enrolment.RecordGrade(anyOtherCourse.Code, new Grade(12m)));

            // Assert
            Assert.Contains(anyOtherCourse.Code, exception.Message);
            Assert.Null(enrolment.GradeFor(anyOtherCourse.Code));
        }

        [Fact]
        public void A_grade_for_a_null_course_code_is_refused_as_an_unknown_course() {
            // Arrange
            Enrolment enrolment = EnrolmentBuilder.Open(6);

            // Assert
            Assert.Throws<InvalidOperationException>(() => enrolment.RecordGrade(null!, new Grade(12m)));
        }

        [Fact]
        public void A_closed_enrolment_refuses_any_further_grade_and_keeps_the_ones_it_has() {
            // Arrange
            Enrolment enrolment = EnrolmentBuilder.Closed((6, 12m));
            string    code      = enrolment.Courses[0].Code;

            // Act
            Assert.Throws<InvalidOperationException>(() => enrolment.RecordGrade(code, new Grade(20m)));

            // Assert
            Assert.Equal(new Grade(12m), enrolment.GradeFor(code));
        }

        [Fact]
        public void The_grade_of_an_unknown_course_is_null() {
            // Arrange
            Enrolment enrolment = EnrolmentBuilder.Open(6);
            Course    anyOtherCourse = Any.Course().WithCode(c => c.DifferentFrom(enrolment.Courses[0].Code)).Generate();

            // Assert
            Assert.Null(enrolment.GradeFor(anyOtherCourse.Code));
        }

        [Fact]
        public void The_grade_of_a_null_code_is_refused_with_a_null_argument_exception() {
            // Arrange
            Enrolment enrolment = EnrolmentBuilder.Open(6);

            // Assert
            Assert.Throws<ArgumentNullException>(() => enrolment.GradeFor(null!));
        }

    }

    public sealed class Closing {

        [Fact]
        public void An_enrolment_cannot_be_closed_while_a_course_has_no_grade() {
            // Arrange
            Enrolment enrolment = EnrolmentBuilder.Open(6, 4, 3);

            enrolment.RecordGrade(enrolment.Courses[0].Code, new Grade(12m));

            // Act
            InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() => enrolment.Close());

            // Assert
            Assert.Contains("2 course(s)", exception.Message);
            Assert.Equal(EnrolmentStatus.Open, enrolment.Status);
        }

        [Fact]
        public void An_enrolment_with_no_grade_at_all_cannot_be_closed() {
            // Arrange
            Enrolment enrolment = EnrolmentBuilder.Open(6);

            // Assert
            Assert.Throws<InvalidOperationException>(() => enrolment.Close());
        }

        [Fact]
        public void A_fully_graded_enrolment_closes() {
            // Arrange
            Enrolment enrolment = EnrolmentBuilder.Graded((6, 12m), (4, 9.5m));

            // Act
            enrolment.Close();

            // Assert
            Assert.Equal(EnrolmentStatus.Closed, enrolment.Status);
        }

        [Fact]
        public void Overwriting_a_grade_does_not_count_twice_towards_closing() {
            // Arrange
            Enrolment enrolment = EnrolmentBuilder.Open(6, 4);
            string    code      = enrolment.Courses[0].Code;

            enrolment.RecordGrade(code, new Grade(8m));
            enrolment.RecordGrade(code, new Grade(9m));

            // Assert
            Assert.Throws<InvalidOperationException>(() => enrolment.Close());
        }

        [Fact]
        public void Closing_a_closed_enrolment_again_succeeds_and_changes_nothing() {
            // Arrange
            Enrolment enrolment = EnrolmentBuilder.Closed((6, 12m));

            // Act
            enrolment.Close();

            // Assert
            Assert.Equal(EnrolmentStatus.Closed, enrolment.Status);
            Assert.Equal(12m, enrolment.Average());
        }

    }

    public sealed class Averaging {

        [Fact]
        public void The_average_is_refused_while_the_enrolment_is_open() {
            // Arrange
            Enrolment enrolment = EnrolmentBuilder.Graded((6, 12m));

            // Assert
            Assert.Throws<InvalidOperationException>(() => enrolment.Average());
        }

        [Fact]
        public void The_average_of_a_single_course_is_its_grade() {
            // Arrange
            Enrolment enrolment = EnrolmentBuilder.Closed((6, 13.5m));

            // Assert
            Assert.Equal(13.5m, enrolment.Average());
        }

        [Fact]
        public void The_average_weighs_each_grade_by_its_credits() {
            // Arrange: (6 * 12 + 3 * 15) / 9 = 13
            Enrolment enrolment = EnrolmentBuilder.Closed((6, 12m), (3, 15m));

            // Assert
            Assert.Equal(13m, enrolment.Average());
        }

        [Fact]
        public void The_average_is_not_the_plain_mean_when_the_credits_differ() {
            // Arrange: a plain mean would give 10, the weighted one (1 * 20 + 9 * 0) / 10 = 2
            Enrolment enrolment = EnrolmentBuilder.Closed((1, 20m), (9, 0m));

            // Assert
            Assert.Equal(2m, enrolment.Average());
        }

        [Fact]
        public void The_average_of_equal_grades_is_that_grade_whatever_the_credits() {
            // Arrange
            int anyCredits      = Any.Int32().Between(1, 4).Generate();
            int anyOtherCredits = Any.Int32().Between(1, 4).Generate();
            int anyLastCredits  = Any.Int32().Between(1, 4).Generate();

            Enrolment enrolment = EnrolmentBuilder.Closed((anyCredits, 13.5m), (anyOtherCredits, 13.5m), (anyLastCredits, 13.5m));

            // Assert
            Assert.Equal(13.5m, enrolment.Average());
        }

        [Fact]
        public void The_average_is_rounded_to_two_decimals() {
            // Arrange: (3 * 10 + 5 * 10.5) / 8 = 10.3125
            Enrolment enrolment = EnrolmentBuilder.Closed((3, 10m), (5, 10.5m));

            // Assert
            Assert.Equal(10.31m, enrolment.Average());
        }

        [Fact]
        public void A_midpoint_is_rounded_away_from_zero_not_to_even() {
            // Arrange: (3 * 12 + 5 * 9) / 8 = 10.125, which banker's rounding would give as 10.12
            Enrolment enrolment = EnrolmentBuilder.Closed((3, 12m), (5, 9m));

            // Assert
            Assert.Equal(10.13m, enrolment.Average());
        }

        [Fact]
        public void An_average_of_zero_is_zero() {
            // Arrange
            Enrolment enrolment = EnrolmentBuilder.Closed((6, 0m), (4, 0m));

            // Assert
            Assert.Equal(0m, enrolment.Average());
        }

    }

    public sealed class Passing {

        [Fact]
        public void An_average_of_exactly_ten_is_a_pass() {
            // Arrange
            Enrolment enrolment = EnrolmentBuilder.Closed((6, 10m), (4, 10m));

            // Assert
            Assert.True(enrolment.IsPassed());
        }

        [Fact]
        public void An_average_just_below_ten_is_a_fail() {
            // Arrange: (6 * 10 + 4 * 9.5) / 10 = 9.8
            Enrolment enrolment = EnrolmentBuilder.Closed((6, 10m), (4, 9.5m));

            // Assert
            Assert.False(enrolment.IsPassed());
        }

        [Fact]
        public void A_strong_average_does_not_save_an_eliminatory_grade() {
            // Arrange: average (1 * 4.5 + 1 * 20) / 2 = 12.25, but 4.5 is below 5
            Enrolment enrolment = EnrolmentBuilder.Closed((1, 4.5m), (1, 20m));

            // Assert
            Assert.Equal(12.25m, enrolment.Average());
            Assert.False(enrolment.IsPassed());
        }

        [Fact]
        public void A_grade_of_exactly_five_is_not_eliminatory() {
            // Arrange
            Enrolment enrolment = EnrolmentBuilder.Closed((1, 5m), (1, 20m));

            // Assert
            Assert.True(enrolment.IsPassed());
        }

        [Fact]
        public void An_eliminatory_grade_on_a_small_course_still_fails_the_semester() {
            // Arrange: weighted average (1 * 0 + 29 * 20) / 30 = 19.33
            Enrolment enrolment = EnrolmentBuilder.Closed((1, 0m), (29, 20m));

            // Assert
            Assert.False(enrolment.IsPassed());
        }

        [Fact]
        public void Whether_the_enrolment_is_passed_cannot_be_asked_while_it_is_open() {
            // Arrange
            Enrolment enrolment = EnrolmentBuilder.Graded((6, 12m));

            // Assert
            Assert.Throws<InvalidOperationException>(() => enrolment.IsPassed());
        }

    }

    public sealed class AcquiringCredits {

        [Fact]
        public void A_passed_semester_acquires_every_credit_by_compensation() {
            // Arrange: average (6 * 8 + 6 * 14) / 12 = 11, the 8 is compensated
            Enrolment enrolment = EnrolmentBuilder.Closed((6, 8m), (6, 14m));

            // Assert
            Assert.True(enrolment.IsPassed());
            Assert.Equal(12, enrolment.AcquiredCredits());
        }

        [Fact]
        public void A_failed_semester_acquires_only_the_courses_graded_ten_or_more() {
            // Arrange: average (6 * 10 + 4 * 9.5 + 3 * 5) / 13 = 8.54
            Enrolment enrolment = EnrolmentBuilder.Closed((6, 10m), (4, 9.5m), (3, 5m));

            // Assert
            Assert.False(enrolment.IsPassed());
            Assert.Equal(6, enrolment.AcquiredCredits());
        }

        [Fact]
        public void An_eliminatory_grade_leaves_the_other_passed_courses_acquired() {
            // Arrange: average (4 * 4 + 6 * 19 + 2 * 12) / 12 = 12.83 but the 4 eliminates
            Enrolment enrolment = EnrolmentBuilder.Closed((4, 4m), (6, 19m), (2, 12m));

            // Assert
            Assert.False(enrolment.IsPassed());
            Assert.Equal(8, enrolment.AcquiredCredits());
        }

        [Fact]
        public void A_failed_semester_with_no_course_at_ten_acquires_nothing() {
            // Arrange
            Enrolment enrolment = EnrolmentBuilder.Closed((6, 9.5m), (4, 9m));

            // Assert
            Assert.Equal(0, enrolment.AcquiredCredits());
        }

        [Fact]
        public void The_acquired_credits_cannot_be_asked_while_the_enrolment_is_open() {
            // Arrange
            Enrolment enrolment = EnrolmentBuilder.Graded((6, 12m));

            // Assert
            Assert.Throws<InvalidOperationException>(() => enrolment.AcquiredCredits());
        }

    }

    public sealed class Honours {

        [Theory]
        [InlineData(20, Education.Domain.Honours.VeryGood)]
        [InlineData(16, Education.Domain.Honours.VeryGood)]
        [InlineData(15.5, Education.Domain.Honours.Good)]
        [InlineData(14, Education.Domain.Honours.Good)]
        [InlineData(13.5, Education.Domain.Honours.Fair)]
        [InlineData(12, Education.Domain.Honours.Fair)]
        [InlineData(11.5, Education.Domain.Honours.Pass)]
        [InlineData(10, Education.Domain.Honours.Pass)]
        [InlineData(9.5, Education.Domain.Honours.None)]
        [InlineData(0, Education.Domain.Honours.None)]
        public void The_honours_follow_the_average_thresholds(double grade, Education.Domain.Honours expected) {
            // Arrange
            int anyCredits = Any.Int32().Between(1, 30).Generate();

            Enrolment enrolment = EnrolmentBuilder.Closed((anyCredits, (decimal)grade));

            // Assert
            Assert.Equal(expected, enrolment.Honours());
        }

        [Fact]
        public void An_eliminatory_grade_gives_no_honours_whatever_the_average() {
            // Arrange: average (1 * 4.5 + 29 * 20) / 30 = 19.52
            Enrolment enrolment = EnrolmentBuilder.Closed((1, 4.5m), (29, 20m));

            // Assert
            Assert.Equal(Education.Domain.Honours.None, enrolment.Honours());
        }

        [Fact]
        public void The_honours_are_read_on_the_rounded_average() {
            // Arrange: (3 * 16 + 5 * 15.5) / 8 = 15.6875, rounded 15.69: Good
            Enrolment enrolment = EnrolmentBuilder.Closed((3, 16m), (5, 15.5m));

            // Assert
            Assert.Equal(Education.Domain.Honours.Good, enrolment.Honours());
        }

        [Fact]
        public void The_honours_cannot_be_asked_while_the_enrolment_is_open() {
            // Arrange
            Enrolment enrolment = EnrolmentBuilder.Graded((6, 12m));

            // Assert
            Assert.Throws<InvalidOperationException>(() => enrolment.Honours());
        }

    }

    public sealed class Generator {

        [Fact]
        public void Every_enrolment_the_generator_draws_is_valid_and_open() {
            // Arrange + Act + Assert: the generator itself is test infrastructure, so it is smoke-checked here.
            for (int i = 0; i < 300; i++) {
                Enrolment anyEnrolment = Any.Enrolment().Generate();

                Assert.Equal(EnrolmentStatus.Open, anyEnrolment.Status);
                Assert.InRange(anyEnrolment.Credits, 1, Enrolment.MaximumCredits);
            }
        }

    }

}
