using Education.Domain;

using JustDummies;

using Xunit;

namespace Education.Tests.Expert;

// The hand-completed generators are code of this project: each must draw only what its type's constructor accepts.
public sealed class GeneratorsTests {

    [Fact]
    public void Every_generator_draws_an_object_its_constructor_accepts() {
        // Act
        StudentNumber number    = Any.StudentNumber().Generate();
        Course        course    = Any.Course().Generate();
        Grade         grade     = Any.Grade().Generate();
        Semester      semester  = Any.Semester().Generate();
        Student       student   = Any.Student().Generate();
        Enrolment     enrolment = Any.Enrolment().Generate();

        // Assert
        Assert.NotNull(number);
        Assert.NotNull(course);
        Assert.NotNull(grade);
        Assert.NotNull(semester);
        Assert.NotNull(student);
        Assert.NotNull(enrolment);
    }

    [Fact]
    public void A_semester_draws_both_terms() {
        // Act
        HashSet<Term> terms = Any.ListOf(Any.Semester()).WithCount(200).Generate().Select(semester => semester.Term).ToHashSet();

        // Assert
        Assert.Equal(2, terms.Count);
    }

}
