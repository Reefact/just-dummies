using Education.Domain;

using JustDummies;

namespace Education.Tests.Expert;

// What the enrolment tests are about is the courses' credits and the grades; the codes, titles, student and semester
// are whatever the generators draw.
internal static class Fixtures {

    // Courses of the given credits, with distinct codes (an enrolment refuses a course twice).
    internal static List<Course> CoursesWithCredits(params int[] credits) {
        List<Course> anyCourses = Any.ListOf(Any.Course()).WithCount(credits.Length).Distinct().Generate();
        List<Course> courses    = new List<Course>();

        for (int i = 0; i < credits.Length; i++) {
            courses.Add(new Course(anyCourses[i].Code, anyCourses[i].Title, credits[i]));
        }

        return courses;
    }

    internal static Enrolment OpenEnrolmentOf(IReadOnlyList<Course> courses) {
        return Any.Enrolment().WithCourses(courses).Generate();
    }

    // One course per result, in the order given, each graded and the enrolment still open.
    internal static Enrolment GradedEnrolment(params (int Credits, decimal Grade)[] results) {
        List<Course> courses   = CoursesWithCredits(results.Select(result => result.Credits).ToArray());
        Enrolment    enrolment = OpenEnrolmentOf(courses);

        for (int i = 0; i < results.Length; i++) {
            enrolment.RecordGrade(courses[i].Code, new Grade(results[i].Grade));
        }

        return enrolment;
    }

    internal static Enrolment ClosedEnrolment(params (int Credits, decimal Grade)[] results) {
        Enrolment enrolment = GradedEnrolment(results);

        enrolment.Close();

        return enrolment;
    }

}
