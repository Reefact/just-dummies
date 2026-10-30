using Education.Domain;
using JustDummies;
using JustDummies.Xunit;
using Xunit;

namespace Education.Tests;

// One harness test per scaffolded generator: 500 draws, and no constructor may throw.
[Reproducible]
public sealed class ScaffoldedGeneratorTests {

    private const int Draws = 500;

    private static void DrawMany<T>(IAny<T> generator) {
        for (int draw = 0; draw < Draws; draw++) {
            generator.Generate();
        }
    }

    [Fact]
    public void Scaffolded_AnyGrade_always_builds_a_valid_Grade() {
        DrawMany(new AnyGrade());
    }

    [Fact]
    public void Scaffolded_AnyStudentNumber_always_builds_a_valid_StudentNumber() {
        DrawMany(new AnyStudentNumber());
    }

    [Fact]
    public void Scaffolded_AnyCourse_always_builds_a_valid_Course() {
        DrawMany(new AnyCourse());
    }

    [Fact]
    public void Scaffolded_AnyStudent_always_builds_a_valid_Student() {
        DrawMany(new AnyStudent());
    }

    [Fact]
    public void Scaffolded_AnySemester_always_builds_a_valid_Semester() {
        DrawMany(Arbitrary.Semester());
    }

    [Fact]
    public void Scaffolded_AnyEnrolment_always_builds_a_valid_Enrolment() {
        DrawMany(Any.Enrolment());
    }

    // The two probe types whose guards the scaffolder claimed to read in full (recap: 'guard', nothing to verify).
    [Fact]
    public void Scaffolded_AnySplitRangeMark_always_builds_a_valid_SplitRangeMark() {
        DrawMany(new AnySplitRangeMark());
    }
}
