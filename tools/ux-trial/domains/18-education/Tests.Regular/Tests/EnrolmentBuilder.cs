using Education.Domain;

using JustDummies;

namespace Education.Tests.Regular;

// The student, the semester and the codes and titles of the courses never matter to the rules under test:
// they are drawn. What does matter, the credits and the grades, is passed in by each test as a literal.
internal static class EnrolmentBuilder {

    public static List<Course> CoursesWorth(params int[] credits) {
        List<Course> anyCourses = Any.ListOf(Any.Course())
                                     .WithCount(credits.Length)
                                     .DistinctBy(course => course.Code)
                                     .Generate();

        List<Course> courses = new List<Course>();
        for (int i = 0; i < credits.Length; i++) {
            courses.Add(new Course(anyCourses[i].Code, anyCourses[i].Title, credits[i]));
        }

        return courses;
    }

    public static Enrolment Open(params int[] credits) {
        return new Enrolment(Any.Student().Generate(), Any.Semester().Generate(), CoursesWorth(credits));
    }

    // Records one grade per course, in order, and stops there: the enrolment stays open.
    public static Enrolment Graded(params (int Credits, decimal Grade)[] lines) {
        Enrolment enrolment = Open(lines.Select(line => line.Credits).ToArray());

        for (int i = 0; i < lines.Length; i++) {
            enrolment.RecordGrade(enrolment.Courses[i].Code, new Grade(lines[i].Grade));
        }

        return enrolment;
    }

    public static Enrolment Closed(params (int Credits, decimal Grade)[] lines) {
        Enrolment enrolment = Graded(lines);

        enrolment.Close();

        return enrolment;
    }

}
